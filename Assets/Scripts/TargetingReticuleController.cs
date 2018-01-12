using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetingReticuleController : MonoBehaviour {

    public Camera cam;
    private SpriteRenderer sr;

	// Use this for initialization
	void Start () {
        sr = GetComponent<SpriteRenderer>();
	}
	
	// Update is called once per frame
	void Update () {
        //move cursor to grid tile nearest player's cursor
        transform.position = resolveToTile(Input.mousePosition.x, Input.mousePosition.y);
    }

    Vector2 resolveToTile(float mouseX, float mouseY)
    {
        Vector3 cursorWorldPosition = cam.ScreenToWorldPoint(new Vector3(mouseX, mouseY, cam.transform.position.z));
        return new Vector2((Mathf.Floor(cursorWorldPosition.x) + 1 / 2f), (Mathf.Floor(cursorWorldPosition.y) + 1 / 2f));
    }

    public void setColor(Color newColor)
    {
        sr.color = newColor;
    }
}
