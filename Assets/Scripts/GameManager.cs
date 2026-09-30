using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

/// <summary>Starts a small resource field on the server and provides host/client controls.</summary>
public sealed class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private Cubito resourcePrefab;
    [SerializeField, Min(1)] private int resourceCount = 8;
    [SerializeField] private float spacing = 2.5f;
    [SerializeField] private string address = "127.0.0.1";

    private void Awake() => Instance = this;

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;

        if (resourcePrefab == null)
        {
            Debug.LogError("Asigna el prefab Cubito (recurso) en GameManager.");
            return;
        }

        int columns = Mathf.CeilToInt(Mathf.Sqrt(resourceCount));
        for (int i = 0; i < resourceCount; i++)
        {
            int x = i % columns;
            int z = i / columns;
            Vector3 position = new((x - (columns - 1) * 0.5f) * spacing, 0.5f,
                (z - (Mathf.CeilToInt(resourceCount / (float)columns) - 1) * 0.5f) * spacing + 5f);
            Instantiate(resourcePrefab, position, Quaternion.identity).NetworkObject.Spawn();
        }
    }

    private void OnGUI()
    {
        if (NetworkManager.Singleton == null || NetworkManager.Singleton.IsListening)
            return;

        GUI.Box(new Rect(12, 12, 270, 128), "Netcode: conexión local o por IP");
        GUI.Label(new Rect(24, 42, 70, 24), "Dirección");
        address = GUI.TextField(new Rect(94, 40, 170, 24), address);
        if (GUI.Button(new Rect(24, 76, 76, 42), "Host"))
        {
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData("127.0.0.1", 7777, "0.0.0.0");
            NetworkManager.Singleton.StartHost();
        }
        if (GUI.Button(new Rect(108, 76, 76, 42), "Cliente"))
        {
            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetConnectionData(address, 7777);
            NetworkManager.Singleton.StartClient();
        }
        if (GUI.Button(new Rect(192, 76, 72, 42), "Servidor"))
        {
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData("127.0.0.1", 7777, "0.0.0.0");
            NetworkManager.Singleton.StartServer();
        }
    }

    public override void OnNetworkDespawn()
    {
        if (Instance == this)
            Instance = null;
    }
}

