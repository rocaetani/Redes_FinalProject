using System;
using System.Collections;
using System.Collections.Generic;
using MLAPI;
using MLAPI.Messaging;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

public class Throw : NetworkBehaviour
{
    public Transform BombPosition;
    public GameObject BombPrefab;

    public int ExplosionPower = 3;

    public float ThrowStrength;
    public float ArcThrow;

    private bool _isOnMatch;

    private PlayerController _playerController;

    public List<GameObject> InstantiatedServerBombs;

    public int MaxBombs;

    private void Awake()
    {
        if (IsServer)
        {
            InstantiatedServerBombs = new List<GameObject>();
        }

        _playerController = GetComponentInParent<PlayerController>();

        _isOnMatch = false;
        SceneManager.OnMatchLoaded += TurnOn;
        SceneManager.OnMenuLoaded += TurnOff;
    }

    private void OnDestroy()
    {
        SceneManager.OnMatchLoaded -= TurnOn;
        SceneManager.OnMenuLoaded -= TurnOff;
    }

    private void TurnOn(string sceneName)
    {
        _isOnMatch = true;
    }

    private void TurnOff(string sceneName)
    {
        _isOnMatch = false;
    }

    private void Update()
    {
        if (IsOwner && _isOnMatch && !_playerController.isFrozen)
        {
            Vector3 initialPosition = BombPosition.position;

            Vector3 target = CalculateTargetPosition();

            if (InputManager.PressFireButton())
            {
                Throw_ServerRpc(target, initialPosition);
            }
        }

    }

    private bool GetBomb(out GameObject bomb)
    {
        if (InstantiatedServerBombs.FindAll(bombInstance => bombInstance.activeSelf).Count < MaxBombs)
        {

            bomb = InstantiatedServerBombs.Find(bombInstance => !bombInstance.activeSelf);
            if (bomb == null)
            {
                bomb = InstantiateBomb();
                InstantiatedServerBombs.Add(bomb);
            }
            return true;

        }
        bomb = null;
        return false;
    }

    private GameObject InstantiateBomb()
    {
        GameObject instantiatedBomb = Instantiate(BombPrefab, Vector3.one, Quaternion.identity);
        instantiatedBomb.GetComponent<NetworkObject>().Spawn();
        return instantiatedBomb;
    }


    private Vector3 CalculateTargetPosition()
    {
        Vector3 screenMiddle = new Vector3();
        screenMiddle.x = Screen.width / 2;
        screenMiddle.y = Screen.height / 2;
        Ray ray = Camera.main.ScreenPointToRay(screenMiddle);

        Vector3 target = ray.direction * ThrowStrength;
        target += Vector3.up * ArcThrow;
        return target;
    }

    [ServerRpc]
    private void Throw_ServerRpc(Vector3 target, Vector3 initialPosition)
    {
        if (GetBomb(out var bomb))
        {
            var bombScript = bomb.GetComponent<Bomb>();

            bomb.SetActive(true);
            bombScript.ActivateBomb_ClientRpc();

            bombScript.explosionPower.Value = ExplosionPower;

            bomb.transform.position = initialPosition;

            Rigidbody bombRigidbody = bomb.GetComponent<Rigidbody>();
            bombRigidbody.velocity = Vector3.zero;
            bombRigidbody.AddForce(target, ForceMode.Impulse);
        }
        else
        {
            print("Numero de bombas exedido");
        }
    }

}