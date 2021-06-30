using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;

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
}
