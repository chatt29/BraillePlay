using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class LocalHapticSender : MonoBehaviour
{
    UdpClient udp;

    void Start()
    {
        udp = new UdpClient();
    }

    public void TriggerHaptic()
    {
        byte[] data = Encoding.UTF8.GetBytes("V");
        udp.Send(data, data.Length, "127.0.0.1", 50555);
        Debug.Log("Sent V to local bridge");
    }

    void OnApplicationQuit()
    {
        udp?.Close();
    }
}