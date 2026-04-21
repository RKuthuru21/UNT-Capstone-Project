using UnityEngine;
using UnityEngine.SceneManagement; // This line is vital for scene switching

public class MainMenu : MonoBehaviour
{
    // Call this function from your "Start" button
    public void StartSimulation()
    {
        // Make sure "SimulationScene" matches your scene's name exactly!
        SceneManager.LoadScene("TriggersforKitchen(Updated)");
    }

    // 2. Move from Simulation back to the Menu (Resets everything)
    public void GoToMainMenu()
    {
        // Replace "MainMenu" with the exact name of your start screen scene
        SceneManager.LoadScene("MainMenu");
    }

    // Call this function from your "Quit" button
    public void QuitApp()
    {
        Debug.Log("Quit Button Pressed");
        Application.Quit();
    }
}