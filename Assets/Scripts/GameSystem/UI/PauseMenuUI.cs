using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenuUI : MonoBehaviour
{
    [SerializeField] private PausePanelView panelView;

    private bool isPaused;
    private InputAction escAction;

    private void Awake()
    {
        if (panelView == null)
            panelView = FindFirstObjectByType<PausePanelView>(FindObjectsInactive.Include);

        panelView.gameObject.SetActive(false);
        panelView.resumeButton.onClick.AddListener(Resume);
        panelView.restartButton.onClick.AddListener(Restart);
        panelView.quitButton.onClick.AddListener(Quit);

        escAction = new InputAction(binding: "<Keyboard>/escape");
        escAction.performed += _ => SetPaused(!isPaused);
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
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void Quit()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("TitleScene");
    }
}
