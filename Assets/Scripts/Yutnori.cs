using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Yutnori : MonoBehaviour    // 대부분의 핵심 로직 담당
{
    // 플레이어
    private Player[] players = new Player[2];
    private Player currPlayer;

    // UI
    public GameObject BlackYutButton;

    private void Awake()
    {
        for (int i = 0; i < players.Length; i++)
        {
            players[i] = new Player() { playerId = i };
        }

        BlackYutButton.SetActive(false);
        BlackYutButton.GetComponent<Button>().onClick.AddListener(() =>
        {
            currPlayer.Throw(isBlackYut: true);

            if (!currPlayer.HasBlackYut) 
                BlackYutButton.SetActive(false);
        });
    }

    public void Init()
    {
        foreach (var player in players)
        {
            player.Init();
        }

        WhoGoesFirst();
    }

    public void WhoGoesFirst()
    {
        currPlayer = players[Random.Range(0, 2)];
    }

    private void SwitchTurn()
    {
        currPlayer = players[1 - currPlayer.playerId];
    }

    public void HandlePlayerTurn()
    {
        StartCoroutine(CoHandlePlayerTurn());
    }

    IEnumerator CoHandlePlayerTurn()
    {
        // 윷을 던진다.
        currPlayer.Throw();

        // 검은 윷이 있다면 UI 활성화
        if (currPlayer.HasBlackYut)
            BlackYutButton.SetActive(true);

        // 말 움직이기 코루틴 시작
        yield return StartCoroutine(CoMovePiece());

        IEnumerator CoMovePiece()
        {


            if (currPlayer.yutResults.Count > 0) StartCoroutine(CoMovePiece());
        }
    }
}