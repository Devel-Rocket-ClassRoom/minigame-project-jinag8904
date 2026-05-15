using System.Collections.Generic;

public enum YutResult
{
    BACKDO = -1,
    Do = 1,
    Gae,
    Geol,
    Yut,
    Mo
}

public class Player
{
    public int playerId;

    // 가진 말
    private Piece[] pieces = new Piece[4];

    private int finishedPiecesCount = 0;  // 완주한 말 개수
   
    public bool Completed => finishedPiecesCount >= 4;

    // 이동 가능한 수 리스트(도/개/걸/윷/모)
    public List<YutResult> yutResults = new();

    // 원한 게이지
    private int WonhanGauge = 0;

    // 검은 윷 사용 가능 횟수
    private int blackYutCount = 0;

    private void Awake()
    {
        for (int i = 0; i < pieces.Length; i++)
        {
            pieces[i].owner = this;
        }
    }

    public void Init()
    {
        for (int i = 0; i < pieces.Length; i++)
        {
            pieces[i].hasFinished = false;
        }
    }
}