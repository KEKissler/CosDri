using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class Activator {
    public enum ActivatorType { Fuel, Checkpoint }
    public ActivatorType type;
    public bool reusable;
    public Sprite sprite;
    public Vector2[] centers;
    public float[] ranges;
}
