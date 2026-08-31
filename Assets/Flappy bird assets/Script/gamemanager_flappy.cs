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
    private Coroutine countdownRoutine;
    private Pipe_spawner pipeSpawner;

    [SerializeField] private TMP_Text scoreTextCounter;
    [SerializeField] private GameObject scoreLabel;
    [SerializeField] private TMP_Text currentScoreLabel;
    [SerializeField] private TMP_Text highScoreLabel ;
    [SerializeField] private GameObject startbutton;
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private int countdownSeconds = 3;

    [Header("Speed up banner")]
    [SerializeField] private float speedUpFlashDuration = 2f;
    [SerializeField] private float speedUpFontSize = 88f;
    [SerializeField] private float speedUpBlinkHz = 3f;

    [Header("Idle lobby (ก่อนเริ่มเกม)")]
    [SerializeField] private float idleMinY = -4f;
    [SerializeField] private float idleMaxY = 4f;

    private float countdownDefaultFontSize = 96f;
    private FontStyles countdownDefaultFontStyle = FontStyles.Bold;
    private Color countdownDefaultColor = Color.white;
    private bool countdownStyleCached;

    public bool IsPlaying { get; private set; }
    public bool ShouldClampIdleFlight => !IsPlaying && !isGameOver;
    public float IdleMinY => idleMinY;
    public float IdleMaxY => idleMaxY;

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
        if (pipeSpawner != null)
            pipeSpawner.SetSpawningPaused(true);
    }

    private void Start()
    {
        EnterIdleLobby();
    }
    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
    private bool isGameOver;

    public void GameOver()
    {
         if (!IsPlaying || isGameOver)
            return;

         isGameOver = true;
         IsPlaying = false;
         StopDifficultyRoutine();
         Time.timeScale = 0f;

         gameOverText.gameObject.SetActive(true);
         playButton.SetActive(true);
         if (startbutton != null)
             startbutton.SetActive(false);
         SetCountdownVisible(false);

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
        if (countdownRoutine != null)
            return;

        scoreLabel.SetActive(false);
        Debug.Log("Start Game logic inside game manager script");

        gameOverText.gameObject.SetActive(false);
        playButton.SetActive(false);
        if (startbutton != null)
            startbutton.SetActive(false);

        StopDifficultyRoutine();
        clearPipes();
        resetscore();
        ResetDifficulty();
        ResetPlayer();

        isGameOver = false;
        IsPlaying = false;
        Time.timeScale = 1f;

        countdownRoutine = StartCoroutine(CountdownThenPlay());
    }

    private IEnumerator CountdownThenPlay()
    {
        SetCountdownVisible(true);

        int seconds = Mathf.Max(1, countdownSeconds);
        for (int i = seconds; i >= 1; i--)
        {
            SetCountdownText(i.ToString());
            yield return new WaitForSeconds(1f);
        }

        SetCountdownText("GO!");
        yield return new WaitForSeconds(0.5f);

        SetCountdownVisible(false);
        countdownRoutine = null;
        BeginMatch();
    }

    private void SetCountdownText(string value)
    {
        if (countdownText != null)
            countdownText.text = value;
    }

    private void SetCountdownVisible(bool visible)
    {
        if (countdownText == null)
            return;

        if (!visible)
            RestoreCountdownStyle();

        countdownText.gameObject.SetActive(visible);
    }

    private void CacheCountdownStyle()
    {
        if (countdownText == null || countdownStyleCached)
            return;

        countdownDefaultFontSize = countdownText.fontSize;
        countdownDefaultFontStyle = countdownText.fontStyle;
        countdownDefaultColor = countdownText.color;
        countdownStyleCached = true;
    }

    private void RestoreCountdownStyle()
    {
        if (countdownText == null || !countdownStyleCached)
            return;

        countdownText.fontSize = countdownDefaultFontSize;
        countdownText.fontStyle = countdownDefaultFontStyle;
        countdownText.color = countdownDefaultColor;
    }

    private void SetCountdownAlpha(float alpha)
    {
        if (countdownText == null)
            return;

        Color c = countdownText.color;
        c.a = alpha;
        countdownText.color = c;
    }

    private void EnterIdleLobby()
    {
        IsPlaying = false;
        isGameOver = false;
        Time.timeScale = 1f;
        StopDifficultyRoutine();
        clearPipes();
        resetscore();
        ResetDifficulty();
        ResetPlayer();

        if (gameOverText != null)
            gameOverText.SetActive(false);
        if (playButton != null)
            playButton.SetActive(false);
        if (scoreLabel != null)
            scoreLabel.SetActive(false);
        if (startbutton != null)
            startbutton.SetActive(true);
        SetCountdownVisible(false);
    }

    private void BeginMatch()
    {
        IsPlaying = true;
        if (pipeSpawner != null)
            pipeSpawner.StartWave();
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
        if (Pipe_spawner == null)
            return;

        foreach (var o in Pipe_spawner.GetComponentsInChildren<Pipe_movement>())
        {
            Destroy(o.gameObject);
        }

    }
    public void addScore()
    {
        if (!IsPlaying)
            return;

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

        yield return ShowSpeedUpThenCountdown();

        IncreaseDifficulty();

        if (pipeSpawner != null)
            pipeSpawner.StartWave();

        difficultyRoutine = null;
    }

    private IEnumerator ShowSpeedUpThenCountdown()
    {
        if (countdownText == null)
        {
            yield return new WaitForSeconds(Mathf.Max(0.1f, difficultyPauseDuration));
            yield break;
        }

        CacheCountdownStyle();
        countdownText.fontStyle = FontStyles.Bold;
        countdownText.fontSize = speedUpFontSize;
        countdownText.gameObject.SetActive(true);
        countdownText.text = "SPEED UP";
        SetCountdownAlpha(1f);

        float elapsed = 0f;
        float duration = Mathf.Max(0.4f, speedUpFlashDuration);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float wave = (Mathf.Sin(elapsed * speedUpBlinkHz * Mathf.PI * 2f) + 1f) * 0.5f;
            SetCountdownAlpha(Mathf.Lerp(0.2f, 1f, wave));
            yield return null;
        }

        SetCountdownAlpha(1f);

        int seconds = Mathf.Max(1, countdownSeconds);
        for (int i = seconds; i >= 1; i--)
        {
            countdownText.fontStyle = FontStyles.Bold;
            countdownText.fontSize = speedUpFontSize;
            countdownText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }

        SetCountdownVisible(false);
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
