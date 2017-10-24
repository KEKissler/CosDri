using UnityEngine;
using System.Collections;


public class StarProperties : MonoBehaviour {

    LineRenderer l;
    public float range;       // default 9
    public float strength;  // default 9
    public bool canMove;    // --UNIMPLEMENTED-- true for temporary sources of gravity, false for permenant   
    public Color color;     // shading color, I guess
    public int numCircleVertecies;// for how many points to define the visual circle around the star to have. More points makes a better looking circle.
    public float circleRenderHeight;

    public StarProperties(float rang, float str)
    {
        range = rang;
        strength = str;
    }
    // needed setter methods so that gameManager can properly instantiate all stars
    public void setRange(float newVal)
    {
        range = newVal;
    }
    public void setStrength(float newVal)
    {
        strength = newVal;
    }

    void Start()
    {
        // drawing visual range circle around the star
        l = gameObject.GetComponent<LineRenderer>();
        //l.material = new Material();
        Vector3[] circle = new Vector3[numCircleVertecies + 1];
        circle[0] = new Vector3(range * Mathf.Cos(0 * Mathf.Deg2Rad) + transform.position.x, range * Mathf.Sin(0 * Mathf.Deg2Rad) + transform.position.y, circleRenderHeight);
        for (float angle = 360f / numCircleVertecies, i = 1; angle < 360; angle += 360f / numCircleVertecies, i++)
        {
            circle[(int)i] = new Vector3(range * Mathf.Cos(angle * Mathf.Deg2Rad) + transform.position.x, range * Mathf.Sin(angle * Mathf.Deg2Rad) + transform.position.y, circleRenderHeight);
        }
        circle[numCircleVertecies] = new Vector3(range * Mathf.Cos(0 * Mathf.Deg2Rad) + transform.position.x, range * Mathf.Sin(0 * Mathf.Deg2Rad) + transform.position.y, circleRenderHeight);
        l.numPositions = circle.Length;
        l.SetPositions(circle);
        l.startColor = color;
        l.endColor = color;
    }
}
