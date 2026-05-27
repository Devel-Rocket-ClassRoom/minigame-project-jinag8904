using UnityEngine;

public class OpponentCharacterController : MonoBehaviour
{
    [SerializeField] private int opponentCharacterId = 1;

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

    void HandleYutThrown(int id)      => Debug.Log($"[Opp] YutThrown by {id}");
    void HandleCaptureSuccess(int id) => Debug.Log($"[Opp] CaptureSuccess by {id}");
    void HandleCaptureFailed(int id)  => Debug.Log($"[Opp] CaptureFailed by {id}");
    void HandlePieceFinished(int id)  => Debug.Log($"[Opp] PieceFinished by {id}");
}