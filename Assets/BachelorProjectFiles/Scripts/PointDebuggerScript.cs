using System;
using System.Collections.Generic;
using UnityEngine;


//component to display points in Unity as colored spheres
public class PointDebuggerScript : MonoBehaviour {

    //a dictionary storing a group of Points based on a string identifier, useful for later deletion
    private Dictionary<string, GameObject> allPoints = new Dictionary<string, GameObject>();
    [SerializeField]
    private Boolean showEditorPoints = false;
    //Points declared within the Unity Editor
    [SerializeField]
    private Vector3[] editorPoints = new Vector3[] { new Vector3(0, 0, 0), new Vector3(5, 0, 0), new Vector3(0, 0, 5) };

    [SerializeField]
    private GameObject ball;

    private Color currentColor = Color.red;

    // Start is called before the first frame update
    void Start() {

        if (showEditorPoints) {
            RenderPoints("startup", editorPoints);
        }
    }

    //change color of prefab for Points placed after this method call
    private void ChangePointColor(Color color) {
        currentColor = color;
    }

    public void RenderPoint(Vector3 p) {
        Vector3 currentPos = p;
        GameObject point = Instantiate(ball, currentPos, transform.rotation, this.transform);
        point.name = "Point";
        string text = "\n" + currentPos.x.ToString("F2") + "\n" + currentPos.z.ToString("F2");
        point.transform.GetChild(0).GetComponent<TextMesh>().text = text;
        point.GetComponent<Renderer>().material.SetColor("_BaseColor", currentColor);
    }

    public void RenderPoint(Vector2 p) {
        RenderPoint(ConvertPoint2DTo3D(p));
    }

    //render a collection of points and save them in the dictionary via the identifier for later access
    public void RenderPoints(string identifier, Vector3[] points) {
        GameObject parentOfPoints = new GameObject(identifier);
        parentOfPoints.transform.parent = this.transform;

        for (int i = 0; i < points.Length; i++) {
            Vector3 currentPos = points[i];
            GameObject point = Instantiate(ball, currentPos, transform.rotation, parentOfPoints.transform);
            point.name = "Point " + i + " (" + currentPos.x.ToString("F2") + "|" + currentPos.z.ToString("F2") + ")";
            string text = i + "\n" + currentPos.x.ToString("F2") + "\n" + currentPos.z.ToString("F2");
            GameObject textObject = point.transform.GetChild(0).gameObject;
            textObject.GetComponent<TextMesh>().text = text;
            point.GetComponent<Renderer>().material.SetColor("_BaseColor", currentColor);
        }

        allPoints.Add(identifier, parentOfPoints);
    }

    //method overload of above that additionally sets the color
    public void RenderPoints(string identifier, Vector3[] points, Color color) {
        ChangePointColor(color);
        RenderPoints(identifier, points);
    }

    //converts a 2D vector to a 3D vector with y becoming the z coordinate and setting height (y) of the 3D vector to 0
    // (x,y) ==> (x, 0, y)
    private Vector3[] ConvertVectors2Dto3D(Vector2[] vectors) {
        Vector3[] newPoints = new Vector3[vectors.Length];

        for (int i = 0; i < vectors.Length; i++) {
            Vector2 currentPoint = vectors[i];
            newPoints[i] = ConvertPoint2DTo3D(currentPoint);
        }
        return newPoints;
    }

    private Vector3 ConvertPoint2DTo3D(Vector2 v) {
        return new Vector3(v.x, 0, v.y);
    }
    public void RenderPoints(string identifier, Vector2[] points) {
        RenderPoints(identifier, ConvertVectors2Dto3D(points));
    }

    public void RenderPoints(string identifier, Vector2[] points, Color color) {
        //foreach (Vector2 point in points) {
        //    Debug.Log(point);
        //}
        ChangePointColor(color);
        RenderPoints(identifier, ConvertVectors2Dto3D(points));
    }

    public void RenderTrackSegments(string identifier, TrackSegment[] track) {
        Color[] curveColors = new Color[] { Color.red, Color.blue, Color.green };
        for (int i = 0; i < track.Length; i++) {
            Color curColor = curveColors[(int)track[i].CurrentTrackType()];
            ChangePointColor(curColor);
            RenderPoints(identifier + " " + i, track[i].GetPath());
            //ChangePointColor(Color.cyan);
            //RenderPoints("debug " + i, track[i].DebugPoints());
        }
        RenderPoint(track[^1].GetEndPoint());
    }

    private void DebugArray<T>(T[] array) {
        string output = "Array: { ";
        foreach (T t in array) {
            output += t.ToString() + ", ";
        }
        Debug.Log(output + " }");
    }
}
