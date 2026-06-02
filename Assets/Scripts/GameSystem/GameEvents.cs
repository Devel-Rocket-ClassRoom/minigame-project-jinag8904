using System;

public static class GameEvents
{
    public static event Action<int> OnYutThrown;
    public static event Action<int> OnCaptureSuccess;
    public static event Action<int> OnCaptured;
    public static event Action<int> OnPieceFinished;
    public static event Action<int> OnPieceMoved;
    public static event Action<int> OnJunctionReached;
    public static event Action<int> OnPieceStacked;
    public static event Action<int> OnBlackYutObtained;
    public static event Action<int> OnBlackYutUsed;
    public static event Action<int, int> OnWonhanChanged;

    public static void InvokeYutThrown(int id) => OnYutThrown?.Invoke(id);
    public static void InvokeCaptureSuccess(int id) => OnCaptureSuccess?.Invoke(id);
    public static void InvokeCaptured(int id) => OnCaptured?.Invoke(id);
    public static void InvokePieceFinished(int id) => OnPieceFinished?.Invoke(id);
    public static void InvokePieceMoved(int id) => OnPieceMoved?.Invoke(id);
    public static void InvokeJunctionReached(int id) => OnJunctionReached?.Invoke(id);
    public static void InvokePieceStacked(int id) => OnPieceStacked?.Invoke(id);
    public static void InvokeBlackYutObtained(int id) => OnBlackYutObtained?.Invoke(id);
    public static void InvokeBlackYutUsed(int id) => OnBlackYutUsed?.Invoke(id);
    public static void InvokeWonhanChanged(int id, int wonhan) => OnWonhanChanged?.Invoke(id, wonhan);
}
