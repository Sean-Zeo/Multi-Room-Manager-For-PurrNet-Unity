using PurrNet;
using PurrNet.Modules;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MultiRoomNetworkManager : MonoBehaviour
{
    public static MultiRoomNetworkManager Instance;
    public static NetworkManager networkManager;
    //Optional lobby network player
    public NetworkIdentity lobbyPlayerPrefab;

    [HideInInspector]
    public List<RoomInfo> rooms = new List<RoomInfo>();
    public class RoomInfo
    {
        public string roomName;
        public string roomData;
        public string sceneName;
        public int currentPlayers;
        public int maxPlayers;
        public SceneID scene;
        public List<PlayerID> playerConnections = new List<PlayerID>();
    }
    private readonly Dictionary<PlayerID, RoomInfo> playerToRoom = new();


    private void Awake()
    {
        if (Instance != null)
            Destroy(Instance.gameObject);

        Instance = this;
        networkManager = GetComponent<NetworkManager>();
        DontDestroyOnLoad(this.gameObject);

        networkManager.onPlayerJoined += NetworkManager_onPlayerJoined;
        networkManager.onPlayerLeft += NetworkManager_onPlayerLeft;
        networkManager.onServerConnectionState += NetworkManager_onServerConnectionState;
        networkManager.onClientConnectionState += NetworkManager_onClientConnectionState;

        networkManager.Subscribe<RoomListRequestMessage>(OnRoomListRequest, asServer: true);
        networkManager.Subscribe<CreateRoomMessage>(OnCreateRoom, asServer: true);
        networkManager.Subscribe<JoinRoomMessage>(OnJoinRoom, asServer: true);
    }

    private void OnDestroy()
    {
        networkManager.onPlayerJoined -= NetworkManager_onPlayerJoined;
        networkManager.onPlayerLeft -= NetworkManager_onPlayerLeft;
        networkManager.onServerConnectionState -= NetworkManager_onServerConnectionState;
        networkManager.onClientConnectionState -= NetworkManager_onClientConnectionState;

        networkManager.Unsubscribe<RoomListRequestMessage>(OnRoomListRequest, asServer: true);
        networkManager.Unsubscribe<CreateRoomMessage>(OnCreateRoom, asServer: true);
        networkManager.Unsubscribe<JoinRoomMessage>(OnJoinRoom, asServer: true);
    }

    private void NetworkManager_onPlayerJoined(PlayerID player, bool isReconnect, bool asServer)
    {
        if (!asServer) return;
        if (lobbyPlayerPrefab != null)
        {
            NetworkIdentity newLobbyPlayer = Instantiate(lobbyPlayerPrefab);
            newLobbyPlayer.GiveOwnership(player);
        }
    }

    private void NetworkManager_onPlayerLeft(PlayerID player, bool asServer)
    {
        if (!asServer) return;
        if (playerToRoom.ContainsKey(player))
        {
            RoomInfo info = playerToRoom[player];
            info.currentPlayers--;
            info.playerConnections.Remove(player);
            playerToRoom.Remove(player);

            if (info.currentPlayers <= 0)
            {
                StartCoroutine(UnloadEmptyScene(info.scene));
                rooms.Remove(info);
            }
        }
    }

    private void NetworkManager_onServerConnectionState(PurrNet.Transports.ConnectionState state)
    {
        if (state == PurrNet.Transports.ConnectionState.Disconnected)
        {
            Application.LoadLevel(0);
            Destroy(this.gameObject);
        }
    }

    private void NetworkManager_onClientConnectionState(PurrNet.Transports.ConnectionState state)
    {
        if(state == PurrNet.Transports.ConnectionState.Disconnected)
        {
            Application.LoadLevel(0);
            Destroy(this.gameObject);
        }
    }

    private void OnRoomListRequest(PlayerID player, RoomListRequestMessage msg, bool asServer)
    {
        int n = rooms.Count;
        var resp = new RoomListResponseMessage
        {
            roomNames = new string[n],
            roomDatas = new string[n],
            sceneNames = new string[n],
            currentCounts = new int[n],
            maxCounts = new int[n]
        };

        for (int i = 0; i < n; i++)
        {
            var r = rooms[i];
            resp.roomNames[i] = r.roomName;
            resp.roomDatas[i] = r.roomData;
            resp.sceneNames[i] = r.sceneName;
            resp.currentCounts[i] = r.currentPlayers;
            resp.maxCounts[i] = r.maxPlayers;
        }

        networkManager.Send(player, resp);
    }

    private void OnCreateRoom(PlayerID player, CreateRoomMessage msg, bool asServer)
    {
        if (!asServer) return;
        if (playerToRoom.ContainsKey(player))
        {
            Debug.LogWarning($"[Server] Player {player} already in room; create ignored.");
            return;
        }

        if (rooms.Exists(r => r.roomName == msg.roomName))
        {
            Debug.LogWarning($"[Server] Room '{msg.roomName}' already exists; ignoring.");
            return;
        }

        StartCoroutine(CreateRoomCoroutine(player, msg));
    }

    private void OnJoinRoom(PlayerID player, JoinRoomMessage msg, bool asServer)
    {
        if (!asServer) return;
        if (playerToRoom.ContainsKey(player)) return;

        var info = rooms.Find(r => r.roomName == msg.roomName);
        if (info == null || info.currentPlayers >= info.maxPlayers) return;

        networkManager.scenePlayersModule.MovePlayerToSingleScene(player, info.scene);
        playerToRoom[player] = info;
        info.currentPlayers++;
        info.playerConnections.Add(player);
    }

    IEnumerator CreateRoomCoroutine(PlayerID player, CreateRoomMessage msg)
    {
        var settings = new PurrSceneSettings();
        settings.isPublic = false;
        settings.mode = LoadSceneMode.Additive;
        var scene = networkManager.sceneModule.LoadSceneAsync(msg.sceneName, settings);
        while (!scene.isDone) 
            yield return null;

        SceneID sceneID = networkManager.sceneModule.lastSceneId;
        var info = new RoomInfo
        {
            roomName = msg.roomName,
            roomData = msg.roomData,
            sceneName = msg.sceneName,
            currentPlayers = 0,
            maxPlayers = msg.maxPlayers,
            scene = sceneID
        };

        networkManager.scenePlayersModule.MovePlayerToSingleScene(player, sceneID);
        playerToRoom[player] = info;
        info.currentPlayers++;
        info.playerConnections.Add(player);

        rooms.Add(info);
    }

    IEnumerator UnloadEmptyScene(SceneID sceneID)
    {
        if (networkManager.sceneModule.TryGetSceneState(sceneID, out SceneState state))
        {
            var scene = networkManager.sceneModule.UnloadSceneAsync(state.scene);
            while (!scene.isDone)
                yield return null;
        }
        yield return null;
    }

}
