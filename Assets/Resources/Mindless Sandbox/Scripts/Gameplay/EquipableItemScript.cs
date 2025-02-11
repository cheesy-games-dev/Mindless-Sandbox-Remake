using Unity.Netcode;
using UnityEngine;

public class EquipableItemScript : NetworkBehaviour
{
    private void Start()
    {
        if (IsSpawned) return;
        NetworkObject.Spawn();
    }

    private new void OnDestroy()
    {
        NetworkObject.Despawn();
    }
}