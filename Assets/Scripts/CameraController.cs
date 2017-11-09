using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour {
    public float panSpeed, panBoxPercentageScreenSize;
    public GameObject focus;// object around which to focus
    public float weighting; // between 0 and 1, weights the averaging (effectively speed) of positions of the camera and the focus
    public bool snapToFocus;// if true, moves camera towards focus, else doesnt mess with camera
    private Camera cam;

	// Use this for initialization
	void Start () {
        cam = GetComponent<Camera>();
        snapToFocus = false;
	}
	
	// Update is called once per frame
	void FixedUpdate () {
        if (snapToFocus)// automatic averaging mode
        {
            transform.position = weighting * transform.position + (1 - weighting) * focus.transform.position;
        }//edge pan manual movement mode
        else
        {
            Vector3 relPos = cam.ScreenToViewportPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, transform.position.z));

            //checks x position edge pan
            if (relPos.x >= 0 && relPos.x < panBoxPercentageScreenSize)
            {
                transform.position = new Vector3(transform.position.x - panSpeed, transform.position.y, transform.position.z);
            }
            else if (relPos.x <= 1 && relPos.x > 1-panBoxPercentageScreenSize)
            {
                transform.position = new Vector3(transform.position.x + panSpeed, transform.position.y, transform.position.z);
            }

            //checks y position edge pan
            if (relPos.y >= 0 && relPos.y < panBoxPercentageScreenSize)
            {
                transform.position = new Vector3(transform.position.x, transform.position.y - panSpeed, transform.position.z);
            }
            else if (relPos.y <= 1 && relPos.y > 1 - panBoxPercentageScreenSize)
            {
                transform.position = new Vector3(transform.position.x, transform.position.y + panSpeed, transform.position.z);
            }
        }
    }

    public void focusTo(GameObject other, float weight, bool enable)// public function to do the focus thing with this cam to a given object
    {
        if (enable)
        {
            transform.position = weighting * transform.position + (1 - weighting) * other.transform.position;
        }
    }
}
