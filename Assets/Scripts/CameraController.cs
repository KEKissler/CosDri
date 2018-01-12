using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour {
    public float panSpeed, panBoxPercentageScreenSize, zoomSpeed, maxCamSize, minCamSize;
    public GameObject focus;// object around which to focus
    public float weighting; // between 0 and 1, weights the averaging (effectively speed) of positions of the camera and the focus
    public bool snapToFocus;// if true, moves camera towards focus, else doesnt mess with camera
    private Camera cam;
    public float leftMapLimit, rightMapLimit, upMapLimit, downMapLimit;
    private Vector3 originalClickPosition;

	// Use this for initialization
	void Start () {
        cam = GetComponent<Camera>();
        snapToFocus = false;
	}
	
	// Update is called once per frame
    void Update()
    {
        if (Input.GetAxis("Mouse ScrollWheel") < 0 && cam.orthographicSize + zoomSpeed * Time.deltaTime < maxCamSize)
        {
            cam.orthographicSize += zoomSpeed * Time.deltaTime;
            if (Mathf.Abs(cam.ViewportToWorldPoint(new Vector3()).x - cam.ViewportToWorldPoint(new Vector3(1f, 1f, 1f)).x) > Mathf.Abs((leftMapLimit - rightMapLimit)) || Mathf.Abs(cam.ViewportToWorldPoint(new Vector3()).y - cam.ViewportToWorldPoint(new Vector3(1f, 1f, 1f)).y) > Mathf.Abs(downMapLimit - upMapLimit))
            {
                //too big, undoing size increase
                cam.orthographicSize -= zoomSpeed * Time.deltaTime;
            }
        }
        else if (Input.GetAxis("Mouse ScrollWheel") > 0 && cam.orthographicSize - zoomSpeed * Time.deltaTime > minCamSize)
        {
            cam.orthographicSize -= zoomSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.Mouse2))
        {
            if (Input.GetKeyDown(KeyCode.Mouse2))
            {
                originalClickPosition = cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, transform.position.z));
            }
            Vector3 toModifyCamPos = cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, transform.position.z)) - originalClickPosition;
            toModifyCamPos.z = 0f;
            transform.position -= toModifyCamPos;
        }

        //moving camera back to valid area
        Vector3 toCorrectCamPos = new Vector3();
        //left and right corrections
        if (cam.ViewportToWorldPoint(new Vector3()).x < leftMapLimit)
        {
            toCorrectCamPos.x = cam.ViewportToWorldPoint(new Vector3()).x - leftMapLimit;
        }
        else if (cam.ViewportToWorldPoint(new Vector3(1f, 1f, 1f)).x > rightMapLimit)
        {
            toCorrectCamPos.x = cam.ViewportToWorldPoint(new Vector3(1f, 1f, 1f)).x - rightMapLimit;
        }
        //up and down corrections
        if (cam.ViewportToWorldPoint(new Vector3()).y < downMapLimit)
        {
            toCorrectCamPos.y = cam.ViewportToWorldPoint(new Vector3()).y - downMapLimit;
        }
        else if (cam.ViewportToWorldPoint(new Vector3(1f, 1f, 1f)).y > upMapLimit)
        {
            toCorrectCamPos.y = cam.ViewportToWorldPoint(new Vector3(1f, 1f, 1f)).y - upMapLimit;
        }
        //applying correction
        transform.position -= toCorrectCamPos;

    }

	void FixedUpdate () {
        if (snapToFocus)// automatic averaging mode
        {
            transform.position = weighting * transform.position + (1 - weighting) * focus.transform.position;
        }//edge pan manual movement mode
        else
        {
            Vector3 relPos = cam.ScreenToViewportPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, transform.position.z));
            //checks x position edge pan
            if (relPos.x < panBoxPercentageScreenSize)
            {
                if (cam.ViewportToWorldPoint(new Vector3()).x - panSpeed > leftMapLimit)
                {
                    transform.position = new Vector3(transform.position.x - panSpeed, transform.position.y, transform.position.z);
                }
            }
            else if (relPos.x > 1-panBoxPercentageScreenSize)
            {
                if (cam.ViewportToWorldPoint(new Vector3(1f, 1f, 1f)).x + panSpeed < rightMapLimit)
                {
                    transform.position = new Vector3(transform.position.x + panSpeed, transform.position.y, transform.position.z);
                }
            }

            //checks y position edge pan
            if (relPos.y < panBoxPercentageScreenSize)
            {
                if(cam.ViewportToWorldPoint(new Vector3()).y - panSpeed > downMapLimit)
                {
                    transform.position = new Vector3(transform.position.x, transform.position.y - panSpeed, transform.position.z);
                }
            }
            else if (relPos.y > 1 - panBoxPercentageScreenSize)
            {
                if (cam.ViewportToWorldPoint(new Vector3(1f, 1f, 1f)).y + panSpeed < upMapLimit)
                {
                    transform.position = new Vector3(transform.position.x, transform.position.y + panSpeed, transform.position.z);
                }
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
