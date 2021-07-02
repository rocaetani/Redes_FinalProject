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
    private int _timeStart;

    public GameObject explosionFX;
    private GameObject _explosionFXInstance = null;

    [SerializeField]
    private GameObject _childComponent = null;

    public NetworkVariable<int> explosionPower = new NetworkVariable<int>(new NetworkVariableSettings {
        WritePermission = NetworkVariablePermission.ServerOnly,
        ReadPermission = NetworkVariablePermission.Everyone
    });

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
            if (timePassed > explosionTimer)
            {
                Explode();
            }
        }
    }

    private void Explode()
    {
        if (IsServer)
        {
            destroyMapBlocks();
            Explode_ClientRpc();
        }
    }

    private Tuple<int, int> getMapCoords()
    {
        var x = (int) transform.position.x / MatchManager.blockSize;
        var z = (int) transform.position.z / MatchManager.blockSize;

        return new Tuple<int, int>(x, z);
    }

    private void destroyMapBlocks()
    {
        if(MatchManager.matchMap == null)
        {
            return;
        }


        var power = explosionPower.Value;

        var coords = getMapCoords();

        var x = coords.Item1;
        if(x < 0 || x >= MatchManager.width) // Out of Bounds
        {
            return;
        }

        var z = coords.Item2;
        if(z < 0 || z >= MatchManager.height) // Out of Bounds
        {
            return;
        }

        if(transform.position.y < 0) // Out of Bounds
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

    [ClientRpc]
    private void Explode_ClientRpc()
    {

        if(_explosionFXInstance == null)
        {
            _explosionFXInstance = Instantiate(explosionFX, Vector3.zero, Quaternion.identity);
        }

        var coords = getMapCoords();
        var x = coords.Item1 * MatchManager.blockSize + MatchManager.blockSize / 2;
        var z = coords.Item2 * MatchManager.blockSize + MatchManager.blockSize / 2;
        _explosionFXInstance.transform.position = new Vector3(x, transform.position.y, z);

        var fx = _explosionFXInstance.GetComponent<ExplosionEffect>();
        fx.SetPower(explosionPower.Value);
        fx.ActivateEffect();

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