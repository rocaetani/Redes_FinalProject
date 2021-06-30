using System.Collections;
using System.Collections.Generic;
using MLAPI;
using MLAPI.Messaging;
using UnityEngine;

public class Bomb : NetworkBehaviour
{
    

    private int _timeStart;


    private void Explode()
    {
        if (IsServer)
        {
            gameObject.SetActive(false);
            Explode_ClientRpc();
        }
    }



    public void OnStart()
    {
        if (IsServer)
        {
            _timeStart = (int) Time.time;
            
        }
    }

    private void Update()
    {
        if (IsServer)
        {
            int timePassed = (int) Time.time - _timeStart;
            if (timePassed > 5)
            {
                Explode();
            }
        }
    }
    

    [ClientRpc]
    private void Explode_ClientRpc()
    {
        if (IsHost)
        {
            return;
        }

        gameObject.SetActive(false);
    }
    
    
    
    [ClientRpc]
    public void ActivateBomb_ClientRpc()
    {
        if (IsHost)
        {
            return;
        }

        gameObject.SetActive(true);
    }
    
}