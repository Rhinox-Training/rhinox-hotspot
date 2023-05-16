using Rhinox.Lightspeed;
using Rhinox.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshBoundsCalculator
{
    public static void GetNavmeshRects(out List<List<Bounds>> listOfBoundList, int mask, in Mesh mesh, float margins)
    {
        listOfBoundList = new List<List<Bounds>>();

        var edgeLoopsList = NavMeshHelper.GetOuterEdgeLoops(mesh, true);
        foreach (var edgeLoop in edgeLoopsList)
        {
            List<Bounds> boundsList = new List<Bounds>
            {
                mesh.bounds
            };

            foreach (var edge in edgeLoop)
            {
                var edgeDir = edge.V2 - edge.V1;
                Vector3 nrml = Vector3.Cross(edgeDir.normalized, Vector3.up);

                if (!nrml.TryGetCardinalAxis(out Axis cardinalAxis))
                    continue;

                SubdivideBounds(ref boundsList, cardinalAxis, edge.V1);
            }

            // Discard pass
            boundsList.RemoveAll(x => !IsOnNavMesh(x, mask));
            if (boundsList.Count == 0)
                continue;

            // Merge pass
            CheckMergeAligning(ref boundsList);

            //go over all passing bounds and add margins
            for (int i = 0; i < boundsList.Count; ++i)
            {
                boundsList[i] = boundsList[i].AddMarginToExtends(margins);
            }

            listOfBoundList.Add(boundsList);
        }
    }

    //creates sample points inside the bounds (with a certain offset from the borders)
    //amount of sample points is increments²
    //checks if ALL these sample points fall on the navmesh, if not then return false.
    private static bool IsOnNavMesh(Bounds bounds, int mask, int increments = 4)
    {
        float stepX = bounds.size.x / increments;
        float stepY = bounds.size.z / increments;

        float samplePointBorderOffsetX = 0.5f * stepX;
        float samplePointBorderOffsetY = 0.5f * stepY;

        for (int i = 0; i < increments; ++i)
        {
            float x = bounds.center.x - bounds.extents.x + i * stepX + samplePointBorderOffsetX;

            for (int j = 0; j < increments; ++j)
            {
                float y = bounds.center.z - bounds.extents.z + j * stepY + samplePointBorderOffsetY;

                //lift sample point 0.5f above navmesh
                Vector3 pt = new Vector3(x, bounds.center.y + 0.5f, y);

                //use sample ray lenght of 1f
                if (NavMesh.SamplePosition(pt, out var hitResult, 1f, mask))
                {
                    if (hitResult.distance > 0.5f)
                        return false;
                }
                else
                    return false;
            }
        }

        return true;
    }

    private static void SubdivideBounds(ref List<Bounds> boundsList, Axis axis, Vector3 pointOnPlane)
    {
        var subdividedBounds = new List<Bounds>();
        foreach (var bounds in boundsList)
        {
            if (bounds.TrySliceBounds(axis, pointOnPlane, out Bounds halfBounds1, out Bounds halfBounds2))
            {
                subdividedBounds.Add(halfBounds1);
                subdividedBounds.Add(halfBounds2);
            }
            else
            {
                subdividedBounds.Add(bounds);
            }
        }

        boundsList = subdividedBounds;
    }

    private static void CheckMergeAligning(ref List<Bounds> boundsList)
    {
        if (boundsList == null)
            return;


        int count = boundsList.Count;
        int newCount = -1;
        while (count != newCount)
        {
            count = boundsList.Count;
            boundsList.SortBy(GetSmallestAxisSize);
            SinglePassSearchForAligningBounds(ref boundsList);
            newCount = boundsList.Count;
        }
    }

    private static IComparable GetSmallestAxisSize(Bounds arg)
    {
        return MathF.Min(MathF.Min(arg.size.x, arg.size.y), arg.size.z);
    }

    private static void SinglePassSearchForAligningBounds(ref List<Bounds> boundsList)
    {
        for (var i = 0; i < boundsList.Count; ++i)
        {
            var bound = boundsList[i];
            for (var j = 0; j < boundsList.Count; ++j)
            {
                if (i == j)
                    continue;
                var otherBound = boundsList[j];
                if (AlignedBoxes(bound, otherBound))
                {
                    bound.Encapsulate(otherBound);
                    boundsList[i] = bound;
                    boundsList.RemoveAt(j);
                    return;
                }
            }
        }
    }

    private static bool AlignedBoxes(Bounds bound, Bounds otherBound)
    {
        Vector3 centerAxis = (otherBound.center - bound.center).normalized;
        if (!centerAxis.TryGetCardinalAxis(out var axis))
            return false;
        return TestBoundAlignmentOnAxis(bound, otherBound, axis);
    }

    private static bool TestBoundAlignmentOnAxis(Bounds bound, Bounds otherBound, Axis axis)
    {
        Vector3 corner1 = bound.min;
        Vector3 corner2 = GetCorner(bound, false, axis);

        Vector3 lineSegmentA = corner2 - corner1;

        if (!lineSegmentA.IsColinear(otherBound.min - corner2))
            return false;

        Vector3 corner3 = bound.max;
        Vector3 corner4 = GetCorner(bound, true, axis);

        Vector3 lineSegmentB = corner4 - corner3;

        if (!lineSegmentB.IsColinear(otherBound.max - corner3))
            return false;
        return AreTouching(bound, otherBound, axis, 0.0001f);
    }

    private static bool AreTouching(Bounds bound, Bounds otherBound, Axis axis, float epsilon = float.Epsilon)
    {
        switch (axis)
        {
            case Axis.X:
                return CheckTouchingSegments(bound.min.x, bound.max.x, otherBound.min.x, otherBound.max.x, epsilon);
            case Axis.Y:
                return CheckTouchingSegments(bound.min.y, bound.max.y, otherBound.min.y, otherBound.max.y, epsilon);
            case Axis.Z:
                return CheckTouchingSegments(bound.min.z, bound.max.z, otherBound.min.z, otherBound.max.z, epsilon);
            default:
                throw new ArgumentOutOfRangeException(nameof(axis), axis, null);
        }
    }

    private static bool CheckTouchingSegments(float minA, float maxA, float minB, float maxB, float epsilon = float.Epsilon)
    {
        return Mathf.Min(minA, maxA).LossyEquals(Mathf.Max(minB, maxB), epsilon) ||
               Mathf.Max(minA, maxA).LossyEquals(Mathf.Min(minB, maxB), epsilon);
    }

    private static Vector3 GetCorner(Bounds bound, bool maxNotMinFlipped, Axis axis)
    {
        switch (axis)
        {
            case Axis.X:
                if (!maxNotMinFlipped)
                    return new Vector3(bound.max.x, bound.min.y, bound.min.z);
                else
                    return new Vector3(bound.min.x, bound.max.y, bound.max.z);
            case Axis.Y:
                if (!maxNotMinFlipped)
                    return new Vector3(bound.min.x, bound.max.y, bound.min.z);
                else
                    return new Vector3(bound.max.x, bound.min.y, bound.max.z);
            case Axis.Z:
                if (!maxNotMinFlipped)
                    return new Vector3(bound.min.x, bound.min.y, bound.max.z);
                else
                    return new Vector3(bound.max.x, bound.max.y, bound.min.z);
            default:
                throw new ArgumentOutOfRangeException(nameof(axis), axis, null);
        }
    }
}