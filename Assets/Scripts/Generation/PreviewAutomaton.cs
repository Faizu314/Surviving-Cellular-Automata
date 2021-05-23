using System;
using UnityEngine;
using System.Collections.Generic;

public class PreviewAutomaton : MonoBehaviour
{
    public GameObject floorPrefab;
    public GameObject wallPrefab;
    [Range(0, 5)] public int chunkX;
    [Range(0, 5)] public int chunkY;
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

                tiles = SimulateTilesGeneration(currentChunkX, currentChunkY);

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
        prng = new System.Random(prng.Next(-100000, 100000) + currentChunkY);

        bool[,] initialTiles = new bool[chunkSize, chunkSize];

        for (int y = 0; y < chunkSize; y++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                initialTiles[x, y] = (prng.Next(0, 100000) / 100000f) < P;
            }
        }

        bool[,] iterativeTiles = new bool[chunkSize, chunkSize];

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
            initialTiles = iterativeTiles;
        }
        ChunkCorrection(iterativeTiles);
        return iterativeTiles;
    }

    private void ChunkCorrection(bool[,] chunkMap)
    {
        //This is to ensure the index of fillers begin with 1 (walls will have index = 0 and id = -1)
        List<int> fillIds = new List<int>();
        fillIds.Add(-1);

        int[,] fillMap = new int[chunkSize, chunkSize];
        int[,] blockMap = new int[chunkSize / 3, chunkSize / 3];

        List<TestFiller> fillers = new List<TestFiller>();
        for (int i = 0; i < 3; i++)
            fillers.AddRange(FloodFillPartition(chunkMap, fillMap, blockMap, fillIds, i));

        debugFills = fillMap;
        colors = fillIds;
        //Merge
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
    }
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


    public void OnDrawGizmos()
    {
        if (debugFills == null)
            return;
        if (colors == null)
            return;
        for (int y = 0; y < chunkSize; y++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                Color color = new Color();
                Debug.Log(debugFills[x, y]);
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
                Gizmos.DrawCube(new Vector2(x, y), Vector3.one);
            }
        }
    }
}
