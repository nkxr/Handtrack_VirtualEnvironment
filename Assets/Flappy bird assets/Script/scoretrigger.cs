using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class scoretrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (gamemanager_flappy.instance == null || !gamemanager_flappy.instance.IsPlaying)
            return;

        gamemanager_flappy.instance.addScore();
    }
}
