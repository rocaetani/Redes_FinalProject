using System.Collections;

using System.Collections.Generic;
using UnityEngine;
using MLAPI;
using MLAPI.Messaging;

public class MatchManager : NetworkBehaviour
{
    // Events
    private delegate void OnGenerationConfigsAvailableDelegate();
    private event OnGenerationConfigsAvailableDelegate OnGenerationConfigsAvailable;

    private bool _configsAvailable = false;

    private const string _containerName = "MatchPrefabs";
    private Transform _prefabContainer;

    private int _mapSeed;

    [Header("Building Blocks")]
    public GameObject UnbreakableWallPrefab;
    public GameObject BreakableWallPrefab;
    public GameObject FloorPrefab;

    [Header("Map Generation Config")]
    public int mapWidth = 18;
    public int mapHeight = 12;
    public int fillPercentage = 65;

    private void Awake()
    {
        _prefabContainer = gameObject.GetComponent<Transform>();

        if(IsServer || !(IsServer || IsClient))
        {
            // Validate if width and height are even then transform then in odd
            if (isEven(mapWidth))
            {
                mapWidth++;
            }
            if (isEven(mapHeight))
            {
                mapHeight++;
            }

            // Validate fill percentage
            fillPercentage = Mathf.Clamp(fillPercentage, 0, 100);

            // Initialize a random seed from Unity's RNG
            _mapSeed = Random.Range(int.MinValue, int.MaxValue);

            // Signal that all configs are available for client requests
            _configsAvailable = true;
        }
    }

    private void Start()
    {
        // Start map generation
        if(IsServer || !(IsServer || IsClient)) // Simply generate the map if you're either the Server, or an Offline instance
        {
            generateMap();
        }
        else
        {
            requestGenerationConfigs_ServerRpc();
        }
    }

    private void generateMap()
    {
        var openSpaces = generateEmptyMap();

        // We use System.Random here instead of Unity's Random, because we must
        // seed it with a fixed number to ensure all Clients generate a map that is
        // equal to the Server's. And because Unity's Random is a static class, we
        // cannot reseed it without poisoning the generator's future, unrelated, operations.
        // We instantiate a new Random() that is only used here.
        var mapGenRNG = new System.Random(_mapSeed);

        var breakableWallsToPlace = openSpaces.Count * fillPercentage / 100;

        #if UNITY_EDITOR
            Debug.Log($"Available Empty Squares: {openSpaces.Count}");
            Debug.Log($"Squares to Fill: {breakableWallsToPlace}");
        #endif

        while(breakableWallsToPlace > 0)
        {
            // Pick a new random index between 0 (inclusive) and openSpaces.Count (exclusive)
            int randomSpaceIndex = mapGenRNG.Next(openSpaces.Count);

            // Place new breakable wall
            Instantiate(BreakableWallPrefab, openSpaces[randomSpaceIndex], Quaternion.identity, _prefabContainer);
            openSpaces.RemoveAt(randomSpaceIndex);

            breakableWallsToPlace--;
        }


        // Vector3 playerPosition1 = new Vector3(1*10, 6, 1*10);
        // Vector3 playerPosition2 = new Vector3(1*10, 6,  (height - 2)*10);
        // Vector3 playerPosition3 = new Vector3((width - 2)*10, 6, 1*10);
        // Vector3 playerPosition4 = new Vector3((width - 2)*10, 6, (height - 2)*10);
        // MatchManager.Instance.SpawnPlayers(playerPosition1, playerPosition2, playerPosition3, playerPosition4);
    }

    private List<Vector3> generateEmptyMap()
    {
        List<Vector3> openSpaces = new List<Vector3>();
        for (int x = 0; x < mapWidth; x++)
        {
            for (int z = 0; z < mapHeight; z++)
            {
                // Instantiate the floor
                Vector3 position = new Vector3(x*10+5, 0, z*10+5);
                Instantiate(FloorPrefab, position, Quaternion.identity, _prefabContainer);

                position.y = 5.5f;

                // Instantiate unbreakable walls in a grid pattern
                if(isEven(x) && isEven(z))
                {
                    Instantiate(UnbreakableWallPrefab, position, Quaternion.identity, _prefabContainer);
                    continue;
                }

                // Instantiate the edges of the map
                if(x == 0 || x == mapWidth - 1 || z == 0 || z == mapHeight - 1)
                {
                    Instantiate(UnbreakableWallPrefab, position, Quaternion.identity, _prefabContainer);
                    continue;
                }

                // An 'L' shape must be left empty on each of
                // the four corners for players to spawn,
                // so we check and skip adding them to the
                // list of open blocks that gets returned
                if(x == 1)
                {
                    if (z == 1 || z == 2 || z == mapHeight - 2 || z == mapHeight - 3)
                    {
                        continue;
                    }
                }

                if(x == 2)
                {
                    if (z == 1 || z == mapHeight - 2 )
                    {
                        continue;
                    }
                }

                if(x == mapWidth - 2)
                {
                    if (z == 1 || z == 2 || z == mapHeight - 2 || z == mapHeight - 3)
                    {
                        continue;
                    }
                }

                if(x == mapWidth - 3)
                {
                    if (z == 1 || z == mapHeight - 2 )
                    {
                        continue;
                    }
                }

                // If we didn't fall into any of the previous conditiond,
                // mark this spot as empty
                openSpaces.Add(position);
            }
        }

        return openSpaces;

    }


    private bool isEven(int value)
    {
        return value % 2 == 0;
    }

    /*********** RPCs ***********/
    // When client is ready, request the map generation params from the server
    [ServerRpc(RequireOwnership = false)]
    private void requestGenerationConfigs_ServerRpc(ServerRpcParams rpcReceiveParams = default)
    {
        #if UNITY_EDITOR
            print($"Received Map Config request from #{rpcReceiveParams.Receive.SenderClientId}");
        #endif

        // Prepare ClientRPC's params, so that we only
        // reply with an RPC to the requester Client
        var clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[]{ rpcReceiveParams.Receive.SenderClientId }
            }
        };

        // If we can, simply send the params back to the client
        if(_configsAvailable)
        {
            #if UNITY_EDITOR
                print("Configs found, Sending back to client...");
            #endif
            sendGenerationConfigs_ClientRpc(mapWidth, mapHeight, fillPercentage, _mapSeed, clientRpcParams);
        }

        // Otherwise, wait for the event to notify us that the configs are available
        else
        {
            #if UNITY_EDITOR
                print("Configs NOT found!, deferring to Event...");
            #endif

            void waitForConfigs()
            {
                #if UNITY_EDITOR
                    print("Configs Updated, Sending back to client...");
                #endif

                OnGenerationConfigsAvailable -= waitForConfigs;
                sendGenerationConfigs_ClientRpc(mapWidth, mapHeight, fillPercentage, _mapSeed, clientRpcParams);
            }

            OnGenerationConfigsAvailable += waitForConfigs;
        }
    }

    // Server replies to client telling it to generate a random map based on a predefined set of parameters and a fixed seed
    [ClientRpc]
    private void sendGenerationConfigs_ClientRpc(int width, int height, int percentage, int seed, ClientRpcParams clientRpcParams = default)
    {
        #if UNITY_EDITOR
            print($"Reply from Server with Map Configs: Width: {width}, Height: {height}, Fill: {percentage}%, Seed: {seed}");
        #endif

        mapWidth = width;
        mapHeight = height;
        fillPercentage = percentage;
        _mapSeed = seed;
        generateMap();
    }

}
