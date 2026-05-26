using System;
using UnityEngine;

[Serializable]
public class Vec3Data
{
    public float x;
    public float y;
    public float z;
}

[Serializable]
public class CarSampleData
{
    public float t;
    public float x;
    public float y;
    public float z;
    public float speed;

}

[Serializable]
public class DriverSessionData
{
    public string driverCode;
    public string fullName;
    public string teamName;
    public string colorHex;
    public CarSampleData[] samples;
}

[Serializable]
public class SessionData
{
    public string sessionName;
    public string trackName;
    public int sampleRateHz;
    public float durationSeconds;
    public Vec3Data[] trackPolyline;
    public DriverSessionData[] drivers;
    
}