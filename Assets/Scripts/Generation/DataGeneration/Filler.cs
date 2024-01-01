using System.Collections.Generic;
using UnityEngine;

public class Filler
{
    public readonly int fillerIndex;

    private Queue<Vector2> toExplore;
    private int yStart, yEnd;
    private int currX, currY;

    public Filler(int index, int yStart, int yEnd, int currX, int currY)
    {

        fillerIndex = index;
        this.yStart = yStart;
        this.yEnd = yEnd;
        this.currX = currX;
        this.currY = currY;

        toExplore = new Queue<Vector2>();
        toExplore.Enqueue(new Vector2(currX, currY));
    }
    public Vector2 GetPoint()
    {
        return new Vector2(currX, currY);
    }
    public bool Step(in bool[,] tileMap, int[,] fillMap, int[,] blockMap)
    {
        if (toExplore.Count == 0)
        {
            toExplore = null;
            return false;
        }

        int size = tileMap.GetLength(0);
        Vector2 floorPos = toExplore.Dequeue();
        currX = (int)floorPos.x;
        currY = (int)floorPos.y;

        blockMap[currX / 3, currY / 3] = fillerIndex;

        if (currX + 1 < size && tileMap[currX + 1, currY] && (fillMap[currX + 1, currY] != fillerIndex))
        {
            fillMap[currX + 1, currY] = fillerIndex;
            toExplore.Enqueue(new Vector2(currX + 1, currY));
        }

        if (currX - 1 >= 0 && tileMap[currX - 1, currY] && (fillMap[currX - 1, currY] != fillerIndex))
        {
            fillMap[currX - 1, currY] = fillerIndex;
            toExplore.Enqueue(new Vector2(currX - 1, currY));
        }

        if ((currY + 1) < yEnd && tileMap[currX, currY + 1] && (fillMap[currX, currY + 1] != fillerIndex))
        {
            fillMap[currX, currY + 1] = fillerIndex;
            toExplore.Enqueue(new Vector2(currX, currY + 1));
        }

        if ((currY - 1) >= yStart && tileMap[currX, currY - 1] && (fillMap[currX, currY - 1] != fillerIndex))
        {
            fillMap[currX, currY - 1] = fillerIndex;
            toExplore.Enqueue(new Vector2(currX, currY - 1));
        }

        return true;
    }
}
