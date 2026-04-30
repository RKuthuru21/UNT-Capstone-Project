using UnityEngine;

/// <summary>
/// JANUS — Floor Plan Data
///
/// One ScriptableObject per environment layout.
/// Create via: Assets → Create → JANUS → Floor Plan Data
///
/// Assign each asset to the FloorPlanCards array in JANUSMenuManager.
/// </summary>
[CreateAssetMenu(menuName = "JANUS/Floor Plan Data", fileName = "FloorPlan_New")]
public class FloorPlanData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Display name shown on the selection card, e.g. 'Layout A — Standard'.")]
    public string LayoutName = "Layout A";

    [Tooltip("Short description shown beneath the name on the card.")]
    public string Description = "3 rooms, standard configuration";

    [Tooltip("The Unity scene name to load when this layout is selected. Must match exactly.")]
    public string SceneName = "";

    [Header("Visual")]
    [Tooltip("Optional thumbnail sprite shown on the floor plan card.")]
    public Sprite Thumbnail;

    [Header("Assessment Metadata")]
    [Tooltip("Complexity rating 1–5 shown to clinician.")]
    [Range(1, 5)]
    public int ComplexityLevel = 1;

    [Tooltip("Estimated assessment duration in minutes.")]
    public int EstimatedMinutes = 10;
}
