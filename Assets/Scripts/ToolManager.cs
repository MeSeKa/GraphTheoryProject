using System;
using UnityEngine;
using UnityEngine.UI;

public enum ToolType { Scissors, Axe, Bomb, Joker, Pickaxe }

public class ToolManager : MonoBehaviour
{
    [SerializeField] Button scissorsButton;
    [SerializeField] Button axeButton;
    [SerializeField] Button bombButton;

    public ToolType ActiveTool { get; private set; } = ToolType.Scissors;

    private void Start()
    {
        scissorsButton.onClick.AddListener(() => SelectTool(ToolType.Scissors));
        axeButton.onClick.AddListener(() => SelectTool(ToolType.Axe));
        bombButton.onClick.AddListener(() => SelectTool(ToolType.Bomb));
    }

    public bool CanCut(EdgeType edgeType) => ActiveTool switch
    {
        ToolType.Scissors => edgeType == EdgeType.Rope,
        ToolType.Axe      => edgeType == EdgeType.Wood,
        ToolType.Bomb     => edgeType == EdgeType.Stone,
        ToolType.Joker    => true,
        _                 => false
    };

    public void SetAvailableTools(ToolType[] tools)
    {
        scissorsButton.gameObject.SetActive(Array.IndexOf(tools, ToolType.Scissors) >= 0);
        axeButton.gameObject.SetActive(Array.IndexOf(tools, ToolType.Axe)           >= 0);
        bombButton.gameObject.SetActive(Array.IndexOf(tools, ToolType.Bomb)         >= 0);

        if (tools.Length > 0) SelectTool(tools[0]);
    }

    private void SelectTool(ToolType tool)
    {
        ActiveTool = tool;
        UpdateHighlights();
    }

    private void UpdateHighlights()
    {
        var normal = Color.white;
        var active = new Color(1f, 0.75f, 0.1f);
        SetButtonColor(scissorsButton, ActiveTool == ToolType.Scissors ? active : normal);
        SetButtonColor(axeButton,      ActiveTool == ToolType.Axe      ? active : normal);
        SetButtonColor(bombButton,     ActiveTool == ToolType.Bomb     ? active : normal);
    }

    private static void SetButtonColor(Button btn, Color color)
    {
        var cb = btn.colors;
        cb.normalColor = color;
        btn.colors = cb;
    }
}
