using UnityEngine;

public class CarMarker : MonoBehaviour
{
    private DriverSessionData driverData;
    private Renderer cachedRenderer;
    private Vector3 trackCenter;

    public float CurrentSpeed { get; private set; }
    public string DriverCode => driverData != null ? driverData.driverCode : "";
    public string FullName => driverData != null ? driverData.fullName : "";
    public string TeamName => driverData != null ? driverData.teamName : "";


    public void Initialize(DriverSessionData data, Vector3 center)
    {
        driverData = data;
        trackCenter = center;
        cachedRenderer = GetComponentInChildren<Renderer>();
    }

    public void UpdatePose(float t, float scale)
    {
        if (driverData == null || driverData.samples == null || driverData.samples.Length == 0)
            return;

        var samples = driverData.samples;

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
                float u = Mathf.InverseLerp(a.t, b.t, t);

                Vector3 pa = ConvertPosition(a.x, a.y, a.z, scale);
                Vector3 pb = ConvertPosition(b.x, b.y, b.z, scale);

                transform.position = Vector3.Lerp(pa, pb, u);
                CurrentSpeed = Mathf.Lerp(a.speed, b.speed, u);

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
        return (raw - trackCenter) * scale;
    }

    public string GetDriverCode()
    {
        return driverData != null ? driverData.driverCode : "";
    }

    public string GetTeamName()
    {
        return driverData != null ? driverData.teamName : "";
    }
}