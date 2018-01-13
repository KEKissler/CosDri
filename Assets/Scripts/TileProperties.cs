public class TileProperties {
    public float x = 0, y = 0;
    public int gravityStrength = 0;
    public enum GravDir {right, upRight, up, upLeft, left, downLeft, down, downRight, none};// do not edit order plz
    public GravDir gravityDirection = GravDir.none;
}
