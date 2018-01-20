using UnityEngine;
public struct TileProperties{
    public int gravityStrength;
    public enum GravDir {right, upRight, up, upLeft, left, downLeft, down, downRight, none};// do not edit order plz
    public GravDir gravityDirection;
    public Vector2 position;
}
