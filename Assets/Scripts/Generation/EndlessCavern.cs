using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessCavern : MonoBehaviour
{
    public const int CHUNK_SIZE = 27;
    public const int SEED = 0;

    [SerializeField] private Transform observer;

    public List<ProgressThreshold> progressThresholds;
    public int maxRange;

    private Dictionary<Vector2, MapChunk> nearbyChunks = new Dictionary<Vector2, MapChunk>();
    private Vector3 previousObserverPos;
    private TilesGenerator tilesGenerator;
    private ProceduralPrefabs proceduralPrefabs;

    private void Start()
    {
        tilesGenerator = gameObject.GetComponent<TilesGenerator>();
        proceduralPrefabs = gameObject.GetComponent<ProceduralPrefabs>();
        previousObserverPos = observer.position + Vector3.one * 4;
    }

    private void Update()
    {
        if (Vector2.SqrMagnitude(previousObserverPos - observer.position) < 2)
            return;
        previousObserverPos = observer.position;
        int observerChunkX = ((int)observer.position.x / CHUNK_SIZE) - (observer.position.x < 0 ? 1 : 0);
        int observerChunkY = ((int)observer.position.y / CHUNK_SIZE) - (observer.position.y < 0 ? 1 : 0);
        Vector2 observerChunkPos = new Vector2(observerChunkX, observerChunkY);

        for (int yOffset = maxRange; yOffset >= -maxRange; yOffset--)
        {
            for (int xOffset = maxRange; xOffset >= -maxRange; xOffset--)
            {
                Vector2 currentChunkPos = new Vector2(observerChunkPos.x + xOffset, observerChunkPos.y + yOffset);

                if (nearbyChunks.ContainsKey(currentChunkPos))
                {
                    nearbyChunks[currentChunkPos].Update();
                }
                else
                {
                    nearbyChunks.Add(currentChunkPos, new MapChunk(currentChunkPos, tilesGenerator, proceduralPrefabs));
                }

                float currentPosX = (currentChunkPos.x + 0.5f) * CHUNK_SIZE;
                float currentPosY = (currentChunkPos.y + 0.5f) * CHUNK_SIZE;
                Vector3 currentPos = new Vector3(currentPosX, currentPosY);

                Bounds currentChunkPerimeter = new Bounds(currentPos, Vector2.one * CHUNK_SIZE);
                float currentChunkMinSqrDistance = currentChunkPerimeter.SqrDistance(observer.position);

                bool isNearby = false;
                for (int i = progressThresholds.Count - 1; i >= 0; i--)
                {
                    if (currentChunkMinSqrDistance < Mathf.Pow(progressThresholds[i].distance, 2))
                    {
                        nearbyChunks[currentChunkPos].ObtainStage(progressThresholds[i].stage);
                        isNearby = true;
                        break;
                    }
                }
                if (!isNearby)
                {
                    nearbyChunks[currentChunkPos].Deactivate();
                }
            }
        }
    }

    private class MapChunk
    {
        public bool[,] tiles;
        //later attributes go here as bool[,] arrays
        public int[,] debugData;

        private TilesGenerator tilesGenerator;
        private ProceduralPrefabs prefabs;
        private int chunkX, chunkY;
        private int chunkSize;
        private float cellWidth, cellHeight;
        private bool upBoundary, rightBoundary, upRightCell;

        private const int FINAL_STAGE = 3;
        private int nextStage, goalStage;
        private bool hasSubscribed;
        private bool requestedNextStage;
        private bool tilesPrepared;

        public MapChunk(Vector2 chunkPos, TilesGenerator tilesGenerator, ProceduralPrefabs proceduralPrefabs)
        {
            this.tilesGenerator = tilesGenerator;
            prefabs = proceduralPrefabs;

            chunkX = (int)chunkPos.x;
            chunkY = (int)chunkPos.y;
            chunkSize = CHUNK_SIZE;
            cellWidth = ProceduralPrefabs.CELL_WIDTH;
            cellHeight = ProceduralPrefabs.CELL_HEIGHT;

            debugData = new int[chunkSize, chunkSize];
            upBoundary = rightBoundary = upRightCell = false;

            nextStage = 0;
            goalStage = -1;
            requestedNextStage = tilesPrepared = false;
        }
        public void Update()
        {
            //if (goalStage >= nextStage)
            while(goalStage >= nextStage)
            {
                //Final stage is to create the cell objects and has nothing to do with tileMap generation
                if (!requestedNextStage && nextStage != FINAL_STAGE)
                    tiles = tilesGenerator.ExecuteGenerationStep(chunkX, chunkY, nextStage++);
                if (nextStage == FINAL_STAGE)
                {
                    RenderChunk();
                    tilesPrepared = true;
                    nextStage++;
                }
                //return;
            }
            if (hasSubscribed)
            {
                if (upBoundary && rightBoundary && upRightCell)
                    return;
                if (!upBoundary)
                {
                    bool[,] upChunk = MapDataSaver.instance.GetCorrectedTiles(new Vector2(chunkX, chunkY + 1));
                    if (upChunk != null)
                    {
                        CreateUpperCells(upChunk);
                        upBoundary = true;
                    }
                }
                if (!rightBoundary)
                {
                    bool[,] rightChunk = MapDataSaver.instance.GetCorrectedTiles(new Vector2(chunkX + 1, chunkY));
                    if (rightChunk != null)
                    {
                        CreateRightCells(rightChunk);
                        rightBoundary = true;
                    }
                }
                if (!upRightCell && rightBoundary && upBoundary)
                {
                    bool[,] upRightChunk = MapDataSaver.instance.GetCorrectedTiles(new Vector2(chunkX + 1, chunkY + 1));
                    bool[,] rightChunk = MapDataSaver.instance.GetCorrectedTiles(new Vector2(chunkX + 1, chunkY));
                    bool[,] upChunk = MapDataSaver.instance.GetCorrectedTiles(new Vector2(chunkX, chunkY + 1));
                    if (upRightChunk != null)
                    {
                        CreateTopRightCell(upRightChunk[0, 0], upChunk[chunkSize - 1, 0], rightChunk[0, chunkSize - 1]);
                        upRightCell = true;
                    }
                }
            }
        }
        public void Deactivate()
        {
            if (hasSubscribed)
            {
                prefabs.UnSubscribe(chunkX, chunkY);
                hasSubscribed = false;
                upBoundary = rightBoundary = upRightCell = false;
            }
        }
        public void ObtainStage(int stage)
        {
            if (tilesPrepared && stage == FINAL_STAGE)
                RenderChunk();
            goalStage = stage;
            Update();
        }

        private void RenderChunk()
        {
            if (!hasSubscribed)
            {
                prefabs.GetPackageSubscription(chunkX, chunkY);
                hasSubscribed = true;
                CreateCells();
            }
        }
        private void CreateCells()
        {
            for (int y = 0; y < chunkSize - 1; y++)
            {
                for (int x = 0; x < chunkSize - 1; x++)
                {
                    int binaryToDecimal = (tiles[x, y] ? 1 : 0) * 8 + (tiles[x + 1, y] ? 1 : 0) * 4 + (tiles[x + 1, y + 1] ? 1 : 0) * 2 + (tiles[x, y + 1] ? 1 : 0);
                    if (binaryToDecimal == 0)
                        continue;
                    GameObject floorObj = null;
                    Vector3 basePosition = new Vector2((chunkX * chunkSize + x) * cellWidth, (chunkY * chunkSize + y) * cellHeight);
                    if (binaryToDecimal == 15)
                    {
                        floorObj = prefabs.GetPrefab(chunkX, chunkY, ProceduralPrefabs.PrefabType.fullFloor);
                        floorObj.transform.position = basePosition;
                        continue;
                    }
                    GameObject wallObj = null;
                    Vector3 midCell = basePosition + new Vector3(cellWidth / 2f, cellHeight / 2f, 0);

                    MarchingSquareWall(binaryToDecimal, wallObj, chunkX, chunkY, ref basePosition, ref midCell);

                    binaryToDecimal = 15 - binaryToDecimal;

                    MarchingSquareFloor(binaryToDecimal, floorObj, chunkX, chunkY, ref basePosition, ref midCell);
                }
            }
        }
        private void CreateUpperCells(bool[,] upChunk)
        {
            int y = chunkSize - 1;
            for (int x = 0; x < chunkSize - 1; x++)
            {
                int binaryToDecimal = (tiles[x, y] ? 1 : 0) * 8 + (tiles[x + 1, y] ? 1 : 0) * 4 + (upChunk[x + 1, 0] ? 1 : 0) * 2 + (upChunk[x, 0] ? 1 : 0);
                if (binaryToDecimal == 0)
                    continue;
                GameObject floorObj = null;
                Vector3 basePosition = new Vector2((chunkX * chunkSize + x) * cellWidth, (chunkY * chunkSize + y) * cellHeight);
                if (binaryToDecimal == 15)
                {
                    floorObj = prefabs.GetPrefab(chunkX, chunkY, ProceduralPrefabs.PrefabType.fullFloor);
                    floorObj.transform.position = basePosition;
                    continue;
                }
                GameObject wallObj = null;
                Vector3 midCell = basePosition + new Vector3(cellWidth / 2f, cellHeight / 2f, 0);

                MarchingSquareWall(binaryToDecimal, wallObj, chunkX, chunkY, ref basePosition, ref midCell);

                binaryToDecimal = 15 - binaryToDecimal;

                MarchingSquareFloor(binaryToDecimal, floorObj, chunkX, chunkY, ref basePosition, ref midCell);
            }

        }
        private void CreateRightCells(bool[,] rightChunk)
        {
            int x = chunkSize - 1;
            for (int y = 0; y < chunkSize - 1; y++)
            {
                int binaryToDecimal = (tiles[x, y] ? 1 : 0) * 8 + (rightChunk[0, y] ? 1 : 0) * 4 + (rightChunk[0, y + 1] ? 1 : 0) * 2 + (tiles[x, y + 1] ? 1 : 0);
                if (binaryToDecimal == 0)
                    continue;
                GameObject floorObj = null;
                Vector3 basePosition = new Vector2((chunkX * chunkSize + x) * cellWidth, (chunkY * chunkSize + y) * cellHeight);
                if (binaryToDecimal == 15)
                {
                    floorObj = prefabs.GetPrefab(chunkX, chunkY, ProceduralPrefabs.PrefabType.fullFloor);
                    floorObj.transform.position = basePosition;
                    continue;
                }
                GameObject wallObj = null;
                Vector3 midCell = basePosition + new Vector3(cellWidth / 2f, cellHeight / 2f, 0);

                MarchingSquareWall(binaryToDecimal, wallObj, chunkX, chunkY, ref basePosition, ref midCell);

                binaryToDecimal = 15 - binaryToDecimal;

                MarchingSquareFloor(binaryToDecimal, floorObj, chunkX, chunkY, ref basePosition, ref midCell);
            }
        }
        private void CreateTopRightCell(bool cornerTile, bool upTile, bool rightTile)
        {
            int x = chunkSize - 1;
            int y = chunkSize - 1;

            int binaryToDecimal = (tiles[x, y] ? 1 : 0) * 8 + (rightTile ? 1 : 0) * 4 + (cornerTile ? 1 : 0) * 2 + (upTile ? 1 : 0);
            if (binaryToDecimal == 0)
                return;
            GameObject floorObj = null;
            Vector3 basePosition = new Vector2((chunkX * chunkSize + x) * cellWidth, (chunkY * chunkSize + y) * cellHeight);
            if (binaryToDecimal == 15)
            {
                floorObj = prefabs.GetPrefab(chunkX, chunkY, ProceduralPrefabs.PrefabType.fullFloor);
                floorObj.transform.position = basePosition;
                return;
            }
            GameObject wallObj = null;
            Vector3 midCell = basePosition + new Vector3(cellWidth / 2f, cellHeight / 2f, 0);

            MarchingSquareWall(binaryToDecimal, wallObj, chunkX, chunkY, ref basePosition, ref midCell);

            binaryToDecimal = 15 - binaryToDecimal;

            MarchingSquareFloor(binaryToDecimal, wallObj, chunkX, chunkY, ref basePosition, ref midCell);
        }

        private void MarchingSquareWall(int code, GameObject wallObj, int chunkX, int chunkY, ref Vector3 basePosition, ref Vector3 midCell)
        {
            switch (code)
            {
                case 1:
                    wallObj = prefabs.GetPrefab(chunkX, chunkY, ProceduralPrefabs.PrefabType.cornerWall);
                    wallObj.transform.SetPositionAndRotation(basePosition, Quaternion.identity);
                    wallObj.transform.RotateAround(midCell, Vector3.back, -90f);
                    break;
                case 2:
                    wallObj = prefabs.GetPrefab(chunkX, chunkY, ProceduralPrefabs.PrefabType.cornerWall);
                    wallObj.transform.SetPositionAndRotation(basePosition, Quaternion.identity);
                    break;
                case 3:
                    wallObj = prefabs.GetPrefab(chunkX, chunkY, ProceduralPrefabs.PrefabType.rectangleWall);
                    wallObj.transform.SetPositionAndRotation(basePosition, Quaternion.identity);
                    break;
                case 4:
                    wallObj = prefabs.GetPrefab(chunkX, chunkY, ProceduralPrefabs.PrefabType.cornerWall);
                    wallObj.transform.SetPositionAndRotation(basePosition, Quaternion.identity);
                    wallObj.transform.RotateAround(midCell, Vector3.back, 90f);
                    break;
                case 5:
                    wallObj = prefabs.GetPrefab(chunkX, chunkY, ProceduralPrefabs.PrefabType.diagonalWall);
                    wallObj.transform.SetPositionAndRotation(basePosition, Quaternion.identity);
                    break;
                case 6:
                    wallObj = prefabs.GetPrefab(chunkX, chunkY, ProceduralPrefabs.PrefabType.rectangleWall);
                    wallObj.transform.SetPositionAndRotation(basePosition, Quaternion.identity);
                    wallObj.transform.RotateAround(midCell, Vector3.back, 90f);
                    break;
                case 7:
                    wallObj = prefabs.GetPrefab(chunkX, chunkY, ProceduralPrefabs.PrefabType.triangleWall);
                    wallObj.transform.SetPositionAndRotation(basePosition, Quaternion.identity);
                    break;
                case 8:
                    wallObj = prefabs.GetPrefab(chunkX, chunkY, ProceduralPrefabs.PrefabType.cornerWall);
                    wallObj.transform.SetPositionAndRotation(basePosition, Quaternion.identity);
                    wallObj.transform.RotateAround(midCell, Vector3.back, 180f);
                    break;
                case 9:
                    wallObj = prefabs.GetPrefab(chunkX, chunkY, ProceduralPrefabs.PrefabType.rectangleWall);
                    wallObj.transform.SetPositionAndRotation(basePosition, Quaternion.identity);
                    wallObj.transform.RotateAround(midCell, Vector3.back, -90f);
                    break;
                case 10:
                    wallObj = prefabs.GetPrefab(chunkX, chunkY, ProceduralPrefabs.PrefabType.diagonalWall);
                    wallObj.transform.SetPositionAndRotation(basePosition, Quaternion.identity);
                    wallObj.transform.RotateAround(midCell, Vector3.back, 90f);
                    break;
                case 11:
                    wallObj = prefabs.GetPrefab(chunkX, chunkY, ProceduralPrefabs.PrefabType.triangleWall);
                    wallObj.transform.SetPositionAndRotation(basePosition, Quaternion.identity);
                    wallObj.transform.RotateAround(midCell, Vector3.back, -90f);
                    break;
                case 12:
                    wallObj = prefabs.GetPrefab(chunkX, chunkY, ProceduralPrefabs.PrefabType.rectangleWall);
                    wallObj.transform.SetPositionAndRotation(basePosition, Quaternion.identity);
                    wallObj.transform.RotateAround(midCell, Vector3.back, 180f);
                    break;
                case 13:
                    wallObj = prefabs.GetPrefab(chunkX, chunkY, ProceduralPrefabs.PrefabType.triangleWall);
                    wallObj.transform.SetPositionAndRotation(basePosition, Quaternion.identity);
                    wallObj.transform.RotateAround(midCell, Vector3.back, 180f);
                    break;
                case 14:
                    wallObj = prefabs.GetPrefab(chunkX, chunkY, ProceduralPrefabs.PrefabType.triangleWall);
                    wallObj.transform.SetPositionAndRotation(basePosition, Quaternion.identity);
                    wallObj.transform.RotateAround(midCell, Vector3.back, 90f);
                    break;
            }
        }
        private void MarchingSquareFloor(int code, GameObject floorObj, int chunkX, int chunkY, ref Vector3 basePosition, ref Vector3 midCell)
        {
            switch (code)
            {
                case 1:
                    floorObj = prefabs.GetPrefab(chunkX, chunkY, ProceduralPrefabs.PrefabType.cornerFloor);
                    floorObj.transform.SetPositionAndRotation(basePosition, Quaternion.identity);
                    floorObj.transform.RotateAround(midCell, Vector3.back, -90f);
                    break;
                case 2:
                    floorObj = prefabs.GetPrefab(chunkX, chunkY, ProceduralPrefabs.PrefabType.cornerFloor);
                    floorObj.transform.SetPositionAndRotation(basePosition, Quaternion.identity);
                    break;
                case 3:
                    floorObj = prefabs.GetPrefab(chunkX, chunkY, ProceduralPrefabs.PrefabType.rectangleFloor);
                    floorObj.transform.SetPositionAndRotation(basePosition, Quaternion.identity);
                    break;
                case 4:
                    floorObj = prefabs.GetPrefab(chunkX, chunkY, ProceduralPrefabs.PrefabType.cornerFloor);
                    floorObj.transform.SetPositionAndRotation(basePosition, Quaternion.identity);
                    floorObj.transform.RotateAround(midCell, Vector3.back, 90f);
                    break;
                case 5:
                    floorObj = prefabs.GetPrefab(chunkX, chunkY, ProceduralPrefabs.PrefabType.crossFloor);
                    floorObj.transform.SetPositionAndRotation(basePosition, Quaternion.identity);
                    floorObj.transform.RotateAround(midCell, Vector3.back, 90f);
                    break;
                case 6:
                    floorObj = prefabs.GetPrefab(chunkX, chunkY, ProceduralPrefabs.PrefabType.rectangleFloor);
                    floorObj.transform.SetPositionAndRotation(basePosition, Quaternion.identity);
                    floorObj.transform.RotateAround(midCell, Vector3.back, 90f);
                    break;
                case 7:
                    floorObj = prefabs.GetPrefab(chunkX, chunkY, ProceduralPrefabs.PrefabType.triangleFloor);
                    floorObj.transform.SetPositionAndRotation(basePosition, Quaternion.identity);
                    break;
                case 8:
                    floorObj = prefabs.GetPrefab(chunkX, chunkY, ProceduralPrefabs.PrefabType.cornerFloor);
                    floorObj.transform.SetPositionAndRotation(basePosition, Quaternion.identity);
                    floorObj.transform.RotateAround(midCell, Vector3.back, 180f);
                    break;
                case 9:
                    floorObj = prefabs.GetPrefab(chunkX, chunkY, ProceduralPrefabs.PrefabType.rectangleFloor);
                    floorObj.transform.SetPositionAndRotation(basePosition, Quaternion.identity);
                    floorObj.transform.RotateAround(midCell, Vector3.back, -90f);
                    break;
                case 10:
                    floorObj = prefabs.GetPrefab(chunkX, chunkY, ProceduralPrefabs.PrefabType.crossFloor);
                    floorObj.transform.SetPositionAndRotation(basePosition, Quaternion.identity);
                    break;
                case 11:
                    floorObj = prefabs.GetPrefab(chunkX, chunkY, ProceduralPrefabs.PrefabType.triangleFloor);
                    floorObj.transform.SetPositionAndRotation(basePosition, Quaternion.identity);
                    floorObj.transform.RotateAround(midCell, Vector3.back, -90f);
                    break;
                case 12:
                    floorObj = prefabs.GetPrefab(chunkX, chunkY, ProceduralPrefabs.PrefabType.rectangleFloor);
                    floorObj.transform.SetPositionAndRotation(basePosition, Quaternion.identity);
                    floorObj.transform.RotateAround(midCell, Vector3.back, 180f);
                    break;
                case 13:
                    floorObj = prefabs.GetPrefab(chunkX, chunkY, ProceduralPrefabs.PrefabType.triangleFloor);
                    floorObj.transform.SetPositionAndRotation(basePosition, Quaternion.identity);
                    floorObj.transform.RotateAround(midCell, Vector3.back, 180f);
                    break;
                case 14:
                    floorObj = prefabs.GetPrefab(chunkX, chunkY, ProceduralPrefabs.PrefabType.triangleFloor);
                    floorObj.transform.SetPositionAndRotation(basePosition, Quaternion.identity);
                    floorObj.transform.RotateAround(midCell, Vector3.back, 90f);
                    break;
            }
        }
    }

    [System.Serializable]
    public struct ProgressThreshold
    {
        public string name;
        public int distance;
        public int stage;
    }
}
