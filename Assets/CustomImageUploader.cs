using UnityEngine;
using UnityEngine.UIElements;
using System.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;


public class CustomImageUploader : MonoBehaviour
{
    [SerializeField] private GameObject uiObject; // Reference to CreateObjectUI GameObject
    [SerializeField] private Material customImageMaterial;
    [SerializeField] private HybridButtonHandler hybridButtonHandler; // Reference to our hybrid button handler

    private VisualElement root;
    private Button selectImageButton;
    private Button createButton;
    private Button cancelButton;
    private Button closeButton;
    private VisualElement previewContainer;
    private TextField widthField;
    private TextField heightField;
    private TextField depthField;
    private string pendingImagePath = null;

    private Texture2D selectedImage;
    private GameObject customObjectPrefab;
    private ARRaycastManager arRaycastManager;

    private void Awake()
    {
        arRaycastManager = FindObjectOfType<ARRaycastManager>();
        Debug.Log("CustomImageUploader Awake called");
        
        // Find the hybrid button handler if not already assigned
        if (hybridButtonHandler == null)
        {
            hybridButtonHandler = FindObjectOfType<HybridButtonHandler>();
            if (hybridButtonHandler == null)
            {
                Debug.LogWarning("HybridButtonHandler not found. Button clicks may not work.");
            }
        }
        
        // Keep the UI hidden at start
        if (uiObject != null)
        {
            uiObject.SetActive(false);
        }
    }

    // Set up the UI Toolkit elements
    public void SetupUI()
    {
        Debug.Log("SetupUI method running");
        
        if (uiObject == null)
        {
            Debug.LogError("UI Object not assigned!");
            return;
        }

        var uiDoc = uiObject.GetComponent<UIDocument>();
        if (uiDoc == null)
        {
            Debug.LogError("UIDocument component is missing on the assigned UI Object!");
            return;
        }

        // Save active state
        bool wasActive = uiObject.activeSelf;
        
        // Temporarily activate UI for setup if needed
        if (!wasActive)
        {
            uiObject.SetActive(true);
        }

        // Get the root element
        root = uiDoc.rootVisualElement;
        if (root == null)
        {
            Debug.LogError("Root VisualElement is null!");
            
            // Restore state
            if (!wasActive)
            {
                uiObject.SetActive(false);
            }
            return;
        }

        Debug.Log("Root VisualElement retrieved successfully.");

        // Find UI elements
        selectImageButton = root.Q<Button>("SelectButton");
        createButton = root.Q<Button>("Create");
        cancelButton = root.Q<Button>("Cancel");
        closeButton = root.Q<Button>("X");
        previewContainer = root.Q<VisualElement>("preview-container");
        
        // Find TextFields
        var textFields = root.Query<TextField>().ToList();
        foreach (var textField in textFields)
        {
            if (textField.label == "Width") widthField = textField;
            else if (textField.label == "Height") heightField = textField;
            else if (textField.label == "Depth") depthField = textField;
        }

        Debug.Log($"Buttons found - Select: {selectImageButton != null}, Create: {createButton != null}, Cancel: {cancelButton != null}, Close: {closeButton != null}");
        Debug.Log($"Fields found - Width: {widthField != null}, Height: {heightField != null}, Depth: {depthField != null}");

        // We'll still try to register UI Toolkit handlers
        // But they're just a backup - we'll use the hybrid buttons for actual input
        if (selectImageButton != null)
        {
            selectImageButton.clickable = new Clickable(OnSelectImageClicked);
            Debug.Log("Select Image button handler registered");
        }
        
        if (createButton != null)
        {
            createButton.clickable = new Clickable(OnCreateClicked);
            Debug.Log("Create button handler registered");
        }
        
        if (cancelButton != null)
        {
            cancelButton.clickable = new Clickable(OnCancelClicked);
            Debug.Log("Cancel button handler registered");
        }
        
        if (closeButton != null)
        {
            closeButton.clickable = new Clickable(OnCancelClicked);
            Debug.Log("Close button handler registered");
        }
        
        // Restore UI state if we temporarily activated it
        if (!wasActive)
        {
            uiObject.SetActive(false);
        }
        
        if (customImageMaterial == null)
        {
            customImageMaterial = new Material(Shader.Find("Standard"));
            Debug.Log("No custom material assigned - using default Standard shader");
        }
    }

    public void ShowUploader()
    {
        // Set up the UI before showing
        SetupUI();
        
        // Show the UI
        uiObject.SetActive(true);
        Debug.Log("UI activated - ShowUploader called");
        
        // Set up the hybrid button overlays
        if (hybridButtonHandler != null)
        {
            // Small delay to make sure the UI is laid out
            StartCoroutine(SetupHybridButtonsDelayed());
        }
    }
    
    private IEnumerator SetupHybridButtonsDelayed()
    {
        // Wait one frame for the UI to be fully laid out
        yield return null;
        
        if (hybridButtonHandler != null)
        {
            hybridButtonHandler.SetupButtons();
        }
    }

    // Public method for the hybrid button handler to call
    public void OnSelectImageClickedPublic()
    {
        OnSelectImageClicked();
    }
    
    private void OnSelectImageClicked()
    {
        Debug.Log("OnSelectImageClicked method running");
        StartCoroutine(GetImageFromGallery());
    }

    private IEnumerator GetImageFromGallery()
    {
        bool hasPermission = NativeGallery.CheckPermission(NativeGallery.PermissionType.Read, NativeGallery.MediaType.Image);
        if (!hasPermission)
        {
            Debug.LogWarning("Gallery permission denied - cannot pick image");
            yield break;
        }

        NativeGallery.GetImageFromGallery((path) =>
        {
            if (!string.IsNullOrEmpty(path))
            {
                pendingImagePath = path;
                Debug.Log("Image selected: " + path);
            }
            else
            {
                Debug.Log("No image selected or path is null.");
            }
        }, "Select an image", "image/*");

        yield return null;
    }
    
    private void HandlePendingImageLoad()
    {
        if (string.IsNullOrEmpty(pendingImagePath))
            return;

        Debug.Log("Loading image from: " + pendingImagePath);
        Texture2D texture = NativeGallery.LoadImageAtPath(pendingImagePath, maxSize: 1024);
        if (texture == null)
        {
            Debug.LogError("Couldn't load texture from " + pendingImagePath);
        }
        else
        {
            selectedImage = texture;
            DisplayImagePreview(texture);
            Debug.Log("Image loaded successfully");
        }

        pendingImagePath = null; // Reset
    }

    private void DisplayImagePreview(Texture2D texture)
    {
        if (previewContainer == null)
        {
            Debug.LogError("Preview container not found in the UI!");
            return;
        }

        VisualElement imageElement = new VisualElement();
        imageElement.style.width = new StyleLength(new Length(180, LengthUnit.Pixel));
        imageElement.style.height = new StyleLength(new Length(180, LengthUnit.Pixel));
        imageElement.style.backgroundImage = new StyleBackground(texture);

        imageElement.style.borderTopWidth = 1;
        imageElement.style.borderBottomWidth = 1;
        imageElement.style.borderLeftWidth = 1;
        imageElement.style.borderRightWidth = 1;
        imageElement.style.borderTopColor = new StyleColor(Color.white);
        imageElement.style.borderBottomColor = new StyleColor(Color.white);
        imageElement.style.borderLeftColor = new StyleColor(Color.white);
        imageElement.style.borderRightColor = new StyleColor(Color.white);

        previewContainer.Clear();
        previewContainer.Add(imageElement);
        Debug.Log("Image preview displayed");
    }

    // Public method for the hybrid button handler to call
    public void ForceCreateObject()
    {
        OnCreateClicked();
    }

    private void OnCreateClicked()
    {
        Debug.Log("OnCreateClicked method running");
        
        if (selectedImage == null)
        {
            Debug.LogWarning("No image selected");
            return;
        }

        float width = 10f;
        float height = 10f;
        float depth = 10f;

        if (widthField != null && !string.IsNullOrEmpty(widthField.value))
        {
            float.TryParse(widthField.value, out width);
        }

        if (heightField != null && !string.IsNullOrEmpty(heightField.value))
        {
            float.TryParse(heightField.value, out height);
        }

        if (depthField != null && !string.IsNullOrEmpty(depthField.value))
        {
            float.TryParse(depthField.value, out depth);
        }

        Debug.Log($"Creating custom object with dimensions: {width}x{height}x{depth}");
        CreateCustomObject(width, height, depth);
        CloseUI();
    }

    private void CreateCustomObject(float width, float height, float depth)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "CustomImageCube";
        cube.transform.localScale = new Vector3(width * 0.01f, height * 0.01f, depth * 0.01f);

        Material material = new Material(customImageMaterial);
        material.mainTexture = selectedImage;

        MeshRenderer renderer = cube.GetComponent<MeshRenderer>();
        renderer.material = material;

        RegisterWithARSystem(cube);
        customObjectPrefab = cube;
        cube.SetActive(false);
        
        Debug.Log("Custom object created and registered with AR system");
    }

    private void RegisterWithARSystem(GameObject objectPrefab)
    {
        ObjectPlacer objectPlacer = FindObjectOfType<ObjectPlacer>();
        if (objectPlacer != null)
        {
            objectPlacer.objectPrefab = objectPrefab;
            Debug.Log("Custom image object registered with AR system");
        }
        else
        {
            Debug.LogError("ObjectPlacer component not found!");
        }
    }

    // Public method for the hybrid button handler to call
    public void OnCancelClickedPublic()
    {
        OnCancelClicked();
    }
    
    private void OnCancelClicked()
    {
        Debug.Log("OnCancelClicked method running");
        CloseUI();
    }
    
    private void CloseUI()
    {
        selectedImage = null;
        previewContainer?.Clear();
        uiObject.SetActive(false);
        
        // Hide the hybrid buttons too
        if (hybridButtonHandler != null)
        {
            hybridButtonHandler.HideButtons();
        }
    }
    
    private void Update()
    {
        HandlePendingImageLoad();
    }
}