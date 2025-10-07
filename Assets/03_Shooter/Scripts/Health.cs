using System;
using System.Linq;
using Fusion;
using Starter.Shooter;
using UnityEngine;


/// <summary>
/// A common component that represents entity health.
/// It is used for both players and chickens.
/// </summary>
public class Health : NetworkBehaviour
{
    [Header("Setup")]
    public int InitialHealth = 3;
    public float DeathTime;

    [Header("References")]
    public Transform ScalingRoot;
    public GameObject VisualRoot;
    public GameObject DeathRoot;

    public Action<Health> Killed;

    public bool IsAlive => CurrentHealth > 0;
    public bool IsFinished => _networkHealth <= 0 && _deathCooldown.Expired(Runner);
    public int CurrentHealth => HasStateAuthority ? _networkHealth : _localHealth;

    [Networked]
    private int _networkHealth { get; set; }
    [Networked]
    private TickTimer _deathCooldown { get; set; }
    [Networked, Capacity(10)] // 设置容量为10，可以根据需要调整
    [SerializeField] private NetworkArray<PlayerRef> DamageSources => default;

    private int _lastVisibleHealth;
    private int _localHealth;
    private int _localDataExpirationTick;

    // 获取伤害来源数组的只读副本
    public PlayerRef[] GetDamageSources()
    {
        var sources = new PlayerRef[DamageSources.Length];
        for (int i = 0; i < DamageSources.Length; i++)
        {
            sources[i] = DamageSources[i];
        }
        return sources;
    }

    // 获取最后一个伤害来源
    public PlayerRef GetLastDamageSource()
    {
        for (int i = DamageSources.Length - 1; i >= 0; i--)
        {
            if (DamageSources[i] != default(PlayerRef))
            {
                return DamageSources[i];
            }
        }
        return default(PlayerRef);
    }

    // 添加伤害来源
    private void AddDamageSource(PlayerRef playerSource)
    {
        if (!DamageSources.Contains(playerSource))
        {
            // 将所有元素向前移动一位，为新的伤害来源腾出位置
            for (int i = 0; i < DamageSources.Length - 1; i++)
            {
                DamageSources.Set(i, DamageSources[i + 1]);
            }

            // 将新的伤害来源添加到数组末尾
            DamageSources.Set(DamageSources.Length - 1, playerSource);
        }
    }

    public void TakeHit(int damage, bool reportKill = false)
    {
        if (IsAlive == false)
            return;

        RPC_TakeHit(damage, reportKill);

        if (HasStateAuthority == false)
        {
            // To have responsive hit reactions on all clients we trust
            // local health value for some time after the health change
            _localHealth = Mathf.Max(0, _localHealth - damage);
            _localDataExpirationTick = GetLocalDataExpirationTick();
        }
    }

    public void Revive()
    {
        _networkHealth = InitialHealth;
        _deathCooldown = default;

        // 清空伤害来源数组
        for (int i = 0; i < DamageSources.Length; i++)
        {
            DamageSources.Set(i, default(PlayerRef));
        }
    }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            // Set initial health
            Revive();
        }

        _localHealth = _networkHealth;
        _lastVisibleHealth = _networkHealth;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        Killed = null;
    }

    public override void Render()
    {
        if (Object.LastReceiveTick >= _localDataExpirationTick)
        {
            // Local health data expired, just use network health from now on
            _localHealth = _networkHealth;
        }

        VisualRoot.SetActive(IsAlive && IsAliveInterpolated());
        DeathRoot.SetActive(IsAlive == false);

        // Check if hit should be shown
        if (_lastVisibleHealth > CurrentHealth)
        {
            // Show hit reaction by simple scale (but not for local player).
            // Scaling root scale is lerped back to one in the Player script.
            if (HasStateAuthority == false && ScalingRoot != null)
            {
                ScalingRoot.localScale = new Vector3(0.85f, 1.15f, 0.85f);
            }
        }

        _lastVisibleHealth = CurrentHealth;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_TakeHit(int damage, bool reportKill = false, RpcInfo info = default)
    {
        if (IsAlive == false)
            return;

        // 添加伤害来源
        AddDamageSource(info.Source);

        _networkHealth -= damage;

        if (IsAlive == false)
        {
            // Entity died, let's start death cooldown
            _networkHealth = 0;
            _deathCooldown = TickTimer.CreateFromSeconds(Runner, DeathTime);

            if (reportKill)
            {
                // We are using targeted RPC to send kill confirmation
                // only to the killer client
                RPC_KilledBy(info.Source);
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_KilledBy([RpcTarget] PlayerRef playerRef)
    {
        Killed?.Invoke(this);
    }

    private int GetLocalDataExpirationTick()
    {
        // How much time it takes to receive response from the server
        float expirationTime = (float)Runner.GetPlayerRtt(Runner.LocalPlayer);

        // Additional safety 200 ms
        expirationTime += 0.2f;

        int expirationTicks = Mathf.CeilToInt(expirationTime * Runner.TickRate);
        //Debug.Log($"Expiration time {expirationTime}, ticks {expirationTicks}");

        return Runner.Tick + expirationTicks;
    }

    private bool IsAliveInterpolated()
    {
        // We use interpolated value when checking if object should be made visible in Render.
        // This helps with showing player visual at the correct position right away after respawn
        // (= player won't be visible before KCC teleport that is interpolated as well).
        var interpolator = new NetworkBehaviourBufferInterpolator(this);
        return interpolator.Int(nameof(_networkHealth)) > 0;
    }

    private void OnDrawGizmos()
    {
        if (HasStateAuthority)
        {
            foreach (PlayerRef playerRef in DamageSources)
            {
                var player = Runner.GetPlayerObject(playerRef).GetComponent<Player>();
                Gizmos.DrawLine(transform.position, player.transform.position);
            }
        }
    }
}
