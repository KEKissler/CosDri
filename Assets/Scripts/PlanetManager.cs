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
    private Vector3 rotation;
    public Vector3 rotToUse;

	// Use this for initialization
	void Start () {
        rotation = (!useProvidedRotation)?new Vector3(Random.Range(-5.0f, 5.0f), Random.Range(-5.0f, 5.0f), 0f):rotToUse;
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
        Quaternion newQuat = transform.rotation;
        newQuat.eulerAngles += rotation;
        transform.rotation = newQuat;
        //transform.rotation.eulerAngles += rotationSpeed * rotation;
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
            rotation = new Vector3(Random.Range(-5.0f, 5.0f), Random.Range(-5.0f, 5.0f), 0f);
        }
	}
}
