using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActivatorLine : MonoBehaviour {

    public enum LineType {Fuel, Checkpoint, Stun}
    public LineType type;
    public int modifier;
    public float zPos;
    public Vector2 offset;
    public Vector2 lineStart;
    public Vector2 lineEnd;
    private float slope;
    public LineRenderer linRen;

    public void setLine(Vector2 first, Vector2 second)
    {
        lineStart = first;
        lineEnd = second;
        linRen.SetPositions(new Vector3[]{new Vector3(first.x, first.y, zPos), new Vector3(second.x, second.y, zPos)});
        //set slope to be what it is, unless it needs to be infinity, in which case max float value is a good enough approximation
        slope = (lineStart.y != lineEnd.y)?((lineStart.x - lineEnd.x) / (lineStart.y - lineEnd.y)):float.MaxValue;
    }

    public void activateMove(Player target)
    {
        for (int i = 0; i < 10; ++i)
        {
            if(intersects(target.cachedSmoothPath[i], target.cachedSmoothPath[i + 1])){
                enactChange(target);
                return;//only one change enacted per line per turn. Without this return, people could ride lines for at most 10 times normal amounts changed
            }
        }
    }

    public void enactChange(Player target)
    {
        if (type == LineType.Checkpoint)
        {
            ++target.numCheckpointsPassed;
            Debug.Log("     Player " + (target.playerNum - 1) + " crossed a checkpoint.");
        }
        else if (type == LineType.Fuel)
        {
            target.fuel += modifier;
            //if target fuel is below min or above max, set fuel to be within bounds again
            target.fuel = (target.fuel > 0) ? (target.fuel > target.FUEL_LIMIT) ? target.FUEL_LIMIT : target.fuel : 0;
            Debug.Log("     Added " + modifier + " fuel to Player " + (target.playerNum - 1) + "\n     making their total fuel: " + target.fuel);
        }
        else if (type == LineType.Stun)
        {
            //need one extra turn of stun only for players who are currently taking a turn
            target.numTurnsStunned = (target.GetHasEndedTurn()) ? modifier : modifier + 1;
            Debug.Log("     Stunned " + (target.playerNum - 1) + " for " + modifier + " (more) turns\nmaking them stunned for a total of " + target.numTurnsStunned + " turns from now.");
        }
    }

    //checks if given line segment between two parameter vector positions intersects the locally stored line info, defined in setLine call
    public bool intersects(Vector3 first, Vector3 second)
    {
        float paramSlope = (first.y != second.y) ? ((first.x - second.x) / (first.y - second.y)) : float.MaxValue;
        if (slope == paramSlope) {
            //first check they represent the same line by a0a1c0c1 calcs
            //ret false if they differ
            if ((lineStart.y - 1 / slope * lineStart.x) != (first.y - 1 / paramSlope * first.x))
            {
                Debug.Log("no intersection needed, line segments parallel but not same lines");
                return false;
            }else
            {
                //check endpoints?
                return (liesWithinLineSegment(lineStart, first, second)
                     || liesWithinLineSegment(lineEnd, first, second)
                     || liesWithinLineSegment(first, lineStart, lineEnd)
                     || liesWithinLineSegment(second, lineStart, lineEnd));
            }
            //then now knowing they are in the same line test endpoints to see if they lie within both segments ret liesWithinBothSegments if found false if not
            //check if one line has an endpoint within the other line
            //TODO
            //return liesWithinBothSegments(new Vector2(), first, second);
        }
        else if (slope == 0)//true infinity
        {
            if (first.y == second.y)
            {
                //lines are perpendicular,
                //slope = infinity
                //paramSlope = 0, != infinity
                //therefore intersection is at x of local and y of param
                return liesWithinBothSegments(new Vector2(lineStart.x, first.y), first, second);
            }else
            {
                //slope = infinity
                //param slope != 0, != infinity
                //therefore interesection is at x of local and y of where the other one has same x
                return liesWithinBothSegments(new Vector2(lineStart.x, lineStart.x/paramSlope + (first.y - 1 / paramSlope * first.x)), first, second);
            }
            //float a0 = -1 / slope, b0 = 1, c0 = (lineStart.y - 1 / slope * lineStart.x),
              //      a1 = -1 / paramSlope, b1 = 1, c1 = (first.y - 1 / paramSlope * first.x);
            //solve param line for x = lineStart.x
            //return liesWithinBothSegments(new Vector2(), first, second);
        }
        else if (/* "slope" == infinity */ lineStart.y == lineEnd.y)//true 0
        {
            if (paramSlope == 0)// slope of param is infinity
            {
                //slope is 0
                //param slope is infinity
                //intersection at y val of og and x val of param
                return liesWithinBothSegments(new Vector2(first.x, lineStart.y), first, second);
            }else
            {
                //slope is 0
                //param slope is neither 0 nor infinity
                return liesWithinBothSegments(new Vector2((((first.y - 1 / paramSlope * first.x) - lineStart.y)/ (-1 / paramSlope)), lineStart.y), first, second);
            }
            //float a0 = -1 / slope, b0 = 1, c0 = (lineStart.y - 1 / slope * lineStart.x),
              //      a1 = -1 / paramSlope, b1 = 1, c1 = (first.y - 1 / paramSlope * first.x);
            //solve param line for y = lineStart.y
            //return liesWithinBothSegments(new Vector2(), first, second);
        }
        else if (paramSlope == 0)
        {
            if (lineStart.y == lineEnd.y)
            {
                //lines are perpendicular,
                //slope = infinity
                //paramSlope = 0, != infinity
                //therefore intersection is at x of local and y of param
                return liesWithinBothSegments(new Vector2(first.x, lineStart.y), first, second);
            }
            else
            {
                //slope = infinity
                //param slope != 0, != infinity
                //therefore interesection is at x of local and y of where the other one has same x
                return liesWithinBothSegments(new Vector2(first.x, first.x / slope + (lineStart.y - 1 / slope * lineStart.x)), first, second);
            }
        }
        else if (/*param slope == infinity*/first.y == second.y)
        {
            if (slope == 0)// slope of param is infinity
            {
                //slope is 0
                //param slope is infinity
                //intersection at y val of og and x val of param
                return liesWithinBothSegments(new Vector2(lineStart.x, first.y), first, second);
            }
            else
            {
                //slope is 0
                //param slope is neither 0 nor infinity
                return liesWithinBothSegments(new Vector2((((lineStart.y - 1 / slope * lineStart.x) - first.y) / (-1 / slope)), first.y), first, second);
            }
        }
        else
        {
            return liesWithinBothSegments(defaultIntersection(first, second), first, second);
        }
    }

    public bool liesWithinBothSegments(Vector2 proposedIntersection, Vector2 first, Vector2 second)
    {
        return liesWithinLineSegment(proposedIntersection, lineStart, lineEnd) && liesWithinLineSegment(proposedIntersection, first, second);
    }

    public bool liesWithinLineSegment(Vector2 point, Vector2 first, Vector2 second)
    {
        //assumes point passed in already adheres to the line function defined by the two line points passed in
        //for the line eq defined by first and second, f(x) = -ax + c
        // tests point.y == f(point.x)
        float paramSlope = (first.y != second.y) ? ((first.x - second.x) / (first.y - second.y)) : float.MaxValue;
        if (paramSlope == 0)
        {
            if (!Mathf.Approximately(point.x,first.x))
            {
                return false;// not even along the line...
            }
            //given line has slope 1/paramSlope which is infinity now, so just check that the y value lies within the valid segment range
            return ((first.y <= point.y && point.y <= second.y) || (first.y >= point.y && point.y >= second.y));
        }
        if (!Mathf.Approximately(point.y, point.x / paramSlope + (first.y - 1 / paramSlope * first.x)))
        {
            return false;
        }
        //x and y bounds checking at this point. We know the point lies on the given line if it got to this point in code, so now just need to check segment
        return ((first.y <= point.y && point.y <= second.y) || (first.y >= point.y && point.y >= second.y))
            && ((first.x <= point.x && point.x <= second.x) || (first.x >= point.x && point.x >= second.x));
    }
    public Vector2 defaultIntersection(Vector2 first, Vector2 second)
    {
        float paramSlope = (first.y != second.y) ? ((first.x - second.x) / (first.y - second.y)) : float.MaxValue;
        //float   a0 = -1 / slope, b0 = 1, c0 = (lineStart.y - 1 / slope * lineStart.x), 
        //      a1 = -1 / paramSlope, b1 = 1, c1 = (first.y - 1 / paramSlope * first.x);
        //calculate intersection of the two lines
        Vector2 intersectionPoint = new Vector2(
            //x
            //(c0 / (a0 - a1) * (b1) + c1 / (a0 - a1) * (-b0))
            ((lineStart.y - 1 / slope * lineStart.x) / (-1 / slope + 1 / paramSlope) * (1) + (first.y - 1 / paramSlope * first.x) / (-1 / slope + 1 / paramSlope) * (-1))
            ,//y
            //(c0 / (a0 - a1) * (-a1) + c1 / (a0 - a1) * (a0))
            ((lineStart.y - 1 / slope * lineStart.x) / (-1 / slope + 1 / paramSlope) * (1 / paramSlope) + (first.y - 1 / paramSlope * first.x) / (-1 / slope + 1 / paramSlope) * (-1 / slope))
            );
        //check if intersection point lies within each line segment
        return intersectionPoint;
    }
}
