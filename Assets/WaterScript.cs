using UnityEngine;

public class WaterScript : MonoBehaviour
{
    // Renamed to reflect water instead of smoke
    public GameObject waterObject;

    public void ToggleWater()
    {
        if (waterObject == null)
        {
            // Updated error message for clarity
            Debug.LogError("WATER SYSTEM ERROR: You forgot to drag the Water object into the script slot!");
            return;
        }

        // Logic to flip the current active state
        bool currentState = waterObject.activeSelf;
        waterObject.SetActive(!currentState);
        
        // Updated success messaging
        Debug.Log("WATER SYSTEM SUCCESS: Water is now " + (waterObject.activeSelf ? "ON" : "OFF"));
    }
}