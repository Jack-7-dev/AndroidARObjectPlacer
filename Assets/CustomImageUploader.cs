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

        root = uiDoc.rootVisualElement;
        if (root == null)
        {
            Debug.LogError("Root VisualElement is null! Check if the UIDocument has a valid VisualTreeAsset assigned.");
            return;
        }

Debug.Log("Root VisualElement successfully retrieved.");

        selectImageButton = root.Q<Button>("SelectButton") ?? 
                           root.Query<Button>().Where(b => b.text == "Select Image from Gallery").First();

        if (selectImageButton == null)
        {
            Debug.LogError("Select Image button not found in the UI!");
            return;
        }                   
        createButton = root.Q<Button>("Create") ?? 
                      root.Query<Button>().Where(b => b.text == "Create").First();
        cancelButton = root.Q<Button>("Cancel") ?? 
                      root.Query<Button>().Where(b => b.text == "Cancel").First();
        closeButton = root.Q<Button>("X") ?? 
                     root.Query<Button>().Where(b => b.text == "X").First();

        previewContainer = root.Q<VisualElement>("preview-container");

        widthField = root.Query<TextField>().Where(t => t.label == "Width").First();
        heightField = root.Query<TextField>().Where(t => t.label == "Height").First();
        depthField = root.Query<TextField>().Where(t => t.label == "Depth").First();

        if (selectImageButton != null) selectImageButton.clicked += OnSelectImageClicked;
        if (createButton != null) 
        {
            createButton.clicked += OnCreateClicked;
            Debug.Log("Create button clicked.");
        }
        if (cancelButton != null) cancelButton.clicked += OnCancelClicked;
        if (closeButton != null) closeButton.clicked += OnCancelClicked;

        // Start hidden
        uiObject.SetActive(false);

        if (customImageMaterial == null)
        {
            customImageMaterial = new Material(Shader.Find("Standard"));
            Debug.Log("No custom material assigned - using default Standard shader");
        }
    }

    public void ShowUploader()
    {
        uiObject.SetActive(true);
    }

    private void OnSelectImageClicked()
    {
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

        Texture2D texture = NativeGallery.LoadImageAtPath(pendingImagePath, maxSize: 1024);
        if (texture == null)
        {
            Debug.LogError("Couldn't load texture from " + pendingImagePath);
        }
        else
        {
            selectedImage = texture;
            DisplayImagePreview(texture);
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
    }

    private void OnCreateClicked()
    {
        if (selectedImage == null)
        {
            Debug.LogWarning("No image selected");
            return;
        }

        if (!float.TryParse(widthField.value, out float width) ||
            !float.TryParse(heightField.value, out float height) ||
            !float.TryParse(depthField.value, out float depth))
        {
            Debug.LogWarning("Invalid dimensions");
            return;
        }

        CreateCustomObject(width, height, depth);
        uiObject.SetActive(false);
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

    private void OnCancelClicked()
    {
        Debug.Log("Cancel button clicked. Resetting UI and clearing selected image.");
        selectedImage = null;
        previewContainer?.Clear();
        uiObject.SetActive(false);
    }
    private void Update()
    {
        HandlePendingImageLoad();
    }
}
