using Cysharp.Threading.Tasks;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoginPanelView : MonoBehaviour
{
    public TMP_InputField emailInput;
    public TMP_InputField pwInput;
    public Button loginBtn;
    public Button signupBtn;
    public Button closeBtn;
    public TextMeshProUGUI errorText;

    public bool IsOpen => gameObject.activeSelf;
    public event Action OnLoginSuccess; // 성공 시 TitleManager가 통계 패널을 연다

    private void Awake()
    {
        loginBtn.onClick.AddListener(() => Submit(false).Forget());
        signupBtn.onClick.AddListener(() => Submit(true).Forget());
        closeBtn.onClick.AddListener(Hide);
    }

    public void Show()
    {
        gameObject.SetActive(true);
        errorText.text = "";
    }

    public void Hide() => gameObject.SetActive(false);

    private async UniTaskVoid Submit(bool isSignup)
    {
        string email = emailInput.text.Trim();
        string pw = pwInput.text;
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pw))
        {
            errorText.text = LocalizationManager.Get("LOGIN_NEED_INPUT");
            return;
        }

        SetButtons(false);
        await UniTask.WaitUntil(() => AuthManager.Instance != null && AuthManager.Instance.IsInitialized);

        var (success, error) = isSignup
            ? await AuthManager.Instance.CreateUserWithEmailAsync(email, pw)
            : await AuthManager.Instance.SignInUserWithEmailAsync(email, pw);

        SetButtons(true);
        if (this == null) return;

        if (success) { Hide(); OnLoginSuccess?.Invoke(); }
        else errorText.text = LocalizationManager.Get(error);
    }

    private void SetButtons(bool interactable)
    {
        loginBtn.interactable = interactable;
        signupBtn.interactable = interactable;
    }
}
