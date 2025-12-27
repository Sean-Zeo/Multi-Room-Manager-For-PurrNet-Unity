using UnityEngine;
using PurrNet;
using PurrNet.Transports;

public class PurrNetNetworkManagerHUD : MonoBehaviour
{
    public int offsetX;
    public int offsetY;
    private string addressInput = "localhost";
    private string portInput = "7777";

    private void Start()
    {
        GetTransportSettings();
    }

    private void OnGUI()
    {
        int width = 300;
        GUILayout.BeginArea(new Rect(10 + offsetX, 40 + offsetY, width, 9999));

        if (InstanceHandler.NetworkManager.clientState == ConnectionState.Disconnected &&
            InstanceHandler.NetworkManager.serverState == ConnectionState.Disconnected)
        {
#if !UNITY_WEBGL
            if (GUILayout.Button("Server + Client"))
            {
                SetTransportSettings();
                InstanceHandler.NetworkManager.StartServer();
                InstanceHandler.NetworkManager.StartClient();
            }
#endif
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Client"))
            {
                SetTransportSettings();
                InstanceHandler.NetworkManager.StartClient();
            }
            addressInput = GUILayout.TextField(addressInput);
            portInput = GUILayout.TextField(portInput);
            GUILayout.EndHorizontal();
#if !UNITY_WEBGL
            if (GUILayout.Button("Server Only"))
            {
                SetTransportSettings();
                InstanceHandler.NetworkManager.StartServer();
            }
#endif
        }
        else
        {
            if (InstanceHandler.NetworkManager.clientState == ConnectionState.Connecting)
                GUILayout.Label($"Connecting to " + addressInput + "..");
            else if (InstanceHandler.NetworkManager.clientState == ConnectionState.Connected)
            {
                GUILayout.Label($"<b>Client</b>: connected to {addressInput}");
                if (InstanceHandler.NetworkManager.serverState == ConnectionState.Connected)
                {
                    if (GUILayout.Button("Stop Server + Client"))
                        InstanceHandler.NetworkManager.StopServer();
                }
                else
                {
                    if (GUILayout.Button("Stop Client"))
                        InstanceHandler.NetworkManager.StopClient();
                }
            }
            else if (InstanceHandler.NetworkManager.serverState == ConnectionState.Connecting)
                GUILayout.Label($"Creating server..");
            else if (InstanceHandler.NetworkManager.serverState == ConnectionState.Connected)
            {
                GUILayout.Label("<b>Server</b>: running");
                if (GUILayout.Button("Stop Server"))
                {
                    InstanceHandler.NetworkManager.StopServer();
                }
            }
        }

        GUILayout.EndArea();
    }

    void GetTransportSettings()
    {
        if (InstanceHandler.NetworkManager.transport != null)
        {
            if (InstanceHandler.NetworkManager.transport is WebTransport webTransport)
            {
                addressInput = webTransport.address;
                portInput = webTransport.serverPort.ToString();
            }
            else if (InstanceHandler.NetworkManager.transport is UDPTransport udpTransport)
            {
                addressInput = udpTransport.address;
                portInput = udpTransport.serverPort.ToString();
            }
        }
    }
    void SetTransportSettings()
    {
        if (InstanceHandler.NetworkManager.transport != null)
        {
            if (InstanceHandler.NetworkManager.transport is WebTransport webTransport)
            {
                webTransport.address = addressInput;
                webTransport.serverPort = ushort.Parse(portInput);
            }
            else if (InstanceHandler.NetworkManager.transport is UDPTransport udpTransport)
            {
                udpTransport.address = addressInput;
                udpTransport.serverPort = ushort.Parse(portInput);
            }
        }
    }
}
