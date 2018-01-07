using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
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
    public int numTurnsStunned = 0;// if set to a value greater than 1, removes turn and decrements this number until it is 0
    public float zPos;// determines render layer of the players
    Vector2 gravity = new Vector2(), momentum = new Vector2(), movement = new Vector2(), currentPosition = new Vector3(), previousPosition = new Vector2(), teleportPosition = new Vector2();// positional things that make sense
    // a note on teleport position, it is the position from which you teleported, not to where you teleported.
    public Sprite thrusterRange, teleporterRange;
    public Camera cam;
    public GameManager gameManager;
    public GameObject targetingReticulePrefab;
    public Sprite ship;
    public GameObject offsetShipImage;
    private LineRenderer lineRen, altLineRen;
    public Color reticuleColor, gravIndicator, momentumIndicator, movementIndicator, resultantIndicator;
    public int numTurnsToPredictMovement;
    public float smoothness;

    private TargetingReticuleController targetingReticule;
    private SpriteRenderer targetingReticuleSR;
    private SpriteRenderer sr;

    private Vector3[] cachedSmoothPath = new Vector3[11];
    private float timeWaited = 0.0f;//seconds, reset to 0 every turn
    private float turnAnimationTime = 0.5f;//seconds, never is changed
    private GameObject playerVisuals;
    private bool hasEndedTurn = false, hasDecidedButNotEndedTurn = false;
    public bool isInFirst;// for checkpoint calculations and potentially end of game calculations
    public int numCheckpointsPassed = 0; // also for checkpoint calculations and end of game calcss 
    // Use this for initialization
    void Start() {
        // player count
        ++playerCount;
        playerNum = playerCount;

        //Sprite Renderer and Line Renderer management
        sr = GetComponent<SpriteRenderer>();
        lineRen = GetComponent<LineRenderer>();
        //creating a slightly offset ship image location, because messing with player position for visuals' sake seems like a bad idea
        playerVisuals = Instantiate(offsetShipImage,new Vector3(transform.position.x + 0.0f, transform.position.y -0.0f, zPos), Quaternion.identity, this.transform);
        playerVisuals.GetComponent<SpriteRenderer>().sprite = ship;
        altLineRen = playerVisuals.GetComponent<LineRenderer>();// can only have one line renderer per gameObject
        
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
        //genCubicBez(20, new Vector3(0, 0, -10), new Vector3(5, 5, -10), new Vector3(5, 5, -10), new Vector3(10, 0, -10));

    }

    // Update is called once per frame
    void Update()
    {
        if (cursorIsActive)
        {
            // render player
            //showFuturePath(numTurnsToPredictMovement, resultantIndicator, false, true);
            // if its the player's turn still and more input is needed to advance
            if (!hasDecidedButNotEndedTurn && fuel > 0 && (noMovementActionYetCompleted() || (false && noCombatActionYetCompleted())) && numTurnsStunned == 0)
            {
                checkForInput();
                timeWaited = 0.0f;
            }
            // otherwise move the player until they are at their final position for this turn, and then end the players turn
            else{
                //if this is true, the player was not eligible to move this turn due to being stunned or perhaps being out of fuel
                if (numTurnsStunned != 0)
                {
                    //abstainSelected = true;
                    //Debug.Log("currentPosition: " + currentPosition + "  previousPosition: " + previousPosition + "  momentum: " + momentum + "  teleportPosition: " + teleportPosition + "\nnumTurnsStunned!=0");
                    //showFuturePath(numTurnsToPredictMovement, resultantIndicator, false, true);
                }
                hasDecidedButNotEndedTurn = true;
                if (timeWaited < turnAnimationTime)
                {
                    timeWaited += Time.deltaTime;
                    if (timeWaited < turnAnimationTime)
                    {
                        //Debug.DrawRay(transform.position, cachedSmoothPath[(int)(Mathf.Ceil(timeWaited * cachedSmoothPath.Length / turnAnimationTime))], Color.red);
                        //Debug.Log(timeWaited / turnAnimationTime + "% of the way done.");
                        //Debug.Log("Trying to access cachedSmoothPath[" + ((int)(Mathf.Ceil(timeWaited * cachedSmoothPath.Length / turnAnimationTime)) - 1) + "]\nWhich has a Length value of " + cachedSmoothPath.Length);
                        //transform.rotation = Quaternion.LookRotation(cachedSmoothPath[(int)(Mathf.Ceil(timeWaited * cachedSmoothPath.Length / turnAnimationTime)) - 1] - transform.position);
                        //Debug.Log("n a n i ? ? ? \n" + Mathf.Asin((cachedSmoothPath[(int)(Mathf.Ceil(timeWaited * cachedSmoothPath.Length / turnAnimationTime)) - 1].y - transform.position.y) / (cachedSmoothPath[(int)(Mathf.Ceil(timeWaited * cachedSmoothPath.Length / turnAnimationTime)) - 1].x - transform.position.x)));
                        Vector3 nextPos = cachedSmoothPath[(int)(Mathf.Ceil(timeWaited * cachedSmoothPath.Length / turnAnimationTime)) - 1];
                        //Debug.Log(nextPos.ToString("F5") + "and " + transform.position.ToString("F5"));
                        if (nextPos != transform.position && !Mathf.Approximately(Mathf.Abs(nextPos.x-transform.position.x),0f))
                        {
                            //Debug.Log((nextPos.y - transform.position.y) / (nextPos.x - transform.position.x));
                            //Debug.Log(Mathf.Rad2Deg * Mathf.Atan(Mathf.Tan(Mathf.Deg2Rad * -300f)));
                            //Debug.Log(Mathf.Rad2Deg * (Mathf.Atan2((nextPos.y - transform.position.y), (nextPos.x - transform.position.x)) + 1 / 2f * Mathf.PI));
                            playerVisuals.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Rad2Deg * (Mathf.Atan2((nextPos.y - transform.position.y), (nextPos.x - transform.position.x)) - 1/2f * Mathf.PI));
                        }
                        int floor = (int)Mathf.Floor(timeWaited * cachedSmoothPath.Length / turnAnimationTime), ceil = (int)Mathf.Ceil(timeWaited * cachedSmoothPath.Length / turnAnimationTime);
                        float actual = timeWaited * cachedSmoothPath.Length / turnAnimationTime;
                        transform.position = Mathf.Abs((actual - ceil) / (floor - ceil)) * cachedSmoothPath[floor] + Mathf.Abs((actual - floor) / (floor - ceil)) * cachedSmoothPath[ceil - 1];
                       // Debug.Log(previousPosition + " vs " + cachedSmoothPath[0] + " and " + currentPosition + " vs " + cachedSmoothPath[cachedSmoothPath.Length - 1]);
                        return;
                    }
                }
                hasEndedTurn = true;
            }
        }
    }

    bool noMovementActionYetCompleted()
    {
        if (thrusterSelected && thrusterSelect())// if player wants to use the thruster and if the thruster successfully activates
        {
            return false;//exit the function and keep checking for input
        }
        else if (thrusterOverdriveSelected && thrusterOverdriveSelect())// if player wants to use overdrive and if overdrive successfully activates
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
           // Debug.Log("currentPosition: " + currentPosition + "  previousPosition: " + previousPosition + "  momentum: " + momentum + "  teleportPosition: " + teleportPosition);
            showFuturePath(numTurnsToPredictMovement, resultantIndicator, false, true);
            abstainSelected = true;
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

        showFuturePath(numTurnsToPredictMovement, resultantIndicator, false, false);

        if (Input.GetKeyDown(KeyCode.Mouse1))// cancel selection
        {
            thrusterSelected = false;
            return false;
        }

        if (/*Input.GetKeyDown(KeyCode.Mouse0)*/Input.GetKeyDown(KeyCode.W))
        {
            Vector2 temp = resolveToTile(Input.mousePosition.x, Input.mousePosition.y);
            Debug.Log("trying to execute thruster at selected tile: " + temp + " whose index values are: tiles[" + lenToIndex(temp.x) + "][" + heightToIndex(temp.y) + "]");
            // if a tile on the map is selected
            if (0 <= lenToIndex(temp.x) && lenToIndex(temp.x) <= MAP_LEN - 1 && 0 <= heightToIndex(temp.y) && heightToIndex(temp.y) <= MAP_HGHT - 1)
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
        if (Mathf.Abs(resolveToTile(Input.mousePosition.x, Input.mousePosition.y).x - currentPosition.x) <= 2.1f && Mathf.Abs(resolveToTile(Input.mousePosition.x, Input.mousePosition.y).y - currentPosition.y) <= 2.1f)
        {
            sr.enabled = false;
            lineRen.enabled = true;
            altLineRen.enabled = true;
            showFuturePath(numTurnsToPredictMovement, resultantIndicator, false, false);
        }
        else
        {
            lineRen.enabled = false;
            altLineRen.enabled = false;
            sr.sprite = thrusterRange;
            //sr.transform.rotation = new Quaternion();
            sr.enabled = true;
        }
        
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
                if (/*within 2 tiles of the player position in x and y*/ (Mathf.Abs(lenToIndex(temp.x) - lenToIndex(currentPosition.x)) <= 2f) && (Mathf.Abs(lenToIndex(temp.y) - lenToIndex(currentPosition.y)) <= 2f))
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
        if (Mathf.Abs(resolveToTile(Input.mousePosition.x, Input.mousePosition.y).x - currentPosition.x) <= 5.1f && Mathf.Abs(resolveToTile(Input.mousePosition.x, Input.mousePosition.y).y - currentPosition.y) <= 5.1f)
        {
            sr.enabled = false;
            lineRen.enabled = true;
            altLineRen.enabled = true;
            showFuturePath(numTurnsToPredictMovement, resultantIndicator, true, false);
        }
        else
        {
            lineRen.enabled = false;
            altLineRen.enabled = false;
            sr.sprite = teleporterRange;
            //sr.transform.rotation = new Quaternion();
            sr.enabled = true;
        }
        //showFuturePath(numTurnsToPredictMovement, resultantIndicator, true);

        if (Input.GetKeyDown(KeyCode.Mouse1))// cancel selection
        {
            teleportSelected = false;
            return false;
        }

        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            Vector2 temp = resolveToTile(Input.mousePosition.x, Input.mousePosition.y);
            Debug.Log("trying to execute teleporter at selected tile: " + temp + " whose index values are: tiles[" + lenToIndex(temp.x) + "][" + heightToIndex(temp.y) + "]");
            // if a tile on the map is selected
            if (0 <= lenToIndex(temp.x) && lenToIndex(temp.x) <= MAP_LEN - 1 && 0 <= heightToIndex(temp.y) && heightToIndex(temp.y) <= MAP_HGHT - 1)
            {
                if (/*within 5 tile of the player position in x and y*/ (Mathf.Abs(lenToIndex(temp.x) - lenToIndex(currentPosition.x)) <= 5f) && (Mathf.Abs(lenToIndex(temp.y) - lenToIndex(currentPosition.y)) <= 5f))
                {
                    //movement = temp - currentPosition;
                    movement = new Vector2();
                    teleportPosition = currentPosition;
                    currentPosition = temp;
                    numTurnsStunned = 2;//numTurnsStunned is decremented after the end of every turn if it was not 0 at the end of a turn, as such you need to actually set turn stun on self to be one greater
                    //otherwise the turn it is applied it would be set back to 0 and just do nothing
                    //hasEndedTurn = true;// the teleport action stuns you therefore your turn should end now(?)
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
        } else
        {
            //invalid gravity position or empty valid position
            gravity = new Vector2();
        }
        Debug.Log("Gravity is now " + gravity);

        previousPosition = currentPosition;// previous position really means position at the start of a turn, before anything moves you; ie now.
        currentPosition += gravity;// as determined above
        currentPosition += momentum;// as determined by other movement function
        //snapCamera to this
        //animate motion
        //free up camera
        transform.position = posRealignedToGrid(new Vector3(currentPosition.x, currentPosition.y, zPos));


    }

    public void updateMomentum()
    {
        //this function calculates momentum but doesnt apply it
        // applying grav and momentum happen at a different time in the turn order, so its in another function for that


        //after applying momentum, update it (updating moment varies on whether or not you teleported, because teleporting shouldnt give you momentum, just positional changes should)
        if (teleportSelected)
        {
            // a teleport when activated, will set teleport position to be current position, then it will change current position to wherever was selected
            momentum = teleportPosition - previousPosition;
            Debug.Log("Applying momemtum in special case: Teleport");
            teleportSelected = false;
        }
        else
        {
            // if no teleport, positional change over the course of this turn is set to momentum, to be applied next turn
            momentum = currentPosition + movement - previousPosition;
        }
        Debug.Log("Movement choice is now " + movement);
        Debug.Log("Momentum is now " + momentum + "\nwhich is equal to currentPosition: " + currentPosition + " + movement: " + movement + " - previousPosition: " + previousPosition);// + "; currPos - prevPos =  " + currentPosition + " - " + previousPosition);



        movement -= movement;//resetting movement vector for next turn
        //transform.position = posRealignedToGrid(new Vector3(currentPosition.x, currentPosition.y, zPos));
        Debug.Log("Moving player to " + new Vector2(lenToIndex(currentPosition.x), heightToIndex(currentPosition.y)));
    }

    public void showFuturePath(int numTurns, Color pathColor, bool usingTeleporter, bool doingNothing)//numTurns is number of turns to project out, path color starts path that color, using teleporter just tells us if we need to calc momentum the first time or not
    {
        bool firstTurn = true;// here to just do something special for the first calc
        if (usingTeleporter && !doingNothing)
        {
            numTurns--;
        }
        Vector3[] points = new Vector3[numTurns + 1 + 1]; // +1 for first turn drawing from current position, + 1 for extra prediction at end to assist in path smoothing
        // nothing yet calculated, need to calc both grav and momentum from here, before turn executes
        
       
        points[0] = transform.position;
        if (usingTeleporter && !doingNothing)
            points[0] = new Vector3(resolveToTile(Input.mousePosition.x, Input.mousePosition.y).x, resolveToTile(Input.mousePosition.x, Input.mousePosition.y).y, zPos);

        Vector2 tempPosition = currentPosition, tempPreviousPosition = previousPosition;
        Vector2 tempGrav = new Vector2(), tempMomentum = new Vector2();
        if (usingTeleporter && !doingNothing)
            tempPosition = resolveToTile(Input.mousePosition.x, Input.mousePosition.y);

        for (int i = 0; i < numTurns + 1; ++i) // (the +1 in the test is to generate one extra, not needed point, so that the smoothing calculations can have info from which to average)
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
                    //I once thought that we do nothing at this point. I was wrong. This works, but I'd need to spend a long time to know why.
                    tempMomentum = currentPosition + movement - previousPosition;
                    //tempPosition = ;
                    firstTurn = false;
                }
                else if (doingNothing)
                {
                    tempMomentum = tempPosition - tempPreviousPosition;
                    firstTurn = false;
                }
                else
                {
                    // use the current mouse position as the theoretical end point of this turn's movement
                    tempMomentum = resolveToTile(Input.mousePosition.x, Input.mousePosition.y) - tempPreviousPosition;
                    firstTurn = false;

                }
            } else
            {
                // not first turn, so not predicting movement on future turns
                tempMomentum = tempPosition - tempPreviousPosition;
            }

            //add momemtum and grav to tempPosition and then add it to the master list
            tempPreviousPosition = tempPosition;
            tempPosition += (tempGrav + tempMomentum);
            points[i + 1] = new Vector3(tempPosition.x, tempPosition.y, zPos);// +1 so that it doesnt overwrite the special first entry, current position
        }
        //if you want sprites every step the player will visit, place them at every non first entry in points right here, or cache path at this point
        points = smoothPath(points, 10);
        drawPath(points, pathColor);// actually managing the important main curvy path
        //now seting up the precise jagged helper path
        if (!doingNothing)
        {
            if (!usingTeleporter)
            {

                //temp vars for one turn out calculation for helper line
                bool isInMap = 0 <= lenToIndex(currentPosition.x) && lenToIndex(currentPosition.x) <= MAP_LEN - 1 && 0 <= heightToIndex(currentPosition.y) && heightToIndex(currentPosition.y) <= MAP_HGHT - 1;
                Vector2 tempMousePosition = resolveToTile(Input.mousePosition.x, Input.mousePosition.y);
                Vector2 tempGravity;
                if (isInMap)
                {
                    TileProperties temp = gameManager.tiles[lenToIndex(currentPosition.x), heightToIndex(currentPosition.y)];
                    tempGravity = temp.gravityStrength * new Vector2(Mathf.Cos((int)temp.gravityDirection * 45f * Mathf.Deg2Rad), Mathf.Sin((int)temp.gravityDirection * 45f * Mathf.Deg2Rad));
                    tempGravity = (int)temp.gravityDirection % 2 == 1 ? Mathf.Sqrt(2) * tempGravity : tempGravity;
                }
                else
                {
                    tempGravity = new Vector2();// there is no gravity off the map
                }
                Vector2 tempMomentmn = currentPosition + movement - previousPosition;
                //using above variables, calculates the points for the second line renderer to draw, specifically drawing grav from player to grav + momentum from player to grav + momentum + potential movement from player.
                //Guaranteed to line up with final true path, therefore useful information for player
                Vector3[] tempPositions = {new Vector3(currentPosition.x, currentPosition.y, zPos),
            new Vector3(currentPosition.x + tempGravity.x, currentPosition.y + tempGravity.y, zPos),
            new Vector3(currentPosition.x + tempGravity.x + tempMomentmn.x, currentPosition.y + tempGravity.y + tempMomentmn.y, zPos),
            new Vector3(currentPosition.x + tempGravity.x + tempMomentmn.x + (tempMousePosition.x - currentPosition.x), currentPosition.y + tempGravity.y + tempMomentmn.y + (tempMousePosition.y - currentPosition.y), zPos)};
                altLineRen.positionCount = tempPositions.Length;
                altLineRen.SetPositions(tempPositions);
                altLineRen.material.color = momentumIndicator;

            }
            else
            {
                //temp vars for one turn out calculation for helper line
                Vector2 tempMousePosition = resolveToTile(Input.mousePosition.x, Input.mousePosition.y);
                bool isInMap = 0 <= lenToIndex(tempMousePosition.x) && lenToIndex(tempMousePosition.x) <= MAP_LEN - 1 && 0 <= heightToIndex(tempMousePosition.y) && heightToIndex(tempMousePosition.y) <= MAP_HGHT - 1;
                Vector2 tempGravity;
                if (isInMap)
                {
                    TileProperties temp = gameManager.tiles[lenToIndex(tempMousePosition.x), heightToIndex(tempMousePosition.y)];
                    tempGravity = temp.gravityStrength * new Vector2(Mathf.Cos((int)temp.gravityDirection * 45f * Mathf.Deg2Rad), Mathf.Sin((int)temp.gravityDirection * 45f * Mathf.Deg2Rad));
                    tempGravity = (int)temp.gravityDirection % 2 == 1 ? Mathf.Sqrt(2) * tempGravity : tempGravity;
                }
                else
                {
                    tempGravity = new Vector2(); // again, no gravity off the map
                }
                Vector2 tempMomentmn = currentPosition + movement - previousPosition;
                //using above variables, calculates the points for the second line renderer to draw, specifically drawing grav from player to grav + momentum from player to grav + momentum + potential movement from player.
                //Guaranteed to line up with final true path, therefore useful information for player
                Vector3[] tempPositions = {new Vector3(tempMousePosition.x, tempMousePosition.y, zPos),
            new Vector3(tempMousePosition.x + tempGravity.x, tempMousePosition.y + tempGravity.y, zPos),
            new Vector3(tempMousePosition.x + tempGravity.x + tempMomentmn.x, tempMousePosition.y + tempGravity.y + tempMomentmn.y, zPos)};
                altLineRen.positionCount = tempPositions.Length;
                altLineRen.SetPositions(tempPositions);
                altLineRen.material.color = gravIndicator;
            }
        }
        

    }

    public void drawPath(Vector3[] points, Color newColor)
    {
        lineRen.positionCount = points.Length;
        lineRen.SetPositions(points);
        lineRen.material.color = newColor;
    }

    public void resetTurnVars()
    {
        cursorIsActive = false;
        hasEndedTurn = false;
        hasDecidedButNotEndedTurn = false;
        thrusterSelected = false;
        thrusterOverdriveSelected = false;
        abstainSelected = false;
        teleportSelected = false;
        miningLaserSelected = false;
        missileSelected = false;
        piercingMissileSelected = false;
        targetingReticule.setColor(reticuleColor);
        targetingReticuleSR.enabled = false;
        if (numTurnsStunned != 0)
            --numTurnsStunned;
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
        return (Mathf.RoundToInt(Ypos + (MAP_HGHT - 1) / 2f));
    }

    //for generating smooth looking curves, given exact points and a number of steps
    private Vector3[] genCubicBez(int jumpsToMake/* >= 0*/, Vector3 A, Vector3 B, Vector3 C, Vector3 D)
    {
        Vector3[] toReturn = new Vector3[jumpsToMake + 1];
        //t1 points
        Vector3 ab, bc, cd;
        //t2 points
        Vector3 abbc, bccd;
        //t3 point
        Vector3 abbcbccd;
        for (int i = 0; i <= jumpsToMake; ++i)
        {
            //t1 updates
            ab = (1.0f * i / jumpsToMake) * B + (1.0f * (jumpsToMake - i) / jumpsToMake) * A;
            bc = (1.0f * i / jumpsToMake) * C + (1.0f * (jumpsToMake - i) / jumpsToMake) * B;
            cd = (1.0f * i / jumpsToMake) * D + (1.0f * (jumpsToMake - i) / jumpsToMake) * C;
            //t2 updates
            abbc = (1.0f * i / jumpsToMake) * bc + (1.0f * (jumpsToMake - i) / jumpsToMake) * ab;
            bccd = (1.0f * i / jumpsToMake) * cd + (1.0f * (jumpsToMake - i) / jumpsToMake) * bc;
            //t3 update
            abbcbccd = (1.0f * i / jumpsToMake) * bccd + (1.0f * (jumpsToMake - i) / jumpsToMake) * abbc;
            //Debug.Log(abbcbccd);
            toReturn[i] = abbcbccd;
        }
        //lineRen.positionCount = toReturn.Length;
        //lineRen.SetPositions(toReturn);
        return toReturn;
    }
    //specificaly takes a list of vectors and makes a smooth path out of them through using averaging of vectors and the bezier curve function above
    private Vector3[] smoothPath(Vector3[] originalPath, int jumps)
    {
        Vector3[] finalPath = new Vector3[(originalPath.Length - 2) * jumps + 1];// final output
        Vector3[] temp = new Vector3[originalPath.Length * 3 - 3];// helper structure. It gets big for good reason, all helper bezier curve directing things are inserted into this list
        //first entry in originalPath is current position, last entry is the extra position. No values from originalPath can not be present at equal interval in the final path, while this function is still correct
        if (originalPath.Length < 3)
        {
            // if there are not enough entries to create curves from, its a line or an invalid thing we were passed, so just return what was passed.
            return originalPath;
        } else
        {
            //setting the first value of temp to the previousPosition value
            temp[0] = new Vector3(previousPosition.x, previousPosition.y, zPos);
            //setting each true path value to its correct spaced out point in the temp array
            for (int i = 0; i < originalPath.Length - 1; ++i) {
                temp[3 * i + 1] = originalPath[i];
            }
            //setting the last value of temp to the last value of originalPath
            temp[temp.Length - 1] = originalPath[originalPath.Length - 1];

            //manually setting the first forward facing bezier helper point
            temp[2] = temp[1] + smoothness * (temp[1] - temp[0] + temp[4] - temp[1]);
            //setting all inner forwards facing bezier helpers
            for (int i = 5; i < temp.Length - 3; i += 3)
            {
                temp[i] = temp[i - 1] + smoothness * (temp[i - 1] - temp[i - 4] + temp[i + 2] - temp[i - 1]);
            }
            //setting all inner backwards facing bezier helpers
            for (int i = 3; i < temp.Length - 3; i += 3)
            {
                temp[i] = temp[i + 1] - smoothness * (temp[i + 1] - temp[i - 2] + temp[i + 4] - temp[i + 1]);
            }
            //manually setting last backwards facing bezier helper
            temp[temp.Length - 3] = temp[temp.Length - 2] - smoothness * (temp[temp.Length - 1] - temp[temp.Length - 2] + temp[temp.Length - 2] - temp[temp.Length - 5]);

            for (int i = 1; i < temp.Length - 2; i += 3)
            {
                //checking for overlapping sections, and removing jank bezier helper confusion
                Vector3 changeTo = resolveIntersections(temp[i], temp[i + 1], temp[i + 2], temp[i + 3]);
                //Debug.Log("Testing the following: " + temp[i] + " " + temp[i + 1] + " " + temp[i + 2] + " " + temp[i + 3] + "\nand returning: " + changeTo);
                if (changeTo.z == zPos)
                {
                    temp[i + 1] = changeTo;
                    temp[i + 2] = changeTo;
                }//else do nothing
                 //Debug.Log(" ");
                 //Debug.Log(findSmallestTriArea(temp[i], temp[i + 1], temp[i + 2], temp[i + 3]));
            //    Debug.Log("step num " + (i+2)/3);
             //   findSmallestTriArea(temp[i], temp[i + 1], temp[i + 2], temp[i + 3]);
            /*
                //more checking, this time removing slim triangles that create hiccups in continuous path
                if (temp[i+1] != temp[i+2] && findSmallestTriArea(temp[i], temp[i + 1], temp[i + 2], temp[i + 3]) > 70f)
                {
                    //Debug.Log(findSmallestTriArea(temp[i], temp[i + 1], temp[i + 2], temp[i + 3]));
                    temp[i + 1] = temp[i];
                    temp[i + 2] = temp[i + 3];
                }*/
            }
            /*string tempstr = "";
            for (int i = 0; i < temp.Length; ++i)
            {
                tempstr += temp[i].ToString() + "  ";
            }
            Debug.Log(tempstr);*/

            //creating bezier curves...
            finalPath[0] = originalPath[0];
            for (int i = 1, block = 0; i < temp.Length - 2; i += 3, block += jumps)
            {
                Vector3[] veryTemp = genCubicBez(jumps, temp[i], temp[i + 1], temp[i + 2], temp[i + 3]);
                for (int j = 1; j < veryTemp.Length; ++j)
                {
                    finalPath[block + j] = veryTemp[j];
                }
            }
        }
        //saving one turn out path for animation purposes
        cachedSmoothPath = new Vector3[jumps+1];
        for (int i = 0; i < jumps+1; ++i)
        {
            cachedSmoothPath[i] = finalPath[i];
        }
        //returning final smooth path
        return finalPath;
    }

    private Vector3 resolveIntersections(Vector3 st, Vector3 h1, Vector3 h2, Vector3 end)
    {
        //return 1 / 2f * (h1 + h2);
        float x1 = (h1 - st).x, y1 = (h1 - st).y, x2 = (end - h2).x, y2 = (end - h2).y;
        //Debug.Log(y1 / x1 + "  " + y2 / x2);
        if (!Mathf.Approximately(y1, 0f) && !Mathf.Approximately(y2, 0f))
        {
            float yInt1 = st.y - (y1 / x1) * st.x;
            float yInt2 = end.y - (y2 / x2) * end.x;
            float xCollisionPoint = (yInt2 - yInt1) / ((y1 / x1) - (y2 / x2));
            if (((st.x <= xCollisionPoint && xCollisionPoint <= h1.x) || (st.x >= xCollisionPoint && xCollisionPoint >= h1.x)) && ((h2.x <= xCollisionPoint && xCollisionPoint <= end.x) || (h2.x >= xCollisionPoint && xCollisionPoint >= end.x)))
            {
                //Debug.Log("!!!!Intersection!!!!");
                //Debug.Log(((y1 / x1) * xCollisionPoint + yInt1) + " vs " + ((y2 / x2) * xCollisionPoint + yInt2));
                return new Vector3(xCollisionPoint, (y1 / x1) * xCollisionPoint + yInt1, zPos);
            }
            //Debug.Log("Segments do not intersect");
        }
        else
        {
            return new Vector3(0, 0, zPos + 20f);// something specifically not possible here
        }
        return new Vector3(0, 0, zPos + 20f);// something specifically not possible here
    }
    private Vector3 posRealignedToGrid(Vector3 givenPos)
    {
        //return givenPos;
        return new Vector3(Mathf.Floor(givenPos.x) + 0.5f, Mathf.Floor(givenPos.y) + 0.5f, zPos);
    }
}
