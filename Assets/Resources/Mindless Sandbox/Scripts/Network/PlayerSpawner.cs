using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem;
using Unity.VisualScripting;

[RequireComponent(typeof(PlayerInputManager))]
public class PlayerSpawner : NetworkBehaviour
{
    public static PlayerSpawner Instance { get; private set; }
    [HideInInspector] public PlayerInputManager playerInputManager;

    public Transform[] spawnPoints;

    public List<NetworkPlayer> networkPlayers { get { return FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.InstanceID).ToList(); } }

    void Start()
    {
        Instance = this;
        playerInputManager = PlayerInputManager.instance;
        playerInputManager.onPlayerJoined += playerInput => { SpawnPlayer(playerInput); };
    }

    public void SpawnPlayer(PlayerInput playerInput)
    {
        SpawnPlayerServerRpc(NetworkManager.Singleton.LocalClientId, playerInput.playerIndex);
    }

    [ServerRpc]
    public void SpawnPlayerServerRpc(ulong newClientID, int newPlayerIndex)
    {
        Transform selectedSpawnPoint = spawnPoints[Random.Range(minInclusive: 0, spawnPoints.Length - 1)];
        NetworkObject playerObject = playerInputManager.JoinPlayer(newPlayerIndex).GetComponent<NetworkObject>();
        playerObject.Spawn(true);
        playerObject.ChangeOwnership(newClientID);
    }
}
