using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private GameOverPanelView panelView;
    [SerializeField] private PauseMenuUI pauseMenuUI;
    [SerializeField] private string restartSceneName;   // 비워두면 현재 씬
    [SerializeField] private AudioClip bellClip;

    private void Awake()
    {
        if (panelView == null)
            panelView = FindFirstObjectByType<GameOverPanelView>(FindObjectsInactive.Include);

        panelView.gameObject.SetActive(false);
        panelView.restartButton.onClick.AddListener(Restart);
        panelView.quitButton.onClick.AddListener(() => SceneLoader.LoadScene("TitleScene"));
    }

    public void Show(int winnerId, bool isVsAI)
    {
        SoundManager.Instance?.PlaySFX(bellClip);

        if (panelView.resultText != null)
        {
            string key = isVsAI
                ? (winnerId == 0 ? "LABEL_VICTORY" : "LABEL_DEFEAT")
                : (winnerId == 0 ? "LABEL_P1_WIN" : "LABEL_P2_WIN");
            panelView.resultText.text = LocalizationManager.Get(key);
        }

        if (pauseMenuUI != null) pauseMenuUI.enabled = false;
        panelView.gameObject.SetActive(true);
    }

    private void Restart()
    {
        string scene = string.IsNullOrEmpty(restartSceneName)
            ? SceneManager.GetActiveScene().name
            : restartSceneName;
        SceneLoader.LoadScene(scene);
    }
}
