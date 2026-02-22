using UnityEngine;

/// <summary>
/// Simple script to make any GameObject persist across scene changes
/// Just attach this to any GameObject you want to keep alive
/// </summary>
public class PersistAcrossScenes : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("If true, will destroy duplicates when loading new scenes")]
    public bool destroyDuplicates = true;
    
    [Tooltip("Unique identifier for this persistent object (leave empty for automatic)")]
    public string uniqueId = "";
    
    void Awake()
    {
        if (destroyDuplicates)
        {
            HandleDuplicates();
        }
        
        // Make this object persist across scenes
        DontDestroyOnLoad(gameObject);
        
    }
    
    private void HandleDuplicates()
    {
        // Generate unique ID if not provided
        if (string.IsNullOrEmpty(uniqueId))
        {
            uniqueId = gameObject.name + "_" + GetType().Name;
        }
        
        // Find all objects with this script
        PersistAcrossScenes[] persistentObjects = FindObjectsByType<PersistAcrossScenes>(FindObjectsSortMode.None);
        
        foreach (PersistAcrossScenes obj in persistentObjects)
        {
            // Skip self
            if (obj == this) continue;
            
            // Check if this is a duplicate (same unique ID or same GameObject name + script type)
            bool isDuplicate = (obj.uniqueId == this.uniqueId) || 
                              (string.IsNullOrEmpty(obj.uniqueId) && string.IsNullOrEmpty(this.uniqueId) && 
                               obj.gameObject.name == this.gameObject.name);
            
            if (isDuplicate)
            {
                // Keep the one that's already in DontDestroyOnLoad scene
                if (obj.gameObject.scene.name == "DontDestroyOnLoad")
                {
                    Destroy(gameObject);
                    return;
                }
                else if (this.gameObject.scene.name != "DontDestroyOnLoad")
                {
                    // Both are in regular scenes, keep this one and destroy the other
                    Destroy(obj.gameObject);
                }
            }
        }
    }
}