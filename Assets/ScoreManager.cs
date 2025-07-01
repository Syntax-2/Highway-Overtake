using UnityEngine;
using TMPro; // Required for TextMeshPro components
using System.Collections; // Required for Coroutines

public class ScoreManager : MonoBehaviour
{
    // --- Singleton Instance ---
    public static ScoreManager Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("Assign the TextMeshProUGUI component that will display the score.")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI CollectedText;
    public TextMeshProUGUI GivingUpCollectedText;

    // --- NEW: Score Pop Effect Settings ---
    [Header("Juice Settings")]
    [Tooltip("How much larger the score text gets when a point is added.")]
    public float popScale = 1.5f;
    [Tooltip("How quickly the text animates back to its normal size.")]
    public float popSpeed = 10f;

    // --- Score Tracking ---
    public static int _score = 0;

    // --- NEW: Private variables for the animation ---
    private Coroutine _popCoroutine;
    private Vector3 _originalScoreScale;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
            // DontDestroyOnLoad(this.gameObject); // Uncomment if this needs to persist
        }
    }

    private void Start()
    {
        _score = 0;
        // --- NEW: Store the original scale of the score text ---
        if (scoreText != null)
        {
            _originalScoreScale = scoreText.transform.localScale;
        }
        UpdateScoreText();
    }

    public void AddOnePoint()
    {
        _score += 1;
        UpdateScoreText();
        TriggerPopEffect(); // --- NEW: Trigger the animation
    }

    public void AddScore(int amount)
    {
        if (amount < 0) return;
        _score += amount;
        UpdateScoreText();
        TriggerPopEffect(); // --- NEW: Trigger the animation
    }

    public int GetScore()
    {
        return _score;
    }

    public void ResetScore()
    {
        _score = 0;
        UpdateScoreText();
    }

    private void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + _score;
        }
        if (CollectedText != null)
        {
            CollectedText.text = _score + "$";
        }
        if (GivingUpCollectedText != null)
        {
            GivingUpCollectedText.text = _score + "$";
        }
    }

    // --- NEW: Methods for the Pop Effect ---

    /// <summary>
    /// Starts the pop animation coroutine.
    /// </summary>
    private void TriggerPopEffect()
    {
        if (scoreText == null) return;

        // If a pop animation is already running, stop it first to restart it.
        if (_popCoroutine != null)
        {
            StopCoroutine(_popCoroutine);
        }
        // Start the new animation.
        _popCoroutine = StartCoroutine(PopEffectCoroutine());
    }

    /// <summary>
    /// Coroutine that handles the scale animation of the score text.
    /// </summary>
    private IEnumerator PopEffectCoroutine()
    {
        // Immediately scale up
        scoreText.transform.localScale = _originalScoreScale * popScale;

        // Gradually scale back down to the original size
        while (scoreText.transform.localScale.x > _originalScoreScale.x)
        {
            scoreText.transform.localScale = Vector3.Lerp(
                scoreText.transform.localScale,
                _originalScoreScale,
                Time.deltaTime * popSpeed
            );
            yield return null; // Wait for the next frame
        }

        // Ensure it ends exactly at the original scale
        scoreText.transform.localScale = _originalScoreScale;
        _popCoroutine = null; // Mark the coroutine as finished
    }
}
