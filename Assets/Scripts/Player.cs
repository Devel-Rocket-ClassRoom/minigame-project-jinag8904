using System.Collections.Generic;
using UnityEngine;

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

    // 말
    public Piece[] pieces = new Piece[4];
    private int finishedPiecesCount = 0;  // 완주
    public bool AllFinished => finishedPiecesCount >= 4;

    // 이동 가능한 수 리스트(도/개/걸/윷/모)
    public List<YutResult> yutResults = new();

    // 검은 윷
    private int blackYutCount = 0;
    public bool HasBlackYut => blackYutCount > 0;

    // 원한
    private int wonhan = 0;

    public Player()
    {
        for (int i = 0; i < pieces.Length; i++)
        {
            pieces[i] = new Piece { owner = this };
        }
    }

    public void Init()
    {
        for (int i = 0; i < pieces.Length; i++)
        {
            pieces[i].currentNode = null;
            pieces[i].previousNode = null;
            pieces[i].hasFinished = false;
        }
        yutResults.Clear();
    }

    public void OnThrowBlackYut()
    {
        Throw(isBlackYut: true);
    }

    public void Throw(bool isCaptureBonus = false, bool isBlackYut = false)
    {
        if (isBlackYut)
        {
            yutResults.Add(ThrowYut.Throw(true));
            blackYutCount--;
            return;
        }

        yutResults.Add(ThrowYut.Throw());

        if (isCaptureBonus) return;

        // 윷이나 모가 나오면 한 번 더
        while (yutResults[^1] == YutResult.Yut || yutResults[^1] == YutResult.Mo)
        {           
            Throw();
        }
    }

    public void OnCaught(Piece piece)
    {
        var nodeName = piece.currentNode.data.nodeName;
        var isExtraWonhan = nodeName == "날윷" || nodeName == "안찌" || nodeName == "참먹이";

        wonhan += (!isExtraWonhan) ? 3 : 5;

        if (wonhan >= 5)
        {
            wonhan -= 5;
            blackYutCount++;
        }
    }
}