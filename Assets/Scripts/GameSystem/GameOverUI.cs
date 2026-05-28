using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private PauseMenuUI pauseMenuUI;
    // 비워두면 현재 씬 재시작, 값을 넣으면 해당 씬으로 이동 (TutorialScene → "GameScene")
    [SerializeField] private string restartSceneName;

    private void Awake()
    {
        panel.SetActive(false);
        restartButton.onClick.AddListener(Restart);
        quitButton.onClick.AddListener(() => SceneManager.LoadScene("TitleScene"));
    }

    public void Show(int winnerId, bool isVsAI)
    {
        if (resultText != null)
        {
            string key = isVsAI
                ? (winnerId == 0 ? "LABEL_VICTORY" : "LABEL_DEFEAT")
                : (winnerId == 0 ? "LABEL_P1_WIN"  : "LABEL_P2_WIN");
            resultText.text = LocalizationManager.Get(key);
        }

        if (pauseMenuUI != null) pauseMenuUI.enabled = false;
        panel.SetActive(true);
    }

    private void Restart()
    {
        string scene = string.IsNullOrEmpty(restartSceneName)
            ? SceneManager.GetActiveScene().name
            : restartSceneName;
        SceneManager.LoadScene(scene);
    }
}
