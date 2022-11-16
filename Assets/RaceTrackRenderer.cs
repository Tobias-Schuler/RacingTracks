using System;
using System.Collections.Generic;
using UnityEngine;

struct TrackRenderSlice {

    Vector3[] _vertices;
    public TrackRenderSlice(Vector3[] vertices) {
        _vertices = vertices;
    }

    public Vector3[] Vertices {
        get {
            return _vertices;
        }
        set {
            _vertices = value;
        }
    }
}

public class RaceTrackRenderer : MonoBehaviour {

    [SerializeField]
    Material trackMaterial;

    readonly int sliceSize = 8;
    public Vector3[] RenderTrack(TrackSegment[] track, float width, float railWidth, float railHeight) {
        List<Vector3> vertices = new List<Vector3>();
        //List<TrackRenderSlice> slices = new List<TrackRenderSlice>();
        //List<Vector3> pathPoints = new List<Vector3>();

        int counter = 0;

        TrackRenderSlice[][] allTrackSlices = new TrackRenderSlice[track.Length][];

        for (int i = 0; i < track.Length; i++) {
            TrackSegment current = track[i];
            TrackRenderSlice[] trackSlices = CreateTrackRenderSlices(current, width, railWidth, railHeight);
            allTrackSlices[i] = trackSlices;
        }

        for (int i = 0; i < allTrackSlices.Length; i++) {
            TrackRenderSlice[] current = allTrackSlices[i];
            //if there is a track segment coming after
            if (i < allTrackSlices.Length - 1) {
                TrackRenderSlice[] next = allTrackSlices[i + 1];
                current[^1] = next[0];      //set the last slice of current to the first slice of next to ensure a smooth transition
            }
            vertices.AddRange(CreateTrackMesh("Track " + counter++, current));
        }

        //foreach (TrackSegment t in track) {
        //pathPoints.AddRange(Convert2DVectors(t.GetPathWithoutEndPoint()));
        //slices.AddRange(trackSlices);
        //}

        //pathPoints.Add(Convert2DVector(track[^1].GetEndPoint()));

        /*Vector3[] pathArray = pathPoints.ToArray();

        Vector3 nextDirection = (pathArray[1] - pathArray[0]).normalized;
        Vector3 prevDirection = nextDirection;

        for (int i = 0; i < pathArray.Length; i++) {
            if (i < pathArray.Length - 1) {
                nextDirection = (pathArray[i + 1] - pathArray[i]).normalized;
            }
            if (i > 1) {
                prevDirection = (pathArray[i] - pathArray[i - 1]).normalized;
            }

            Vector3 thisPoint = pathArray[i];
            Vector3 perp = Vector3.Cross(prevDirection.normalized + nextDirection.normalized, Vector3.up).normalized;
            slices.Add(CreateTrackRenderSlice(thisPoint, perp, width, railWidth, railHeight));
            
        }
        vertices.AddRange(CreateTrackMesh("Track", slices.ToArray()));*/
        return vertices.ToArray();
    }



    /* schematic of a track slice
     * 
     points that need to get generated from one track point (thisPoint), this is a slice view, numbers are indices

        <------ perpendicular to thisPoint 

      1 leftUpOut----leftUpIn 2             5 rightUpIn----rightUpOut 6     ^
            |           |                         |             |           |  railHeight
            |           | 3                     4 |             |           |
            |         leftIn-----thisPoint-----rightIn          |           v 
            |                                                   |
            |                                                   |
            |                                                   |           
            |                                                   |           
     0 leftDownOut----------------------------------------rightDownOut 7   
    
            <---------------------width------------------------->
     */
    private TrackRenderSlice[] CreateTrackRenderSlices(TrackSegment t, float width, float railWidth, float railHeight) {
        Vector3[] pathPoints = Convert2DVectors(t.GetPath());

        TrackRenderSlice[] slices = new TrackRenderSlice[pathPoints.Length];

        for (int i = 0; i < pathPoints.Length; i++) {
            //use direction towards next point if it exists
            if (i < pathPoints.Length - 1) {
                Vector3 thisPoint = pathPoints[i];
                Vector3 nextPoint = pathPoints[i + 1];
                Vector3 direction = nextPoint - thisPoint;
                Vector3 perp = Vector3.Cross(direction, Vector3.up).normalized;
                slices[i] = CreateTrackRenderSlice(thisPoint, perp, width, railWidth, railHeight);
            } else { //otherwise base its direction on previous point
                Vector3 thisPoint = pathPoints[i];
                Vector3 direction = thisPoint - pathPoints[i - 1];
                Vector3 perp = Vector3.Cross(direction, Vector3.up).normalized;
                slices[i] = CreateTrackRenderSlice(thisPoint, perp, width, railWidth, railHeight);
            }
        }
        return slices;
    }

    private TrackRenderSlice CreateTrackRenderSlice(Vector3 thisPoint, Vector3 perp, float width, float railWidth, float railHeight) {
        Vector3 leftUpOut = thisPoint + (perp * width / 2) + (Vector3.up * railHeight);
        Vector3 leftDownOut = leftUpOut + (Vector3.down * railHeight) + (Vector3.down * 0.25f);
        Vector3 leftUpIn = leftUpOut - (perp * railWidth);
        Vector3 leftIn = leftUpIn + (Vector3.down * railHeight);

        Vector3 rightUpOut = thisPoint - (perp * width / 2) + (Vector3.up * railHeight);
        Vector3 rightDownOut = rightUpOut + (Vector3.down * railHeight) + (Vector3.down * 0.25f);
        Vector3 rightUpIn = rightUpOut + (perp * railWidth);
        Vector3 rightIn = rightUpIn + (Vector3.down * railHeight);
        return new TrackRenderSlice(new Vector3[] { leftDownOut, leftUpOut, leftUpIn, leftIn, rightIn, rightUpIn, rightUpOut, rightDownOut });
    }

    private Vector3[] CreateTrackMesh(string identifier, TrackRenderSlice[] slices) {
        GameObject newTrackPiece = new GameObject(identifier);
        newTrackPiece.transform.parent = this.transform;

        //retrieve vertices
        List<Vector3> vertices = new List<Vector3>();
        foreach (TrackRenderSlice slice in slices) {
            vertices.AddRange(slice.Vertices);
        }

        //generate triangle indices
        List<int> triangles = new List<int>();
        triangles.AddRange(DrawFrontFace(0));
        for (int i = 0; i < slices.Length - 1; i++) {
            int startIndex = sliceSize * i;      //the starting index for this slice
            int nextIndex = startIndex + sliceSize;
            //draw left outwards triangles
            triangles.AddRange(DrawQuad(startIndex + 1, startIndex + 0, nextIndex + 0, nextIndex + 1));
            //draw left railing top triangles
            triangles.AddRange(DrawQuad(startIndex + 2, startIndex + 1, nextIndex + 1, nextIndex + 2));
            //draw left inwards triangles
            triangles.AddRange(DrawQuad(startIndex + 3, startIndex + 2, nextIndex + 2, nextIndex + 3));
            //draw road triangles
            triangles.AddRange(DrawQuad(startIndex + 4, startIndex + 3, nextIndex + 3, nextIndex + 4));
            //draw right inwards triangles
            triangles.AddRange(DrawQuad(startIndex + 5, startIndex + 4, nextIndex + 4, nextIndex + 5));
            //draw right railing top triangles
            triangles.AddRange(DrawQuad(startIndex + 6, startIndex + 5, nextIndex + 5, nextIndex + 6));
            //draw right outwards triangles
            triangles.AddRange(DrawQuad(startIndex + 7, startIndex + 6, nextIndex + 6, nextIndex + 7));
            //draw under track triangles
            triangles.AddRange(DrawQuad(startIndex + 0, startIndex + 7, nextIndex + 7, nextIndex + 0));
        }
        int lastIndex = sliceSize * (slices.Length - 1);
        triangles.AddRange(DrawBackFace(lastIndex));

        MeshFilter m = newTrackPiece.AddComponent<MeshFilter>();
        m.mesh.vertices = vertices.ToArray();
        m.mesh.triangles = triangles.ToArray();
        m.mesh.RecalculateNormals();
        m.mesh.RecalculateTangents();
        newTrackPiece.AddComponent<MeshRenderer>().material = trackMaterial;
        newTrackPiece.AddComponent<MeshCollider>();

        return vertices.ToArray();
    }

    //indices have to be clockwise order for unity to determine front
    private int[] DrawQuad(int a1, int a2, int b1, int b2) {
        return new int[] { a1, a2, b1, b1, b2, a1 };
    }

    //fill the mesh hole at the start of a track piece
    private int[] DrawFrontFace(int startIndex) {
        return new int[] {
            startIndex + 0, startIndex + 1, startIndex + 3,
            startIndex + 1 ,startIndex + 2, startIndex + 3,
            startIndex + 0, startIndex + 3, startIndex + 7,
            startIndex + 3, startIndex + 4, startIndex + 7,
            startIndex + 4, startIndex + 5, startIndex + 6,
            startIndex + 4, startIndex + 6, startIndex + 7,
        };
    }

    //fill the mesh hole at the end of a track piece, this is simply the reverse order to drawing the front
    private int[] DrawBackFace(int startIndex) {
        int[] result = DrawFrontFace(startIndex);
        Array.Reverse(result);
        return result;
    }

    private Vector3[] Convert2DVectors(Vector2[] vectors) {
        Vector3[] result = new Vector3[vectors.Length];
        for (int i = 0; i < vectors.Length; i++) {
            Vector2 v = vectors[i];
            result[i] = new Vector3(v.x, 0, v.y); ;
        }
        return result;
    }

    /*private Vector3 Convert2DVector(Vector2 v) {
        return new Vector3(v.x, 0, v.y);
    }
*/

    /* private string VectorArrayOutput(Vector3[] input) {
         string result = "";
         foreach (Vector3 v in input) {
             result += "{ " + v.x.ToString("F2") + "|" + v.y.ToString("F2") + "|" + v.z.ToString("F2") + "}\n";
         }
         return result;
     }*/

}
