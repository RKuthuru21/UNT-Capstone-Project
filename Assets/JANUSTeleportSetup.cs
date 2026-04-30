using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

/// <summary>
/// JANUS — Teleport Setup
///
/// Targets: Unity 6000.3.10f1 · XRI 3.3.1
///
/// Solves the core problem: floor plan meshes imported from Sweet Home 3D
/// have MeshColliders but NO TeleportationArea components, so the teleport
/// ray has nowhere valid to land.
///
/// WHAT THIS SCRIPT DOES:
///   1. Finds all floor/ground meshes (by tag, layer, or name pattern)
///   2. Adds TeleportationArea + Collider to each
///   3. Sets their Interaction Layer to "Teleport" (layer 31 in your project)
///   4. Optionally creates TeleportationAnchors at predefined spawn points
///   5. Validates the full teleport pipeline and logs any issues
///
/// ATTACH TO: XR Origin root (same object as JANUSVRMovement)
///
/// SETUP:
///   1. Tag your floor meshes as "TeleportFloor" in the Inspector
///      OR set floorNamePatterns to match mesh names (e.g. "ground", "room_")
///   2. Assign a teleport reticle prefab if you have one
///   3. Hit Play — this script handles the rest
/// </summary>
public class JANUSTeleportSetup : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────
    // Inspector
    // ─────────────────────────────────────────────────────────────────────

    [Header("Floor Detection")]
    [Tooltip("Tag applied to floor GameObjects. Script will find all objects with this tag.")]
    [SerializeField] private string floorTag = "TeleportFloor";

    [Tooltip("If no tagged objects found, match mesh names containing these patterns (case-insensitive).")]
    [SerializeField] private string[] floorNamePatterns = new string[]
    {
        "ground", "floor", "room_", "carpet", "rug"
    };

    [Header("Interaction Layer")]
    [Tooltip("XRI Interaction Layer index for teleport. Must match the teleport interactor's layer mask. " +
             "Your InteractionLayerSettings.asset has 'Teleport' at index 31.")]
    [SerializeField] private int teleportInteractionLayer = 31;

    [Header("Teleport Anchors (Optional)")]
    [Tooltip("Predefined spawn/landing points. Leave empty if you only want area teleport on floors.")]
    [SerializeField] private Transform[] anchorPoints;

    [Tooltip("Reticle prefab shown at the teleport destination. Assign from XRI Starter Assets.")]
    [SerializeField] private GameObject teleportReticlePrefab;

    // ─────────────────────────────────────────────────────────────────────
    // Runtime
    // ─────────────────────────────────────────────────────────────────────

    private readonly List<TeleportationArea> _createdAreas = new List<TeleportationArea>();
    private readonly List<TeleportationAnchor> _createdAnchors = new List<TeleportationAnchor>();

    private void Start()
    {
        var floors = FindFloorObjects();

        if (floors.Count == 0)
        {
            Debug.LogError(
                "[JANUS Teleport] No floor meshes found!\n" +
                $"  → Tag floor objects as \"{floorTag}\"\n" +
                $"  → OR ensure mesh names contain: {string.Join(", ", floorNamePatterns)}\n" +
                "  Teleportation will NOT work until floors are configured.");
            return;
        }

        foreach (var floor in floors)
            ConfigureTeleportArea(floor);

        CreateAnchors();
        ValidatePipeline();

        Debug.Log($"[JANUS Teleport] Setup complete: {_createdAreas.Count} areas, {_createdAnchors.Count} anchors.");
    }

    // ─────────────────────────────────────────────────────────────────────
    // Floor Detection
    // ─────────────────────────────────────────────────────────────────────

    private List<GameObject> FindFloorObjects()
    {
        var results = new List<GameObject>();

        // Strategy 1: Find by tag
        try
        {
            var tagged = GameObject.FindGameObjectsWithTag(floorTag);
            if (tagged != null && tagged.Length > 0)
            {
                results.AddRange(tagged);
                Debug.Log($"[JANUS Teleport] Found {tagged.Length} floor(s) by tag \"{floorTag}\".");
                return results;
            }
        }
        catch (UnityException)
        {
            // Tag doesn't exist yet — fall through to name matching
            Debug.LogWarning($"[JANUS Teleport] Tag \"{floorTag}\" not defined. " +
                             "Add it via Edit → Project Settings → Tags and Layers. " +
                             "Falling back to name-pattern matching.");
        }

        // Strategy 2: Find by name pattern among all MeshRenderers
        var allRenderers = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
        foreach (var renderer in allRenderers)
        {
            string name = renderer.gameObject.name.ToLower();
            foreach (var pattern in floorNamePatterns)
            {
                if (name.Contains(pattern.ToLower()))
                {
                    results.Add(renderer.gameObject);
                    break;
                }
            }
        }

        Debug.Log($"[JANUS Teleport] Found {results.Count} floor(s) by name pattern matching.");
        return results;
    }

    // ─────────────────────────────────────────────────────────────────────
    // TeleportationArea Setup
    // ─────────────────────────────────────────────────────────────────────

    private void ConfigureTeleportArea(GameObject floor)
    {
        // Skip if already configured
        if (floor.GetComponent<TeleportationArea>() != null)
        {
            Debug.Log($"[JANUS Teleport] \"{floor.name}\" already has TeleportationArea — skipping.");
            _createdAreas.Add(floor.GetComponent<TeleportationArea>());
            return;
        }

        // Ensure a collider exists (Sweet Home 3D OBJ imports get MeshCollider by default)
        var collider = floor.GetComponent<Collider>();
        if (collider == null)
        {
            var meshFilter = floor.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                var mc = floor.AddComponent<MeshCollider>();
                mc.sharedMesh = meshFilter.sharedMesh;
                Debug.Log($"[JANUS Teleport] Added MeshCollider to \"{floor.name}\".");
            }
            else
            {
                Debug.LogWarning($"[JANUS Teleport] \"{floor.name}\" has no collider and no mesh — cannot teleport to it.");
                return;
            }
        }

        // Add TeleportationArea
        var area = floor.AddComponent<TeleportationArea>();

        // Set interaction layer to "Teleport" (layer 31)
        // This prevents the teleport ray from hitting non-teleport interactables
        area.interactionLayers = 1 << teleportInteractionLayer;

        // Assign reticle if available
        if (teleportReticlePrefab != null)
            area.teleportTrigger = BaseTeleportationInteractable.TeleportTrigger.OnDeactivated;

        _createdAreas.Add(area);
        Debug.Log($"[JANUS Teleport] Configured TeleportationArea on \"{floor.name}\" " +
                  $"(interaction layer {teleportInteractionLayer}).");
    }

    // ─────────────────────────────────────────────────────────────────────
    // Teleport Anchors (specific landing spots)
    // ─────────────────────────────────────────────────────────────────────

    private void CreateAnchors()
    {
        if (anchorPoints == null) return;

        foreach (var point in anchorPoints)
        {
            if (point == null) continue;

            var anchor = point.gameObject.GetComponent<TeleportationAnchor>();
            if (anchor == null)
                anchor = point.gameObject.AddComponent<TeleportationAnchor>();

            anchor.interactionLayers = 1 << teleportInteractionLayer;

            if (teleportReticlePrefab != null)
                anchor.teleportTrigger = BaseTeleportationInteractable.TeleportTrigger.OnDeactivated;

            _createdAnchors.Add(anchor);
            Debug.Log($"[JANUS Teleport] Anchor created at \"{point.name}\" {point.position}.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Pipeline Validation
    // ─────────────────────────────────────────────────────────────────────

    private void ValidatePipeline()
    {
        int issues = 0;

        // 1. TeleportationProvider must exist on the XR Origin rig
        var provider = GetComponentInChildren<TeleportationProvider>(true);
        if (provider == null)
        {
            Debug.LogError("[JANUS Teleport] ✗ No TeleportationProvider found on XR Origin children. " +
                           "The XRI Starter Assets prefab should include one. " +
                           "Add a TeleportationProvider component to the Locomotion System child.");
            issues++;
        }
        else
        {
            Debug.Log("[JANUS Teleport] ✓ TeleportationProvider found.");
        }

        // 2. Check for a teleport-capable interactor
        // In XRI 3.3.1, NearFarInteractor CAN do teleport if configured,
        // or there may be a separate XRRayInteractor for teleport
        var interactors = FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactors.NearFarInteractor>(
            FindObjectsSortMode.None);

        bool foundTeleportInteractor = false;
        foreach (var interactor in interactors)
        {
            // Check if any interactor's layer mask includes the teleport layer
            if ((interactor.interactionLayers & (1 << teleportInteractionLayer)) != 0)
            {
                foundTeleportInteractor = true;
                Debug.Log($"[JANUS Teleport] ✓ \"{interactor.name}\" has teleport layer enabled.");
            }
        }

        if (!foundTeleportInteractor)
        {
            Debug.LogWarning(
                "[JANUS Teleport] ⚠ No interactor has the Teleport interaction layer enabled.\n" +
                $"  → Select your NearFarInteractor(s) in the XR Origin rig\n" +
                $"  → In the Inspector, under Interaction Layers, enable layer {teleportInteractionLayer} (\"Teleport\")\n" +
                "  → OR add a dedicated teleport XRRayInteractor with that layer.\n" +
                "  Without this, the teleport ray cannot hit TeleportationArea surfaces.");
            issues++;
        }

        // 3. Check that floor areas actually have colliders
        int collidersOk = 0;
        foreach (var area in _createdAreas)
        {
            if (area != null && area.GetComponent<Collider>() != null)
                collidersOk++;
        }
        Debug.Log($"[JANUS Teleport] ✓ {collidersOk}/{_createdAreas.Count} teleport areas have colliders.");

        // 4. Summary
        if (issues == 0)
            Debug.Log("[JANUS Teleport] ✓ Pipeline validation passed — teleportation should work.");
        else
            Debug.LogWarning($"[JANUS Teleport] Pipeline validation found {issues} issue(s). See warnings above.");
    }

    // ─────────────────────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Call this after loading a new floor plan scene to re-scan for floors.
    /// E.g., subscribe to JANUSEvents.OnFloorPlanSelected.
    /// </summary>
    public void RescanFloors()
    {
        _createdAreas.Clear();
        _createdAnchors.Clear();

        var floors = FindFloorObjects();
        foreach (var floor in floors)
            ConfigureTeleportArea(floor);

        CreateAnchors();
        ValidatePipeline();

        Debug.Log($"[JANUS Teleport] Rescan complete: {_createdAreas.Count} areas, {_createdAnchors.Count} anchors.");
    }
}
