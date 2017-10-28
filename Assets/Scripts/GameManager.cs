using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour {
    // prefabs
    public TileProperties tile;
    public StarProperties star;
    public Player player;

    // empty objects in scene view for neat nesting thing. Helps in unity editor.
    public GameObject tileParent, starParent, playerParent;

    // tile vars
    public int MAP_LEN, MAP_HGHT; // direcly influences tiles to instantiate
    public TileProperties[,] tiles;// local tile data

    // star vars
    public Vector4[] starData = new Vector4[4];// local star data
    private StarProperties[] stars;// dont use this for anything, specific for grav calculations

    // player vars
    public int playersToCreate;// num characters in this game. 1-4 are valid entries
    public Player[] players = new Player[4]; // a list to hold them locally
    public Vector2[] spawnLocations = new Vector2[4]; // a list of where to create the people
    public Color[] playerColors = new Color[4];// targeting reticule color
    public Color momentumIndicatorColor, gravityIndicatorColor, movementIndicatorColor, resultantIndicatorColor;// colors for helpful lines
    public Sprite[] playerSprites = new Sprite[4];// for ships, I guess. Will add more art vars here later as needed.
    public int fuelSpawnAmount, maxFuelLimit; // default fuel setting specifications
    public Camera mainCamera; // for giving to players so that they can all align their targeting reticules to the grid properly
    public GameObject targetingReticulePrefab; // for giving to each player
    private int selectedPlayer; // one player may have a turn at a time. selectedPlayer tracks that player.
    public float zPosition;// determines height to place player objects
    

    // Use this for initialization
    void Start () {
        // tiles
        tiles = new TileProperties[MAP_LEN, MAP_HGHT];
        for (int i = 0; i < MAP_LEN; i++)
        {
            for (int j = 0; j < MAP_HGHT; j++)
            {// this whole +1/2f thing is correct here. don't change it.
                //original index to position mapping
                tiles[i, j] = Instantiate(tile, new Vector2((i + 1f / 2 - MAP_LEN / 2f), (j + 1f / 2 - MAP_HGHT / 2f)), Quaternion.identity, tileParent.transform);
            }
        }

        // stars
        stars = new StarProperties[starData.Length];
        for (int k = 0; k < starData.Length; k++)
        {
            stars[k] = Instantiate(star, new Vector3(starData[k].z-MAP_LEN/2f,starData[k].w-MAP_HGHT/2f,0f), Quaternion.identity, starParent.transform);
            stars[k].setRange(starData[k].x);
            stars[k].setStrength(starData[k].y);
        }

        // players
        Debug.Log("Creating " + playersToCreate + " player(s).");
        for (int i = 0; i < playersToCreate; i++)
        {
            Vector3 positionAtWhichToCreatePlayer = new Vector3(spawnLocations[i].x + 1 / 2f - MAP_LEN / 2f, spawnLocations[i].y + 1 / 2f - MAP_HGHT / 2f, zPosition);
            players[i] = Instantiate(player, positionAtWhichToCreatePlayer, Quaternion.identity, playerParent.transform);
            //manually setting the player's currentPosition Vector here, as opposed to where it used to be in the player's start function.
            // turns out that even though it was *just* instantiated, its start function isnt called yet. So in order to actually call updateGravAndMomentum, below, we need its currentPsotion value to be initialized
            players[i].setCurrentPosition(positionAtWhichToCreatePlayer);
            players[i].fuel = fuelSpawnAmount;
            players[i].FUEL_LIMIT = maxFuelLimit;
            players[i].cam = mainCamera;
            players[i].gameManager = this;
            players[i].targetingReticulePrefab = targetingReticulePrefab;
            players[i].reticuleColor = playerColors[i];
            players[i].momentumIndicator = momentumIndicatorColor;
            players[i].gravIndicator = gravityIndicatorColor;
            players[i].movementIndicator = movementIndicatorColor;
            players[i].resultantIndicator = resultantIndicatorColor;
            players[i].ship = playerSprites[i];
            players[i].zPos = zPosition;
            players[i].MAP_HGHT = MAP_HGHT;
            players[i].MAP_LEN = MAP_LEN;
        }
        selectedPlayer = 0;
        players[selectedPlayer].setCursorIsActive(true);
        players[selectedPlayer].updateGravAndMomentum();
        Debug.Log("It is now Player " + selectedPlayer + "'s turn!");

        // applying gravity to tiles
        for (int i = 0; i < MAP_LEN; i++)
        {
            for (int j = 0; j < MAP_HGHT; j++)
            {
                Vector2 collectiveGrav = new Vector2();
                foreach (StarProperties s in stars)
                {
                    float distToStar = Vector2.Distance(tiles[i, j].transform.position, s.transform.position);
                    if (distToStar <= s.range)
                    {
                        collectiveGrav += s.strength * Mathf.Pow((s.range - distToStar)/s.range, 2f) * ((Vector2)(s.transform.position - tiles[i, j].transform.position)).normalized;
                    }
                    
                }
                if (collectiveGrav.magnitude != 0f)
                {
                    tiles[i, j].gravityStrength = (int)Mathf.Ceil(collectiveGrav.magnitude);
                    if (Vector2.Dot(collectiveGrav, new Vector2(Mathf.Cos(45f * Mathf.Deg2Rad), Mathf.Sin(45f * Mathf.Deg2Rad))) > Vector2.Dot(collectiveGrav, new Vector2(Mathf.Cos(0f * Mathf.Deg2Rad), Mathf.Sin(0f * Mathf.Deg2Rad))) && Vector2.Dot(collectiveGrav, new Vector2(Mathf.Cos(45f * Mathf.Deg2Rad), Mathf.Sin(45f * Mathf.Deg2Rad))) > Vector2.Dot(collectiveGrav, new Vector2(Mathf.Cos(90f * Mathf.Deg2Rad), Mathf.Sin(90f * Mathf.Deg2Rad))))
                    {
                        tiles[i, j].gravityDirection = TileProperties.GravDir.upRight;
                    }
                    else if (Vector2.Dot(collectiveGrav, new Vector2(Mathf.Cos(90f * Mathf.Deg2Rad), Mathf.Sin(90f * Mathf.Deg2Rad))) > Vector2.Dot(collectiveGrav, new Vector2(Mathf.Cos(45f * Mathf.Deg2Rad), Mathf.Sin(45f * Mathf.Deg2Rad))) && Vector2.Dot(collectiveGrav, new Vector2(Mathf.Cos(90f * Mathf.Deg2Rad), Mathf.Sin(90f * Mathf.Deg2Rad))) > Vector2.Dot(collectiveGrav, new Vector2(Mathf.Cos(135f * Mathf.Deg2Rad), Mathf.Sin(135f * Mathf.Deg2Rad))))
                    {
                        tiles[i, j].gravityDirection = TileProperties.GravDir.up;
                    }
                    else if (Vector2.Dot(collectiveGrav, new Vector2(Mathf.Cos(135f * Mathf.Deg2Rad), Mathf.Sin(135f * Mathf.Deg2Rad))) > Vector2.Dot(collectiveGrav, new Vector2(Mathf.Cos(90f * Mathf.Deg2Rad), Mathf.Sin(90f * Mathf.Deg2Rad))) && Vector2.Dot(collectiveGrav, new Vector2(Mathf.Cos(135f * Mathf.Deg2Rad), Mathf.Sin(135f * Mathf.Deg2Rad))) > Vector2.Dot(collectiveGrav, new Vector2(Mathf.Cos(180f * Mathf.Deg2Rad), Mathf.Sin(180f * Mathf.Deg2Rad))))
                    {
                        tiles[i, j].gravityDirection = TileProperties.GravDir.upLeft;
                    }
                    else if (Vector2.Dot(collectiveGrav, new Vector2(Mathf.Cos(180f * Mathf.Deg2Rad), Mathf.Sin(180f * Mathf.Deg2Rad))) > Vector2.Dot(collectiveGrav, new Vector2(Mathf.Cos(135f * Mathf.Deg2Rad), Mathf.Sin(135f * Mathf.Deg2Rad))) && Vector2.Dot(collectiveGrav, new Vector2(Mathf.Cos(180f * Mathf.Deg2Rad), Mathf.Sin(180f * Mathf.Deg2Rad))) > Vector2.Dot(collectiveGrav, new Vector2(Mathf.Cos(225f * Mathf.Deg2Rad), Mathf.Sin(225f * Mathf.Deg2Rad))))
                    {
                        tiles[i, j].gravityDirection = TileProperties.GravDir.left;
                    }
                    else if (Vector2.Dot(collectiveGrav, new Vector2(Mathf.Cos(225f * Mathf.Deg2Rad), Mathf.Sin(225f * Mathf.Deg2Rad))) > Vector2.Dot(collectiveGrav, new Vector2(Mathf.Cos(180f * Mathf.Deg2Rad), Mathf.Sin(180f * Mathf.Deg2Rad))) && Vector2.Dot(collectiveGrav, new Vector2(Mathf.Cos(225f * Mathf.Deg2Rad), Mathf.Sin(225f * Mathf.Deg2Rad))) > Vector2.Dot(collectiveGrav, new Vector2(Mathf.Cos(270f * Mathf.Deg2Rad), Mathf.Sin(270f * Mathf.Deg2Rad))))
                    {
                        tiles[i, j].gravityDirection = TileProperties.GravDir.downLeft;
                    }
                    else if (Vector2.Dot(collectiveGrav, new Vector2(Mathf.Cos(270f * Mathf.Deg2Rad), Mathf.Sin(270f * Mathf.Deg2Rad))) > Vector2.Dot(collectiveGrav, new Vector2(Mathf.Cos(225f * Mathf.Deg2Rad), Mathf.Sin(225f * Mathf.Deg2Rad))) && Vector2.Dot(collectiveGrav, new Vector2(Mathf.Cos(270f * Mathf.Deg2Rad), Mathf.Sin(270f * Mathf.Deg2Rad))) > Vector2.Dot(collectiveGrav, new Vector2(Mathf.Cos(315f * Mathf.Deg2Rad), Mathf.Sin(315f * Mathf.Deg2Rad))))
                    {
                        tiles[i, j].gravityDirection = TileProperties.GravDir.down;
                    }
                    else if (Vector2.Dot(collectiveGrav, new Vector2(Mathf.Cos(315f * Mathf.Deg2Rad), Mathf.Sin(315f * Mathf.Deg2Rad))) > Vector2.Dot(collectiveGrav, new Vector2(Mathf.Cos(270f * Mathf.Deg2Rad), Mathf.Sin(270f * Mathf.Deg2Rad))) && Vector2.Dot(collectiveGrav, new Vector2(Mathf.Cos(315f * Mathf.Deg2Rad), Mathf.Sin(315f * Mathf.Deg2Rad))) > Vector2.Dot(collectiveGrav, new Vector2(Mathf.Cos(0f * Mathf.Deg2Rad), Mathf.Sin(0f * Mathf.Deg2Rad))))
                    {
                        tiles[i, j].gravityDirection = TileProperties.GravDir.downRight;
                    }
                    else
                    {
                        tiles[i, j].gravityDirection = TileProperties.GravDir.right;
                    }
                }
                else
                {
                    tiles[i, j].gravityStrength = 0;
                    tiles[i, j].gravityDirection = TileProperties.GravDir.none;
                }
            }
        }
    }

    // Update is called once per frame
    void Update() {
        
        // if chosen player ends their turn, set the next player in line to have a turn and repeat
        if (players[selectedPlayer].GetHasEndedTurn())
        {
            players[selectedPlayer].updateMomentum();//doesn't actually move the player
            players[selectedPlayer].resetTurnVars();// previous turn ends

            
            Debug.Log("Player " + selectedPlayer + "'s turn has ended.");
            if (selectedPlayer + 1 == playersToCreate)
            {
                selectedPlayer = 0;
            }
            else
            {
                ++selectedPlayer;
            }
            Debug.Log("It is now Player " + selectedPlayer + "'s turn!");
            players[selectedPlayer].updateGravAndMomentum();
            players[selectedPlayer].enableReticule();
            players[selectedPlayer].setCursorIsActive(true);

        }
    }

    public int getMapLen()
    {
        return MAP_LEN;
    }
    public int getMapHeight()
    {
        return MAP_HGHT;
    }
}
