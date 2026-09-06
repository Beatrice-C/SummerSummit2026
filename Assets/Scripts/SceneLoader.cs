using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public string sceneName = "Narrative";

    public void LoadScene()
    {
        SceneManager.LoadScene(sceneName);
    }
}
