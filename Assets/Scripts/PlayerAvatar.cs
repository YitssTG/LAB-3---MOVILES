using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Locally reads this client's controls and sends movement to the server.</summary>
[RequireComponent(typeof(NetworkTransform))]
public sealed class PlayerAvatar : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float lookSensitivity = 0.12f;
    [SerializeField] private float cameraHeight = 1.5f;
    [SerializeField] private float cameraDistance = 4f;
    [SerializeField] private float reach = 100f;

    private Camera localCamera;
    private float yaw;
    private float pitch = 20f;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            GetComponent<Renderer>().material.color = new Color(0.25f, 0.55f, 1f);
            return;
        }

        GetComponent<Renderer>().material.color = new Color(1f, 0.65f, 0.1f);
        localCamera = Camera.main;
        if (localCamera == null)
            Debug.LogError("PlayerAvatar necesita una cámara activa con el tag MainCamera en la escena.");
        yaw = transform.eulerAngles.y;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (!IsOwner || localCamera == null)
            return;

        Mouse mouse = Mouse.current;
        Keyboard keyboard = Keyboard.current;
        if (mouse == null || keyboard == null)
            return;

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else if (mouse.leftButton.wasPressedThisFrame && Cursor.lockState == CursorLockMode.Locked)
        {
            TryInteract();
        }

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Vector2 delta = mouse.delta.ReadValue();
            yaw += delta.x * lookSensitivity;
            pitch = Mathf.Clamp(pitch - delta.y * lookSensitivity, -20f, 65f);
        }

        Quaternion viewRotation = Quaternion.Euler(pitch, yaw, 0f);
        localCamera.transform.position = transform.position + Vector3.up * cameraHeight - viewRotation * Vector3.forward * cameraDistance;
        localCamera.transform.rotation = viewRotation;
    }

    private void FixedUpdate()
    {
        if (!IsOwner || Keyboard.current == null)
            return;

        Vector2 input = Vector2.zero;
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) input.y += 1f;
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) input.y -= 1f;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) input.x += 1f;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) input.x -= 1f;
        input = Vector2.ClampMagnitude(input, 1f);
        MoveServerRpc(input, yaw, Time.fixedDeltaTime);
    }

    [Rpc(SendTo.Server)]
    private void MoveServerRpc(Vector2 input, float requestedYaw, float deltaTime, RpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != OwnerClientId)
            return;

        yaw = requestedYaw;
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        Vector3 direction = transform.right * input.x + transform.forward * input.y;
        transform.position += direction * (moveSpeed * Mathf.Clamp(deltaTime, 0f, 0.05f));
    }

    private void TryInteract()
    {
        Ray ray = localCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        RaycastHit[] hits = Physics.RaycastAll(ray, reach);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform))
                continue;

            Cubito resource = hit.collider.GetComponentInParent<Cubito>();
            if (resource != null)
            {
                resource.RequestClaimOrChop();
               return;
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
