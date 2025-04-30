using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIController : MonoBehaviour
{
    [SerializeField] private GameObject createObjectUI; // Drag your UI prefab instance here
    [SerializeField] private CustomImageUploader imageUploader;
    [SerializeField] private HybridButtonHandler hybridButtonHandler;

    private void Awake()
    {
        // Find components if not assigned in the inspector
        if (imageUploader == null)
        {
            imageUploader = FindObjectOfType<CustomImageUploader>();
            if (imageUploader == null)
            {
                Debug.LogWarning("CustomImageUploader not found in the scene.");
            }
        }

        if (hybridButtonHandler == null)
        {
            hybridButtonHandler = FindObjectOfType<HybridButtonHandler>();
            if (hybridButtonHandler == null)
            {
                Debug.LogWarning("HybridButtonHandler not found. You may need to add one to the scene.");
            }
        }
    }

    public void ShowCustomImageUI()
    {
        // Use the CustomImageUploader's method rather than directly activating the UI
        if (imageUploader != null)
        {
            imageUploader.ShowUploader();
            Debug.Log("Showing custom image UI through CustomImageUploader");
        }
        else
        {
            Debug.LogError("Cannot show UI - CustomImageUploader not found!");
        }
    }
}