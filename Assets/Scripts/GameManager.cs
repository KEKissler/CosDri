using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour {
    // prefabs
    public TileProperties tile;
    public StarProperties star;
    public Player player;
    public GameObject spaceBG;
    public GameObject starBG;
    public GameObject draw;

    // empty objects in scene view for neat nesting thing. Helps in unity editor.
    public GameObject tileParent, starParent, playerParent, BackgroundParent, spaceParallaxParent, starParallaxParent, checkpointParent;

    // tile vars
    [Header("Map Definitions")]
    public int MAP_LEN; public int MAP_HGHT; // direcly influences tiles to instantiate
    [Tooltip("Checkpoints are the win condition of the game.\n\nAt least one is required, and those not explicitly active will be made active over the course of the game.")]
    public Checkpoint[] checkpoints;
    public TileProperties[,] tiles;// local tile data

    // star vars
    [Tooltip("x=range\ny=strength\nz=nxPos\nw=nyPos")]
    public Vector4[] starData = new Vector4[4];// local star data
    private StarProperties[] stars;// dont use this for anything, specific for grav calculations

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

    //checkpoint vars
    public Color inactive, cleared; // shared colors
    public Color[] specialColors;// holds three colors per checkpoint. For activating, active, and active and a border.
    public int numCheckpointsRequiredToWin;// the number of checkpoints any player must clear in order to win
    private int winningPlayer = -1;// holds the index of the player that has won the game, -1 if no one has yet
    
    

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

        // checkpoint shading
        int chkptIndex = 0;
        foreach(Checkpoint chkpt in checkpoints)
        {
            int pointsIndex = 0;
            SpriteRenderer[] tempSRList = new SpriteRenderer[chkpt.points.Length];
            if (!chkpt.isActive)
            {
                foreach (Vector2 pos in chkpt.points)
                {
                    GameObject temp = Instantiate(draw, new Vector3((pos.x + (1 - MAP_LEN) / 2f), (pos.y + (1 - MAP_HGHT) / 2f), -4.5f), Quaternion.identity, checkpointParent.transform);
                    tempSRList[pointsIndex] = temp.GetComponent<SpriteRenderer>();
                    tempSRList[pointsIndex].color = inactive;
                    ++pointsIndex;
                }
            }else{
                foreach (Vector2 pos in chkpt.points)
                {
                    // shared colors: inactive, cleared
                    //custom colors: activating, active, active w/ border (in that order)
                    //this is the start function, therefore only inactive, active, and active w/ border are possible
                    GameObject temp = Instantiate(draw, new Vector3((pos.x + (1-MAP_LEN)/2f), (pos.y + (1 - MAP_HGHT) / 2f), -4.5f), Quaternion.identity, checkpointParent.transform);
                    tempSRList[pointsIndex] = temp.GetComponent<SpriteRenderer>();
                    tempSRList[pointsIndex].color = specialColors[3*chkptIndex+2];// border color
                     // if there is a position bordering this tile that does not also belong to the checkpoint, change its color to be a border
                    int borderCount = 0;
                    foreach (Vector2 altPos in chkpt.points)
                    {
                        if ((pos-altPos).magnitude < 1.5f)
                        {
                            ++borderCount;
                            if (borderCount == 8)
                            {
                                tempSRList[pointsIndex].color = specialColors[3*chkptIndex + 1];// active color w/o border
                                ++pointsIndex;
                                break;
                            }
                        }
                    }
                }
            }
            chkpt.usefulList = tempSRList;
            chkpt.hasPassed = new bool[playersToCreate];
            chkpt.progress = new int[playersToCreate];
            ++chkptIndex;
        }
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
            updateCheckpoints(selectedPlayer, false); // this function can make isGameOver() change its value
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
    public void updateCheckpoints(int playerIndex, bool isInFirst)
    {
        Checkpoint[] toUpdate = getCheckpointsAt(players[playerIndex].transform.position);
        foreach (Checkpoint ch in toUpdate)
        {
            updateCheckpoint(ch, playerIndex, isInFirst);
        }

    }

    public void updateCheckpoint(Checkpoint ch, int playerIndex, bool isInFirst)
    {
        //updates the count of the progress for this checkpoint for this player
        //if the updated count == min needed count to clear checkpoint then change this checkpoint to look cleared and check if the player is in first
        //if this player is one of the players who are in first, and incremeting their clear count wouldnt win them the game, select a new checkpoint to make active
        // and then make other people who are in first, not in first
        // otherwise if they are not in first check to see if they should be in first now
        //updates progress and checks if done with this checkpoint
        if(++ch.progress[playerIndex] >= ch.turnsRequiredToRemain)
        {
            ch.hasPassed[playerIndex] = true;
            if(++players[playerIndex].numCheckpointsPassed >= numCheckpointsRequiredToWin)
            {
                winningPlayer = playerIndex;
                //TODO PLEASE CHANGE THIS SCENE TO ui or something
                Debug.Log("-----Player " + playerIndex + " wins!!!!!-----");
                Application.Quit();
            }
            else
            {
                if (players[playerIndex].isInFirst)
                {
                    Debug.Log("-----Player " + playerIndex + " is now in first!-----");
                    //make every other player not be in first anymore
                    for (int i = 0; i < playersToCreate; ++i)
                    {
                        players[i].isInFirst = false;
                    }
                    players[playerIndex].isInFirst = true;

                    bool shouldMakeAnotherCheckpointActive = true;
                    List<Checkpoint> viable = new List<Checkpoint>();
                    foreach (Checkpoint _ch in checkpoints)
                    {
                        if (!_ch.hasPassed[playerIndex] && ch.isActive)
                        {
                            shouldMakeAnotherCheckpointActive = false;
                            viable.Add(_ch);
                        }
                    }
                    if (shouldMakeAnotherCheckpointActive)
                    {
                        chooseCheckpointToMakeActive(viable.ToArray(), playerIndex);
                    }

                }
                else
                {
                    //check if tied with other people in first, and if so make this player be in first too
                    int maxCheckPointsPassed = 0;
                    for (int i = 0; i < playersToCreate; ++i)
                    {
                        if (players[i].numCheckpointsPassed > maxCheckPointsPassed)
                        {
                            maxCheckPointsPassed = players[i].numCheckpointsPassed;
                        }
                    }
                    if (players[playerIndex].numCheckpointsPassed == maxCheckPointsPassed)
                    {
                        players[playerIndex].isInFirst = true;
                    }
                }
            }
        }
    }

    void chooseCheckpointToMakeActive(Checkpoint[] options, int firstPlacePlayerIndex)
    {
        makeActive(options[(int)(Random.value * options.Length)], 0);
    }

    void makeActive(Checkpoint ch, int inNumTurns)
    {
        int checkpointIndex = -1;
        for (int i = 0; i < checkpoints.Length; ++i)
        {
            if (ch == checkpoints[i])
            {
                checkpointIndex = i;
                break;
            }
        }

        foreach (SpriteRenderer tempSR in ch.usefulList)
        {
            int neighborCount = 0;
            foreach (SpriteRenderer moreTempSR in ch.usefulList)
            {
                if ((tempSR.transform.position - moreTempSR.transform.position).magnitude < 1.5f)
                {
                    ++neighborCount;
                }
            }
            if (neighborCount == 8)
            {
                tempSR.color = specialColors[checkpointIndex+1];
            }
            else
            {
                tempSR.color = specialColors[checkpointIndex+2];
            }
        }
    }

    Checkpoint[] getCheckpointsAt(Vector2 pos)
    {
        List<Checkpoint> growingList = new List<Checkpoint>();
        foreach (Checkpoint ch in checkpoints)
        {
            foreach(Vector2 v in ch.points)
            {
                if (new Vector2(v.x + (1 - MAP_LEN) / 2f, v.y + (1 - MAP_HGHT) / 2f) == pos)
                {
                    growingList.Add(ch);
                    break;
                }
            }
        }
        return growingList.ToArray();
    }
    public bool isGameOver()
    {
        return winningPlayer > 0;
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
