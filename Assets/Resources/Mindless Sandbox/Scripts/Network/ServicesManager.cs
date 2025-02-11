using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Netcode.Transports.Facepunch;
using Steamworks;
using Steamworks.Data;
using System;

namespace MindlessSandbox {
    [RequireComponent(typeof(LobbySaver))]
    public class ServicesManager : MonoBehaviour
    {
        public static ServicesManager Instance { get; private set; }

        //Server Properties
        public bool ServerJoinable = true;
        public bool ServerPublic = true;
        public bool ServerModded = false;
        private bool ServerCommunityHosted = Application.platform.Equals(RuntimePlatform.WindowsServer) || Application.platform.Equals(RuntimePlatform.LinuxServer);

        private const string ModdedKey = "md";
        private const string CommunityHostedKey = "ch";

        // Steam/Lobby Stuff
        public FacepunchTransport transport;

        void OnEnable()
        {
            SteamMatchmaking.OnLobbyCreated += LobbyCreated;
            SteamMatchmaking.OnLobbyEntered += LobbyEntered;
            SteamFriends.OnGameLobbyJoinRequested += GameLobbyJoinRequested;
        }

        void OnDisable()
        {
            SteamMatchmaking.OnLobbyCreated -= LobbyCreated;
            SteamMatchmaking.OnLobbyEntered -= LobbyEntered;
            SteamFriends.OnGameLobbyJoinRequested -= GameLobbyJoinRequested;
        }

        private async void GameLobbyJoinRequested(Lobby lobby, SteamId id)
        {
            await lobby.Join();
            Debug.Log($"Game Lobby Join Requested {lobby} + {id}");
        }

        private void LobbyEntered(Lobby lobby)
        {
            LobbySaver.Instance.currentLobby = lobby;
            if (!NetworkManager.Singleton.IsServer)
            {
                transport.targetSteamId = lobby.Owner.Id;
                NetworkManager.Singleton.StartClient();
            }
            Debug.Log($"Lobby Entered {lobby}");
        }

        private void LobbyCreated(Result result, Lobby lobby)
        {
            if (result.Equals(Result.OK))
            {
                lobby.SetJoinable(ServerJoinable);

                if (ServerPublic)
                {
                    lobby.SetPublic();
                }
                else
                {
                    lobby.SetPrivate();
                }

                NetworkManager.Singleton.StartHost();
            }
            Debug.Log($"Lobby Created {result} + {lobby}");
        }

        void Awake()
        {
            Instance = this;
            transport = FindFirstObjectByType<FacepunchTransport>();
        }

        void Start()
        {
            HostLobby();
        }

        private async void HostLobby()
        {
            await SteamMatchmaking.CreateLobbyAsync(16);
        }

        private async void JoinLobby(string lobbyID = "")
        {
            ulong ID;
            if (!ulong.TryParse(lobbyID, out ID))
            {
                Lobby[] lobbies = await SteamMatchmaking.LobbyList.WithSlotsAvailable(1).RequestAsync();
                await lobbies[0].Join();
            }
            else
            {
                Lobby[] lobbies = await SteamMatchmaking.LobbyList.WithSlotsAvailable(1).RequestAsync();

                foreach (Lobby lobby in lobbies)
                {
                    if (lobby.Id == ID)
                    {
                        await lobby.Join();
                        return;
                    }
                }
            }
        }

        void OnDisconnectedFromServer()
        {
            SceneManager.LoadScene(0);
            HostLobby();
        }

        [ContextMenu("Create Room")] public void CreateRoomCommand() => HostLobby();
        [ContextMenu("Join Room")] public void JoinRoomCommand() => JoinLobby();
        //public void ListLobbiesCommand() => ListLobbies();

        [ContextMenu("Shutdown")]
        public void ShutdownCommand()
        {
            NetworkManager.Singleton.Shutdown();
            transport.Shutdown();
        }
    }
}