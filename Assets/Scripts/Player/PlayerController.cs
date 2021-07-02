using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;
using MLAPI.Messaging;

public class PlayerController : NetworkBehaviour
{
    // By default, players start with no movement or camera behaviour,
    // because they are first instantiated on the menu scene

    // This event allows a static class method call to inform all
    // Player instances that they should update their behaviour state
    private delegate void OnBehaviourChangeDelegate(bool behaviourEnabled);
    private static event OnBehaviourChangeDelegate OnPlayerBehaviourChanged;

    private static bool _playerBehaviourEnabled = false;
    public static bool playerBehaviourEnabled
    {
        get => _playerBehaviourEnabled;
        set
        {
            if(value != _playerBehaviourEnabled)
            {
                _playerBehaviourEnabled = value;
                OnPlayerBehaviourChanged?.Invoke(value);
            }
        }
    }

    public bool isFrozen = false;

    private Camera _playerCamera;
    private PlayerMovement _movementScript;
    private CameraMove _cameraScript;

    private Vector3 _spawnLocation;

    private void Awake()
    {
        _playerCamera = gameObject.GetComponentInChildren<Camera>();

        _movementScript = gameObject.GetComponent<PlayerMovement>();
        _cameraScript = gameObject.GetComponent<CameraMove>();

        // Listen on OnPlayerBehaviourChanged event
        OnPlayerBehaviourChanged += updateBehaviourState;

        // Button events
        InputManager.OnEscapeKeyPress += freezePlayer;
        ExitMenu.OnStayOnMatch += unfreezePlayer;

        // Scene Events
        SceneManager.OnMatchLoaded += moveToSceneSpawn;

        // Match Events
        Bomb.OnExplosion += detectExplosion;
        PlayerManager.OnEndMatch += endPlayerMovement;
    }

    private void Start()
    {
        // Set initial behaviour state for this player
        updateBehaviourState(IsOwner && playerBehaviourEnabled);
    }

    private void OnDestroy()
    {
        OnPlayerBehaviourChanged -= updateBehaviourState;

        InputManager.OnEscapeKeyPress -= freezePlayer;
        ExitMenu.OnStayOnMatch -= unfreezePlayer;

        SceneManager.OnMatchLoaded -= moveToSceneSpawn;

        Bomb.OnExplosion -= detectExplosion;
        PlayerManager.OnEndMatch -= endPlayerMovement;

        usePlayerCamera(false);
    }

    private void updateBehaviourState(bool behaviourEnabled)
    {
        _movementScript.enabled = behaviourEnabled;
        _cameraScript.enabled = behaviourEnabled;

        usePlayerCamera(behaviourEnabled);
    }

    private void usePlayerCamera(bool usePlayerCamera)
    {
        if(IsOwner)
        {
            _playerCamera.enabled = usePlayerCamera;
            ObjectsManager.OverviewCamera?.SetActive(!usePlayerCamera);
        }
    }

    private void endPlayerMovement(bool draw, ulong playerID) => freezePlayer();

    private void freezePlayer() => toggleFreeze(true);
    private void unfreezePlayer() => toggleFreeze(false);
    private void toggleFreeze(bool frozen)
    {
        if(IsOwner && isFrozen != frozen)
        {
            isFrozen = frozen;

            _cameraScript.MouseLocked = !frozen;
            _movementScript.FreezeMovement = frozen;
        }
    }

    private void moveToSceneSpawn(string sceneName)
    {
        if(IsOwner)
        {
            MatchManager.requestSpawnPoint(spawnLocation => _movementScript.MoveToSpawn(spawnLocation));
        }
    }

    private void detectExplosion(Vector3 explosionPosition, int explosionPower)
    {
        if(!IsOwner)
        {
            return;
        }

        if(outOfBounds(explosionPosition) || outOfBounds(transform.position))
        {
            return;
        }

        if(playerWasHit(transform.position, explosionPosition, explosionPower))
        {
            // kill player
            Die();
        }

    }

    public void Die()
    {
        _movementScript.FreezeMovement = true;

        IEnumerator waitBeforeDeath()
        {
            yield return new WaitForSeconds(ExplosionEffect.explosionDuration);
            playerBehaviourEnabled = false;
            requestPlayerDeath_ServerRpc();
        }

        StartCoroutine(waitBeforeDeath());
    }

    private static bool outOfBounds(Vector3 position)
    {
        if(position.x < 0 || position.x >= MatchManager.width * MatchManager.blockSize)
        {
            return true;
        }

        if(position.z < 0 || position.z >= MatchManager.height * MatchManager.blockSize)
        {
            return true;
        }

        if(position.y < 0 || position.y > MatchManager.blockSize)
        {
            return true;
        }

        return false;
    }

    private static bool playerWasHit(Vector3 playerPosition, Vector3 explosionPosition, int explosionPower)
    {
        int playerX, playerZ, explosionX, explosionZ;

        var playerPositionOnGrid = MatchManager.converToMapCoords(playerPosition.x, playerPosition.z);
        playerX = playerPositionOnGrid.x;
        playerZ = playerPositionOnGrid.y;

        var explosionPositionOnGrid = MatchManager.converToMapCoords(explosionPosition.x, explosionPosition.z);
        explosionX = explosionPositionOnGrid.x;
        explosionZ = explosionPositionOnGrid.y;

        // Check if the explosion happened where the player is separately
        if(playerX == explosionX && playerZ == explosionZ)
        {
            return true;
        }

        int min, max;

        /** Horizontal (Variable X, Fixed Z) **/
        // Forward
        if(playerZ == explosionZ)
        {
            min = Mathf.Min(explosionX + 1, MatchManager.width - 1);
            max = Mathf.Min(explosionX + explosionPower + 1, MatchManager.width);
            for(int i = min; i < max; i++)
            {
                if(MatchManager.matchMap[i, explosionZ] != TileType.Empty)
                {
                    break;
                }

                if(i == playerX)
                {
                    return true;
                }
            }

            // Back
            min = Mathf.Max(explosionX - explosionPower - 1, 0);
            max = Mathf.Max(explosionX - 1, 0);
            for(int i = max; i > min; i--)
            {
                if(MatchManager.matchMap[i, explosionZ] != TileType.Empty)
                {
                    break;
                }

                if(i == playerX)
                {
                    return true;
                }
            }
        }

        /** Vertical (Fixed X, Variable Z) **/
        // Forward
        if(playerX == explosionX)
        {
            min = Mathf.Min(explosionZ + 1, MatchManager.height - 1);
            max = Mathf.Min(explosionZ + explosionPower + 1, MatchManager.height);
            for(int i = min; i < max; i++)
            {
                if(MatchManager.matchMap[explosionX, i] != TileType.Empty)
                {
                    break;
                }

                if(i == playerZ)
                {
                    return true;
                }
            }

            // Back
            min = Mathf.Max(explosionZ - explosionPower - 1, 0);
            max = Mathf.Max(explosionZ - 1, 0);
            for(int i = max; i > min; i--)
            {
                if(MatchManager.matchMap[explosionX, i] != TileType.Empty)
                {
                    break;
                }

                if(i == playerZ)
                {
                    return true;
                }
            }
        }

        return false;
    }

    // RPCs
    [ServerRpc(RequireOwnership = false)]
    private void requestPlayerDeath_ServerRpc(ServerRpcParams rpcReceiveParams = default)
    {
        // Check winner
        PlayerManager.RegisterDeadPlayer(rpcReceiveParams.Receive.SenderClientId);

        broadcastPlayerDeath_ClientRpc();
    }

    [ClientRpc]
    private void broadcastPlayerDeath_ClientRpc()
    {
        Destroy(gameObject);
    }
}
