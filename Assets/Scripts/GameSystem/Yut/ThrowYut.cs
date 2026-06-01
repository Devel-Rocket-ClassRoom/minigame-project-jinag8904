using System.Collections.Generic;
using UnityEngine;

public class ThrowYut : MonoBehaviour
{
    public static Queue<YutResult> ForcedResults = new();

    public class Yut
    {
        public bool isTail; // 뒷면인가?

        public bool Throw()
        {
            isTail = Random.Range(0, 2) == 1;
            return isTail;
        }
    }

    private static Yut[] yuts = new Yut[4] { new(), new(), new(), new() };

    private static int tailCount = 0;

    public static YutResult Throw(bool isBlackYut = false)
    {
        if (isBlackYut) // 1:4 확률
        {
            return Random.Range(0, 5) == 0 ? YutResult.BACKDO : YutResult.Mo;
        }

        tailCount = 0;
        for (int i = 0; i < yuts.Length; i++)
        {
            yuts[i].Throw();

            if (yuts[i].isTail)
            {
                tailCount++;
            }
        }

        switch (tailCount)
        {
            case 1:
                if (yuts[3].isTail) return YutResult.BACKDO;
                else                return YutResult.Do;                
            case 2:
                return YutResult.Gae;
            case 3:
                return YutResult.Geol;
            case 4:
                return YutResult.Yut;
            case 0:
                return YutResult.Mo;
            default:
                throw new System.Exception("Invalid tail count");
        }
    }
}