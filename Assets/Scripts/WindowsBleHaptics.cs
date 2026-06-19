using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using InTheHand.Bluetooth;

public class WindowsBleHaptics : MonoBehaviour
{
    public string deviceName = "BP_HAPTIC_TEST";

    private static readonly Guid SERVICE_UUID =
        Guid.Parse("12345678-1234-1234-1234-1234567890ab");

    private static readonly Guid CHARACTERISTIC_UUID =
        Guid.Parse("abcd1234-5678-90ab-cdef-1234567890ab");

    private BluetoothDevice device;
    private GattCharacteristic characteristic;
    private bool connected;

    private async void Start()
    {
        await Connect();
    }

    private async Task Connect()
    {
        Debug.Log("Scanning for " + deviceName);

        var devices = await Bluetooth.ScanForDevicesAsync(
            new RequestDeviceOptions
            {
                AcceptAllDevices = true
            }
        );

        foreach (var dev in devices)
        {
            Debug.Log("Found BLE device: " + dev.Name);

            if (!string.IsNullOrEmpty(dev.Name) &&
                dev.Name.Trim().Equals(deviceName, StringComparison.OrdinalIgnoreCase))
            {
                device = dev;
                Debug.Log("Matched ESP32: " + dev.Name);
                break;
            }
        }

        if (device == null)
        {
            Debug.LogError("ESP32 not found.");
            return;
        }

        Debug.Log("Getting haptic service...");
        var service = await device.Gatt.GetPrimaryServiceAsync(SERVICE_UUID);

        if (service == null)
        {
            Debug.LogError("Haptic service not found.");
            return;
        }

        Debug.Log("Getting haptic characteristic...");
        characteristic = await service.GetCharacteristicAsync(CHARACTERISTIC_UUID);

        if (characteristic == null)
        {
            Debug.LogError("Haptic characteristic not found.");
            return;
        }

        connected = true;
        Debug.Log("BLE haptics connected.");
    }

    public async void TriggerHaptic()
    {
        if (!connected || characteristic == null)
        {
            Debug.LogWarning("BLE haptics not connected yet.");
            return;
        }

        byte[] data = { (byte)'V' };

        await characteristic.WriteValueWithResponseAsync(data);

        Debug.Log("Sent V to ESP32.");
    }
}