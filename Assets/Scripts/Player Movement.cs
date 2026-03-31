using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public Camera playerCamera;

    [Header("Movement")]
    public float walkSpeed = 6f;
    public float runSpeed = 12f;
    public float jumpPower = 7f;
    public float gravity = 10f;

    [Header("Mouse Look")]
    public float lookSpeed = 0.1f;      // New Input System mouse delta is sensitive, start small
    public float lookXLimit = 45f;

    [Header("Crouch")]
    public float defaultHeight = 2f;
    public float crouchHeight = 1f;
    public float crouchSpeed = 3f;

    [Header("Key Turning")]
    public bool enableSmoothKeyTurn = false; // false = snap turning
    public float keyTurnSpeed = 180f;        // degrees per second (smooth)
    public float snapTurnAngle = 90f;        // degrees per press (snap)
    public bool mouseLookRequiresRightClick = true;

    private Vector3 moveDirection = Vector3.zero;
    private float rotationX = 0f;
    private CharacterController characterController;
    private bool canMove = true;

    void Start()
    {
        characterController = GetComponent<CharacterController>();

        // Lock cursor for FPS style view
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;

        if (keyboard == null)
            return;

        // --- Movement (WASD) ---
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        float vertical = 0f;
        if (keyboard.wKey.isPressed) vertical += 1f;
        if (keyboard.sKey.isPressed) vertical -= 1f;

        float horizontal = 0f;
        if (keyboard.dKey.isPressed) horizontal += 1f;
        if (keyboard.aKey.isPressed) horizontal -= 1f;

        bool isRunning = keyboard.leftShiftKey.isPressed;

        float curSpeedX = canMove ? (isRunning ? runSpeed : walkSpeed) * vertical : 0f;
        float curSpeedY = canMove ? (isRunning ? runSpeed : walkSpeed) * horizontal : 0f;

        float movementDirectionY = moveDirection.y;
        moveDirection = (forward * curSpeedX) + (right * curSpeedY);

        // --- Jump (Space) ---
        if (keyboard.spaceKey.wasPressedThisFrame && canMove && characterController.isGrounded)
        {
            moveDirection.y = jumpPower;
        }
        else
        {
            moveDirection.y = movementDirectionY;
        }

        // --- Gravity ---
        if (!characterController.isGrounded)
        {
            moveDirection.y -= gravity * Time.deltaTime;
        }

        // --- Crouch (R held) ---
        if (keyboard.rKey.isPressed && canMove)
        {
            characterController.height = crouchHeight;
            walkSpeed = crouchSpeed;
            runSpeed = crouchSpeed;
        }
        else
        {
            characterController.height = defaultHeight;
            walkSpeed = 6f;
            runSpeed = 12f;
        }

        characterController.Move(moveDirection * Time.deltaTime);

        // --- Key Turning (Arrow Keys) ---
        if (canMove)
        {
            if (enableSmoothKeyTurn)
            {
                // Hold left/right arrows to rotate smoothly
                float turn = 0f;
                if (keyboard.rightArrowKey.isPressed) turn += 1f;
                if (keyboard.leftArrowKey.isPressed) turn -= 1f;

                if (turn != 0f)
                {
                    transform.Rotate(0f, turn * keyTurnSpeed * Time.deltaTime, 0f);
                }
            }
            else
            {
                // Tap arrows to snap turn
                if (keyboard.rightArrowKey.wasPressedThisFrame)
                    transform.Rotate(0f, snapTurnAngle, 0f);

                if (keyboard.leftArrowKey.wasPressedThisFrame)
                    transform.Rotate(0f, -snapTurnAngle, 0f);

                // Quick 180 turn (tap down arrow)
                if (keyboard.downArrowKey.wasPressedThisFrame)
                    transform.Rotate(0f, 180f, 0f);

                // Optional 360 (does nothing visually but included if you want)
                // if (keyboard.upArrowKey.wasPressedThisFrame)
                //     transform.Rotate(0f, 360f, 0f);
            }
        }

        // --- Mouse Look (optional: only while holding Right Click) ---
        if (canMove && mouse != null)
        {
            bool allowMouseLook = !mouseLookRequiresRightClick || mouse.rightButton.isPressed;

            if (allowMouseLook)
            {
                Vector2 mouseDelta = mouse.delta.ReadValue();

                rotationX += -mouseDelta.y * lookSpeed;
                rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);

                playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
                transform.Rotate(0f, mouseDelta.x * lookSpeed, 0f);
            }
        }

        // --- Optional: Press Esc to unlock mouse cursor while testing ---
        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}