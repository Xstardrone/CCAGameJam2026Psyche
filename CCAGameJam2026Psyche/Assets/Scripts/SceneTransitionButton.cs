using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Attach to a UI Button. Assign a transition prefab (with an Animator),
/// a target scene, and an optional sound clip.
/// On click: hides all UI, spawns the prefab, waits for animation,
/// plays the sound, then loads the target scene.
/// </summary>
public class SceneTransitionButton : MonoBehaviour
{
    [Header("Transition Settings")]
    [Tooltip("Prefab with an Animator that plays a transition animation.")]
    public GameObject transitionPrefab;

    [Tooltip("Name of the scene to load after the transition.")]
    public string targetSceneName;

    [Header("Audio")]
    [Tooltip("Optional sound to play after the animation finishes and before the scene loads.")]
    public AudioClip transitionSound;

    [Tooltip("Volume of the transition sound (0-1).")]
    [Range(0f, 1f)]
    public float soundVolume = 1f;

    [Header("Gray-Out Fade")]
    [Tooltip("Color of the fade overlay.")]
    public Color fadeColor = Color.gray;

    [Tooltip("How long (seconds) each fade takes.")]
    public float fadeDuration = 0.5f;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        if (_button != null)
        {
            _button.onClick.AddListener(OnButtonClicked);
        }
        else
        {
            Debug.LogWarning("SceneTransitionButton: No Button component found on this GameObject.");
        }
    }

    private void OnButtonClicked()
    {
        // Prevent double-clicks
        if (_button != null)
            _button.interactable = false;

        // Run the coroutine on a standalone GameObject so it survives
        // the Canvas being disabled (disabling this button's parent
        // would kill any coroutine running on it).
        GameObject runner = new GameObject("TransitionRunner");
        DontDestroyOnLoad(runner);
        TransitionRunner helper = runner.AddComponent<TransitionRunner>();
        helper.Run(transitionPrefab, targetSceneName, transitionSound, soundVolume, fadeColor, fadeDuration);
    }
}

/// <summary>
/// Lightweight helper that runs the transition coroutine on its own
/// GameObject so it is never interrupted by UI being disabled.
/// </summary>
public class TransitionRunner : MonoBehaviour
{
    private Color _fadeColor;
    private float _fadeDuration;

    public void Run(GameObject prefab, string sceneName, AudioClip sound, float volume,
                    Color fadeColor, float fadeDuration)
    {
        _fadeColor = fadeColor;
        _fadeDuration = fadeDuration;
        StartCoroutine(TransitionCoroutine(prefab, sceneName, sound, volume));
    }

    private IEnumerator TransitionCoroutine(GameObject prefab, string sceneName, AudioClip sound, float volume)
    {
        // 1. Create an overlay and fade to gray (covers the UI)
        Image overlay = CreateFadeOverlay();
        yield return StartCoroutine(Fade(overlay, 0f, 1f, _fadeDuration));

        // 2. Hide all Canvas UI in the scene (except our overlay)
        Canvas overlayCanvas = overlay.canvas;
        Canvas[] allCanvases = FindObjectsOfType<Canvas>();
        foreach (Canvas canvas in allCanvases)
        {
            if (canvas != overlayCanvas)
                canvas.gameObject.SetActive(false);
        }

        // 3. Instantiate the transition prefab behind the overlay
        GameObject transitionInstance = null;
        Animator animator = null;

        if (prefab != null)
        {
            transitionInstance = Instantiate(prefab);
            animator = transitionInstance.GetComponent<Animator>();
        }

        // 4. Fade gray out to reveal the transition prefab
        yield return StartCoroutine(Fade(overlay, 1f, 0f, _fadeDuration));

        // 5. Wait for the Animator to finish playing its default state
        if (animator != null)
        {
            yield return null;

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            float animationLength = stateInfo.length;

            yield return new WaitForSeconds(animationLength);
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }

        // 6. Play the transition sound
        if (sound != null)
        {
            AudioSource audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = sound;
            audioSource.volume = volume;
            audioSource.Play();

            yield return new WaitForSeconds(sound.length);
        }

        // 7. Fade to gray before loading the next scene
        yield return StartCoroutine(Fade(overlay, 0f, 1f, _fadeDuration));

        // 8. Load the target scene
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogWarning("SceneTransitionButton: No target scene name specified.");
        }

        // Clean up
        Destroy(overlayCanvas.gameObject);
        Destroy(gameObject);
    }

    // ───── Fade helpers ─────

    private Image CreateFadeOverlay()
    {
        GameObject canvasObj = new GameObject("FadeCanvas");
        DontDestroyOnLoad(canvasObj);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject imgObj = new GameObject("FadeImage");
        imgObj.transform.SetParent(canvasObj.transform, false);

        Image image = imgObj.AddComponent<Image>();
        image.color = new Color(_fadeColor.r, _fadeColor.g, _fadeColor.b, 0f);
        image.raycastTarget = true;

        RectTransform rt = image.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        return image;
    }

    private IEnumerator Fade(Image overlay, float fromAlpha, float toAlpha, float duration)
    {
        float elapsed = 0f;
        Color c = overlay.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            c.a = Mathf.Lerp(fromAlpha, toAlpha, t);
            overlay.color = c;
            yield return null;
        }

        c.a = toAlpha;
        overlay.color = c;
    }
}
