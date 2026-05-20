using System.Collections.Generic;

public class Piece
{
    public PieceObject pieceObject;
    public Player owner;

    public BoardNode currentNode;
    public Stack<BoardNode> nodeHistory = new();
    public BoardNode previousNode => nodeHistory.Count > 0 ? nodeHistory.Peek() : null;

    public bool hasFinished;

    public List<Piece> stackedPieces = new();
    public Piece stackLeader;  // null이면 이 말이 리더(또는 독립)
}
