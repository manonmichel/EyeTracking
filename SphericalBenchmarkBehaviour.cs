using PupilLabs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Random = System.Random;

public class SphericalBenchmarkBehaviour : MonoBehaviour
{
    public Camera sceneCamera;
    private List<GameObject> sphereMarkers;
    public GameObject wall;

    // gaze
    [Header("Gaze")]
    public GazeController gazeController;
    Vector3 localGazeDirection;
    float gazeDistance;
    private GazeListener gazeListener;
    public Transform gazeOrigin;
    public Transform projectionMarker;
    public Transform gazeDirectionMarker;
    public GazeVisualizer gazeVisualizer;
    public float sphereCastRadius = 0.05f;
    Vector3 direction = new Vector3(-1, -1, -1);
    private float angularError = -1f;
    private String currentSphere;

    // Fixation detection
    [Header("Fixation")]
    public float maxDispersion = 1f;    // Recommended: 1 degree
    public float minDuration = 0.3f ; // Recommended: 300 ms
    public SubscriptionsController subscriptionsController;
    private RequestController requestController;
    private bool fixation = false;
    public bool annotationsOnlyDuringFixations = true;

    // Exporting data
    [Header("Exporting Data")]
    public RecordingController recorder;
    public AnnotationPublisher annotationPub;
    public Transform head;
    public bool sendHeadAsAnnotation = false;
    public string pathForExperimentData;
    private string FolderName;
    private StreamWriter sw;
    private int sampleIndex;

    //Vectors
    private Vector3 directioninWorldSpace;
    private Vector3 relativespherePosition;
    Vector3 sphereDirection = new Vector3(-1,-1,-1);
    private Vector3 spherePosition;
    private Vector3 sphereLocalPos;
    private Vector3 centralRay;
    private Vector3 origin;

    // confidence
    [Header("Confidence")]
    public float confidenceThreshold = 0.6f;
    float lastConfidence;
    public Text debugText; // For debugging - displays pupil confidence

    // Start is called before the first frame update
    void Start()
    {
        origin = gazeOrigin.position;

        // Hide benchmarking elements at beginning
        sphereMarkers = GameObject.FindGameObjectsWithTag("Sphere").ToList();
        centralRay = (sphereMarkers[0].transform.position - origin);// First sphere is centre sphere

        Debug.Log(sphereMarkers[0].name + " distance from head: " + centralRay.magnitude);
        Debug.Log(sphereMarkers[13].name + " distance from head: " + (sphereMarkers[13].transform.position - origin).magnitude);
        Debug.Log(sphereMarkers[26].name + " distance from head: " + (sphereMarkers[26].transform.position - origin).magnitude);

        foreach (GameObject sphere in sphereMarkers)
        {
            Debug.Log(sphere.name + " - " + Vector3.Angle(centralRay, sphere.transform.position - origin));
            sphere.SetActive(false);
        }
        // Hide when not debugging
        debugText.enabled = false;

        requestController = subscriptionsController.requestCtrl;
        wall.SetActive(true);

        // Experiment Data
        //sampleIndex = 1;
        //FolderName = "Experiment_" + System.DateTime.Now.ToString("yyyyMMddHHmmss");
        //string path = pathForExperimentData + "/" + FolderName + "_Data.txt";
        //sw = new StreamWriter(path, true);
        //sw.WriteLine("SampleIndex Timestamp CamPos.x CamPos.y CamPos.z CamRot.x CamRot.y CamRot.z gazeDirection.x gazeDirection.y gazeDirection.z eyeCenter0.x eyeCenter0.y eyeCenter0.z eyeCenter1.x eyeCenter1.y eyeCenter1.z Confidence angularError spherePos.x spherePos.y SpherePos.z");
        //sw.Flush();
    }

    // Update is called once per frame
    void Update()
    {
        bool connected = recorder.requestCtrl.IsConnected;
        UpdateText(connected);

        // S key begins the experiment
        if (Input.GetKeyDown(KeyCode.S))
        {

            // Set scene for benchmarking
            foreach (GameObject elem in GameObject.FindGameObjectsWithTag("RoomElement"))
            {
                elem.SetActive(false);
            }

            // Start viewing spheres
            StartCoroutine(displaySpheres());


            if (requestController.IsConnected)
            {
                StartFixationSubscription();
            }

        }
        // Get Gaze Data
        gazeController.OnReceive3dGaze += ReceiveGaze;

        // Visualise gaze ray in Unity scene mode
        ShowProjected();
    }

    IEnumerator recordData(int sampleIndex, Vector3 eyeCenter0, Vector3 eyeCenter1, Vector3 gazeDirection, float conf, float angularErr)
    {
        yield return new WaitForSeconds(1f);
        float timestamp = Time.realtimeSinceStartup;

        sw.WriteLine(sampleIndex + " " + timestamp + " " + head.position.x + " " + head.position.y + " " + head.position.z + " " +
               head.rotation.x + " " + head.rotation.y + " " + head.rotation.z + " " +
               gazeDirection.x + " " + gazeDirection.y + " " + gazeDirection.z + " " + eyeCenter0.x + " " + eyeCenter0.y + " " + eyeCenter0.z + " " +
               + eyeCenter1.x + " " + eyeCenter1.y + " " + eyeCenter1.z + " " + conf + " " + angularErr + " " + spherePosition.x + " " + spherePosition.y + " " + spherePosition.z);
        sw.Flush();
    }

    // Display spheres one by one for benchmarking
    IEnumerator displaySpheres()
    {
        //Randomise order of spheres
        Shuffle(sphereMarkers);

        foreach (GameObject sphere in sphereMarkers)
        {
            sphere.SetActive(true);

            relativespherePosition = sphere.transform.position - sceneCamera.transform.position;
            directioninWorldSpace = gazeOrigin.TransformDirection(localGazeDirection);
            sphereDirection = (sphere.transform.position - origin).normalized;
            spherePosition = sphere.transform.position;
            sphereLocalPos = sphere.transform.localPosition;

            currentSphere = sphere.name;

            yield return new WaitForSeconds(2);
            sphere.SetActive(false);
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

        // -- Uncomment to display confidence
        // debugText.text = Math.Round((double)gazeData.Confidence, 2).ToString();

        angularError = Vector3.Angle(direction, sphereDirection);

        // -- This was to output data regularly (not just with fixations), too heavy though
        //StartCoroutine(recordData(sampleIndex, gazeData.EyeCenter0, gazeData.EyeCenter1, gazeData.GazeDirection, gazeData.Confidence, angularError));
        //sampleIndex++;
    }

    void ShowProjected()
    {
        direction = gazeOrigin.TransformDirection(localGazeDirection);

        if (Physics.SphereCast(origin, sphereCastRadius, direction, out RaycastHit hit, Mathf.Infinity))
        {
            if (fixation)
            {
                Debug.DrawRay(origin, direction * hit.distance, Color.magenta);
            }

            projectionMarker.position = hit.point;

            gazeDirectionMarker.position = origin + direction * hit.distance;
            gazeDirectionMarker.LookAt(origin);

        }
        else
        {
            if (fixation)
            {
                Debug.DrawRay(origin, direction * 10, Color.blue);
            }
        }
    }


    void SendFloatAnnotation(String aLabel, float value)
    {
        Dictionary<string, object> data = new Dictionary<string, object>();
        data[aLabel] = value;
        annotationPub.SendAnnotation(label: aLabel, customData: data);
    }

    void SendStringAnnotation(String aLabel, String value)
    {
        Dictionary<string, object> data = new Dictionary<string, object>();
        data[aLabel] = value;
        annotationPub.SendAnnotation(label: aLabel, customData: data);
    }

    void SendVectorAnnotation(String aLabel, Vector3 value)
    {
        Dictionary<string, object> data = new Dictionary<string, object>();
        data[aLabel + "_x"] = value.x;
        data[aLabel + "_y"] = value.y;
        data[aLabel + "_z"] = value.z;
        annotationPub.SendAnnotation(label: aLabel, customData: data);
    }

    void SendBenchmarkAnnotation(float angularError, Vector3 gazeDir, Vector3 sphereDir, String sphereLabel, object duration, object confidence, Vector3 gazePoint, Vector2 normPos, object dispersion, Vector3 spherePos, object id)
    {
        Dictionary<string, object> data = new Dictionary<string, object>();
        data["fixation_id"] = id;
        data["angularError"] = angularError;
        data["fixation_duration"] = duration;
        data["dispersion"] = dispersion;
        data["avg_pupil_confidence"] = confidence;
        data["gaze_position_3d" + "_x"] = gazePoint.x;
        data["gaze_position_3d" + "_y"] = gazePoint.y;
        data["gaze_position_3d" + "_z"] = gazePoint.z;
        data["norm_pos" + "_x"] = normPos.x;
        data["norm_pos" + "_y"] = normPos.y;
        data["head_world_x"] = head.position.x;
        data["head_world_y"] = head.position.y;
        data["head_world_z"] = head.position.z;
        data["head_rot_x"] = head.rotation.x;
        data["head_rot_y"] = head.rotation.y;
        data["head_rot_z"] = head.rotation.z;
        data["gaze_direction" + "_x"] = gazeDir.x;
        data["gaze_direction" + "_y"] = gazeDir.y;
        data["gaze_direction" + "_z"] = gazeDir.z;
        data["sphere_direction" + "_x"] = sphereDir.x;
        data["sphere_direction" + "_y"] = sphereDir.y;
        data["sphere_direction" + "_z"] = sphereDir.z;
        data["sphere_position" + "_x"] = spherePos.x;
        data["sphere_position" + "_y"] = spherePos.y;
        data["sphere_position" + "_z"] = spherePos.z;
        data["sphere_label"] = sphereLabel;
        data["sphere_angle"] = Vector3.Angle(centralRay, spherePos - origin);
        annotationPub.SendAnnotation(label: "fixation_annotations", customData: data);
    }

    void StartFixationSubscription()
    {
        Debug.Log("StartFixationSubscription");

        subscriptionsController.SubscribeTo("fixation", CustomReceiveData);

        requestController.StartPlugin(
            "Fixation_Detector",
            new Dictionary<string, object> {
                    { "max_dispersion", maxDispersion },
                    { "min_duration", minDuration }
            }
        );
    }

    void CustomReceiveData(string topic, Dictionary<string, object> dictionary, byte[] thirdFrame = null)
    {
        if (dictionary.ContainsKey("timestamp"))
        {
            Debug.Log("Fixation detected: " + dictionary["timestamp"].ToString() + dictionary["gaze_point_3d"]);

            Vector3 gazePoint = Helpers.Position(dictionary["gaze_point_3d"], false);
            Vector2 normPos = Helpers.Position(dictionary["norm_pos"], false);

            // Send annotations
            SendBenchmarkAnnotation(angularError, direction, sphereDirection, currentSphere, dictionary["duration"], dictionary["confidence"], gazePoint, normPos, dictionary["dispersion"], spherePosition, dictionary["id"]);

            Debug.DrawLine(head.transform.position, spherePosition, Color.green, 2f);

            if (!fixation)
            {
                fixation = true;

            }
        }
    }

    void UpdateText(bool connected)
    {
        if (connected)
        {
            debugText.text = "Press R to Start/Stop the recording.";

            var status = recorder.IsRecording ? "recording" : "not recording";
            debugText.text = $"Status: {status}";
        }
    }

    public static void Shuffle(List<GameObject> list)
    {
        Random rng = new Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            GameObject value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}
