using UnityEngine;
public class ChunkData
{
    private const int FINAL_STAGE = 3;

    private bool[,] data;
    public bool[,] Data { get { return data; } }
    //later attributes go here as bool[,] arrays

    private TilesGenerator tilesGenerator;

    private bool upBoundary, rightBoundary, upRightCell;

    private int[,] marchingSquares;
    private int nextStage, goalStage;
    private int chunkX, chunkY, chunkSize;
    public bool isDataPrepared;


    public ChunkData(Vector2 chunkPos, TilesGenerator tilesGenerator)
    {
        this.tilesGenerator = tilesGenerator;

        chunkX = (int)chunkPos.x;
        chunkY = (int)chunkPos.y;
        chunkSize = EndlessCavern.CHUNK_SIZE;
        upBoundary = rightBoundary = upRightCell = false;

        marchingSquares = new int[chunkSize, chunkSize];
        nextStage = 0;
        goalStage = -1;
        isDataPrepared = false;
    }

    public void RequestData(int stage)
    {
        //This logic will change after the implementation of threading.
        //No while loop will be required, the next stage tiles will be requested in OnTilesRecieved function.

        goalStage = stage;

        while (goalStage >= nextStage)
        {
            if (nextStage != FINAL_STAGE)
                data = tilesGenerator.ExecuteGenerationStep(chunkX, chunkY, nextStage++);
            if (nextStage == FINAL_STAGE)
            {
                isDataPrepared = true;
                nextStage++;
                PrepareMidCells();
            }
        }
        ObtainNeighbors();
    }


    private void ObtainNeighbors()
    {
        if (!isDataPrepared)
            return;
        if (upBoundary && rightBoundary && upRightCell)
            return;
        if (!upBoundary)
        {
            bool[,] upChunk = MapDataSaver.instance.GetTiles(new Vector3(chunkX, chunkY + 1, 2));
            if (upChunk != null)
            {
                PrepareUpperCells(upChunk);
                upBoundary = true;
            }
        }
        if (!rightBoundary)
        {
            bool[,] rightChunk = MapDataSaver.instance.GetTiles(new Vector3(chunkX + 1, chunkY, 2));
            if (rightChunk != null)
            {
                PrepareRightCells(rightChunk);
                rightBoundary = true;
            }
        }
        if (!upRightCell && rightBoundary && upBoundary)
        {
            bool[,] upRightChunk = MapDataSaver.instance.GetTiles(new Vector3(chunkX + 1, chunkY + 1, 2));
            bool[,] rightChunk = MapDataSaver.instance.GetTiles(new Vector3(chunkX + 1, chunkY, 2));
            bool[,] upChunk = MapDataSaver.instance.GetTiles(new Vector3(chunkX, chunkY + 1, 2));
            if (upRightChunk != null)
            {
                PrepareUpRightCell(upRightChunk[0, 0], upChunk[chunkSize - 1, 0], rightChunk[0, chunkSize - 1]);
                upRightCell = true;
            }
        }
    }
    private void PrepareMidCells()
    {
        for (int y = 0; y < chunkSize - 1; y++)
        {
            for (int x = 0; x < chunkSize - 1; x++)
            {
                int binaryToDecimal = (data[x, y] ? 1 : 0) * 8 + (data[x + 1, y] ? 1 : 0) * 4 + (data[x + 1, y + 1] ? 1 : 0) * 2 + (data[x, y + 1] ? 1 : 0);

                marchingSquares[x, y] = binaryToDecimal;
            }
        }
    }
    private void PrepareUpperCells(bool[,] upChunk)
    {
        int y = chunkSize - 1;
        for (int x = 0; x < chunkSize - 1; x++)
        {
            int binaryToDecimal = (data[x, y] ? 1 : 0) * 8 + (data[x + 1, y] ? 1 : 0) * 4 + (upChunk[x + 1, 0] ? 1 : 0) * 2 + (upChunk[x, 0] ? 1 : 0);

            marchingSquares[x, y] = binaryToDecimal;
        }

    }
    private void PrepareRightCells(bool[,] rightChunk)
    {
        int x = chunkSize - 1;
        for (int y = 0; y < chunkSize - 1; y++)
        {
            int binaryToDecimal = (data[x, y] ? 1 : 0) * 8 + (rightChunk[0, y] ? 1 : 0) * 4 + (rightChunk[0, y + 1] ? 1 : 0) * 2 + (data[x, y + 1] ? 1 : 0);

            marchingSquares[x, y] = binaryToDecimal;
        }
    }
    private void PrepareUpRightCell(bool cornerTile, bool upTile, bool rightTile)
    {
        int x = chunkSize - 1;
        int y = chunkSize - 1;

        int binaryToDecimal = (data[x, y] ? 1 : 0) * 8 + (rightTile ? 1 : 0) * 4 + (cornerTile ? 1 : 0) * 2 + (upTile ? 1 : 0);

        marchingSquares[x, y] = binaryToDecimal;
    }
}
