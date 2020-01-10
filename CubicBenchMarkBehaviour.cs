using PupilLabs;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CubicBenchMarkBehaviour : MonoBehaviour
{
    public Camera sceneCamera;
    private GameObject[] cubes;
    public Text confidenceText;


    // gaze
    public GazeController gazeController;
    Vector3 localGazeDirection;
    float gazeDistance;
    private GazeListener gazeListener;
    public Transform gazeOrigin;
    public Transform projectionMarker;
    public Transform gazeDirectionMarker;
    public GazeVisualizer gazeVisualizer;
    public float sphereCastRadius = 0.05f;

    //Vectors
    private Vector3 directioninWorldSpace;
    private Vector3 relativeCubePosition;
    Vector3 cubeDirection;
    Vector3 direction;

    // confidence
    public float confidenceThreshold = 0.6f;
    float lastConfidence;



    // Start is called before the first frame update
    void Start()
    {
        cubes = GameObject.FindGameObjectsWithTag("SmallCube");
        // hide all cubes
        foreach (GameObject cube in cubes)
        {
            cube.SetActive(false);
        }

        // Displaying confidence is used for debugging
        confidenceText.enabled = false;

    }

    // Update is called once per frame
    void Update()
    {

        // S key begins displaying the cubes
        if (Input.GetKeyDown(KeyCode.S))
        {
            // Display gaze visualisation
            projectionMarker.gameObject.SetActive(true);
            gazeDirectionMarker.gameObject.SetActive(true);

            StartCoroutine(displayCubes());

        }
        gazeController.OnReceive3dGaze += ReceiveGaze;
        ShowProjected();
    }

    IEnumerator displayCubes()
    {
        Vector3 origin = gazeOrigin.position;
        // Display cube one by one
        foreach (GameObject cube in cubes)
        {
            cube.SetActive(true);

            relativeCubePosition = cube.transform.position - sceneCamera.transform.position;
            directioninWorldSpace = gazeOrigin.TransformDirection(localGazeDirection);
            cubeDirection = (cube.transform.position - origin).normalized;

            // Wait before printing next angles as change in cube position isn't representative of errors
            yield return new WaitForSeconds(0.5f);
            // Display angular error
            Debug.Log("angle: " + Vector3.Angle(direction, cubeDirection));

            // Display predicted gaze ray in Unity Scene view
            Debug.DrawLine(sceneCamera.transform.position, cube.transform.position, Color.green, 2f);

            // Wait before displaying next cube
            yield return new WaitForSeconds(2);

            // Hide current cube before next
            cube.SetActive(false);
        }
    }

    void ReceiveGaze(GazeData gazeData)
    {

        if (gazeData.MappingContext != GazeData.GazeMappingContext.Binocular)
        {
            return;
        }

        lastConfidence = gazeData.Confidence;

        if (gazeData.Confidence < confidenceThreshold)
        {
            return;
        }

        localGazeDirection = gazeData.GazeDirection;
        gazeDistance = gazeData.GazeDistance;

        // Display confidence
        confidenceText.text = Math.Round((double)gazeData.Confidence, 2).ToString();


    }

    void ShowProjected()
    {

        Vector3 origin = gazeOrigin.position;
        direction = gazeOrigin.TransformDirection(localGazeDirection);

        // Detect collision
        if (Physics.SphereCast(origin, sphereCastRadius, direction, out RaycastHit hit, Mathf.Infinity))
        {
            // Display real gaze ray in Unity Scene view
            Debug.DrawRay(origin, direction * hit.distance, Color.magenta);

            projectionMarker.position = hit.point;
            gazeDirectionMarker.position = origin + direction * hit.distance;
            gazeDirectionMarker.LookAt(origin);

        }
        else
        {
            Debug.DrawRay(origin, direction * 10, Color.blue);
        }
    }
}
