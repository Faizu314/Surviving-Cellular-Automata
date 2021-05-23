using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessCavern : MonoBehaviour
{
    public const int CHUNK_SIZE = 25;
    public const int SEED = 0;

    [SerializeField] private Transform observer;

    public List<ProgressThreshold> progressThresholds;
    public int maxRange;

    private Dictionary<Vector2, MapChunk> nearbyChunks = new Dictionary<Vector2, MapChunk>();
    private TilesGenerator tilesGenerator;
    private ProceduralPrefabs proceduralPrefabs;

    private void Start()
    {
        tilesGenerator = gameObject.GetComponent<TilesGenerator>();
        proceduralPrefabs = gameObject.GetComponent<ProceduralPrefabs>();
    }

    private void Update()
    {
        int observerChunkX = ((int)observer.position.x / CHUNK_SIZE) - (observer.position.x < 0 ? 1 : 0);
        int observerChunkY = ((int)observer.position.y / CHUNK_SIZE) - (observer.position.y < 0 ? 1 : 0);
        Vector2 observerChunkPos = new Vector2(observerChunkX, observerChunkY);

        for (int yOffset = -maxRange; yOffset <= maxRange; yOffset++)
        {
            for (int xOffset = -maxRange; xOffset <= maxRange; xOffset++)
            {
                Vector2 currentChunkPos = new Vector2(observerChunkPos.x + xOffset, observerChunkPos.y + yOffset);

                if (nearbyChunks.ContainsKey(currentChunkPos))
                {
                    nearbyChunks[currentChunkPos].Update();
                }
                else
                {
                    //send coord to BiomeManager and get biome Type
                    //currently sending default biome type => 0
                    PrefabPackage chunkPrefabs = proceduralPrefabs.GetPrefabPackage(0);

                    nearbyChunks.Add(currentChunkPos, new MapChunk(currentChunkPos, tilesGenerator, chunkPrefabs, gameObject));
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
        private GameObject chunkObject;
        private GameObject[,] cellObjects;
        private PrefabPackage prefabs;
        private int chunkX, chunkY;
        private int chunkSize;
        private float cellWidth, cellHeight;
        private bool upBoundary, rightBoundary, upRightCell;

        private const int FINAL_STAGE = 2;
        private int nextStage, goalStage;
        private bool requestedNextStage;
        private bool fullyPrepared;

        public MapChunk(Vector2 chunkPos, TilesGenerator tilesGenerator, PrefabPackage prefabs, GameObject myObject)
        {
            this.prefabs = prefabs;
            this.tilesGenerator = tilesGenerator;

            chunkX = (int)chunkPos.x;
            chunkY = (int)chunkPos.y;
            chunkSize = CHUNK_SIZE;
            cellWidth = ProceduralPrefabs.CELL_WIDTH;
            cellHeight = ProceduralPrefabs.CELL_HEIGHT;

            chunkObject = new GameObject("Chunk: " + chunkX + ", " + chunkY);
            chunkObject.transform.parent = myObject.transform;
            cellObjects = new GameObject[chunkSize, chunkSize];
            debugData = new int[chunkSize, chunkSize];
            upBoundary = rightBoundary = upRightCell = false;

            nextStage = 0;
            goalStage = -1;
            requestedNextStage = fullyPrepared = false;
        }
        public void Update()
        {
            if (goalStage >= nextStage)
            {
                //Final stage is to create the cell objects and has nothing to do with tileMap generation
                if (!requestedNextStage && nextStage != FINAL_STAGE)
                    tiles = tilesGenerator.ExecuteGenerationStep(chunkX, chunkY, nextStage++);
                if (nextStage == FINAL_STAGE)
                {
                    RenderChunk();
                    fullyPrepared = true;
                    nextStage++;
                }
                return;
            }
            if (nextStage > FINAL_STAGE)
            {
                if (upBoundary && rightBoundary && upRightCell)
                    return;
                if (!upBoundary)
                {
                    bool[,] upChunk = MapDataSaver.instance.GetAutomataTiles(new Vector2(chunkX, chunkY + 1));
                    if (upChunk != null)
                    {
                        CreateUpperCells(upChunk);
                        upBoundary = true;
                    }
                }
                if (!rightBoundary)
                {
                    bool[,] rightChunk = MapDataSaver.instance.GetAutomataTiles(new Vector2(chunkX + 1, chunkY));
                    if (rightChunk != null)
                    {
                        CreateRightCells(rightChunk);
                        rightBoundary = true;
                    }
                }
                if (!upRightCell && rightBoundary && upBoundary)
                {
                    bool[,] upRightChunk = MapDataSaver.instance.GetAutomataTiles(new Vector2(chunkX + 1, chunkY + 1));
                    bool[,] rightChunk = MapDataSaver.instance.GetAutomataTiles(new Vector2(chunkX + 1, chunkY));
                    bool[,] upChunk = MapDataSaver.instance.GetAutomataTiles(new Vector2(chunkX, chunkY + 1));
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
            chunkObject.SetActive(false);
        }
        private void RenderChunk()
        {
            chunkObject.SetActive(true);
            CreateCells();
        }
        public void ObtainStage(int stage)
        {
            if (fullyPrepared)
                chunkObject.SetActive(true);
            goalStage = stage;
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
                    cellObjects[x, y] = new GameObject(x + ", " + y + ": Cell");
                    GameObject floorObj;
                    Vector3 basePosition = new Vector2((chunkX * chunkSize + x) * cellWidth, (chunkY * chunkSize + y) * cellHeight);
                    if (binaryToDecimal == 15)
                    {
                        floorObj = Instantiate(prefabs.fullFloorPrefab);
                        floorObj.transform.position = basePosition;
                        floorObj.transform.parent = cellObjects[x, y].transform;
                        cellObjects[x, y].transform.parent = chunkObject.transform;
                        continue;
                    }
                    GameObject wallObj;
                    Vector3 midCell = basePosition + new Vector3(cellWidth / 2f, cellHeight / 2f, 0);

                    switch (binaryToDecimal)
                    {
                        case 1:
                            wallObj = Instantiate(prefabs.cornerWallPrefab);
                            wallObj.name = "Corner Wall";
                            wallObj.transform.position = basePosition;
                            wallObj.transform.RotateAround(midCell, Vector3.back, -90f);
                            wallObj.transform.parent = cellObjects[x, y].transform;
                            break;
                        case 2:
                            wallObj = Instantiate(prefabs.cornerWallPrefab);
                            wallObj.name = "Corner Wall";
                            wallObj.transform.position = basePosition;
                            wallObj.transform.parent = cellObjects[x, y].transform;
                            break;
                        case 3:
                            wallObj = Instantiate(prefabs.rectangleWallPrefab);
                            wallObj.name = "Rectangle Wall";
                            wallObj.transform.position = basePosition;
                            wallObj.transform.parent = cellObjects[x, y].transform;
                            break;
                        case 4:
                            wallObj = Instantiate(prefabs.cornerWallPrefab);
                            wallObj.name = "Corner Wall";
                            wallObj.transform.position = basePosition;
                            wallObj.transform.RotateAround(midCell, Vector3.back, 90f);
                            wallObj.transform.parent = cellObjects[x, y].transform;
                            break;
                        case 5:
                            wallObj = Instantiate(prefabs.diagonalWallPrefab);
                            wallObj.name = "Diagonal Wall";
                            wallObj.transform.position = basePosition;
                            wallObj.transform.parent = cellObjects[x, y].transform;
                            break;
                        case 6:
                            wallObj = Instantiate(prefabs.rectangleWallPrefab);
                            wallObj.name = "Rectangle Wall";
                            wallObj.transform.position = basePosition;
                            wallObj.transform.RotateAround(midCell, Vector3.back, 90f);
                            wallObj.transform.parent = cellObjects[x, y].transform;
                            break;
                        case 7:
                            wallObj = Instantiate(prefabs.triangleWallPrefab);
                            wallObj.name = "Triangle Wall";
                            wallObj.transform.position = basePosition;
                            wallObj.transform.parent = cellObjects[x, y].transform;
                            break;
                        case 8:
                            wallObj = Instantiate(prefabs.cornerWallPrefab);
                            wallObj.name = "Corner Wall";
                            wallObj.transform.position = basePosition;
                            wallObj.transform.RotateAround(midCell, Vector3.back, 180f);
                            wallObj.transform.parent = cellObjects[x, y].transform;
                            break;
                        case 9:
                            wallObj = Instantiate(prefabs.rectangleWallPrefab);
                            wallObj.name = "Rectangle Wall";
                            wallObj.transform.position = basePosition;
                            wallObj.transform.RotateAround(midCell, Vector3.back, -90f);
                            wallObj.transform.parent = cellObjects[x, y].transform;
                            break;
                        case 10:
                            wallObj = Instantiate(prefabs.diagonalWallPrefab);
                            wallObj.name = "Diagonal Wall";
                            wallObj.transform.position = basePosition;
                            wallObj.transform.RotateAround(midCell, Vector3.back, 90f);
                            wallObj.transform.parent = cellObjects[x, y].transform;
                            break;
                        case 11:
                            wallObj = Instantiate(prefabs.triangleWallPrefab);
                            wallObj.name = "Triangle Wall";
                            wallObj.transform.position = basePosition;
                            wallObj.transform.RotateAround(midCell, Vector3.back, -90f);
                            wallObj.transform.parent = cellObjects[x, y].transform;
                            break;
                        case 12:
                            wallObj = Instantiate(prefabs.rectangleWallPrefab);
                            wallObj.name = "Rectangle Wall";
                            wallObj.transform.position = basePosition;
                            wallObj.transform.RotateAround(midCell, Vector3.back, 180f);
                            wallObj.transform.parent = cellObjects[x, y].transform;
                            break;
                        case 13:
                            wallObj = Instantiate(prefabs.triangleWallPrefab);
                            wallObj.name = "Triangle Wall";
                            wallObj.transform.position = basePosition;
                            wallObj.transform.RotateAround(midCell, Vector3.back, 180f);
                            wallObj.transform.parent = cellObjects[x, y].transform;
                            break;
                        case 14:
                            wallObj = Instantiate(prefabs.triangleWallPrefab);
                            wallObj.name = "Triangle Wall";
                            wallObj.transform.position = basePosition;
                            wallObj.transform.RotateAround(midCell, Vector3.back, 90f);
                            wallObj.transform.parent = cellObjects[x, y].transform;
                            break;
                    }

                    binaryToDecimal = 15 - binaryToDecimal;

                    switch (binaryToDecimal)
                    {
                        case 1:
                            floorObj = Instantiate(prefabs.cornerFloorPrefab);
                            floorObj.name = "Corner Floor";
                            floorObj.transform.position = basePosition;
                            floorObj.transform.RotateAround(midCell, Vector3.back, -90f);
                            floorObj.transform.parent = cellObjects[x, y].transform;
                            break;
                        case 2:
                            floorObj = Instantiate(prefabs.cornerFloorPrefab);
                            floorObj.name = "Corner Floor";
                            floorObj.transform.position = basePosition;
                            floorObj.transform.parent = cellObjects[x, y].transform;
                            break;
                        case 3:
                            floorObj = Instantiate(prefabs.rectangleFloorPrefab);
                            floorObj.name = "Rectangle Floor";
                            floorObj.transform.position = basePosition;
                            floorObj.transform.parent = cellObjects[x, y].transform;
                            break;
                        case 4:
                            floorObj = Instantiate(prefabs.cornerFloorPrefab);
                            floorObj.name = "Corner Floor";
                            floorObj.transform.position = basePosition;
                            floorObj.transform.RotateAround(midCell, Vector3.back, 90f);
                            floorObj.transform.parent = cellObjects[x, y].transform;
                            break;
                        case 5:
                            floorObj = Instantiate(prefabs.crossFloorPrefab);
                            floorObj.name = "Cross Floor";
                            floorObj.transform.position = basePosition;
                            floorObj.transform.RotateAround(midCell, Vector3.back, 90f);
                            floorObj.transform.parent = cellObjects[x, y].transform;
                            break;
                        case 6:
                            floorObj = Instantiate(prefabs.rectangleFloorPrefab);
                            floorObj.name = "Rectangle Floor";
                            floorObj.transform.position = basePosition;
                            floorObj.transform.RotateAround(midCell, Vector3.back, 90f);
                            floorObj.transform.parent = cellObjects[x, y].transform;
                            break;
                        case 7:
                            floorObj = Instantiate(prefabs.triangleFloorPrefab);
                            floorObj.name = "Triangle Floor";
                            floorObj.transform.position = basePosition;
                            floorObj.transform.parent = cellObjects[x, y].transform;
                            break;
                        case 8:
                            floorObj = Instantiate(prefabs.cornerFloorPrefab);
                            floorObj.name = "Corner Floor Case 8";
                            floorObj.transform.position = basePosition;
                            floorObj.transform.RotateAround(midCell, Vector3.back, 180f);
                            floorObj.transform.parent = cellObjects[x, y].transform;
                            break;
                        case 9:
                            floorObj = Instantiate(prefabs.rectangleFloorPrefab);
                            floorObj.name = "Rectangle Floor";
                            floorObj.transform.position = basePosition;
                            floorObj.transform.RotateAround(midCell, Vector3.back, -90f);
                            floorObj.transform.parent = cellObjects[x, y].transform;
                            break;
                        case 10:
                            floorObj = Instantiate(prefabs.crossFloorPrefab);
                            floorObj.name = "Cross Floor";
                            floorObj.transform.position = basePosition;
                            floorObj.transform.parent = cellObjects[x, y].transform;
                            break;
                        case 11:
                            floorObj = Instantiate(prefabs.triangleFloorPrefab);
                            floorObj.name = "Triangle Floor";
                            floorObj.transform.position = basePosition;
                            floorObj.transform.RotateAround(midCell, Vector3.back, -90f);
                            floorObj.transform.parent = cellObjects[x, y].transform;
                            break;
                        case 12:
                            floorObj = Instantiate(prefabs.rectangleFloorPrefab);
                            floorObj.name = "Rectangle Cell";
                            floorObj.transform.position = basePosition;
                            floorObj.transform.RotateAround(midCell, Vector3.back, 180f);
                            floorObj.transform.parent = cellObjects[x, y].transform;
                            break;
                        case 13:
                            floorObj = Instantiate(prefabs.triangleFloorPrefab);
                            floorObj.name = "Triangle Floor";
                            floorObj.transform.position = basePosition;
                            floorObj.transform.RotateAround(midCell, Vector3.back, 180f);
                            floorObj.transform.parent = cellObjects[x, y].transform;
                            break;
                        case 14:
                            floorObj = Instantiate(prefabs.triangleFloorPrefab);
                            floorObj.name = "Triangle Floor";
                            floorObj.transform.position = basePosition;
                            floorObj.transform.RotateAround(midCell, Vector3.back, 90f);
                            floorObj.transform.parent = cellObjects[x, y].transform;
                            break;
                    }

                    cellObjects[x, y].transform.parent = chunkObject.transform;
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
                cellObjects[x, y] = new GameObject(x + ", " + y + ": Cell");
                GameObject floorObj;
                Vector3 basePosition = new Vector2((chunkX * chunkSize + x) * cellWidth, (chunkY * chunkSize + y) * cellHeight);
                if (binaryToDecimal == 15)
                {
                    floorObj = Instantiate(prefabs.fullFloorPrefab);
                    floorObj.transform.position = basePosition;
                    floorObj.transform.parent = cellObjects[x, y].transform;
                    cellObjects[x, y].transform.parent = chunkObject.transform;
                    continue;
                }
                GameObject wallObj;
                Vector3 midCell = basePosition + new Vector3(cellWidth / 2f, cellHeight / 2f, 0);

                switch (binaryToDecimal)
                {
                    case 1:
                        wallObj = Instantiate(prefabs.cornerWallPrefab);
                        wallObj.name = "Corner Wall";
                        wallObj.transform.position = basePosition;
                        wallObj.transform.RotateAround(midCell, Vector3.back, -90f);
                        wallObj.transform.parent = cellObjects[x, y].transform;
                        break;
                    case 2:
                        wallObj = Instantiate(prefabs.cornerWallPrefab);
                        wallObj.name = "Corner Wall";
                        wallObj.transform.position = basePosition;
                        wallObj.transform.parent = cellObjects[x, y].transform;
                        break;
                    case 3:
                        wallObj = Instantiate(prefabs.rectangleWallPrefab);
                        wallObj.name = "Rectangle Wall";
                        wallObj.transform.position = basePosition;
                        wallObj.transform.parent = cellObjects[x, y].transform;
                        break;
                    case 4:
                        wallObj = Instantiate(prefabs.cornerWallPrefab);
                        wallObj.name = "Corner Wall";
                        wallObj.transform.position = basePosition;
                        wallObj.transform.RotateAround(midCell, Vector3.back, 90f);
                        wallObj.transform.parent = cellObjects[x, y].transform;
                        break;
                    case 5:
                        wallObj = Instantiate(prefabs.diagonalWallPrefab);
                        wallObj.name = "Diagonal Wall";
                        wallObj.transform.position = basePosition;
                        wallObj.transform.parent = cellObjects[x, y].transform;
                        break;
                    case 6:
                        wallObj = Instantiate(prefabs.rectangleWallPrefab);
                        wallObj.name = "Rectangle Wall";
                        wallObj.transform.position = basePosition;
                        wallObj.transform.RotateAround(midCell, Vector3.back, 90f);
                        wallObj.transform.parent = cellObjects[x, y].transform;
                        break;
                    case 7:
                        wallObj = Instantiate(prefabs.triangleWallPrefab);
                        wallObj.name = "Triangle Wall";
                        wallObj.transform.position = basePosition;
                        wallObj.transform.parent = cellObjects[x, y].transform;
                        break;
                    case 8:
                        wallObj = Instantiate(prefabs.cornerWallPrefab);
                        wallObj.name = "Corner Wall";
                        wallObj.transform.position = basePosition;
                        wallObj.transform.RotateAround(midCell, Vector3.back, 180f);
                        wallObj.transform.parent = cellObjects[x, y].transform;
                        break;
                    case 9:
                        wallObj = Instantiate(prefabs.rectangleWallPrefab);
                        wallObj.name = "Rectangle Wall";
                        wallObj.transform.position = basePosition;
                        wallObj.transform.RotateAround(midCell, Vector3.back, -90f);
                        wallObj.transform.parent = cellObjects[x, y].transform;
                        break;
                    case 10:
                        wallObj = Instantiate(prefabs.diagonalWallPrefab);
                        wallObj.name = "Diagonal Wall";
                        wallObj.transform.position = basePosition;
                        wallObj.transform.RotateAround(midCell, Vector3.back, 90f);
                        wallObj.transform.parent = cellObjects[x, y].transform;
                        break;
                    case 11:
                        wallObj = Instantiate(prefabs.triangleWallPrefab);
                        wallObj.name = "Triangle Wall";
                        wallObj.transform.position = basePosition;
                        wallObj.transform.RotateAround(midCell, Vector3.back, -90f);
                        wallObj.transform.parent = cellObjects[x, y].transform;
                        break;
                    case 12:
                        wallObj = Instantiate(prefabs.rectangleWallPrefab);
                        wallObj.name = "Rectangle Wall";
                        wallObj.transform.position = basePosition;
                        wallObj.transform.RotateAround(midCell, Vector3.back, 180f);
                        wallObj.transform.parent = cellObjects[x, y].transform;
                        break;
                    case 13:
                        wallObj = Instantiate(prefabs.triangleWallPrefab);
                        wallObj.name = "Triangle Wall";
                        wallObj.transform.position = basePosition;
                        wallObj.transform.RotateAround(midCell, Vector3.back, 180f);
                        wallObj.transform.parent = cellObjects[x, y].transform;
                        break;
                    case 14:
                        wallObj = Instantiate(prefabs.triangleWallPrefab);
                        wallObj.name = "Triangle Wall";
                        wallObj.transform.position = basePosition;
                        wallObj.transform.RotateAround(midCell, Vector3.back, 90f);
                        wallObj.transform.parent = cellObjects[x, y].transform;
                        break;
                }

                binaryToDecimal = 15 - binaryToDecimal;

                switch (binaryToDecimal)
                {
                    case 1:
                        floorObj = Instantiate(prefabs.cornerFloorPrefab);
                        floorObj.name = "Corner Floor";
                        floorObj.transform.position = basePosition;
                        floorObj.transform.RotateAround(midCell, Vector3.back, -90f);
                        floorObj.transform.parent = cellObjects[x, y].transform;
                        break;
                    case 2:
                        floorObj = Instantiate(prefabs.cornerFloorPrefab);
                        floorObj.name = "Corner Floor";
                        floorObj.transform.position = basePosition;
                        floorObj.transform.parent = cellObjects[x, y].transform;
                        break;
                    case 3:
                        floorObj = Instantiate(prefabs.rectangleFloorPrefab);
                        floorObj.name = "Rectangle Floor";
                        floorObj.transform.position = basePosition;
                        floorObj.transform.parent = cellObjects[x, y].transform;
                        break;
                    case 4:
                        floorObj = Instantiate(prefabs.cornerFloorPrefab);
                        floorObj.name = "Corner Floor";
                        floorObj.transform.position = basePosition;
                        floorObj.transform.RotateAround(midCell, Vector3.back, 90f);
                        floorObj.transform.parent = cellObjects[x, y].transform;
                        break;
                    case 5:
                        floorObj = Instantiate(prefabs.crossFloorPrefab);
                        floorObj.name = "Cross Floor";
                        floorObj.transform.position = basePosition;
                        floorObj.transform.RotateAround(midCell, Vector3.back, 90f);
                        floorObj.transform.parent = cellObjects[x, y].transform;
                        break;
                    case 6:
                        floorObj = Instantiate(prefabs.rectangleFloorPrefab);
                        floorObj.name = "Rectangle Floor";
                        floorObj.transform.position = basePosition;
                        floorObj.transform.RotateAround(midCell, Vector3.back, 90f);
                        floorObj.transform.parent = cellObjects[x, y].transform;
                        break;
                    case 7:
                        floorObj = Instantiate(prefabs.triangleFloorPrefab);
                        floorObj.name = "Triangle Floor";
                        floorObj.transform.position = basePosition;
                        floorObj.transform.parent = cellObjects[x, y].transform;
                        break;
                    case 8:
                        floorObj = Instantiate(prefabs.cornerFloorPrefab);
                        floorObj.name = "Corner Floor Case 8";
                        floorObj.transform.position = basePosition;
                        floorObj.transform.RotateAround(midCell, Vector3.back, 180f);
                        floorObj.transform.parent = cellObjects[x, y].transform;
                        break;
                    case 9:
                        floorObj = Instantiate(prefabs.rectangleFloorPrefab);
                        floorObj.name = "Rectangle Floor";
                        floorObj.transform.position = basePosition;
                        floorObj.transform.RotateAround(midCell, Vector3.back, -90f);
                        floorObj.transform.parent = cellObjects[x, y].transform;
                        break;
                    case 10:
                        floorObj = Instantiate(prefabs.crossFloorPrefab);
                        floorObj.name = "Cross Floor";
                        floorObj.transform.position = basePosition;
                        floorObj.transform.parent = cellObjects[x, y].transform;
                        break;
                    case 11:
                        floorObj = Instantiate(prefabs.triangleFloorPrefab);
                        floorObj.name = "Triangle Floor";
                        floorObj.transform.position = basePosition;
                        floorObj.transform.RotateAround(midCell, Vector3.back, -90f);
                        floorObj.transform.parent = cellObjects[x, y].transform;
                        break;
                    case 12:
                        floorObj = Instantiate(prefabs.rectangleFloorPrefab);
                        floorObj.name = "Rectangle Cell";
                        floorObj.transform.position = basePosition;
                        floorObj.transform.RotateAround(midCell, Vector3.back, 180f);
                        floorObj.transform.parent = cellObjects[x, y].transform;
                        break;
                    case 13:
                        floorObj = Instantiate(prefabs.triangleFloorPrefab);
                        floorObj.name = "Triangle Floor";
                        floorObj.transform.position = basePosition;
                        floorObj.transform.RotateAround(midCell, Vector3.back, 180f);
                        floorObj.transform.parent = cellObjects[x, y].transform;
                        break;
                    case 14:
                        floorObj = Instantiate(prefabs.triangleFloorPrefab);
                        floorObj.name = "Triangle Floor";
                        floorObj.transform.position = basePosition;
                        floorObj.transform.RotateAround(midCell, Vector3.back, 90f);
                        floorObj.transform.parent = cellObjects[x, y].transform;
                        break;
                }

                cellObjects[x, y].transform.parent = chunkObject.transform;
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
                cellObjects[x, y] = new GameObject(x + ", " + y + ": Cell");
                GameObject floorObj;
                Vector3 basePosition = new Vector2((chunkX * chunkSize + x) * cellWidth, (chunkY * chunkSize + y) * cellHeight);
                if (binaryToDecimal == 15)
                {
                    floorObj = Instantiate(prefabs.fullFloorPrefab);
                    floorObj.transform.position = basePosition;
                    floorObj.transform.parent = cellObjects[x, y].transform;
                    cellObjects[x, y].transform.parent = chunkObject.transform;
                    continue;
                }
                GameObject wallObj;
                Vector3 midCell = basePosition + new Vector3(cellWidth / 2f, cellHeight / 2f, 0);

                switch (binaryToDecimal)
                {
                    case 1:
                        wallObj = Instantiate(prefabs.cornerWallPrefab);
                        wallObj.name = "Corner Wall";
                        wallObj.transform.position = basePosition;
                        wallObj.transform.RotateAround(midCell, Vector3.back, -90f);
                        wallObj.transform.parent = cellObjects[x, y].transform;
                        break;
                    case 2:
                        wallObj = Instantiate(prefabs.cornerWallPrefab);
                        wallObj.name = "Corner Wall";
                        wallObj.transform.position = basePosition;
                        wallObj.transform.parent = cellObjects[x, y].transform;
                        break;
                    case 3:
                        wallObj = Instantiate(prefabs.rectangleWallPrefab);
                        wallObj.name = "Rectangle Wall";
                        wallObj.transform.position = basePosition;
                        wallObj.transform.parent = cellObjects[x, y].transform;
                        break;
                    case 4:
                        wallObj = Instantiate(prefabs.cornerWallPrefab);
                        wallObj.name = "Corner Wall";
                        wallObj.transform.position = basePosition;
                        wallObj.transform.RotateAround(midCell, Vector3.back, 90f);
                        wallObj.transform.parent = cellObjects[x, y].transform;
                        break;
                    case 5:
                        wallObj = Instantiate(prefabs.diagonalWallPrefab);
                        wallObj.name = "Diagonal Wall";
                        wallObj.transform.position = basePosition;
                        wallObj.transform.parent = cellObjects[x, y].transform;
                        break;
                    case 6:
                        wallObj = Instantiate(prefabs.rectangleWallPrefab);
                        wallObj.name = "Rectangle Wall";
                        wallObj.transform.position = basePosition;
                        wallObj.transform.RotateAround(midCell, Vector3.back, 90f);
                        wallObj.transform.parent = cellObjects[x, y].transform;
                        break;
                    case 7:
                        wallObj = Instantiate(prefabs.triangleWallPrefab);
                        wallObj.name = "Triangle Wall";
                        wallObj.transform.position = basePosition;
                        wallObj.transform.parent = cellObjects[x, y].transform;
                        break;
                    case 8:
                        wallObj = Instantiate(prefabs.cornerWallPrefab);
                        wallObj.name = "Corner Wall";
                        wallObj.transform.position = basePosition;
                        wallObj.transform.RotateAround(midCell, Vector3.back, 180f);
                        wallObj.transform.parent = cellObjects[x, y].transform;
                        break;
                    case 9:
                        wallObj = Instantiate(prefabs.rectangleWallPrefab);
                        wallObj.name = "Rectangle Wall";
                        wallObj.transform.position = basePosition;
                        wallObj.transform.RotateAround(midCell, Vector3.back, -90f);
                        wallObj.transform.parent = cellObjects[x, y].transform;
                        break;
                    case 10:
                        wallObj = Instantiate(prefabs.diagonalWallPrefab);
                        wallObj.name = "Diagonal Wall";
                        wallObj.transform.position = basePosition;
                        wallObj.transform.RotateAround(midCell, Vector3.back, 90f);
                        wallObj.transform.parent = cellObjects[x, y].transform;
                        break;
                    case 11:
                        wallObj = Instantiate(prefabs.triangleWallPrefab);
                        wallObj.name = "Triangle Wall";
                        wallObj.transform.position = basePosition;
                        wallObj.transform.RotateAround(midCell, Vector3.back, -90f);
                        wallObj.transform.parent = cellObjects[x, y].transform;
                        break;
                    case 12:
                        wallObj = Instantiate(prefabs.rectangleWallPrefab);
                        wallObj.name = "Rectangle Wall";
                        wallObj.transform.position = basePosition;
                        wallObj.transform.RotateAround(midCell, Vector3.back, 180f);
                        wallObj.transform.parent = cellObjects[x, y].transform;
                        break;
                    case 13:
                        wallObj = Instantiate(prefabs.triangleWallPrefab);
                        wallObj.name = "Triangle Wall";
                        wallObj.transform.position = basePosition;
                        wallObj.transform.RotateAround(midCell, Vector3.back, 180f);
                        wallObj.transform.parent = cellObjects[x, y].transform;
                        break;
                    case 14:
                        wallObj = Instantiate(prefabs.triangleWallPrefab);
                        wallObj.name = "Triangle Wall";
                        wallObj.transform.position = basePosition;
                        wallObj.transform.RotateAround(midCell, Vector3.back, 90f);
                        wallObj.transform.parent = cellObjects[x, y].transform;
                        break;
                }

                binaryToDecimal = 15 - binaryToDecimal;

                switch (binaryToDecimal)
                {
                    case 1:
                        floorObj = Instantiate(prefabs.cornerFloorPrefab);
                        floorObj.name = "Corner Floor";
                        floorObj.transform.position = basePosition;
                        floorObj.transform.RotateAround(midCell, Vector3.back, -90f);
                        floorObj.transform.parent = cellObjects[x, y].transform;
                        break;
                    case 2:
                        floorObj = Instantiate(prefabs.cornerFloorPrefab);
                        floorObj.name = "Corner Floor";
                        floorObj.transform.position = basePosition;
                        floorObj.transform.parent = cellObjects[x, y].transform;
                        break;
                    case 3:
                        floorObj = Instantiate(prefabs.rectangleFloorPrefab);
                        floorObj.name = "Rectangle Floor";
                        floorObj.transform.position = basePosition;
                        floorObj.transform.parent = cellObjects[x, y].transform;
                        break;
                    case 4:
                        floorObj = Instantiate(prefabs.cornerFloorPrefab);
                        floorObj.name = "Corner Floor";
                        floorObj.transform.position = basePosition;
                        floorObj.transform.RotateAround(midCell, Vector3.back, 90f);
                        floorObj.transform.parent = cellObjects[x, y].transform;
                        break;
                    case 5:
                        floorObj = Instantiate(prefabs.crossFloorPrefab);
                        floorObj.name = "Cross Floor";
                        floorObj.transform.position = basePosition;
                        floorObj.transform.RotateAround(midCell, Vector3.back, 90f);
                        floorObj.transform.parent = cellObjects[x, y].transform;
                        break;
                    case 6:
                        floorObj = Instantiate(prefabs.rectangleFloorPrefab);
                        floorObj.name = "Rectangle Floor";
                        floorObj.transform.position = basePosition;
                        floorObj.transform.RotateAround(midCell, Vector3.back, 90f);
                        floorObj.transform.parent = cellObjects[x, y].transform;
                        break;
                    case 7:
                        floorObj = Instantiate(prefabs.triangleFloorPrefab);
                        floorObj.name = "Triangle Floor";
                        floorObj.transform.position = basePosition;
                        floorObj.transform.parent = cellObjects[x, y].transform;
                        break;
                    case 8:
                        floorObj = Instantiate(prefabs.cornerFloorPrefab);
                        floorObj.name = "Corner Floor Case 8";
                        floorObj.transform.position = basePosition;
                        floorObj.transform.RotateAround(midCell, Vector3.back, 180f);
                        floorObj.transform.parent = cellObjects[x, y].transform;
                        break;
                    case 9:
                        floorObj = Instantiate(prefabs.rectangleFloorPrefab);
                        floorObj.name = "Rectangle Floor";
                        floorObj.transform.position = basePosition;
                        floorObj.transform.RotateAround(midCell, Vector3.back, -90f);
                        floorObj.transform.parent = cellObjects[x, y].transform;
                        break;
                    case 10:
                        floorObj = Instantiate(prefabs.crossFloorPrefab);
                        floorObj.name = "Cross Floor";
                        floorObj.transform.position = basePosition;
                        floorObj.transform.parent = cellObjects[x, y].transform;
                        break;
                    case 11:
                        floorObj = Instantiate(prefabs.triangleFloorPrefab);
                        floorObj.name = "Triangle Floor";
                        floorObj.transform.position = basePosition;
                        floorObj.transform.RotateAround(midCell, Vector3.back, -90f);
                        floorObj.transform.parent = cellObjects[x, y].transform;
                        break;
                    case 12:
                        floorObj = Instantiate(prefabs.rectangleFloorPrefab);
                        floorObj.name = "Rectangle Cell";
                        floorObj.transform.position = basePosition;
                        floorObj.transform.RotateAround(midCell, Vector3.back, 180f);
                        floorObj.transform.parent = cellObjects[x, y].transform;
                        break;
                    case 13:
                        floorObj = Instantiate(prefabs.triangleFloorPrefab);
                        floorObj.name = "Triangle Floor";
                        floorObj.transform.position = basePosition;
                        floorObj.transform.RotateAround(midCell, Vector3.back, 180f);
                        floorObj.transform.parent = cellObjects[x, y].transform;
                        break;
                    case 14:
                        floorObj = Instantiate(prefabs.triangleFloorPrefab);
                        floorObj.name = "Triangle Floor";
                        floorObj.transform.position = basePosition;
                        floorObj.transform.RotateAround(midCell, Vector3.back, 90f);
                        floorObj.transform.parent = cellObjects[x, y].transform;
                        break;
                }

                cellObjects[x, y].transform.parent = chunkObject.transform;
            }
        }
        private void CreateTopRightCell(bool cornerTile, bool upTile, bool rightTile)
        {
            int x = chunkSize - 1;
            int y = chunkSize - 1;

            int binaryToDecimal = (tiles[x, y] ? 1 : 0) * 8 + (rightTile ? 1 : 0) * 4 + (cornerTile ? 1 : 0) * 2 + (upTile ? 1 : 0);
            if (binaryToDecimal == 0)
                return;
            cellObjects[x, y] = new GameObject(x + ", " + y + ": Cell");
            GameObject floorObj;
            Vector3 basePosition = new Vector2((chunkX * chunkSize + x) * cellWidth, (chunkY * chunkSize + y) * cellHeight);
            if (binaryToDecimal == 15)
            {
                floorObj = Instantiate(prefabs.fullFloorPrefab);
                floorObj.transform.position = basePosition;
                floorObj.transform.parent = cellObjects[x, y].transform;
                cellObjects[x, y].transform.parent = chunkObject.transform;
                return;
            }
            GameObject wallObj;
            Vector3 midCell = basePosition + new Vector3(cellWidth / 2f, cellHeight / 2f, 0);

            switch (binaryToDecimal)
            {
                case 1:
                    wallObj = Instantiate(prefabs.cornerWallPrefab);
                    wallObj.name = "Corner Wall";
                    wallObj.transform.position = basePosition;
                    wallObj.transform.RotateAround(midCell, Vector3.back, -90f);
                    wallObj.transform.parent = cellObjects[x, y].transform;
                    break;
                case 2:
                    wallObj = Instantiate(prefabs.cornerWallPrefab);
                    wallObj.name = "Corner Wall";
                    wallObj.transform.position = basePosition;
                    wallObj.transform.parent = cellObjects[x, y].transform;
                    break;
                case 3:
                    wallObj = Instantiate(prefabs.rectangleWallPrefab);
                    wallObj.name = "Rectangle Wall";
                    wallObj.transform.position = basePosition;
                    wallObj.transform.parent = cellObjects[x, y].transform;
                    break;
                case 4:
                    wallObj = Instantiate(prefabs.cornerWallPrefab);
                    wallObj.name = "Corner Wall";
                    wallObj.transform.position = basePosition;
                    wallObj.transform.RotateAround(midCell, Vector3.back, 90f);
                    wallObj.transform.parent = cellObjects[x, y].transform;
                    break;
                case 5:
                    wallObj = Instantiate(prefabs.diagonalWallPrefab);
                    wallObj.name = "Diagonal Wall";
                    wallObj.transform.position = basePosition;
                    wallObj.transform.parent = cellObjects[x, y].transform;
                    break;
                case 6:
                    wallObj = Instantiate(prefabs.rectangleWallPrefab);
                    wallObj.name = "Rectangle Wall";
                    wallObj.transform.position = basePosition;
                    wallObj.transform.RotateAround(midCell, Vector3.back, 90f);
                    wallObj.transform.parent = cellObjects[x, y].transform;
                    break;
                case 7:
                    wallObj = Instantiate(prefabs.triangleWallPrefab);
                    wallObj.name = "Triangle Wall";
                    wallObj.transform.position = basePosition;
                    wallObj.transform.parent = cellObjects[x, y].transform;
                    break;
                case 8:
                    wallObj = Instantiate(prefabs.cornerWallPrefab);
                    wallObj.name = "Corner Wall";
                    wallObj.transform.position = basePosition;
                    wallObj.transform.RotateAround(midCell, Vector3.back, 180f);
                    wallObj.transform.parent = cellObjects[x, y].transform;
                    break;
                case 9:
                    wallObj = Instantiate(prefabs.rectangleWallPrefab);
                    wallObj.name = "Rectangle Wall";
                    wallObj.transform.position = basePosition;
                    wallObj.transform.RotateAround(midCell, Vector3.back, -90f);
                    wallObj.transform.parent = cellObjects[x, y].transform;
                    break;
                case 10:
                    wallObj = Instantiate(prefabs.diagonalWallPrefab);
                    wallObj.name = "Diagonal Wall";
                    wallObj.transform.position = basePosition;
                    wallObj.transform.RotateAround(midCell, Vector3.back, 90f);
                    wallObj.transform.parent = cellObjects[x, y].transform;
                    break;
                case 11:
                    wallObj = Instantiate(prefabs.triangleWallPrefab);
                    wallObj.name = "Triangle Wall";
                    wallObj.transform.position = basePosition;
                    wallObj.transform.RotateAround(midCell, Vector3.back, -90f);
                    wallObj.transform.parent = cellObjects[x, y].transform;
                    break;
                case 12:
                    wallObj = Instantiate(prefabs.rectangleWallPrefab);
                    wallObj.name = "Rectangle Wall";
                    wallObj.transform.position = basePosition;
                    wallObj.transform.RotateAround(midCell, Vector3.back, 180f);
                    wallObj.transform.parent = cellObjects[x, y].transform;
                    break;
                case 13:
                    wallObj = Instantiate(prefabs.triangleWallPrefab);
                    wallObj.name = "Triangle Wall";
                    wallObj.transform.position = basePosition;
                    wallObj.transform.RotateAround(midCell, Vector3.back, 180f);
                    wallObj.transform.parent = cellObjects[x, y].transform;
                    break;
                case 14:
                    wallObj = Instantiate(prefabs.triangleWallPrefab);
                    wallObj.name = "Triangle Wall";
                    wallObj.transform.position = basePosition;
                    wallObj.transform.RotateAround(midCell, Vector3.back, 90f);
                    wallObj.transform.parent = cellObjects[x, y].transform;
                    break;
            }

            binaryToDecimal = 15 - binaryToDecimal;

            switch (binaryToDecimal)
            {
                case 1:
                    floorObj = Instantiate(prefabs.cornerFloorPrefab);
                    floorObj.name = "Corner Floor";
                    floorObj.transform.position = basePosition;
                    floorObj.transform.RotateAround(midCell, Vector3.back, -90f);
                    floorObj.transform.parent = cellObjects[x, y].transform;
                    break;
                case 2:
                    floorObj = Instantiate(prefabs.cornerFloorPrefab);
                    floorObj.name = "Corner Floor";
                    floorObj.transform.position = basePosition;
                    floorObj.transform.parent = cellObjects[x, y].transform;
                    break;
                case 3:
                    floorObj = Instantiate(prefabs.rectangleFloorPrefab);
                    floorObj.name = "Rectangle Floor";
                    floorObj.transform.position = basePosition;
                    floorObj.transform.parent = cellObjects[x, y].transform;
                    break;
                case 4:
                    floorObj = Instantiate(prefabs.cornerFloorPrefab);
                    floorObj.name = "Corner Floor";
                    floorObj.transform.position = basePosition;
                    floorObj.transform.RotateAround(midCell, Vector3.back, 90f);
                    floorObj.transform.parent = cellObjects[x, y].transform;
                    break;
                case 5:
                    floorObj = Instantiate(prefabs.crossFloorPrefab);
                    floorObj.name = "Cross Floor";
                    floorObj.transform.position = basePosition;
                    floorObj.transform.RotateAround(midCell, Vector3.back, 90f);
                    floorObj.transform.parent = cellObjects[x, y].transform;
                    break;
                case 6:
                    floorObj = Instantiate(prefabs.rectangleFloorPrefab);
                    floorObj.name = "Rectangle Floor";
                    floorObj.transform.position = basePosition;
                    floorObj.transform.RotateAround(midCell, Vector3.back, 90f);
                    floorObj.transform.parent = cellObjects[x, y].transform;
                    break;
                case 7:
                    floorObj = Instantiate(prefabs.triangleFloorPrefab);
                    floorObj.name = "Triangle Floor";
                    floorObj.transform.position = basePosition;
                    floorObj.transform.parent = cellObjects[x, y].transform;
                    break;
                case 8:
                    floorObj = Instantiate(prefabs.cornerFloorPrefab);
                    floorObj.name = "Corner Floor Case 8";
                    floorObj.transform.position = basePosition;
                    floorObj.transform.RotateAround(midCell, Vector3.back, 180f);
                    floorObj.transform.parent = cellObjects[x, y].transform;
                    break;
                case 9:
                    floorObj = Instantiate(prefabs.rectangleFloorPrefab);
                    floorObj.name = "Rectangle Floor";
                    floorObj.transform.position = basePosition;
                    floorObj.transform.RotateAround(midCell, Vector3.back, -90f);
                    floorObj.transform.parent = cellObjects[x, y].transform;
                    break;
                case 10:
                    floorObj = Instantiate(prefabs.crossFloorPrefab);
                    floorObj.name = "Cross Floor";
                    floorObj.transform.position = basePosition;
                    floorObj.transform.parent = cellObjects[x, y].transform;
                    break;
                case 11:
                    floorObj = Instantiate(prefabs.triangleFloorPrefab);
                    floorObj.name = "Triangle Floor";
                    floorObj.transform.position = basePosition;
                    floorObj.transform.RotateAround(midCell, Vector3.back, -90f);
                    floorObj.transform.parent = cellObjects[x, y].transform;
                    break;
                case 12:
                    floorObj = Instantiate(prefabs.rectangleFloorPrefab);
                    floorObj.name = "Rectangle Cell";
                    floorObj.transform.position = basePosition;
                    floorObj.transform.RotateAround(midCell, Vector3.back, 180f);
                    floorObj.transform.parent = cellObjects[x, y].transform;
                    break;
                case 13:
                    floorObj = Instantiate(prefabs.triangleFloorPrefab);
                    floorObj.name = "Triangle Floor";
                    floorObj.transform.position = basePosition;
                    floorObj.transform.RotateAround(midCell, Vector3.back, 180f);
                    floorObj.transform.parent = cellObjects[x, y].transform;
                    break;
                case 14:
                    floorObj = Instantiate(prefabs.triangleFloorPrefab);
                    floorObj.name = "Triangle Floor";
                    floorObj.transform.position = basePosition;
                    floorObj.transform.RotateAround(midCell, Vector3.back, 90f);
                    floorObj.transform.parent = cellObjects[x, y].transform;
                    break;
            }

            cellObjects[x, y].transform.parent = chunkObject.transform;
        }
    }

    [System.Serializable]
    public struct ProgressThreshold
    {
        public string name;
        public int distance;
        public int stage;
    }

    //private void OnDrawGizmos()
    //{
    //    bool[,] tiles = null;
    //    if (loadedChunks.ContainsKey(Vector2.zero))
    //        tiles = loadedChunks[Vector2.zero].tiles;
    //    else
    //        return;

    //    Gizmos.color = Color.black;
    //    for (int y = 0; y < tiles.GetLength(1) - 1; y++)
    //    {
    //        for (int x = 0; x < tiles.GetLength(0) - 1; x++)
    //        {
    //            int binaryToDecimal = (tiles[x, y] ? 1 : 0) * 8 + (tiles[x + 1, y] ? 1 : 0) * 4 + (tiles[x + 1, y + 1] ? 1 : 0) * 2 + (tiles[x, y + 1] ? 1 : 0);
    //            Gizmos.color = tiles[x, y] ? Color.white : Color.black;
    //            Gizmos.DrawSphere(new Vector2(x, y), 0.1f);
    //            Gizmos.color = Color.red;
    //            switch(binaryToDecimal)
    //            {
    //                case 1:
    //                    Gizmos.DrawLine(new Vector2(x + 0.5f, y + 1), new Vector2(x, y + 0.5f));
    //                    break;
    //                case 2:
    //                    Gizmos.DrawLine(new Vector2(x + 0.5f, y + 1), new Vector2(x + 1, y + 0.5f));
    //                    break;
    //                case 3:
    //                    Gizmos.DrawLine(new Vector2(x, y + 0.5f), new Vector2(x + 1, y + 0.5f));
    //                    break;
    //                case 4:
    //                    Gizmos.DrawLine(new Vector2(x + 0.5f, y), new Vector2(x + 1, y + 0.5f));
    //                    break;
    //                case 5:
    //                    Gizmos.DrawLine(new Vector2(x, y + 0.5f), new Vector2(x + 0.5f, y + 1));
    //                    Gizmos.DrawLine(new Vector2(x + 0.5f, y), new Vector2(x + 1, y + 0.5f));
    //                    break;
    //                case 6:
    //                    Gizmos.DrawLine(new Vector2(x + 0.5f, y), new Vector2(x + 0.5f, y + 1));
    //                    break;
    //                case 7:
    //                    Gizmos.DrawLine(new Vector2(x, y + 0.5f), new Vector2(x + 0.5f, y));
    //                    break;
    //                case 8:
    //                    Gizmos.DrawLine(new Vector2(x, y + 0.5f), new Vector2(x + 0.5f, y));
    //                    break;
    //                case 9:
    //                    Gizmos.DrawLine(new Vector2(x + 0.5f, y), new Vector2(x + 0.5f, y + 1));
    //                    break;
    //                case 10:
    //                    Gizmos.DrawLine(new Vector2(x, y + 0.5f), new Vector2(x + 0.5f, y));
    //                    Gizmos.DrawLine(new Vector2(x + 0.5f, y + 1), new Vector2(x + 1, y + 0.5f));
    //                    break;
    //                case 11:
    //                    Gizmos.DrawLine(new Vector2(x + 1, y + 0.5f), new Vector2(x + 0.5f, y));
    //                    break;
    //                case 12:
    //                    Gizmos.DrawLine(new Vector2(x, y + 0.5f), new Vector2(x + 1, y + 0.5f));
    //                    break;
    //                case 13:
    //                    Gizmos.DrawLine(new Vector2(x + 0.5f, y + 1), new Vector2(x + 1, y + 0.5f));
    //                    break;
    //                case 14:
    //                    Gizmos.DrawLine(new Vector2(x, y + 0.5f), new Vector2(x + 0.5f, y + 1));
    //                    break;
    //            }
    //        }
    //        Gizmos.color = tiles[tiles.GetLength(0) - 1, y] ? Color.white : Color.black;
    //        Gizmos.DrawSphere(new Vector2(tiles.GetLength(0) - 1, y), 0.1f);
    //    }

    //}
}
