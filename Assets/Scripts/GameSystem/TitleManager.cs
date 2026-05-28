using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleManager : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button tutorialButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button languageButton;

    private void Awake()
    {
        startButton.onClick.AddListener(() => SceneManager.LoadScene("GameScene"));
        tutorialButton.onClick.AddListener(() => SceneManager.LoadScene("TutorialScene"));
        quitButton.onClick.AddListener(Quit);
        languageButton.onClick.AddListener(ToggleLanguage);
    }

    private void ToggleLanguage()
    {
        var next = LocalizationManager.CurrentLanguage == Language.Korean
            ? Language.English : Language.Korean;
        LocalizationManager.SetLanguage(next);
    }

    private void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
