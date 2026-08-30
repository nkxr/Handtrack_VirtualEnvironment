using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Playerhit : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
      gamemanager_flappy.instance.GameOver();
    }
}