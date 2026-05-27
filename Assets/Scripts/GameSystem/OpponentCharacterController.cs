using UnityEngine;

public class OpponentCharacterController : MonoBehaviour
{
    [SerializeField] private int opponentCharacterId = 1;
    private Animator _animator;

    void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    void OnEnable()
    {
        GameEvents.OnYutThrown      += HandleYutThrown;
        GameEvents.OnCaptureSuccess += HandleCaptureSuccess;
        GameEvents.OnCaptureFailed  += HandleCaptureFailed;
        GameEvents.OnPieceFinished  += HandlePieceFinished;
    }

    void OnDisable()
    {
        GameEvents.OnYutThrown      -= HandleYutThrown;
        GameEvents.OnCaptureSuccess -= HandleCaptureSuccess;
        GameEvents.OnCaptureFailed  -= HandleCaptureFailed;
        GameEvents.OnPieceFinished  -= HandlePieceFinished;
    }

    void HandleYutThrown(int id)
    {
        if (id != opponentCharacterId) return;
        _animator.SetTrigger("YutThrown");
    }

    void HandleCaptureSuccess(int id)
    {
        if (id != opponentCharacterId) return;
        _animator.SetTrigger("CaptureSuccess");
    }

    void HandleCaptureFailed(int id)
    {
        if (id != opponentCharacterId) return;
        _animator.SetTrigger("CaptureFailed");
    }

    void HandlePieceFinished(int id)
    {
        if (id != opponentCharacterId) return;
        _animator.SetTrigger("PieceFinished");
    }
}