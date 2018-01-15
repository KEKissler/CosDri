using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class ParallaxLayer{
    public Sprite sprite;
    public Vector2 camDelta, scale;
    private GameObject instance;

    public GameObject getInstance() { return instance; }
    public void setInstance(GameObject newVal) { instance = newVal; }
}
