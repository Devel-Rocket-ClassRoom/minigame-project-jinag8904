using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TutorialPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private TextMeshProUGUI bodyTextRight;
    private string _titleKey;
    private string _bodyKey;
    private string _bodyKeyRight;

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

    public IEnumerator CoShow(string titleKey, string bodyKey, string bodyKeyRight = null)
    {
        clicked = false;

        _titleKey = titleKey;
        _bodyKey = bodyKey;
        _bodyKeyRight = bodyKeyRight;
        RefreshText();

        gameObject.SetActive(true);

        yield return new WaitUntil(() => clicked);

        gameObject.SetActive(false);
    }

    private void RefreshText()
    {
        titleText.text = LocalizationManager.Get(_titleKey);
        bodyText.text = LocalizationManager.Get(_bodyKey);
        if (bodyTextRight != null)
        {
            var hasRight = !string.IsNullOrEmpty(_bodyKeyRight);
            bodyTextRight.gameObject.SetActive(hasRight);
            if (hasRight)
                bodyTextRight.text = LocalizationManager.Get(_bodyKeyRight);
        }
    }
}