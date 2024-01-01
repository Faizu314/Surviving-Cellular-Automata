using System;

public static class CellularAutomata
{
    private static float INITIAL_TRUE_CELL_PROBABILITY;
    private static bool EDGE_CELL;
    private static int BIRTH_RATE;
    private static int DEATH_RATE;
    private static int CHUNK_SIZE;

    public static void SetInitialCellProbability(float initialCellProbability)
    {
        INITIAL_TRUE_CELL_PROBABILITY = initialCellProbability;
    }
    public static void SetAutomataTemplate(bool edgeCell, int birthRate, int deathRate)
    {
        EDGE_CELL = edgeCell;
        BIRTH_RATE = birthRate;
        DEATH_RATE = deathRate;
    }
    public static void SetChunkSize(int chunkSize)
    {
        CHUNK_SIZE = chunkSize;
    }
    public static bool[,] GetInitialMap(int chunkSeed)
    {
        Random prng = new Random(chunkSeed);
        int chunkSize = CHUNK_SIZE;

        bool[,] initialTiles = new bool[chunkSize, chunkSize];

        for (int y = 0; y < chunkSize; y++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                initialTiles[x, y] = prng.Next(-100000, 100000) / 100000f < INITIAL_TRUE_CELL_PROBABILITY;
            }
        }

        return initialTiles;
    }
    public static void TopCellularAutomata(bool[,] iterativeUpTiles, bool[,] initialUpTiles, bool[,] initialMainTiles, bool[,] initialLeftTiles, bool[,] initialRightTiles)
    {
        int chunkSize = CHUNK_SIZE;

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
    public static void LeftCellularAutomata(bool[,] iterativeLeftTiles, bool[,] initialLeftTiles, bool[,] initialUpTiles, bool[,] initialMainTiles, bool[,] initialDownTiles)
    {
        int chunkSize = CHUNK_SIZE;

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
    public static void MainCellularAutomata(bool[,] iterativeMainTiles, bool[,] initialMainTiles, bool[,] initialUpTiles, bool[,] initialLeftTiles, bool[,] initialRightTiles, bool[,] initialDownTiles)
    {
        int chunkSize = CHUNK_SIZE;

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
    public static void RightCellularAutomata(bool[,] iterativeRightTiles, bool[,] initialRightTiles, bool[,] initialUpTiles, bool[,] initialMainTiles, bool[,] initialDownTiles)
    {
        int chunkSize = CHUNK_SIZE;

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
    public static void DownCellularAutomata(bool[,] iterativeDownTiles, bool[,] initialDownTiles, bool[,] initialLeftTiles, bool[,] initialMainTiles, bool[,] initialRightTiles)
    {
        int chunkSize = CHUNK_SIZE;

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
}