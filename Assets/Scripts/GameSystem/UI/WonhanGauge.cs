using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class WonhanGauge : MonoBehaviour
{
    [SerializeField] private int playerId;                 // 0 = P1, 1 = P2
    [SerializeField] private Image[] pips = new Image[5];  // 좌→우 구슬 5개
    [SerializeField] private Color emptyColor = new Color(0.035f, 0.04f, 0.05f, 0.75f); // 빈 소켓
    [SerializeField] private float highlightOnAlpha = 0.6f;                              // 채워졌을 때 광택 세기

    private Color _fillColor = Color.white;   // 캐릭터 테마색 (주입받음)
    private int _displayed;                   // 현재 켜진 구슬 수
    private int _pending = -1;                // 터짐 연출 중 들어온 값 보류
    private bool _bursting;

    private void Awake() => ApplyFill(0);

    private void OnEnable()
    {
        GameEvents.OnWonhanChanged += HandleWonhanChanged;
        GameEvents.OnBlackYutObtained += HandleBlackYut;
    }
    private void OnDisable()
    {
        GameEvents.OnWonhanChanged -= HandleWonhanChanged;
        GameEvents.OnBlackYutObtained -= HandleBlackYut;
    }

    public void SetColor(Color c)
    {
        const float deepen = 0.8f;   // 분위기에 맞게 살짝 진하게 (배너/파티클 색은 그대로)
        _fillColor = new Color(c.r * deepen, c.g * deepen, c.b * deepen, 1f);
        ApplyFill(_displayed);
    }

    private void HandleWonhanChanged(int id, int wonhan)
    {
        if (id != playerId) return;
        if (_bursting) { _pending = wonhan; return; }   // 터짐 끝나고 반영
        FillTo(wonhan);
    }

    private void HandleBlackYut(int id)
    {
        if (id != playerId) return;
        StartCoroutine(CoBurst());
    }

    private void FillTo(int wonhan)
    {
        int target = Mathf.Clamp(wonhan, 0, 5);
        for (int i = 0; i < pips.Length; i++)
        {
            if (pips[i] == null) continue;
            bool on = i < target;
            Light(i, on);
            if (on && i >= _displayed) Pop(pips[i].transform, 0.5f, 0.3f);  // 새로 켜진 것만 팝
        }
        _displayed = target;
    }

    private void ApplyFill(int count)
    {
        for (int i = 0; i < pips.Length; i++)
            Light(i, i < count);
        _displayed = Mathf.Clamp(count, 0, 5);
    }

    // 구슬 on/off: 채워지면 테마색+광택, 비면 어두운 소켓+광택 끔
    private void Light(int i, bool on)
    {
        if (pips[i] == null) return;
        pips[i].color = on ? _fillColor : emptyColor;
        var t = pips[i].transform;
        if (t.childCount > 0)
        {
            var hl = t.GetChild(0).GetComponent<Image>();
            if (hl != null) { var c = hl.color; c.a = on ? highlightOnAlpha : 0f; hl.color = c; }
        }
    }

    private IEnumerator CoBurst()
    {
        _bursting = true;
        for (int i = 0; i < pips.Length; i++)
        {
            if (pips[i] == null) continue;
            Light(i, true);
            Pop(pips[i].transform, 0.8f, 0.4f);
        }
        yield return new WaitForSeconds(0.45f);
        ApplyFill(0);                 // 비우기
        _bursting = false;
        if (_pending >= 0) { FillTo(_pending); _pending = -1; }
    }

    private static void Pop(Transform t, float strength, float dur)
    {
        t.DOKill();
        t.localScale = Vector3.one;
        t.DOPunchScale(Vector3.one * strength, dur, 6, 0.8f);
    }
}