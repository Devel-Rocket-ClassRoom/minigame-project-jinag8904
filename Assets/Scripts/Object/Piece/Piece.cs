using System.Collections.Generic;

public class Piece
{
    public PieceObject pieceObject;
    public Player owner;

    public BoardNode currentNode;
    public BoardNode previousNode;  // 뒷도 처리 용도

    public bool hasFinished;

    public List<Piece> stackedPieces = new();
    public Piece stackLeader;  // null이면 이 말이 리더(또는 독립)
}