using UnityEngine;

public class Yutnori : MonoBehaviour    // 대부분의 핵심 로직 담당
{
    private int currPlayerId = -1;

    public void Init()
    {
        currPlayerId = -1;
    }

    

    private void SetNextPlayer()
    {
        currPlayerId = Mathf.Clamp(++currPlayerId, 0, 1);
    }
}