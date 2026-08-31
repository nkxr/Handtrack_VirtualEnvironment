using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Playerhit : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
      if (gamemanager_flappy.instance == null || !gamemanager_flappy.instance.IsPlaying)
          return;

      gamemanager_flappy.instance.GameOver();
    }
}