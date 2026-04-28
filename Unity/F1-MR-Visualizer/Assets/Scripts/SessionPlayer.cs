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

    private SessionData sessionData;
    private readonly List<CarMarker> carMarkers = new();
    
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
        if (sessionData == null) return;

        if (isPlaying)
        {
            currentTime += Time.deltaTime;
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
        
        trackline.positionCount = sessionData.trackPolyline.Length;

        for (int i = 0; i < sessionData.trackPolyline.Length; i++)
        {
            var p = sessionData.trackPolyline[i];
            Vector3 pos = ConvertPosition(p.x, p.y, p.z);
            trackline.SetPosition(i, pos);
        }
    }


    private void SpawnCars()
    {
        if (sessionData == null || carPrefab == null) return;

        foreach (var driver in sessionData.drivers)
        {
            GameObject go = Instantiate(carPrefab, Vector3.zero, Quaternion.identity, carsParent);
            go.name = driver.driverCode;

            CarMarker marker = go.GetComponent<CarMarker>();
            marker.Initialize(driver);
            carMarkers.Add(marker);
        }
    }

    private Vector3 ConvertPosition(float x, float y, float z)
    {
        // Temporary mapping - may need adjustment after inspecting data orientation
        return new Vector3(x, z, y) * worldScale;
    }
}
