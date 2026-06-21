using UnityEngine;
using System.Collections.Generic;
using Robots.Components.Movement;

/// <summary>
/// Draws a glowing, animated path trail on the ground showing the A* route
/// from the robot's current position to its destination.
/// Only renders for the robot currently tracked by the main camera.
/// </summary>
[RequireComponent(typeof(RobotPathfinder))]
public class PathRenderer : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private Color pathColor = new Color(0.16f, 0.72f, 0.64f, 0.85f);  // Turquoise
    [SerializeField] private Color endColor  = new Color(0.16f, 0.72f, 0.64f, 0.05f);   // Fade-out
    [SerializeField] private float lineWidth = 0.25f;
    [SerializeField] private float groundOffset = 0.15f;

    [Header("Animation")]
    [SerializeField] private float pulseSpeed = 1.5f;
    [SerializeField] private float pulseMin = 0.5f;
    [SerializeField] private float pulseMax = 1.0f;

    [Header("Destination Marker")]
    [SerializeField] private float markerRadius = 0.6f;
    [SerializeField] private int   markerSegments = 24;

    private RobotPathfinder pathfinder;
    private LineRenderer pathLine;
    private LineRenderer markerLine;
    private Material lineMaterial;
    private RobotCamera mainCam;
    private Terrain terrain;

    // Reusable list to avoid GC
    private readonly List<Vector3> sampledPoints = new List<Vector3>(64);

    private void Awake()
    {
        pathfinder = GetComponent<RobotPathfinder>();
        CreateLineMaterial();
        pathLine = CreateLineRenderer("PathLine");
        markerLine = CreateLineRenderer("DestMarker");
    }

    private void CreateLineMaterial()
    {
        // Use the built-in Sprites/Default shader — it supports vertex colors,
        // transparency, and doesn't require a texture. Works on all Unity versions.
        lineMaterial = new Material(Shader.Find("Sprites/Default"));
        lineMaterial.renderQueue = 3100; // Render above terrain
    }

    private LineRenderer CreateLineRenderer(string childName)
    {
        var go = new GameObject(childName);
        go.transform.SetParent(transform, false);

        var lr = go.AddComponent<LineRenderer>();
        lr.material = lineMaterial;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.allowOcclusionWhenDynamic = false;
        lr.useWorldSpace = true;
        lr.numCapVertices = 3;
        lr.numCornerVertices = 3;
        lr.positionCount = 0;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        return lr;
    }

    private void LateUpdate()
    {
        if (terrain == null)
            terrain = Terrain.activeTerrain;

        bool shouldShow = IsTrackedByCamera() && pathfinder != null && pathfinder.HasTarget;

        pathLine.enabled = shouldShow;
        markerLine.enabled = shouldShow;

        if (!shouldShow) return;

        // Pulse animation
        float pulse = Mathf.Lerp(pulseMin, pulseMax, (Mathf.Sin(Time.time * pulseSpeed * Mathf.PI) + 1f) * 0.5f);
        Color startCol = pathColor * pulse;
        startCol.a = pathColor.a * pulse;

        UpdatePathLine(startCol);
        UpdateDestinationMarker(startCol);
    }

    private void UpdatePathLine(Color startCol)
    {
        List<Vector3> currentPath = pathfinder.CurrentPath;
        if (currentPath == null || currentPath.Count == 0)
        {
            pathLine.positionCount = 0;
            return;
        }

        // Build the visible path: robot position → remaining waypoints
        sampledPoints.Clear();
        sampledPoints.Add(GetGroundPoint(transform.position));

        // We access pathfinder internals through the public CurrentPath
        // The path list represents all waypoints; the robot consumes them as it moves
        for (int i = pathfinder.PathIndex; i < currentPath.Count; i++)
        {
            Vector3 gp = GetGroundPoint(currentPath[i]);

            // Skip waypoints that are very close to the previous point (avoid clutter)
            if (sampledPoints.Count > 0 && Vector3.Distance(gp, sampledPoints[sampledPoints.Count - 1]) < 0.3f)
                continue;

            sampledPoints.Add(gp);
        }

        if (sampledPoints.Count < 2)
        {
            pathLine.positionCount = 0;
            return;
        }

        // Apply positions
        pathLine.positionCount = sampledPoints.Count;
        for (int i = 0; i < sampledPoints.Count; i++)
            pathLine.SetPosition(i, sampledPoints[i]);

        // Width: slightly thicker at robot, thinner at destination
        pathLine.startWidth = lineWidth;
        pathLine.endWidth = lineWidth * 0.4f;

        // Gradient: bright at robot, fades toward destination
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(startCol, 0f),
                new GradientColorKey(endColor, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(startCol.a, 0f),
                new GradientAlphaKey(startCol.a * 0.6f, 0.5f),
                new GradientAlphaKey(0.08f, 1f)
            }
        );
        pathLine.colorGradient = gradient;
    }

    private void UpdateDestinationMarker(Color col)
    {
        Vector3? finalTarget = pathfinder.FinalTarget;
        if (!finalTarget.HasValue)
        {
            markerLine.positionCount = 0;
            return;
        }

        // Draw a ring/circle on the ground at the destination
        Vector3 center = GetGroundPoint(finalTarget.Value);
        float animatedRadius = markerRadius * (0.9f + 0.1f * Mathf.Sin(Time.time * 3f));

        markerLine.positionCount = markerSegments + 1;
        markerLine.startWidth = lineWidth * 0.7f;
        markerLine.endWidth = lineWidth * 0.7f;
        markerLine.loop = true;

        for (int i = 0; i <= markerSegments; i++)
        {
            float angle = (i / (float)markerSegments) * Mathf.PI * 2f;
            float x = center.x + Mathf.Cos(angle) * animatedRadius;
            float z = center.z + Mathf.Sin(angle) * animatedRadius;
            float y = center.y;

            // Sample terrain height for each ring point
            if (terrain != null)
                y = terrain.SampleHeight(new Vector3(x, 0, z)) + terrain.transform.position.y + groundOffset;

            markerLine.SetPosition(i, new Vector3(x, y, z));
        }

        // Solid color ring (subtle pulse)
        Color ringColor = col;
        ringColor.a = Mathf.Clamp01(col.a * 0.7f);
        Gradient ringGrad = new Gradient();
        ringGrad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(ringColor, 0f), new GradientColorKey(ringColor, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(ringColor.a, 0f), new GradientAlphaKey(ringColor.a, 1f) }
        );
        markerLine.colorGradient = ringGrad;
    }

    private Vector3 GetGroundPoint(Vector3 worldPos)
    {
        float y = worldPos.y;
        if (terrain != null)
            y = terrain.SampleHeight(worldPos) + terrain.transform.position.y + groundOffset;
        return new Vector3(worldPos.x, y, worldPos.z);
    }

    private bool IsTrackedByCamera()
    {
        if (mainCam == null)
            mainCam = Camera.main?.GetComponent<RobotCamera>();

        return mainCam != null && mainCam.target == transform;
    }

    private void OnDestroy()
    {
        if (lineMaterial != null)
            Destroy(lineMaterial);
    }
}
