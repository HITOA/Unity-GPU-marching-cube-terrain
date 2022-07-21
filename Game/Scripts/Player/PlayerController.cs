using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [SerializeField]
    InputActionReference aMovement, aCameraMovement, aJump, aRun;
    [SerializeField]
    Camera pCamera;
    [SerializeField]
    CharacterController pController;
    [SerializeField]
    Vector3 gravity;
    [SerializeField]
    float maxFallingSpeed;
    [SerializeField]
    float maxDistanceGroundDetection;
    [SerializeField]
    float jumpForce;
    [SerializeField]
    float movementSpeed;
    [SerializeField]
    float cameraMovementSpeed;
    [SerializeField]
    float runningSpeedMultiplier;

    Vector3 velocity;
    Vector2 movementDir;
    Vector2 cameraMovementDir;

    bool jump;
    bool run;

    private void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Start()
    {
        BindInputAction(aMovement, OnMovement);
        BindInputAction(aCameraMovement, OnCameraMovement);
        BindInputAction(aJump, OnJump);
        BindInputAction(aRun, OnRun);
    }

    private void FixedUpdate()
    {
        if (Physics.Raycast(transform.position, Vector3.down, maxDistanceGroundDetection, Physics.DefaultRaycastLayers))
        {
            if (jump)
                velocity = new Vector3(0, jumpForce * Time.fixedDeltaTime, 0);
            else
                velocity = Vector3.zero;
        }else
        {
            if (velocity.y > -maxFallingSpeed)
                velocity += gravity * Time.fixedDeltaTime;
        }

        pController.Move(velocity * Time.fixedDeltaTime);
        pController.Move(transform.rotation * new Vector3(movementDir.x, 0, movementDir.y) * movementSpeed * Time.fixedDeltaTime * (run ? runningSpeedMultiplier : 1));

        jump = false;
    }

    private void Update()
    {
        transform.Rotate(new Vector3(0, cameraMovementDir.x, 0) * cameraMovementSpeed);
        pCamera.transform.Rotate(new Vector3(-cameraMovementDir.y, 0, 0) * cameraMovementSpeed);
    }

    private void BindInputAction(InputActionReference aRef, Action<InputAction.CallbackContext> callback)
    {
        aRef.action.started += callback;
        aRef.action.performed += callback;
        aRef.action.canceled += callback;

        aRef.action.Enable();
    }

    private void OnMovement(InputAction.CallbackContext ctx)
    {
        movementDir = ctx.ReadValue<Vector2>();
    }

    private void OnCameraMovement(InputAction.CallbackContext ctx)
    {
        cameraMovementDir = ctx.ReadValue<Vector2>();
    }

    private void OnJump(InputAction.CallbackContext ctx)
    {
        if (ctx.phase == InputActionPhase.Performed)
            jump = true;
    }

    private void OnRun(InputAction.CallbackContext ctx)
    {
        if (ctx.phase == InputActionPhase.Performed)
            run = true;
        else
            run = false;
    }
}
