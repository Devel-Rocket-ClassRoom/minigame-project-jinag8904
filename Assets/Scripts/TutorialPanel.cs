using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TutorialPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private TextMeshProUGUI bodyTextRight;
    [SerializeField] private GameObject yutGuide;   // 윷 결과 가이드 (선택)
    private string _titleKey;
    private string _bodyKey;
    private string _bodyKeyRight;
    private bool _useYutGuide;

    [SerializeField] private Button nextButton;
    private bool clicked = false;

    public RectTransform NextButtonRect => nextButton.GetComponent<RectTransform>();

    private void Awake()
    {
        gameObject.SetActive(false);
        nextButton.onClick.AddListener(() => clicked = true);
    }

    private void OnEnable()
    {
        LocalizationManager.OnLanguageChanged += RefreshText;
    }

    private void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= RefreshText;
    }

    public IEnumerator CoShow(string titleKey, string bodyKey, string bodyKeyRight = null, bool useYutGuide = false)
    {
        clicked = false;

        _titleKey = titleKey;
        _bodyKey = bodyKey;
        _bodyKeyRight = bodyKeyRight;
        _useYutGuide = useYutGuide;
        RefreshText();

        gameObject.SetActive(true);

        yield return new WaitUntil(() => clicked);

        gameObject.SetActive(false);
    }

    private void RefreshText()
    {
        titleText.text = LocalizationManager.Get(_titleKey);
        if (yutGuide != null) yutGuide.SetActive(_useYutGuide);

        bool showBody = !_useYutGuide;                 // 가이드 모드면 Body 텍스트 숨김
        bodyText.gameObject.SetActive(showBody);
        if (showBody) bodyText.text = LocalizationManager.Get(_bodyKey);

        if (bodyTextRight != null)
        {
            bool hasRight = showBody && !string.IsNullOrEmpty(_bodyKeyRight);
            bodyTextRight.gameObject.SetActive(hasRight);
            if (hasRight)
                bodyTextRight.text = LocalizationManager.Get(_bodyKeyRight);
        }
    }
}