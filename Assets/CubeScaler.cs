using UnityEngine;
using TMPro;
using UnityEngine.UI; // For Button and Image components

public class CubeScaler : MonoBehaviour
{
    [SerializeField] private GameObject targetCube;
    [SerializeField] private TMP_InputField widthInput, heightInput, depthInput;
    [SerializeField] private Button openGalleryButton; // Button to open gallery
    [SerializeField] private Button imageDisplayButton; // Button to display the selected image
    private string selectedImagePath; // Store the path of the selected image
    private Texture2D selectedImageTexture; // Store the texture of the selected image

    // Default dimensions
    private float defaultWidth = 1f;
    private float defaultHeight = 1f;
    private float defaultDepth = 1f;
    
    private void Start()
    {
        // Initialize input fields with default values
        widthInput.text = defaultWidth.ToString();
        heightInput.text = defaultHeight.ToString();
        depthInput.text = defaultDepth.ToString();
        
        // Apply default scale
        UpdateCubeDimensions();
        
        // Add listeners to input fields for real-time updates
        widthInput.onValueChanged.AddListener(delegate { UpdateCubeDimensions(); });
        heightInput.onValueChanged.AddListener(delegate { UpdateCubeDimensions(); });
        depthInput.onValueChanged.AddListener(delegate { UpdateCubeDimensions(); });
        
        // Add listener to open gallery button
        if (openGalleryButton != null)
        {
            openGalleryButton.onClick.AddListener(OpenGallery);
        }
    }
    
    public void UpdateCubeDimensions()
    {
        if (targetCube == null) return;

        // Parse dimension inputs with error handling
        float width = defaultWidth;
        float height = defaultHeight;
        float depth = defaultDepth;

        if (float.TryParse(widthInput.text, out float w) && w > 0) width = w;
        if (float.TryParse(heightInput.text, out float h) && h > 0) height = h;
        if (float.TryParse(depthInput.text, out float d) && d > 0) depth = d;

        // Divide dimensions by 205 to scale correctly between millimeters and Unity units
        width /= 205f;
        height /= 205f;
        depth /= 205f;

        // Apply scale to cube
        targetCube.transform.localScale = new Vector3(width, height, depth);
    }
    
    // Method that can be called from a button to apply dimensions
    public void ApplyDimensions()
    {
        UpdateCubeDimensions();
    }
    
    // Method to reset to default dimensions
    public void ResetDimensions()
    {
        widthInput.text = defaultWidth.ToString();
        heightInput.text = defaultHeight.ToString();
        depthInput.text = defaultDepth.ToString();
        UpdateCubeDimensions();
    }
    
    // Method to open the gallery
    public void OpenGallery()
    {
        // Check if NativeGallery is already busy
        if (NativeGallery.IsMediaPickerBusy())
            return;
            
        // Open gallery and handle the callback
        NativeGallery.GetImageFromGallery(OnImageSelected, "Select Image", "image/*");
    }
    
    // Callback for when an image is selected
    private void OnImageSelected(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            Debug.Log("No image selected");
            return;
        }
        
        // Store the selected image path
        selectedImagePath = path;
        Debug.Log("Image selected: " + path);
        
        // Load the image as a texture
        selectedImageTexture = NativeGallery.LoadImageAtPath(path, -1, false);
        
        if (selectedImageTexture == null)
        {
            Debug.Log("Couldn't load image from path: " + path);
            return;
        }
        
        // Create a sprite from the texture
        Sprite sprite = Sprite.Create(selectedImageTexture, new Rect(0, 0, selectedImageTexture.width, selectedImageTexture.height), new Vector2(0.5f, 0.5f));
        
        if (targetCube != null && selectedImageTexture != null)
        {
            // Find the quad within the targetCube's hierarchy
            
            // Option 1: By name (if the quad has a specific name)
            Transform quadTransform = targetCube.transform.Find("ImageMesh");
            
            if (quadTransform != null)
            {
                Renderer quadRenderer = quadTransform.GetComponent<Renderer>();
                if (quadRenderer != null)
                {
                    // Create a new material with the texture
                    Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    material.mainTexture = selectedImageTexture;
                    
                    // Apply the material to the quad only
                    quadRenderer.material = material;
                    Debug.Log("Texture applied to quad successfully");
                }
                else
                {
                    Debug.LogError("Renderer component not found on quad");
                }
            }
            else
            {
                Debug.LogError("Could not find quad within the target cube");
            }
        }
        // Also apply the texture to the button
        if (imageDisplayButton != null)
        {
            // Create a sprite from the texture for the button
            Sprite sprite2 = Sprite.Create(
                selectedImageTexture, 
                new Rect(0, 0, selectedImageTexture.width, selectedImageTexture.height), 
                new Vector2(0.5f, 0.5f)
            );
            
            // Get the Image component from the button
            Image buttonImage = imageDisplayButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                // Set the sprite on the button
                buttonImage.sprite = sprite2;
                
                // Optionally preserve aspect ratio
                buttonImage.preserveAspect = true;
                
                Debug.Log("Image set to button successfully");
            }
            else
            {
                // If the button doesn't have an Image component directly, try finding it in children
                buttonImage = imageDisplayButton.GetComponentInChildren<Image>();
                if (buttonImage != null)
                {
                    buttonImage.sprite = sprite2;
                    buttonImage.preserveAspect = true;
                    Debug.Log("Image set to button's child image component successfully");
                }
                else
                {
                    Debug.LogError("No Image component found on button or its children");
                }
            }
        }
    }
}