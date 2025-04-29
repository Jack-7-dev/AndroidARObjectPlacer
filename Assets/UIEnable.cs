using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIController : MonoBehaviour
{
    [SerializeField] private GameObject createObjectUI; // Drag your UI prefab instance here

    public void ShowCustomImageUI()
    {
        createObjectUI.SetActive(true);
    }
}