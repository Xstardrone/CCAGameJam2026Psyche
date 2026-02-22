using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to an empty "GameManager" GameObject in each scene.
/// On scene load: Canvas is hidden, the intro transition prefab plays,
/// the screen grays out, then the Canvas is revealed by fading the gray away.
/// </summary>
public class SceneIntroManager : MonoBehaviour
{
    [Header("Intro Transition")]
    [Tooltip("Prefab with an Animator that plays an intro animation when the scene loads.")]
    public GameObject introPrefab;

    [Header("Scene UI")]
    [Tooltip("The Canvas that holds all the scene's UI / options. It will be hidden until the intro finishes.")]
    public Canvas sceneCanvas;

    [Header("Audio (Optional)")]
    [Tooltip("Optional sound to play alongside the intro animation.")]
    public AudioClip introSound;

    [Range(0f, 1f)]
    public float soundVolume = 1f;

    [Header("Gray-Out Fade")]
    [Tooltip("Color of the fade overlay.")]
    public Color fadeColor = Color.gray;

    [Tooltip("How long (seconds) the fade-in and fade-out each take.")]
    public float fadeDuration = 0.5f;

    private void Start()
    {
        // Hide the canvas immediately
        if (sceneCanvas != null)
            sceneCanvas.gameObject.SetActive(false);

        StartCoroutine(IntroCoroutine());
    }

    private IEnumerator IntroCoroutine()
    {
        // 1. Spawn the intro transition prefab
        GameObject introInstance = null;
        Animator animator = null;

        if (introPrefab != null)
        {
            introInstance = Instantiate(introPrefab);
            animator = introInstance.GetComponent<Animator>();
        }

        // 2. Play optional intro sound
        AudioSource audioSource = null;
        if (introSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = introSound;
            audioSource.volume = soundVolume;
            audioSource.Play();
        }

        // 3. Wait for the animation to finish
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

        // Hide the prefab immediately so the last frame doesn't flash
        if (introInstance != null)
            introInstance.SetActive(false);

        // 4. If the sound is still playing, wait for it to finish
        if (audioSource != null && audioSource.isPlaying)
        {
            yield return new WaitWhile(() => audioSource.isPlaying);
            Destroy(audioSource);
        }

        // 5. Fade to gray (covers the intro prefab)
        Image overlay = CreateFadeOverlay();
        yield return StartCoroutine(Fade(overlay, 0f, 1f, fadeDuration));

        // 6. Destroy the intro prefab behind the gray overlay
        if (introInstance != null)
            Destroy(introInstance);

        // 7. Show the Canvas behind the overlay
        if (sceneCanvas != null)
            sceneCanvas.gameObject.SetActive(true);

        // 8. Fade gray overlay out to reveal the Canvas
        yield return StartCoroutine(Fade(overlay, 1f, 0f, fadeDuration));

        // 9. Clean up overlay
        Destroy(overlay.canvas.gameObject);
    }

    // ───── Fade helpers ─────

    private Image CreateFadeOverlay()
    {
        GameObject canvasObj = new GameObject("FadeCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999; // always on top
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject imgObj = new GameObject("FadeImage");
        imgObj.transform.SetParent(canvasObj.transform, false);

        Image image = imgObj.AddComponent<Image>();
        image.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
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
