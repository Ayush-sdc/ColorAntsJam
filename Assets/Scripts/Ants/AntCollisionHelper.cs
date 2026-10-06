using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Authoritative collision proxy and continuous swept contact calculation for ant queues.
/// Derives 2D Oriented Bounding Boxes (OBB) on the floor (XZ plane) from actual ant visual dimensions,
/// scales, and poses to prevent mesh penetration during failed moves.
/// </summary>
public static class AntCollisionHelper
{
    public const float DefaultClearance = 0.04f;

    /// <summary>
    /// 2D Oriented Bounding Box on the XZ floor plane.
    /// </summary>
    public struct AntBox2D
    {
        public Vector2 center;     // (x, z)
        public Vector2 axisFwd;    // Normalized forward direction in XZ
        public Vector2 axisRight;  // Normalized lateral direction in XZ
        public float halfLength;   // Half-dimension along forward axis (including clearance)
        public float halfWidth;    // Half-dimension along lateral axis (including clearance)
        public float clearance;

        public AntBox2D(Vector2 center, Vector2 forward, float halfLength, float halfWidth, float clearance)
        {
            this.center = center;
            this.axisFwd = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector2.up;
            this.axisRight = new Vector2(this.axisFwd.y, -this.axisFwd.x);
            this.halfLength = halfLength;
            this.halfWidth = halfWidth;
            this.clearance = clearance;
        }

        public bool IsValid => halfLength > 0f && halfWidth > 0f;
    }

    /// <summary>
    /// Computes the 2D collision proxy for an AntUnit at a specified world position and rotation.
    /// </summary>
    public static AntBox2D GetUnitProxy(AntUnit unit, Vector3 worldPosition, Quaternion worldRotation, float clearance = DefaultClearance)
    {
        if (unit == null)
        {
            return new AntBox2D(new Vector2(worldPosition.x, worldPosition.z), Vector2.up, 0.4f + clearance, 0.3f + clearance, clearance);
        }

        bool isHead = unit.IsHead;
        var visualSettings = AntQueueVisualSettings.Instance;

        float visualScale = isHead 
            ? (visualSettings != null ? visualSettings.LeaderVisualScale : 0.38f)
            : (visualSettings != null ? visualSettings.WorkerVisualScale : 0.35f);

        // AntUnit or queue root scale
        float unitScale = unit.transform != null ? unit.transform.lossyScale.x : 1.0f;
        if (unitScale <= 0.001f) unitScale = 1.0f;

        float baseFrontExtent;
        float baseRearExtent;
        float baseHalfWidth;

        if (isHead)
        {
            // ChubbyAntColors at reference scale 0.38: extents = (0.31, 0.21, 0.52), center = (0, 0.21, 0)
            float normScale = (visualScale / 0.38f) * unitScale;
            baseFrontExtent = 0.52f * normScale;
            baseRearExtent = 0.52f * normScale;
            baseHalfWidth = 0.31f * normScale;
        }
        else
        {
            // WorkerAnt at reference scale 0.35: extents = (0.29, 0.19, 0.41), center = (0, 0.20, -0.07)
            // min.z = -0.48, max.z = +0.35
            float normScale = (visualScale / 0.35f) * unitScale;
            baseFrontExtent = 0.35f * normScale;
            baseRearExtent = 0.48f * normScale;
            baseHalfWidth = 0.29f * normScale;
        }

        float halfLen = ((baseFrontExtent + baseRearExtent) * 0.5f) + clearance;
        float halfWid = baseHalfWidth + clearance;
        float fwdOffset = ((baseFrontExtent - baseRearExtent) * 0.5f);

        Vector3 fwd3D = worldRotation * Vector3.forward;
        Vector2 fwd2D = new Vector2(fwd3D.x, fwd3D.z);
        if (fwd2D.sqrMagnitude < 0.0001f) fwd2D = Vector2.up;
        else fwd2D.Normalize();

        Vector2 center2D = new Vector2(worldPosition.x, worldPosition.z) + fwd2D * fwdOffset;
        return new AntBox2D(center2D, fwd2D, halfLen, halfWid, clearance);
    }

    /// <summary>
    /// Computes the 2D collision proxy for an AntUnit at its current transform position and rotation.
    /// </summary>
    public static AntBox2D GetUnitProxy(AntUnit unit, float clearance = DefaultClearance)
    {
        if (unit == null) return default;
        return GetUnitProxy(unit, unit.transform.position, unit.transform.rotation, clearance);
    }

    /// <summary>
    /// Tests intersection between two 2D OBBs using the Separating Axis Theorem (SAT).
    /// Returns true if the proxies overlap.
    /// </summary>
    public static bool CheckOverlap(in AntBox2D a, in AntBox2D b)
    {
        if (!a.IsValid || !b.IsValid) return false;

        Vector2 d = b.center - a.center;

        // Axis 1: a.axisFwd
        float dist1 = Mathf.Abs(Vector2.Dot(d, a.axisFwd));
        float radius1 = a.halfLength +
                        b.halfLength * Mathf.Abs(Vector2.Dot(b.axisFwd, a.axisFwd)) +
                        b.halfWidth * Mathf.Abs(Vector2.Dot(b.axisRight, a.axisFwd));
        if (dist1 > radius1) return false;

        // Axis 2: a.axisRight
        float dist2 = Mathf.Abs(Vector2.Dot(d, a.axisRight));
        float radius2 = a.halfWidth +
                        b.halfLength * Mathf.Abs(Vector2.Dot(b.axisFwd, a.axisRight)) +
                        b.halfWidth * Mathf.Abs(Vector2.Dot(b.axisRight, a.axisRight));
        if (dist2 > radius2) return false;

        // Axis 3: b.axisFwd
        float dist3 = Mathf.Abs(Vector2.Dot(d, b.axisFwd));
        float radius3 = b.halfLength +
                        a.halfLength * Mathf.Abs(Vector2.Dot(a.axisFwd, b.axisFwd)) +
                        a.halfWidth * Mathf.Abs(Vector2.Dot(a.axisRight, b.axisFwd));
        if (dist3 > radius3) return false;

        // Axis 4: b.axisRight
        float dist4 = Mathf.Abs(Vector2.Dot(d, b.axisRight));
        float radius4 = b.halfWidth +
                        a.halfLength * Mathf.Abs(Vector2.Dot(a.axisFwd, b.axisRight)) +
                        a.halfWidth * Mathf.Abs(Vector2.Dot(a.axisRight, b.axisRight));
        if (dist4 > radius4) return false;

        return true;
    }

    /// <summary>
    /// Calculates the minimum separation distance (clearance gap) between two 2D OBBs along the separating axes.
    /// Returns a negative value if penetrating.
    /// </summary>
    public static float GetSeparationDistance(in AntBox2D a, in AntBox2D b)
    {
        if (!a.IsValid || !b.IsValid) return float.MaxValue;

        Vector2 d = b.center - a.center;

        float maxSeparation = float.MinValue;

        // Axis 1: a.axisFwd
        float dist1 = Mathf.Abs(Vector2.Dot(d, a.axisFwd));
        float radius1 = a.halfLength +
                        b.halfLength * Mathf.Abs(Vector2.Dot(b.axisFwd, a.axisFwd)) +
                        b.halfWidth * Mathf.Abs(Vector2.Dot(b.axisRight, a.axisFwd));
        float sep1 = dist1 - radius1;
        if (sep1 > maxSeparation) maxSeparation = sep1;

        // Axis 2: a.axisRight
        float dist2 = Mathf.Abs(Vector2.Dot(d, a.axisRight));
        float radius2 = a.halfWidth +
                        b.halfLength * Mathf.Abs(Vector2.Dot(b.axisFwd, a.axisRight)) +
                        b.halfWidth * Mathf.Abs(Vector2.Dot(b.axisRight, a.axisRight));
        float sep2 = dist2 - radius2;
        if (sep2 > maxSeparation) maxSeparation = sep2;

        // Axis 3: b.axisFwd
        float dist3 = Mathf.Abs(Vector2.Dot(d, b.axisFwd));
        float radius3 = b.halfLength +
                        a.halfLength * Mathf.Abs(Vector2.Dot(a.axisFwd, b.axisFwd)) +
                        a.halfWidth * Mathf.Abs(Vector2.Dot(a.axisRight, b.axisFwd));
        float sep3 = dist3 - radius3;
        if (sep3 > maxSeparation) maxSeparation = sep3;

        // Axis 4: b.axisRight
        float dist4 = Mathf.Abs(Vector2.Dot(d, b.axisRight));
        float radius4 = b.halfWidth +
                        a.halfLength * Mathf.Abs(Vector2.Dot(a.axisFwd, b.axisRight)) +
                        a.halfWidth * Mathf.Abs(Vector2.Dot(a.axisRight, b.axisRight));
        float sep4 = dist4 - radius4;
        if (sep4 > maxSeparation) maxSeparation = sep4;

        return maxSeparation;
    }

    /// <summary>
    /// Computes the precise continuous contact distance s >= 0 along the approach route
    /// before any moving queue unit's proxy overlaps any blocker queue unit's proxy.
    /// Clamps movement to first contact with guaranteed zero mesh penetration.
    /// </summary>
    public static float ComputeSweptContactDistance(
        AntQueueController movingQueue,
        List<Vector3> fullApproachRoute,
        AntQueueController blockerQueue,
        float clearance = DefaultClearance)
    {
        if (movingQueue == null || blockerQueue == null || fullApproachRoute == null || fullApproachRoute.Count < 2)
        {
            return 0f;
        }

        // Cache blocker proxies
        var blockerUnits = blockerQueue.Units;
        if (blockerUnits == null || blockerUnits.Count == 0) return 0f;

        List<AntBox2D> blockerBoxes = new List<AntBox2D>(blockerUnits.Count);
        for (int i = 0; i < blockerUnits.Count; i++)
        {
            if (blockerUnits[i] != null)
            {
                blockerBoxes.Add(GetUnitProxy(blockerUnits[i], clearance));
            }
        }

        if (blockerBoxes.Count == 0) return 0f;

        // Extract route total length and points
        float maxRouteLen = 0f;
        for (int i = 0; i < fullApproachRoute.Count - 1; i++)
        {
            maxRouteLen += Vector3.Distance(fullApproachRoute[i], fullApproachRoute[i + 1]);
        }

        if (maxRouteLen <= 0.001f) return 0f;

        var movingUnits = movingQueue.Units;
        if (movingUnits == null || movingUnits.Count == 0) return 0f;

        float segmentDist = movingQueue.SegmentDistance;
        if (segmentDist <= 0.001f) segmentDist = 1.0f;

        // Get initial trail points from the queue
        // A moving queue's trail has initial points representing body coordinates from tail to head
        List<Vector3> initialTrailPositions = new List<Vector3>();
        List<Vector3> initialTrailForwards = new List<Vector3>();

        var bodyCoords = movingQueue.CurrentBodyCoordinates;
        if (bodyCoords != null && bodyCoords.Count > 0)
        {
            // Build body coordinates from tail to head
            for (int i = bodyCoords.Count - 1; i >= 0; i--)
            {
                Vector3 p = movingQueue.GetWorldPositionForGrid(bodyCoords[i]);
                Vector3 fwd = Vector3.forward;
                if (i > 0)
                {
                    Vector3 prevP = movingQueue.GetWorldPositionForGrid(bodyCoords[i - 1]);
                    fwd = prevP - p;
                    fwd.y = 0f;
                    if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.forward;
                    else fwd.Normalize();
                }
                else if (movingUnits[0] != null)
                {
                    fwd = movingUnits[0].transform.forward;
                }
                initialTrailPositions.Add(p);
                initialTrailForwards.Add(fwd);
            }
        }

        // Helper to evaluate if collision occurs at advance distance s
        bool CheckCollisionAtDistance(float s)
        {
            // Sample all moving units at advance distance s
            // For unit k (0 = head): trail distance is totalInitialDist + s - k * segmentDist
            for (int k = 0; k < movingUnits.Count; k++)
            {
                var u = movingUnits[k];
                if (u == null) continue;

                Vector3 pos;
                Quaternion rot;

                float unitDistFromHead = k * segmentDist;
                if (s >= unitDistFromHead)
                {
                    // Unit has entered the approach route
                    float routeDist = s - unitDistFromHead;
                    SampleRoute(fullApproachRoute, routeDist, out pos, out Vector3 fwd);
                    rot = Quaternion.LookRotation(fwd, Vector3.up);
                }
                else
                {
                    // Unit is still on the initial body path
                    float backFromHead = unitDistFromHead - s;
                    SampleInitialBody(initialTrailPositions, initialTrailForwards, backFromHead, out pos, out Vector3 fwd);
                    rot = Quaternion.LookRotation(fwd, Vector3.up);
                }

                AntBox2D movingBox = GetUnitProxy(u, pos, rot, clearance);

                for (int b = 0; b < blockerBoxes.Count; b++)
                {
                    if (CheckOverlap(movingBox, blockerBoxes[b]))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // 1. Initial check at s = 0: if already touching/overlapping, safe distance is 0
        if (CheckCollisionAtDistance(0f))
        {
            return 0f;
        }

        // 2. Coarse sweep forward to find collision bracket
        float stepSize = 0.04f;
        float currS = 0f;
        float hitS = -1f;

        while (currS < maxRouteLen)
        {
            currS += stepSize;
            if (currS > maxRouteLen) currS = maxRouteLen;

            if (CheckCollisionAtDistance(currS))
            {
                hitS = currS;
                break;
            }
        }

        // If no collision up to maxRouteLen, the entire route is safe
        if (hitS < 0f)
        {
            return maxRouteLen;
        }

        // 3. Binary bisection search to pinpoint exact first contact with zero penetration
        float low = hitS - stepSize;
        if (low < 0f) low = 0f;
        float high = hitS;

        for (int iter = 0; iter < 12; iter++)
        {
            float mid = (low + high) * 0.5f;
            if (CheckCollisionAtDistance(mid))
            {
                high = mid;
            }
            else
            {
                low = mid;
            }
        }

        // low is guaranteed to be collision-free and within 0.0001f of physical contact
        return Mathf.Max(0f, low);
    }

    /// <summary>
    /// Computes the 2D collision proxy for a hole's outer rim on the floor plane.
    /// </summary>
    public static AntBox2D GetHoleProxy(HoleController hole, float clearance = 0f)
    {
        if (hole == null) return default;
        Bounds bounds = hole.GetOuterRimBounds();
        Vector2 center2D = new Vector2(bounds.center.x, bounds.center.z);
        float halfLength = (bounds.size.z * 0.5f) + clearance;
        float halfWidth = (bounds.size.x * 0.5f) + clearance;
        return new AntBox2D(center2D, Vector2.up, halfLength, halfWidth, clearance);
    }

    /// <summary>
    /// Computes the exact swept contact distance along fullApproachRoute where the moving queue's leading
    /// visual edge touches the target hole's outer rim, with the specified clearance.
    /// Clamps movement so that the ant's visual front stops at the outer rim and the pivot never enters the hole.
    /// </summary>
    public static float ComputeSweptContactDistanceWithHole(
        AntQueueController movingQueue,
        List<Vector3> fullApproachRoute,
        HoleController targetHole,
        float clearance = DefaultClearance)
    {
        if (movingQueue == null || targetHole == null || fullApproachRoute == null || fullApproachRoute.Count < 2)
        {
            return 0f;
        }

        AntBox2D holeBox = GetHoleProxy(targetHole, 0f);
        if (!holeBox.IsValid) return 0f;

        float maxRouteLen = 0f;
        for (int i = 0; i < fullApproachRoute.Count - 1; i++)
        {
            maxRouteLen += Vector3.Distance(fullApproachRoute[i], fullApproachRoute[i + 1]);
        }

        if (maxRouteLen <= 0.001f) return 0f;

        var movingUnits = movingQueue.Units;
        if (movingUnits == null || movingUnits.Count == 0) return 0f;

        float segmentDist = movingQueue.SegmentDistance;
        if (segmentDist <= 0.001f) segmentDist = 1.0f;

        List<Vector3> initialTrailPositions = new List<Vector3>();
        List<Vector3> initialTrailForwards = new List<Vector3>();

        var bodyCoords = movingQueue.CurrentBodyCoordinates;
        if (bodyCoords != null && bodyCoords.Count > 0)
        {
            for (int i = bodyCoords.Count - 1; i >= 0; i--)
            {
                Vector3 p = movingQueue.GetWorldPositionForGrid(bodyCoords[i]);
                Vector3 fwd = Vector3.forward;
                if (i > 0)
                {
                    Vector3 prevP = movingQueue.GetWorldPositionForGrid(bodyCoords[i - 1]);
                    fwd = prevP - p;
                    fwd.y = 0f;
                    if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.forward;
                    else fwd.Normalize();
                }
                else if (movingUnits[0] != null)
                {
                    fwd = movingUnits[0].transform.forward;
                }
                initialTrailPositions.Add(p);
                initialTrailForwards.Add(fwd);
            }
        }

        bool CheckCollisionAtDistance(float s)
        {
            for (int k = 0; k < movingUnits.Count; k++)
            {
                var u = movingUnits[k];
                if (u == null) continue;

                Vector3 pos;
                Quaternion rot;

                float unitDistFromHead = k * segmentDist;
                if (s >= unitDistFromHead)
                {
                    float routeDist = s - unitDistFromHead;
                    SampleRoute(fullApproachRoute, routeDist, out pos, out Vector3 fwd);
                    rot = Quaternion.LookRotation(fwd, Vector3.up);
                }
                else
                {
                    float backFromHead = unitDistFromHead - s;
                    SampleInitialBody(initialTrailPositions, initialTrailForwards, backFromHead, out pos, out Vector3 fwd);
                    rot = Quaternion.LookRotation(fwd, Vector3.up);
                }

                AntBox2D movingBox = GetUnitProxy(u, pos, rot, clearance);
                if (CheckOverlap(movingBox, holeBox))
                {
                    return true;
                }
            }
            return false;
        }

        if (CheckCollisionAtDistance(0f))
        {
            return 0f;
        }

        float step = 0.02f;
        float coarseDist = maxRouteLen;
        bool foundContact = false;

        for (float s = step; s <= maxRouteLen; s += step)
        {
            if (CheckCollisionAtDistance(s))
            {
                coarseDist = s;
                foundContact = true;
                break;
            }
        }

        if (!foundContact && CheckCollisionAtDistance(maxRouteLen))
        {
            coarseDist = maxRouteLen;
            foundContact = true;
        }

        if (!foundContact)
        {
            return maxRouteLen;
        }

        // Bisection refinement
        float low = Mathf.Max(0f, coarseDist - step);
        float high = coarseDist;

        for (int iter = 0; iter < 12; iter++)
        {
            float mid = (low + high) * 0.5f;
            if (CheckCollisionAtDistance(mid))
            {
                high = mid;
            }
            else
            {
                low = mid;
            }
        }

        return Mathf.Max(0f, low);
    }

    private static void SampleRoute(List<Vector3> route, float dist, out Vector3 pos, out Vector3 fwd)
    {
        pos = route[0];
        fwd = (route.Count > 1 ? (route[1] - route[0]).normalized : Vector3.forward);
        if (dist <= 0f) return;

        float accumulated = 0f;
        for (int i = 0; i < route.Count - 1; i++)
        {
            float seg = Vector3.Distance(route[i], route[i + 1]);
            if (accumulated + seg >= dist)
            {
                float remain = dist - accumulated;
                float t = seg > 0.0001f ? (remain / seg) : 0f;
                pos = Vector3.Lerp(route[i], route[i + 1], t);
                Vector3 d = route[i + 1] - route[i];
                d.y = 0f;
                fwd = d.sqrMagnitude > 0.0001f ? d.normalized : Vector3.forward;
                return;
            }
            accumulated += seg;
        }

        pos = route[route.Count - 1];
        if (route.Count >= 2)
        {
            Vector3 d = route[route.Count - 1] - route[route.Count - 2];
            d.y = 0f;
            fwd = d.sqrMagnitude > 0.0001f ? d.normalized : Vector3.forward;
        }
    }

    private static void SampleInitialBody(List<Vector3> trailPos, List<Vector3> trailFwd, float distBackFromHead, out Vector3 pos, out Vector3 fwd)
    {
        if (trailPos == null || trailPos.Count == 0)
        {
            pos = Vector3.zero;
            fwd = Vector3.forward;
            return;
        }

        int lastIdx = trailPos.Count - 1; // Head is at lastIdx
        pos = trailPos[lastIdx];
        fwd = trailFwd[lastIdx];
        if (distBackFromHead <= 0f) return;

        float accumulated = 0f;
        for (int i = lastIdx; i > 0; i--)
        {
            float seg = Vector3.Distance(trailPos[i], trailPos[i - 1]);
            if (accumulated + seg >= distBackFromHead)
            {
                float remain = distBackFromHead - accumulated;
                float t = seg > 0.0001f ? (remain / seg) : 0f;
                pos = Vector3.Lerp(trailPos[i], trailPos[i - 1], t);
                fwd = trailFwd[i];
                return;
            }
            accumulated += seg;
        }

        pos = trailPos[0];
        fwd = trailFwd[0];
    }
}
