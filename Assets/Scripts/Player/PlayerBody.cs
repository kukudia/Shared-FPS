using Fusion;
using UnityEngine;
using UnityEngine.Rendering;

namespace Projectiles
{
    public class PlayerBody : ContextBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private GameObject _visual;
        [SerializeField] private Transform _capTransform;
        [SerializeField] private float _capImpulse = 10f;
        [SerializeField] private GameObject _deathEffectPrefab;

        private PlayerAgent _agent;
        private HitboxRoot _hitboxRoot;

        private bool _lastGameStartState;

        public override void Spawned()
        {
            _root.SetActive(_agent.Health.IsAlive);
            _agent.Health.FatalHitTaken += OnFatalHit;

            // 记录初始状态
            _lastGameStartState = _agent.gameStart;

            ApplyVisualState(_agent.gameStart);
        }

        public override void Render()
        {
            // 当网络变量变化时自动同步
            if (_agent == null) return;

            if (_agent.gameStart != _lastGameStartState)
            {
                _lastGameStartState = _agent.gameStart;
                ApplyVisualState(_agent.gameStart);
            }
        }

        private void ApplyVisualState(bool gameStarted)
        {
            if (!gameStarted)
                return;

            var renderers = _visual.GetComponentsInChildren<MeshRenderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].shadowCastingMode =
                    HasInputAuthority ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (_hitboxRoot != null)
                _hitboxRoot.HitboxRootActive = _agent.Health.IsAlive;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (_agent != null)
            {
                _agent.Health.FatalHitTaken -= OnFatalHit;
            }
        }

        protected void Awake()
        {
            _agent = GetComponent<PlayerAgent>();
            _hitboxRoot = GetComponent<HitboxRoot>();
        }

        private void OnFatalHit(HitData hit)
        {
            _agent.KCC.SetActive(false);
            _root.SetActive(false);

            var deathEffect = Runner.InstantiateInRunnerScene(_deathEffectPrefab);
            deathEffect.transform.position = transform.position + Vector3.up;

            if (Runner.Config.PeerMode == NetworkProjectConfig.PeerModes.Multiple)
            {
                Runner.AddVisibilityNodes(deathEffect.gameObject);
            }
        }
    }
}
