using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Bridge Destroy")]
    [SerializeField] AudioClip[] woodDestroySounds;
    [SerializeField] AudioClip[] stoneDestroySounds;
    [SerializeField] AudioClip[] metalDestroySounds;

    [Header("Actions")]
    [SerializeField] AudioClip errorSound;
    [SerializeField] AudioClip bombExplodeSound;

    [Header("UI")]
    [SerializeField] AudioClip toolSelectSound;
    [SerializeField] AudioClip shopOpenSound;
    [SerializeField] AudioClip shopCloseSound;
    [SerializeField] AudioClip buySuccessSound;
    [SerializeField] AudioClip buyFailSound;

    [Header("Win / Lose")]
    [SerializeField] AudioClip winSound;
    [SerializeField] AudioClip loseSound;

    private AudioSource _source;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _source = gameObject.AddComponent<AudioSource>();
        _source.playOnAwake = false;
    }

    public void PlayBridgeDestroy(EdgeType type) => PlayRandom(type switch
    {
        EdgeType.Stone => stoneDestroySounds,
        EdgeType.Metal => metalDestroySounds,
        _              => woodDestroySounds,
    });

    public void PlayBombExplode() => Play(bombExplodeSound);
    public void PlayError()       => Play(errorSound);
    public void PlayToolSelect()  => Play(toolSelectSound);
    public void PlayShopOpen()    => Play(shopOpenSound);
    public void PlayShopClose()   => Play(shopCloseSound);
    public void PlayBuySuccess()  => Play(buySuccessSound);
    public void PlayBuyFail()     => Play(buyFailSound);
    public void PlayWin()         => Play(winSound);
    public void PlayLose()        => Play(loseSound);

    private void Play(AudioClip clip)
    {
        if (clip != null) _source.PlayOneShot(clip);
    }

    private void PlayRandom(AudioClip[] pool)
    {
        if (pool == null || pool.Length == 0) return;
        Play(pool[Random.Range(0, pool.Length)]);
    }
}
