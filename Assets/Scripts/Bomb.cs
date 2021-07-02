using System;
using System.Collections;
using System.Collections.Generic;
using MLAPI;
using MLAPI.Messaging;
using MLAPI.NetworkVariable;
using UnityEngine;

public class Bomb : NetworkBehaviour
{
    public int explosionTimer = 5;

    // Events
    public delegate void OnExplosionDelegate(Vector3 explosionPosition, int explosionPower);
    public static event OnExplosionDelegate OnExplosion;

    public GameObject explosionFX;
    private GameObject _explosionFXInstance = null;

    [SerializeField]
    private GameObject _childComponent = null;

    public NetworkVariable<int> explosionPower = new NetworkVariable<int>(new NetworkVariableSettings {
        WritePermission = NetworkVariablePermission.ServerOnly,
        ReadPermission = NetworkVariablePermission.Everyone
    });

    public void OnEnable()
    {
        if(IsServer)
        {
            IEnumerator scheduleExplosion()
            {
                yield return new WaitForSeconds(explosionTimer);
                Explode();
            }

            StartCoroutine(scheduleExplosion());
        }
    }

    private void Explode()
    {
        Explode_ClientRpc();

        const float waitTime = 0.5f;

        IEnumerator destroyAfterExplosion()
        {
            yield return new WaitForSeconds(waitTime);
            destroyMapBlocks();
            gameObject.SetActive(false);
        }

        StartCoroutine(destroyAfterExplosion());
    }

    private void destroyMapBlocks()
    {
        if(MatchManager.matchMap == null)
        {
            return;
        }


        var power = explosionPower.Value;

        var coords = MatchManager.converToMapCoords(transform.position.x, transform.position.z);

        var x = coords.x;
        var z = coords.y;

        if(outOfBounds(x, transform.position.y, z))
        {
            return;
        }

        // The bonb landed on top of a block
        if(transform.position.y > MatchManager.blockSize)
        {
            if(MatchManager.matchMap[x, z] == TileType.BreakableWall)
            {
                MatchManager.matchMap[x, z] = TileType.Empty;
                print($"Below at [{x}, {z}]");
                return;
            }
        }

        int min, max;

        /** Horizontal (Variable X, Fixed Z) **/
        // Forward
        min = Mathf.Min(x + 1, MatchManager.width - 1);
        max = Mathf.Min(x + power + 1, MatchManager.width);
        for(int i = min; i < max; i++)
        {
            if(MatchManager.matchMap[i, z] == TileType.UnbreakableWall)
            {
                break;
            }

            if(MatchManager.matchMap[i, z] == TileType.BreakableWall)
            {
                MatchManager.matchMap[i, z] = TileType.Empty;
                print($"Front X at [{i}, {z}]");
                break;
            }
        }

        // Back
        min = Mathf.Max(x - power - 1, 0);
        max = Mathf.Max(x - 1, 0);
        for(int i = max; i > min; i--)
        {
            if(MatchManager.matchMap[i, z] == TileType.UnbreakableWall)
            {
                break;
            }

            if(MatchManager.matchMap[i, z] == TileType.BreakableWall)
            {
                MatchManager.matchMap[i, z] = TileType.Empty;
                print($"Back X at [{i}, {z}]");
                break;
            }
        }

        /** Vertical (Fixed X, Variable Z) **/
        // Forward
        min = Mathf.Min(z + 1, MatchManager.height - 1);
        max = Mathf.Min(z + power + 1, MatchManager.height);
        for(int i = min; i < max; i++)
        {
            if(MatchManager.matchMap[x, i] == TileType.UnbreakableWall)
            {
                break;
            }

            if(MatchManager.matchMap[x, i] == TileType.BreakableWall)
            {
                MatchManager.matchMap[x, i] = TileType.Empty;
                print($"Front Z at [{x}, {i}]");
                break;
            }
        }

        // Back
        min = Mathf.Max(z - power - 1, 0);
        max = Mathf.Max(z - 1, 0);
        for(int i = max; i > min; i--)
        {
            if(MatchManager.matchMap[x, i] == TileType.UnbreakableWall)
            {
                break;
            }

            if(MatchManager.matchMap[x, i] == TileType.BreakableWall)
            {
                MatchManager.matchMap[x, i] = TileType.Empty;
                print($"Back Z at [{x}, {i}]");
                break;
            }
        }
    }

    private bool outOfBounds(float x, float y, float z)
    {
        if(x < 0 || x >= MatchManager.width)
        {
            return true;
        }

        if(z < 0 || z >= MatchManager.height)
        {
            return true;
        }

        if(y < 0)
        {
            return true;
        }

        return false;
    }

    [ClientRpc]
    private void Explode_ClientRpc()
    {

        if(_explosionFXInstance == null)
        {
            _explosionFXInstance = Instantiate(explosionFX, Vector3.zero, Quaternion.identity);
        }

        // Get grid coords where the explosion happened
        var coords = MatchManager.converToMapCoords(transform.position.x, transform.position.z);

        // Convert from grid back to World coords of the center of the block
        var x = coords.x * MatchManager.blockSize + MatchManager.blockSize / 2;
        var z = coords.y * MatchManager.blockSize + MatchManager.blockSize / 2;
        var explosionCenter = new Vector3(x, transform.position.y, z);

        _explosionFXInstance.transform.position = explosionCenter;

        var fx = _explosionFXInstance.GetComponent<ExplosionEffect>();
        fx.SetPower(explosionPower.Value);
        fx.ActivateEffect();

        if(!IsHost)
        {
            gameObject.SetActive(false);
        }

        OnExplosion?.Invoke(explosionCenter, explosionPower.Value);
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