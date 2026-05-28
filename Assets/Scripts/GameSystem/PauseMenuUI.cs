using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button quitButton;

    private bool isPaused;
    private InputAction escAction;

    private void Awake()
    {
        pausePanel.SetActive(false);
        resumeButton.onClick.AddListener(Resume);
        restartButton.onClick.AddListener(Restart);
        quitButton.onClick.AddListener(Quit);

        escAction = new InputAction(binding: "<Keyboard>/escape");
        escAction.performed += _ => SetPaused(!isPaused);
    }

    private void OnEnable() => escAction.Enable();
    private void OnDisable() => escAction.Disable();

    private void SetPaused(bool paused)
    {
        isPaused = paused;
        pausePanel.SetActive(paused);
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
