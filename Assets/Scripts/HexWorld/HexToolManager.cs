using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HexToolManager : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] Button   scissorsButton;
    [SerializeField] Button   axeButton;
    [SerializeField] Button   bombButton;
    [SerializeField] Button   jokerButton;

    [Header("Count Labels")]
    [SerializeField] TMP_Text scissorsCountText;
    [SerializeField] TMP_Text axeCountText;
    [SerializeField] TMP_Text bombCountText;
    [SerializeField] TMP_Text jokerCountText;

    private int _scissors;
    private int _axe;
    private int _bomb;
    private int _joker;

    public ToolType ActiveTool { get; private set; } = ToolType.Scissors;

    private void Start()
    {
        scissorsButton.onClick.AddListener(() => SelectTool(ToolType.Scissors));
        axeButton.onClick.AddListener(()      => SelectTool(ToolType.Axe));
        bombButton.onClick.AddListener(()     => SelectTool(ToolType.Bomb));
        jokerButton.onClick.AddListener(()    => SelectTool(ToolType.Joker));
    }

    public void LoadInventory(HexLevelData data)
    {
        _scissors = data.scissorsCount;
        _axe      = data.axeCount;
        _bomb     = data.bombCount;
        _joker    = data.jokerCount;
        RefreshUI();
        AutoSelectFirst();
    }

    // Returns true if the active tool can legally cut this edge type.
    public bool CanCut(EdgeType edgeType)
    {
        if (ActiveTool == ToolType.Joker)
            return _joker > 0;
        return ToolMatchesEdge(ActiveTool, edgeType) && Remaining(ActiveTool) > 0;
    }

    public void ConsumeActiveTool()
    {
        switch (ActiveTool)
        {
            case ToolType.Scissors: _scissors--; break;
            case ToolType.Axe:      _axe--;      break;
            case ToolType.Bomb:     _bomb--;      break;
            case ToolType.Joker:    _joker--;     break;
        }
        RefreshUI();
        if (Remaining(ActiveTool) <= 0) AutoSelectFirst();
    }

    public bool AnyToolRemaining() =>
        _scissors > 0 || _axe > 0 || _bomb > 0 || _joker > 0;

    // ── Helpers ──

    private void SelectTool(ToolType tool)
    {
        if (Remaining(tool) <= 0) return;
        ActiveTool = tool;
        UpdateHighlights();
    }

    private void AutoSelectFirst()
    {
        foreach (ToolType t in new[] { ToolType.Scissors, ToolType.Axe, ToolType.Bomb, ToolType.Joker })
            if (Remaining(t) > 0) { ActiveTool = t; UpdateHighlights(); return; }
    }

    private int Remaining(ToolType t) => t switch
    {
        ToolType.Scissors => _scissors,
        ToolType.Axe      => _axe,
        ToolType.Bomb     => _bomb,
        ToolType.Joker    => _joker,
        _                 => 0
    };

    private static bool ToolMatchesEdge(ToolType tool, EdgeType edge) => tool switch
    {
        ToolType.Scissors => edge == EdgeType.Rope,
        ToolType.Axe      => edge == EdgeType.Wood,
        ToolType.Bomb     => edge == EdgeType.Stone,
        _                 => false
    };

    private void RefreshUI()
    {
        SetCount(scissorsCountText, _scissors);
        SetCount(axeCountText,      _axe);
        SetCount(bombCountText,     _bomb);
        SetCount(jokerCountText,    _joker);

        SetInteractable(scissorsButton, _scissors > 0);
        SetInteractable(axeButton,      _axe      > 0);
        SetInteractable(bombButton,     _bomb     > 0);
        SetInteractable(jokerButton,    _joker    > 0);
    }

    private void UpdateHighlights()
    {
        var normal = Color.white;
        var active = new Color(1f, 0.78f, 0.1f);
        SetButtonColor(scissorsButton, ActiveTool == ToolType.Scissors ? active : normal);
        SetButtonColor(axeButton,      ActiveTool == ToolType.Axe      ? active : normal);
        SetButtonColor(bombButton,     ActiveTool == ToolType.Bomb     ? active : normal);
        SetButtonColor(jokerButton,    ActiveTool == ToolType.Joker    ? active : normal);
    }

    private static void SetCount(TMP_Text label, int count)
    {
        if (label) label.text = count.ToString();
    }

    private static void SetInteractable(Button btn, bool on)
    {
        if (btn) btn.interactable = on;
    }

    private static void SetButtonColor(Button btn, Color col)
    {
        if (btn == null) return;
        var cb = btn.colors;
        cb.normalColor = col;
        btn.colors = cb;
    }
}
