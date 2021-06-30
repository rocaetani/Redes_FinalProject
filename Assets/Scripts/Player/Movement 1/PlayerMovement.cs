using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;

public class PlayerMovement : NetworkBehaviour
{
    private CharacterController _controller;


    public float MoveSpeed;
    public float WalkSpeed;

    public bool FreezeMovement = false;

    private float _realSpeed;
    private float _verticalSpeed;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        if(!FreezeMovement && IsOwner)
        {
            UpdateSpeed();
            Move();
        }
    }

    private void UpdateSpeed()
    {
        _realSpeed = MoveSpeed;
        if (InputManager.Instance.WalkButton)
        {
            _realSpeed = WalkSpeed;
        }
    }

    private void Move()
    {
        float _forwardInput = InputManager.Instance.Frontal;
        float _lateralInput = InputManager.Instance.Lateral;
        Vector3 moveDirection = _forwardInput * transform.forward + _lateralInput * transform.right;

        if (moveDirection.sqrMagnitude > 1)
        {
            moveDirection.Normalize();
        }

        Vector3 frameMovement = _realSpeed * Time.deltaTime * moveDirection;

        if (_controller.isGrounded)
        {
            _verticalSpeed = 0;
        }
        else
        {
            float gravity = Physics.gravity.y;
            _verticalSpeed += gravity * Time.deltaTime;
            frameMovement += _verticalSpeed * Time.deltaTime * Vector3.up;
        }

        _controller.Move(frameMovement);
    }

    public void MoveToSpawn(Vector3 spawnPosition)
    {
        // Need to disable character controller to allow player teleport to Spawn
        _controller.enabled = false;
        transform.position = spawnPosition;
        _controller.enabled = true;

    }
}
