using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthBarRotation : MonoBehaviour
{
    public Transform cameraTransform;
    public Camera mainCamera;
    // Start is called before the first frame update
    void Start()
    {
        mainCamera = Camera.main;
        cameraTransform = mainCamera.transform;
    }

    // Update is called once per frame
    void Update()
    {
        this.transform.LookAt(cameraTransform); // rotating the health bars to look at the camera so that the player can see them better 

        //set up this way befcasue i aim to set up a panable and angular camera system rather than a top down view for the next parts of the POE 
    } 
}
