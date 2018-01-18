using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanetManager : MonoBehaviour {
    public bool selectColorsOnlyFromList, useProvidedRotation;
    public Texture[] textures;
    public Color[] colors;
    private Material mat;
    private Texture selectedTex;
    private Color selectedColor;
    public Vector3 rotToUse;
    private Quaternion rotation = new Quaternion();

	// Use this for initialization
	void Start () {
        rotation.eulerAngles = (!useProvidedRotation) ? new Vector3(Random.Range(-5.0f, 5.0f), Random.Range(-5.0f, 5.0f), 0f) : rotToUse;

        mat =this.GetComponent<MeshRenderer>().material;
        if (selectColorsOnlyFromList)
        {
            mat.SetColor("_Color", colors[Random.Range(0, colors.Length)]);
        }
        else
        {
            mat.SetColor("_Color", Random.ColorHSV(0f,1f,1f,1f,0.5f,1f));
        }
        mat.SetTexture("_MainTex", textures[Random.Range(0, textures.Length)]);
        //    mat.SetFloatArray("_Color", new List<float>());
        //    this.GetComponent<MeshRenderer>().materials = new Material[] { mat };//SetTexture("_MainTex", textures[Random.Range(0, textures.Length)]);
        
    }
	void FixedUpdate()
    {
        transform.rotation = transform.rotation * rotation;
    }
	// Update is called once per frame
	void Update () {
        //Debug.Log(rotation);
        if (Input.GetKeyDown(KeyCode.I))
        {
            //mat = this.GetComponent<MeshRenderer>().material;
            if (selectColorsOnlyFromList && colors.Length > 0)
            {
                mat.SetColor("_Color", colors[Random.Range(0, colors.Length)]);
            }
            else
            {
                mat.SetColor("_Color", Random.ColorHSV(0f, 1f, 0f, 1f, 0f, 1f));
            }
            mat.SetTexture("_MainTex", textures[Random.Range(0, textures.Length)]);
        }
        if (Input.GetKeyDown(KeyCode.O) && !useProvidedRotation)
        {
            Quaternion nextQuat = new Quaternion();
            nextQuat.eulerAngles = new Vector3(Random.Range(-5.0f, 5.0f), Random.Range(-5.0f, 5.0f), 0f);
            rotation = nextQuat;
        }
	}
}
