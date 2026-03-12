using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// JANUS — VR Input Handler
///
/// Targets: Unity 6000.3.10f1 · XRI 3.3.1 · New Input System 1.18.0
///
/// ATTACH TO: The JANUS_Menu Canvas GameObject (same as JANUSMenuManager).
///
/// This works because JANUSMenuManager.SetVisible no longer disables the
/// entire GameObject — it only disables the Canvas component and CanvasGroup.
/// So this script stays alive and can hear the Menu button at all times.
///
/// NAVIGATION:
///   NearFarInteractor ray + Trigger  →  click UI elements (handled by XRUIInputModule)
///   Menu button (either controller)  →  toggle JANUS menu
///   Head-gaze dwell                  →  fallback activation (no controller required)
///
/// HAPTICS:
///   Hover new element  →  soft 30ms pulse
///   Select / click     →  firmer 55ms pulse
/// </summary>
[RequireComponent(typeof(JANUSMenuManager))]
public class JANUSVRInputHandler : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // Inspector
    // ─────────────────────────────────────────────

    [Header("NearFar Interactors (from XR Origin rig)")]
    [SerializeField] private NearFarInteractor leftInteractor;
    [SerializeField] private NearFarInteractor rightInteractor;

    [Header("Menu Toggle — Input Action")]
    [Tooltip("Drag the 'Menu' InputActionReference from XRI Default Input Actions here. " +
             "Maps to Menu button on Quest/OpenXR controllers.")]
    [SerializeField] private InputActionReference leftMenuAction;
    [SerializeField] private InputActionReference rightMenuAction;

    [Header("Gaze Dwell Fallback")]
    [SerializeField] private bool  gazeEnabled  = true;
    [SerializeField] private float dwellSeconds = 1.8f;

    [Header("Haptics")]
    [SerializeField] private bool  hapticsEnabled   = true;
    [SerializeField] private float hoverAmplitude   = 0.04f;
    [SerializeField] private float hoverDuration    = 0.030f;
    [SerializeField] private float selectAmplitude  = 0.22f;
    [SerializeField] private float selectDuration   = 0.055f;

    // ─────────────────────────────────────────────
    // Private
    // ─────────────────────────────────────────────

    private JANUSMenuManager _menu;

    // Gaze dwell
    private GameObject _gazeTarget;
    private float      _gazeTimer;

    // Hover tracking for haptics
    private GameObject _lastHoveredLeft;
    private GameObject _lastHoveredRight;

    // Cached XR input devices for haptics
    private UnityEngine.XR.InputDevice _leftDevice;
    private UnityEngine.XR.InputDevice _rightDevice;
    private bool _devicesResolved;

    // ─────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────

    private void Awake()
    {
        _menu = GetComponent<JANUSMenuManager>();
    }

    private void OnEnable()
    {
        if (leftMenuAction?.action  != null) leftMenuAction.action.performed  += OnMenuToggle;
        if (rightMenuAction?.action != null) rightMenuAction.action.performed += OnMenuToggle;

        leftMenuAction?.action.Enable();
        rightMenuAction?.action.Enable();
    }

    private void OnDisable()
    {
        if (leftMenuAction?.action  != null) leftMenuAction.action.performed  -= OnMenuToggle;
        if (rightMenuAction?.action != null) rightMenuAction.action.performed -= OnMenuToggle;
    }

    private void Update()
    {
        if (!_devicesResolved) ResolveDevices();
        HandleHoverHaptics();
        if (gazeEnabled) HandleGazeDwell();
    }

    // ─────────────────────────────────────────────
    // Resolve XR Input Devices (for haptics)
    // ─────────────────────────────────────────────

    private void ResolveDevices()
    {
        var devices = new List<UnityEngine.XR.InputDevice>();

        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller, devices);
        if (devices.Count > 0) _leftDevice = devices[0];

        devices.Clear();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, devices);
        if (devices.Count > 0) _rightDevice = devices[0];

        _devicesResolved = _leftDevice.isValid && _rightDevice.isValid;
    }

    // ─────────────────────────────────────────────
    // Menu Toggle
    // ─────────────────────────────────────────────

    private void OnMenuToggle(InputAction.CallbackContext ctx)
    {
        _menu.ToggleVisible();
        _menu.PlaceInFrontOfPlayer();
        SendHapticToDevice(_rightDevice, selectAmplitude, selectDuration);
        Debug.Log("[JANUS Input] Menu toggled.");
    }

    // ─────────────────────────────────────────────
    // Hover Haptics
    // ─────────────────────────────────────────────

    private void HandleHoverHaptics()
    {
        CheckHover(rightInteractor, ref _lastHoveredRight, _rightDevice);
        CheckHover(leftInteractor,  ref _lastHoveredLeft,  _leftDevice);
    }

    private void CheckHover(NearFarInteractor interactor, ref GameObject last,
                            UnityEngine.XR.InputDevice device)
    {
        if (interactor == null) return;

        GameObject current = null;
        if (interactor.TryGetCurrentUIRaycastResult(out var result))
            current = result.gameObject;

        if (current != null && current != last)
            SendHapticToDevice(device, hoverAmplitude, hoverDuration);

        last = current;
    }

    // ─────────────────────────────────────────────
    // Gaze Dwell (hands-free fallback)
    // ─────────────────────────────────────────────

    private void HandleGazeDwell()
    {
        if (_lastHoveredLeft != null || _lastHoveredRight != null)
        {
            _gazeTarget = null;
            _gazeTimer  = 0f;
            return;
        }

        var cam = Camera.main;
        if (cam == null) return;

        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out var hit, 6f))
        {
            if (hit.collider.gameObject == _gazeTarget)
            {
                _gazeTimer += Time.deltaTime;
                JANUSEvents.OnGazeDwellProgress?.Invoke(_gazeTimer / dwellSeconds);

                if (_gazeTimer >= dwellSeconds)
                {
                    FireClick(hit.collider.gameObject);
                    SendHapticToDevice(_rightDevice, selectAmplitude, selectDuration);
                    _gazeTarget = null;
                    _gazeTimer  = 0f;
                }
            }
            else
            {
                _gazeTarget = hit.collider.gameObject;
                _gazeTimer  = 0f;
            }
        }
        else
        {
            _gazeTarget = null;
            _gazeTimer  = 0f;
        }
    }

    private static void FireClick(GameObject target)
    {
        var pointer = new PointerEventData(EventSystem.current);
        ExecuteEvents.ExecuteHierarchy(target, pointer, ExecuteEvents.pointerClickHandler);
    }

    // ─────────────────────────────────────────────
    // Haptics — XRI 3.3.1 safe approach
    // ─────────────────────────────────────────────

    private void SendHapticToDevice(UnityEngine.XR.InputDevice device, float amplitude, float duration)
    {
        if (!hapticsEnabled || !device.isValid) return;

        HapticCapabilities caps;
        if (device.TryGetHapticCapabilities(out caps) && caps.supportsImpulse)
        {
            device.SendHapticImpulse(0u, amplitude, duration);
        }
    }

    public void SelectHaptic(bool left = false)
        => SendHapticToDevice(left ? _leftDevice : _rightDevice, selectAmplitude, selectDuration);
}
