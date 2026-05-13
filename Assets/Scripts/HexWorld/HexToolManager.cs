using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HexToolManager : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] Button axeButton;
    [SerializeField] Button pickaxeButton;
    [SerializeField] Button ironShearsButton;
    [SerializeField] Button bombButton;

    [Header("Count Labels")]
    [SerializeField] TMP_Text axeCountText;
    [SerializeField] TMP_Text pickaxeCountText;
    [SerializeField] TMP_Text ironShearsCountText;
    [SerializeField] TMP_Text bombCountText;

    [Header("Selection Colors")]
    [SerializeField] Color normalColor   = Color.white;
    [SerializeField] Color selectedColor = new Color(1f, 0.78f, 0.1f);

    private int _axe;
    private int _pickaxe;
    private int _ironShears;
    private int _bomb;

    public ToolType ActiveTool { get; private set; } = ToolType.Axe;

    private void Start()
    {
        axeButton?.onClick.AddListener(()         => SelectTool(ToolType.Axe));
        pickaxeButton?.onClick.AddListener(()     => SelectTool(ToolType.Pickaxe));
        ironShearsButton?.onClick.AddListener(()  => SelectTool(ToolType.IronShears));
        bombButton?.onClick.AddListener(()        => SelectTool(ToolType.Bomb));
        UpdateHighlights();
    }

    public void LoadInventory(HexLevelData data)
    {
        _axe        = data.axeCount;
        _pickaxe    = data.pickaxeCount;
        _ironShears = data.ironShearsCount;
        _bomb       = data.bombCount;
        RefreshUI();
        AutoSelectFirst();
    }

    // Returns true when the active (bridge-cutting) tool can cut this edge type.
    // Bomb is a node tool — it never cuts bridges directly.
    public bool CanCut(EdgeType edgeType)
    {
        if (ActiveTool == ToolType.Bomb) return false;
        return ToolMatchesEdge(ActiveTool, edgeType) && Remaining(ActiveTool) > 0;
    }

    // True when Bomb is selected and at least one remains.
    public bool IsBombActive() => ActiveTool == ToolType.Bomb && _bomb > 0;

    public void ConsumeActiveTool()
    {
        switch (ActiveTool)
        {
            case ToolType.Axe:        _axe--;        break;
            case ToolType.Pickaxe:    _pickaxe--;    break;
            case ToolType.IronShears: _ironShears--; break;
            case ToolType.Bomb:       _bomb--;        break;
        }
        RefreshUI();
        if (Remaining(ActiveTool) <= 0) AutoSelectFirst();
    }

    public bool AnyToolRemaining() => _axe > 0 || _pickaxe > 0 || _ironShears > 0 || _bomb > 0;

    public void AddTool(ToolType tool)
    {
        switch (tool)
        {
            case ToolType.Axe:        _axe++;        break;
            case ToolType.Pickaxe:    _pickaxe++;    break;
            case ToolType.IronShears: _ironShears++; break;
            case ToolType.Bomb:       _bomb++;        break;
        }
        RefreshUI();
    }

    // ── Helpers ──

    private void SelectTool(ToolType tool)
    {
        if (Remaining(tool) <= 0) return;
        ActiveTool = tool;
        UpdateHighlights();
    }

    private void AutoSelectFirst()
    {
        foreach (ToolType t in new[] { ToolType.Axe, ToolType.Pickaxe, ToolType.IronShears, ToolType.Bomb })
            if (Remaining(t) > 0) { ActiveTool = t; UpdateHighlights(); return; }
    }

    private int Remaining(ToolType t) => t switch
    {
        ToolType.Axe        => _axe,
        ToolType.Pickaxe    => _pickaxe,
        ToolType.IronShears => _ironShears,
        ToolType.Bomb       => _bomb,
        _                   => 0
    };

    private static bool ToolMatchesEdge(ToolType tool, EdgeType edge) => tool switch
    {
        ToolType.Axe        => edge == EdgeType.Wood,
        ToolType.Pickaxe    => edge == EdgeType.Stone,
        ToolType.IronShears => edge == EdgeType.Metal,
        _                   => false
    };

    private void RefreshUI()
    {
        SetCount(axeCountText,         _axe);
        SetCount(pickaxeCountText,     _pickaxe);
        SetCount(ironShearsCountText,  _ironShears);
        SetCount(bombCountText,        _bomb);

        SetInteractable(axeButton,         _axe        > 0);
        SetInteractable(pickaxeButton,     _pickaxe    > 0);
        SetInteractable(ironShearsButton,  _ironShears > 0);
        SetInteractable(bombButton,        _bomb       > 0);
    }

    private void UpdateHighlights()
    {
        SetButtonSelected(axeButton,        ActiveTool == ToolType.Axe);
        SetButtonSelected(pickaxeButton,    ActiveTool == ToolType.Pickaxe);
        SetButtonSelected(ironShearsButton, ActiveTool == ToolType.IronShears);
        SetButtonSelected(bombButton,       ActiveTool == ToolType.Bomb);
    }

    private static void SetCount(TMP_Text label, int count)
    {
        if (label) label.text = count.ToString();
    }

    private static void SetInteractable(Button btn, bool on)
    {
        if (btn) btn.interactable = on;
    }

    private void SetButtonSelected(Button btn, bool selected)
    {
        if (btn == null) return;
        if (btn.targetGraphic is Image img)
            img.color = selected ? selectedColor : normalColor;
        var sel = btn.transform.Find("Selected");
        if (sel != null) sel.gameObject.SetActive(selected);
    }
}
