using System;

public static class GameEvents
{
    public static event Action<int> OnYutThrown;
    public static event Action<int> OnCaptureSuccess;
    public static event Action<int> OnCaptureFailed;
    public static event Action<int> OnPieceFinished;

    public static void InvokeYutThrown(int id)      => OnYutThrown?.Invoke(id);
    public static void InvokeCaptureSuccess(int id) => OnCaptureSuccess?.Invoke(id);
    public static void InvokeCaptureFailed(int id)  => OnCaptureFailed?.Invoke(id);
    public static void InvokePieceFinished(int id)  => OnPieceFinished?.Invoke(id);
}
