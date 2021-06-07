using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class Connector
{
    private static int ManhattanDistance(Vector2 a, Vector2 b)
    {
        return (int)Mathf.Abs(a.x - b.x) + (int)Mathf.Abs(a.y - b.y);
    }
    private static Vector2 AStar(int startFillId, List<int> fillIds, int[,] blockMap, Vector2 start, Vector2 target)
    {
        if (fillIds[blockMap[(int)start.x, (int)start.y]] != startFillId)
        {
            //start block already contains a tile that belongs to a different fill
            return start;
        }
        Vector2 closestPoint = start;
        int minDistance = int.MaxValue;

        PriorityQueue<Node> openNodes = new PriorityQueue<Node>();
        openNodes.Enqueue(new Node(start, ManhattanDistance(start, target)));

        int currX, currY, currDist, size = blockMap.GetLength(0);
        bool[,] marks = new bool[size, size];
        Vector2 currPos;
        Node currNode;
        while (openNodes.Size() != 0)
        {
            currNode = openNodes.Dequeue();
            currDist = ManhattanDistance(currNode.pos, target);
            closestPoint = currDist < minDistance ? currNode.pos : closestPoint;
            minDistance = Mathf.Min(currDist, minDistance);

            currX = (int)currNode.pos.x;
            currY = (int)currNode.pos.y;

            if (currX + 1 < size && fillIds[blockMap[currX + 1, currY]] == startFillId && !marks[currX + 1, currY])
            {
                currPos.x = currX + 1;
                currPos.y = currY;
                currDist = ManhattanDistance(currPos, target);
                if (currDist < 2 * minDistance)
                {
                    marks[currX + 1, currY] = true;
                    openNodes.Enqueue(new Node(currPos, currDist));
                }
            }
            if (currX - 1 >= 0 && fillIds[blockMap[currX - 1, currY]] == startFillId && !marks[currX - 1, currY])
            {
                currPos.x = currX - 1;
                currPos.y = currY;
                currDist = ManhattanDistance(currPos, target);
                if (currDist < 2 * minDistance)
                {
                    marks[currX - 1, currY] = true;
                    openNodes.Enqueue(new Node(currPos, currDist));
                }
            }
            if (currY + 1 < size && fillIds[blockMap[currX, currY + 1]] == startFillId && !marks[currX, currY + 1])
            {
                currPos.x = currX;
                currPos.y = currY + 1;
                currDist = ManhattanDistance(currPos, target);
                if (currDist < 2 * minDistance)
                {
                    marks[currX, currY + 1] = true;
                    openNodes.Enqueue(new Node(currPos, currDist));
                }
            }
            if (currY - 1 >= 0 && fillIds[blockMap[currX, currY - 1]] == startFillId && !marks[currX, currY - 1])
            {
                currPos.x = currX;
                currPos.y = currY - 1;
                currDist = ManhattanDistance(currPos, target);
                if (currDist < 2 * minDistance)
                {
                    marks[currX, currY - 1] = true;
                    openNodes.Enqueue(new Node(currPos, currDist));
                }
            }
        }

        return closestPoint;
    }

    public static bool[,] ConnectAutomataPaths(List<TilesGenerator.Filler> fillers, List<int> fillIds, int[,] blockMap, int[,] fillMap, bool[,] chunkMap, int chunkSeed)
    {
        if (fillers.Count < 2)
            return chunkMap;
        TilesGenerator.Filler baseFiller = fillers[0];

        Vector2 baseBlockPos = new Vector2((int)baseFiller.GetPoint().x / 3, (int)baseFiller.GetPoint().y / 3);
        int baseFillerId = fillIds[baseFiller.fillerIndex];
        int[] fillIdsArr = fillIds.ToArray();

        Vector2 startPoint, newBasePoint, newStartPoint;
        List<int> toBeMerged;
        while (fillers.Count != 1)
        {
            startPoint = new Vector2((int)fillers[1].GetPoint().x / 3, (int)fillers[1].GetPoint().y / 3);
            newBasePoint = AStar(fillIds[baseFillerId], fillIds, blockMap, baseBlockPos, startPoint);
            newStartPoint = AStar(fillIds[fillers[1].fillerIndex], fillIds, blockMap, startPoint, newBasePoint);
            toBeMerged = RandomDirectedWalk(baseFiller.fillerIndex, fillers[1].fillerIndex, ref newBasePoint, ref newStartPoint, fillIdsArr, chunkMap, fillMap, chunkSeed);

            int count = toBeMerged.Count;
            for (int i = 0; i < count; i++)
            {
                int mergingId = toBeMerged[0];
                fillIds.ConvertAll((id) => id = (id == mergingId ? baseFillerId : id));
                fillers.Remove(fillers.Find((filler) => fillIds[filler.fillerIndex] == mergingId && filler != baseFiller));
                toBeMerged.RemoveAt(0);
            }
        }

        return chunkMap;
    }

    //Vector2s are references just to reduce call memory :p don't get confused
    private static List<int> RandomDirectedWalk(int baseFillerIndex, int startFillerIndex, ref Vector2 basePos, ref Vector2 targetPos, int[] fillIdsArr, bool[,] chunkMap, int[,] fillMap, int chunkSeed)
    {
        int chunkSize = chunkMap.GetLength(0);
        List<int> toBeMerged = new List<int>();
        toBeMerged.Add(fillIdsArr[startFillerIndex]);

        Vector2 baseTile = basePos * 3;
        bool baseTileFound = false;
        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                if (fillIdsArr[fillMap[(int)baseTile.x + x, (int)baseTile.y + y]] == fillIdsArr[baseFillerIndex])
                {
                    baseTile.x += x;
                    baseTile.y += y;
                    baseTileFound = true;
                    break;
                }
            }
            if (baseTileFound)
            {
                break;
            }
        }
        Vector2 currTile = targetPos * 3;
        bool targetTileFound = false;
        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                if (fillIdsArr[fillMap[(int)currTile.x + x, (int)currTile.y + y]] == fillIdsArr[startFillerIndex])
                {
                    currTile.x += x;
                    currTile.y += y;
                    targetTileFound = true;
                    break;
                }
            }
            if (targetTileFound)
            {
                break;
            }
        }

        System.Random prng = new System.Random(chunkSeed);
        Vector2 currToBase;
        bool[] markFill = new bool[fillIdsArr.Length];
        while (fillIdsArr[fillMap[(int)currTile.x, (int)currTile.y]] != fillIdsArr[baseFillerIndex])
        {
            //Move to the closest edge inside the targetFill
            while (true)
            {
                currToBase = baseTile - currTile;

                if (currToBase.x == 0 && currToBase.y == 0)
                    break;
                if (currToBase.x * currToBase.x >= currToBase.y * currToBase.y)
                {
                    if (currToBase.x > 0 && currTile.x + 1 < chunkSize && chunkMap[(int)currTile.x + 1, (int)currTile.y])
                        currTile.x++;
                    else if (currToBase.x < 0 && currTile.x - 1 >= 0 && chunkMap[(int)currTile.x - 1, (int)currTile.y])
                        currTile.x--;
                    else if (currToBase.y > 0 && currTile.y + 1 < chunkSize && chunkMap[(int)currTile.x, (int)currTile.y + 1])
                        currTile.y++;
                    else if (currToBase.y < 0 && currTile.y - 1 >= 0 && chunkMap[(int)currTile.x, (int)currTile.y - 1])
                        currTile.y--;
                    else
                        break;
                }
                else
                {
                    if (currToBase.y > 0 && currTile.y + 1 < chunkSize && chunkMap[(int)currTile.x, (int)currTile.y + 1])
                        currTile.y++;
                    else if (currToBase.y < 0 && currTile.y - 1 >= 0 && chunkMap[(int)currTile.x, (int)currTile.y - 1])
                        currTile.y--;
                    else if (currToBase.x > 0 && currTile.x + 1 < chunkSize && chunkMap[(int)currTile.x + 1, (int)currTile.y])
                        currTile.x++;
                    else if (currToBase.x < 0 && currTile.x - 1 >= 0 && chunkMap[(int)currTile.x - 1, (int)currTile.y])
                        currTile.x--;
                    else
                        break;
                }

                int currFillId = fillIdsArr[fillMap[(int)currTile.x, (int)currTile.y]];
                if (currFillId == fillIdsArr[baseFillerIndex])
                    return toBeMerged;
                if (currFillId != fillIdsArr[startFillerIndex] && currFillId != fillIdsArr[baseFillerIndex] && !markFill[currFillId])
                {
                    toBeMerged.Add(currFillId);
                    markFill[currFillId] = true;
                }
            }

            if (currToBase.x == 0 && currToBase.y == 0)
                break;

            //Break walls
            float dirAngle = Mathf.Atan2(currToBase.y, currToBase.x);
            float zigzagChance = Mathf.Abs(Mathf.Cos(2 * dirAngle));

            float value = prng.Next(0, 100000) / 100000f;
            float zzValue = prng.Next(0, 100000) / 100000f;
            if (value <= 0.5f)
            {
                if (zzValue <= zigzagChance)
                {
                    if (zzValue <= zigzagChance / 2f && currTile.y + 1 < chunkSize)
                    {
                        if (!chunkMap[(int)currTile.x, (int)currTile.y + 1])
                            fillMap[(int)currTile.x, (int)currTile.y + 1] = startFillerIndex;
                        chunkMap[(int)currTile.x, (int)currTile.y + 1] = true;
                    }
                    else if (currTile.y - 1 >= 0)
                    {
                        if (!chunkMap[(int)currTile.x, (int)currTile.y - 1])
                            fillMap[(int)currTile.x, (int)currTile.y - 1] = startFillerIndex;
                        chunkMap[(int)currTile.x, (int)currTile.y - 1] = true;
                    }
                }
                if (currToBase.x > 0 && currTile.x + 1 < chunkSize)
                {
                    currTile.x++;
                    if (!chunkMap[(int)currTile.x, (int)currTile.y])
                        fillMap[(int)currTile.x, (int)currTile.y] = startFillerIndex;
                    chunkMap[(int)currTile.x, (int)currTile.y] = true;
                }
                else if (currTile.x - 1 >= 0)
                {
                    currTile.x--;
                    if (!chunkMap[(int)currTile.x, (int)currTile.y])
                        fillMap[(int)currTile.x, (int)currTile.y] = startFillerIndex;
                    chunkMap[(int)currTile.x, (int)currTile.y] = true;
                }
            }
            else
            {
                if (zzValue <= zigzagChance)
                {
                    if (zzValue <= zigzagChance / 2f && currTile.x + 1 < chunkSize)
                    {
                        if (!chunkMap[(int)currTile.x + 1, (int)currTile.y])
                            fillMap[(int)currTile.x + 1, (int)currTile.y] = startFillerIndex;
                        chunkMap[(int)currTile.x + 1, (int)currTile.y] = true;
                    }
                    else if (currTile.x - 1 >= 0)
                    {
                        if (!chunkMap[(int)currTile.x - 1, (int)currTile.y])
                            fillMap[(int)currTile.x - 1, (int)currTile.y] = startFillerIndex;
                        chunkMap[(int)currTile.x - 1, (int)currTile.y] = true;
                    }
                }
                if (currToBase.y > 0 && currTile.y + 1 < chunkSize)
                {
                    currTile.y++;
                    if (!chunkMap[(int)currTile.x, (int)currTile.y])
                        fillMap[(int)currTile.x, (int)currTile.y] = startFillerIndex;
                    chunkMap[(int)currTile.x, (int)currTile.y] = true;
                }
                else if (currTile.y - 1 >= 0)
                {
                    currTile.y--;
                    if (!chunkMap[(int)currTile.x, (int)currTile.y])
                        fillMap[(int)currTile.x, (int)currTile.y] = startFillerIndex;
                    chunkMap[(int)currTile.x, (int)currTile.y] = true;
                }
            }
        }

        return toBeMerged;
    }

    private class Node : IComparable
    {
        public Vector2 pos;
        public int distance;

        public Node(Vector2 pos, int distance)
        {
            this.pos = pos;
            this.distance = distance;
        }

        public int CompareTo(object obj)
        {
            int objDist = ((Node)obj).distance;
            if (objDist > distance)
                return -1;
            if (objDist < distance)
                return 1;
            return 0;
        }
    }

    public class PriorityQueue<T> where T : IComparable
    {
        List<T> queue;

        public PriorityQueue()
        {
            queue = new List<T>();
        }

        public void Enqueue(T item)
        {
            queue.Add(item);
            int childIndex = queue.Count - 1;

            while (childIndex > 0)
            {
                int parentIndex = (childIndex - 1) / 2;

                if (queue[childIndex].CompareTo(queue[parentIndex]) >= 0)
                {
                    break;
                }

                T temp = queue[childIndex];
                queue[childIndex] = queue[parentIndex];
                queue[parentIndex] = temp;

                childIndex = parentIndex;
            }
        }

        public T Dequeue()
        {
            int lastIndex = queue.Count - 1;
            T frontItem = queue[0];
            queue[0] = queue[lastIndex];
            queue.RemoveAt(lastIndex);
            lastIndex--;
            int parentIndex = 0;

            while (true)
            {
                int childIndex = parentIndex * 2 + 1;

                if (childIndex > lastIndex)
                {
                    break;
                }

                int rightChild = childIndex + 1;
                if (rightChild <= lastIndex && queue[rightChild].CompareTo(queue[childIndex]) < 0)
                {
                    childIndex = rightChild;
                }
                if (queue[parentIndex].CompareTo(queue[childIndex]) <= 0)
                {
                    break;
                }

                T temp = queue[parentIndex];
                queue[parentIndex] = queue[childIndex];
                queue[childIndex] = temp;

                parentIndex = childIndex;
            }
            return frontItem;
        }

        public int Size()
        {
            return queue.Count;
        }
    }
}
