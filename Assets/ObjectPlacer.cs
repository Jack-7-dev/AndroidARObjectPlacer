using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

public class ObjectPlacer : MonoBehaviour
{
    public GameObject objectPrefab; // This can be set dynamically by CustomImageUploader
    private ARRaycastManager arRaycastManager;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    void Awake()
    {
        arRaycastManager = GetComponent<ARRaycastManager>();
    }

    void Update()
    {
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            // Make sure we have a prefab to instantiate
            if (objectPrefab == null)
                return;
                
            // Perform a raycast to detect a plane at the touch location
            if (arRaycastManager.Raycast(Input.GetTouch(0).position, hits, TrackableType.PlaneWithinPolygon))
            {
                Pose hitPose = hits[0].pose;
                // Instantiate the object at the hit position
                GameObject spawnedObject = Instantiate(objectPrefab, hitPose.position, hitPose.rotation);
                
                // Make sure it's active (in case it was disabled before)
                spawnedObject.SetActive(true);
            }
        }
    }
}