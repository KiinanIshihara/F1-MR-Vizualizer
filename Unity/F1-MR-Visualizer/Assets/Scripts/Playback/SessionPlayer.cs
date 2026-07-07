using System;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class SessionPlayer : MonoBehaviour
{
    [Header("Data")]
    public string resourceFileName = "spa_2023_q";

    [Header("Scene References")]
    public TrackMeshGenerator trackMeshGenerator;
    public LineRenderer trackLine;
    public GameObject carPrefab;
    public Transform carsParent;
    public TextMeshProUGUI sessionInfoText;
    public TextMeshProUGUI selectedCarText;
    private CarMarker selectedCar;


    [Header("Playback")]
    
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float startTimeOffset = 0f;
    [SerializeField] private float globalCarVisibilityStartTime = 1400f; //TODO need to find a way to automatically determine when during the playback that the cars actually start the session, 
    //since the telemetry playback includes idle time before the session officially starts in local time.
    [SerializeField] private float jumpSeconds = 10f;
    public float worldScale = 0.01f;
    public bool isPlaying = true;
    public float currentTime = 0f;

    public float playbackSpeed = 1f;

    [Header("Timeline UI")]
    [SerializeField] private Slider timelineSlider;
    private bool isUpdatingSliderFromCode = false;

    private Vector3 initialCameraPos;
    private Quaternion initialCameraRot;
    
    private readonly float[] speedOptions = { 0.5f,1f, 2f, 5f, 10f };
    private int speedIndex = 1;

    private SessionData sessionData;
    private readonly List<CarMarker> carMarkers = new();
    private Vector3 trackCenter;


    // UI metadata
    private string activeWindowText = ""; //TODO  delete later?
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        
        initialCameraPos = mainCamera.transform.position;
        initialCameraRot = mainCamera.transform.rotation;


        LoadSession();
        BuildTrack();
        SpawnCars();

        SetupTimelineSlider();    

        if (sessionData.activeEndSeconds > sessionData.activeStartSeconds)
        {
            activeWindowText = $"Active Window: {FormatTime(sessionData.activeStartSeconds)} - {FormatTime(sessionData.activeEndSeconds)}\n";
        }

        //currentTime = startTimeOffset;

        if (startTimeOffset > 0f)
        {
            currentTime = startTimeOffset;
        }
        else if (sessionData.activeEndSeconds > sessionData.activeStartSeconds)
        {
            currentTime = sessionData.activeStartSeconds;
        }

    }

    // Update is called once per frame
    void Update()
    {
        HandleKeyboardInput();
        HandleCarSelection();


        if (sessionData == null) return;

        if (isPlaying)
        {
            currentTime += Time.deltaTime * playbackSpeed;
            if (currentTime > sessionData.durationSeconds)
            currentTime = 0f;
        }

        foreach (var marker in carMarkers)
        {
            marker.UpdatePose(currentTime, worldScale, currentTime >= globalCarVisibilityStartTime);        }

        UpdateSessionUI();
        UpdateSelectedCarUI();
        UpdateTimelineSlider();
    }

    public void Play() => isPlaying = true;
    public void Pause() => isPlaying = false;
    
    private void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TogglePlayPause();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            Restart();
        }

        if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.Plus))
        {
            IncreasePlaybackSpeed();
        }

        if (Input.GetKeyDown(KeyCode.Minus))
        {
            DecreasePlaybackSpeed();
        }

        //TODO Might need to be deleted for XR camera
        if (Input.GetKeyDown(KeyCode.F))
        {
            FocusSelectedCar();
        }

        //TODO Might need to be deleted for XR camera
        if (Input.GetKeyDown(KeyCode.C))
        {
            ReturnCameraToInitialPos();
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            JumpTime(jumpSeconds);
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            JumpTime(-jumpSeconds);
        }

    }


    private void HandleCarSelection()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            CarMarker marker = hit.collider.GetComponentInParent<CarMarker>();

            if (marker != null)
            {
                
                if (selectedCar != null)
                {
                    selectedCar.SetSelected(false);
                }

                selectedCar = marker;
                selectedCar.SetSelected(true);

                UpdateSelectedCarUI();
            }
        } else
        {      
            if (selectedCar != null)
            {
                selectedCar.SetSelected(false);
            }

            selectedCar = null;


            //Debug.Log("clearing ui");


        }
    }
    
    private void LoadSession()
    {
        TextAsset jsonAsset = Resources.Load<TextAsset>(resourceFileName);
        if (jsonAsset == null)
        {
            Debug.LogError($"Could not load JSON resource: {resourceFileName}");
            return;
        }

        sessionData = JsonUtility.FromJson<SessionData>(jsonAsset.text);

        if (sessionData == null)
            Debug.LogError("Failed to parse session JSON");
        else 
            Debug.Log($"Loaded session: {sessionData.sessionName}");
    }

    /**private void BuildTrack()
    {
        if (sessionData == null || trackline == null || sessionData.trackPolyline == null)
            return;

        trackCenter = CalculateTrackCenter();
        
        trackline.positionCount = sessionData.trackPolyline.Length;

        for (int i = 0; i < sessionData.trackPolyline.Length; i++)
        {
            var p = sessionData.trackPolyline[i];
            Vector3 pos = ConvertPosition(p.x, p.y, p.z);
            trackline.SetPosition(i, pos);
        }

        Debug.Log($"Track points: {sessionData.trackPolyline.Length}");
    }
    **/

    private void BuildTrack()
{
    if (sessionData == null || sessionData.trackPolyline == null)
        return;

    trackCenter = CalculateTrackCenter();

    Vector3[] convertedPoints = new Vector3[sessionData.trackPolyline.Length];

    for (int i = 0; i < sessionData.trackPolyline.Length; i++)
    {
        var p = sessionData.trackPolyline[i];
        convertedPoints[i] = ConvertPosition(p.x, p.y, p.z);
    }

    // Keep the old LineRenderer as a debug reference.
    if (trackLine != null)
    {
        trackLine.positionCount = convertedPoints.Length;

        for (int i = 0; i < convertedPoints.Length; i++)
        {
            trackLine.SetPosition(i, convertedPoints[i]);
        }
    }

    // New procedural track ribbon.
    if (trackMeshGenerator != null)
    {
        trackMeshGenerator.GenerateTrackMesh(convertedPoints);
    }

    Debug.Log($"Track points: {sessionData.trackPolyline.Length}");
    Debug.Log($"Track center: {trackCenter}");
}


    private void SpawnCars()
    {
        if (sessionData == null || carPrefab == null) return;

        foreach (var driver in sessionData.drivers)
        {
            GameObject go = Instantiate(carPrefab, Vector3.zero, Quaternion.identity, carsParent);
            go.name = driver.driverCode;

            CarMarker marker = go.GetComponent<CarMarker>();
            marker.Initialize(driver, trackCenter);
            carMarkers.Add(marker);
        }

        Debug.Log($"Spawned cars: {carMarkers.Count}");
    }

    private Vector3 ConvertPosition(float x, float y, float z)
    {
        Vector3 raw = new Vector3(x, z, y);
        // Temporary mapping - may need adjustment after inspecting data orientation
        return (raw - trackCenter) * worldScale;
    }

    private Vector3 CalculateTrackCenter()
    {
        Vector3 sum = Vector3.zero;

        foreach (var p in sessionData.trackPolyline)
        {
            sum += new Vector3(p.x, p.z, p.y);
        }

        return sum / sessionData.trackPolyline.Length;
    }


    #region PLAYBACK CONTROLS METHODS
    public void IncreasePlaybackSpeed()
    {
        speedIndex = Mathf.Min(speedIndex + 1, speedOptions.Length - 1);
        playbackSpeed = speedOptions[speedIndex];
    }

    
    public void DecreasePlaybackSpeed()
    {
        speedIndex = Math.Max(speedIndex - 1, 0);
        playbackSpeed = speedOptions[speedIndex];
    }


    public void Restart()
    {
        //currentTime = 0f; //TODO?

        if (startTimeOffset > 0f)
        {
            currentTime = startTimeOffset;
        }
        else if (sessionData.activeEndSeconds > sessionData.activeStartSeconds)
        {
            currentTime = sessionData.activeStartSeconds;
        }
    }


    public void TogglePlayPause()
    {
        isPlaying = !isPlaying;
    }

    private void SetupTimelineSlider()
    {
        if (timelineSlider == null || sessionData == null) return;

        timelineSlider.minValue = 0f;
        timelineSlider.maxValue = 1f;
        timelineSlider.wholeNumbers = false;

        Navigation navigation = timelineSlider.navigation;
        navigation.mode = Navigation.Mode.None;
        timelineSlider.navigation = navigation;

        timelineSlider.onValueChanged.AddListener(OnTimelineSliderValueChanged);
    }

    public void OnTimelineSliderValueChanged(float normalizedValue)
    {
        if (isUpdatingSliderFromCode) return;
        if (sessionData == null) return;

        currentTime = Mathf.Lerp(0f, sessionData.durationSeconds, normalizedValue);
    }


    private void UpdateTimelineSlider()
    {
        if (timelineSlider == null || sessionData == null) return;
        if (sessionData.durationSeconds <= 0f) return;

        float normalizedTime = Mathf.InverseLerp(
            0f,
            sessionData.durationSeconds,
            currentTime
        );

        isUpdatingSliderFromCode = true;
        timelineSlider.value = normalizedTime;
        isUpdatingSliderFromCode = false;
    }


    private void JumpTime(float delta)
    {
        if (sessionData == null) return;

        currentTime = Mathf.Clamp(
            currentTime + delta,
            0f,
            sessionData.durationSeconds
        );

        Debug.Log($"Jumping {delta} seconds.\n Current time is: {currentTime} seconds");
        
    }

    #endregion


    public void UpdateSessionUI()
    {
        if (sessionInfoText == null || sessionData == null)
        {
            Debug.Log("sessionInfoText or sessionData is Null");
            return;
        }

        

        string status = isPlaying ? "Playing" : "Paused";

        sessionInfoText.text = 
            $"Session: {sessionData.sessionName}\n" +
            $"Track: {sessionData.trackName}\n" +
            $"Time: {FormatTime(currentTime)} / {FormatTime(sessionData.durationSeconds)}\n" +
            $"Active Window: {activeWindowText}\n" +
            $"Status: {status}\n" +
            $"Active Cars: {GetActiveCarCount()} / {carMarkers.Count}\n" +
            $"Speed: {playbackSpeed}x\n\n" +
            $"Controls:\n" +
            $"Space = Play/Pause\n" +
            $"← / → = Jump 10s\n" +
            $"R = Restart\n" +
            $"+ / - = Playback Speed";
        
    }

    private void UpdateSelectedCarUI()
    {
        if (selectedCarText == null) return;

        if (selectedCar == null)
        {
            selectedCarText.text = "Selected Car: None";
            return;
        }

        selectedCarText.text =
            $"Driver: {selectedCar.DriverCode} #{selectedCar.DriverNumber}\n" +
            $"Name: {selectedCar.FullName}\n" +
            $"Team: {selectedCar.TeamName}\n" +
            $"Fastest Lap: {selectedCar.FastestLap}\n" +
            $"Speed: {selectedCar.CurrentSpeed:F0} km/h";
    }




    private void FocusSelectedCar()
    {
        if (selectedCar == null || mainCamera == null) return;

        Vector3 target = selectedCar.transform.position;

        mainCamera.transform.position = target + new Vector3(0f, 1.5f, -2f);
        mainCamera.transform.LookAt(target);
    }

    private void ReturnCameraToInitialPos()
    {
        if (mainCamera == null) return;

        mainCamera.transform.position = initialCameraPos;
        mainCamera.transform.rotation = initialCameraRot;
    }


    private string FormatTime(float seconds)
    {
        int totalSeconds = Mathf.FloorToInt(seconds);
        int minutes = totalSeconds / 60;
        int secs = totalSeconds % 60;
        return $"{minutes:00}:{secs:00}";
    }


    /// <summary>
    /// Helper method to get the number of active cars / cars currently running on track during session.
    /// </summary>
    /// <returns></returns>
    private int GetActiveCarCount()
    {
        int count = 0;

        foreach( var marker in carMarkers)
        {
            if (marker != null && marker.IsVisible)
                count++;
        }

        return count;
    }
}
