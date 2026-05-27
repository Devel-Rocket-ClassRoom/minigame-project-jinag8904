using System;

public static class GameEvents
{
    public static event Action<int> OnYutThrown;
    public static event Action<int> OnCaptureSuccess;
    public static event Action<int> OnCaptured;
    public static event Action<int> OnPieceFinished;

    public static void InvokeYutThrown(int id)      => OnYutThrown?.Invoke(id);
    public static void InvokeCaptureSuccess(int id) => OnCaptureSuccess?.Invoke(id);
    public static void InvokeCaptured(int id)        => OnCaptured?.Invoke(id);
    public static void InvokePieceFinished(int id)  => OnPieceFinished?.Invoke(id);
}
