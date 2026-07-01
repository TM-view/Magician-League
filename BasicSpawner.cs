using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class BasicSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    private NetworkRunner _runner;

    [SerializeField]
    private NetworkPrefabRef _playerPrefab;

    [SerializeField]
    private InputActionReference moveActionReference;

    private readonly Dictionary<PlayerRef, NetworkObject> _spawnedCharacters =
        new Dictionary<PlayerRef, NetworkObject>();

    private InputAction _fallbackMoveAction;

    private InputAction MoveAction =>
        moveActionReference != null ? moveActionReference.action : _fallbackMoveAction;

    private void OnEnable()
    {
        if (moveActionReference != null && moveActionReference.action != null)
        {
            moveActionReference.action.Enable();
            return;
        }

        GetOrCreateFallbackMoveAction().Enable();
    }

    private void OnDisable()
    {
        if (_fallbackMoveAction != null)
        {
            _fallbackMoveAction.Disable();
        }
    }

    public async void StartGame()
    {
        if (_runner != null)
            return;

        _runner = gameObject.AddComponent<NetworkRunner>();

        _runner.ProvideInput = true;

        _runner.AddCallbacks(this);

        var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);

        StartGameArgs args = new StartGameArgs()
        {
            GameMode = GameMode.Shared,

            SessionName = "TestRoom",

            Scene = scene,

            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>(),
        };

        var result = await _runner.StartGame(args);

        if (result.Ok)
        {
            Debug.Log("Fusion Started");
        }
        else
        {
            Debug.LogError($"Fusion Start Failed: {result.ShutdownReason}");
        }
    }

    // =========================
    // PLAYER JOIN
    // =========================

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Player Joined: {player}");

        // ใน Shared Mode ให้แต่ละ client spawn ตัวเองเท่านั้น
        if (player != runner.LocalPlayer)
            return;

        if (_spawnedCharacters.ContainsKey(player))
            return;

        Vector3 spawnPosition = new Vector3(player.RawEncoded * 2, 0, 0);

        NetworkObject obj = runner.Spawn(
            _playerPrefab,
            spawnPosition,
            Quaternion.identity,
            player // inputAuthority = ตัวเอง
        );

        _spawnedCharacters.Add(player, obj);
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (_spawnedCharacters.TryGetValue(player, out NetworkObject obj))
        {
            runner.Despawn(obj);

            _spawnedCharacters.Remove(player);

            Debug.Log($"Player Left: {player}");
        }
    }

    // =========================
    // INPUT
    // =========================

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new NetworkInputData();

        data.Direction = ReadMoveInput();

        input.Set(data);
    }

    private Vector2 ReadMoveInput()
    {
        InputAction moveAction = MoveAction;
        if (moveAction == null)
        {
            return Vector2.zero;
        }

        Vector2 direction = moveAction.ReadValue<Vector2>();
        return direction.sqrMagnitude > 1f ? direction.normalized : direction;
    }

    private InputAction GetOrCreateFallbackMoveAction()
    {
        if (_fallbackMoveAction != null)
        {
            return _fallbackMoveAction;
        }

        _fallbackMoveAction = new InputAction(
            "Move",
            InputActionType.Value,
            expectedControlType: "Vector2"
        );

        _fallbackMoveAction
            .AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        _fallbackMoveAction
            .AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");

        _fallbackMoveAction.AddBinding("<Gamepad>/leftStick");

        return _fallbackMoveAction;
    }

    // =========================
    // UNUSED CALLBACKS
    // =========================

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        SceneManager.LoadScene(0);
        Debug.Log($"Shutdown: {shutdownReason}");
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("Connected To Server");
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.LogWarning($"Disconnected: {reason}");
    }

    public void OnConnectRequest(
        NetworkRunner runner,
        NetworkRunnerCallbackArgs.ConnectRequest request,
        byte[] token
    ) { }

    public void OnConnectFailed(
        NetworkRunner runner,
        NetAddress remoteAddress,
        NetConnectFailedReason reason
    )
    {
        Debug.LogError($"Connect Failed: {reason}");
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }

    public void OnCustomAuthenticationResponse(
        NetworkRunner runner,
        Dictionary<string, object> data
    ) { }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        Debug.Log("Scene Load Done");
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        Debug.Log("Scene Load Start");
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    public void OnReliableDataReceived(
        NetworkRunner runner,
        PlayerRef player,
        ReliableKey key,
        System.ArraySegment<byte> data
    ) { }

    public void OnReliableDataProgress(
        NetworkRunner runner,
        PlayerRef player,
        ReliableKey key,
        float progress
    ) { }
}
