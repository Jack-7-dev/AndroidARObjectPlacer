using UnityEngine;
using UnityEngine.UI; // For traditional UI Button
using UnityEngine.UIElements; // For UI Toolkit

// This component creates traditional Unity UI Buttons that overlay the UI Toolkit buttons
// and bridge the input to your CustomImageUploader component
using UnityEngine;
using UnityEngine.UI; // For traditional UI Button
using Button = UnityEngine.UI.Button; // Explicit reference to UI Button
using UIButton = UnityEngine.UIElements.Button; // Renamed UIElements Button to UIButton
using Image = UnityEngine.UI.Image; // For Image component

// This component creates traditional Unity UI Buttons that overlay the UI Toolkit buttons
// and bridge the input to your CustomImageUploader component
public class HybridButtonHandler : MonoBehaviour
{
    [SerializeField] private CustomImageUploader imageUploader;
    [SerializeField] private GameObject uiDocumentObject; // The GameObject with UIDocument component
    
    // Button prefab (will create a basic one at runtime if not assigned)
    [SerializeField] private Button buttonPrefab;
    
    // References to created buttons
    private Button selectButton;
    private Button createButton;
    private Button cancelButton;
    private Button closeButton;
    
    private Canvas buttonsCanvas;
    private RectTransform canvasRect;
    
    void Awake()
    {
        if (imageUploader == null)
        {
            imageUploader = FindObjectOfType<CustomImageUploader>();
            if (imageUploader == null)
            {
                Debug.LogError("No CustomImageUploader found in the scene!");
                return;
            }
        }
        
        if (uiDocumentObject == null)
        {
            Debug.LogError("UI Document GameObject not assigned!");
            return;
        }
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            Debug.LogError("No EventSystem found in the scene! UI buttons require an EventSystem.");
        }
        
        // Create a Canvas for our buttons if it doesn't exist
        CreateButtonsCanvas();
        
        // The buttons will be created when the UI is activated
    }
    
    private void CreateButtonsCanvas()
    {
        // Create a canvas for our buttons that will overlay the UI Toolkit UI
        GameObject canvasObject = new GameObject("ButtonsCanvas");
        canvasObject.AddComponent<GraphicRaycaster>();
        canvasObject.transform.SetParent(transform);
        
        buttonsCanvas = canvasObject.AddComponent<Canvas>();
        buttonsCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        buttonsCanvas.sortingOrder = 100; // Ensure it's on top
        
        // Add a CanvasScaler to handle different screen sizes
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        
        // Add a GraphicRaycaster for button input
        canvasObject.AddComponent<GraphicRaycaster>();
        
        canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;
        
        // Start with the canvas disabled
        canvasObject.SetActive(false);
    }
    
    // This should be called by CustomImageUploader when the UI is shown
    public void SetupButtons()
    {
        if (buttonsCanvas == null)
        {
            Debug.LogError("Buttons canvas not created!");
            return;
        }
        
        buttonsCanvas.gameObject.SetActive(true);
        
        // Get UI Document component
        var uiDocument = uiDocumentObject.GetComponent<UnityEngine.UIElements.UIDocument>();
        if (uiDocument == null)
        {
            Debug.LogError("UIDocument component not found!");
            return;
        }
        
        var root = uiDocument.rootVisualElement;
        if (root == null)
        {
            Debug.LogError("Root visual element not found!");
            return;
        }
        
        // Find UI Toolkit buttons
        var selectUIButton = root.Q<UIButton>("SelectButton");
        var createUIButton = root.Q<UIButton>("Create");
        var cancelUIButton = root.Q<UIButton>("Cancel");
        var closeUIButton = root.Q<UIButton>("X");
        
        // Create overlay buttons if the UI Toolkit buttons are found
        if (selectUIButton != null) CreateOverlayButton(selectUIButton, "SelectButton", OnSelectButtonClicked);
        if (createUIButton != null) CreateOverlayButton(createUIButton, "CreateButton", OnCreateButtonClicked);
        if (cancelUIButton != null) CreateOverlayButton(cancelUIButton, "CancelButton", OnCancelButtonClicked);
        if (closeUIButton != null) CreateOverlayButton(closeUIButton, "CloseButton", OnCloseButtonClicked);
        
        Debug.Log("Hybrid buttons created and positioned over UI Toolkit buttons");
    }
    
    private void CreateOverlayButton(UnityEngine.UIElements.VisualElement uiElement, string buttonName, UnityEngine.Events.UnityAction action)
    {
        // Calculate world position from UI Toolkit element
        var worldRect = GetWorldRectFromVisualElement(uiElement);
        
        // Create button GameObject
        GameObject buttonObj;
        if (buttonPrefab != null)
        {
            buttonObj = Instantiate(buttonPrefab.gameObject, buttonsCanvas.transform);
        }
        else
        {
            // Create a basic button if no prefab is provided
            buttonObj = new GameObject(buttonName);
            buttonObj.transform.SetParent(buttonsCanvas.transform);
            buttonObj.AddComponent<RectTransform>();
            buttonObj.AddComponent<CanvasRenderer>();
            var image = buttonObj.AddComponent<Image>();
            image.color = new Color(1, 0, 0, 0.3f); // Almost invisible
            var btn = buttonObj.AddComponent<Button>();
            btn.transition = Selectable.Transition.None; // No visual transition
        }
        // Position and size the button
        RectTransform rectTransform = buttonObj.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(0, 0);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = new Vector2(worldRect.x + worldRect.width/2, worldRect.y + worldRect.height/2);
        rectTransform.sizeDelta = new Vector2(worldRect.width, worldRect.height);

        // For debugging
        Debug.Log($"Button positioned at: ({rectTransform.anchoredPosition.x}, {rectTransform.anchoredPosition.y}) with size: ({rectTransform.sizeDelta.x}, {rectTransform.sizeDelta.y})");
        
        // Add click handler
        Button button = buttonObj.GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
        
        Debug.Log($"Created overlay button '{buttonName}' at position: {rectTransform.position}, size: {rectTransform.sizeDelta}");
    }
    
    private Rect GetWorldRectFromVisualElement(UnityEngine.UIElements.VisualElement element)
    {
        // Get the element's rect in screen space
        var rect = element.worldBound;
        
        // Convert screen space (which is measured from top-left) to Unity UI space
        float x = rect.x;
        float y = Screen.height - rect.y - rect.height; // Flip y-coordinate
        float width = rect.width;
        float height = rect.height;
        
        Debug.Log($"Element rect: Screen Position ({rect.x}, {rect.y}), Size ({width}, {height})");
        
        return new Rect(x, y, width, height);
    }
    
    // Button event handlers
    private void OnSelectButtonClicked()
    {
        Debug.Log("Hybrid SelectButton clicked");
        if (imageUploader != null)
        {
            imageUploader.OnSelectImageClickedPublic();
        }
    }
    
    private void OnCreateButtonClicked()
    {
        Debug.Log("Hybrid CreateButton clicked");
        if (imageUploader != null)
        {
            imageUploader.ForceCreateObject();
        }
    }
    
    private void OnCancelButtonClicked()
    {
        Debug.Log("Hybrid CancelButton clicked");
        if (imageUploader != null)
        {
            imageUploader.OnCancelClickedPublic();
        }
    }
    
    private void OnCloseButtonClicked()
    {
        Debug.Log("Hybrid CloseButton clicked");
        if (imageUploader != null)
        {
            imageUploader.OnCancelClickedPublic();
        }
    }
    
    public void HideButtons()
    {
        if (buttonsCanvas != null)
        {
            buttonsCanvas.gameObject.SetActive(false);
            
            // Clean up existing buttons
            foreach (Transform child in buttonsCanvas.transform)
            {
                Destroy(child.gameObject);
            }
        }
    }
}