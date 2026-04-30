using UnityEngine;

public class ToasterScript1 : MonoBehaviour
{
    // The Toast or Heating Element GameObject
    public GameObject toastObject;

    public void ToggleToaster()
    {
        if (toastObject == null)
        {
            // Error handling for the Inspector slot
            Debug.LogError("TOASTER ERROR: You forgot to drag the Toast object into the script slot!");
            return;
        }

        // Logic to flip the current active state (Pop up / Push down)
        bool currentState = toastObject.activeSelf;
        toastObject.SetActive(!currentState);
        
        // Console feedback
        Debug.Log("TOASTER SUCCESS: Toast is now " + (toastObject.activeSelf ? "COOKING" : "READY"));
    }
}