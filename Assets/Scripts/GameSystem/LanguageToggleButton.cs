using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class LanguageToggleButton : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(Toggle);
        LocalizationManager.OnLanguageChanged += UpdateLabel;
    }

    private void Start() => UpdateLabel();

    private void OnDestroy() => LocalizationManager.OnLanguageChanged -= UpdateLabel;

    private void Toggle()
    {
        var next = LocalizationManager.CurrentLanguage == Language.Korean
            ? Language.English
            : Language.Korean;
        LocalizationManager.SetLanguage(next);
    }

    private void UpdateLabel()
    {
        if (label != null)
            label.text = LocalizationManager.CurrentLanguage == Language.Korean ? "English" : "한국어";
    }
}
