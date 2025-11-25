using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadMenu() => SceneManager.LoadScene("Menu");
    public void LoadMain() => SceneManager.LoadScene("Main");
    public void LoadHighscore() => SceneManager.LoadScene("Highscore");
    public void LoadStory() => SceneManager.LoadScene("Story");

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Quit Game");
    }
}
