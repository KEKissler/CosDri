using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Checkpoint : System.Object {
    [Tooltip("Determines if this checkpoint is checking players positions or not.")]
    public bool isActive;
    [Tooltip("The number of turns a player is required to remain in this checkpoint in order to clear it.")]
    public int turnsRequiredToRemain;
    [Tooltip("The tile locations that make up this checkpoint.")]
    public Vector2[] points;
    [Tooltip("Please do not edit this field. Any information left here will be ignored.")]
    public bool[] hasPassed;
    [Tooltip("Please do not edit this field. Any information left here will be ignored.")]
    public int[] progress;

    public SpriteRenderer[] usefulList;

}
