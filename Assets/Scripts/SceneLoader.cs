using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Attach to any GameObject. Wire a Button's OnClick() to LoadScene().
/// </summary>
public class SceneLoader : MonoBehaviour
{
    [SerializeField] private string targetScene;

    public void LoadScene()
    {
        if (string.IsNullOrEmpty(targetScene))
        {
            Debug.LogWarning("SceneLoader: targetScene is empty!");
            return;
        }

        SceneManager.LoadScene(targetScene);
    }
}
