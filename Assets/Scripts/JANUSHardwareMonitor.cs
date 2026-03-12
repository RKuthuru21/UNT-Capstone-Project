using System.Collections;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// JANUS — Hardware Monitor
///
/// Polls HMD tracking, left/right controller battery, and eye-tracking
/// calibration state every 2 seconds. Fires JANUSEvents.OnHardwareWarning
/// for any critical condition so the menu UI can display a status dot.
///
/// ATTACH TO: Any persistent GameObject (e.g. the JANUS_Menu canvas).
/// No Inspector wiring required — all XR devices are found automatically.
/// </summary>
public class JANUSHardwareMonitor : MonoBehaviour
{
    [Header("Poll Settings")]
    [Tooltip("How often (seconds) to re-check hardware status.")]
    [SerializeField] private float pollInterval = 2f;

    [Header("Battery Thresholds")]
    [Tooltip("Battery level (0–1) below which a warning is fired.")]
    [SerializeField] private float warnThreshold     = 0.30f;
    [Tooltip("Battery level (0–1) below which a critical warning is fired.")]
    [SerializeField] private float criticalThreshold = 0.15f;

    // ── Public state — read by JANUSMenuManager to update status dots ──
    public bool  HMDConnected          { get; private set; }
    public bool  LeftControllerOk      { get; private set; }
    public bool  RightControllerOk     { get; private set; }
    public float LeftBattery           { get; private set; } = 1f;
    public float RightBattery          { get; private set; } = 1f;
    public bool  EyeTrackingCalibrated { get; private set; }

    // ── Events for menu dot colours ───────────────────────────────────
    public System.Action<HardwareStatus> OnStatusChanged;

    public enum HardwareStatus { OK, Warn, Critical }

    private void OnEnable()  => StartCoroutine(PollRoutine());
    private void OnDisable() => StopAllCoroutines();

    private IEnumerator PollRoutine()
    {
        while (true)
        {
            Poll();
            yield return new WaitForSeconds(pollInterval);
        }
    }

    private void Poll()
    {
        // ── HMD ───────────────────────────────────────────────────────
        var hmds = new System.Collections.Generic.List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, hmds);
        HMDConnected = hmds.Count > 0;

        if (!HMDConnected)
        {
            JANUSEvents.OnHardwareWarning?.Invoke("HMD not detected.");
            OnStatusChanged?.Invoke(HardwareStatus.Critical);
            return;
        }

        // ── Eye tracking ──────────────────────────────────────────────
        EyeTrackingCalibrated = true;
        if (hmds.Count > 0 && hmds[0].TryGetFeatureValue(CommonUsages.isTracked, out bool tracked))
            EyeTrackingCalibrated = tracked;

        // ── Controllers ───────────────────────────────────────────────
        // Properties cannot be passed as ref/out in C#.
        // Use local variables, then assign back to the properties.
        float leftBat  = LeftBattery;
        float rightBat = RightBattery;
        bool  leftOk, rightOk;

        CheckController(InputDeviceCharacteristics.Left,  ref leftBat,  out leftOk);
        CheckController(InputDeviceCharacteristics.Right, ref rightBat, out rightOk);

        LeftBattery       = leftBat;
        RightBattery      = rightBat;
        LeftControllerOk  = leftOk;
        RightControllerOk = rightOk;

        // ── Overall status ────────────────────────────────────────────
        float lowest = Mathf.Min(LeftBattery, RightBattery);
        HardwareStatus status;

        if (lowest <= criticalThreshold)
        {
            status = HardwareStatus.Critical;
            string side = LeftBattery < RightBattery ? "Left" : "Right";
            JANUSEvents.OnHardwareWarning?.Invoke($"{side} controller battery critical ({lowest:P0}).");
        }
        else if (lowest <= warnThreshold)
        {
            status = HardwareStatus.Warn;
        }
        else
        {
            status = HardwareStatus.OK;
        }

        OnStatusChanged?.Invoke(status);
    }

    private void CheckController(InputDeviceCharacteristics side,
                                 ref float battery, out bool ok)
    {
        var devices = new System.Collections.Generic.List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(
            side | InputDeviceCharacteristics.Controller, devices);

        ok = devices.Count > 0;
        if (!ok) { battery = 0f; return; }

        if (!devices[0].TryGetFeatureValue(CommonUsages.batteryLevel, out float level))
            level = 1f; // Unknown — assume full

        battery = level;
    }

    /// <summary>Returns a summary string for the menu status row.</summary>
    public string GetStatusSummary()
    {
        if (!HMDConnected) return "HMD offline";
        if (!LeftControllerOk)  return "Left controller missing";
        if (!RightControllerOk) return "Right controller missing";

        float lowest = Mathf.Min(LeftBattery, RightBattery);
        if (lowest <= criticalThreshold) return $"Battery critical ({lowest:P0})";
        if (lowest <= warnThreshold)     return $"Battery low ({lowest:P0})";
        return "All systems OK";
    }
}
