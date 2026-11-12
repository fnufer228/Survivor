using UnityEngine;
using UnityEngine.SceneManagement;

public class Bootstrap : MonoBehaviour
{
    public string mapName;

    void Awake()
    {
        SceneManager.LoadScene(mapName);
    }
}
