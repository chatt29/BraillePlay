using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class WirelessHaptics : MonoBehaviour
{
    [Header("Bridge Settings")]
    public string bridgeIp = "127.0.0.1";
    public int bridgePort = 50555;

    private UdpClient udpClient;
    private IPEndPoint endPoint;

    private void Awake()
    {
        udpClient = new UdpClient();
        endPoint = new IPEndPoint(IPAddress.Parse(bridgeIp), bridgePort);
    }

    public void TriggerHaptic()
    {
        byte[] data = Encoding.UTF8.GetBytes("V");
        udpClient.Send(data, data.Length, endPoint);

        Debug.Log("Sent haptic V to bridge.");
    }

    private void OnDestroy()
    {
        udpClient?.Close();
        udpClient = null;
    }
}