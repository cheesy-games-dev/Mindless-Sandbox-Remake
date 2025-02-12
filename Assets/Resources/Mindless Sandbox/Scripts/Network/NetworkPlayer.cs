using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkPlayer : NetworkBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    IEnumerator Start()
    {
        yield return new WaitForSeconds(5);
        if (!IsSpawned) Destroy(this);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
