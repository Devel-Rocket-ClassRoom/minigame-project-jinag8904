using Cysharp.Threading.Tasks;
using Firebase.Auth;
using System;
using UnityEngine;

public class AuthManager : MonoBehaviour
{
    private static AuthManager instance;
    public static AuthManager Instance => instance;

    private FirebaseUser currentUser;
    public FirebaseUser CurrentUser => currentUser;

    private bool isInitialized = false;
    public bool IsInitialized => isInitialized;

    private FirebaseAuth auth;
    private bool lastNotifiedSignedIn = false;


    public bool IsLogedIn => currentUser != null;
    public string UserId => currentUser?.UserId ?? string.Empty;

    public event Action<bool> LoginStateChanged;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (auth != null)
            auth.StateChanged -= OnAuthStateChanged;

        if (instance == this)
            instance = null;
    }

    private async UniTaskVoid Start()
    {
        bool isReady = await FirebaseInitializer.Instance.WaitForInitializationAsync();

        if (!isReady)
        {
            Debug.LogError("[Auth] 파이어 베이스 초기화 실패 Auth 초기화 불가");
            return;
        }

        auth = FirebaseInitializer.Instance.Auth;
        auth.StateChanged += OnAuthStateChanged;

        currentUser = auth.CurrentUser;
        Debug.Log(currentUser != null ? "[Auth] 이미 로그인 됨" : "[Auth] 로그인 필요");

        isInitialized = true;
        NotifyLoginState();
    }

    private void OnAuthStateChanged(object sender, EventArgs eventArgs)
    {
        NotifyLoginState();
    }

    private void NotifyLoginState()
    {
        bool signedIn = IsLogedIn;
        if (signedIn == lastNotifiedSignedIn) return;

        lastNotifiedSignedIn = signedIn;
        Debug.Log(signedIn ? $"[Auth] 로그인 상태: {UserId}" : "[Auth] 로그아웃 상태");
        LoginStateChanged?.Invoke(signedIn);
    }

    public async UniTask<(bool success, string error)> SignInAnonymousyAsync()
    {
        try
        {
            Debug.Log("[Auth] 익명 로그인 시도...");

            AuthResult result = await auth.SignInAnonymouslyAsync();
            currentUser = result.User;
            NotifyLoginState();

            Debug.Log($"[Auth] 익명 로그인 성공: {currentUser.UserId}");
            return (true, null);
        }
        catch (Exception ex)
        {
            Debug.Log($"[Auth] 익명 로그인 실패: {ex.Message}");
            return (false, ParseFirebaseError(ex.Message));
        }
    }

    public async UniTask<(bool success, string error)> CreateUserWithEmailAsync(string email, string pw)
    {
        try
        {
            Debug.Log("[Auth] 회원 가입 시도...");

            AuthResult result = await auth.CreateUserWithEmailAndPasswordAsync(email, pw);
            currentUser = result.User;
            NotifyLoginState();

            Debug.Log($"[Auth] 회원 가입 성공: {currentUser.UserId}");
            return (true, null);
        }
        catch (Exception ex)
        {
            Debug.Log($"[Auth] 회원 가입 실패: {ex.Message}");
            return (false, ParseFirebaseError(ex.Message));
        }
    }

    public async UniTask<(bool success, string error)> SignInUserWithEmailAsync(string email, string pw)
    {
        try
        {
            Debug.Log("[Auth] 이메일 로그인 시도...");

            AuthResult result = await auth.SignInWithEmailAndPasswordAsync(email, pw);
            currentUser = result.User;
            NotifyLoginState();

            Debug.Log($"[Auth] 이메일 로그인 성공: {currentUser.UserId}");
            return (true, null);
        }
        catch (Exception ex)
        {
            Debug.Log($"[Auth] 이메일 로그인 실패: {ex.Message}");
            return (false, ParseFirebaseError(ex.Message));
        }
    }

    public void SignOut()
    {
        if (auth != null && currentUser != null)
        {
            Debug.Log($"[Auth] 로그아웃: {currentUser.UserId}");
            auth.SignOut();
            currentUser = null;
            NotifyLoginState();
        }
    }

    private string ParseFirebaseError(string error)
    {
        Debug.LogWarning($"[Auth] Firebase 에러 원문: {error}");

        string lower = error.ToLowerInvariant();

        if (lower.Contains("already in use") || lower.Contains("email-already"))
        {
            return "AUTH_ERR_EMAIL_IN_USE";
        }
        if (lower.Contains("at least 6") || lower.Contains("weak") || lower.Contains("password is invalid"))
        {
            return "AUTH_ERR_WEAK_PW";
        }
        if (lower.Contains("badly formatted") || lower.Contains("invalid-email"))
        {
            return "AUTH_ERR_INVALID_EMAIL";
        }
        if (lower.Contains("network"))
        {
            return "AUTH_ERR_NETWORK";
        }

        return "AUTH_ERR_GENERIC";
    }
}
