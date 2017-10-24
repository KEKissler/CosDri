using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour {
    static int playerCount = 0;// a running total of all players in the game
    public int fuel = 0;// fuel meter for this character
    public int FUEL_LIMIT;// plz dont change this value, max fuel allowed
    public int MAP_LEN, MAP_HGHT;// just for initialization purposes, this allows this player to adjust their own offset correctly
    private bool cursorIsActive = false;// for allowing or disallowing this player's  ability to select tiles. Set by the gameManager to start a player's turn
    private bool thrusterSelected = false, thrusterOverdriveSelected = false, teleportSelected = false, abstainSelected = false; // which movement skill has this players selected
    private bool miningLaserSelected = false, missileSelected = false, piercingMissileSelected = false;// which of the combat abilities has this player selected
    public int playerNum;// assigned a value in Start()
    public float zPos;// determines render layer of the players
    Vector2 gravity = new Vector2(), momentum = new Vector2(), movement = new Vector2(), currentPosition = new Vector3(), previousPosition = new Vector2(), teleportPosition = new Vector2();// positional things that make sense
    // a note on teleport position, it is the position from which you teleported, not to where you teleported.
    public Camera cam;
    public GameManager gameManager;
    public GameObject targetingReticulePrefab;
    public Sprite ship;
    private SpriteRenderer sr;
    private LineRenderer lineRen;
    public Color reticuleColor, gravIndicator, momentumIndicator, movementIndicator, resultantIndicator;
    public int numTurnsToPredictMovement;

    private TargetingReticuleController targetingReticule;
    private SpriteRenderer targetingReticuleSR;
    

    private bool hasEndedTurn = false;
    // Use this for initialization
    void Start () {
        // player count
        ++playerCount;
        playerNum = playerCount;

        //Sprite Renderer and Line Renderer management
        sr = GetComponent<SpriteRenderer>();
        lineRen = GetComponent<LineRenderer>();
        sr.sprite = ship;

        // managing own targeting reticule
        GameObject temp = Instantiate(targetingReticulePrefab);
        targetingReticuleSR = temp.GetComponent<SpriteRenderer>();
        targetingReticule = temp.GetComponent<TargetingReticuleController>();
        targetingReticule.GetComponent<SpriteRenderer>().sortingOrder = 10;
        targetingReticule.cam = cam;
        targetingReticule.GetComponent<SpriteRenderer>().color = reticuleColor;

        // positional initialization
        //currentPosition = new Vector3(transform.position.x, transform.position.y, zPos);
        //previousPosition = currentPosition;

    }
	
	// Update is called once per frame
	void Update () {
        // render player

        // if its the player's turn still and more input is needed to advance
        if (cursorIsActive && !hasEndedTurn && fuel > 0 && noMovementActionYetCompleted() && noCombatActionYetCompleted())
        {
            checkForInput();
        }
        // otherwise, verify that this player even was supposed to be taking a turn, and then immidiately end it
        else if (cursorIsActive)
        {
            hasEndedTurn = true;
        }
	}

    bool noMovementActionYetCompleted()
    {
        if (thrusterSelected && thrusterSelect())// if player wants to use the thruster and if the thruster successfully activates
        {
            return false;//exit the function and keep checking for input
        }
        else if (thrusterOverdriveSelected && thrusterOverdriveSelect())// if player wants to use overdriv and if overdrive successfully activates
        {
            return false;
        }
        else if (teleportSelected && teleportSelect()) // if want tp and tp succeeds
        {
            return false;
        }
        else if (abstainSelected)// if the player wants to end the turn
        {
            return false;
        }
        return true;
    }

    bool noCombatActionYetCompleted()
    {
        if (miningLaserSelected && miningLaserSelect())// if player wants to use the miningLaser and if the Laser successfully activates
        {
            targetingReticule.setColor(reticuleColor); // reset the targeting reticule's color to the player's default
            return false;//exit the function 
        }
        else if (missileSelected && missileSelect())
        {
            targetingReticule.setColor(reticuleColor);
            return false;
        }
        else if (piercingMissileSelected && piercingMissileSelect())
        {
            targetingReticule.setColor(reticuleColor);
            return false;
        }
        // a note about abstain, this is not the skip turn option. This option merely skips the movement action part of ones turn and goes straight to combat.
        // If this is selected there should exist a go back option for the combat action, allowing the player to essentially fully reset their turn.
        else if (abstainSelected)
        {
            targetingReticule.setColor(reticuleColor);
            return false;
        }
        return true;
    }

    void checkForInput()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            thrusterSelected = true;
            targetingReticule.setColor(Color.gray);
        }
        else if (Input.GetKeyDown(KeyCode.W))
        {
            thrusterOverdriveSelected = true;
            targetingReticule.setColor(Color.gray);
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            teleportSelected = true;
            targetingReticule.setColor(Color.gray);
        }
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            hasEndedTurn = true;
        }
    }

    Vector2 resolveToTile(float mouseX, float mouseY)
    {
        Vector3 cursorWorldPosition = cam.ScreenToWorldPoint(new Vector3(mouseX, mouseY, cam.transform.position.z));
        return new Vector2((Mathf.Floor(cursorWorldPosition.x) + 1 / 2f), (Mathf.Floor(cursorWorldPosition.y) + 1 / 2f));
    }
    // for this and all other actions, the bool returned represents whether or not the action has been executed yet or not. true if finished, false if not.
    bool thrusterSelect()
    {
        // if left click, resolve cursor position to nearest tile, and then check if that tile is valid, for the fuel and position of this player.
        //if allowed, set the movement vector to the appropriate vector and then call updatePosition() to finalize movement this turn
        // if not, dont do anything and maybe display some sort of error message or sound?

        showFuturePath(numTurnsToPredictMovement, resultantIndicator, false);

        if (Input.GetKeyDown(KeyCode.Mouse1))// cancel selection
        {
            thrusterSelected = false;
            return false;
        }

        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            Vector2 temp = resolveToTile(Input.mousePosition.x, Input.mousePosition.y);
            Debug.Log("trying to execute thruster at selected tile: " + temp + " whose index values are: tiles[" + lenToIndex(temp.x) + "][" + heightToIndex(temp.y) + "]");
            // if a tile on the map is selected
            if (0 <= lenToIndex(temp.x) && lenToIndex(temp.x) <= MAP_LEN-1 && 0 <= heightToIndex(temp.y) && heightToIndex(temp.y) <= MAP_HGHT - 1)
            {
                if (/*within 1 tile of the player position in x and y*/ (Mathf.Abs(lenToIndex(temp.x) - lenToIndex(currentPosition.x)) <= 1f) && (Mathf.Abs(lenToIndex(temp.y) - lenToIndex(currentPosition.y)) <= 1f))
                {
                    movement = temp - currentPosition;
                    thrusterSelected = false;
                    return true;// ran thing, action ended.
                }
            }
        }
        return false;
    }
    
    bool thrusterOverdriveSelect()
    {
        showFuturePath(numTurnsToPredictMovement, resultantIndicator, false);

        if (Input.GetKeyDown(KeyCode.Mouse1))// cancel selection
        {
            thrusterOverdriveSelected = false;
            return false;
        }

        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            Vector2 temp = resolveToTile(Input.mousePosition.x, Input.mousePosition.y);
            Debug.Log("trying to execute thruster overdrive at selected tile: " + temp + " whose index values are: tiles[" + lenToIndex(temp.x) + "][" + heightToIndex(temp.y) + "]");
            // if a tile on the map is selected
            if (0 <= lenToIndex(temp.x) && lenToIndex(temp.x) <= MAP_LEN - 1 && 0 <= heightToIndex(temp.y) && heightToIndex(temp.y) <= MAP_HGHT - 1)
            {
                if (/*within 1 tile of the player position in x and y*/ (Mathf.Abs(lenToIndex(temp.x) - lenToIndex(currentPosition.x)) <= 2f) && (Mathf.Abs(lenToIndex(temp.y) - lenToIndex(currentPosition.y)) <= 2f))
                {
                    movement = temp - currentPosition;
                    thrusterOverdriveSelected = false;
                    return true;// ran thing, action ended.
                }
            }
        }
        return false;
    }

    bool teleportSelect()
    {
        showFuturePath(numTurnsToPredictMovement, resultantIndicator, true);

        if (Input.GetKeyDown(KeyCode.Mouse1))// cancel selection
        {
            thrusterOverdriveSelected = false;
            return false;
        }

        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            Vector2 temp = resolveToTile(Input.mousePosition.x, Input.mousePosition.y);
            Debug.Log("trying to execute teleporter at selected tile: " + temp + " whose index values are: tiles[" + lenToIndex(temp.x) + "][" + heightToIndex(temp.y) + "]");
            // if a tile on the map is selected
            if (0 <= lenToIndex(temp.x) && lenToIndex(temp.x) <= MAP_LEN - 1 && 0 <= heightToIndex(temp.y) && heightToIndex(temp.y) <= MAP_HGHT - 1)
            {
                if (/*within 1 tile of the player position in x and y*/ (Mathf.Abs(lenToIndex(temp.x) - lenToIndex(currentPosition.x)) <= 5f) && (Mathf.Abs(lenToIndex(temp.y) - lenToIndex(currentPosition.y)) <= 5f))
                {
                    movement = temp - currentPosition;
                    thrusterOverdriveSelected = false;
                    return true;// ran thing, action ended.
                }
            }
        }
        return false;
    }

    bool miningLaserSelect()
    {
        return false;
    }

    bool missileSelect()
    {
        return false;
    }

    bool piercingMissileSelect()
    {
        return false;
    }

    public void updateGravAndMomentum()
    {
        // movement happens in two steps, one at the start of the player's turn, and another function at the end.
        // the reason this is split is because players should have gravity and momentum apply before the player is asked to make a movement decision
        //this is the first of those two functions

        // first things first create a local vector equal to the gravity vector at this current player's position on the board
        Debug.Log("trying to access the gravity at [" + lenToIndex(currentPosition.x) + "][" + heightToIndex(currentPosition.y) + "]");
        if (0 <= lenToIndex(currentPosition.x) && lenToIndex(currentPosition.x) <= MAP_LEN - 1 && 0 <= heightToIndex(currentPosition.y) && heightToIndex(currentPosition.y) <= MAP_HGHT - 1 && gameManager.tiles[lenToIndex(currentPosition.x), heightToIndex(currentPosition.y)].gravityDirection != TileProperties.GravDir.none)
        {
            Debug.Log("It has direction " + gameManager.tiles[lenToIndex(currentPosition.x), heightToIndex(currentPosition.y)].gravityDirection + " and strength " + gameManager.tiles[lenToIndex(currentPosition.x), heightToIndex(currentPosition.y)].gravityStrength);
            if (gameManager.tiles[lenToIndex(currentPosition.x), heightToIndex(currentPosition.y)].gravityDirection == TileProperties.GravDir.right)
            {
                gravity = gameManager.tiles[lenToIndex(currentPosition.x), heightToIndex(currentPosition.y)].gravityStrength * new Vector2(1, 0).normalized;
            }
            else if (gameManager.tiles[lenToIndex(currentPosition.x), heightToIndex(currentPosition.y)].gravityDirection == TileProperties.GravDir.upRight)
            {
                gravity = gameManager.tiles[lenToIndex(currentPosition.x), heightToIndex(currentPosition.y)].gravityStrength * new Vector2(1, 1).normalized * Mathf.Sqrt(2);
            }
            else if (gameManager.tiles[lenToIndex(currentPosition.x), heightToIndex(currentPosition.y)].gravityDirection == TileProperties.GravDir.up)
            {
                gravity = gameManager.tiles[lenToIndex(currentPosition.x), heightToIndex(currentPosition.y)].gravityStrength * new Vector2(0, 1).normalized;
            }
            else if (gameManager.tiles[lenToIndex(currentPosition.x), heightToIndex(currentPosition.y)].gravityDirection == TileProperties.GravDir.upLeft)
            {
                gravity = gameManager.tiles[lenToIndex(currentPosition.x), heightToIndex(currentPosition.y)].gravityStrength * new Vector2(-1, 1).normalized * Mathf.Sqrt(2);
            }
            else if (gameManager.tiles[lenToIndex(currentPosition.x), heightToIndex(currentPosition.y)].gravityDirection == TileProperties.GravDir.left)
            {
                gravity = gameManager.tiles[lenToIndex(currentPosition.x), heightToIndex(currentPosition.y)].gravityStrength * new Vector2(-1, 0).normalized;
            }
            else if (gameManager.tiles[lenToIndex(currentPosition.x), heightToIndex(currentPosition.y)].gravityDirection == TileProperties.GravDir.downLeft)
            {
                gravity = gameManager.tiles[lenToIndex(currentPosition.x), heightToIndex(currentPosition.y)].gravityStrength * new Vector2(-1, -1).normalized * Mathf.Sqrt(2);
            }
            else if (gameManager.tiles[lenToIndex(currentPosition.x), heightToIndex(currentPosition.y)].gravityDirection == TileProperties.GravDir.down)
            {
                gravity = gameManager.tiles[lenToIndex(currentPosition.x), heightToIndex(currentPosition.y)].gravityStrength * new Vector2(0, -1).normalized;
            }
            else
            {
                gravity = gameManager.tiles[lenToIndex(currentPosition.x), heightToIndex(currentPosition.y)].gravityStrength * new Vector2(1, -1).normalized * Mathf.Sqrt(2);
            }
        }else
        {
            //invalid gravity position or empty valid position
            gravity = new Vector2();
        }
        Debug.Log("Gravity is now " + gravity);

        previousPosition = currentPosition;// previous position really means position at the start of a turn, before anything moves you; ie now.
        currentPosition += gravity;// as determined above
        currentPosition += momentum;// as determined by other movement function
        transform.position = new Vector3(currentPosition.x, currentPosition.y, zPos);


    }

    public void updateMovementandMomentum()
    {
        //this function calculates momentum but doesnt apply it
        // applying grav and momentum happen at a different time in the turn order, so its in another function for that
        
        //first things first move the player, so momentum can correctly update for the next turn.
        //currentPosition += movement;

        //after applying momentum, update it (updating moment varies on whether or not you teleported, because teleporting shouldnt give you momentum, just positional changes should)
        if (teleportSelected)
        {
            // a teleport when activated, will set teleport position to be current position, then it will change current position to wherever was selected
            momentum = teleportPosition - previousPosition;
            Debug.Log("Applying momemtum in special case: Teleport");
        }
        else
        {
            // if no teleport, positional change over the course of this turn is set to momentum, to be applied next turn
            momentum = currentPosition + movement - previousPosition;
        }
        Debug.Log("Movement choice is now " + movement);
        Debug.Log("Momentum is now " + momentum);// + "; currPos - prevPos =  " + currentPosition + " - " + previousPosition);


        
        movement -= movement;//resetting movement vector for next turn
        transform.position = new Vector3(currentPosition.x, currentPosition.y, zPos);
        Debug.Log("Moving player to " + new Vector2(lenToIndex(currentPosition.x), heightToIndex(currentPosition.y)));
    }

    public void showFuturePath(int numTurns, Color pathColor, bool usingTeleporter)//numTurns is number of turns to project out, path color starts path that color, using teleporter just tells us if we need to calc momentum the first time or not
    {
        bool firstTurn = true;// here to just do something special for the first calc
        Vector3[] points = new Vector3[numTurns + 1]; // +1 for first turn drawing from current position
        // nothing yet calculated, need to calc both grav and momentum from here, before turn executes
        points[0] = transform.position;
        Vector2 tempPosition = currentPosition, tempPreviousPosition = previousPosition;
        Vector2 tempGrav = new Vector2(), tempMomentum = new Vector2();

        for (int i = 0; i < numTurns; ++i)
        {
            //calc gravity this turn
            // if checking a valid point in the grav grid
            if (0 <= lenToIndex(tempPosition.x) && lenToIndex(tempPosition.x) <= MAP_LEN - 1 && 0 <= heightToIndex(tempPosition.y) && heightToIndex(tempPosition.y) <= MAP_HGHT - 1)
            {
                bool angled = (int)(gameManager.tiles[lenToIndex(tempPosition.x), heightToIndex(tempPosition.y)].gravityDirection) % 2 == 1;
                float angle = (int)(gameManager.tiles[lenToIndex(tempPosition.x), heightToIndex(tempPosition.y)].gravityDirection) * 45f;
                int strength = gameManager.tiles[lenToIndex(tempPosition.x), heightToIndex(tempPosition.y)].gravityStrength;
                if (angled)
                {
                    tempGrav = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)).normalized * strength * Mathf.Sqrt(2);
                }
                else
                {
                    tempGrav = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)).normalized * strength;
                }

            }
            else
            {
                // there exists no gravity off the defined map
                tempGrav = new Vector2();
            }

            //calc momentum this turn

            if (firstTurn)
            {
                // skips one cycle of recalculating momentum
                if (usingTeleporter)
                {
                    usingTeleporter = false;
                    //do nothing, momentum is same as the prev turn's actual momentum
                    tempMomentum = momentum;
                    firstTurn = false;
                }
                else
                {
                    // use the current mouse position as the theoretical end point of this turn's movement
                    tempMomentum = resolveToTile(Input.mousePosition.x, Input.mousePosition.y) - tempPreviousPosition;
                    firstTurn = false;
                    
                }
            }else
            {
                // not first turn, so not predicting movement on future turns
                tempMomentum = tempPosition - tempPreviousPosition;
            }

            //add momemtum and grav to tempPosition and then add it to the master list
            tempPreviousPosition = tempPosition;
            tempPosition += (tempGrav + tempMomentum);
        
            points[i+1] = new Vector3(tempPosition.x, tempPosition.y, zPos);// +1 so that it doesnt overwrite the special first entry, current position
        }
        drawPath(points, pathColor, new Color(1, 1, 1, 0));// 1,1,1,0 is white with fully transparant alpha
    }

     public void drawPath(Vector3[] points, Color start, Color end)
    {
        lineRen.numPositions = points.Length;
        lineRen.SetPositions(points);
        lineRen.startColor = start;
        lineRen.endColor = end;
    }

    public void resetTurnVars()
    {
        cursorIsActive = false;
        hasEndedTurn = false;
        thrusterSelected = false;
        thrusterOverdriveSelected = false;
        teleportSelected = false;
        miningLaserSelected = false;
        missileSelected = false;
        piercingMissileSelected = false;
        targetingReticule.setColor(reticuleColor);
        targetingReticuleSR.enabled = false; 
    }

    public void enableReticule()
    {
        targetingReticuleSR.enabled = true;

    }

    public void setCursorIsActive(bool newVal)
    {
        cursorIsActive = newVal;
    }

    public void setCurrentPosition(Vector3 newPosition)
    {
        currentPosition = newPosition;
    }

    public bool GetHasEndedTurn()
    {
        return hasEndedTurn;
    }

    //for conversions and sanity ;-;
    private int lenToIndex(float Xpos)
    {
        return (Mathf.RoundToInt(Xpos + (MAP_LEN - 1) / 2f));
    }

    private int heightToIndex(float Ypos)
    {
        return (Mathf.RoundToInt(Ypos + (MAP_HGHT- 1) / 2f));
    }
}
