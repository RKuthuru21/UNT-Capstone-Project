using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class smoothTeleport : MonoBehaviour
{
    public GameObject xrOrigin;
    public float transitionTime = 0.5f;

    public void SmoothMove(BaseInteractionEventArgs args)
    {
        // Get the destination from the Anchor's transform
        Vector3 targetPos = args.interactableObject.transform.position;
        StartCoroutine(MoveRoutine(targetPos));
    }

    IEnumerator MoveRoutine(Vector3 target)
    {
        Vector3 startPos = xrOrigin.transform.position;
        float elapsed = 0;

        while (elapsed < transitionTime)
        {
            // Smoothly interpolate position
            xrOrigin.transform.position = Vector3.Lerp(startPos, target, elapsed / transitionTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
        xrOrigin.transform.position = target;
    }
}