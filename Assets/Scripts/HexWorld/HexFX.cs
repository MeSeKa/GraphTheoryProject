using UnityEngine;

public class HexFX : MonoBehaviour
{
    public static HexFX Instance { get; private set; }

    [Header("Bridge Destroy")]
    [SerializeField] GameObject woodDestroyFX;
    [SerializeField] GameObject stoneDestroyFX;
    [SerializeField] GameObject metalDestroyFX;

    [Header("Bomb / Node")]
    [SerializeField] GameObject bombExplodeFX;

    [Header("Destroy Poof (bridge & node)")]
    [SerializeField] GameObject destroyPoofFX;

    [Header("Error")]
    [SerializeField] GameObject errorFX;

    [Header("Win Fireworks (random pool)")]
    [SerializeField] GameObject[] winFXPool;

    [Header("Lose")]
    [SerializeField] GameObject loseFX;

    [Header("Scale")]
    [SerializeField] float fxScale = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void SpawnBridgeDestroy(EdgeType type, Vector3 pos) => Spawn(type switch
    {
        EdgeType.Stone => stoneDestroyFX,
        EdgeType.Metal => metalDestroyFX,
        _              => woodDestroyFX,
    }, pos);

    public void SpawnBombExplode(Vector3 pos)  => Spawn(bombExplodeFX, pos);
    public void SpawnDestroyPoof(Vector3 pos) => Spawn(destroyPoofFX, pos);
    public void SpawnError(Vector3 pos)        => Spawn(errorFX, pos);
    public void SpawnLose(Vector3 pos)        => Spawn(loseFX, pos);

    public void SpawnWin(Vector3 pos)
    {
        if (winFXPool == null || winFXPool.Length == 0) return;
        Spawn(winFXPool[Random.Range(0, winFXPool.Length)], pos);
    }

    private void Spawn(GameObject prefab, Vector3 pos)
    {
        if (prefab == null) return;
        var go = Instantiate(prefab, pos, Quaternion.identity);
        if (fxScale != 1f) go.transform.localScale *= fxScale;
        Destroy(go, 5f);
    }
}
