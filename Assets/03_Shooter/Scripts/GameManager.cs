using System.Collections.Generic;
using System.Linq;
using Fusion;
using Starter.Shooter;
using UnityEngine;

public sealed class GameManager : NetworkBehaviour
{
    public Player PlayerPrefab;

    [Networked] public PlayerRef BestHunter { get; set; }
    public Player LocalPlayer { get; private set; }

    private SpawnPoint[] _spawnPoints;
    private List<PlayerRef> _knownPlayers = new List<PlayerRef>();

    // GUI提示用的队列
    private Queue<string> _notifications = new Queue<string>();
    private float _notificationTimer = 0f;
    private string _currentMessage = "";

    public Vector3 GetSpawnPosition()
    {
        var spawnPoint = _spawnPoints[Random.Range(0, _spawnPoints.Length)];
        var randomPositionOffset = Random.insideUnitCircle * spawnPoint.Radius;
        return spawnPoint.transform.position + new Vector3(randomPositionOffset.x, 0f, randomPositionOffset.y);
    }

    public override void Spawned()
    {
        _spawnPoints = FindObjectsOfType<SpawnPoint>();

        LocalPlayer = Runner.Spawn(PlayerPrefab, GetSpawnPosition(), Quaternion.identity, Runner.LocalPlayer);
        Runner.SetPlayerObject(Runner.LocalPlayer, LocalPlayer.Object);

        _knownPlayers.Clear();
        foreach (var player in Runner.ActivePlayers)
        {
            _knownPlayers.Add(player);
        }
    }

    public override void FixedUpdateNetwork()
    {
        BestHunter = PlayerRef.None;
        int bestHunterKills = 0;

        CheckForNewPlayers();

        foreach (var playerRef in Runner.ActivePlayers)
        {
            var playerObject = Runner.GetPlayerObject(playerRef);
            var player = playerObject.GetComponent<Player>();

            if (player == null)
                continue;

            if (player.Health.IsAlive && player.ChickenKills > bestHunterKills)
            {
                bestHunterKills = player.ChickenKills;
                BestHunter = player.Object.StateAuthority;
            }
        }
    }

    private void CheckForNewPlayers()
    {
        foreach (PlayerRef playerRef in Runner.ActivePlayers)
        {
            if (!_knownPlayers.Contains(playerRef))
            {
                _knownPlayers.Add(playerRef);

                var playerObject = Runner.GetPlayerObject(playerRef);
                var player = playerObject.GetComponent<Player>();
                string playerName = player != null ? player.Nickname : $"Player {playerRef.PlayerId}";

                AddNotification($"{playerName} joined the room.");
            }
        }

        for (int i = _knownPlayers.Count - 1; i >= 0; i--)
        {
            if (!Runner.ActivePlayers.Contains(_knownPlayers[i]))
            {
                _knownPlayers.RemoveAt(i);
                AddNotification($"Player {_knownPlayers[i].PlayerId} left the room.");
            }
        }
    }

    private void AddNotification(string message)
    {
        _notifications.Enqueue(message);
    }

    private void Update()
    {
        if (_notificationTimer <= 0f && _notifications.Count > 0)
        {
            _currentMessage = _notifications.Dequeue();
            _notificationTimer = 3f; // 每条消息显示3秒
        }

        if (_notificationTimer > 0f)
        {
            _notificationTimer -= Time.deltaTime;
        }
    }

    private void OnGUI()
    {
        if (!string.IsNullOrEmpty(_currentMessage) && _notificationTimer > 0f)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 24;
            style.normal.textColor = Color.yellow;

            GUI.Label(new Rect(20, 20, 600, 40), _currentMessage, style);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        LocalPlayer = null;
        _knownPlayers.Clear();
    }
}
