public class Piece
{
    public BoardNodeData currentNode;
    public BoardNodeData previousNode;  // 뒷도 처리 용도
    public Player owner;
    public bool hasFinished;
}