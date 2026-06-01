using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class LocalizedText : MonoBehaviour
{
    [SerializeField] private string key;

    private TextMeshProUGUI label;

    private void Awake()
    {
        label = GetComponent<TextMeshProUGUI>();
        LocalizationManager.OnLanguageChanged += Refresh;
    }

    private void Start() => Refresh();

    private void OnDestroy() => LocalizationManager.OnLanguageChanged -= Refresh;

    private void Refresh()
    {
        if (!string.IsNullOrEmpty(key))
            label.text = LocalizationManager.Get(key);
    }
}
