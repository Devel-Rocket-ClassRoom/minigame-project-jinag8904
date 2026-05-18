using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class GameMaster : MonoBehaviour
{
    // 플레이어
    private Player[] players = new Player[2];
    private Player currPlayer;
    public Player CurrPlayer => currPlayer;

    // UI
    [SerializeField] private Button blackYutButton;

    private void Awake()
    {
        for (int i = 0; i < players.Length; i++)
            players[i] = new Player() { playerId = i };

        blackYutButton.gameObject.SetActive(false);
        blackYutButton.onClick.AddListener(() =>
        {
            currPlayer.Throw(isBlackYut: true);
            if (!currPlayer.HasBlackYut) blackYutButton.gameObject.SetActive(false);
        });
    }

    private void Init()
    {
        foreach (var player in players)
            player.Init();
    }

    private void Start()
    {
        Debug.Log("게임 시작");
        StartCoroutine(CoRunGame());
    }

    private IEnumerator CoRunGame()
    {
        Init();
        yield return StartCoroutine(CoWhoGoesFirst());
        yield return StartCoroutine(CoPlayGame());
        yield return StartCoroutine(CoEndGame());
    }

    private IEnumerator CoWhoGoesFirst()
    {
        yield return new WaitForSeconds(1);

        Debug.Log("<color=green>순서 정하는 중...</color>");
        currPlayer = players[Random.Range(0, 2)];

        yield return new WaitForSeconds(3);

        Debug.Log($"<color=yellow>{currPlayer}부터 시작</color>");
    }

    private IEnumerator CoPlayGame()
    {
        while (!players.Any(p => p.AllFinished))
        {
            yield return new WaitForSeconds(1);
            yield return StartCoroutine(CoHandlePlayerTurn(currPlayer));
            SwitchTurn();
        }
    }

    private IEnumerator CoHandlePlayerTurn(Player player)
    {
        yield return new WaitForSeconds(1);

        Debug.Log("<color=green>윷 던지는 중...</color>");
        player.Throw();

        yield return new WaitForSeconds(3);

        StringBuilder sb = new();
        var yrs = player.yutResults;
        sb.Append("결과: ");
        for (int i = 0; i < yrs.Count; i++)
        {
            if (i != 0) sb.Append(" / ");
            sb.Append($"{yrs[i]}");
        }
        Debug.Log(sb.ToString());

        if (player.HasBlackYut) blackYutButton.gameObject.SetActive(true);

        // TODO: MouseInput 연동 후 break 제거하고 실제 입력 대기로 교체
        while (player.yutResults.Count > 0)
        {
            yield return null;
            break;
        }
    }

    private IEnumerator CoEndGame()
    {
        Debug.Log("게임 끝");
        yield return null;
    }

    private void SwitchTurn()
    {
        currPlayer = players[1 - currPlayer.playerId];
    }
}
