using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class TilesGenerator : MonoBehaviour
{
    private EndlessCavern endlessCavern;
    //Probability of a True cell in initialization.
    private const float P = 0.57f;
    //Whether the boundary cells are considered true or false
    private const bool E = true;
    //Birth Rate
    private const int B = 5;
    //Death Rate
    private const int D = 2;
    private const int ITERATIONS = 2;

    public List<Func<int, int, bool[,]>> GenerationSteps = new List<Func<int, int, bool[,]>>();

    //will have a queue of threads => this will give us control over how many threads can be dispatched at a time
    //and it can allow us to dispatch certain threads before others

    private void Start()
    {
        endlessCavern = GameObject.Find("Cavern Generator").GetComponent<EndlessCavern>();
        GenerationSteps.Add(GenerateInitialMaps);
        GenerationSteps.Add(ApplyCellularAutomaton);
    }
    public bool[,] ExecuteGenerationStep(int chunkX, int chunkY, int step)
    {
        return GenerationSteps[step](chunkX, chunkY);
    }

    private bool[,] GenerateInitialMaps(int chunkX, int chunkY)
    {
        bool[,] initialMainTiles = MapDataSaver.instance.GetInitialTiles(new Vector2(chunkX, chunkY));
        if (initialMainTiles == null)
        {
            System.Random prngMain = new System.Random(EndlessCavern.SEED);
            prngMain = new System.Random(prngMain.Next(-100000, 100000) + chunkX);
            prngMain = new System.Random(prngMain.Next(-100000, 100000) + chunkY);
            initialMainTiles = GetInitialMap(prngMain);
        }

        bool[,] initialUpTiles = MapDataSaver.instance.GetInitialTiles(new Vector2(chunkX, chunkY + 1));
        if (initialUpTiles == null)
        {
            System.Random prngUp = new System.Random(EndlessCavern.SEED);
            prngUp = new System.Random(prngUp.Next(-100000, 100000) + chunkX);
            prngUp = new System.Random(prngUp.Next(-100000, 100000) + chunkY + 1);
            initialUpTiles = GetInitialMap(prngUp);
        }

        bool[,] initialDownTiles = MapDataSaver.instance.GetInitialTiles(new Vector2(chunkX, chunkY - 1));
        if (initialDownTiles == null)
        {
            System.Random prngDown = new System.Random(EndlessCavern.SEED);
            prngDown = new System.Random(prngDown.Next(-100000, 100000) + chunkX);
            prngDown = new System.Random(prngDown.Next(-100000, 100000) + chunkY - 1);
            initialDownTiles = GetInitialMap(prngDown);
        }

        bool[,] initialLeftTiles = MapDataSaver.instance.GetInitialTiles(new Vector2(chunkX - 1, chunkY));
        if (initialLeftTiles == null)
        {
            System.Random prngLeft = new System.Random(EndlessCavern.SEED);
            prngLeft = new System.Random(prngLeft.Next(-100000, 100000) + chunkX - 1);
            prngLeft = new System.Random(prngLeft.Next(-100000, 100000) + chunkY);
            initialLeftTiles = GetInitialMap(prngLeft);
        }

        bool[,] initialRightTiles = MapDataSaver.instance.GetInitialTiles(new Vector2(chunkX + 1, chunkY));
        if (initialRightTiles == null)
        {
            System.Random prngRight = new System.Random(EndlessCavern.SEED);
            prngRight = new System.Random(prngRight.Next(-100000, 100000) + chunkX + 1);
            prngRight = new System.Random(prngRight.Next(-100000, 100000) + chunkY);
            initialRightTiles = GetInitialMap(prngRight);
        }

        MapDataSaver.instance.SaveInitialTiles(new Vector2(chunkX, chunkY), initialMainTiles);
        MapDataSaver.instance.SaveInitialTiles(new Vector2(chunkX, chunkY + 1), initialUpTiles);
        MapDataSaver.instance.SaveInitialTiles(new Vector2(chunkX, chunkY - 1), initialDownTiles);
        MapDataSaver.instance.SaveInitialTiles(new Vector2(chunkX + 1, chunkY), initialRightTiles);
        MapDataSaver.instance.SaveInitialTiles(new Vector2(chunkX - 1, chunkY), initialLeftTiles);

        return initialMainTiles;
    }
    private bool[,] ApplyCellularAutomaton(int chunkX, int chunkY)
    {
        bool[,] iterativeMainTiles = MapDataSaver.instance.GetAutomataTiles(new Vector2(chunkX, chunkY));
        if (iterativeMainTiles != null)
            return iterativeMainTiles;

        int chunkSize = EndlessCavern.CHUNK_SIZE;

        bool[,] initialMainTiles = MapDataSaver.instance.GetInitialTiles(new Vector2(chunkX, chunkY));
        bool[,] initialUpTiles = MapDataSaver.instance.GetInitialTiles(new Vector2(chunkX, chunkY + 1));
        bool[,] initialDownTiles = MapDataSaver.instance.GetInitialTiles(new Vector2(chunkX, chunkY - 1));
        bool[,] initialLeftTiles = MapDataSaver.instance.GetInitialTiles(new Vector2(chunkX - 1, chunkY));
        bool[,] initialRightTiles = MapDataSaver.instance.GetInitialTiles(new Vector2(chunkX + 1, chunkY));

        iterativeMainTiles = new bool[chunkSize, chunkSize];
        bool[,] iterativeUpTiles = new bool[chunkSize, chunkSize];
        bool[,] iterativeDownTiles = new bool[chunkSize, chunkSize];
        bool[,] iterativeLeftTiles = new bool[chunkSize, chunkSize];
        bool[,] iterativeRightTiles = new bool[chunkSize, chunkSize];

        for (int i = 0; i < ITERATIONS; i++)
        {
            TopCellularAutomata(iterativeUpTiles, initialUpTiles, initialMainTiles, initialLeftTiles, initialRightTiles);
            LeftCellularAutomata(iterativeLeftTiles, initialLeftTiles, initialUpTiles, initialMainTiles, initialDownTiles);
            MainCellularAutomata(iterativeMainTiles, initialMainTiles, initialUpTiles, initialLeftTiles, initialRightTiles, initialDownTiles);
            RightCellularAutomata(iterativeRightTiles, initialRightTiles, initialUpTiles, initialMainTiles, initialDownTiles);
            DownCellularAutomata(iterativeDownTiles, initialDownTiles, initialLeftTiles, initialMainTiles, initialRightTiles);

            initialUpTiles = iterativeUpTiles;
            initialLeftTiles = iterativeLeftTiles;
            initialMainTiles = iterativeMainTiles;
            initialRightTiles = iterativeRightTiles;
            initialDownTiles = iterativeDownTiles;
        }

        MapDataSaver.instance.SaveAutomataTiles(new Vector2(chunkX, chunkY), iterativeMainTiles);

        return iterativeMainTiles;
    }
    //private bool[,] ChunkCorrection(int chunkX, int chunkY)
    //{
    //    bool[,] chunkMap = MapDataSaver.instance.GetCorrectedTiles(new Vector2(chunkX, chunkY));
    //    if (chunkMap != null)
    //        return chunkMap;

    //    //This is to ensure the index of fillers begin with 1 (walls will have index = 0 and id = -1)
    //    List<int> fillIds = new List<int>();
    //    fillIds.Add(-1);

    //    chunkMap = MapDataSaver.instance.GetAutomataTiles(new Vector2(chunkX, chunkY));
    //    int chunkSize = EndlessCavern.CHUNK_SIZE;
    //    int[,] fillMap = new int[chunkSize, chunkSize];

    //    List<Filler> fillers = new List<Filler>();
    //    for (int i = 0; i < 3; i++) {
    //        fillers.AddRange(FloodFillPartition(chunkMap, fillMap, i, fillIds));
    //    }

    //    //Merge the borders
    //    for (int i = 1; i < 3; i++)
    //    {
    //        int yStart = chunkSize * (i / 3);
    //        for (int x = 0; x < chunkSize; x++)
    //        {
    //            int index1 = fillMap[x, yStart];
    //            int index2 = fillMap[x, yStart - 1];
    //            if (fillIds[index1] != fillIds[index2] && fillIds[index1] != -1 && fillIds[index2] != -1)
    //            {
    //                //neighboring fills exist
    //                Filler fillerA = fillers.Find((filler) => filler.fillerIndex == index1);
    //                Filler fillerB = fillers.Find((filler) => filler.fillerIndex == index2);
    //                fillerA.fillCollider.Encapsulate(fillerB.fillCollider);
    //                fillIds[index2] = fillIds[index1];
    //                fillers.Remove(fillerB);
    //            }
    //        }
    //    }

    //    int j = 0;
    //    while (fillers.Count != 1)
    //    {
    //        Vector2 pointA = fillers[0].GetPoint();
    //        Vector2 pointB = pointA;
    //        float minSqrDistance = float.MaxValue;
    //        int minFillerIndex = -1;
    //        foreach (Filler filler in fillers)
    //        {
    //            if (filler != fillers[0])
    //            {
    //                float sqrDistance = filler.fillCollider.SqrDistance(pointA);
    //                if (sqrDistance < minSqrDistance)
    //                {
    //                    minSqrDistance = sqrDistance;
    //                    pointB = filler.fillCollider.ClosestPoint(pointA);
    //                    minFillerIndex = j;
    //                }
    //            }
    //            j++;
    //        }
    //        pointA = fillers[0].fillCollider.ClosestPoint(pointB);
    //        //Apply connecting algorithm to pointA and pointB (send chunkMap and fillMap so it applies the changes as it goes)
    //        fillers[0].fillCollider.Encapsulate(fillers[minFillerIndex].fillCollider);
    //        fillers.RemoveAt(j);
    //    }

    //    return chunkMap;
    //}

    //Step one jobs
    #region
    private bool[,] GetInitialMap(System.Random prng)
    {
        int chunkSize = EndlessCavern.CHUNK_SIZE;

        bool[,] initialTiles = new bool[chunkSize, chunkSize];

        for (int y = 0; y < chunkSize; y++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                initialTiles[x, y] = prng.Next(-100000, 100000) < P;
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
                    dR = (x == chunkSize - 1) || y == 0 ? E : initialUpTiles[x + 1, y - 1];
                    d = y == 0 ? E : initialUpTiles[x, y - 1];
                    dL = y == 0 || x == 0 ? E : initialUpTiles[x - 1, y - 1];
                }

                l = x == 0 ? E : initialUpTiles[x - 1, y];
                uL = x == 0 || (y == chunkSize - 1) ? E : initialUpTiles[x - 1, y + 1];
                u = (y == chunkSize - 1) ? E : initialUpTiles[x, y + 1];
                uR = (x == chunkSize - 1) || (y == chunkSize - 1) ? E : initialUpTiles[x + 1, y + 1];
                r = (x == chunkSize - 1) ? E : initialUpTiles[x + 1, y];

                t += Convert.ToInt32(l) + Convert.ToInt32(uL) + Convert.ToInt32(u) + Convert.ToInt32(uR) + Convert.ToInt32(r) + Convert.ToInt32(dR) + Convert.ToInt32(d) + Convert.ToInt32(dL);

                if (initialUpTiles[x, y])
                {
                    iterativeUpTiles[x, y] = !(t < D);
                }
                else
                {
                    iterativeUpTiles[x, y] = t > B;
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
                    uR = (x == chunkSize - 1) || (y == chunkSize - 1) ? E : initialLeftTiles[x + 1, y + 1];
                    r = (x == chunkSize - 1) ? E : initialLeftTiles[x + 1, y];
                    dR = (x == chunkSize - 1) || y == 0 ? E : initialLeftTiles[x + 1, y - 1];
                }

                l = x == 0 ? E : initialLeftTiles[x - 1, y];
                uL = x == 0 || (y == chunkSize - 1) ? E : initialLeftTiles[x - 1, y + 1];
                u = (y == chunkSize - 1) ? E : initialLeftTiles[x, y + 1];
                d = y == 0 ? E : initialLeftTiles[x, y - 1];
                dL = y == 0 || x == 0 ? E : initialLeftTiles[x - 1, y - 1];

                t += Convert.ToInt32(l) + Convert.ToInt32(uL) + Convert.ToInt32(u) + Convert.ToInt32(uR) + Convert.ToInt32(r) + Convert.ToInt32(dR) + Convert.ToInt32(d) + Convert.ToInt32(dL);

                if (initialLeftTiles[x, y])
                {
                    iterativeLeftTiles[x, y] = !(t < D);
                }
                else
                {
                    iterativeLeftTiles[x, y] = t > B;
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
                    dL = E;
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
                    uR = E;
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
                    uL = E;
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
                    dR = E;
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
                    iterativeMainTiles[x, y] = !(t < D);
                }
                else
                {
                    iterativeMainTiles[x, y] = t > B;
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
                    l = x == 0 ? E : initialRightTiles[x - 1, y];
                    uL = x == 0 || (y == chunkSize - 1) ? E : initialRightTiles[x - 1, y + 1];
                    dL = y == 0 || x == 0 ? E : initialRightTiles[x - 1, y - 1];
                }

                u = (y == chunkSize - 1) ? E : initialRightTiles[x, y + 1];
                uR = (x == chunkSize - 1) || (y == chunkSize - 1) ? E : initialRightTiles[x + 1, y + 1];
                r = (x == chunkSize - 1) ? E : initialRightTiles[x + 1, y];
                dR = (x == chunkSize - 1) || y == 0 ? E : initialRightTiles[x + 1, y - 1];
                d = y == 0 ? E : initialRightTiles[x, y - 1];

                t += Convert.ToInt32(l) + Convert.ToInt32(uL) + Convert.ToInt32(u) + Convert.ToInt32(uR) + Convert.ToInt32(r) + Convert.ToInt32(dR) + Convert.ToInt32(d) + Convert.ToInt32(dL);

                if (initialRightTiles[x, y])
                {
                    iterativeRightTiles[x, y] = !(t < D);
                }
                else
                {
                    iterativeRightTiles[x, y] = t > B;
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
                    uL = x == 0 || (y == chunkSize - 1) ? E : initialDownTiles[x - 1, y + 1];
                    u = (y == chunkSize - 1) ? E : initialDownTiles[x, y + 1];
                    uR = (x == chunkSize - 1) || (y == chunkSize - 1) ? E : initialDownTiles[x + 1, y + 1];
                }

                l = x == 0 ? E : initialDownTiles[x - 1, y];
                r = (x == chunkSize - 1) ? E : initialDownTiles[x + 1, y];
                dR = (x == chunkSize - 1) || y == 0 ? E : initialDownTiles[x + 1, y - 1];
                d = y == 0 ? E : initialDownTiles[x, y - 1];
                dL = y == 0 || x == 0 ? E : initialDownTiles[x - 1, y - 1];

                t += Convert.ToInt32(l) + Convert.ToInt32(uL) + Convert.ToInt32(u) + Convert.ToInt32(uR) + Convert.ToInt32(r) + Convert.ToInt32(dR) + Convert.ToInt32(d) + Convert.ToInt32(dL);

                if (initialDownTiles[x, y])
                {
                    iterativeDownTiles[x, y] = !(t < D);
                }
                else
                {
                    iterativeDownTiles[x, y] = t > B;
                }
            }
        }
    }
    #endregion
    private List<Filler> FloodFillPartition(in bool[,] chunkMap, int[,] fillMap, int partitionID, List<int> fillIds)
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
                Filler filler = new Filler(index, yStart, yEnd, x, y);
                fillers.Add(filler);

                while (!filler.Step(chunkMap, fillMap));
            }
        }

        return fillers;
    }
    private class Filler
    {
        public readonly int fillerIndex;
        public MeshCollider fillCollider;
        private List<Vector3> fillVertices;

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

            fillCollider = new MeshCollider();
            fillVertices = new List<Vector3>();
            toExplore = new Queue<Vector2>();
            toExplore.Enqueue(new Vector2(currX, currY));
        }
        public Vector2 GetPoint()
        {
            return new Vector2(currX, currY);
        }
        public bool Step(in bool[,] tileMap, int[,] fillMap)
        {
            if (toExplore.Count == 0)
            {
                Mesh mesh = new Mesh();
                mesh.vertices = fillVertices.ToArray();
                fillCollider.sharedMesh = mesh;
                return false;
            }

            int size = tileMap.GetLength(0);
            Vector2 floorPos = toExplore.Dequeue();
            currX = (int)floorPos.x;
            currY = (int)floorPos.y;
            fillMap[currX, currY] = fillerIndex;
            EncapsulateTile(currX, currY);

            if (currX + 1 < size && tileMap[currX + 1, currY] && fillMap[currX + 1, currY] != fillerIndex)
                toExplore.Enqueue(new Vector2(currX + 1, currY));

            if (currX - 1 >= 0 && tileMap[currX - 1, currY] && fillMap[currX - 1, currY] != fillerIndex)
                toExplore.Enqueue(new Vector2(currX - 1, currY));

            if ((currY + 1) < yEnd && tileMap[currX, currY + 1] && fillMap[currX, currY + 1] != fillerIndex)
                toExplore.Enqueue(new Vector2(currX, currY + 1));

            if ((currY - 1) >= yStart && tileMap[currX, currY - 1] && fillMap[currX, currY - 1] != fillerIndex)
                toExplore.Enqueue(new Vector2(currX, currY - 1));
          
            return true;
        }
        private void EncapsulateTile(int x, int y)
        {
            float roundingCorrection = 0.1f;
            Vector2 cellCentre = new Vector2(x + ProceduralPrefabs.CELL_WIDTH / 2f, y + ProceduralPrefabs.CELL_HEIGHT / 2f);
            Vector2 cellDimensions = new Vector2(ProceduralPrefabs.CELL_WIDTH - roundingCorrection, ProceduralPrefabs.CELL_HEIGHT - roundingCorrection);
            fillVertices.Add(cellCentre - cellDimensions / 2f);
            fillVertices.Add(cellCentre + cellDimensions / 2f);
            fillVertices.Add(new Vector2(cellCentre.x + cellDimensions.x / 2f, cellCentre.y - cellDimensions.y / 2f));
            fillVertices.Add(new Vector2(cellCentre.x - cellDimensions.x / 2f, cellCentre.y + cellDimensions.y / 2f));
        }
    }
}
