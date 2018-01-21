using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class UIManager : MonoBehaviour {

    public Color inactive, selected, inaccessible;
    public GameManager gm;
    public Button thruster, teleporter, turnSkip;
    private Player currentPlayer;
	// Use this for initialization
	void Start () {
        //thruster.interactable = false;
        //teleporter.interactable = false;
        //turnSkip.interactable = false;
	}
    public void thrusterButtonOnClick() { gm.players[gm.getSelectedPlayer()].thrusterOverdriveSelected = true; gm.players[gm.getSelectedPlayer()].targetingReticule.setColor(Color.gray); gm.players[gm.getSelectedPlayer()].sr.enabled = true; }
    public void teleporterButtonOnClick() { gm.players[gm.getSelectedPlayer()].teleportSelected = true; gm.players[gm.getSelectedPlayer()].targetingReticule.setColor(Color.gray); gm.players[gm.getSelectedPlayer()].sr.enabled = true; }
    public void skipTurnButtonOnClick() { gm.players[gm.getSelectedPlayer()].abstainSelected = true; gm.players[gm.getSelectedPlayer()].showFuturePath(gm.players[gm.getSelectedPlayer()].numTurnsToPredictMovement, gm.players[gm.getSelectedPlayer()].resultantIndicator, false, true); }
    // Update is called once per frame
    void Update () {
        currentPlayer = gm.players[gm.getSelectedPlayer()];
        if (currentPlayer.thrusterOverdriveSelected)
        {
            thruster.GetComponent<Image>().color = selected;
            teleporter.GetComponent<Image>().color = (currentPlayer.fuel > 1) ? inactive : inaccessible;
        }
        else if(currentPlayer.teleportSelected)
        {
            thruster.GetComponent<Image>().color = inactive;
            teleporter.GetComponent<Image>().color = selected;
        }
        else
        {
            thruster.GetComponent<Image>().color = (currentPlayer.fuel > 0) ? inactive : inaccessible;
            teleporter.GetComponent<Image>().color = (currentPlayer.fuel > 1) ? inactive : inaccessible;
        }
	}
}
