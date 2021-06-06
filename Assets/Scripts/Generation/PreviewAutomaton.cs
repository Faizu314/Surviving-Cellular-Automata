using System;
using UnityEngine;
using System.Collections.Generic;

public class PreviewAutomaton : MonoBehaviour
{
    public ComputeShader shader;
    public GameObject floorPrefab;
    public GameObject wallPrefab;
    [Range(0, 5)] public int chunkX;
    [Range(0, 5)] public int chunkY;
    public enum Test { correction, noCorrection };
    public enum Mode { CPU, GPU };
    public Test test;
    public Mode mode;
    public int chunkSize;
    public float cellWidth, cellHeight;
    public bool autoUpdate;

    [Space(10)]
    [Header("Automaton Configuration")]
    public int seed;
    public float P;
    public bool E;
    public int B;
    public int D;
    public int ITERATIONS;

    private bool[,] tiles;
    private GameObject chunkObject;
    private GameObject[,] cells;
    public int[,] debugFills;
    public List<int> colors;

    public void Preview()
    {
        if (transform.childCount != 0)
        {
            int childCount = transform.childCount;
            for (int k = 0; k < childCount; k++)
            {
                DestroyImmediate(transform.GetChild(0).gameObject);
            }
        }

        cells = new GameObject[chunkSize, chunkSize];

        for (int i = 0; i < chunkY; i++)
        {
            for (int j = 0; j < chunkX; j++)
            {
                int currentChunkY = i;
                int currentChunkX = j;

                chunkObject = new GameObject("Chunk: " + currentChunkX + ", " + currentChunkY);
                chunkObject.transform.parent = transform;

                if (mode == Mode.CPU)
                    tiles = SimulateTilesGeneration(currentChunkX, currentChunkY);
                else if (mode == Mode.GPU)
                    tiles = GenerateTilesOnGPU(currentChunkX, currentChunkY);

                for (int y = 0; y < chunkSize; y++)
                {
                    for (int x = 0; x < chunkSize; x++)
                    {
                        cells[x, y] = Instantiate(tiles[x, y] ? floorPrefab : wallPrefab);
                        cells[x, y].name = "Cell: " + x + ", " + y;
                        cells[x, y].transform.position = new Vector2((currentChunkX * chunkSize + x + 0.5f) * cellWidth, (currentChunkY * chunkSize + y + 0.5f) * cellHeight);
                        cells[x, y].transform.parent = chunkObject.transform;
                    }
                }
            }
        }
    }

    public bool[,] SimulateTilesGeneration(int currentChunkX, int currentChunkY)
    {
        System.Random prng = new System.Random(seed);
        prng = new System.Random(prng.Next(-100000, 100000) + currentChunkX);
        int chunkSeed = prng.Next(-100000, 100000) + currentChunkY;
        prng = new System.Random(chunkSeed);

        bool[,] initialTiles = new bool[chunkSize, chunkSize];

        for (int y = 0; y < chunkSize; y++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                initialTiles[x, y] = (prng.Next(0, 100000) / 100000f) < P;
            }
        }

        if (ITERATIONS == 0)
            return initialTiles;

        bool[,] iterativeTiles = new bool[chunkSize, chunkSize];

        bool[,] temp;
        for (int i = 0; i < ITERATIONS; i++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                for (int x = 0; x < chunkSize; x++)
                {
                    int t = 0;
                    bool l, uL, u, uR, r, dR, d, dL;
                    l = x == 0 ? E : initialTiles[x - 1, y];
                    uL = x == 0 || (y == chunkSize - 1) ? E : initialTiles[x - 1, y + 1];
                    u = (y == chunkSize - 1) ? E : initialTiles[x, y + 1];
                    uR = (x == chunkSize - 1) || (y == chunkSize - 1) ? E : initialTiles[x + 1, y + 1];
                    r = (x == chunkSize - 1) ? E : initialTiles[x + 1, y];
                    dR = (x == chunkSize - 1) || y == 0 ? E : initialTiles[x + 1, y - 1];
                    d = y == 0 ? E : initialTiles[x, y - 1];
                    dL = y == 0 || x == 0 ? E : initialTiles[x - 1, y - 1];

                    t += Convert.ToInt32(l) + Convert.ToInt32(uL) + Convert.ToInt32(u) + Convert.ToInt32(uR) + Convert.ToInt32(r) + Convert.ToInt32(dR) + Convert.ToInt32(d) + Convert.ToInt32(dL);

                    if (initialTiles[x, y])
                    {
                        iterativeTiles[x, y] = !(t < D);
                    }
                    else
                    {
                        iterativeTiles[x, y] = t > B;
                    }
                }
            }
            temp = initialTiles;
            initialTiles = iterativeTiles;
            iterativeTiles = temp;
        }

        if (test == Test.correction)
            initialTiles = ChunkCorrection(initialTiles, chunkSeed);
        return initialTiles;
    }

    public bool[,] GenerateTilesOnGPU(int currentChunkX, int currentChunkY)
    {
        System.Random prng = new System.Random(seed);
        prng = new System.Random(prng.Next(-100000, 100000) + currentChunkX);
        int chunkSeed = prng.Next(-100000, 100000) + currentChunkY;
        prng = new System.Random(chunkSeed);

        float[,] initialTiles = new float[chunkSize, chunkSize];

        for (int y = 0; y < chunkSize; y++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                initialTiles[x, y] = (prng.Next(0, 100000) / 100000f) < P ? 1 : 0;
            }
        }

        ComputeBuffer prev = new ComputeBuffer(chunkSize * chunkSize, sizeof(float), ComputeBufferType.Default);
        ComputeBuffer curr = new ComputeBuffer(chunkSize * chunkSize, sizeof(float), ComputeBufferType.Default);

        shader.SetInt("E", E ? 1 : 0);
        shader.SetInt("B", B);
        shader.SetInt("D", D);
        shader.SetInt("chunkSize", chunkSize);

        int kernelHandle = shader.FindKernel("CSMain");
        
        prev.SetData(initialTiles);
        for (int i = 0; i < ITERATIONS; i++)
        {
            if (i % 2 == 0)
            {
                shader.SetBuffer(kernelHandle, "initialTiles", prev);
                shader.SetBuffer(kernelHandle, "iterativeTiles", curr);
            }
            else
            {
                shader.SetBuffer(kernelHandle, "initialTiles", curr);
                shader.SetBuffer(kernelHandle, "iterativeTiles", prev);
            }
            shader.Dispatch(kernelHandle, 1, 1, 1);    
        }
        if (ITERATIONS % 2 == 0)
        {
            prev.GetData(initialTiles);
        }
        else
        {
            curr.GetData(initialTiles);
        }
        curr.Dispose();
        prev.Dispose();

        bool[,] answer = new bool[chunkSize, chunkSize];
        for (int y = 0; y < chunkSize; y++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                answer[x, y] = initialTiles[x, y] == 1f;
            }
        }
        return answer;
    }

    private bool[,] ChunkCorrection(bool[,] chunkMap, int chunkSeed)
    {
        List<int> fillIds = new List<int>();
        fillIds.Add(-1);

        int[,] fillMap = new int[chunkSize, chunkSize];
        int[,] blockMap = new int[chunkSize / 3, chunkSize / 3];

        List<TestFiller> fillers = new List<TestFiller>();
        for (int i = 0; i < 3; i++)
            fillers.AddRange(FloodFillPartition(chunkMap, fillMap, blockMap, fillIds, i));

        debugFills = blockMap;
        colors = fillIds;

        for (int i = 1; i < 3; i++)
        {
            int yStart = (int)(chunkSize * (i / 3f));
            for (int x = 0; x < chunkSize; x++)
            {
                int index1 = fillMap[x, yStart];
                int index2 = fillMap[x, yStart - 1];
                if (fillIds[index1] != fillIds[index2] && fillIds[index1] != -1 && fillIds[index2] != -1)
                {
                    TestFiller fillerB = fillers.Find((filler) => filler.fillerIndex == index2);
                    //TestFiller fillerB = fillers.Find((filler) => fillerIds[filler.fillerIndex] == fillerIds[index2]);
                    int colorToReplace = fillIds[index2];
                    int colorToReplaceWith = fillIds[index1];
                    for (int j = 0; j < fillIds.Count; j++)
                    {
                        if (fillIds[j] == colorToReplace)
                            fillIds[j] = colorToReplaceWith;
                    }
                    fillers.Remove(fillerB);
                }
            }
        }

        List<int> duplicates = new List<int>();
        for (int i = 0; i < fillers.Count; i++)
        {
            if (duplicates.Contains(fillIds[fillers[i].fillerIndex]))
                fillers.Remove(fillers[i--]);
            else
                duplicates.Add(fillIds[fillers[i].fillerIndex]);
        }

        return ConnectAutomataPaths(fillers, fillIds, blockMap, fillMap, chunkMap, chunkSeed);
    }

    //Connector code
    #region
    private List<TestFiller> FloodFillPartition(in bool[,] chunkMap, int[,] fillMap, int[,] blockMap, List<int> fillIds, int partitionID)
    {
        int yStart = (int)(chunkSize * (partitionID / 3f));
        int yEnd = (int)(chunkSize * ((partitionID + 1f) / 3f));

        List<TestFiller> fillers = new List<TestFiller>();

        for (int y = yStart; y < yEnd; y++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                if (!chunkMap[x, y])
                    continue;
                if (fillMap[x, y] != 0)
                    continue;

                int index = fillIds.Count;
                fillIds.Add(index);
                fillMap[x, y] = index;
                TestFiller filler = new TestFiller(index, yStart, yEnd, x, y);
                fillers.Add(filler);

                while (filler.Step(chunkMap, fillMap, blockMap)) ;
            }
        }

        return fillers;
    }
    public class TestFiller
    {
        public readonly int fillerIndex;

        private Queue<Vector2> toExplore;
        private int yStart, yEnd;
        private int currX, currY;

        public TestFiller(int index, int yStart, int yEnd, int currX, int currY)
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
    private int ManhattanDistance(Vector2 a, Vector2 b)
    {
        return (int)Mathf.Abs(a.x - b.x) + (int)Mathf.Abs(a.y - b.y);
    }
    private Vector2 AStar(int startFillId, List<int> fillIds, int[,] blockMap, Vector2 start, Vector2 target)
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
    private bool[,] ConnectAutomataPaths(List<TestFiller> fillers, List<int> fillIds, int[,] blockMap, int[,] fillMap, bool[,] chunkMap, int chunkSeed)
    {
        TestFiller baseFiller = fillers[0];

        Vector2 baseBlockPos = new Vector2((int)baseFiller.GetPoint().x / 3, (int)baseFiller.GetPoint().y / 3);
        int baseFillerId = fillIds[baseFiller.fillerIndex];

        while (fillers.Count != 1)
        {
            Vector2 startPoint = new Vector2((int)fillers[1].GetPoint().x / 3, (int)fillers[1].GetPoint().y / 3);
            Vector2 newBasePoint = AStar(fillIds[baseFillerId], fillIds, blockMap, baseBlockPos, startPoint);
            Vector2 newStartPoint = AStar(fillIds[fillers[1].fillerIndex], fillIds, blockMap, startPoint, newBasePoint);
            List<int> toBeMerged = RandomDirectedWalk(baseFiller.fillerIndex, fillers[1].fillerIndex, ref newBasePoint, ref newStartPoint, fillIds, chunkMap, fillMap, chunkSeed);

            for (int i = 0; i < toBeMerged.Count; i++)
            {
                int mergingId = toBeMerged[0];
                fillIds.ConvertAll((id) => id = (id == mergingId ? baseFillerId : id));
                fillers.Remove(fillers.Find((filler) => fillIds[filler.fillerIndex] == mergingId));
                toBeMerged.RemoveAt(0);
            }
        }

        return chunkMap;
    }
    private List<int> RandomDirectedWalk(int baseFillerIndex, int startFillerIndex, ref Vector2 basePos, ref Vector2 targetPos, List<int> fillIds, bool[,] chunkMap, int[,] fillMap, int chunkSeed)
    {
        int chunkSize = chunkMap.GetLength(0);
        List<int> toBeMerged = new List<int>();
        toBeMerged.Add(fillIds[startFillerIndex]);

        Vector2 baseTile = basePos * 3;
        bool baseTileFound = false;
        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                if (fillIds[fillMap[(int)baseTile.x + x, (int)baseTile.y + y]] == fillIds[baseFillerIndex])
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
                if (fillIds[fillMap[(int)currTile.x + x, (int)currTile.y + y]] == fillIds[startFillerIndex])
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
        while (fillIds[fillMap[(int)currTile.x, (int)currTile.y]] != fillIds[baseFillerIndex])
        {
            Vector2 currToBase;
            //Move to the closest edge inside the targetFill
            while (true)
            {
                currToBase = baseTile - currTile;

                if (currToBase.sqrMagnitude == 0)
                    break;
                if (Mathf.Abs(currToBase.x) >= Mathf.Abs(currToBase.y))
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

                int currFillId = fillIds[fillMap[(int)currTile.x, (int)currTile.y]];
                if (currFillId == fillIds[baseFillerIndex])
                    return toBeMerged;
                if (currFillId != fillIds[startFillerIndex] && currFillId != fillIds[baseFillerIndex] && !toBeMerged.Contains(currFillId))
                    toBeMerged.Add(currFillId);
            }

            if (currToBase.sqrMagnitude == 0)
                break;

            //Break walls
            float dirAngle = Mathf.Atan2(currToBase.y, currToBase.x);
            float zigzagChance = Mathf.Abs(Mathf.Cos(2 * dirAngle * Mathf.Rad2Deg));

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
    private class PriorityQueue<T> where T : IComparable
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
    #endregion


    public void OnDrawGizmos()
    {
        if (debugFills == null)
            return;
        if (colors == null)
            return;
        for (int y = 0; y < chunkSize / 3; y++)
        {
            for (int x = 0; x < chunkSize / 3; x++)
            {
                Color color = new Color();
                switch (colors[debugFills[x, y]]) {
                    case 1:
                        color = Color.red;
                        break;
                    case 2:
                        color = Color.green;
                        break;
                    case 3:
                        color = Color.blue;
                        break;
                    case 4:
                        color = Color.white;
                        break;
                    case 5:
                        color = Color.cyan;
                        break;
                    case 6:
                        color = Color.gray;
                        break;
                    case 7:
                        color = Color.magenta;
                        break;
                    case 8:
                        color = Color.black;
                        break;
                    case -1:
                        color = Color.clear;
                        break;
                    case 9:
                        color = new Color(234, 146, 13);
                        break;
                    case 10:
                        color = new Color(1, 193, 31);
                        break;
                }
                Gizmos.color = color;
                Gizmos.DrawCube(new Vector2(x * 3, y * 3), Vector3.one * 3);
            }
        }
    }
}
