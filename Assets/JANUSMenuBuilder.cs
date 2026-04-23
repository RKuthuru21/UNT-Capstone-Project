using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// JANUS — Menu Builder (PDF replica — absolute-positioned)
///
/// Generates the complete JANUS menu UI at runtime using direct
/// RectTransform anchoring for every element — NO LayoutGroups,
/// NO ContentSizeFitters, NO nested flex boxes. Guarantees
/// pixel-perfect results.
///
/// ATTACH TO: the JANUS_Menu Canvas GameObject, alongside JANUSMenuManager.
/// Runs in Awake (before JANUSMenuManager.Start).
///
/// Coexists with JANUSMenuSetup (VR canvas config). This builder
/// FORCES the canvas, the setup's WidthMetres/HeightMetres, and the
/// BoxCollider to all match its own design dimensions so there's no
/// conflict between the two scripts.
/// </summary>
[RequireComponent(typeof(Canvas))]
[RequireComponent(typeof(JANUSMenuManager))]
public class JANUSMenuBuilder : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────
    // Canvas dimensions — 1200×825 @ scale 0.001 → 1.2m × 0.825m in VR
    // ─────────────────────────────────────────────────────────────────

    const float W           = 1200f;
    const float H           = 825f;
    const float ScaleFactor = 0.001f;

    const float HeaderH = 96f;
    const float ActionH = 84f;
    const float SidePad = 42f;

    // ─────────────────────────────────────────────────────────────────
    // Type sizes (scaled for 1200-wide canvas in VR)
    // ─────────────────────────────────────────────────────────────────

    const float BrandSize    = 42f;
    const float SubtitleSize = 16f;
    const float TagSize      = 12f;
    const float SectionSize  = 11f;
    const float StatLblSize  = 10f;
    const float StatValSize  = 20f;
    const float CardNameSize = 17f;
    const float CardDescSize = 11f;
    const float ModNumSize   = 12f;
    const float ModTitleSize = 17f;
    const float ModDescSize  = 11f;
    const float ModStatSize  = 12f;
    const float BtnSize      = 17f;

    // ─────────────────────────────────────────────────────────────────
    // Palette
    // ─────────────────────────────────────────────────────────────────

    static readonly Color HeaderBg      = new Color(0.941f, 0.925f, 0.878f, 1f);
    static readonly Color BodyBg        = new Color(0.992f, 0.988f, 0.976f, 1f);
    static readonly Color CardWhite     = new Color(1.000f, 1.000f, 1.000f, 1f);

    static readonly Color CardBeige     = new Color(0.890f, 0.855f, 0.800f, 1f);
    static readonly Color CardInner     = new Color(0.933f, 0.913f, 0.875f, 1f);
    static readonly Color SelectedBlue  = new Color(0.118f, 0.310f, 0.486f, 1f);
    static readonly Color SelectedInner = new Color(0.878f, 0.855f, 0.815f, 1f);

    static readonly Color TextDark      = new Color(0.094f, 0.094f, 0.094f, 1f);
    static readonly Color TextMuted     = new Color(0.380f, 0.372f, 0.356f, 1f);
    static readonly Color TextFaint     = new Color(0.560f, 0.548f, 0.529f, 1f);
    static readonly Color DividerLine   = new Color(0.839f, 0.824f, 0.800f, 1f);
    static readonly Color AccentGreen   = new Color(0.345f, 0.545f, 0.404f, 1f);

    static readonly Color BtnDark       = new Color(0.290f, 0.298f, 0.294f, 1f);
    static readonly Color BtnMid        = new Color(0.698f, 0.690f, 0.678f, 1f);
    static readonly Color BtnLight      = new Color(0.761f, 0.753f, 0.741f, 1f);
    static readonly Color BtnDarkText   = Color.white;
    static readonly Color BtnGrayText   = new Color(0.192f, 0.192f, 0.192f, 1f);

    // ─────────────────────────────────────────────────────────────────
    // Options
    // ─────────────────────────────────────────────────────────────────

    [Header("Options")]
    [Tooltip("Keep existing children under the Canvas. Unchecked wipes them.")]
    [SerializeField] private bool preserveExistingChildren = false;

    // ─────────────────────────────────────────────────────────────────
    // Refs
    // ─────────────────────────────────────────────────────────────────

    JANUSMenuManager _manager;
    TextMeshProUGUI _patientID, _sessionCounter, _elapsed, _status;
    JANUSMenuManager.FloorPlanCard[] _floorCards;
    JANUSMenuManager.ModuleRow[]     _moduleRows;
    Button _begin, _pause, _end;
    Image  _beginOutline;

    void Awake()
    {
        _manager    = GetComponent<JANUSMenuManager>();
        _floorCards = new JANUSMenuManager.FloorPlanCard[3];
        _moduleRows = new JANUSMenuManager.ModuleRow[3];

        // Force canvas, setup dimensions, and collider to all match. This
        // prevents JANUSMenuSetup from sizing the canvas smaller than our
        // design and causing content to spill outside the visible area.
        SyncDimensions();

        // Force full opacity on the canvas group so any scene-saved
        // alpha < 1 (leftover from fade hide/show) can't make the menu
        // render translucent.
        ForceOpaque();

        BuildUI();

        _manager.WireFromBuilder(
            _patientID, _sessionCounter, _elapsed,
            _floorCards, _moduleRows,
            _begin, _beginOutline, _pause, _end, _status);

        _manager.ApplyBuilderPalette(
            selected:      SelectedBlue,
            unselected:    CardBeige,
            beginActive:   BtnDark,
            beginInactive: new Color(0.470f, 0.470f, 0.470f, 1f),
            moduleActive:  new Color(SelectedBlue.r, SelectedBlue.g, SelectedBlue.b, 0.08f));
    }

    /// <summary>
    /// Force the Canvas rect, JANUSMenuSetup WidthMetres/HeightMetres, and
    /// BoxCollider to all agree with our design dimensions (W × H).
    /// </summary>
    void SyncDimensions()
    {
        // Canvas
        var canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rt = canvas.GetComponent<RectTransform>();
        rt.sizeDelta  = new Vector2(W, H);
        rt.localScale = Vector3.one * ScaleFactor;

        // Update JANUSMenuSetup so it doesn't fight us with its own numbers.
        var setup = GetComponent<JANUSMenuSetup>();
        if (setup != null)
        {
            setup.WidthMetres  = W * ScaleFactor;   // 1.2 m
            setup.HeightMetres = H * ScaleFactor;   // 0.825 m
        }

        // Collider for head-gaze fallback
        var col = GetComponent<BoxCollider>();
        if (col != null)
        {
            col.size   = new Vector3(W, H, 2f);
            col.center = Vector3.zero;
        }
    }

    /// <summary>
    /// Guarantees the menu renders fully opaque. Fixes the washed-out
    /// "see-through" look when the scene had a CanvasGroup alpha &lt; 1
    /// saved from a prior fade, or when a parent CanvasGroup is dimming
    /// this canvas. Also disables pixel-perfect (can cause artifacts in
    /// world space) and clears any material override on the Canvas.
    /// </summary>
    void ForceOpaque()
    {
        var cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
        cg.alpha              = 1f;
        cg.interactable       = true;
        cg.blocksRaycasts     = true;
        cg.ignoreParentGroups = true;  // don't let a parent fade us

        var canvas = GetComponent<Canvas>();
        canvas.pixelPerfect     = false;   // unreliable in world space
        canvas.overrideSorting  = false;
        canvas.additionalShaderChannels =
              AdditionalCanvasShaderChannels.TexCoord1
            | AdditionalCanvasShaderChannels.Normal
            | AdditionalCanvasShaderChannels.Tangent;
    }

    // ─────────────────────────────────────────────────────────────────
    // Main build
    // ─────────────────────────────────────────────────────────────────

    void BuildUI()
    {
        if (!preserveExistingChildren)
            for (int i = transform.childCount - 1; i >= 0; i--)
                DestroyImmediate(transform.GetChild(i).gameObject);

        // Root fills the canvas with body background (in case no panel covers it)
        var root = Panel("Root", transform, BodyBg);
        FillParent(root);

        BuildHeader(root.transform);
        BuildBody(root.transform);
        BuildActionBar(root.transform);
    }

    // ─────────────────────────────────────────────────────────────────
    // HEADER (top tan bar — brand + tags)
    // ─────────────────────────────────────────────────────────────────

    void BuildHeader(Transform parent)
    {
        var header = Panel("Header", parent, HeaderBg);
        var rt = header.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0f, HeaderH);

        // JANUS wordmark (big bold, left)
        PlaceText(header.transform, "JANUS",
            SidePad, 18f, 220f, 58f,
            BrandSize, FontWeight.Bold, TextDark, TextAlignmentOptions.MidlineLeft);

        // Assessment System subtitle (right of brand)
        PlaceText(header.transform, "Assessment System",
            SidePad + 190f, 40f, 260f, 24f,
            SubtitleSize, FontWeight.Regular, TextMuted, TextAlignmentOptions.MidlineLeft);

        // Status tags (right side, right-aligned)
        _status = PlaceText(header.transform, "Headset",
            W - 440f, 40f, 90f, 24f,
            TagSize, FontWeight.Regular, TextMuted, TextAlignmentOptions.MidlineRight);

        PlaceText(header.transform, "Tracking",
            W - 330f, 40f, 90f, 24f,
            TagSize, FontWeight.Regular, TextMuted, TextAlignmentOptions.MidlineRight);

        PlaceText(header.transform, "L. Controller 62%",
            W - 220f, 40f, 180f, 24f,
            TagSize, FontWeight.Regular, TextMuted, TextAlignmentOptions.MidlineRight);
    }

    // ─────────────────────────────────────────────────────────────────
    // BODY (middle — sessions / floor plans / modules)
    // ─────────────────────────────────────────────────────────────────

    void BuildBody(Transform parent)
    {
        var body = Panel("Body", parent, BodyBg);
        var rt = body.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.offsetMin = new Vector2(0f, ActionH);
        rt.offsetMax = new Vector2(0f, -HeaderH);

        // Body usable: W × (H - HeaderH - ActionH) = 1200 × 645
        float contentW = W - 2 * SidePad;
        float y = 18f;

        SectionLabel(body.transform, "SESSION", SidePad, y);
        y += 24f;

        BuildStatBar(body.transform, SidePad, y, contentW, 68f);
        y += 68f + 22f;

        SectionLabel(body.transform, "ENVIRONMENT — FLOOR PLAN", SidePad, y);
        y += 24f;

        BuildFloorPlanRow(body.transform, SidePad, y, contentW, 160f);
        y += 160f + 22f;

        SectionLabel(body.transform, "ASSESSMENT MODULES", SidePad, y);
        y += 26f;

        BuildModulesList(body.transform, SidePad, y, contentW);
    }

    // ─── STAT BAR (3 columns in a single white card) ───────────────

    void BuildStatBar(Transform parent, float x, float y, float width, float height)
    {
        var bar = Panel("StatBar", parent, CardWhite);
        PlaceRect(bar, x, y, width, height);

        float colW = width / 3f;
        _patientID      = BuildStatCol(bar.transform, 0f * colW, colW, height, "PATIENT",  "—",              divider: false);
        _sessionCounter = BuildStatCol(bar.transform, 1f * colW, colW, height, "PROGRESS", "Session 0 of 0", divider: true);
        _elapsed        = BuildStatCol(bar.transform, 2f * colW, colW, height, "ELAPSED",  "00:00",          divider: true);
    }

    TextMeshProUGUI BuildStatCol(Transform parent, float x, float w, float h,
        string label, string value, bool divider)
    {
        var col = Panel("Col_" + label, parent, Color.clear);
        PlaceRect(col, x, 0f, w, h);

        if (divider)
        {
            var div = Panel("Divider", col.transform, DividerLine);
            PlaceRect(div, 0f, h * 0.22f, 1.2f, h * 0.56f);
        }

        var lbl = PlaceText(col.transform, label,
            22f, 12f, w - 44f, 16f,
            StatLblSize, FontWeight.Medium, TextFaint, TextAlignmentOptions.MidlineLeft);
        lbl.characterSpacing = 15f;

        return PlaceText(col.transform, value,
            22f, 32f, w - 44f, 28f,
            StatValSize, FontWeight.SemiBold, TextDark, TextAlignmentOptions.MidlineLeft);
    }

    // ─── FLOOR PLAN ROW (3 cards) ──────────────────────────────────

    void BuildFloorPlanRow(Transform parent, float x, float y, float width, float height)
    {
        const float gap = 14f;
        float cardW = (width - 2f * gap) / 3f;

        string[] names = { "Layout A", "Layout B", "Layout C" };
        string[] descs = { "3 rooms · Standard", "4 rooms · Extended", "5 rooms · Complex" };

        for (int i = 0; i < 3; i++)
            _floorCards[i] = BuildFloorCard(parent, i,
                x + i * (cardW + gap), y, cardW, height,
                names[i], descs[i], selected: i == 0);
    }

    JANUSMenuManager.FloorPlanCard BuildFloorCard(Transform parent, int idx,
        float x, float y, float w, float h, string name, string desc, bool selected)
    {
        var card = Panel("Card_FloorPlan_" + idx, parent, selected ? SelectedBlue : CardBeige);
        PlaceRect(card, x, y, w, h);

        const float pad = 12f;
        const float footerH = 56f;

        // Thumbnail area
        var thumb = Panel("Thumbnail", card.transform, selected ? SelectedInner : CardInner);
        PlaceRect(thumb, pad, pad, w - 2f * pad, h - footerH - pad);

        // Checkmark (top-right of thumbnail) — GameObject that manager toggles
        var check = PlaceText(thumb.transform, "✓",
            0f, 0f, 24f, 24f,
            18f, FontWeight.Bold, TextDark, TextAlignmentOptions.Center);
        var checkRT = check.GetComponent<RectTransform>();
        checkRT.anchorMin        = new Vector2(1f, 1f);
        checkRT.anchorMax        = new Vector2(1f, 1f);
        checkRT.pivot            = new Vector2(1f, 1f);
        checkRT.anchoredPosition = new Vector2(-4f, -4f);
        check.gameObject.SetActive(selected);

        // Card name
        var nameTxt = PlaceText(card.transform, name,
            pad + 4f, h - footerH + 4f, w - 2f * pad - 8f, 24f,
            CardNameSize, FontWeight.SemiBold, TextDark, TextAlignmentOptions.MidlineLeft);

        // Card description
        var descTxt = PlaceText(card.transform, desc,
            pad + 4f, h - footerH + 30f, w - 2f * pad - 8f, 18f,
            CardDescSize, FontWeight.Regular, selected ? TextMuted : TextFaint, TextAlignmentOptions.MidlineLeft);

        // Click target on outer card
        var btn = card.AddComponent<Button>();
        btn.targetGraphic = card.GetComponent<Image>();
        btn.transition    = Selectable.Transition.ColorTint;
        var colors = btn.colors;
        colors.normalColor      = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
        colors.pressedColor     = new Color(1f, 1f, 1f, 0.85f);
        colors.selectedColor    = Color.white;
        btn.colors = colors;

        return new JANUSMenuManager.FloorPlanCard
        {
            CardButton   = btn,
            OutlineImage = card.GetComponent<Image>(),
            CheckMark    = check.gameObject,
            NameText     = nameTxt,
            DescText     = descTxt,
        };
    }

    // ─── MODULE LIST (3 rows, clean) ───────────────────────────────

    void BuildModulesList(Transform parent, float x, float y, float width)
    {
        string[] nums     = { "01", "02", "03" };
        string[] titles   = { "Spatial Navigation", "Attention & Focus", "Memory Recall" };
        string[] descs    = {
            "Orientation and wayfinding within selected layout",
            "Sustained attention protocol",
            "Short and long-term encoding tasks",
        };
        string[] statuses = { "In progress", "Complete", "Pending" };
        Color[]  statCols = { TextMuted, AccentGreen, TextFaint };
        string[] ids      = { "spatial_nav", "attention", "memory_recall" };

        const float rowH = 58f;
        for (int i = 0; i < 3; i++)
            _moduleRows[i] = BuildModuleRow(parent,
                x, y + i * rowH, width, rowH,
                nums[i], titles[i], descs[i], statuses[i], statCols[i], ids[i]);
    }

    JANUSMenuManager.ModuleRow BuildModuleRow(Transform parent, float x, float y, float w, float h,
        string num, string title, string desc, string status, Color statCol, string id)
    {
        var row = Panel("Row_Module_" + id, parent, Color.clear);
        PlaceRect(row, x, y, w, h);

        // Number at left
        PlaceText(row.transform, num,
            8f, 0f, 36f, h,
            ModNumSize, FontWeight.Regular, TextFaint, TextAlignmentOptions.Center);

        // Title (upper half)
        var titleTxt = PlaceText(row.transform, title,
            60f, 10f, w - 200f, 24f,
            ModTitleSize, FontWeight.SemiBold, TextDark, TextAlignmentOptions.BottomLeft);

        // Description (lower half)
        PlaceText(row.transform, desc,
            60f, 32f, w - 200f, 20f,
            ModDescSize, FontWeight.Regular, TextFaint, TextAlignmentOptions.TopLeft);

        // Status (right-aligned)
        var statusTxt = PlaceText(row.transform, status,
            w - 150f, 0f, 138f, h,
            ModStatSize, FontWeight.Medium, statCol, TextAlignmentOptions.MidlineRight);

        // Transparent click button on full row
        var btn = row.AddComponent<Button>();
        var img = row.GetComponent<Image>();
        btn.targetGraphic = img;
        btn.transition    = Selectable.Transition.ColorTint;
        var colors = btn.colors;
        colors.normalColor      = new Color(1f, 1f, 1f, 0.001f);
        colors.highlightedColor = new Color(0f, 0f, 0f, 0.04f);
        colors.pressedColor     = new Color(0f, 0f, 0f, 0.08f);
        colors.selectedColor    = new Color(1f, 1f, 1f, 0.001f);
        btn.colors = colors;

        return new JANUSMenuManager.ModuleRow
        {
            RowButton  = btn,
            ModuleID   = id,
            LabelText  = titleTxt,
            StatusText = statusTxt,
            Background = img,
        };
    }

    // ─────────────────────────────────────────────────────────────────
    // ACTION BAR (bottom 3-button strip)
    // ─────────────────────────────────────────────────────────────────

    void BuildActionBar(Transform parent)
    {
        var bar = new GameObject("ActionBar", typeof(RectTransform));
        bar.transform.SetParent(parent, false);
        var rt = bar.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot     = new Vector2(0.5f, 0f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0f, ActionH);

        float btnW = W / 3f;

        (_begin, _beginOutline) = BuildSolidButton(bar.transform, "Btn_Begin", "Begin Module",
            0f * btnW, 0f, btnW, ActionH, BtnDark,  BtnDarkText, primary: true);
        (_pause, _)             = BuildSolidButton(bar.transform, "Btn_Pause", "Pause",
            1f * btnW, 0f, btnW, ActionH, BtnMid,   BtnGrayText, primary: false);
        (_end,   _)             = BuildSolidButton(bar.transform, "Btn_End",   "End Session",
            2f * btnW, 0f, btnW, ActionH, BtnLight, BtnGrayText, primary: false);
    }

    (Button, Image) BuildSolidButton(Transform parent, string name, string label,
        float x, float y, float w, float h, Color fill, Color textColor, bool primary)
    {
        var btnGO = Panel(name, parent, fill);
        PlaceRect(btnGO, x, y, w, h);

        PlaceText(btnGO.transform, label,
            0f, 0f, w, h,
            BtnSize, primary ? FontWeight.SemiBold : FontWeight.Medium,
            textColor, TextAlignmentOptions.Center);

        var btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = btnGO.GetComponent<Image>();
        btn.transition    = Selectable.Transition.ColorTint;
        var colors = btn.colors;
        float d = primary ? 0.18f : 0.08f;
        colors.normalColor      = Color.white;
        colors.highlightedColor = new Color(1f - d,       1f - d,       1f - d,       1f);
        colors.pressedColor     = new Color(1f - d * 1.5f, 1f - d * 1.5f, 1f - d * 1.5f, 1f);
        colors.selectedColor    = Color.white;
        btn.colors = colors;

        return (btn, btnGO.GetComponent<Image>());
    }

    // ─────────────────────────────────────────────────────────────────
    // Section label
    // ─────────────────────────────────────────────────────────────────

    void SectionLabel(Transform parent, string text, float x, float y)
    {
        var lbl = PlaceText(parent, text,
            x, y, 500f, 22f,
            SectionSize, FontWeight.Medium, TextFaint, TextAlignmentOptions.MidlineLeft);
        lbl.characterSpacing = 15f;
    }

    // ─────────────────────────────────────────────────────────────────
    // Primitives
    // ─────────────────────────────────────────────────────────────────

    GameObject Panel(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return go;
    }

    TextMeshProUGUI PlaceText(Transform parent, string text,
        float x, float y, float w, float h,
        float size, FontWeight weight, Color color, TextAlignmentOptions align)
    {
        var go = new GameObject("Text_" + SafeName(text),
            typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        PlaceRect(go, x, y, w, h);

        var t = go.GetComponent<TextMeshProUGUI>();
        t.text               = text;
        t.fontSize           = size;
        t.fontWeight         = weight;
        t.color              = color;
        t.alignment          = align;
        t.raycastTarget      = false;
        t.enableWordWrapping = false;
        t.overflowMode       = TextOverflowModes.Overflow;
        return t;
    }

    static string SafeName(string s)
    {
        if (string.IsNullOrEmpty(s)) return "T";
        s = s.Length > 20 ? s.Substring(0, 20) : s;
        return s.Replace(" ", "_")
                .Replace(".", "")
                .Replace("·", "")
                .Replace("—", "-")
                .Replace("%", "")
                .Replace("&", "and");
    }

    /// <summary>Places a GameObject at (x, y) from TOP-LEFT of parent. Y is positive-downward.</summary>
    static void PlaceRect(GameObject go, float x, float y, float w, float h)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(0f, 1f);
        rt.pivot            = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta        = new Vector2(w, h);
    }

    static void FillParent(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
