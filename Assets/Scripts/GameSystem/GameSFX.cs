using UnityEngine;

/// <summary>
/// GameEvents를 구독해 효과음을 재생한다. (VFXManager의 사운드 버전)
/// 새 효과음은 클립 필드 + 이벤트 구독만 추가하면 된다.
/// </summary>
public class GameSFX : MonoBehaviour
{
    [SerializeField] private AudioClip[] yutThrowClips;
    [SerializeField] private AudioClip pieceMoveClip;
    [SerializeField] private AudioClip sacrificeLoopClip;   // 제물 선택 대기 중 지속 재생
    [SerializeField] private AudioClip sacrificePickedClip; // 제물 선택 완료 시 1회 재생

    private int lastThrowIndex = -1;

    private void OnEnable()
    {
        GameEvents.OnYutLanded += HandleYutLanded;
        GameEvents.OnPieceMoved += HandlePieceMoved;
        GameEvents.OnSacrificePickStart += HandleSacrificePickStart;
        GameEvents.OnSacrificePickEnd += HandleSacrificePickEnd;
    }

    private void OnDisable()
    {
        GameEvents.OnYutLanded -= HandleYutLanded;
        GameEvents.OnPieceMoved -= HandlePieceMoved;
        GameEvents.OnSacrificePickStart -= HandleSacrificePickStart;
        GameEvents.OnSacrificePickEnd -= HandleSacrificePickEnd;
    }

    private void HandleYutLanded(int playerId)
        => PlayRandom(yutThrowClips, ref lastThrowIndex);

    private void HandlePieceMoved(int playerId)
        => SoundManager.Instance?.PlaySFX(pieceMoveClip);

    private void HandleSacrificePickStart()
        => SoundManager.Instance?.PlaySustained(sacrificeLoopClip);

    private void HandleSacrificePickEnd()
    {
        SoundManager.Instance?.StopSustained();
        SoundManager.Instance?.PlaySFX(sacrificePickedClip);
    }

    // 랜덤 재생하되 직전 클립의 연속 재생은 피한다.
    private void PlayRandom(AudioClip[] clips, ref int lastIndex)
    {
        if (clips == null || clips.Length == 0) return;
        int i = Random.Range(0, clips.Length);
        if (clips.Length > 1 && i == lastIndex)
            i = (i + 1) % clips.Length;
        lastIndex = i;
        SoundManager.Instance?.PlaySFX(clips[i]);
    }
}
