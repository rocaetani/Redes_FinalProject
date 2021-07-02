using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MLAPI;
using MLAPI.Transports;
using MLAPI.Transports.UNET;
using MLAPI.Transports.PhotonRealtime;
using MLAPI.SceneManagement;

public enum NetworkTransportTypes {
  Direct,
  Relayed,
  None
}

public class NetworkController : MonoBehaviour
{
    // Events
    public delegate void OnConnectedDelegate(bool isHost);
    public static event OnConnectedDelegate OnConnected;

    public delegate void OnDisconnectedDelegate(bool wasHost, bool connectionWasLost);
    public static event OnDisconnectedDelegate OnDisconnected;

    public delegate void OnOtherClientConnectedDelegate(ulong otherClientID);
    public static event OnOtherClientConnectedDelegate OnOtherClientConnected;

    public delegate void OnOtherClientDisconnectedDelegate(ulong otherClientID);
    public static event OnOtherClientDisconnectedDelegate OnOtherClientDisconnected;

    // Static
    private const ushort _port = 53658;

    private static NetworkController _instance = null;

    public static bool IsServer
    {
        get => _instance != null? _instance._netManager.IsServer : false;
    }

    public static bool IsHost
    {
        get => _instance != null? _instance._netManager.IsHost : false;
    }

    public static bool IsClient
    {
        get => _instance != null? _instance._netManager.IsClient : false;
    }

    public static bool IsThinClient
    {
        get => _instance != null? !_instance._netManager.IsHost && _instance._netManager.IsClient : false;
    }

    public static bool IsOffline
    {
        get => _instance != null? !(_instance._netManager.IsClient || _instance._netManager.IsServer) : true;
    }


    private NetworkManager _netManager;
    private UNetTransport _ipTransport;
    private PhotonRealtimeTransport _relayedTransport;

    private NetworkTransport _transport
    {
        get
        {
            return _netManager?.NetworkConfig.NetworkTransport;
        }

        set
        {
            if(_netManager != null)
            {
                _netManager.NetworkConfig.NetworkTransport = value;
            }
        }
    }

    private NetworkTransportTypes _transportType
    {
        get
        {
            var curr = _transport;
            if(curr == null)
            {
                return NetworkTransportTypes.None;
            }

            return curr == _ipTransport? NetworkTransportTypes.Direct : NetworkTransportTypes.Relayed;
        }

        set
        {
            switch(value)
            {
                case NetworkTransportTypes.Direct:
                    _transport = _ipTransport;
                    break;

                case NetworkTransportTypes.Relayed:
                    _transport = _relayedTransport;
                    break;

                case NetworkTransportTypes.None:
                    _transport = null;
                    break;
            }
        }
    }

    private void Awake()
    {
        _instance = _instance ?? this;

        _netManager = GetComponent<NetworkManager>();
        _ipTransport = GetComponent<UNetTransport>();
        _relayedTransport = GetComponent<PhotonRealtimeTransport>();

        // Listen on NetworkManager Events
        _netManager.OnClientConnectedCallback += clientConnectEvent;
        _netManager.OnClientDisconnectCallback += clientDisconnectEvent;

        // Event Subscribings
        ConnectionMenu.OnGoToLobby += startLobbyConnection;

        // Disconnect Events
        LoadingMenu.OnCancel += disconnect;
        LobbyMenu.OnCancelMatch += disconnect;
        ExitMenu.OnLeaveMatch += disconnect;
        GameOverMenu.OnLeaveMatch += disconnect;

        if(_instance != this)
        {
            DestroyImmediate(gameObject);
        }
    }

    private void OnDestroy()
    {
        // Disconnect from Events
        _netManager.OnClientConnectedCallback -= clientConnectEvent;
        _netManager.OnClientDisconnectCallback -= clientDisconnectEvent;

        ConnectionMenu.OnGoToLobby -= startLobbyConnection;

        LoadingMenu.OnCancel -= disconnect;
        LobbyMenu.OnCancelMatch -= disconnect;
        ExitMenu.OnLeaveMatch -= disconnect;
        GameOverMenu.OnLeaveMatch -= disconnect;
    }

    private void startLobbyConnection(bool isHost, NetworkTransportTypes transportType, string address)
    {
        print("Starting Connection");

        /* Setup Transport */
        _transportType = transportType;
        if(_transport is UNetTransport unet)
        {
            unet.ConnectAddress = address;
            unet.ConnectPort = _port;
            unet.ServerListenPort = _port;

            if(isHost) // Make sure IP address to Host the server at is the localhost
            {
                unet.ConnectAddress = "127.0.0.1";
            }

        }
        else if(_transport is PhotonRealtimeTransport photon)
        {
            photon.RoomName = address;
        }

        /* Setup Connect Events */
        // Defer connection event to trigger together with the _netManager events
        if(isHost)
        {
            // Run self unsubscribing Local Function on Host Started
            void hostIsConnected()
            {
                _netManager.OnServerStarted -= hostIsConnected;

                #if UNITY_EDITOR
                    Debug.Log("Host Connected.");
                #endif

                OnConnected?.Invoke(true);
            }

            _netManager.OnServerStarted += hostIsConnected;
        }
        else
        {
            // Run self unsubscribing Local Function on this Client connected
            void clientIsConnected(ulong clientID)
            {
                if(clientID == _netManager.LocalClientId)
                {
                    _netManager.OnClientConnectedCallback -= clientIsConnected;

                    #if UNITY_EDITOR
                        Debug.Log($"Client Connected. ID: {clientID}");
                    #endif

                    OnConnected?.Invoke(false);
                }
            }

            _netManager.OnClientConnectedCallback += clientIsConnected;
        }

        /* Start Connection */
        if(isHost)
        {
            _netManager.StartHost();
        }
        else
        {
            _netManager.StartClient();
        }
    }

    private void clientConnectEvent(ulong clientID) => handleClientEvent(clientID, false);
    private void clientDisconnectEvent(ulong clientID) => handleClientEvent(clientID, true);

    private void handleClientEvent(ulong clientID, bool isDisconnect)
    {
        #if UNITY_EDITOR
            Debug.Log($"{(clientID == _netManager.LocalClientId? "Local" : "Other")} Client {(isDisconnect? "disconnected" : "connected")}. ID: {clientID}, Local: {_netManager.LocalClientId}");
        #endif
        if(clientID != _netManager.LocalClientId) // Other Client Event
        {
            if(isDisconnect)
            {
                OnOtherClientDisconnected?.Invoke(clientID);
            }
            else
            {
                OnOtherClientConnected?.Invoke(clientID);
            }
        }
        else // Local Client has done something
        {
            // We don't need to consider Local Client -> Remote Host connection here,
            // that's handled on the self unsubscribing event created during startLobbyConnection()

            if(isDisconnect)
            {
                // Local Client has lost connection to the remote Host
                // Intended disconnection is handled on the disconnect() method
                OnDisconnected?.Invoke(false, true);
            }
        }
    }

    public static void switchNetworkScene(string sceneName) => _instance.switchManagerNetworkScene(sceneName);
    private void switchManagerNetworkScene(string sceneName)
    {
        if(!_netManager.IsServer)
        {
            return;
        }

        if(!_netManager.NetworkConfig.EnableSceneManagement || !_netManager.NetworkConfig.RegisteredScenes.Contains(sceneName))
        {
            return;
        }

        NetworkSceneManager.SwitchScene(sceneName);
    }

    private IEnumerator disconnectAfterDelay(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        disconnect();
    }

    public static List<ulong> getConnectedPlayers() => _instance.managerGetConnectedPlayers();
    private List<ulong> managerGetConnectedPlayers()
    {
        return _netManager.ConnectedClientsList.Select(client => client.ClientId).ToList();
    }

    public static ulong getOwnID() => _instance.managerGetOwnID();
    private ulong managerGetOwnID()
    {
        return _netManager.LocalClientId;
    }

    public static void disconnect() => _instance.managerDisconnect();
    private void managerDisconnect()
    {
        // Can't disconnect if you're neither a Server nor Client (Host is both)
        if(!(_netManager.IsServer || _netManager.IsClient))
        {
            return;
        }

        if(_netManager.IsHost)
        {
            _netManager.StopHost();
            OnDisconnected?.Invoke(true, false);
        }
        /* Not valid for this Game, as all Servers are also Hosts */
        // else if(_netManager.IsServer)
        // {
        //     _netManager.StopServer();
        //     OnDisconnected?.Invoke(true);
        // }
        else if(_netManager.IsClient)
        {
            _netManager.StopClient();
            OnDisconnected?.Invoke(false, false);
        }
    }
}