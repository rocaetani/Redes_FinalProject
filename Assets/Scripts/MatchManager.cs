using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;
using MLAPI.Messaging;

public enum TileType
{
    Empty = 0,
    BreakableWall = 1,
    UnbreakableWall = 2
}
public class MatchManager : NetworkBehaviour
{
    private static MatchManager _instance = null;

    private const int _playersPerMatch = 4;

    public struct Coords
    {
        public int x, y;
    }

    public class MatchLayout
    {
        Action<int, int, TileType> _updateClientMatch;

        private int _rowCount;
        private int _colCount;

        private List<GameObject> _tileObjects;
        private List<TileType> _tileTypes;

        private GameObject _unbreakableWallPrefab;
        private GameObject _breakableWallPrefab;

        private Transform _prefabContainer;

        public MatchLayout(int x, int y, GameObject unbreakablePrefab, GameObject breakablePrefab, Transform prefabContainer, Action<int, int, TileType> updateClientMatch)
        {
            _updateClientMatch = updateClientMatch;

            _rowCount = x;
            _colCount = y;

            var count = x * y;
            _tileObjects = new List<GameObject>(count);
            _tileTypes = new List<TileType>(count);

            for(int i = 0; i < count; i++)
            {
                _tileObjects.Add(null);
                _tileTypes.Add(TileType.Empty);
            }

            _unbreakableWallPrefab = unbreakablePrefab;
            _breakableWallPrefab = breakablePrefab;

            _prefabContainer = prefabContainer;
        }

        public GameObject getGridTileObject(int x, int y) {
            return _tileObjects[x * _colCount + y];
        }

        public void initializeTile(int x, int y, TileType tile) => updateTile(x, y, tile);

        private void updateTile(int x, int y, TileType type)
        {
            var i = x * _colCount + y;

            if(_tileTypes[i] == type)
            {
                return;
            }

            _tileTypes[i] = type;

            Destroy(_tileObjects[i]);

            const int prefabSize = 10;
            const int prefabTranslation = 5;
            const float prefabHeight = 5.5f;

            var position = new Vector3(x * prefabSize + prefabTranslation, prefabHeight, y * prefabSize + prefabTranslation);
            switch(type)
            {
                case TileType.Empty:
                    _tileObjects[i] = null;
                    break;

                case TileType.BreakableWall:
                    _tileObjects[i] = Instantiate(_breakableWallPrefab, position, Quaternion.identity, _prefabContainer);
                    #if UNITY_EDITOR
                        _tileObjects[i].name = $"[{x}, {y}]: Breakable";
                    #endif
                    break;

                case TileType.UnbreakableWall:
                    _tileObjects[i] = Instantiate(_unbreakableWallPrefab, position, Quaternion.identity, _prefabContainer);
                    #if UNITY_EDITOR
                        _tileObjects[i].name = $"[{x}, {y}]: Unbreakable";
                    #endif
                    break;
            }
        }

        public TileType this[int x, int y]
        {
            get => _tileTypes[x * _colCount + y];

            set
            {
                if(NetworkController.IsServer)
                {
                    if(!NetworkController.IsHost)
                    {
                        updateTile(x, y, value);
                    }
                    _updateClientMatch(x, y, value);
                }
            }
        }

        public void updateClientTile(int x, int y, TileType tile)
        {
            print("Received Server Tile Update");
            updateTile(x, y, tile);
        }

    }

    // Events
    private delegate void OnSpawnLocationDelegate(Vector3 spawnLocation);
    private static event OnSpawnLocationDelegate OnSpawnLocation;

    private delegate void OnGenerationConfigsAvailableDelegate();
    private event OnGenerationConfigsAvailableDelegate OnGenerationConfigsAvailable;

    private delegate void OnSpawnsAvailableDelegate();
    private event OnSpawnsAvailableDelegate OnSpawnsAvailable;

    // Building Block Dimensions
    public const int blockSize = 10;
    public const int blockY = 6;
    public static int width
    {
        get => _instance != null? _instance.mapWidth : 0;
    }
    public static int height
    {
        get => _instance != null? _instance.mapHeight : 0;
    }

    private static Vector3 _clientSpawnLocation = Vector3.zero;

    private static bool _clientSpawnAvailable = false;
    private bool _configsAvailable = false;
    private bool _spawnsAvailable = false;

    private const string _containerName = "MatchPrefabs";
    private Transform _prefabContainer;

    private Stack<Vector3> _spawnLocations;

    private int _mapSeed;

    public MatchLayout _matchLayout;
    public static MatchLayout matchMap
    {
        get => _instance != null? _instance._matchLayout : null;
    }

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
        _instance = _instance ?? this;

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
            _mapSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);

            // Signal that all configs are available for client requests
            _configsAvailable = true;
            OnGenerationConfigsAvailable?.Invoke();
        }
    }

    private void OnDestroy()
    {
        if(_instance == this)
        {
            _instance = null;
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

        // Unlike generation configs, all instances, including host, must request a spawn locations
        requestSpawnLocation_ServerRpc();

    }

    private void generateMap()
    {
        var matchLayout = new MatchLayout(mapWidth, mapHeight, UnbreakableWallPrefab, BreakableWallPrefab, _prefabContainer, updateClientMatch_ClientRpc);

        var openSpaces = generateEmptyMap(ref matchLayout);

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
            matchLayout.initializeTile(openSpaces[randomSpaceIndex].x, openSpaces[randomSpaceIndex].y, TileType.BreakableWall);

            openSpaces.RemoveAt(randomSpaceIndex);
            breakableWallsToPlace--;
        }

        _matchLayout = matchLayout;

        if(IsServer)
        {
            createSpawns();
        }
    }

    private void createSpawns()
    {
        _spawnLocations = new Stack<Vector3>(_playersPerMatch);

        var initialX = blockSize;
        var finalX = (mapWidth - 2) * blockSize;

        var initialZ = blockSize;
        var finalZ = (mapHeight - 2) * blockSize;

        _spawnLocations.Push(new Vector3(initialX, blockY, initialZ));
        _spawnLocations.Push(new Vector3(initialX, blockY, finalZ));
        _spawnLocations.Push(new Vector3(finalX,   blockY, initialZ));
        _spawnLocations.Push(new Vector3(finalX,   blockY, finalZ));

        _spawnsAvailable = true;
        OnSpawnsAvailable?.Invoke();
    }

    private List<Coords> generateEmptyMap(ref MatchLayout matchLayout)
    {
        List<Coords> openSpaces = new List<Coords>();
        for(int x = 0; x < mapWidth; x++)
        {
            for(int z = 0; z < mapHeight; z++)
            {
                // Instantiate the floor
                Vector3 position = new Vector3(x*blockSize + blockSize/2, 0, z*blockSize + blockSize/2);
                var floor = Instantiate(FloorPrefab, position, Quaternion.identity, _prefabContainer);
                #if UNITY_EDITOR
                    floor.name = $"[{x},{z}] Floor";
                #endif

                // Instantiate unbreakable walls in a grid pattern
                if(isEven(x) && isEven(z))
                {
                    matchLayout.initializeTile(x, z, TileType.UnbreakableWall);
                    continue;
                }

                // Instantiate the edges of the map
                if(x == 0 || x == mapWidth - 1 || z == 0 || z == mapHeight - 1)
                {
                    matchLayout.initializeTile(x, z, TileType.UnbreakableWall);
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
                        matchLayout.initializeTile(x, z, TileType.Empty);
                        continue;
                    }
                }

                if(x == 2)
                {
                    if (z == 1 || z == mapHeight - 2 )
                    {
                        matchLayout.initializeTile(x, z, TileType.Empty);
                        continue;
                    }
                }

                if(x == mapWidth - 2)
                {
                    if (z == 1 || z == 2 || z == mapHeight - 2 || z == mapHeight - 3)
                    {
                        matchLayout.initializeTile(x, z, TileType.Empty);
                        continue;
                    }
                }

                if(x == mapWidth - 3)
                {
                    if (z == 1 || z == mapHeight - 2 )
                    {
                        matchLayout.initializeTile(x, z, TileType.Empty);
                        continue;
                    }
                }

                // If we didn't fall into any of the previous conditions,
                // mark this spot as empty
                Coords open;
                open.x = x;
                open.y = z;
                openSpaces.Add(open);
            }
        }

        return openSpaces;

    }

    public static void requestSpawnPoint(Action<Vector3> responseAction)
    {
        #if UNITY_EDITOR
            print("Spawn Point Requested");
        #endif

        if(_clientSpawnAvailable)
        {
            responseAction?.Invoke(_clientSpawnLocation);
            return;
        }

        void autoUnsubscribeEvent(Vector3 spawnLocation)
        {
            OnSpawnLocation -= autoUnsubscribeEvent;
            responseAction?.Invoke(spawnLocation);
        }

        OnSpawnLocation += autoUnsubscribeEvent;
    }

    private bool isEven(int value)
    {
        return value % 2 == 0;
    }

    public static Coords converToMapCoords(float x, float y)
    {
        Coords res;
        res.x = (int) x / MatchManager.blockSize;
        res.y = (int) y / MatchManager.blockSize;
        return res;
    }

    /*********** RPCs ***********/
    // When client is ready, request the map generation params from the server
    [ServerRpc(RequireOwnership = false)]
    private void requestGenerationConfigs_ServerRpc(ServerRpcParams rpcReceiveParams = default)
    {
        #if UNITY_EDITOR
            print($"Received Map Config request from #{rpcReceiveParams.Receive.SenderClientId}");
        #endif

        var clientRpcParams = rpcReceiveParams.returnToSender();

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


    // In order to update the Match variable, we must rely on the RPC call
    // of a network object, which is why we pass this method as a parameter for
    // MatchLayout, and call a match layout method here.
    [ClientRpc]
    private void updateClientMatch_ClientRpc(int x, int y, TileType type)
    {
        _matchLayout?.updateClientTile(x, y, type);
    }

    // Same as above, when a client requests it's spawn location
    // the Server sends it back
    [ServerRpc(RequireOwnership = false)]
    private void requestSpawnLocation_ServerRpc(ServerRpcParams rpcReceiveParams = default)
    {
        #if UNITY_EDITOR
            print($"Received Spawn Location request from #{rpcReceiveParams.Receive.SenderClientId}");
        #endif

        void sendSpawn()
        {
            var spawnPosition = _spawnLocations.Count > 0? _spawnLocations.Pop() : Vector3.zero;
            sendSpawnLocation_ClientRpc(spawnPosition, rpcReceiveParams.returnToSender());
        }

        // If we can, simply send the params back to the client
        if(_spawnsAvailable)
        {
            #if UNITY_EDITOR
                print("Spawn locations found, Sending back to client...");
            #endif

            sendSpawn();
        }

        // Otherwise, wait for the event to notify us that the configs are available
        else
        {
            #if UNITY_EDITOR
                print("Spawn locations NOT found!, deferring to Event...");
            #endif

            void waitForSpawn()
            {
                #if UNITY_EDITOR
                    print("Spawn locations Updated, Sending back to client...");
                #endif

                OnSpawnsAvailable -= waitForSpawn;
                sendSpawn();
            }

            OnGenerationConfigsAvailable += waitForSpawn;
        }
    }

    [ClientRpc]
    private void sendSpawnLocation_ClientRpc(Vector3 spawnLocation, ClientRpcParams clientRpcParams = default)
    {
        _clientSpawnLocation = spawnLocation;
        _clientSpawnAvailable = true;
        OnSpawnLocation?.Invoke(spawnLocation);
    }
}
