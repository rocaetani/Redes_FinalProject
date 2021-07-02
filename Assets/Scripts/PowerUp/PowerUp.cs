using System.Collections;
using System.Collections.Generic;
using MLAPI;
using MLAPI.Messaging;
using UnityEngine;

public class PowerUp : NetworkBehaviour
{
    public PowerUpType Type;

    public MeshRenderer MiddleMesh1;
    public MeshRenderer MiddleMesh2;

    public void ApplyEffect(GameObject objectPowered)
    {
        switch (Type)
        {
            case PowerUpType.Bombs:
                ApplyBomb(objectPowered.GetComponent<Throw>());
                break;
            case PowerUpType.Power:
                ApplyPower(objectPowered.GetComponent<Throw>());
                break;
            case PowerUpType.Speed:
                ApplySpeed(objectPowered.GetComponent<PlayerMovement>());
                break;
        }
    }

    public void SetMiddleMesh(int materialIndex)
    {
        SetMiddleMesh_ServerRpc(materialIndex);
    }

    [ServerRpc]
    public void SetMiddleMesh_ServerRpc(int materialIndex)
    {
        SetMiddleMesh_ClientRpc(materialIndex);
    }

    [ClientRpc]
    public void SetMiddleMesh_ClientRpc(int materialIndex)
    {
        switch (materialIndex)
        {
            case 1:
                MiddleMesh1.material = PowerUpSpawner.Instance.PowerMaterial;
                MiddleMesh2.material = PowerUpSpawner.Instance.PowerMaterial;
                break;
            case 2:
                MiddleMesh1.material = PowerUpSpawner.Instance.SpeedMaterial;
                MiddleMesh2.material = PowerUpSpawner.Instance.SpeedMaterial;
                break;
            case 3:
                MiddleMesh1.material = PowerUpSpawner.Instance.BombMaterial;
                MiddleMesh2.material = PowerUpSpawner.Instance.BombMaterial;
                break;
        }
    }

    public void ApplyBomb(Throw applyToThrow)
    {
        applyToThrow.MaxBombs++;
    }

    public void ApplyPower(Throw applyToThrow)
    {
        applyToThrow.ExplosionPower++;
    }
    
    public void ApplySpeed(PlayerMovement playerMovement)
    {
        playerMovement.MoveSpeed += 5;
    }
    
    
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            ApplyEffect(other.gameObject);
            DestroyPowerUp_ServerRpc();

        }

    }
    
    [ServerRpc]
    public void DestroyPowerUp_ServerRpc()
    {
        print("Chamou Power Up");
        DestroyPowerUp_ClientRpc();
    }

    
    [ClientRpc]
    public void DestroyPowerUp_ClientRpc()
    {
        
        Destroy(gameObject);
    }


}
