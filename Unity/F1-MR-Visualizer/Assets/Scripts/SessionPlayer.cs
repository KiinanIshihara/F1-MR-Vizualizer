using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SessionPlayer : MonoBehaviour
{
    [Header("Data")]
    public string resourceFileName = "spa_2023_q";

    [Header("Scene References")]
    public LineRenderer trackline;
    public GameObject carPrefab;
    public Transform carsParent;
    public TextMeshProUGUI sessionTimeText;

    [Header("Playback")]
    public float worldScale = 0.01f;
    public bool isPlaying = true;
    public float currentTime = 0f;

    public float playbackSpeed = 1f;
    private readonly float[] speedOptions = { 0.5f, 1f, 1.5f, 2f };
    private int speedIndex = 1;

    private SessionData sessionData;
    private readonly List<CarMarker> carMarkers = new();
    private Vector3 trackCenter;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        LoadSession();
        BuildTrack();
        SpawnCars();
    }

    // Update is called once per frame
    void Update()
    {
        HandleKeyboardInput();


        if (sessionData == null) return;

        if (isPlaying)
        {
            currentTime += Time.deltaTime * playbackSpeed;
            if (currentTime > sessionData.durationSeconds)
            currentTime = 0f;
        }

        foreach (var marker in carMarkers)
        {
            marker.UpdatePose(currentTime, worldScale);
        }

        if (sessionTimeText != null) 
            sessionTimeText.text = $"t = {currentTime:F1}s";
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

    private void BuildTrack()
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
        currentTime = 0f;
    }


    public void TogglePlayPause()
    {
        isPlaying = !isPlaying;
    }
}
