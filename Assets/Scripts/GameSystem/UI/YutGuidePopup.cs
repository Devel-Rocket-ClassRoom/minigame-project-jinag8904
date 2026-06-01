using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이 중 H 키로 윷 결과 가이드 패널을 토글하고, 닫기 버튼으로 닫는다.
/// 게임을 멈추지 않고 오버레이만 띄운다(Time.timeScale 건드리지 않음).
/// ESC 닫기는 PauseMenuUI가 단독 소유하므로 여기서 ESC는 잡지 않는다.
/// </summary>
public class YutGuidePopup : MonoBehaviour
{
    [SerializeField] private GameObject panel;     // 가이드 패널 루트 (비활성 시작)
    [SerializeField] private Button closeButton;

    [Tooltip("이 패널이 떠 있는 동안에는 H로 열 수 없다(예: 튜토리얼 패널). GameScene에선 비워둠.")]
    [SerializeField] private GameObject blockerPanel;

    private InputAction openAction;
    private bool armed;     // 실제 플레이 중에만 true (모드/캐릭터 선택 중에는 H 무시)

    public bool IsOpen => panel != null && panel.activeSelf;
    private bool IsBlocked => blockerPanel != null && blockerPanel.activeSelf;

    private void Awake()
    {
        if (panel == null) panel = gameObject;

        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);

        // InputAction은 GameObject 활성 상태와 무관하게 살아있어야 한다.
        // (panel이 자기 자신일 때, 꺼진 뒤에도 H로 다시 켤 수 있어야 하므로
        //  Enable/Disable을 OnEnable/OnDisable이 아니라 Awake/OnDestroy에 둔다.)
        openAction = new InputAction(binding: "<Keyboard>/h");
        openAction.performed += _ => Toggle();
        openAction.Enable();

        panel.SetActive(false);
    }

    private void OnDestroy() => openAction?.Dispose();

    // blockerPanel(튜토리얼 패널 등)이 떠 있으면 열려 있던 가이드를 강제로 닫는다.
    private void Update()
    {
        if (IsOpen && IsBlocked) Hide();
    }

    private void Toggle()
    {
        if (!armed) return;     // arm 전(선택 화면, 튜토리얼 윷 가이드 전)에는 H 무시
        if (IsBlocked) return;  // 튜토리얼 패널이 떠 있는 동안에는 못 띄움
        if (IsOpen) Hide();
        else Show();
    }

    // 실제 플레이 시작/종료 시 GameMaster가 호출한다.
    public void Arm() => armed = true;
    public void Disarm()
    {
        armed = false;
        Hide();
    }

    public void Show() => panel.SetActive(true);
    public void Hide() => panel.SetActive(false);
}
