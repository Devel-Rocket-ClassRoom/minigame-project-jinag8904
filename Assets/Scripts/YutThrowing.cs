using UnityEngine;

public class Yut
{
    public bool isTail; // 뒷면인가?

    public bool Throw()
    {
        isTail = Random.Range(0, 2) == 1;
        return isTail;
    }
}

public class YutThrowing : MonoBehaviour
{
    private Yut[] yuts = new Yut[4];

    private int tailCount = 0;

    private void Awake()
    {
        yuts[0] = new Yut();
        yuts[1] = new Yut();
        yuts[2] = new Yut();
        yuts[3] = new Yut();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Throw();
        }
    }

    public void Throw()
    {
        tailCount = 0;

        for (int i = 0; i < yuts.Length; i++)
        {
            yuts[i].Throw();

            if (yuts[i].isTail)
            {
                tailCount++;
            }

            Debug.Log(yuts[i].isTail ? "뒤" : "앞");
        }

        switch (tailCount)
        {
            case 1:
                if (yuts[3].isTail)
                {
                    Debug.Log("뒷도");
                }
                else
                {
                    Debug.Log("도");
                }
                break;
            case 2:
                Debug.Log("개");
                break;
            case 3:
                Debug.Log("걸");
                break;
            case 4:
                Debug.Log("윷");
                break;
            case 0:
                Debug.Log("모");
                break;
        }

        Debug.Log("ㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡㅡ");
    }
}