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

        StartCoroutine(TransitionCoroutine());
    }

    private IEnumerator TransitionCoroutine()
    {
        // 1. Hide all Canvas UI in the scene
        Canvas[] allCanvases = FindObjectsOfType<Canvas>();
        foreach (Canvas canvas in allCanvases)
        {
            canvas.gameObject.SetActive(false);
        }

        // 2. Instantiate the transition prefab
        GameObject transitionInstance = null;
        Animator animator = null;

        if (transitionPrefab != null)
        {
            transitionInstance = Instantiate(transitionPrefab);
            animator = transitionInstance.GetComponent<Animator>();
        }

        // 3. Wait for the Animator to finish playing its default state
        if (animator != null)
        {
            // Wait one frame for the Animator to initialize
            yield return null;

            // Get the current clip length from the Animator
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            float animationLength = stateInfo.length;

            // Wait for the animation to complete
            yield return new WaitForSeconds(animationLength);
        }
        else
        {
            // No animator — just wait a brief moment
            yield return new WaitForSeconds(0.5f);
        }

        // 4. Play the transition sound
        if (transitionSound != null)
        {
            // Create a temporary AudioSource so it plays independently
            GameObject audioObj = new GameObject("TransitionAudio");
            DontDestroyOnLoad(audioObj);
            AudioSource audioSource = audioObj.AddComponent<AudioSource>();
            audioSource.clip = transitionSound;
            audioSource.volume = soundVolume;
            audioSource.Play();

            // Wait for the sound to finish before loading the scene
            yield return new WaitForSeconds(transitionSound.length);

            Destroy(audioObj);
        }

        // 5. Load the target scene
        if (!string.IsNullOrEmpty(targetSceneName))
        {
            SceneManager.LoadScene(targetSceneName);
        }
        else
        {
            Debug.LogWarning("SceneTransitionButton: No target scene name specified.");
        }
    }
}
