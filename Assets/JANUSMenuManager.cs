using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// JANUS — Menu Manager
///
/// Core controller for the JANUS assessment menu. Manages:
///   • Patient info display (ID, session counter)
///   • Floor plan selection (3-card grid)
///   • Module list with selection highlight
///   • Session state machine (Idle / Running / Paused / Ended)
///   • Hardware status bar
///   • Menu visibility and world-space placement
///
/// ATTACH TO: The JANUS_Menu Canvas GameObject.
/// Requires: JANUSMenuSetup, JANUSVRInputHandler, JANUSHardwareMonitor
///           on the same GameObject.
///
/// ── UI HIERARCHY EXPECTED ────────────────────────────────────────────────
/// JANUS_Menu (Canvas)
///   Panel_Root
///     Header
///       Text_Title          ← "JANUS" wordmark
///       Text_PatientID
///       Text_SessionCounter
///     Section_FloorPlans
///       Card_FloorPlan_0
///       Card_FloorPlan_1
///       Card_FloorPlan_2
///     Section_Modules
///       Row_Module_0 … Row_Module_N
///     Footer_Status
///       Text_HardwareStatus
///       Btn_Pause
///       Btn_End
///
/// You can build this hierarchy manually or use the Unity UI builder.
/// All references below are optional — the script degrades gracefully
/// if a field is null (useful during iterative UI development).
/// </summary>
[RequireComponent(typeof(Canvas))]
[RequireComponent(typeof(CanvasGroup))]
public class JANUSMenuManager : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────
    // Inspector — Patient info
    // ─────────────────────────────────────────────────────────────────────

    [Header("Patient Info")]
    [SerializeField] private TextMeshProUGUI patientIDText;
    [SerializeField] private TextMeshProUGUI sessionCounterText;
    [SerializeField] private TextMeshProUGUI elapsedTimeText;

    // ─────────────────────────────────────────────────────────────────────
    // Inspector — Floor Plan Cards
    // ─────────────────────────────────────────────────────────────────────

    [Header("Floor Plan Selection")]
    [Tooltip("Assign one FloorPlanData ScriptableObject per card. Order matches card 0, 1, 2.")]
    [SerializeField] private FloorPlanData[] floorPlans = new FloorPlanData[3];

    [Tooltip("The card root GameObjects (each needs a Button, outline Image, and checkmark Image child).")]
    [SerializeField] private FloorPlanCard[] floorPlanCards = new FloorPlanCard[3];

    [System.Serializable]
    public class FloorPlanCard
    {
        public Button    CardButton;
        public Image     OutlineImage;
        public GameObject CheckMark;
        public TextMeshProUGUI NameText;
        public TextMeshProUGUI DescText;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Inspector — Module Rows
    // ─────────────────────────────────────────────────────────────────────

    [Header("Assessment Modules")]
    [SerializeField] private ModuleRow[] moduleRows;

    [System.Serializable]
    public class ModuleRow
    {
        public Button    RowButton;
        public string    ModuleID;
        public TextMeshProUGUI LabelText;
        public TextMeshProUGUI StatusText;  // "In progress" / "Complete" / "Pending"
        public Image     Background;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Inspector — Session Controls
    // ─────────────────────────────────────────────────────────────────────

    [Header("Session Controls")]
    [SerializeField] private Button beginButton;
    [SerializeField] private Image  beginOutline;   // thick border when active (PDF look)
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button endButton;
    [SerializeField] private TextMeshProUGUI statusText;

    // ─────────────────────────────────────────────────────────────────────
    // Inspector — Colours (minimalist palette)
    // ─────────────────────────────────────────────────────────────────────

    [Header("Colours")]
    [Tooltip("Floor plan card fill when selected (matches JANUSMenuBuilder.SelectedBlue).")]
    [SerializeField] private Color colSelected    = new Color(0.118f, 0.310f, 0.486f, 1f); // #1E4F7C deep blue
    [Tooltip("Floor plan card fill when unselected (matches JANUSMenuBuilder.CardBeige).")]
    [SerializeField] private Color colUnselected  = new Color(0.890f, 0.855f, 0.800f, 1f); // #E3DACC warm beige

    [Tooltip("Begin Module button fill when the player hasn't picked a module yet.")]
    [SerializeField] private Color colBeginInactive = new Color(0.470f, 0.470f, 0.470f, 1f); // dimmed
    [Tooltip("Begin Module button fill when ready to start (module picked, idle state).")]
    [SerializeField] private Color colBeginActive   = new Color(0.290f, 0.298f, 0.294f, 1f); // BtnDark

    [Tooltip("Module row tint when that module is the currently-selected one.")]
    [SerializeField] private Color colModuleActive  = new Color(0.118f, 0.310f, 0.486f, 0.08f); // faint blue wash

    [SerializeField] private Color colHwOK        = new Color(0.24f, 0.48f, 0.35f, 1f); // #3D7A5A
    [SerializeField] private Color colHwWarn       = new Color(0.60f, 0.42f, 0.16f, 1f); // #9A6B2A
    [SerializeField] private Color colHwCritical   = new Color(0.72f, 0.18f, 0.18f, 1f); // #B82E2E

    // ─────────────────────────────────────────────────────────────────────
    // Private state
    // ─────────────────────────────────────────────────────────────────────

    private enum SessionState { Idle, Running, Paused, Ended }
    private SessionState _state = SessionState.Idle;

    private string  _patientID      = "—";
    private int     _currentSession = 0;
    private int     _totalSessions  = 0;
    private int     _selectedFloor  = 0;
    private string  _selectedModule = "";
    private float   _sessionStart;
    private float   _elapsedSeconds;

    private JANUSHardwareMonitor _hw;
    private Canvas               _canvas;
    private CanvasGroup          _canvasGroup;
    private bool                 _visible;

    // ─────────────────────────────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _canvas      = GetComponent<Canvas>();
        _canvasGroup = GetComponent<CanvasGroup>();
        _hw          = GetComponent<JANUSHardwareMonitor>();
    }

    private void Start()
    {
        WireButtons();
        RefreshFloorCards();
        RefreshModuleRows();
        RefreshPatientDisplay();
        RefreshSessionControls();

        if (_hw != null)
            _hw.OnStatusChanged += OnHardwareStatusChanged;

        // Start hidden — shown when clinician presses Menu button
        SetVisible(true);
    }

    private void OnDestroy()
    {
        if (_hw != null)
            _hw.OnStatusChanged -= OnHardwareStatusChanged;
    }

    private void Update()
    {
        if (_state == SessionState.Running)
            _elapsedSeconds += Time.deltaTime;

        if (elapsedTimeText != null)
        {
            int total = Mathf.FloorToInt(_elapsedSeconds);
            elapsedTimeText.text = $"{total / 60:00}:{total % 60:00}";
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Button wiring
    // ─────────────────────────────────────────────────────────────────────

    private void WireButtons()
    {
        // Floor plan cards
        for (int i = 0; i < floorPlanCards.Length; i++)
        {
            int idx = i; // capture for lambda
            if (floorPlanCards[i]?.CardButton != null)
                floorPlanCards[i].CardButton.onClick.AddListener(() => SelectFloor(idx));
        }

        // Module rows
        foreach (var row in moduleRows)
        {
            if (row?.RowButton == null) continue;
            string id = row.ModuleID;
            row.RowButton.onClick.AddListener(() => SelectModule(id));
        }

        // Session controls
        if (beginButton != null) beginButton.onClick.AddListener(OnBeginPressed);
        if (pauseButton != null) pauseButton.onClick.AddListener(OnPausePressed);
        if (endButton   != null) endButton.onClick.AddListener(OnEndPressed);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Auto-wiring from JANUSMenuBuilder (PDF-style UI generator)
    // ─────────────────────────────────────────────────────────────────────

    public void WireFromBuilder(
        TextMeshProUGUI patientID, TextMeshProUGUI sessionCounter, TextMeshProUGUI elapsed,
        FloorPlanCard[] cards, ModuleRow[] modules,
        Button begin, Image beginOutlineImg,
        Button pause, Button end, TextMeshProUGUI status)
    {
        patientIDText      = patientID;
        sessionCounterText = sessionCounter;
        elapsedTimeText    = elapsed;
        floorPlanCards     = cards;
        moduleRows         = modules;
        beginButton        = begin;
        beginOutline       = beginOutlineImg;
        pauseButton        = pause;
        endButton          = end;
        statusText         = status;
    }

    /// <summary>
    /// Override the serialized palette with the builder's values. Call this
    /// from JANUSMenuBuilder.Awake so the runtime colors match the design
    /// even if the component had older values saved in the scene/prefab.
    /// </summary>
    public void ApplyBuilderPalette(Color selected, Color unselected,
                                     Color beginActive, Color beginInactive,
                                     Color moduleActive)
    {
        colSelected      = selected;
        colUnselected    = unselected;
        colBeginActive   = beginActive;
        colBeginInactive = beginInactive;
        colModuleActive  = moduleActive;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Public API — Patient / Session
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>Load patient data into the menu display.</summary>
    public void LoadPatient(string patientID, int currentSession, int totalSessions)
    {
        _patientID      = patientID;
        _currentSession = currentSession;
        _totalSessions  = totalSessions;
        _state          = SessionState.Idle;
        RefreshPatientDisplay();
        RefreshSessionControls();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Public API — Floor Plan
    // ─────────────────────────────────────────────────────────────────────

    public void SelectFloor(int index)
    {
        if (index < 0 || index >= floorPlans.Length) return;
        _selectedFloor = index;
        RefreshFloorCards();

        if (floorPlans[index] != null)
            JANUSEvents.OnFloorPlanSelected?.Invoke(floorPlans[index]);
    }

    public FloorPlanData GetSelectedFloor() =>
        (_selectedFloor >= 0 && _selectedFloor < floorPlans.Length)
            ? floorPlans[_selectedFloor] : null;

    // ─────────────────────────────────────────────────────────────────────
    // Public API — Module
    // ─────────────────────────────────────────────────────────────────────

    public void SelectModule(string moduleID)
    {
        _selectedModule = moduleID;
        RefreshModuleRows();
        JANUSEvents.OnModuleSelected?.Invoke(moduleID);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Public API — Visibility
    // ─────────────────────────────────────────────────────────────────────

    public void SetVisible(bool visible)
    {
        _visible = visible;

        // Hide via Canvas + CanvasGroup, NOT SetActive — keeps
        // JANUSVRInputHandler alive so the Menu button still works.
        _canvas.enabled             = visible;
        _canvasGroup.alpha          = visible ? 1f : 0f;
        _canvasGroup.interactable   = visible;
        _canvasGroup.blocksRaycasts = visible;

        if (visible) JANUSEvents.OnMenuOpened?.Invoke();
        else         JANUSEvents.OnMenuClosed?.Invoke();
    }

    public void ToggleVisible() => SetVisible(!_visible);

    /// <summary>
    /// Positions the menu in front of the player's current view.
    /// Call after the XR rig has initialised.
    /// </summary>
    public void PlaceInFrontOfPlayer(float distance = 1.0f)
    {
        var cam = Camera.main;
        if (cam == null) return;
        var fwd = new Vector3(cam.transform.forward.x, 0f, cam.transform.forward.z).normalized;
        transform.position = cam.transform.position + fwd * distance + Vector3.up * -0.05f;
        transform.rotation = Quaternion.LookRotation(fwd);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Session button handlers
    // ─────────────────────────────────────────────────────────────────────

    private void OnBeginPressed()
    {
        if (_state == SessionState.Idle && !string.IsNullOrEmpty(_selectedModule))
        {
            _state          = SessionState.Running;
            _sessionStart   = Time.time;
            _elapsedSeconds = 0f;
            JANUSEvents.OnModuleBegin?.Invoke(_selectedModule);
            RefreshSessionControls();
        }
    }

    private void OnPausePressed()
    {
        if (_state == SessionState.Running)
        {
            _state = SessionState.Paused;
            JANUSEvents.OnSessionPaused?.Invoke();
        }
        else if (_state == SessionState.Paused)
        {
            _state = SessionState.Running;
            JANUSEvents.OnSessionResumed?.Invoke();
        }
        RefreshSessionControls();
    }

    private void OnEndPressed()
    {
        float duration = _state == SessionState.Running ? Time.time - _sessionStart : 0f;
        _state = SessionState.Ended;
        JANUSEvents.OnSessionEnded?.Invoke(_patientID, duration);
        RefreshSessionControls();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Hardware status callback
    // ─────────────────────────────────────────────────────────────────────

    private void OnHardwareStatusChanged(JANUSHardwareMonitor.HardwareStatus status)
    {
        if (statusText == null) return;

        statusText.text = _hw.GetStatusSummary();
        statusText.color = status switch
        {
            JANUSHardwareMonitor.HardwareStatus.Critical => colHwCritical,
            JANUSHardwareMonitor.HardwareStatus.Warn     => colHwWarn,
            _                                            => colHwOK,
        };
    }

    // ─────────────────────────────────────────────────────────────────────
    // UI refresh helpers
    // ─────────────────────────────────────────────────────────────────────

    private void RefreshPatientDisplay()
    {
        if (patientIDText      != null) patientIDText.text      = _patientID;
        if (sessionCounterText != null) sessionCounterText.text = $"Session {_currentSession} of {_totalSessions}";
    }

    private void RefreshFloorCards()
    {
        for (int i = 0; i < floorPlanCards.Length; i++)
        {
            var card = floorPlanCards[i];
            if (card == null) continue;

            bool selected = i == _selectedFloor;

            if (card.OutlineImage != null)
                card.OutlineImage.color = selected ? colSelected : colUnselected;

            if (card.CheckMark != null)
                card.CheckMark.SetActive(selected);

            // Populate name/desc from ScriptableObject if available
            if (i < floorPlans.Length && floorPlans[i] != null)
            {
                if (card.NameText != null) card.NameText.text = floorPlans[i].LayoutName;
                if (card.DescText != null) card.DescText.text = floorPlans[i].Description;
            }
        }
    }

    private void RefreshModuleRows()
    {
        foreach (var row in moduleRows)
        {
            if (row == null) continue;
            bool active = row.ModuleID == _selectedModule;
            if (row.Background != null)
                row.Background.color = active ? colModuleActive : Color.clear;
        }
    }

    private void RefreshSessionControls()
    {
        bool canBegin = _state == SessionState.Idle && !string.IsNullOrEmpty(_selectedModule);

        if (beginButton != null) beginButton.interactable = canBegin;
        if (beginOutline != null)
            beginOutline.color = canBegin ? colBeginActive : colBeginInactive;

        if (pauseButton != null)
        {
            var label = pauseButton.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = _state == SessionState.Paused ? "Resume" : "Pause";
            pauseButton.interactable = _state == SessionState.Running || _state == SessionState.Paused;
        }

        if (endButton != null)
            endButton.interactable = _state == SessionState.Running || _state == SessionState.Paused;
    }
}
