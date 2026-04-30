using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// JANUS — Menu Setup
/// 
/// Written specifically for:
///   Unity 6000.3.10f1
///   XR Interaction Toolkit 3.3.1
///   OpenXR 1.16.1
///   New Input System 1.18.0
/// 
/// This project uses NearFarInteractor (XRI 3.x), NOT the old XRRayInteractor.
/// The XR Origin (XR Rig) prefab from Starter Assets already contains:
///   Left_NearFarInteractor  and  Right_NearFarInteractor
/// 
/// WHAT THIS SCRIPT DOES AUTOMATICALLY ON AWAKE:
///   1. Sets Canvas to World Space
///   2. Removes GraphicRaycaster, adds TrackedDeviceGraphicRaycaster
///   3. Replaces StandaloneInputModule with XRUIInputModule on EventSystem
///   4. Enables UI interaction on both NearFarInteractors
///   5. Adds BoxCollider for head-gaze fallback
///   6. Sizes and positions the canvas in front of the player
/// 
/// SETUP: Attach to your Canvas GameObject. Drag the two NearFarInteractor
/// components from your XR Origin (XR Rig) into the Inspector fields.
/// Right-click → "JANUS: Auto-find Interactors" to do this automatically.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class JANUSMenuSetup : MonoBehaviour
{
    [Header("Assign from XR Origin (XR Rig) → Left/Right Controller")]
    [Tooltip("Drag Left_NearFarInteractor from your XR Origin rig here.")]
    public NearFarInteractor LeftInteractor;

    [Tooltip("Drag Right_NearFarInteractor from your XR Origin rig here.")]
    public NearFarInteractor RightInteractor;

    [Header("Canvas Size")]
    [Tooltip("Width of the menu panel in world metres.")]
    public float WidthMetres  = 0.68f;
    public float HeightMetres = 0.60f;

    private void Awake()
    {
        ConfigureCanvas();
        ConfigureEventSystem();
        ConfigureInteractors();
        AddGazeCollider();
        Debug.Log("[JANUS] Setup complete — VR menu ready for XRI 3.3.1.");
    }

    // ── 1. Canvas ───────────────────────────────────────────────────────────

    void ConfigureCanvas()
    {
        var canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        var rt = canvas.GetComponent<RectTransform>();
        rt.sizeDelta  = new Vector2(WidthMetres * 1000f, HeightMetres * 1000f);
        rt.localScale = Vector3.one * 0.001f; // 1px = 1mm = 0.001m

        // Remove the standard raycaster — it cannot receive NearFarInteractor hits
        var old = GetComponent<UnityEngine.UI.GraphicRaycaster>();
        if (old != null) Destroy(old);

        // TrackedDeviceGraphicRaycaster routes XRI 3.x interactor hits to UI
        if (GetComponent<TrackedDeviceGraphicRaycaster>() == null)
            gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();

        Debug.Log("[JANUS] Canvas: World Space + TrackedDeviceGraphicRaycaster ✓");
    }

    // ── 2. EventSystem ──────────────────────────────────────────────────────

    void ConfigureEventSystem()
    {
        var es = FindFirstObjectByType<EventSystem>(); // Unity 6 API (replaces FindObjectOfType)
        if (es == null)
        {
            es = new GameObject("EventSystem").AddComponent<EventSystem>();
            Debug.Log("[JANUS] Created EventSystem.");
        }

        // StandaloneInputModule only handles mouse/keyboard — remove it
        var standalone = es.GetComponent<StandaloneInputModule>();
        if (standalone != null) Destroy(standalone);

        // XRUIInputModule bridges NearFarInteractor → EventSystem → UI Button
        if (es.GetComponent<XRUIInputModule>() == null)
        {
            es.gameObject.AddComponent<XRUIInputModule>();
            Debug.Log("[JANUS] XRUIInputModule added ✓");
        }
        else
        {
            Debug.Log("[JANUS] XRUIInputModule already present ✓");
        }
    }

    // ── 3. Interactors ──────────────────────────────────────────────────────

    void ConfigureInteractors()
    {
        // NearFarInteractor in XRI 3.x needs enableUIInteraction = true
        // to detect TrackedDeviceGraphicRaycaster hits
        SetUIInteraction(LeftInteractor,  "Left");
        SetUIInteraction(RightInteractor, "Right");
    }

    static void SetUIInteraction(NearFarInteractor interactor, string label)
    {
        if (interactor == null)
        {
            Debug.LogWarning($"[JANUS] {label} NearFarInteractor not assigned. " +
                             "Drag it from XR Origin (XR Rig) in the Inspector, " +
                             "or right-click JANUSMenuSetup → Auto-find Interactors.");
            return;
        }

        interactor.enableUIInteraction = true;
        Debug.Log($"[JANUS] {label} NearFarInteractor: UI interaction enabled ✓");
    }

    // ── 4. Gaze Collider ────────────────────────────────────────────────────

    void AddGazeCollider()
    {
        // TrackedDeviceGraphicRaycaster handles controller rays without a collider.
        // Head-gaze fallback (in JANUSVRInputHandler) uses Physics.Raycast,
        // which needs a collider on the canvas plane.
        var col = GetComponent<BoxCollider>();
        if (col == null) col = gameObject.AddComponent<BoxCollider>();

        col.isTrigger = true;
        col.size      = new Vector3(WidthMetres * 1000f, HeightMetres * 1000f, 2f);
        col.center    = Vector3.zero;
    }

    // ── 5. Placement helper ─────────────────────────────────────────────────

    /// <summary>Call this after the XR rig has initialised to place the menu.</summary>
    public void PlaceInFrontOfPlayer(float distanceMetres = 1.0f)
    {
        var cam = Camera.main;
        if (cam == null) return;

        var forward = new Vector3(cam.transform.forward.x, 0f, cam.transform.forward.z).normalized;
        transform.position = cam.transform.position + forward * distanceMetres + Vector3.up * -0.05f;
        transform.rotation = Quaternion.LookRotation(forward);
    }

    // ── Editor helper ───────────────────────────────────────────────────────

#if UNITY_EDITOR
    [ContextMenu("JANUS: Auto-find Interactors in Scene")]
    void AutoFindInteractors()
    {
        var all = FindObjectsByType<NearFarInteractor>(FindObjectsSortMode.None);
        foreach (var nfi in all)
        {
            var n = nfi.gameObject.name.ToLower();
            if (n.Contains("left")  && LeftInteractor  == null) LeftInteractor  = nfi;
            if (n.Contains("right") && RightInteractor == null) RightInteractor = nfi;
        }
        EditorUtility.SetDirty(this);
        Debug.Log($"[JANUS] Auto-found: Left={LeftInteractor?.name ?? "none"}, " +
                  $"Right={RightInteractor?.name ?? "none"}");
    }
#endif
}
