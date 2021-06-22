using System.IO;
using System.Collections.Generic;
using System;
using UnityEngine;

public class TilesGenerator : MonoBehaviour
{
    private float INITIAL_TRUE_CELL_PROBABILITY;
    private bool EDGE_CELL;
    private int BIRTH_RATE;
    private int DEATH_RATE;
    private int ITERATIONS;

    public List<Func<int, int, bool[,]>> GenerationSteps = new List<Func<int, int, bool[,]>>();

    private void LoadTemplate()
    {
        using (StreamReader reader = new StreamReader("Assets/Preferences/CA_Config.txt"))
        {
            string line;
            line = reader.ReadLine();
            INITIAL_TRUE_CELL_PROBABILITY = float.Parse(line); 
            line = reader.ReadLine();
            EDGE_CELL = bool.Parse(line);
            line = reader.ReadLine();
            BIRTH_RATE = int.Parse(line);
            line = reader.ReadLine();
            DEATH_RATE = int.Parse(line);
            line = reader.ReadLine();
            ITERATIONS = int.Parse(line);
            reader.Close();
        }
    }

    private void Start()
    {
        GenerationSteps.Add(GenerateInitialMaps);
        GenerationSteps.Add(ApplyCellularAutomaton);
        GenerationSteps.Add(ChunkCorrection);

        LoadTemplate();
    }
    public bool[,] ExecuteGenerationStep(int chunkX, int chunkY, int step)
    {
        return GenerationSteps[step](chunkX, chunkY);
    }

    private int GetChunkSeed(int chunkX, int chunkY)
    {
        System.Random prng = new System.Random(EndlessCavern.SEED);
        prng = new System.Random(prng.Next(-100000, 100000) + chunkX);
        int chunkSeed = prng.Next(-100000, 100000) + chunkY;

        return chunkSeed;
    }
    private bool[,] GenerateInitialMaps(int chunkX, int chunkY)
    {
        bool[,] initialMainTiles = MapDataSaver.instance.GetTiles(new Vector3(chunkX, chunkY, 0));
        if (initialMainTiles == null)
        {
            initialMainTiles = GetInitialMap(chunkX, chunkY);
        }

        bool[,] initialUpTiles = MapDataSaver.instance.GetTiles(new Vector3(chunkX, chunkY + 1, 0));
        if (initialUpTiles == null)
        {
            initialUpTiles = GetInitialMap(chunkX, chunkY + 1);
        }

        bool[,] initialDownTiles = MapDataSaver.instance.GetTiles(new Vector3(chunkX, chunkY - 1, 0));
        if (initialDownTiles == null)
        {
            initialDownTiles = GetInitialMap(chunkX, chunkY - 1);
        }

        bool[,] initialLeftTiles = MapDataSaver.instance.GetTiles(new Vector3(chunkX - 1, chunkY, 0));
        if (initialLeftTiles == null)
        {
            initialLeftTiles = GetInitialMap(chunkX - 1, chunkY);
        }

        bool[,] initialRightTiles = MapDataSaver.instance.GetTiles(new Vector3(chunkX + 1, chunkY, 0));
        if (initialRightTiles == null)
        {
            initialRightTiles = GetInitialMap(chunkX + 1, chunkY);
        }

        MapDataSaver.instance.SaveTiles(new Vector3(chunkX, chunkY, 0), initialMainTiles);
        MapDataSaver.instance.SaveTiles(new Vector3(chunkX, chunkY + 1, 0), initialUpTiles);
        MapDataSaver.instance.SaveTiles(new Vector3(chunkX, chunkY - 1, 0), initialDownTiles);
        MapDataSaver.instance.SaveTiles(new Vector3(chunkX + 1, chunkY, 0), initialRightTiles);
        MapDataSaver.instance.SaveTiles(new Vector3(chunkX - 1, chunkY, 0), initialLeftTiles);

        return initialMainTiles;
    }
    private bool[,] ApplyCellularAutomaton(int chunkX, int chunkY)
    {
        bool[,] iterativeMainTiles = MapDataSaver.instance.GetTiles(new Vector3(chunkX, chunkY, 1));
        if (iterativeMainTiles != null)
            return iterativeMainTiles;

        int chunkSize = EndlessCavern.CHUNK_SIZE;

        bool[,] initialMainTiles = MapDataSaver.instance.GetTiles(new Vector3(chunkX, chunkY, 0));
        bool[,] initialUpTiles = MapDataSaver.instance.GetTiles(new Vector3(chunkX, chunkY + 1, 0));
        bool[,] initialDownTiles = MapDataSaver.instance.GetTiles(new Vector3(chunkX, chunkY - 1, 0));
        bool[,] initialLeftTiles = MapDataSaver.instance.GetTiles(new Vector3(chunkX - 1, chunkY, 0));
        bool[,] initialRightTiles = MapDataSaver.instance.GetTiles(new Vector3(chunkX + 1, chunkY, 0));

        iterativeMainTiles = new bool[chunkSize, chunkSize];
        bool[,] iterativeUpTiles = new bool[chunkSize, chunkSize];
        bool[,] iterativeDownTiles = new bool[chunkSize, chunkSize];
        bool[,] iterativeLeftTiles = new bool[chunkSize, chunkSize];
        bool[,] iterativeRightTiles = new bool[chunkSize, chunkSize];
        bool[,] temp;

        for (int i = 0; i < ITERATIONS; i++)
        {
            TopCellularAutomata(iterativeUpTiles, initialUpTiles, initialMainTiles, initialLeftTiles, initialRightTiles);
            LeftCellularAutomata(iterativeLeftTiles, initialLeftTiles, initialUpTiles, initialMainTiles, initialDownTiles);
            MainCellularAutomata(iterativeMainTiles, initialMainTiles, initialUpTiles, initialLeftTiles, initialRightTiles, initialDownTiles);
            RightCellularAutomata(iterativeRightTiles, initialRightTiles, initialUpTiles, initialMainTiles, initialDownTiles);
            DownCellularAutomata(iterativeDownTiles, initialDownTiles, initialLeftTiles, initialMainTiles, initialRightTiles);

            temp = initialUpTiles;
            initialUpTiles = iterativeUpTiles;
            iterativeUpTiles = temp;

            temp = initialLeftTiles;
            initialLeftTiles = iterativeLeftTiles;
            iterativeLeftTiles = temp;

            temp = initialMainTiles;
            initialMainTiles = iterativeMainTiles;
            iterativeMainTiles = temp;

            temp = initialRightTiles;
            initialRightTiles = iterativeRightTiles;
            iterativeRightTiles = temp;

            temp = initialDownTiles;
            initialDownTiles = iterativeDownTiles;
            iterativeDownTiles = temp;
        }

        MapDataSaver.instance.SaveTiles(new Vector3(chunkX, chunkY, 1), initialMainTiles);

        return initialMainTiles;
    }
    private bool[,] ChunkCorrection(int chunkX, int chunkY)
    {
        bool[,] chunkMap = MapDataSaver.instance.GetTiles(new Vector3(chunkX, chunkY, 2));
        if (chunkMap != null)
            return chunkMap;

        int chunkSize = EndlessCavern.CHUNK_SIZE;
        int chunkSeed = GetChunkSeed(chunkX, chunkY);

        chunkMap = MapDataSaver.instance.GetTiles(new Vector3(chunkX, chunkY, 1));
        List<int> fillIds = new List<int>();
        fillIds.Add(-1);

        int[,] fillMap = new int[chunkSize, chunkSize];
        int[,] blockMap = new int[chunkSize / 3, chunkSize / 3];

        List<Filler> fillers = new List<Filler>();
        for (int i = 0; i < 3; i++)
            fillers.AddRange(FloodFillPartition(chunkMap, fillMap, blockMap, fillIds, i));

        for (int i = 1; i < 3; i++)
        {
            int yStart = (int)(chunkSize * (i / 3f));
            for (int x = 0; x < chunkSize; x++)
            {
                int index1 = fillMap[x, yStart];
                int index2 = fillMap[x, yStart - 1];
                if (fillIds[index1] != fillIds[index2] && fillIds[index1] != -1 && fillIds[index2] != -1)
                {
                    Filler fillerB = fillers.Find((filler) => fillIds[filler.fillerIndex] == fillIds[index2]);
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

        chunkMap = Connector.ConnectAutomataPaths(fillers, fillIds, blockMap, fillMap, chunkMap, chunkSeed);

        MapDataSaver.instance.SaveTiles(new Vector3(chunkX, chunkY, 2), chunkMap);

        return chunkMap;
    }

    //Step one jobs
    #region
    private bool[,] GetInitialMap(int chunkX, int chunkY)
    {
        System.Random prng = new System.Random(GetChunkSeed(chunkX, chunkY));
        int chunkSize = EndlessCavern.CHUNK_SIZE;

        bool[,] initialTiles = new bool[chunkSize, chunkSize];

        for (int y = 0; y < chunkSize; y++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                initialTiles[x, y] = prng.Next(-100000, 100000) < INITIAL_TRUE_CELL_PROBABILITY;
            }
        }

        return initialTiles;
    }
    #endregion

    //Step two jobs
    #region
    private void TopCellularAutomata(bool[,] iterativeUpTiles, bool[,] initialUpTiles, bool[,] initialMainTiles, bool[,] initialLeftTiles, bool[,] initialRightTiles)
    {
        int chunkSize = EndlessCavern.CHUNK_SIZE;

        for (int y = 0; y < chunkSize; y++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                int t = 0;
                bool l, uL, u, uR, r, dR, d, dL;

                if (y == 0)
                {
                    dR = x == chunkSize - 1 ? initialRightTiles[0, chunkSize - 1] : initialMainTiles[x + 1, chunkSize - 1];
                    d = initialMainTiles[x, chunkSize - 1];
                    dL = x == 0 ? initialLeftTiles[chunkSize - 1, chunkSize - 1] : initialMainTiles[x - 1, chunkSize - 1];
                }
                else
                {
                    dR = (x == chunkSize - 1) || y == 0 ? EDGE_CELL : initialUpTiles[x + 1, y - 1];
                    d = y == 0 ? EDGE_CELL : initialUpTiles[x, y - 1];
                    dL = y == 0 || x == 0 ? EDGE_CELL : initialUpTiles[x - 1, y - 1];
                }

                l = x == 0 ? EDGE_CELL : initialUpTiles[x - 1, y];
                uL = x == 0 || (y == chunkSize - 1) ? EDGE_CELL : initialUpTiles[x - 1, y + 1];
                u = (y == chunkSize - 1) ? EDGE_CELL : initialUpTiles[x, y + 1];
                uR = (x == chunkSize - 1) || (y == chunkSize - 1) ? EDGE_CELL : initialUpTiles[x + 1, y + 1];
                r = (x == chunkSize - 1) ? EDGE_CELL : initialUpTiles[x + 1, y];

                t += Convert.ToInt32(l) + Convert.ToInt32(uL) + Convert.ToInt32(u) + Convert.ToInt32(uR) + Convert.ToInt32(r) + Convert.ToInt32(dR) + Convert.ToInt32(d) + Convert.ToInt32(dL);

                if (initialUpTiles[x, y])
                {
                    iterativeUpTiles[x, y] = !(t < DEATH_RATE);
                }
                else
                {
                    iterativeUpTiles[x, y] = t > BIRTH_RATE;
                }
            }
        }
    }
    private void LeftCellularAutomata(bool[,] iterativeLeftTiles, bool[,] initialLeftTiles, bool[,] initialUpTiles, bool[,] initialMainTiles, bool[,] initialDownTiles)
    {
        int chunkSize = EndlessCavern.CHUNK_SIZE;

        for (int y = 0; y < chunkSize; y++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                int t = 0;
                bool l, uL, u, uR, r, dR, d, dL;

                if (x == chunkSize - 1)
                {
                    uR = y == chunkSize - 1 ? initialUpTiles[0, 0] : initialMainTiles[0, y + 1];
                    r = initialMainTiles[0, y];
                    dR = y == 0 ? initialDownTiles[0, chunkSize - 1] : initialMainTiles[0, y - 1];
                }
                else
                {
                    uR = (x == chunkSize - 1) || (y == chunkSize - 1) ? EDGE_CELL : initialLeftTiles[x + 1, y + 1];
                    r = (x == chunkSize - 1) ? EDGE_CELL : initialLeftTiles[x + 1, y];
                    dR = (x == chunkSize - 1) || y == 0 ? EDGE_CELL : initialLeftTiles[x + 1, y - 1];
                }

                l = x == 0 ? EDGE_CELL : initialLeftTiles[x - 1, y];
                uL = x == 0 || (y == chunkSize - 1) ? EDGE_CELL : initialLeftTiles[x - 1, y + 1];
                u = (y == chunkSize - 1) ? EDGE_CELL : initialLeftTiles[x, y + 1];
                d = y == 0 ? EDGE_CELL : initialLeftTiles[x, y - 1];
                dL = y == 0 || x == 0 ? EDGE_CELL : initialLeftTiles[x - 1, y - 1];

                t += Convert.ToInt32(l) + Convert.ToInt32(uL) + Convert.ToInt32(u) + Convert.ToInt32(uR) + Convert.ToInt32(r) + Convert.ToInt32(dR) + Convert.ToInt32(d) + Convert.ToInt32(dL);

                if (initialLeftTiles[x, y])
                {
                    iterativeLeftTiles[x, y] = !(t < DEATH_RATE);
                }
                else
                {
                    iterativeLeftTiles[x, y] = t > BIRTH_RATE;
                }
            }
        }
    }
    private void MainCellularAutomata(bool[,] iterativeMainTiles, bool[,] initialMainTiles, bool[,] initialUpTiles, bool[,] initialLeftTiles, bool[,] initialRightTiles, bool[,] initialDownTiles)
    {
        int chunkSize = EndlessCavern.CHUNK_SIZE;

        for (int y = 0; y < chunkSize; y++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                int t = 0;
                bool l, uL, u, uR, r, dR, d, dL;

                if (x == 0 && y == 0)
                {
                    dL = EDGE_CELL;
                    l = initialLeftTiles[chunkSize - 1, y];
                    uL = initialLeftTiles[chunkSize - 1, y + 1];

                    d = initialDownTiles[x, chunkSize - 1];
                    dR = initialDownTiles[x + 1, chunkSize - 1];

                    u = initialMainTiles[x, y + 1];
                    uR = initialMainTiles[x + 1, y + 1];
                    r = initialMainTiles[x + 1, y];
                }
                else if (x == chunkSize - 1 && y == chunkSize - 1)
                {
                    uR = EDGE_CELL;
                    r = initialRightTiles[0, y];
                    dR = initialRightTiles[0, y - 1];

                    u = initialUpTiles[x, 0];
                    uL = initialUpTiles[x - 1, 0];

                    l = initialMainTiles[x - 1, y];
                    d = initialMainTiles[x, y - 1];
                    dL = initialMainTiles[x - 1, y - 1];
                }
                else if (x == 0 && y == chunkSize - 1)
                {
                    uL = EDGE_CELL;
                    l = initialLeftTiles[chunkSize - 1, y];
                    dL = initialLeftTiles[chunkSize - 1, y - 1];

                    u = initialUpTiles[x, 0];
                    uR = initialUpTiles[x + 1, 0];

                    r = initialMainTiles[x + 1, y];
                    dR = initialMainTiles[x + 1, y - 1];
                    d = initialMainTiles[x, y - 1];
                }
                else if (x == chunkSize - 1 && y == 0)
                {
                    dR = EDGE_CELL;
                    r = initialRightTiles[0, y];
                    uR = initialRightTiles[0, y + 1];

                    d = initialDownTiles[x, chunkSize - 1];
                    dL = initialDownTiles[x - 1, chunkSize - 1];

                    l = initialMainTiles[x - 1, y];
                    uL = initialMainTiles[x - 1, y + 1];
                    u = initialMainTiles[x, y + 1];
                }
                else if (x == 0)
                {
                    l = initialLeftTiles[chunkSize - 1, y];
                    uL = initialLeftTiles[chunkSize - 1, y + 1];
                    dL = initialLeftTiles[chunkSize - 1, y + 1];

                    u = initialMainTiles[x, y + 1];
                    uR = initialMainTiles[x + 1, y + 1];
                    r = initialMainTiles[x + 1, y];
                    dR = initialMainTiles[x + 1, y - 1];
                    d = initialMainTiles[x, y - 1];
                }
                else if (y == 0)
                {
                    d = initialDownTiles[x, chunkSize - 1];
                    dR = initialDownTiles[x + 1, chunkSize - 1];
                    dL = initialDownTiles[x - 1, chunkSize - 1];

                    l = initialMainTiles[x - 1, y];
                    uL = initialMainTiles[x - 1, y + 1];
                    u = initialMainTiles[x, y + 1];
                    uR = initialMainTiles[x + 1, y + 1];
                    r = initialMainTiles[x + 1, y];
                }
                else if (x == chunkSize - 1)
                {
                    r = initialRightTiles[0, y];
                    dR = initialRightTiles[0, y - 1];
                    uR = initialRightTiles[0, y + 1];

                    l = initialMainTiles[x - 1, y];
                    uL = initialMainTiles[x - 1, y + 1];
                    u = initialMainTiles[x, y + 1];
                    d = initialMainTiles[x, y - 1];
                    dL = initialMainTiles[x - 1, y - 1];
                }
                else if (y == chunkSize - 1)
                {
                    u = initialUpTiles[x, 0];
                    uL = initialUpTiles[x - 1, 0];
                    uR = initialUpTiles[x + 1, 0];

                    l = initialMainTiles[x - 1, y];
                    r = initialMainTiles[x + 1, y];
                    dR = initialMainTiles[x + 1, y - 1];
                    d = initialMainTiles[x, y - 1];
                    dL = initialMainTiles[x - 1, y - 1];
                }
                else
                {
                    l = initialMainTiles[x - 1, y];
                    uL = initialMainTiles[x - 1, y + 1];
                    u = initialMainTiles[x, y + 1];
                    uR = initialMainTiles[x + 1, y + 1];
                    r = initialMainTiles[x + 1, y];
                    dR = initialMainTiles[x + 1, y - 1];
                    d = initialMainTiles[x, y - 1];
                    dL = initialMainTiles[x - 1, y - 1];
                }

                t += Convert.ToInt32(l) + Convert.ToInt32(uL) + Convert.ToInt32(u) + Convert.ToInt32(uR) + Convert.ToInt32(r) + Convert.ToInt32(dR) + Convert.ToInt32(d) + Convert.ToInt32(dL);

                if (initialMainTiles[x, y])
                {
                    iterativeMainTiles[x, y] = !(t < DEATH_RATE);
                }
                else
                {
                    iterativeMainTiles[x, y] = t > BIRTH_RATE;
                }
            }
        }
    }
    private void RightCellularAutomata(bool[,] iterativeRightTiles, bool[,] initialRightTiles, bool[,] initialUpTiles, bool[,] initialMainTiles, bool[,] initialDownTiles)
    {
        int chunkSize = EndlessCavern.CHUNK_SIZE;

        for (int y = 0; y < chunkSize; y++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                int t = 0;
                bool l, uL, u, uR, r, dR, d, dL;

                if (x == 0)
                {
                    l = initialMainTiles[chunkSize - 1, y];
                    uL = y == chunkSize - 1 ? initialUpTiles[chunkSize - 1, 0] : initialMainTiles[chunkSize - 1, y + 1];
                    dL = y == 0 ? initialDownTiles[chunkSize - 1, chunkSize - 1] : initialMainTiles[chunkSize - 1, y - 1];
                }
                else
                {
                    l = x == 0 ? EDGE_CELL : initialRightTiles[x - 1, y];
                    uL = x == 0 || (y == chunkSize - 1) ? EDGE_CELL : initialRightTiles[x - 1, y + 1];
                    dL = y == 0 || x == 0 ? EDGE_CELL : initialRightTiles[x - 1, y - 1];
                }

                u = (y == chunkSize - 1) ? EDGE_CELL : initialRightTiles[x, y + 1];
                uR = (x == chunkSize - 1) || (y == chunkSize - 1) ? EDGE_CELL : initialRightTiles[x + 1, y + 1];
                r = (x == chunkSize - 1) ? EDGE_CELL : initialRightTiles[x + 1, y];
                dR = (x == chunkSize - 1) || y == 0 ? EDGE_CELL : initialRightTiles[x + 1, y - 1];
                d = y == 0 ? EDGE_CELL : initialRightTiles[x, y - 1];

                t += Convert.ToInt32(l) + Convert.ToInt32(uL) + Convert.ToInt32(u) + Convert.ToInt32(uR) + Convert.ToInt32(r) + Convert.ToInt32(dR) + Convert.ToInt32(d) + Convert.ToInt32(dL);

                if (initialRightTiles[x, y])
                {
                    iterativeRightTiles[x, y] = !(t < DEATH_RATE);
                }
                else
                {
                    iterativeRightTiles[x, y] = t > BIRTH_RATE;
                }
            }
        }
    }
    private void DownCellularAutomata(bool[,] iterativeDownTiles, bool[,] initialDownTiles, bool[,] initialLeftTiles, bool[,] initialMainTiles, bool[,] initialRightTiles)
    {
        int chunkSize = EndlessCavern.CHUNK_SIZE;

        for (int y = 0; y < chunkSize; y++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                int t = 0;
                bool l, uL, u, uR, r, dR, d, dL;

                if (y == chunkSize - 1)
                {
                    uL = x == 0 ? initialLeftTiles[chunkSize - 1, 0] : initialMainTiles[x - 1, 0];
                    u = initialMainTiles[x, 0];
                    uR = x == chunkSize - 1 ? initialRightTiles[0, 0] : initialMainTiles[x + 1, 0];
                }
                else
                {
                    uL = x == 0 || (y == chunkSize - 1) ? EDGE_CELL : initialDownTiles[x - 1, y + 1];
                    u = (y == chunkSize - 1) ? EDGE_CELL : initialDownTiles[x, y + 1];
                    uR = (x == chunkSize - 1) || (y == chunkSize - 1) ? EDGE_CELL : initialDownTiles[x + 1, y + 1];
                }

                l = x == 0 ? EDGE_CELL : initialDownTiles[x - 1, y];
                r = (x == chunkSize - 1) ? EDGE_CELL : initialDownTiles[x + 1, y];
                dR = (x == chunkSize - 1) || y == 0 ? EDGE_CELL : initialDownTiles[x + 1, y - 1];
                d = y == 0 ? EDGE_CELL : initialDownTiles[x, y - 1];
                dL = y == 0 || x == 0 ? EDGE_CELL : initialDownTiles[x - 1, y - 1];

                t += Convert.ToInt32(l) + Convert.ToInt32(uL) + Convert.ToInt32(u) + Convert.ToInt32(uR) + Convert.ToInt32(r) + Convert.ToInt32(dR) + Convert.ToInt32(d) + Convert.ToInt32(dL);

                if (initialDownTiles[x, y])
                {
                    iterativeDownTiles[x, y] = !(t < DEATH_RATE);
                }
                else
                {
                    iterativeDownTiles[x, y] = t > BIRTH_RATE;
                }
            }
        }
    }
    #endregion

    //Step three jobs
    #region
    private List<Filler> FloodFillPartition(in bool[,] chunkMap, int[,] fillMap, int[,] blockMap, List<int> fillIds, int partitionID)
    {
        int chunkSize = EndlessCavern.CHUNK_SIZE;
        int yStart = (int)(chunkSize * (partitionID / 3f));
        int yEnd = (int)(chunkSize * ((partitionID + 1f) / 3f));

        List<Filler> fillers = new List<Filler>();

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
                Filler filler = new Filler(index, yStart, yEnd, x, y);
                fillers.Add(filler);

                while (filler.Step(chunkMap, fillMap, blockMap)) ;
            }
        }

        return fillers;
    }
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
    #endregion
}
