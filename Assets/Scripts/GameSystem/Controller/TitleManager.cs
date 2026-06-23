using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TitleManager : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button tutorialButton;
    [SerializeField] private Button languageButton;
    
    [SerializeField] private Button settingsButton;
    [SerializeField] private SettingsPanelView settingsPanel;

    [SerializeField] private Button statsButton;
    [SerializeField] private StatsPanelView statsPanel;
    
    [SerializeField] private Button quitButton;

    private InputAction escAction;

    private void Awake()
    {
        startButton.onClick.AddListener(() => SceneLoader.LoadScene("GameScene"));
        tutorialButton.onClick.AddListener(() => SceneLoader.LoadScene("TutorialScene"));
        languageButton.onClick.AddListener(ToggleLanguage);
        settingsButton.onClick.AddListener(() => settingsPanel.Show());
        statsButton.onClick.AddListener(() => statsPanel.Show());
        
        quitButton.onClick.AddListener(Quit);

        // 패널이 열려 있으면 ESC로 닫는다.
        escAction = new InputAction(binding: "<Keyboard>/escape");
        escAction.performed += _ =>
        {
            if (settingsPanel != null && settingsPanel.IsOpen)
                settingsPanel.Hide();
            else if (statsPanel != null && statsPanel.IsOpen)
                statsPanel.Hide();
        };
    }

    private void OnEnable() => escAction.Enable();
    private void OnDisable() => escAction.Disable();

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
