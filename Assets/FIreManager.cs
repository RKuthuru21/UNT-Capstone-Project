using UnityEngine;


public class FireToggle : MonoBehaviour

{

    public GameObject smokeObject;



    public void ToggleSmoke()

    {

        if (smokeObject == null)

        {

            // Consistent error handling

            Debug.LogError("OVEN ERROR: You forgot to drag the Smoke object into the script slot!");

            return;

        }



        // This is the "Light Switch" logic

        bool currentState = smokeObject.activeSelf;

        smokeObject.SetActive(!currentState);

        

        // Consistent success messaging

        Debug.Log("OVEN SUCCESS: Smoke is now " + (smokeObject.activeSelf ? "ON" : "OFF"));

    }

}