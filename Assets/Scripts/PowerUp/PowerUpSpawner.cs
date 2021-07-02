using System;
using System.Collections;
using System.Collections.Generic;
using MLAPI;
using MLAPI.Messaging;
using UnityEngine;
using Random = UnityEngine.Random;

public class PowerUpSpawner : NetworkBehaviour
{
    public static PowerUpSpawner Instance;

    public GameObject PowerUpPrefab;
    public Material PowerMaterial;
    public Material SpeedMaterial;
    public Material BombMaterial;


    private void Awake()
    {
        Instance = Instance ?? this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            GeneratePowerUp(new Vector3(0,2,0));
        }
    }


    public void GeneratePowerUp(Vector3 position)
    {
        if (IsServer)
        {
            GameObject powerUp = Instantiate(PowerUpPrefab, position, Quaternion.identity);
            powerUp.GetComponent<NetworkObject>().Spawn();
            GenerateRandomPowerUp(powerUp.GetComponent<PowerUp>());
        }
    }

    private void GenerateRandomPowerUp(PowerUp powerUp)
    {
        int rand = Random.Range(0, 3);
        switch (rand)
        {
            case 0:
                powerUp.Type = PowerUpType.Bombs;
                powerUp.SetMiddleMesh(3);
                break;
            case 1:
                powerUp.Type  = PowerUpType.Power;
                powerUp.SetMiddleMesh(1);
                break;
            case 2:
                powerUp.Type  = PowerUpType.Speed;
                powerUp.SetMiddleMesh(2);
                break;
            default:
                break;
        }
    }
}
