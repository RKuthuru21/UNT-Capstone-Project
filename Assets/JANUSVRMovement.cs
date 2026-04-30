using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

/// <summary>
/// JANUS — VR Movement Controller
///
/// Targets: Unity 6000.3.10f1 · XRI 3.3.1 · OpenXR 1.16.1
///
/// Replaces the old desktop-only Player Movement.cs and PadTeleportClicker.cs.
/// Attach to the XR Origin (XR Rig) root GameObject (NOT a child).
///
/// Auto-finds and configures:
///   • ContinuousMoveProvider  → thumbstick walk + strafe (head-relative)
///   • ContinuousTurnProvider  → smooth thumbstick turning (speed set in Inspector on the provider)
///   • TeleportationProvider   → arc teleport on thumbstick-up
///
/// SnapTurnProvider is explicitly DISABLED on startup. If you want snap turn
/// instead of smooth turn, uncheck "Use Continuous Turn" in this component's Inspector.
///
/// Freezes all locomotion when the JANUS menu opens and restores on close.
///
/// NOTE: enableStrafe and useGravity do NOT exist on ContinuousMoveProvider
/// in XRI 3.3.1. They are intentionally omitted to avoid compile errors.
/// </summary>
public class JANUSVRMovement : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────
    // Inspector
    // ─────────────────────────────────────────────────────────────────────

    [Header("Move Settings")]
    [Tooltip("Walk speed in metres per second.")]
    [SerializeField] private float moveSpeed = 1.4f;

    [Header("Turn Settings")]
    [Tooltip("Use smooth continuous turning. If unchecked, snap turn is used instead.")]
    [SerializeField] private bool useContinuousTurn = true;

    // ─────────────────────────────────────────────────────────────────────
    // Cached providers
    // ─────────────────────────────────────────────────────────────────────

    private ContinuousMoveProvider  _move;
    private SnapTurnProvider        _snap;
    private ContinuousTurnProvider  _continuous;
    private TeleportationProvider   _teleport;

    // ─────────────────────────────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        // All providers live on children of the XR Origin rig
        _move       = GetComponentInChildren<ContinuousMoveProvider>(true);
        _snap       = GetComponentInChildren<SnapTurnProvider>(true);
        _continuous = GetComponentInChildren<ContinuousTurnProvider>(true);
        _teleport   = GetComponentInChildren<TeleportationProvider>(true);

        ConfigureMove();
        ConfigureTurn();
        LogStatus();
    }

    private void OnEnable()
    {
        JANUSEvents.OnMenuOpened += FreezeLocomotion;
        JANUSEvents.OnMenuClosed += UnfreezeLocomotion;
    }

    private void OnDisable()
    {
        JANUSEvents.OnMenuOpened -= FreezeLocomotion;
        JANUSEvents.OnMenuClosed -= UnfreezeLocomotion;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Configuration
    // ─────────────────────────────────────────────────────────────────────

    private void ConfigureMove()
    {
        if (_move == null)
        {
            Debug.LogWarning("[JANUS Movement] ContinuousMoveProvider not found on XR Origin children.");
            return;
        }

        _move.moveSpeed = moveSpeed;

        Debug.Log($"[JANUS Movement] ContinuousMoveProvider: speed={moveSpeed} m/s ✓");
    }

    private void ConfigureTurn()
    {
        // Enable the chosen turn mode and disable the other.
        // Turn speed / snap angle are NOT overwritten here —
        // set them directly on the provider's Inspector and they will stick.

        if (useContinuousTurn)
        {
            if (_continuous != null) _continuous.enabled = true;
            if (_snap       != null) _snap.enabled       = false;
            Debug.Log("[JANUS Movement] Continuous turn enabled ✓ (set speed on ContinuousTurnProvider Inspector)");
        }
        else
        {
            if (_snap       != null) _snap.enabled       = true;
            if (_continuous != null) _continuous.enabled  = false;
            Debug.Log("[JANUS Movement] Snap turn enabled ✓ (set angle on SnapTurnProvider Inspector)");
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Freeze / Unfreeze (menu events)
    // ─────────────────────────────────────────────────────────────────────

    private void FreezeLocomotion()
    {
        SetLocomotionEnabled(false);
        Debug.Log("[JANUS Movement] Locomotion frozen (menu open).");
    }

    private void UnfreezeLocomotion()
    {
        SetLocomotionEnabled(true);
        Debug.Log("[JANUS Movement] Locomotion restored (menu closed).");
    }

    private void SetLocomotionEnabled(bool enabled)
    {
        if (_move != null) _move.enabled = enabled;
        if (_teleport != null) _teleport.enabled = enabled;

        // Only enable the active turn provider
        if (useContinuousTurn)
        {
            if (_continuous != null) _continuous.enabled = enabled;
            // Keep snap disabled regardless
        }
        else
        {
            if (_snap != null) _snap.enabled = enabled;
            // Keep continuous disabled regardless
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Debug
    // ─────────────────────────────────────────────────────────────────────

    private void LogStatus()
    {
        string Tick(Object o) => o != null ? "✓" : "✗ MISSING";
        string turnMode = useContinuousTurn ? "Continuous" : "Snap";
        Debug.Log($"[JANUS Movement] Provider status (Turn mode: {turnMode}):\n" +
                  $"  ContinuousMoveProvider : {Tick(_move)}\n" +
                  $"  SnapTurnProvider       : {Tick(_snap)}\n" +
                  $"  ContinuousTurnProvider : {Tick(_continuous)}\n" +
                  $"  TeleportationProvider  : {Tick(_teleport)}");
    }
}
