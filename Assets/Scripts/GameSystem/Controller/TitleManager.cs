using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks;
using TMPro;

public class TitleManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI uidText;
    [SerializeField] private Button logoutBtn;

    [SerializeField] private Button startButton;
    [SerializeField] private Button tutorialButton;
    [SerializeField] private Button languageButton;
    
    [SerializeField] private Button settingsButton;
    [SerializeField] private SettingsPanelView settingsPanel;

    [SerializeField] private Button statsButton;
    [SerializeField] private StatsPanelView statsPanel;
    [SerializeField] private LoginPanelView loginPanel;

    [SerializeField] private Button quitButton;

    private InputAction escAction;

    private void Awake()
    {
        startButton.onClick.AddListener(() => SceneLoader.LoadScene("GameScene"));
        tutorialButton.onClick.AddListener(() => SceneLoader.LoadScene("TutorialScene"));
        languageButton.onClick.AddListener(ToggleLanguage);
        settingsButton.onClick.AddListener(() => settingsPanel.Show());
        statsButton.onClick.AddListener(() => OpenStats().Forget());
        loginPanel.OnLoginSuccess += () => OpenStatsAfterLogin().Forget();
        logoutBtn.onClick.AddListener(OnLogout);
        InitAuthUI().Forget();
        quitButton.onClick.AddListener(Quit);

        // 패널이 열려 있으면 ESC로 닫는다.
        escAction = new InputAction(binding: "<Keyboard>/escape");
        escAction.performed += _ =>
        {
            if (settingsPanel != null && settingsPanel.IsOpen)
                settingsPanel.Hide();
            else if (loginPanel != null && loginPanel.IsOpen)
                loginPanel.Hide();
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

    private async UniTaskVoid OpenStats()
    {
        var ct = this.GetCancellationTokenOnDestroy();
        await UniTask.WaitUntil(() => AuthManager.Instance != null && AuthManager.Instance.IsInitialized, cancellationToken: ct);

        if (AuthManager.Instance.IsLogedIn)
        {
            await StatsService.InitAsync();
            if (ct.IsCancellationRequested) return;
            statsPanel.Show();
        }
        else loginPanel.Show();
    }

    private async UniTaskVoid OpenStatsAfterLogin()
    {
        var ct = this.GetCancellationTokenOnDestroy();
        await StatsService.InitAsync();
        if (ct.IsCancellationRequested) return;
        statsPanel.Show();
    }

    private async UniTaskVoid InitAuthUI()
    {
        await UniTask.WaitUntil(() => AuthManager.Instance != null && AuthManager.Instance.IsInitialized, cancellationToken: this.GetCancellationTokenOnDestroy());
        AuthManager.Instance.LoginStateChanged += RefreshAuthUI;
        RefreshAuthUI(AuthManager.Instance.IsLogedIn);
    }

    private void RefreshAuthUI(bool signedIn)
    {
        if (uidText != null)
            uidText.text = signedIn ? AuthManager.Instance.UserId : "";
        if (logoutBtn != null)
            logoutBtn.gameObject.SetActive(signedIn);
    }

    private void OnLogout()
    {
        AuthManager.Instance.SignOut();
    }

    private void OnDestroy()
    {
        if (AuthManager.Instance != null)
            AuthManager.Instance.LoginStateChanged -= RefreshAuthUI;
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
