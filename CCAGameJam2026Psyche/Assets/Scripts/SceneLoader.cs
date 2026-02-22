using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadScene1()
    {
        SceneManager.LoadScene("Scene 1");
    }


    public void LoadMainMenu()
    {
        SceneManager.LoadScene("Main Menu");
    }
}
