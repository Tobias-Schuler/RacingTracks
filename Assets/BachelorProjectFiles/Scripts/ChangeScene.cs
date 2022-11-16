using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeScene : MonoBehaviour {
    public void Change(int sceneIndex) {
        SceneManager.LoadScene(sceneIndex);
    }

    public void ExitApplication() {
        Application.Quit();
    }
}
