using System.IO;
using System.Collections.Generic;
using System;
using UnityEngine;

// does not have to be a monobehaviour
public class TilesGenerator : MonoBehaviour
{
    private float INITIAL_TRUE_CELL_PROBABILITY;
    private bool EDGE_CELL;
    private int BIRTH_RATE;
    private int DEATH_RATE;
    private int ITERATIONS;
    private int CHUNK_SIZE;

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

    private void Initialize()
    {
        GenerationSteps.Add(GenerateInitialMaps);
        GenerationSteps.Add(ApplyCellularAutomaton);
        GenerationSteps.Add(ChunkCorrection);

        LoadTemplate();
    }

    public void SetChunkSize(int chunkSize)
    {
        CHUNK_SIZE = chunkSize;
    }
    public bool[,] ExecuteGenerationStep(int chunkX, int chunkY, int step)
    {
        if (GenerationSteps.Count == 0)
            Initialize();
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

        CellularAutomata.SetInitialCellProbability(INITIAL_TRUE_CELL_PROBABILITY);
        CellularAutomata.SetChunkSize(CHUNK_SIZE);

        if (initialMainTiles == null)
        {
            initialMainTiles = CellularAutomata.GetInitialMap(GetChunkSeed(chunkX, chunkY));
        }

        bool[,] initialUpTiles = MapDataSaver.instance.GetTiles(new Vector3(chunkX, chunkY + 1, 0));
        if (initialUpTiles == null)
        {
            initialUpTiles = CellularAutomata.GetInitialMap(GetChunkSeed(chunkX, chunkY + 1));
        }

        bool[,] initialDownTiles = MapDataSaver.instance.GetTiles(new Vector3(chunkX, chunkY - 1, 0));
        if (initialDownTiles == null)
        {
            initialDownTiles = CellularAutomata.GetInitialMap(GetChunkSeed(chunkX, chunkY - 1));
        }

        bool[,] initialLeftTiles = MapDataSaver.instance.GetTiles(new Vector3(chunkX - 1, chunkY, 0));
        if (initialLeftTiles == null)
        {
            initialLeftTiles = CellularAutomata.GetInitialMap(GetChunkSeed(chunkX - 1, chunkY));
        }

        bool[,] initialRightTiles = MapDataSaver.instance.GetTiles(new Vector3(chunkX + 1, chunkY, 0));
        if (initialRightTiles == null)
        {
            initialRightTiles = CellularAutomata.GetInitialMap(GetChunkSeed(chunkX + 1, chunkY));
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

        int chunkSize = CHUNK_SIZE;

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

        CellularAutomata.SetAutomataTemplate(EDGE_CELL, BIRTH_RATE, DEATH_RATE);

        for (int i = 0; i < ITERATIONS; i++)
        {
            CellularAutomata.TopCellularAutomata(iterativeUpTiles, initialUpTiles, initialMainTiles, initialLeftTiles, initialRightTiles);
            CellularAutomata.LeftCellularAutomata(iterativeLeftTiles, initialLeftTiles, initialUpTiles, initialMainTiles, initialDownTiles);
            CellularAutomata.MainCellularAutomata(iterativeMainTiles, initialMainTiles, initialUpTiles, initialLeftTiles, initialRightTiles, initialDownTiles);
            CellularAutomata.RightCellularAutomata(iterativeRightTiles, initialRightTiles, initialUpTiles, initialMainTiles, initialDownTiles);
            CellularAutomata.DownCellularAutomata(iterativeDownTiles, initialDownTiles, initialLeftTiles, initialMainTiles, initialRightTiles);

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

        int chunkSize = CHUNK_SIZE;
        int chunkSeed = GetChunkSeed(chunkX, chunkY);

        chunkMap = MapDataSaver.instance.GetTiles(new Vector3(chunkX, chunkY, 1));
        List<int> fillIds = new List<int>();
        fillIds.Add(-1);

        int[,] fillMap = new int[chunkSize, chunkSize];
        int[,] blockMap = new int[chunkSize / 3, chunkSize / 3];

        List<Filler> fillers = new List<Filler>();
        for (int i = 0; i < 3; i++)
            fillers.AddRange(FloodFill.FloodFillPartition(chunkMap, fillMap, blockMap, fillIds, i));

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
}