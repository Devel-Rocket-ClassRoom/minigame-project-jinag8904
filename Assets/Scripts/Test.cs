using UnityEngine;

public class Test : MonoBehaviour
{
    Player player = new();

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            player.Throw();
        }
    }
}