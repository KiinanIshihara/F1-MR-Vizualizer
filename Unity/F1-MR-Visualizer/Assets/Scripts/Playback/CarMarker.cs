using UnityEngine;
 using TMPro;
using System;

public class CarMarker : MonoBehaviour
{
    [SerializeField] private Renderer carBodyRenderer;
    [SerializeField] private Collider carCollider;
    private DriverSessionData driverData;
    private Vector3 trackCenter;
    private Camera mainCamera;

    public float CurrentSpeed { get; private set; }
    public string DriverCode => driverData != null ? driverData.driverCode : "";
    public string FullName => driverData != null ? driverData.fullName : "";
    public string TeamName => driverData != null ? driverData.teamName : "";
    public string DriverNumber => driverData != null ? driverData.driverNumber : "";
    public string FastestLap
    {
        get
        {
            if (driverData == null) return "0:00:000";

            float rawTime = driverData.fastestLapSeconds;
            float minutes;
            float seconds;
            string formattedTime;

            if (rawTime > 60f)
            {
                minutes = (float)Math.Floor(rawTime / 60f);
                seconds = rawTime % 60f;
            } else
            {
                minutes = 0f;
                seconds = rawTime;
            }

            formattedTime = minutes.ToString() + ":" + seconds.ToString();

            return formattedTime;
        }
    }

    public bool IsVisible { get; private set;}

    [SerializeField] private float minVisibleSpeed = 5f;
    [SerializeField] private float maxInterpolationGap = 1.0f;
    [SerializeField] private float heightOffset = 0.08f;
    [SerializeField] private TMP_Text driverLabel;
    [SerializeField] private Material highlightShader;

    private Material runtimeMaterial;
    private Color baseColor = Color.white;
    private bool isSelected = false;
    private Vector3 initialCarScale;

    public void Initialize(DriverSessionData data, Vector3 center)
    {
        driverData = data;
        trackCenter = center;
        initialCarScale = transform.localScale;

        if (carBodyRenderer != null)
        {
            runtimeMaterial = new Material(carBodyRenderer.material);
            carBodyRenderer.material = runtimeMaterial;

            baseColor = GetTeamColor(driverData.teamName, driverData.colorHex);
            ApplyBaseColor();
        }

        mainCamera = Camera.main;

        if (driverLabel != null)
        {
            driverLabel.text = data.driverCode;
        }

        SetVisible(false);
    }


    private void LateUpdate()
    {
        if (driverLabel == null || mainCamera == null) return;

        Transform labelTransform = driverLabel.transform;

        Vector3 directionToCamera = labelTransform.position - mainCamera.transform.position;

        if (directionToCamera.sqrMagnitude > 0.001f)
        {
            labelTransform.rotation = Quaternion.LookRotation(directionToCamera);
        }
    }

    public void UpdatePose(float t, float scale, bool allowVisibility)    {
        if (driverData == null || driverData.samples == null || driverData.samples.Length == 0)
            return;

        if (!allowVisibility)
        {
            SetVisible(false);
            return;
        }

        var samples = driverData.samples;


        if (t < samples[0].t || t > samples[samples.Length - 1].t)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);


        if (t <= samples[0].t)
        {
            ApplySample(samples[0], scale);
            return;
        }

        if (t >= samples[samples.Length - 1].t)
        {
            ApplySample(samples[samples.Length - 1], scale);
            return;
        }

        for (int i = 0; i < samples.Length - 1; i++)
        {
            var a = samples[i];
            var b = samples[i + 1];

            if (t >= a.t && t <= b.t)
            {
                float gap = b.t - a.t;

                if (gap > maxInterpolationGap)
                {
                    SetVisible(false);
                    return;
                }

                SetVisible(true);
                
                float u = Mathf.InverseLerp(a.t, b.t, t);

                Vector3 pa = ConvertPosition(a.x, a.y, a.z, scale);
                Vector3 pb = ConvertPosition(b.x, b.y, b.z, scale);

                transform.position = Vector3.Lerp(pa, pb, u);
                CurrentSpeed = Mathf.Lerp(a.speed, b.speed, u);

                if (CurrentSpeed < minVisibleSpeed)
                {
                    SetVisible(false);
                    return;
                }

                SetVisible(true);

                Vector3 forward = (pb - pa).normalized;
                if (forward.sqrMagnitude > 0.0001f)
                    transform.forward = forward;

                return;
            }
        }
    }

    private void ApplySample(CarSampleData s, float scale)
    {
        transform.position = ConvertPosition(s.x, s.y, s.z, scale);
        CurrentSpeed = s.speed;
    }

    private Vector3 ConvertPosition(float x, float y, float z, float scale)
    {
        Vector3 raw = new Vector3(x, z, y);
        Vector3 converted = (raw - trackCenter) * scale;
        converted.y += heightOffset;
        return converted;
    }


    private void SetVisible(bool visible)
    {
        IsVisible = visible;
        
        if (carBodyRenderer != null)
            carBodyRenderer.enabled = visible;

        if (driverLabel != null)
            driverLabel.gameObject.SetActive(visible);

        if (carCollider != null)
            carCollider.enabled = visible;
    }

    public string GetDriverCode()
    {
        return driverData != null ? driverData.driverCode : "";
    }

    public string GetTeamName()
    {
        return driverData != null ? driverData.teamName : "";
    }


    private Color GetTeamColor(string teamName, string colorHex)
    {
        if (!string.IsNullOrEmpty(colorHex) && ColorUtility.TryParseHtmlString(colorHex, out Color parsedColor))
        {
            if (parsedColor != Color.white)
                return parsedColor;
        }

        string team = teamName.ToLower();

        if (team.Contains("red bull")) return new Color32(71, 129, 215, 255);
        if (team.Contains("ferrari")) return new Color32(237, 17, 49, 255);
        if (team.Contains("mercedes")) return new Color32(0, 215, 182, 255);
        if (team.Contains("mclaren")) return new Color32(244, 118, 0, 255);
        if (team.Contains("aston")) return new Color32(34, 153, 113, 255);
        if (team.Contains("alpine")) return new Color32(0, 161, 232, 255);
        if (team.Contains("williams")) return new Color32(24, 104, 219, 255);
        if (team.Contains("haas")) return new Color32(156, 159, 162, 255);
        if (team.Contains("sauber") || team.Contains("kick")) return new Color32(1, 192, 14, 255);
        if (team.Contains("rb") || team.Contains("bulls") || team.Contains("alpha")) return new Color32(108, 152, 255, 255);
        if (team.Contains("alfa")) return new Color32(201, 45, 75, 255);

        return Color.white;
    }

    private void ApplyBaseColor()
    {
        if (runtimeMaterial == null) return;

        runtimeMaterial.color = baseColor;
        driverLabel.color = baseColor;
    }


    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (runtimeMaterial == null) return;

        if (isSelected)
        {
            highlightShader.SetColor("_BaseColor", baseColor);
            carBodyRenderer.material = highlightShader;

            transform.localScale = new Vector3(0.1f, 0.1f, 0.165f);
        }
        else
        {   
            //runtimeMaterial.color = baseColor;
            carBodyRenderer .material = runtimeMaterial;
            transform.localScale = initialCarScale;
        }
    }

    #region DEBUG DRIVER ACTIVITY STATE
    public float FirstSampleTime
    {
        get
        {
            if (driverData == null || driverData.samples == null || driverData.samples.Length == 0)
                return 0f;

            return driverData.samples[0].t;
        }
    }

    public float LastSampleTime
    {
        get
        {
            if (driverData == null || driverData.samples == null || driverData.samples.Length == 0)
                return 0f;

            return driverData.samples[driverData.samples.Length - 1].t;
        }
    }


    #endregion
}