using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityScene = UnityEngine.SceneManagement;

public class SceneManager : MonoBehaviour
{
    // Events
    public delegate void OnMainMenuLostConnectionDelegate();
    public static event OnMainMenuLostConnectionDelegate OnMainMenuLostConnection;
    
    public delegate void OnMenuLoadedDelegate(string sceneName);
    public static event OnMenuLoadedDelegate OnMenuLoaded;
    
    public delegate void OnMatchLoadedDelegate(string sceneName);
    public static event OnMatchLoadedDelegate OnMatchLoaded;

    private const string _selfTag = "SceneManager";

    private const string _mainMenu = "MainMenu";
    private bool _onMainMenu
    {
        get => UnityScene.SceneManager.GetActiveScene().name == _mainMenu;
    }

    private void Awake()
    {
        Object.DontDestroyOnLoad(gameObject);

        LobbyMenu.OnStartMatch += loadMatch;
        NetworkController.OnDisconnected += returnToMainMenu;

        UnityScene.SceneManager.sceneLoaded += TriggerSceneLoadEvent;

        // If, after moving to DontDestroyOnLoad, we detect more than one
        // SceneManager object, that means we are the duplicate one that came after
        // And so should delete ourselves
        if(GameObject.FindGameObjectsWithTag(_selfTag).Length > 1)
        {
            DestroyImmediate(gameObject);
        }
    }

    private void OnDestroy()
    {
        LobbyMenu.OnStartMatch -= loadMatch;
        NetworkController.OnDisconnected -= returnToMainMenu;
        UnityScene.SceneManager.sceneLoaded -= TriggerSceneLoadEvent;
    }

    private void returnToMainMenu(bool wasHost, bool connectionWasLost)
    {
        if(_onMainMenu) // No need to go back to main menu if we're already there
        {
            return;
        }

        // Set a self unsubscribing local function to trigger when the mainMenu scene loads
        void deferredMainMenuAction(UnityScene.Scene scene, UnityScene.LoadSceneMode mode)
        {
            UnityScene.SceneManager.sceneLoaded -= deferredMainMenuAction;
            if(!_onMainMenu)
            {
                return;
            }

            // If we disconnected on purpose, no action is required
            // however, if we lost connection, defer event triger until after scene components
            //have been started
            if(connectionWasLost)
            {
                IEnumerator triggerConnectionLost()
                {
                    // Defer event trigger until after Awakes and Starts
                    yield return new WaitForEndOfFrame();
                    OnMainMenuLostConnection?.Invoke();
                }

                StartCoroutine(triggerConnectionLost());
            }
        }

        // Subscribe the above funtion
        UnityScene.SceneManager.sceneLoaded += deferredMainMenuAction;

        // Finally, load MainMenu
        UnityScene.SceneManager.LoadScene(_mainMenu);
    }

    private void loadMatch()
    {
        // TODO get scene name from lobby
        NetworkController.switchNetworkScene("SampleScene");
    }

    private void TriggerSceneLoadEvent(UnityScene.Scene scene, UnityScene.LoadSceneMode mode)
    {

        if (scene.name == _mainMenu)
        {
            OnMenuLoaded?.Invoke(scene.name);
        }
        else
        {
            OnMatchLoaded?.Invoke(scene.name); 
        }



    }
}
