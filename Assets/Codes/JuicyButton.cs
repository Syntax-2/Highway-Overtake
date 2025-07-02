using UnityEngine;
using UnityEngine.EventSystems; // Required for event handlers

[RequireComponent(typeof(RectTransform))]
public class JuicyButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Animation Settings")]
    [Tooltip("How much the button scales down when pressed.")]
    public float pressScale = 0.95f;
    [Tooltip("How much the button scales up when hovered over.")]
    public float hoverScale = 1.05f;
    [Tooltip("How quickly the button animates to its target scale.")]
    public float animationSpeed = 15f;

    [Header("Sound Effects (Optional)")]
    [Tooltip("Sound to play when the button is pressed down.")]
    public AudioClip pressSound;
    [Tooltip("Sound to play when the button is released.")]
    public AudioClip releaseSound;
    [Tooltip("Sound to play when the mouse hovers over the button.")]
    public AudioClip hoverSound;

    // A shared AudioSource to prevent creating too many components
    private static AudioSource _uiAudioSource;

    private RectTransform _rectTransform;
    private Vector3 _originalScale;
    private Vector3 _targetScale;
    private bool _isPointerOver = false;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _originalScale = _rectTransform.localScale;
        _targetScale = _originalScale;

        // Find or create a shared AudioSource for UI sounds
        if (_uiAudioSource == null)
        {
            GameObject audioObj = GameObject.Find("UIAudioSource");
            if (audioObj == null)
            {
                audioObj = new GameObject("UIAudioSource");
                _uiAudioSource = audioObj.AddComponent<AudioSource>();
                // Optional: if this manager persists, make its audio source persist too
                // DontDestroyOnLoad(audioObj);
            }
            else
            {
                _uiAudioSource = audioObj.GetComponent<AudioSource>();
            }
        }
    }

    private void Update()
    {
        // Smoothly animate the button's scale towards its target scale
        _rectTransform.localScale = Vector3.Lerp(_rectTransform.localScale, _targetScale, Time.unscaledDeltaTime * animationSpeed);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _targetScale = _originalScale * pressScale;
        PlaySound(pressSound);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // If the pointer is still over the button, scale to hover size, otherwise back to normal
        _targetScale = _isPointerOver ? _originalScale * hoverScale : _originalScale;
        PlaySound(releaseSound);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isPointerOver = true;
        _targetScale = _originalScale * hoverScale;
        PlaySound(hoverSound);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isPointerOver = false;
        _targetScale = _originalScale;
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && _uiAudioSource != null)
        {
            _uiAudioSource.PlayOneShot(clip);
        }
    }
}
