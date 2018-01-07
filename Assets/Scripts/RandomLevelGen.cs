using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class RandomLevelGen: MonoBehaviour{
    System.Random RNG;
    public int len, height;
    public int minLen, maxLen, minHeight, maxHeight, maxStarRange, minStarRange, maxStarStrength, minStarStrength, playersToCreate;
    public float starDensity;
    public Vector4[] stars;
    public Vector2[] spawnLocations;

    public void Start()
    {
        RandomLevelGen test = new RandomLevelGen(5);
        test.minHeight = 10;
        test.maxHeight = 10;
        test.minLen = 10;
        test.maxLen = 10;
        test.maxStarRange = 5;
        test.minStarRange = 5;
        test.maxStarStrength = 10;
        test.minStarStrength = 10;
        test.playersToCreate = 1;
        test.starDensity = 1f / 25;
        test.genLevel();
        test.minStarStrength = 5;
    }
    public RandomLevelGen()
    {
        RNG = new System.Random();
    }
    public RandomLevelGen(int seed)
    {
        RNG = new System.Random(seed);
    }
    public void genLevel()
    {
        genLenHeight();
        genStars();
        genSpawnPoints();
    }
    public void genLenHeight()
    {
        height = minHeight + (int)Mathf.Floor((float)RNG.NextDouble() * (maxHeight - minHeight + 1));
        len = minLen + (int)Mathf.Floor((float)RNG.NextDouble() * (maxLen - minLen + 1));
    }
    public void genStars()
    {
        int numStars = (int)Mathf.Ceil(starDensity * (height * len));
        stars = new Vector4[numStars];
        for (int i = 0; i < numStars; ++i)
        {
            //range
            stars[i].x = minStarRange + (int)Mathf.Floor((float)RNG.NextDouble() * (maxStarRange - minStarRange + 1));
            //strength
            stars[i].y = minStarStrength + (int)Mathf.Floor((float)RNG.NextDouble() * (maxStarStrength - minStarStrength + 1));
            //x position
            int tempX = 0 + (int)Mathf.Floor((float)RNG.NextDouble() * (len - 0 + 1));
            if (tempX < stars[i].x)
            {
                stars[i].z = tempX + stars[i].x;
            }else if (tempX > len - stars[i].x)
            {
                stars[i].z = tempX - stars[i].x;
            }else
            {
                stars[i].z = tempX;
            }
            stars[i].z += (int)stars[i].z % 4;
            //y position
            int tempY = 0 + (int)Mathf.Floor((float)RNG.NextDouble() * (height - 0 + 1));
            if (tempY < stars[i].y)
            {
                stars[i].w = tempY + stars[i].y;
            }
            else if (tempY > height - stars[i].y)
            {
                stars[i].w = tempY - stars[i].y;
            }else
            {
                stars[i].w = tempY;
            }
            stars[i].w += (int)stars[i].w % 4;
        }
    }
    public void genSpawnPoints()
    {
        spawnLocations = new Vector2[playersToCreate];
        // keep this simple for now, randomly assign players a spawn point with 0 grav if there are enough 0 grav points in the map
        // if not randomly assign spawn positions

        int zeroGravLocationCount = 0;
        Vector2[] validSpawnLocations = new Vector2[height * len];
        for (int x = 0; x < len; ++x)
        {
            for (int y = 0; y < height; ++y)
            {
                bool valid = true;
                foreach(Vector4 star in stars)
                {
                    if (Vector2.Distance(new Vector2(x,y), new Vector2(star.y, star.z)) <= star.w)
                    {
                        valid = false;
                        break;
                    }
                }
                if (valid)
                {
                    validSpawnLocations[zeroGravLocationCount++] = new Vector2(x,y);
                }
            }
        }
        if (zeroGravLocationCount >= playersToCreate)
        {
            //randomly assign spawn points from validSpawnLocations, index from 0 to zeroGravLocationCount-1, without replacement
            for (int i = 0; i < playersToCreate; ++i)
            {
                int selection = 0 + (int)Mathf.Floor((float)RNG.NextDouble() * (zeroGravLocationCount - 0 + 1));
                spawnLocations[i] = validSpawnLocations[selection];
                validSpawnLocations[selection] = validSpawnLocations[zeroGravLocationCount-1];
                --zeroGravLocationCount;
            }
        }
        else
        {
            // randomly assign spawn points
            for (int i = 0; i < playersToCreate; ++i)
            {
                spawnLocations[i] = new Vector2((int)Mathf.Floor((float)RNG.NextDouble() * (len - 0 + 1)),(int)Mathf.Floor((float)RNG.NextDouble() * height - (0 + 1)));
            }
        }
        
    }
}
