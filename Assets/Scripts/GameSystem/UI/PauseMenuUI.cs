using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenuUI : MonoBehaviour
{
    [SerializeField] private PausePanelView panelView;
    [SerializeField] private YutGuidePopup guidePopup;
    [SerializeField] private SettingsPanelView settingsPanel;

    private bool isPaused;
    private InputAction escAction;

    private void Awake()
    {
        if (panelView == null)
            panelView = FindFirstObjectByType<PausePanelView>(FindObjectsInactive.Include);
        if (guidePopup == null)
            guidePopup = FindFirstObjectByType<YutGuidePopup>(FindObjectsInactive.Include);
        if (settingsPanel == null)
            settingsPanel = FindFirstObjectByType<SettingsPanelView>(FindObjectsInactive.Include);

        panelView.gameObject.SetActive(false);
        panelView.resumeButton.onClick.AddListener(Resume);
        panelView.restartButton.onClick.AddListener(Restart);
        panelView.settingsButton.onClick.AddListener(() => {
            panelView.gameObject.SetActive(false);
            settingsPanel?.Show();
        });
        settingsPanel?.closeButton.onClick.AddListener(() => { if (isPaused) panelView.gameObject.SetActive(true); });
        panelView.quitButton.onClick.AddListener(Quit);

        escAction = new InputAction(binding: "<Keyboard>/escape");
        escAction.performed += _ => OnEscape();
    }

    private void OnEscape()
    {
        if (SceneLoader.IsTransitioning) return;
        // 설정 패널이 열려 있으면 설정만 닫는다.
        if (settingsPanel != null && settingsPanel.IsOpen)
        {
            settingsPanel.Hide();
            panelView.gameObject.SetActive(true);
            return;
        }
        // 가이드가 열려 있으면 가이드만 닫고 pause는 열지 않는다.
        if (guidePopup != null && guidePopup.IsOpen)
        {
            guidePopup.Hide();
            return;
        }
        SetPaused(!isPaused);
    }

    private void OnEnable() => escAction.Enable();
    private void OnDisable() => escAction.Disable();

    private void SetPaused(bool paused)
    {
        isPaused = paused;
        panelView.gameObject.SetActive(paused);
        Time.timeScale = paused ? 0f : 1f;
    }

    private void Resume() => SetPaused(false);

    private void Restart()
    {
        Time.timeScale = 1f;
        SceneLoader.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void Quit()
    {
        Time.timeScale = 1f;
        SceneLoader.LoadScene("TitleScene");
    }
}
