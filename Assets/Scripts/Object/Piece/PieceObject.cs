using Unity.VisualScripting;
using UnityEngine;

public class PieceObject : MonoBehaviour
{
    public Piece piece;
    [HideInInspector] public Vector3 initPosition;
    [SerializeField] private GameObject hightlight;

    public void Bind(Piece p)
    {
        piece = p;
        SetHighLight(false);
    }
    
    public void SetHighLight(bool active)
    {
        if (hightlight != null) hightlight.SetActive(active);
    }
}
