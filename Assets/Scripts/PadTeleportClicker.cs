using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PadTeleportClicker : MonoBehaviour
{
    public Camera cam;
    public LayerMask padMask;      // set to TeleportPad in Inspector
    public float yOffset = 0.05f;  // prevents clipping into floor

    private CharacterController cc;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (cam == null) cam = Camera.main;
    }

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null || cam == null) return;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());

            if (Physics.Raycast(ray, out RaycastHit hit, 5000f, padMask, QueryTriggerInteraction.Ignore))
            {
                Vector3 target = hit.collider.transform.position;
                target.y += yOffset;

                cc.enabled = false;
                transform.position = target;
                cc.enabled = true;
            }
        }
    }
}