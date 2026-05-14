using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// HexWorld Level Editor — generate + görsel düzenleme + kayıt.
// Menü: HexWorld / Level Editor
public class HexLevelEditorWindow : EditorWindow
{
    // ── Generator Params ──
    int    _radius              = 3;
    int    _cutSize             = 2;
    float  _woodRatio           = 0.5f;
    float  _stoneRatio          = 0.3f;
    float  _metalRatio          = 0.2f;
    float  _internalDensity     = 0.4f;
    bool   _internalUnbreakable = false;
    int    _extraTools          = 0;
    int    _bombCount           = 0;
    int    _startingGold        = 500;
    string _levelName           = "Generated Level";
    int    _levelNumber         = 1;

    // ── Level State ──
    List<Vector2Int>                               _tiles   = new();
    Dictionary<(Vector2Int, Vector2Int), EdgeType> _bridges = new();
    Vector2Int _source;
    Vector2Int _destination;
    bool _hasSource;
    bool _hasDest;

    // Derived tool counts (set by generator, editable)
    int _axeCount;
    int _pickaxeCount;
    int _ironShearsCount;

    // ── Interaction ──
    enum EditMode { None, SetSource, SetDest, AddBridge }
    EditMode    _mode            = EditMode.None;
    Vector2Int? _bridgeFirstTile = null;

    // ── Grid View ──
    float   _hexDrawSize = 32f;
    Vector2 _pan         = Vector2.zero;
    bool    _panning;
    Vector2 _panMouseStart;
    Vector2 _panStartAtDrag;
    Vector2Int? _hoveredTile = null;

    [MenuItem("HexWorld/Level Editor")]
    static void Open() => GetWindow<HexLevelEditorWindow>("Level Editor");

    // ──────────────────────────────────────────────────────────
    // OnGUI
    // ──────────────────────────────────────────────────────────

    void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        DrawLeftPanel();
        GUILayout.Box("", GUILayout.Width(1), GUILayout.ExpandHeight(true));
        DrawGridPanel();
        EditorGUILayout.EndHorizontal();
    }

    // ──────────────────────────────────────────────────────────
    // Left Panel
    // ──────────────────────────────────────────────────────────

    void DrawLeftPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(235), GUILayout.ExpandHeight(true));
        EditorGUILayout.Space(4);

        GUILayout.Label("Generator", EditorStyles.boldLabel);
        _levelName    = EditorGUILayout.TextField("Name",     _levelName);
        _levelNumber  = EditorGUILayout.IntField("Number",   _levelNumber);
        _radius       = EditorGUILayout.IntSlider("Radius",   _radius,  1, 5);
        _cutSize      = EditorGUILayout.IntSlider("Cut Size", _cutSize, 1, 10);

        EditorGUILayout.Space(4);
        GUILayout.Label("Cut Bridge Mix", EditorStyles.miniLabel);
        _woodRatio   = EditorGUILayout.Slider("Wood",  _woodRatio,  0f, 1f);
        _stoneRatio  = EditorGUILayout.Slider("Stone", _stoneRatio, 0f, 1f);
        _metalRatio  = EditorGUILayout.Slider("Metal", _metalRatio, 0f, 1f);

        EditorGUILayout.Space(4);
        _internalDensity     = EditorGUILayout.Slider("Int. Density",    _internalDensity, 0f, 1f);
        _internalUnbreakable = EditorGUILayout.Toggle("Int. Unbreakable", _internalUnbreakable);
        _extraTools          = EditorGUILayout.IntSlider("Extra Tools",   _extraTools, 0, 3);
        _bombCount           = EditorGUILayout.IntField("Bombs",          _bombCount);
        _startingGold        = EditorGUILayout.IntField("Start Gold",     _startingGold);

        EditorGUILayout.Space(6);
        if (GUILayout.Button("Generate", GUILayout.Height(26)))
            DoGenerate();

        // ── Tool Counts (düzenlenebilir) ──
        if (_tiles.Count > 0)
        {
            EditorGUILayout.Space(6);
            GUILayout.Label("Tool Inventory", EditorStyles.boldLabel);
            _axeCount        = EditorGUILayout.IntField("Axe",        _axeCount);
            _pickaxeCount    = EditorGUILayout.IntField("Pickaxe",    _pickaxeCount);
            _ironShearsCount = EditorGUILayout.IntField("Iron Shears",_ironShearsCount);
            _bombCount       = EditorGUILayout.IntField("Bombs",      _bombCount);
        }

        // ── Edit Modes ──
        EditorGUILayout.Space(8);
        GUILayout.Label("Edit Mode", EditorStyles.boldLabel);

        if (ToggleButton("Set Source",       _mode == EditMode.SetSource))  SetMode(EditMode.SetSource);
        if (ToggleButton("Set Destination",  _mode == EditMode.SetDest))    SetMode(EditMode.SetDest);
        if (ToggleButton("Add/Change Bridge",_mode == EditMode.AddBridge))  SetMode(EditMode.AddBridge);

        if (_mode == EditMode.AddBridge)
        {
            string hint = _bridgeFirstTile.HasValue
                ? $"2nd click adjacent tile\n(selected: {_bridgeFirstTile})"
                : "1st click: select tile";
            EditorGUILayout.HelpBox(hint, MessageType.Info);
            EditorGUILayout.LabelField("Cycle: Wood→Stone→Metal→Unbreakable→(remove)", EditorStyles.miniLabel);
        }

        // ── Save / Load ──
        EditorGUILayout.Space(8);
        GUI.enabled = _tiles.Count > 0;
        if (GUILayout.Button("Save As Asset"))
            SaveAsset();
        GUI.enabled = true;

        if (GUILayout.Button("Load Existing Asset"))
            LoadExisting();

        // ── Stats ──
        GUILayout.FlexibleSpace();
        if (_tiles.Count > 0)
        {
            int unb = _bridges.Values.Count(t => t == EdgeType.Unbreakable);
            int brk = _bridges.Count - unb;
            GUILayout.Label($"Tiles: {_tiles.Count}  Bridges: {_bridges.Count} ({brk} breakable, {unb} unbreakable)",
                EditorStyles.miniLabel);
        }
        EditorGUILayout.Space(4);
        EditorGUILayout.EndVertical();
    }

    // ──────────────────────────────────────────────────────────
    // Grid Panel
    // ──────────────────────────────────────────────────────────

    void DrawGridPanel()
    {
        var gridRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none,
            GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

        var e = Event.current;

        // Pan (middle mouse or Alt+left drag)
        if (e.type == EventType.MouseDown && gridRect.Contains(e.mousePosition)
            && (e.button == 2 || (e.button == 0 && e.alt)))
        {
            _panning = true; _panMouseStart = e.mousePosition; _panStartAtDrag = _pan;
            e.Use();
        }
        if (_panning && e.type == EventType.MouseDrag)
        {
            _pan = _panStartAtDrag + (e.mousePosition - _panMouseStart);
            Repaint(); e.Use();
        }
        if (_panning && e.type == EventType.MouseUp)
        {
            _panning = false; e.Use();
        }

        // Zoom (scroll)
        if (e.type == EventType.ScrollWheel && gridRect.Contains(e.mousePosition))
        {
            _hexDrawSize = Mathf.Clamp(_hexDrawSize - e.delta.y * 1.5f, 12f, 80f);
            Repaint(); e.Use();
        }

        // Background
        if (e.type == EventType.Repaint)
            EditorGUI.DrawRect(gridRect, new Color(0.13f, 0.17f, 0.22f));

        if (_tiles.Count == 0)
        {
            GUI.Label(gridRect, "Press \"Generate\" to create a level",
                new GUIStyle(EditorStyles.label)
                { alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(0.45f, 0.55f, 0.65f) } });
            return;
        }

        // Hover detection
        _hoveredTile = null;
        if (!_panning && gridRect.Contains(e.mousePosition))
        {
            foreach (var tile in _tiles)
            {
                if (Vector2.Distance(TileScreenPos(tile, gridRect), e.mousePosition) < _hexDrawSize * 0.48f)
                { _hoveredTile = tile; break; }
            }
        }

        // Left click
        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt && gridRect.Contains(e.mousePosition))
        {
            if (_hoveredTile.HasValue) HandleTileClick(_hoveredTile.Value);
            e.Use(); Repaint();
        }

        // Right click — cancel pending bridge
        if (e.type == EventType.MouseDown && e.button == 1)
        {
            _bridgeFirstTile = null; Repaint();
        }

        // Draw
        if (e.type == EventType.Repaint)
        {
            Handles.BeginGUI();
            DrawBridges(gridRect);
            DrawTiles(gridRect);
            Handles.EndGUI();
        }

        if (_hoveredTile.HasValue) Repaint();
    }

    void DrawBridges(Rect gridRect)
    {
        foreach (var kvp in _bridges)
        {
            var (a, b) = kvp.Key;
            Vector2 pa = TileScreenPos(a, gridRect);
            Vector2 pb = TileScreenPos(b, gridRect);
            Handles.color = BridgeColor(kvp.Value);
            Handles.DrawAAPolyLine(4f, pa, pb);
        }

        // Preview line when bridge first tile is selected
        if (_bridgeFirstTile.HasValue && _hoveredTile.HasValue && _hoveredTile != _bridgeFirstTile)
        {
            Handles.color = new Color(1f, 1f, 0.3f, 0.5f);
            Handles.DrawAAPolyLine(2f,
                TileScreenPos(_bridgeFirstTile.Value, gridRect),
                TileScreenPos(_hoveredTile.Value, gridRect));
        }
    }

    void DrawTiles(Rect gridRect)
    {
        var labelStyle = new GUIStyle(EditorStyles.miniLabel)
            { alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };

        foreach (var tile in _tiles)
        {
            Vector2 sc = TileScreenPos(tile, gridRect);
            float r = _hexDrawSize * 0.44f;

            Color c = TileColor(tile);
            if (_hoveredTile == tile) c = Color.Lerp(c, Color.white, 0.25f);

            Handles.color = c;
            Handles.DrawSolidDisc(sc, Vector3.forward, r);
            Handles.color = Color.black;
            Handles.DrawWireDisc(sc, Vector3.forward, r);

            if (_hexDrawSize > 18f)
                GUI.Label(new Rect(sc.x - 22, sc.y - 8, 44, 16), $"{tile.x},{tile.y}", labelStyle);
        }

        // Highlight first bridge tile selection
        if (_bridgeFirstTile.HasValue)
        {
            Vector2 sc = TileScreenPos(_bridgeFirstTile.Value, gridRect);
            Handles.color = new Color(1f, 1f, 0.2f, 0.8f);
            Handles.DrawWireDisc(sc, Vector3.forward, _hexDrawSize * 0.5f);
        }
    }

    // ──────────────────────────────────────────────────────────
    // Interaction
    // ──────────────────────────────────────────────────────────

    void HandleTileClick(Vector2Int tile)
    {
        switch (_mode)
        {
            case EditMode.SetSource:
                _source = tile; _hasSource = true; _mode = EditMode.None; break;

            case EditMode.SetDest:
                _destination = tile; _hasDest = true; _mode = EditMode.None; break;

            case EditMode.AddBridge:
                if (!_bridgeFirstTile.HasValue)
                {
                    _bridgeFirstTile = tile;
                }
                else
                {
                    var first = _bridgeFirstTile.Value;
                    _bridgeFirstTile = null;
                    if (first != tile && HexGrid.AreAdjacent(first.x, first.y, tile.x, tile.y))
                        CycleBridge(first, tile);
                }
                break;
        }
    }

    void CycleBridge(Vector2Int a, Vector2Int b)
    {
        var key = EdgeKey(a, b);
        if (!_bridges.TryGetValue(key, out var cur))
        { _bridges[key] = EdgeType.Wood; return; }

        switch (cur)
        {
            case EdgeType.Wood:        _bridges[key] = EdgeType.Stone;       break;
            case EdgeType.Stone:       _bridges[key] = EdgeType.Metal;       break;
            case EdgeType.Metal:       _bridges[key] = EdgeType.Unbreakable; break;
            case EdgeType.Unbreakable: _bridges.Remove(key);                 break;
        }
    }

    // ──────────────────────────────────────────────────────────
    // Generate / Load / Save
    // ──────────────────────────────────────────────────────────

    void DoGenerate()
    {
        var p = new HexLevelGenerator.Params
        {
            radius              = _radius,
            cutSize             = _cutSize,
            woodRatio           = _woodRatio,
            stoneRatio          = _stoneRatio,
            metalRatio          = _metalRatio,
            internalDensity     = _internalDensity,
            internalUnbreakable = _internalUnbreakable,
            extraTools          = _extraTools,
            bombCount           = _bombCount,
            startingGold        = _startingGold,
            levelName           = _levelName,
            levelNumber         = _levelNumber,
        };
        var temp = HexLevelGenerator.Generate(p);
        LoadFromData(temp);
        DestroyImmediate(temp);
    }

    void LoadFromData(HexLevelData data)
    {
        _tiles.Clear(); _bridges.Clear();
        foreach (var t in data.tiles) _tiles.Add(new Vector2Int(t.q, t.r));
        foreach (var b in data.bridges)
            _bridges[EdgeKey(new(b.q1, b.r1), new(b.q2, b.r2))] = b.edgeType;

        _source          = data.sourceTile;
        _destination     = data.destinationTile;
        _hasSource       = true;
        _hasDest         = true;
        _axeCount        = data.axeCount;
        _pickaxeCount    = data.pickaxeCount;
        _ironShearsCount = data.ironShearsCount;
        _bombCount       = data.bombCount;
        _levelName       = data.levelName;
        _levelNumber     = data.levelNumber;
        _startingGold    = data.startingGold;
        _pan             = Vector2.zero;
        _bridgeFirstTile = null;
        _mode            = EditMode.None;
        Repaint();
    }

    void LoadExisting()
    {
        string path = EditorUtility.OpenFilePanelWithFilters(
            "Load Level", "Assets/LevelData", new[] { "Level Asset", "asset" });
        if (string.IsNullOrEmpty(path)) return;
        path = "Assets" + path[Application.dataPath.Length..];
        var data = AssetDatabase.LoadAssetAtPath<HexLevelData>(path);
        if (data != null) LoadFromData(data);
    }

    void SaveAsset()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Save Level", $"Level{_levelNumber}", "asset", "Save level asset", "Assets/LevelData");
        if (string.IsNullOrEmpty(path)) return;

        var data = ScriptableObject.CreateInstance<HexLevelData>();
        data.levelName       = _levelName;
        data.levelNumber     = _levelNumber;
        data.tileType        = HexTileType.Grass;
        data.startingGold    = _startingGold;
        data.sourceTile      = _hasSource ? _source      : new(-_radius, 0);
        data.destinationTile = _hasDest   ? _destination : new( _radius, 0);
        data.tiles   = _tiles.Select(t => new HexTileEntry { q = t.x, r = t.y }).ToArray();
        data.bridges = _bridges.Select(kvp => new HexBridgeEntry
            { q1 = kvp.Key.Item1.x, r1 = kvp.Key.Item1.y, q2 = kvp.Key.Item2.x, r2 = kvp.Key.Item2.y, edgeType = kvp.Value }).ToArray();
        data.axeCount        = _axeCount;
        data.pickaxeCount    = _pickaxeCount;
        data.ironShearsCount = _ironShearsCount;
        data.bombCount       = _bombCount;

        AssetDatabase.CreateAsset(data, path);
        AssetDatabase.SaveAssets();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = data;
    }

    // ──────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────

    Vector2 GridCenter(Rect gridRect) =>
        new(gridRect.x + gridRect.width  * 0.5f + _pan.x,
            gridRect.y + gridRect.height * 0.5f + _pan.y);

    Vector2 TileScreenPos(Vector2Int t, Rect gridRect)
    {
        var center = GridCenter(gridRect);
        float x = center.x + _hexDrawSize * (Mathf.Sqrt(3f) * t.x + Mathf.Sqrt(3f) / 2f * t.y);
        float y = center.y + _hexDrawSize * (1.5f * t.y);
        return new Vector2(x, y);
    }

    Color TileColor(Vector2Int tile)
    {
        if (_hasSource && tile == _source)      return new Color(0.2f, 0.8f, 0.3f);
        if (_hasDest   && tile == _destination) return new Color(0.9f, 0.3f, 0.2f);
        return new Color(0.4f, 0.55f, 0.7f);
    }

    static Color BridgeColor(EdgeType type) => type switch
    {
        EdgeType.Wood        => new Color(0.6f, 0.35f, 0.1f),
        EdgeType.Stone       => new Color(0.5f, 0.5f,  0.5f),
        EdgeType.Metal       => new Color(0.3f, 0.5f,  0.9f),
        EdgeType.Unbreakable => new Color(0.9f, 0.8f,  0.1f),
        _                    => Color.white
    };

    static (Vector2Int, Vector2Int) EdgeKey(Vector2Int a, Vector2Int b) =>
        (a.x < b.x || (a.x == b.x && a.y < b.y)) ? (a, b) : (b, a);

    void SetMode(EditMode mode)
    {
        _mode = _mode == mode ? EditMode.None : mode;
        _bridgeFirstTile = null;
    }

    bool ToggleButton(string label, bool active)
    {
        var prev = GUI.backgroundColor;
        if (active) GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        bool clicked = GUILayout.Button(label);
        GUI.backgroundColor = prev;
        return clicked;
    }
}
