using Unity.Netcode;
using UnityEngine;

/// <summary>A server-authoritative resource that players can claim and chop.</summary>
public sealed class Cubito : NetworkBehaviour
{
    private const ulong NoOwner = ulong.MaxValue;
    private const int ChopsToFree = 3;

    public readonly NetworkVariable<ulong> OwnerClientId = new(
        NoOwner, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<int> ChopsRemaining = new(
        ChopsToFree, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [SerializeField] private Renderer resourceRenderer;
    [SerializeField] private float interactionDistance = 6f;

    private static readonly Color FreeColor = new(0.55f, 0.55f, 0.55f, 1f);
    private static readonly Color ClaimedColor = new(0.1f, 0.9f, 0.15f, 1f);

    public override void OnNetworkSpawn()
    {
        if (resourceRenderer == null)
            resourceRenderer = GetComponent<Renderer>();

        OwnerClientId.OnValueChanged += OnStateChanged;
        ApplyColor();
    }

    public override void OnNetworkDespawn()
    {
        OwnerClientId.OnValueChanged -= OnStateChanged;
    }

    public void RequestClaimOrChop()
    {
        if (IsSpawned)
            ClaimOrChopRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void ClaimOrChopRpc(RpcParams rpcParams = default)
    {
        ulong sender = rpcParams.Receive.SenderClientId;
        if (!NetworkManager.ConnectedClients.TryGetValue(sender, out NetworkClient client) ||
            client.PlayerObject == null ||
            Vector3.Distance(client.PlayerObject.transform.position, transform.position) > interactionDistance)
            return;

        if (OwnerClientId.Value == NoOwner)
        {
            OwnerClientId.Value = sender;
            ChopsRemaining.Value = ChopsToFree;
            return;
        }

        if (OwnerClientId.Value != sender)
            return;

        ChopsRemaining.Value--;
        if (ChopsRemaining.Value <= 0)
        {
            ChopsRemaining.Value = ChopsToFree;
            OwnerClientId.Value = NoOwner;
        }
    }

    private void OnStateChanged(ulong previous, ulong current)
    {
        ApplyColor();
    }

    private void ApplyColor()
    {
        if (resourceRenderer != null)
            resourceRenderer.material.color = OwnerClientId.Value == NoOwner ? FreeColor : ClaimedColor;
    }

    private void OnGUI()
    {
        Camera viewCamera = Camera.main;
        if (!IsSpawned || viewCamera == null)
            return;

        Vector3 screenPosition = viewCamera.WorldToScreenPoint(transform.position + Vector3.up * 1.15f);
        if (screenPosition.z <= 0f)
            return;

        string label = OwnerClientId.Value == NoOwner ? "Libre" : $"Jugador {OwnerClientId.Value}";
        Rect labelRect = new(screenPosition.x - 60f, Screen.height - screenPosition.y - 16f, 120f, 28f);
        Color previousBackground = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.08f, 0.08f, 0.08f, 0.9f);
        GUI.Box(labelRect, label);
        GUI.backgroundColor = previousBackground;
    }
}
