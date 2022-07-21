using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class PlayerTerraforming : MonoBehaviour
{
    [SerializeField]
    InputActionReference aDig;
    [SerializeField]
    Camera pCamera;
    [SerializeField]
    TestOnlyChunk testOnlyChunk;

    float dig;

    private void Start()
    {
        BindInputAction(aDig, OnDig);
    }

    private void Update()
    {
        if (dig != 0)
        {
            Ray ray = pCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 50.0f, Physics.DefaultRaycastLayers))
            {
                testOnlyChunk.chunkSystem.EditTerrainSpherical(hit.point, 3, dig * 2f * Time.deltaTime);
            }
        }
    }

    private void BindInputAction(InputActionReference aRef, Action<InputAction.CallbackContext> callback)
    {
        aRef.action.started += callback;
        aRef.action.performed += callback;
        aRef.action.canceled += callback;

        aRef.action.Enable();
    }

    private void OnDig(InputAction.CallbackContext ctx)
    {
        dig = ctx.ReadValue<float>();
    }
}
