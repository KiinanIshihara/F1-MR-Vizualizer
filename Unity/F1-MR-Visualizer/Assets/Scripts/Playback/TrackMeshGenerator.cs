using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TrackMeshGenerator : MonoBehaviour
{
    [Header("Track Mesh Settings")]
    [SerializeField] private float trackWidth = 8f;
    [SerializeField] private float verticalOffset = -0.02f;
    //[SerializeField] private bool generateOnStart = false;

    private MeshFilter meshFilter;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
    }

    public void GenerateTrackMesh(Vector3[] centerlinePoints)
    {
        if (centerlinePoints == null || centerlinePoints.Length < 2)
        {
            Debug.LogWarning("Not enough centerline points to generate track mesh.");
            return;
        }

        Mesh mesh = BuildRibbonMesh(centerlinePoints);
        mesh.name = "Procedural Track Ribbon";

        meshFilter.sharedMesh = mesh;

        Debug.Log($"Generated track mesh with {centerlinePoints.Length} centerline points.");
        Debug.Log($"Track mesh vertices: {mesh.vertexCount}, triangles: {mesh.triangles.Length / 3}");

    }

    private Mesh BuildRibbonMesh(Vector3[] centerlinePoints)
    {
        int pointCount = centerlinePoints.Length;

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        for (int i = 0; i < pointCount; i++)
        {
            Vector3 forward = GetForwardDirection(centerlinePoints, i);
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

            Vector3 center = centerlinePoints[i] + Vector3.up * verticalOffset;

            Vector3 leftVertex = center - right * (trackWidth * 0.5f);
            Vector3 rightVertex = center + right * (trackWidth * 0.5f);

            vertices.Add(leftVertex);
            vertices.Add(rightVertex);

            float v = i / (float)(pointCount - 1);
            uvs.Add(new Vector2(0f, v));
            uvs.Add(new Vector2(1f, v));
        }

        for (int i = 0; i < pointCount; i++)
        {
            int nextIndex = (i + 1) % pointCount;

            int currentLeft = i * 2;
            int currentRight = i * 2 + 1;
            int nextLeft = nextIndex * 2;
            int nextRight = nextIndex * 2 + 1;

            triangles.Add(currentLeft);
            triangles.Add(nextLeft);
            triangles.Add(currentRight);

            triangles.Add(currentRight);
            triangles.Add(nextLeft);
            triangles.Add(nextRight);
        }

        Mesh mesh = new Mesh();

        // Spa can have many points, so use 32-bit index format just in case.
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    private Vector3 GetForwardDirection(Vector3[] points, int index)
    {
        int previousIndex = index == 0 ? points.Length - 1 : index - 1;
        int nextIndex = index == points.Length - 1 ? 0 : index + 1;

        Vector3 direction = points[nextIndex] - points[previousIndex];

        // Keep the ribbon width horizontal instead of being affected by elevation changes.
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return Vector3.forward;

        return direction.normalized;
    }
}