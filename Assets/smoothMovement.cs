using Unity.XR.CoreUtils; // Add this!
using UnityEngine;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit;

public class smoothMovement : MonoBehaviour
{
    public float glideDuration = 10.0f; // How long the slide takes
    public AnimationCurve glideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    private XROrigin _xrOrigin;
    private bool _isGliding = false;

    void Awake() => _xrOrigin = GetComponent<XROrigin>();

    // This is the function all 30 anchors will call
    public void StartGlide(BaseInteractionEventArgs args)
    {
        if (_isGliding) return;

        // Get the anchor's position from the object that triggered the teleport
        if (args.interactableObject is UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationAnchor anchor)
        {
            Vector3 targetPos = anchor.teleportAnchorTransform.position;
            StartCoroutine(GlideToPosition(targetPos));
        }
    }

    private IEnumerator GlideToPosition(Vector3 target)
    {
        _isGliding = true;
        Vector3 startPos = _xrOrigin.transform.position;
        float elapsed = 0;

        while (elapsed < glideDuration)
        {
            elapsed += Time.deltaTime;
            float t = glideCurve.Evaluate(elapsed / glideDuration);
            
            // Move the XR Origin smoothly
            _xrOrigin.transform.position = Vector3.Lerp(startPos, target, t);
            yield return null;
        }

        _isGliding = false;
    }
}