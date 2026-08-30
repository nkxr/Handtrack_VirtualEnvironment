using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class GameManager : MonoBehaviour
{
    public static GameManager instance {get; private set;}

    private const  string HighScoreKey = "Highscore";

    [SerializeField] private GameObject playButton;
    [SerializeField] private GameObject gameOverText;
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject Pipe_spawner;

     

    public int score {get; private set;}
    public int highScore {get; private set;}

    [SerializeField] private TMP_Text scoreTextCounter;
    [SerializeField] private GameObject scoreLabel;
    [SerializeField] private TMP_Text currentScoreLabel;
    [SerializeField] private TMP_Text highScoreLabel ;
    
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        highScore = PlayerPrefs.GetInt("HighScoreKey", 0);

    }
    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
    public void GameOver()
    {
         Time.timeScale = 0f;

         gameOverText.gameObject.SetActive(true);
         playButton.SetActive(true);

         //update high score
         updateHighScore();

         scoreLabel.SetActive(true);
         currentScoreLabel.text = $"Current Score:  {score}";
         highScoreLabel.text = $"Best Score: {highScore}";
    }
    private void resetscore()
    {
        score=0;
        scoreTextCounter.text = "0";
    }

    public void StartGame()
    {
        scoreLabel.SetActive(false);
        Debug.Log("Start Game logic inside game manager script");
        Time.timeScale = 1f;

        gameOverText.gameObject.SetActive(false);
        playButton.SetActive(false);

        //reset pos
        player.transform.position = new Vector3(-5f, 0f, 0f);
        clearPipes();
        resetscore();
        
    }
    private void clearPipes()
    {
        foreach (var o in Pipe_spawner.GetComponentsInChildren<Pipe_movement>())
        {
            Destroy(o.gameObject);
        }

    }
    public void addScore()
    {
        Debug.Log("Add Score logic inside game manager script");
        score++;
        scoreTextCounter.text = score.ToString();
    }
    private void updateHighScore()
    {
        if(score <= highScore )return;

        highScore = score ;
        PlayerPrefs.SetInt(HighScoreKey,highScore);
        PlayerPrefs.Save();
    }
}