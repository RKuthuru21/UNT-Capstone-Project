using UnityEngine;
using UnityEngine.InputSystem;

public class PlatformTeleportMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Distance to move per teleport step")]
    public float stepDistance = 2f;

    [Header("Optional: Movement Cooldown")]
    [Tooltip("Minimum time between teleports (0 = instant)")]
    public float teleportCooldown = 0.1f;

    [Header("Optional: Vertical Controls")]
    [Tooltip("Enable vertical movement")]
    public bool enableVerticalMovement = false;

    [Header("Camera Settings")]
    [Tooltip("Attach the Main Camera here - it will move with the platform")]
    public Camera mainCamera;

    [Tooltip("Offset of camera relative to platform")]
    public Vector3 cameraOffset = new Vector3(0, 5, -10);

    [Tooltip("Should camera follow platform?")]
    public bool cameraFollows = true;

    private float lastTeleportTime = 0f;
    private Vector3 lastPosition;

    void Start()
    {
        lastPosition = transform.position;

        // Auto-find camera if not assigned
        if (mainCamera == null && cameraFollows)
        {
            mainCamera = Camera.main;
            if (mainCamera != null)
            {
                cameraOffset = mainCamera.transform.position - transform.position;
            }
        }
    }

    void Update()
    {
        // Check if cooldown has passed
        if (Time.time - lastTeleportTime < teleportCooldown)
            return;

        Vector3 teleportDirection = Vector3.zero;

        // Get keyboard input using new Input System
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Check horizontal inputs (WASD)
        if (keyboard.wKey.isPressed)
        {
            teleportDirection += Vector3.forward;
        }
        else if (keyboard.sKey.isPressed)
        {
            teleportDirection += Vector3.back;
        }

        if (keyboard.aKey.isPressed)
        {
            teleportDirection += Vector3.left;
        }
        else if (keyboard.dKey.isPressed)
        {
            teleportDirection += Vector3.right;
        }

        // Check vertical inputs if enabled
        if (enableVerticalMovement)
        {
            if (keyboard.spaceKey.isPressed)
            {
                teleportDirection += Vector3.up;
            }
            else if (keyboard.leftCtrlKey.isPressed)
            {
                teleportDirection += Vector3.down;
            }
        }

        // If any direction was pressed, teleport
        if (teleportDirection != Vector3.zero)
        {
            Teleport(teleportDirection.normalized);
        }
    }

    void Teleport(Vector3 direction)
    {
        // Calculate movement
        Vector3 movement = direction * stepDistance;

        // Teleport the platform instantly
        transform.position += movement;

        // Move camera with platform if enabled
        if (cameraFollows && mainCamera != null)
        {
            mainCamera.transform.position += movement;
        }

        lastTeleportTime = Time.time;
        lastPosition = transform.position;
    }

    // Public method to teleport programmatically
    public void TeleportInDirection(Vector3 direction)
    {
        Teleport(direction.normalized);
    }

    // Public method to set step distance at runtime
    public void SetStepDistance(float distance)
    {
        stepDistance = distance;
    }
}