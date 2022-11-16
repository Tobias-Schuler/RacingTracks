using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;


//RaceTrack Parameters
public static class RTP {
    public static int seed = (int)DateTime.Now.Ticks;
    public static int trackLength = 2500;
    public static float segmentLength = 30;
    public static float segmentWidth = 7;
    public static float railWidth = 0.5f;
    public static float railHeight = 1f;
}

public class RaceTrackGenerator : MonoBehaviour {

    Stack<TrackSegment> raceTrack = new();

    [SerializeField]
    GameObject pointDebugger;

    // Start is called before the first frame update
    void Start() {

        //-1329884933
        //int timeSeed = (int)DateTime.Now.Ticks;
        //int timeSeed = -1329884933;
        //int timeSeed = 2050203530;           //great seed, try 1000 and 820 length to see how algorithm resolves spatial issues for l=10, w=2

        GenerateNewTrack();
    }

    private void GenerateRaceTrack(float minLength) {
        float currentLength = raceTrack.Peek().GetLength();
        Stack<TrackSegment> savedSegments = new();

        while (currentLength <= minLength) {

            //add new segment to intermediate stack for later placement
            if (savedSegments.Count == 0) {
                TrackSegment previous = raceTrack.Peek();
                savedSegments.Push(new TrackSegment(previous.GetEndPoint(), RTP.segmentLength, previous.GetPenultimatePoint()));
                continue;
            }

            TrackSegment currentSegment = savedSegments.Peek();
            TrackSegment previousSegment = raceTrack.Peek();

            //if end point of previous and start point of current don't match, recalculate current's points
            if (previousSegment.GetEndPoint() != currentSegment.GetStartPoint()) {
                currentSegment.AdjustAllPoints(previousSegment, RTP.segmentLength);
            }


            //placement attempt, hasn't been completely cycled through
            if (currentSegment.GetTrackTypeOffset() < 3) {

                TrackSegment[] toTestAgainst = raceTrack.ToArray()[1..];
                bool isColliding = false;
                foreach (TrackSegment priorSegment in toTestAgainst) {
                    isColliding = currentSegment.CollidesWith(priorSegment, RTP.segmentWidth);
                    if (isColliding) {
                        currentSegment.CycleTrackTypeOffset();
                        break;
                    }
                }
                if (isColliding) {
                    continue;
                }

            } else {
                //Debug.Log("Rolling back to previous segment at " + raceTrack.Count);
                currentSegment.ResetTrackTypeOffset();
                TrackSegment previous = raceTrack.Pop();
                previous.CycleTrackTypeOffset();
                savedSegments.Push(previous);
                continue;
            }

            raceTrack.Push(savedSegments.Pop());
            //calculate new track length
            currentLength = CalculateTrackLength();
        }

    }

    private float CalculateTrackLength() {
        if (raceTrack.Count > 1) {
            float totalLength = 0;
            foreach (TrackSegment segment in raceTrack) {
                totalLength += segment.GetLength();
            }
            return totalLength;
        } else if (raceTrack.Count == 1) {
            return raceTrack.Peek().GetLength();
        }
        return 0;
    }

    public void GenerateNewTrack() {
        Random.InitState(RTP.seed);
        foreach (Transform child in transform) {
            Destroy(child.gameObject);
        }
        raceTrack = new();

        raceTrack.Push(new TrackSegment(new Vector2(0, 0), RTP.segmentLength));
        GenerateRaceTrack(RTP.trackLength);

        TrackSegment[] raceTrackArray = raceTrack.ToArray();
        Array.Reverse(raceTrackArray);

        //pointDebugger.GetComponent<PointDebuggerScript>().RenderTrackSegments("RaceTrack", raceTrackArray);
        this.GetComponent<RaceTrackRenderer>().RenderTrack(raceTrackArray, RTP.segmentWidth, RTP.railWidth, RTP.railHeight);
    }

    private Vector2[] GetTrackPoints(TrackSegment[] track) {
        if (track.Length == 0) {
            return new Vector2[] { };
        }
        if (track.Length == 1) {
            return track[0].GetPath();
        }
        List<Vector2> trackPoints = new();
        foreach (TrackSegment segment in track) {
            trackPoints.AddRange(segment.GetPathWithoutEndPoint());
        }
        trackPoints.Add(track[^1].GetEndPoint());
        return trackPoints.ToArray();
    }

}
