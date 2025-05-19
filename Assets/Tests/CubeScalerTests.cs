using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[TestFixture]
public class CubeScalerTests
{
    private GameObject testGameObject;
    private CubeScaler cubeScaler;
    private GameObject targetCube;
    private TMP_InputField widthInput;
    private TMP_InputField heightInput;
    private TMP_InputField depthInput;
    private Button imageDisplayButton;

    [SetUp]
    public void SetUp()
    {
        // Create test game object and add CubeScaler component
        testGameObject = new GameObject("CubeScalerTest");
        cubeScaler = testGameObject.AddComponent<CubeScaler>();

        // Create target cube
        targetCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        targetCube.name = "TargetCube";

        // Create UI input fields
        CreateInputFields();

        // Create image display button
        GameObject displayButtonGO = new GameObject("ImageDisplayButton");
        imageDisplayButton = displayButtonGO.AddComponent<Button>();
        displayButtonGO.AddComponent<Image>();

        // Use reflection to set private fields
        SetPrivateField("targetCube", targetCube);
        SetPrivateField("widthInput", widthInput);
        SetPrivateField("heightInput", heightInput);
        SetPrivateField("depthInput", depthInput);
        SetPrivateField("imageDisplayButton", imageDisplayButton);
    }

    [TearDown]
    public void TearDown()
    {
        if (testGameObject != null)
            Object.DestroyImmediate(testGameObject);
        if (targetCube != null)
            Object.DestroyImmediate(targetCube);
        
        // Clean up UI components
        if (widthInput != null && widthInput.transform.root != null)
            Object.DestroyImmediate(widthInput.transform.root.gameObject);
        if (imageDisplayButton != null)
            Object.DestroyImmediate(imageDisplayButton.gameObject);
    }

    private void CreateInputFields()
    {
        GameObject canvasGO = new GameObject("Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        
        // Width input
        GameObject widthGO = new GameObject("WidthInput");
        widthGO.transform.SetParent(canvasGO.transform);
        widthInput = widthGO.AddComponent<TMP_InputField>();
        GameObject widthTextGO = new GameObject("Text");
        widthTextGO.transform.SetParent(widthGO.transform);
        widthInput.textComponent = widthTextGO.AddComponent<TextMeshProUGUI>();

        // Height input
        GameObject heightGO = new GameObject("HeightInput");
        heightGO.transform.SetParent(canvasGO.transform);
        heightInput = heightGO.AddComponent<TMP_InputField>();
        GameObject heightTextGO = new GameObject("Text");
        heightTextGO.transform.SetParent(heightGO.transform);
        heightInput.textComponent = heightTextGO.AddComponent<TextMeshProUGUI>();

        // Depth input
        GameObject depthGO = new GameObject("DepthInput");
        depthGO.transform.SetParent(canvasGO.transform);
        depthInput = depthGO.AddComponent<TMP_InputField>();
        GameObject depthTextGO = new GameObject("Text");
        depthTextGO.transform.SetParent(depthGO.transform);
        depthInput.textComponent = depthTextGO.AddComponent<TextMeshProUGUI>();
    }

    private void SetPrivateField(string fieldName, object value)
    {
        var field = typeof(CubeScaler).GetField(fieldName, 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(cubeScaler, value);
    }

    // Test 1: Valid numeric inputs should update cube scale correctly
    [Test]
    public void UpdateCubeDimensions_WithValidInputs_UpdatesCubeScale()
    {
        // Arrange
        widthInput.text = "2.5";
        heightInput.text = "3.0";
        depthInput.text = "1.5";

        // Act
        cubeScaler.UpdateCubeDimensions();

        // Assert
        Vector3 expectedScale = new Vector3(
            2.5f / 205f,
            3.0f / 205f,
            1.5f / 205f
        );
        Assert.AreEqual(expectedScale.x, targetCube.transform.localScale.x, 0.0001f);
        Assert.AreEqual(expectedScale.y, targetCube.transform.localScale.y, 0.0001f);
        Assert.AreEqual(expectedScale.z, targetCube.transform.localScale.z, 0.0001f);
    }

    // Test 2: Invalid inputs should use default values (1.0)
    [Test]
    [TestCase("invalid", "2.0", "3.0", 1.0f, 2.0f, 3.0f)]
    [TestCase("2.0", "negative", "3.0", 2.0f, 1.0f, 3.0f)]
    [TestCase("2.0", "3.0", "", 2.0f, 3.0f, 1.0f)]
    [TestCase("-1", "0", "abc", 1.0f, 1.0f, 1.0f)]
    public void UpdateCubeDimensions_WithInvalidInputs_UsesDefaultValues(
        string widthText, string heightText, string depthText,
        float expectedWidth, float expectedHeight, float expectedDepth)
    {
        // Arrange
        widthInput.text = widthText;
        heightInput.text = heightText;
        depthInput.text = depthText;

        // Act
        cubeScaler.UpdateCubeDimensions();

        // Assert
        Vector3 expectedScale = new Vector3(
            expectedWidth / 205f,
            expectedHeight / 205f,
            expectedDepth / 205f
        );
        Assert.AreEqual(expectedScale.x, targetCube.transform.localScale.x, 0.0001f);
        Assert.AreEqual(expectedScale.y, targetCube.transform.localScale.y, 0.0001f);
        Assert.AreEqual(expectedScale.z, targetCube.transform.localScale.z, 0.0001f);
    }

    // Test 4: Edge case inputs (very small/large values, decimals)
    [Test]
    [TestCase("0.001", "1000", "0.5", 0.001f, 1000f, 0.5f)]
    [TestCase("1.234", "5.678", "9.999", 1.234f, 5.678f, 9.999f)]
    public void UpdateCubeDimensions_WithEdgeCaseInputs_HandlesCorrectly(
        string widthText, string heightText, string depthText,
        float expectedWidth, float expectedHeight, float expectedDepth)
    {
        widthInput.text = widthText;
        heightInput.text = heightText;
        depthInput.text = depthText;

        cubeScaler.UpdateCubeDimensions();

        // Divide dimensions by 205 to scale correctly between millimeters and Unity units
        Vector3 expectedScale = new Vector3(
            expectedWidth / 205f,
            expectedHeight / 205f,
            expectedDepth / 205f
        );

        // Assert are equal with a tolerance for floating point precision
        Assert.AreEqual(expectedScale.x, targetCube.transform.localScale.x, 0.0001f);
        Assert.AreEqual(expectedScale.y, targetCube.transform.localScale.y, 0.0001f);
        Assert.AreEqual(expectedScale.z, targetCube.transform.localScale.z, 0.0001f);
    }

    // Test 5: UI component validation - button should have Image component
    [Test]
    public void ImageDisplayButton_HasRequiredImageComponent()
    {
        // Act
        Image imageComponent = imageDisplayButton.GetComponent<Image>();

        // Assert
        Assert.IsNotNull(imageComponent, "Image display button must have an Image component for texture application");
    }

    // Test 6: Null safety - no exceptions when target cube is null
    [Test]
    public void UpdateCubeDimensions_WithNullTargetCube_DoesNotThrowException()
    {
        // Arrange
        SetPrivateField("targetCube", null);
        widthInput.text = "2.0";
        heightInput.text = "3.0";
        depthInput.text = "4.0";

        // Act & Assert
        Assert.DoesNotThrow(() => cubeScaler.UpdateCubeDimensions());
        Assert.DoesNotThrow(() => cubeScaler.ApplyDimensions());
        Assert.DoesNotThrow(() => cubeScaler.ResetDimensions());
    }
}