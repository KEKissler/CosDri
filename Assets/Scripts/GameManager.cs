using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

//TODO 1/7/2018 Add Start functionality to create activator lines from stuff already declared, Add Update functionality to check lists of lines (theoretical movement) against those activatorLines and to activate and do stuff if true intersection
public class GameManager : MonoBehaviour {
    // prefabs
    public TileProperties tile;
    public StarProperties star;
    public Player player;
    public GameObject checkpoint;
    public GameObject spaceBG;
    public GameObject starBG;
    public GameObject draw;
    public GameObject marble;

    // empty objects in scene view for neat nesting thing. Helps in unity editor.
    public GameObject tileParent, starParent, playerParent, BackgroundParent, spaceParallaxParent, starParallaxParent, checkpointParent, marbleParent;

    // tile vars
    [Header("Map Definitions")]
    public int MAP_LEN; public int MAP_HGHT; // direcly influences tiles to instantiate
    //ew, checkpoints, ew. Phasing those out as fast as possible thanks
    //[Tooltip("Checkpoints are the win condition of the game.\n\nAt least one is required, and those not explicitly active will be made active over the course of the game.")]
    //public Checkpoint[] checkpoints;
    
    public TileProperties[,] tiles;// local tile data

    // star vars
    [Tooltip("x=range\ny=strength\nz=xPos\nw=yPos")]
    public Vector4[] starData = new Vector4[4];// local star data
    private StarProperties[] stars;// dont use this for anything, specific for grav calculations

    //ActivatorLines vars
    public Vector4[] ActivatorLinesToCreate;//data to create lines from (line from w,x to y,z)
    private ActivatorLine[] ActivatorLines;// line list
    //public bool[] activeLines;// starts active if true, otherwise inactive
    public ActivatorLine.LineType[] lineTypes;// identifies the behavior of the ActivatorLine
    public int[] modifiers;// indicates, sometimes irrelevantly, the amount by which to enact an activated change (num turns to stun, amount of fuel to change, ...)
    //public Color inactive, active, cleared; // shared colors
    public Color CheckpointColor, FuelColor, StunColor; // shared colors
    public int numCheckpointsRequiredToWin;// the number of checkpoints any player must clear in order to win
    private static int winningPlayer = -1;// holds the index of the player that has won the game, -1 if no one has yet

    // player vars
    public int playersToCreate;// num characters in this game. 1-4 are valid entries
    public int fuelSpawnAmount, maxFuelLimit; // default fuel setting specifications to pass along to player instances
    public Player[] players = new Player[4]; // a list to hold them locally
    public Vector2[] spawnLocations = new Vector2[4]; // a list of where to create the people
    public Color[] playerColors = new Color[4];// targeting reticule color
    public Color momentumIndicatorColor, gravityIndicatorColor, movementIndicatorColor, resultantIndicatorColor;// colors for helpful lines
    public Sprite[] playerSprites = new Sprite[4];// for ships, I guess. Will add more art vars here later as needed.
    public Camera mainCamera; // for giving to players so that they can all align their targeting reticules to the grid properly
    public GameObject targetingReticulePrefab; // for giving to each player
    private int selectedPlayer; // one player may have a turn at a time. selectedPlayer tracks that player.
    public float zPosition;// determines height to place player objects

    //background visual game vars
    public Sprite space;// scalable background image for space
    public Vector2 spaceScale; //each component has an inverse relationship with number of times the space sprite is scaled in respective directions
    public Sprite starfield;// scalable background image for space
    public Vector2 starScale;// each component has an inverse relationship with number of times the star sprite is scaled in respective directions
    public Sprite[] parallaxLayers = new Sprite[3];// for all parallaxing layers
    private GameObject spaceBGInstance;// will store an instance of the given prefab
    private GameObject starBGInstance;// also storing an instance of the given prefab

    
    
    

    // Use this for initialization
    void Start () {
        /*
        RandomLevelGen kappa = new RandomLevelGen(500);
        kappa.minHeight = 50;
        kappa.maxHeight = 200;
        kappa.minLen = 50;
        kappa.maxLen = 200;
        kappa.maxStarRange = 20;
        kappa.minStarRange = 12;
        kappa.maxStarStrength = 20;
        kappa.minStarStrength = 8;
        kappa.playersToCreate = 1;
        kappa.starDensity = 1f / 1200;
        kappa.genLevel();
        MAP_LEN = kappa.len;
        MAP_HGHT = kappa.height;
        starData = kappa.stars;
        */
        //spawnLocations = kappa.spawnLocations;
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
            stars[k] = Instantiate(star, new Vector3(starData[k].z - MAP_LEN / 2f, starData[k].w - MAP_HGHT / 2f, 0f), Quaternion.identity, starParent.transform);
            Instantiate(marble, new Vector3(starData[k].z - MAP_LEN / 2f, starData[k].w - MAP_HGHT / 2f, -4f), Quaternion.identity, marbleParent.transform).transform.localScale = starData[k].x/4f * new Vector3(1f, 1f, 1f);
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
        mainCamera.transform.position = new Vector3(players[selectedPlayer].transform.position.x, players[selectedPlayer].transform.position.y, mainCamera.transform.position.z);// snap camera to player at start
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

        //setting up background canvas
        //space background
        spaceBGInstance = Instantiate(spaceBG, BackgroundParent.transform);
        SpriteRenderer spaceBGSR = spaceBGInstance.GetComponent<SpriteRenderer>();
        spaceBGSR.drawMode = SpriteDrawMode.Tiled;
        spaceBGSR.tileMode = SpriteTileMode.Continuous;
        //NOTE (scale.x)(size.width) = MAP_LEN and same for y vals
        spaceBGInstance.transform.localScale = new Vector3(spaceScale.x, spaceScale.y, 1f);
        if (!Mathf.Approximately(spaceScale.x, 0f) && !Mathf.Approximately(spaceScale.y, 0f))
        {
            spaceBGSR.size = new Vector2(MAP_LEN / spaceScale.x, MAP_HGHT / spaceScale.y);
        }else{
            Debug.Log("WARNING: space background scale provided too small, defaulting to sizes of 1.");
            spaceBGSR.size = new Vector2(1f, 1f);
        }

        //star background
        starBGInstance = Instantiate(starBG, BackgroundParent.transform);
        SpriteRenderer starBGSR = starBGInstance.GetComponent<SpriteRenderer>();
        starBGSR.drawMode = SpriteDrawMode.Tiled;
        starBGSR.tileMode = SpriteTileMode.Continuous;
        //NOTE (scale.x)(size.width) = MAP_LEN and same for y vals
        starBGInstance.transform.localScale = new Vector3(starScale.x, starScale.y, 1f);
        if (!Mathf.Approximately(spaceScale.x, 0f) && !Mathf.Approximately(spaceScale.y, 0f))
        {
            starBGSR.size = new Vector2(MAP_LEN / starScale.x, MAP_HGHT / starScale.y);
        }else{
            Debug.Log("WARNING: star background scale provided too small, defaulting to sizes of 1.");
            starBGSR.size = new Vector2(1f, 1f);
        }

        //Activator Lines
        ActivatorLines = new ActivatorLine[ActivatorLinesToCreate.Length];
        for(int i = 0; i < ActivatorLinesToCreate.Length; ++i)
        {
            Vector4 v = ActivatorLinesToCreate[i];
            GameObject lineInstance = Instantiate(checkpoint, new Vector3((v.w + v.y) / 2 - MAP_LEN / 2, (v.x + v.z) / 2 - MAP_HGHT / 2, zPosition - 1), Quaternion.identity, checkpointParent.transform);
            ActivatorLine ALRef = lineInstance.GetComponent<ActivatorLine>();
            ALRef.linRen = lineInstance.GetComponent<LineRenderer>();
            ALRef.setLine(new Vector2(v.w - MAP_LEN/2, v.x - MAP_HGHT/2), new Vector2(v.y - MAP_LEN/2, v.z - MAP_HGHT/2));
            ALRef.type = lineTypes[i];
            if ((int)(ALRef.type) == 0)
            {
                ALRef.linRen.startColor = FuelColor;
                ALRef.linRen.endColor = FuelColor;
            }
            else if ((int)(ALRef.type) == 1)
            {
                ALRef.linRen.startColor = CheckpointColor;
                ALRef.linRen.endColor = CheckpointColor;
            }
            else if ((int)(ALRef.type) == 2)
            {
                ALRef.linRen.startColor = StunColor;
                ALRef.linRen.endColor = StunColor;
            }
            ALRef.modifier = modifiers[i];
            ALRef.numCheckpointsRequiredToWin = numCheckpointsRequiredToWin;
            ActivatorLines[i] = ALRef;
        }

        //Camera
        CameraController tempCam = mainCamera.GetComponent<CameraController>();
        tempCam.rightMapLimit = MAP_LEN / 2f;
        tempCam.leftMapLimit = MAP_LEN / -2f;
        tempCam.upMapLimit = MAP_HGHT / 2f;
        tempCam.downMapLimit = MAP_HGHT / -2f;

        tileParent.SetActive(false);
    }

    // Update is called once per frame
    void Update() {
        
        // if chosen player ends their turn, set the next player in line to have a turn and repeat
        if (players[selectedPlayer].GetHasEndedTurn())
        {
            Debug.Log(players[selectedPlayer].fuel + "\n");
            //updateCheckpoints(selectedPlayer, false); // this function can make isGameOver() change its value
            activateActivatorLines(players[selectedPlayer]);
            players[selectedPlayer].updateMomentum();//doesn't actually move the player
            players[selectedPlayer].resetTurnVars();// previous turn ends

            //incrementing player number
            Debug.Log("Player " + selectedPlayer + "'s turn has ended.");
            if (selectedPlayer + 1 == playersToCreate)
            {
                selectedPlayer = 0;
            }
            else
            {
                ++selectedPlayer;
            }
            //turn starts
            Debug.Log("It is now Player " + selectedPlayer + "'s turn!");
            players[selectedPlayer].updateGravAndMomentum();
            players[selectedPlayer].enableReticule();
            players[selectedPlayer].setCursorIsActive(true);

        }
    }

    public void activateActivatorLines(Player target)
    {
        foreach (ActivatorLine al in ActivatorLines)
        {
            al.activateMove(target);
        }
    }

    public bool isGameOver()
    {
        return winningPlayer > 0;
    }

    public static void setWinningPlayer(int playerNum)
    {
        winningPlayer = playerNum;
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
