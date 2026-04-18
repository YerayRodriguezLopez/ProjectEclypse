using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using System.Collections.Generic;

public class XRDebugInfo : MonoBehaviour
{
    void Start()
    {
        Debug.Log("=== XR DEBUG INFO ===");
        
        // 1. Active XR Loader (runtime layer)
        var loader = XRGeneralSettings.Instance?.Manager?.activeLoader;

        if (loader != null)
        {
            Debug.Log($"XR Loader: {loader.name}");
        }
        else
        {
            Debug.LogWarning("No active XR Loader found!");
        }

        // 2. Headset detection
        var hmdDevices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.HeadMounted,
            hmdDevices
        );

        foreach (var device in hmdDevices)
        {
            Debug.Log($"HMD Name: {device.name}");
            Debug.Log($"HMD Manufacturer: {device.manufacturer}");
        }

        // 3. Controller detection
        var controllerDevices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Controller,
            controllerDevices
        );

        foreach (var device in controllerDevices)
        {
            Debug.Log($"Controller: {device.name} ({device.characteristics})");
        }

        // 4. Simple classification (what you actually care about)
        DetectPlatform(hmdDevices);
    }

    void DetectPlatform(List<InputDevice> hmdDevices)
    {
        foreach (var device in hmdDevices)
        {
            string name = device.name.ToLower();

            if (name.Contains("oculus") || name.Contains("meta"))
            {
                Debug.Log("➡ Detected: META QUEST (Quest 2 / 3 / Pro)");
            }
            else if (name.Contains("vive") || name.Contains("index"))
            {
                Debug.Log("➡ Detected: STEAMVR (HTC Vive / Valve Index)");
            }
            else
            {
                Debug.Log("➡ Detected: UNKNOWN XR DEVICE");
            }
        }
    }
}