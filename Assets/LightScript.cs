using UnityEngine;

public class LightScript : MonoBehaviour
{
    // The Light GameObject you want to turn on/off
    public GameObject lightObject;

    public void ToggleLight()
    {
        if (lightObject == null)
        {
            // Helpful error if the slot is empty in the Inspector
            Debug.LogError("LIGHT SYSTEM ERROR: You forgot to drag the Light object into the script slot!");
            return;
        }

        // Logic to flip the current active state
        bool currentState = lightObject.activeSelf;
        lightObject.SetActive(!currentState);
        
        // Success messaging for the Console
        Debug.Log("LIGHT SYSTEM SUCCESS: Light is now " + (lightObject.activeSelf ? "ON" : "OFF"));
    }
}