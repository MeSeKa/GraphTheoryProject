using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class LevelConfig
{
    public string levelName;

    [Header("Graph")]
    public int   nodeCount      = 6;
    public float spawnRadius    = 4f;
    public float edgeProbability = 0.55f;

    [Header("Mechanics")]
    public EdgeType[] allowedEdgeTypes;
    public ToolType[] availableTools;
}

public class LevelManager : MonoBehaviour
{
    [SerializeField] LevelConfig[]    levels;           // 3 entries configured in Inspector
    [SerializeField] GraphGenerator   graphGenerator;
    [SerializeField] SelectionManager selectionManager;
    [SerializeField] CameraFramer     cameraFramer;
    [SerializeField] ToolManager      toolManager;
    [SerializeField] GraphManager     graphManager;

    [Header("Panels")]
    [SerializeField] GameObject levelSelectPanel;
    [SerializeField] GameObject gameModePanel;

    [Header("UI")]
    [SerializeField] TMP_Text levelTitleText;
    [SerializeField] TMP_Text statusText;
    [SerializeField] TMP_Text winText;
    [SerializeField] Button   nextLevelButton;
    [SerializeField] Button   backToMenuButton;
    [SerializeField] Button[] levelButtons;             // one per level

    public GraphNode SourceNode      { get; private set; }
    public GraphNode DestinationNode { get; private set; }

    private int _currentLevel = -1;

    private void Start()
    {
        for (int i = 0; i < levelButtons.Length && i < levels.Length; i++)
        {
            int idx = i;
            levelButtons[i].onClick.AddListener(() => LoadLevel(idx));
        }

        nextLevelButton.onClick.AddListener(LoadNextLevel);
        backToMenuButton.onClick.AddListener(ExitGameMode);

        nextLevelButton.gameObject.SetActive(false);
        winText.gameObject.SetActive(false);
        gameModePanel.SetActive(false);
    }

    public void ShowLevelSelect()
    {
        levelSelectPanel.SetActive(true);
    }

    public void LoadLevel(int index)
    {
        _currentLevel = index;
        var cfg = levels[index];

        graphGenerator.nodeCount        = cfg.nodeCount;
        graphGenerator.spawnRadius      = cfg.spawnRadius;
        graphGenerator.edgeProbability  = cfg.edgeProbability;
        graphGenerator.allowedEdgeTypes = cfg.allowedEdgeTypes;
        graphGenerator.ensureConnectivity = true;

        (SourceNode, DestinationNode) = graphGenerator.GenerateGraph();
        cameraFramer.FrameAllNodes();

        SourceNode.SetMaterial(selectionManager.sourceNodeMaterial);
        DestinationNode.SetMaterial(selectionManager.destinationNodeMaterial);

        toolManager.SetAvailableTools(cfg.availableTools);

        if (levelTitleText) levelTitleText.text = cfg.levelName;
        SetStatus("Find the min-cut and save the sheep!");
        winText.gameObject.SetActive(false);
        nextLevelButton.gameObject.SetActive(false);
        levelSelectPanel.SetActive(false);
        gameModePanel.SetActive(true);

        graphManager.EnterGameMode();
    }

    public void CheckWinCondition()
    {
        if (SourceNode == null || DestinationNode == null) return;
        if (!Pathfinder.IsConnected(SourceNode, DestinationNode))
            OnWin();
    }

    private void OnWin()
    {
        winText.text = $"Level {_currentLevel + 1} Complete!\nThe sheep is safe!";
        winText.gameObject.SetActive(true);
        bool hasNext = _currentLevel + 1 < levels.Length;
        nextLevelButton.gameObject.SetActive(hasNext);
        SetStatus("You win!");
    }

    private void LoadNextLevel()
    {
        if (_currentLevel + 1 < levels.Length)
            LoadLevel(_currentLevel + 1);
    }

    private void ExitGameMode()
    {
        graphManager.ExitGameMode();
        SourceNode      = null;
        DestinationNode = null;
        gameModePanel.SetActive(false);
        levelSelectPanel.SetActive(true);
    }

    private void SetStatus(string msg)
    {
        if (statusText) statusText.text = msg;
    }
}
