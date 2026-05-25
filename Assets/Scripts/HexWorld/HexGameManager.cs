using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class HexGameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] HexLevelLoader  levelLoader;
    [SerializeField] HexToolManager  toolManager;
    [SerializeField] HexShopManager  shopManager;
    [SerializeField] IsometricCamera isoCamera;

    [Header("Level Data")]
    [SerializeField] HexLevelData[]  levels;
    [SerializeField] int             startLevelIndex = 0;

    [Header("Materials — Tiles")]
    [SerializeField] Material normalTileMaterial;

    [Header("Materials — Bridges")]
    [SerializeField] public Material woodBridgeMaterial;
    [SerializeField] public Material stoneBridgeMaterial;
    [SerializeField] public Material metalBridgeMaterial;
    [SerializeField] public Material errorBridgeMaterial;
    [SerializeField] public Material destroyedBridgeMaterial;
    [SerializeField] public Material unbreakableBridgeMaterial;

    [Header("UI")]
    [SerializeField] TMP_Text  levelNameText;
    [SerializeField] TMP_Text  cutsUsedText;
    [SerializeField] TMP_Text  statusText;
    [SerializeField] GameObject winPanel;
    [SerializeField] GameObject losePanel;
    [SerializeField] Button     nextLevelButton;
    [SerializeField] Button     retryButton;
    [SerializeField] Button     retryButtonWin;
    [SerializeField] TMP_Text   starText;        // "⭐⭐⭐" göstermek için

    private int              _currentLevelIndex;
    private int              _cutsUsed;
    private int              _levelStartingGold;
    private List<HexTile>   _tiles   = new();
    private List<HexBridge> _bridges = new();
    private bool             _gameActive;

    private HexBridge _hoveredBridge;
    private HexTile   _hoveredTile;

    private void Start()
    {
        if (nextLevelButton)  nextLevelButton.onClick.AddListener(LoadNextLevel);
        if (retryButton)      retryButton.onClick.AddListener(RetryLevel);
        if (retryButtonWin)   retryButtonWin.onClick.AddListener(RetryLevel);

        winPanel?.SetActive(false);
        losePanel?.SetActive(false);

        toolManager.OnToolChanged += _ => RefreshIndicators();

        if (levels != null && levels.Length > 0)
            LoadLevel(startLevelIndex);
    }

    private void Update()
    {
        if (!_gameActive) return;

        var mouse = Mouse.current;
        if (mouse == null) return;

        Ray ray  = Camera.main.ScreenPointToRay(mouse.position.ReadValue());
        bool hit = Physics.Raycast(ray, out RaycastHit hitInfo);

        UpdateHover(hit ? hitInfo.collider : null);

        if (!mouse.leftButton.wasPressedThisFrame || !hit) return;

        if (toolManager.ActiveTool == ToolType.Bomb)
        {
            HexTile tile = hitInfo.collider.GetComponentInParent<HexTile>();
            if (tile != null) HandleTileClick(tile);
        }
        else
        {
            HexBridge bridge = hitInfo.collider.GetComponentInParent<HexBridge>();
            if (bridge != null) HandleBridgeClick(bridge);
        }
    }

    private void UpdateHover(Collider hitCollider)
    {
        bool isBomb = toolManager.ActiveTool == ToolType.Bomb;

        HexBridge newBridge = null;
        HexTile   newTile   = null;

        if (hitCollider != null)
        {
            if (isBomb) newTile   = hitCollider.GetComponentInParent<HexTile>();
            else        newBridge = hitCollider.GetComponentInParent<HexBridge>();
        }

        if (newBridge != _hoveredBridge)
        {
            // Unity'de destroyed obje için ?. çalışmaz, explicit null check kullan
            if (_hoveredBridge)
                _hoveredBridge.GetComponent<HexInteractIndicator>()?.SetHover(false);
            _hoveredBridge = newBridge;
            if (_hoveredBridge)
                _hoveredBridge.GetComponent<HexInteractIndicator>()?.SetHover(true);
        }

        if (newTile != _hoveredTile)
        {
            if (_hoveredTile)
                _hoveredTile.GetComponent<HexInteractIndicator>()?.SetHover(false);
            _hoveredTile = newTile;
            if (_hoveredTile &&
                _hoveredTile != levelLoader.SourceTile &&
                _hoveredTile != levelLoader.DestinationTile)
                _hoveredTile.GetComponent<HexInteractIndicator>()?.SetHover(true);
        }
    }

    private void RefreshIndicators()
    {
        bool isBomb = toolManager.ActiveTool == ToolType.Bomb;

        foreach (var bridge in _bridges)
        {
            if (!bridge) continue; // yıkılmış bridge'i atla
            var ind = bridge.GetComponent<HexInteractIndicator>();
            if (ind == null) continue;
            ind.SetVisible(!isBomb && !bridge.isUnbreakable && toolManager.CanCut(bridge.edgeType));
        }

        foreach (var tile in _tiles)
        {
            if (!tile) continue;
            var ind = tile.GetComponent<HexInteractIndicator>();
            if (ind == null) continue;
            bool canBomb = isBomb && tile != levelLoader.SourceTile && tile != levelLoader.DestinationTile;
            ind.SetVisible(canBomb);
        }

        _hoveredBridge = null;
        _hoveredTile   = null;
    }

    private void HideAllIndicators()
    {
        foreach (var b in _bridges) { if (b) b.GetComponent<HexInteractIndicator>()?.SetVisible(false); }
        foreach (var t in _tiles)   { if (t) t.GetComponent<HexInteractIndicator>()?.SetVisible(false); }
        _hoveredBridge = null;
        _hoveredTile   = null;
    }

    // ── Level Loading ──

    public void LoadLevel(int index)
    {
        if (levels == null || index >= levels.Length) return;
        _currentLevelIndex = index;

        var data = levels[index];
        _cutsUsed  = 0;
        _gameActive = true;

        winPanel?.SetActive(false);
        losePanel?.SetActive(false);

        _tiles = levelLoader.LoadLevel(data);
        CacheBridges();
        ApplyBridgeMaterials();

        _levelStartingGold = data.startingGold;
        toolManager.LoadInventory(data);
        shopManager?.LoadLevel(data);
        isoCamera.FrameTiles(_tiles);

        if (levelNameText) levelNameText.text = $"Level {data.levelNumber}: {data.levelName}";
        UpdateCutsUI();
        SetStatus("Cut the bridges to save the sheep!");
        RefreshIndicators();
    }

    // ── Click Handling ──

    private void HandleBridgeClick(HexBridge bridge)
    {
        if (bridge.isUnbreakable)
        {
            StartCoroutine(FlashError(bridge));
            SetStatus("This bridge cannot be destroyed!");
            return;
        }

        if (!toolManager.CanCut(bridge.edgeType))
        {
            StartCoroutine(FlashError(bridge));
            ToolType needed = RequiredTool(bridge.edgeType);
            SetStatus($"Wrong tool! {bridge.edgeType} needs {needed}.");
            return;
        }

        DestroyBridge(bridge);
        _cutsUsed++;
        toolManager.ConsumeActiveTool();
        UpdateCutsUI();
        CheckEndCondition();
    }

    private void ShowPanelAnimated(GameObject panel)
    {
        if (panel == null) return;
        panel.SetActive(true);
        panel.transform.localScale = Vector3.zero;
        panel.transform.DOScale(1f, 0.5f).SetEase(Ease.OutElastic);
    }

    private void HandleTileClick(HexTile tile)
    {
        if (tile == levelLoader.SourceTile || tile == levelLoader.DestinationTile)
        {
            SetStatus("Can't bomb the source or destination!");
            return;
        }

        if (!toolManager.IsBombActive())
        {
            SetStatus("No bombs remaining!");
            return;
        }

        BombTile(tile);
        _cutsUsed++;
        toolManager.ConsumeActiveTool();
        UpdateCutsUI();
        CheckEndCondition();
    }

    private void BombTile(HexTile tile)
    {
        AudioManager.Instance?.PlayBombExplode();
        HexFX.Instance?.SpawnBombExplode(tile.transform.position);
        HexFX.Instance?.SpawnDestroyPoof(tile.transform.position);

        var bridges = new List<HexBridge>(tile.bridges);
        foreach (var bridge in bridges)
        {
            if (bridge.isUnbreakable)
            {
                // Unbreakable bridge survives visually; disconnect from the other tile's graph list
                bridge.GetOtherTile(tile)?.bridges.Remove(bridge);
                continue;
            }
            _bridges.Remove(bridge);
            bridge.tileA?.bridges.Remove(bridge);
            bridge.tileB?.bridges.Remove(bridge);
            bridge.AnimateDestroyed(destroyedBridgeMaterial);
            StartCoroutine(RemoveAfterDelay(bridge.gameObject, 0.5f));
        }
        _tiles.Remove(tile);
        StartCoroutine(RemoveAfterDelay(tile.gameObject, 0.5f));
    }

    private void CheckEndCondition()
    {
        if (!IsConnected(levelLoader.SourceTile, levelLoader.DestinationTile))
            OnWin();
        else if (!toolManager.AnyToolRemaining())
            OnLose();
    }

    private void DestroyBridge(HexBridge bridge)
    {
        if (_hoveredBridge == bridge) _hoveredBridge = null;
        _bridges.Remove(bridge);
        bridge.tileA?.bridges.Remove(bridge);
        bridge.tileB?.bridges.Remove(bridge);
        bridge.AnimateDestroyed(destroyedBridgeMaterial);
        AudioManager.Instance?.PlayBridgeDestroy(bridge.edgeType);
        HexFX.Instance?.SpawnBridgeDestroy(bridge.edgeType, bridge.transform.position);
        HexFX.Instance?.SpawnDestroyPoof(bridge.transform.position);
        StartCoroutine(RemoveAfterDelay(bridge.gameObject, 0.5f));
    }

    private IEnumerator FlashError(HexBridge bridge)
    {
        bridge.SetMaterial(errorBridgeMaterial);
        bridge.AnimateError();
        AudioManager.Instance?.PlayError();
        HexFX.Instance?.SpawnError(bridge.transform.position);
        yield return new WaitForSeconds(0.4f);
        if (bridge != null) bridge.RestoreTypeMaterial();
    }

    private IEnumerator RemoveAfterDelay(GameObject go, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (go) Destroy(go);
    }

    // ── Win / Lose ──

    private void OnWin()
    {
        HideAllIndicators();
        _gameActive = false;
        int stars = CalcStars();
        SetStatus($"You saved the sheep! Cuts used: {_cutsUsed}");
        AudioManager.Instance?.PlayWin();
        StartCoroutine(SpawnWinFireworks());

        bool hasNext = _currentLevelIndex + 1 < levels.Length;
        DOVirtual.DelayedCall(1f, () =>
        {
            ShowPanelAnimated(winPanel);
            nextLevelButton?.gameObject.SetActive(hasNext);
            if (starText) starText.text = new string('*', stars);  // UI tarafı sprite'a çevirir
        });
    }

    private IEnumerator SpawnWinFireworks()
    {
        Vector3 center = _tiles.Count > 0 ? _tiles[_tiles.Count / 2].transform.position : Vector3.zero;
        for (int i = 0; i < 6; i++)
        {
            Vector3 offset = new Vector3(Random.Range(-4f, 4f), 1f, Random.Range(-4f, 4f));
            HexFX.Instance?.SpawnWin(center + offset);
            yield return new WaitForSeconds(0.3f);
        }
    }

    private int CalcStars()
    {
        if (_levelStartingGold <= 0 || shopManager == null) return 1;
        float ratio = (float)shopManager.Gold / _levelStartingGold;
        if (ratio >= 0.66f) return 3;
        if (ratio >= 0.33f) return 2;
        return 1;
    }

    private void OnLose()
    {
        HideAllIndicators();
        _gameActive = false;
        SetStatus("No tools left — the wolf crosses!");
        AudioManager.Instance?.PlayLose();
        HexFX.Instance?.SpawnLose(levelLoader.DestinationTile?.transform.position ?? Vector3.zero);
        DOVirtual.DelayedCall(0.5f, () => ShowPanelAnimated(losePanel));
    }

    private void LoadNextLevel() => LoadLevel(_currentLevelIndex + 1);
    private void RetryLevel()    => LoadLevel(_currentLevelIndex);

    // ── Connectivity BFS ──

    private bool IsConnected(HexTile source, HexTile destination)
    {
        if (source == null || destination == null) return false;
        var visited = new HashSet<HexTile> { source };
        var queue   = new Queue<HexTile>();
        queue.Enqueue(source);
        while (queue.Count > 0)
        {
            var tile = queue.Dequeue();
            if (tile == destination) return true;
            foreach (var bridge in tile.bridges)
            {
                var neighbor = bridge.GetOtherTile(tile);
                if (neighbor != null && visited.Add(neighbor))
                    queue.Enqueue(neighbor);
            }
        }
        return false;
    }

    // ── Helpers ──

    private void CacheBridges()
    {
        _bridges.Clear();
        _bridges.AddRange(levelLoader.GetComponentsInChildren<HexBridge>());
    }

    private void ApplyBridgeMaterials()
    {
        foreach (var b in _bridges)
        {
            Material mat = BridgeMaterial(b.edgeType);
            b.Initialize(b.tileA, b.tileB, b.edgeType, mat);
        }
    }

    private Material BridgeMaterial(EdgeType type) => type switch
    {
        EdgeType.Stone       => stoneBridgeMaterial,
        EdgeType.Metal       => metalBridgeMaterial,
        EdgeType.Unbreakable => unbreakableBridgeMaterial,
        _                    => woodBridgeMaterial
    };

    private static ToolType RequiredTool(EdgeType edge) => edge switch
    {
        EdgeType.Stone => ToolType.Pickaxe,
        EdgeType.Metal => ToolType.IronShears,
        _              => ToolType.Axe
    };

    private void UpdateCutsUI()
    {
        if (cutsUsedText) cutsUsedText.text = $"Cuts Used: {_cutsUsed}";
    }

    private void SetStatus(string msg)
    {
        if (statusText) statusText.text = msg;
    }
}
