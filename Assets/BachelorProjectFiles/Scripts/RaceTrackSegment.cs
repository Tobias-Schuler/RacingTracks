using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public enum TrackType {
    Straight,
    RightCurve,
    LeftCurve
}

/* 
    A segment of the track
    It has a TrackType to define what kind of segment it is and is initally represented by
    2 Points for Straights
    3 Points for Curves (by a quadratic Bezier curve)
*/
public class TrackSegment {

    Vector2 straightStartPoint;
    Vector2 straightEndPoint;

    Vector2 curveEndPoint;
    Vector2 bezierControlPoint;

    Vector2[] straightPoints;
    Vector2[] rightPathPoints;
    Vector2[] leftPathPoints;

    Vector2[] rightCollisionPoints;
    Vector2[] leftCollisionPoints;

    readonly TrackType trackType;
    int trackTypeOffset = 0;

    readonly int subdivisions = 4;
    public TrackSegment(Vector2 startPoint, float segmentLength) : this(startPoint, segmentLength, Vector2.down) { }

    public TrackSegment(Vector2 startPoint, float segmentLength, Vector2 previousPoint) {
        //points for straight track
        straightStartPoint = startPoint;
        straightEndPoint = (startPoint - previousPoint).normalized * segmentLength + startPoint;
        straightPoints = CreateStraightPoints(segmentLength);

        //generate random bezier control and end point
        curveEndPoint = RotatePointRandom(straightStartPoint, straightEndPoint);
        bezierControlPoint = CreateRandomBezierControlPoint(straightStartPoint, curveEndPoint);

        ReadjustCurve();

        //subdivide bezier curves where point along the curve is closest to control point (the sharpest point of the curve)
        List<Vector2> results = new();
        SubdivideBezier(straightStartPoint, bezierControlPoint, curveEndPoint, subdivisions, results);

        //combine start point, subdivision points and end point for right curve
        results.Insert(0, straightStartPoint);
        results.Add(curveEndPoint);
        rightPathPoints = results.ToArray();


        //mirror/reflect right curve points along straight track axis to get the left curve
        leftPathPoints = new Vector2[rightPathPoints.Length];
        leftPathPoints = ReflectPoints(rightPathPoints, straightStartPoint, straightEndPoint);


        //generate points for collision detection with other tracks
        rightCollisionPoints = CreateRightCurveCollisionPoints(segmentLength);
        leftCollisionPoints = ReflectPoints(rightCollisionPoints, straightStartPoint, straightEndPoint);

        trackType = (TrackType)Random.Range(0, 3);
    }

    public Vector2[] DebugPoints() {
        return new Vector2[] { straightStartPoint, straightEndPoint, bezierControlPoint, curveEndPoint };
    }

    public Vector2 GetStartPoint() {
        switch (CurrentTrackType()) {
            case TrackType.Straight:
                return straightStartPoint;
            case TrackType.RightCurve:
                return rightPathPoints[0];
            case TrackType.LeftCurve:
                return leftPathPoints[0];
            default:
                return new Vector2();
        }
    }

    public Vector2 GetEndPoint() {
        switch (CurrentTrackType()) {
            case TrackType.Straight:
                return straightEndPoint;
            case TrackType.RightCurve:
                return rightPathPoints[^1];
            case TrackType.LeftCurve:
                return leftPathPoints[^1];
            default:
                return new Vector2();
        }
    }

    public Vector2 GetPenultimatePoint() {
        switch (CurrentTrackType()) {
            case TrackType.Straight:
                return straightStartPoint;
            case TrackType.RightCurve:
                return rightPathPoints[rightPathPoints.Length - 2];
            case TrackType.LeftCurve:
                return leftPathPoints[leftPathPoints.Length - 2];
            default:
                return new Vector2();
        }
    }

    public Vector2[] GetPath() {
        return GetPath(CurrentTrackType());
    }

    public Vector2[] GetPath(TrackType type) {
        switch (type) {
            case TrackType.Straight:
                return straightPoints;
            case TrackType.RightCurve:
                return rightPathPoints;
            case TrackType.LeftCurve:
                return leftPathPoints;
            default:
                return new Vector2[] { };
        }
    }

    public Vector2[] GetPathWithoutEndPoint() {
        return GetPath()[..^2];
    }

    public Vector2[] GetCollisionPath() {
        return GetCollisionPath(CurrentTrackType());
    }

    public Vector2[] GetCollisionPath(TrackType type) {
        switch (type) {
            case TrackType.Straight:
                return straightPoints;
            case TrackType.RightCurve:
                return rightCollisionPoints;
            case TrackType.LeftCurve:
                return leftCollisionPoints;
            default:
                return new Vector2[] { };
        }
    }

    public float GetLength() {
        switch (CurrentTrackType()) {
            case TrackType.Straight:
                return GetPointLength(new Vector2[] { straightStartPoint, straightEndPoint });
            case TrackType.RightCurve:
                return GetPointLength(rightPathPoints);
            case TrackType.LeftCurve:
                return GetPointLength(leftPathPoints);
            default:
                return 0;
        }
    }

    public void CycleTrackTypeOffset() {
        trackTypeOffset++;
    }
    public void ResetTrackTypeOffset() {
        trackTypeOffset = 0;
    }

    public int GetTrackTypeOffset() {
        return trackTypeOffset;
    }

    public TrackType CurrentTrackType() {
        int nextTrackType = (int)trackType + trackTypeOffset;
        return (TrackType)(nextTrackType % 3);
    }

    //checks each point against every other point; track width is the diameter of a circle around the point
    public bool CollidesWith(TrackSegment t, float trackWidth) {
        Vector2[] local = GetCollisionPath();
        Vector2[] other = t.GetCollisionPath();

        foreach (Vector2 localVector in local) {
            foreach (Vector2 otherVector in other) {
                float distance = (localVector - otherVector).sqrMagnitude;
                float totalWidth = trackWidth * trackWidth;
                if (distance <= totalWidth * 1.05f) {
                    return true;
                }
            }
        }
        return false;
    }

    public void AdjustAllPoints(TrackSegment segment, float segmentLength) {
        Vector2 rotationPoint = segment.GetEndPoint();
        Vector2 oldDirection = straightEndPoint - straightStartPoint;
        Vector2 newDirection = rotationPoint - segment.GetPenultimatePoint();

        float angle = Vector2.SignedAngle(oldDirection, newDirection);

        //move this so the this start point is on the same position as the segment start point
        Vector2 translationVector = rotationPoint - straightStartPoint;

        straightStartPoint = rotationPoint;
        straightEndPoint = RotatePoint(rotationPoint, straightEndPoint, angle, translationVector);

        curveEndPoint = RotatePoint(rotationPoint, curveEndPoint, angle, translationVector);
        bezierControlPoint = RotatePoint(rotationPoint, bezierControlPoint, angle, translationVector);

        straightPoints = CreateStraightPoints(segmentLength);

        rightPathPoints = RotatePoints(rotationPoint, rightPathPoints, angle, translationVector);
        leftPathPoints = RotatePoints(rotationPoint, leftPathPoints, angle, translationVector);

        rightCollisionPoints = RotatePoints(rotationPoint, rightCollisionPoints, angle, translationVector);
        leftCollisionPoints = RotatePoints(rotationPoint, leftCollisionPoints, angle, translationVector);

    }

    //readjusts bezier control point and curve end point so that the start of the curve aligns well with a straight exit
    private void ReadjustCurve() {
        Vector2 straightDirection = straightEndPoint - straightStartPoint;
        Vector2 curveStartDirection = bezierControlPoint - straightStartPoint;
        float rotation = Vector2.SignedAngle(curveStartDirection, straightDirection);
        bezierControlPoint = RotatePoint(straightStartPoint, bezierControlPoint, rotation);
        curveEndPoint = RotatePoint(straightStartPoint, curveEndPoint, rotation);
    }

    private Vector2[] CreateStraightPoints(float length) {
        List<Vector2> points = new();
        float increment = 1f / (length * 1.5f);
        for (float t = 0; t < 1f; t += increment) {
            points.Add(Vector2.Lerp(straightStartPoint, straightEndPoint, t));
        }
        points.Add(straightEndPoint);
        return points.ToArray();
    }

    private Vector2[] CreateRightCurveCollisionPoints(float length) {
        List<Vector2> points = new();
        float increment = 1f / (length * 1.5f);
        for (float t = 0; t < 1f; t += increment) {
            points.Add(GetPointFromBezier(t));
        }
        points.Add(curveEndPoint);
        return points.ToArray();
    }

    //rotate by a random amount from 0 to 90 degrees clockwise
    private Vector2 RotatePointRandom(Vector2 rotationPoint, Vector2 point) {
        //return a random index between 0 (inclusive) and 90 (exclusive) and subtract that from 360 to get clockwise rotation
        int randomAngle = 360 - Random.Range(0, 90);
        return RotatePoint(rotationPoint, point, randomAngle);
    }

    //rotate a point around another point with angle in degrees
    private Vector2 RotatePoint(Vector2 rotationPoint, Vector2 point, float angle) {
        //translate to origin for rotation
        Vector2 movedToOrigin = point - rotationPoint;
        //get lookup table values for sin and cos for a random angle between 0 and 90 degrees for a clockwise rotation
        float sin = Mathf.Sin(angle * Mathf.Deg2Rad);
        float cos = Mathf.Cos(angle * Mathf.Deg2Rad);
        //get new x and y coordinates of point rotated around coordinate origin
        float newX = movedToOrigin.x * cos - movedToOrigin.y * sin;
        float newY = movedToOrigin.x * sin + movedToOrigin.y * cos;
        //translate point back after rotation and return the new point
        return new Vector2(newX, newY) + rotationPoint;
    }

    private Vector2 RotatePoint(Vector2 rotationPoint, Vector2 point, float angle, Vector2 translationVector) {
        Vector2 newPoint = point + translationVector;
        return RotatePoint(rotationPoint, newPoint, angle);
    }

    private Vector2[] RotatePoints(Vector2 rotationPoint, Vector2[] points, float angle, Vector2 translationVector) {
        Vector2[] newPoints = new Vector2[points.Length];
        for (int i = 0; i < points.Length; i++) {
            newPoints[i] = RotatePoint(rotationPoint, points[i], angle, translationVector);
        }
        return newPoints;
    }

    //create a random bezier control point within a rectangle on the left of the two input points 
    private Vector2 CreateRandomBezierControlPoint(Vector2 startPoint, Vector2 endPoint) {
        float straightLength = (straightEndPoint - straightStartPoint).magnitude;

        Vector2 leftDirection = Vector2.Perpendicular(endPoint - startPoint).normalized;

        Vector2 randomPointOnLine = Vector2.Lerp(startPoint, endPoint, Random.Range(0f, 1f));

        float multiplier = Random.Range(straightLength * 0.5f, straightLength * 1.5f);

        return randomPointOnLine + (leftDirection * multiplier);
    }

    //recursive function to split a bezier at the point closest to its control point and then split the resulting
    //curves further if division level allows
    //number of points as a result equals 2 ^ (division level) - 1
    private void SubdivideBezier(Vector2 startPoint, Vector2 controlPoint, Vector2 endPoint, int divisionLevel, List<Vector2> results) {
        if (divisionLevel > 1) {
            float bestT = NearestToControlPoint(startPoint, controlPoint, endPoint, 0, 1);
            Vector2 subdividePoint = GetPointFromBezier(startPoint, controlPoint, endPoint, bestT);


            Vector2 perpSub = Vector2.Perpendicular(controlPoint - subdividePoint) + subdividePoint;

            Vector2 newControlPoint1 = Intersection(startPoint, controlPoint, subdividePoint, perpSub);
            Vector2 newControlPoint2 = Intersection(subdividePoint, perpSub, controlPoint, endPoint);

            //call and add results from left to right, this ensures results will hold a collection of points in the right order
            SubdivideBezier(startPoint, newControlPoint1, subdividePoint, divisionLevel - 1, results);
            results.Add(subdividePoint);
            SubdivideBezier(subdividePoint, newControlPoint2, endPoint, divisionLevel - 1, results);

        } else {
            float bestT = NearestToControlPoint(startPoint, controlPoint, endPoint, 0, 1);
            Vector2 bestPoint = GetPointFromBezier(startPoint, controlPoint, endPoint, bestT);
            results.Add(bestPoint);
        }
    }

    private Vector2 GetPointFromBezier(Vector2 startPoint, Vector2 controlPoint, Vector2 endPoint, float t) {
        Vector2 first = Vector2.Lerp(startPoint, controlPoint, t);
        Vector2 second = Vector2.Lerp(controlPoint, endPoint, t);
        return Vector2.Lerp(first, second, t);
    }

    private Vector2 GetPointFromBezier(float t) {
        return GetPointFromBezier(straightStartPoint, bezierControlPoint, curveEndPoint, t);
    }

    //recursive function that returns float t for a point on a bezier curve that is (approximately) closest to the control point
    private float NearestToControlPoint(Vector2 startPoint, Vector2 controlPoint, Vector2 endPoint, float startT, float endT) {

        float middle = ((endT - startT) * 0.5f) + startT;
        //Debug.Log("Rec Test: " + a + " to " + b);

        //accuracy threshold/base case to stop recursion
        if (endT - startT <= 0.000005f) {
            //Debug.Log("Finished with t: " + middle + " with result of " + midResult);
            return middle;
        }

        //calculate how far off the starting point and the middle is from 0
        float aResult = GetTangentAtT(startPoint, controlPoint, endPoint, startT);
        float midResult = GetTangentAtT(startPoint, controlPoint, endPoint, middle);

        if (aResult <= 0 && 0 <= midResult) {
            return NearestToControlPoint(startPoint, controlPoint, endPoint, startT, middle);
        } else {
            return NearestToControlPoint(startPoint, controlPoint, endPoint, middle, endT);
        }
    }

    private float GetTangentAtT(Vector2 startPoint, Vector2 controlPoint, Vector2 endPoint, float t) {

        Vector2 v0 = controlPoint - startPoint;
        Vector2 v1 = endPoint - controlPoint;

        float a = Vector2.Dot(v1 - v0, v1 - v0);
        float b = 3f * (Vector2.Dot(v1, v0) - Vector2.Dot(v0, v0));
        float c = (3f * Vector2.Dot(v0, v0)) - Vector2.Dot(v1, v0);
        float d = -Vector2.Dot(v0, v0);

        return (t * t * t * a) + (t * t * b) + (t * c) + d;
    }

    private Vector2 ReflectPoint(Vector2 point, Vector2 axisPoint1, Vector2 axisPoint2) {
        Vector2 axisNormal = Vector2.Perpendicular(axisPoint2 - axisPoint1).normalized;
        return Vector2.Reflect(point - axisPoint1, axisNormal) + axisPoint1;
    }

    private Vector2[] ReflectPoints(Vector2[] points, Vector2 axisPoint1, Vector2 axisPoint2) {
        List<Vector2> reflectedPoints = new();
        foreach (Vector2 point in points) {
            reflectedPoints.Add(ReflectPoint(point, axisPoint1, axisPoint2));
        }
        return reflectedPoints.ToArray();
    }

    //returns the intersection point of two lines based on two vectors
    private Vector2 Intersection(Vector2 v1, Vector2 v2, Vector2 v3, Vector2 v4) {

        float u1 = (v4.x - v3.x) * (v1.y - v3.y) - (v4.y - v3.y) * (v1.x - v3.x);
        float u2 = (v4.y - v3.y) * (v2.x - v1.x) - (v4.x - v3.x) * (v2.y - v1.y);
        float u = u1 / u2;

        if (Mathf.Approximately(u, 0)) {
            Debug.Log("No Intersection");
            return new Vector2();
        }

        float x = v1.x + u * (v2.x - v1.x);
        float y = v1.y + u * (v2.y - v1.y);
        return new Vector2(x, y);
    }

    //gets the length of all points in the specified order
    private float GetPointLength(Vector2[] points) {
        if (points.Length <= 1) {
            return 0;
        }
        float total = 0;
        for (int i = 1; i < points.Length; i++) {
            total += (points[i] - points[i - 1]).magnitude;
        }
        return total;
    }
}