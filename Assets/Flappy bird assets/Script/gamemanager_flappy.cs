using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class gamemanager_flappy : MonoBehaviour
{
    public static gamemanager_flappy instance {get; private set;}

    private const  string HighScoreKey = "Highscore";

    [SerializeField] private GameObject playButton;
    [SerializeField] private GameObject gameOverText;
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject Pipe_spawner;

    [Header("Difficulty")]
    [SerializeField] private float defaultPipeSpeed = 5f;
    [SerializeField] private float pipeSpacing = 10f;
    [SerializeField] private int scorePerDifficultyBump = 10;
    [SerializeField] private float pipeSpeedMultiplier = 1.2f;
    [SerializeField] private float difficultyPauseDuration = 5f;

    public float PipeSpeed { get; private set; }
    public float PipeSpacing => pipeSpacing;
    public int PipesPerWave => scorePerDifficultyBump;
    public int score {get; private set;}
    public int highScore {get; private set;}

    private Coroutine difficultyRoutine;
    private Pipe_spawner pipeSpawner;

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
        highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        if (Pipe_spawner != null)
            pipeSpawner = Pipe_spawner.GetComponent<Pipe_spawner>();
        ResetDifficulty();
    }
    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
    private bool isGameOver;

    public void GameOver()
    {
         if (isGameOver)
            return;

         isGameOver = true;
         StopDifficultyRoutine();
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

        gameOverText.gameObject.SetActive(false);
        playButton.SetActive(false);

        StopDifficultyRoutine();
        clearPipes();
        resetscore();
        ResetDifficulty();
        ResetPlayer();

        isGameOver = false;
        Time.timeScale = 1f;
    }

    private void ResetPlayer()
    {
        var controller = player.GetComponent<PlayerController>();
        if (controller != null)
            controller.ResetState();
        else
            player.transform.SetPositionAndRotation(new Vector3(-5f, 0f, 0f), Quaternion.identity);
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

        if (score > 0 && score % scorePerDifficultyBump == 0)
            difficultyRoutine = StartCoroutine(PauseThenIncreaseDifficulty());
    }

    private IEnumerator PauseThenIncreaseDifficulty()
    {
        if (pipeSpawner != null)
            pipeSpawner.SetSpawningPaused(true);

        yield return new WaitForSeconds(difficultyPauseDuration);

        IncreaseDifficulty();

        if (pipeSpawner != null)
            pipeSpawner.StartWave();

        difficultyRoutine = null;
    }

    private void StopDifficultyRoutine()
    {
        if (difficultyRoutine != null)
        {
            StopCoroutine(difficultyRoutine);
            difficultyRoutine = null;
        }

        if (pipeSpawner != null)
            pipeSpawner.SetSpawningPaused(true);
    }

    private void ResetDifficulty()
    {
        PipeSpeed = defaultPipeSpeed;
        if (pipeSpawner != null)
            pipeSpawner.StartWave();
    }

    private void IncreaseDifficulty()
    {
        PipeSpeed *= pipeSpeedMultiplier;
    }
    private void updateHighScore()
    {
        if(score <= highScore )return;

        highScore = score ;
        PlayerPrefs.SetInt(HighScoreKey,highScore);
        PlayerPrefs.Save();
    }
}