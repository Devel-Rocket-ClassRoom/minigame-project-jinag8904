using System;

[Serializable]
public class MatchRecord
{
    public bool won;          // 내가 이겼는지
    public string character;  // 내 캐릭터 (귀신/도깨비/물귀신)

    public MatchRecord() { }

    public MatchRecord(bool won, string character)
    {
        this.won = won;
        this.character = character;
    }
}