using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileProperties : MonoBehaviour {

    LineRenderer l;

    public bool specialTile = false;
    
    public int gravityStrength = 0;
    public enum GravDir {right, upRight, up, upLeft, left, downLeft, down, downRight, none};// do not edit order plz
    public enum SpecialModifier {refuel, finish, offMap, none};

    public GravDir gravityDirection = GravDir.none;

	// Use this for initialization
	void Start () {
        l = gameObject.GetComponent<LineRenderer>();
    }
	
	// Update is called once per frame
	void Update () {
        if (gravityDirection != GravDir.none)
        {
            if (gravityDirection == GravDir.right)
            {
                Vector3[] temp = { new Vector3(this.transform.position.x, this.transform.position.y, -1f), new Vector3(this.transform.position.x + 1f / 2 * Mathf.Cos(0 * Mathf.Deg2Rad), this.transform.position.y + 1f / 2 * Mathf.Sin(0 * Mathf.Deg2Rad), -1f) };
                l.SetPositions(temp);
            }
            else if (gravityDirection == GravDir.upRight)
            {
                Vector3[] temp = { new Vector3(this.transform.position.x, this.transform.position.y, -1f), new Vector3(this.transform.position.x + 1f / Mathf.Sqrt(2) * Mathf.Cos(45 * Mathf.Deg2Rad), this.transform.position.y + 1f / Mathf.Sqrt(2) * Mathf.Sin(45 * Mathf.Deg2Rad), -1f) };
                l.SetPositions(temp);
            }
            else if (gravityDirection == GravDir.up)
            {
                Vector3[] temp = { new Vector3(this.transform.position.x, this.transform.position.y, -1f), new Vector3(this.transform.position.x + 1f / 2 * Mathf.Cos(90 * Mathf.Deg2Rad), this.transform.position.y + 1f / 2 * Mathf.Sin(90 * Mathf.Deg2Rad), -1f) };
                l.SetPositions(temp);
            }
            else if (gravityDirection == GravDir.upLeft)
            {
                Vector3[] temp = { new Vector3(this.transform.position.x, this.transform.position.y, -1f), new Vector3(this.transform.position.x + 1f / Mathf.Sqrt(2) * Mathf.Cos(135 * Mathf.Deg2Rad), this.transform.position.y + 1f / Mathf.Sqrt(2) * Mathf.Sin(135 * Mathf.Deg2Rad), -1f) };
                l.SetPositions(temp);
            }
            else if (gravityDirection == GravDir.left)
            {
                Vector3[] temp = { new Vector3(this.transform.position.x, this.transform.position.y, -1f), new Vector3(this.transform.position.x + 1f / 2 * Mathf.Cos(180 * Mathf.Deg2Rad), this.transform.position.y + 1f / 2 * Mathf.Sin(180 * Mathf.Deg2Rad), -1f) };
                l.SetPositions(temp);
            }
            else if (gravityDirection == GravDir.downLeft)
            {
                Vector3[] temp = { new Vector3(this.transform.position.x, this.transform.position.y, -1f), new Vector3(this.transform.position.x + 1f / Mathf.Sqrt(2) * Mathf.Cos(225 * Mathf.Deg2Rad), this.transform.position.y + 1f / Mathf.Sqrt(2) * Mathf.Sin(225 * Mathf.Deg2Rad), -1f) };
                l.SetPositions(temp);
            }
            else if (gravityDirection == GravDir.down)
            {
                Vector3[] temp = { new Vector3(this.transform.position.x, this.transform.position.y, -1f), new Vector3(this.transform.position.x + 1f / 2 * Mathf.Cos(270 * Mathf.Deg2Rad), this.transform.position.y + 1f / 2 * Mathf.Sin(270 * Mathf.Deg2Rad), -1f) };
                l.SetPositions(temp);
            }
            else
            {
                Vector3[] temp = { new Vector3(this.transform.position.x, this.transform.position.y, -1f), new Vector3(this.transform.position.x + 1f / Mathf.Sqrt(2) * Mathf.Cos(315 * Mathf.Deg2Rad), this.transform.position.y + 1f / Mathf.Sqrt(2) * Mathf.Sin(315 * Mathf.Deg2Rad), -1f) };
                l.SetPositions(temp);
            }
        }
    }
}
