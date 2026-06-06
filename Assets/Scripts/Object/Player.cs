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
    public string name;

    // AI, 캐릭터
    public bool isAI;
    public CharacterData characterData;
    public CharacterSkill Skill => characterData?.skill;

    // 말
    public Piece[] pieces = new Piece[4];
    private int finishedPiecesCount = 0;  // 완주
    public bool AllFinished => finishedPiecesCount >= 4;
    public int FinishedCount => finishedPiecesCount;

    // 이동 가능한 수 리스트(도/개/걸/윷/모)
    public List<YutResult> yutResults = new();

    // 검은 윷
    private int blackYutCount = 0;
    public int BlackYutCount => blackYutCount;
    public bool HasBlackYut => blackYutCount > 0;

    // 원한
    private int wonhan = 0;

    public int activeSkillUseCount;

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
            pieces[i].hasFinished = false;
            pieces[i].currentNode = null;
            pieces[i].nodeHistory.Clear();
            pieces[i].stackLeader = null;
            pieces[i].stackedPieces.Clear();
        }
        yutResults.Clear();
        finishedPiecesCount = 0;
        activeSkillUseCount = 0;

        wonhan = 0; blackYutCount = 0;
    }

    public void FinishPiece(Piece piece)
    {
        piece.hasFinished = true;
        finishedPiecesCount++;
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
    }

    public void AddThrowResult(YutResult result)
    {
        yutResults.Add(result);
    }

    // 검은 윷 사용 (개수 차감 + 이벤트) — 던지기 연출 전 클릭 시점에 호출.
    // 던진 결과는 연출 후 AddThrowResult로 따로 반영.
    public void ConsumeBlackYut()
    {
        blackYutCount--;
        GameEvents.InvokeBlackYutUsed(playerId);
    }

    public void OnCaught(Piece piece)
    {
        var nodeName = piece.currentNode.data.nodeName;
        var isExtraWonhan = nodeName == "날윷" || nodeName == "안찌" || nodeName == "참먹이";
        AddWonhan(!isExtraWonhan ? 3 : 5);
    }

    public void AddBlackYut(int count)
    {
        blackYutCount += count;
        GameEvents.InvokeBlackYutObtained(playerId);
        GameEvents.InvokeWonhanChanged(playerId, wonhan);
    }

    public void AddWonhan(int amount)
    {
        wonhan += amount;
        while (wonhan >= 5)
        {
            wonhan -= 5;
            blackYutCount++;
            GameEvents.InvokeBlackYutObtained(playerId); 
        }
        GameEvents.InvokeWonhanChanged(playerId, wonhan);
    }
}