using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class EndlessCavern : MonoBehaviour
{
    public static int CHUNK_SIZE;
    public static int SEED;
    public static int RENDER_WINDOW_WIDTH = 13;
    public static int RENDER_WINDOW_HEIGHT = 11;

    [SerializeField] private Transform observer;

    public bool debug;
    public Vector2 debugChunkPos;

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

        //orthographicSize * 2 * (1 + (sin(angleOfIncident))^2) = height of screen in world units.
        //height of screen in world units * aspectRatio = width of screen in world units.
        //Use this to calculate RENDER_WINDOW dimensions at startup.

        using (StreamReader reader = new StreamReader("Assets/Preferences/Cavern_Config.txt"))
        {
            string line;
            line = reader.ReadLine();
            SEED = int.Parse(line);            
            line = reader.ReadLine();
            CHUNK_SIZE = int.Parse(line);
            reader.Close();
        }
    }

    private void Update()
    {
        if (Vector2.SqrMagnitude(previousObserverPos - observer.position) < 0.5f)
            return;
        previousObserverPos = observer.position;
        int observerGridX = ((int)observer.position.x / CHUNK_SIZE) - (observer.position.x < 0 ? 1 : 0);
        int observerGridY = ((int)observer.position.y / CHUNK_SIZE) - (observer.position.y < 0 ? 1 : 0);
        Vector2 observerGridPos = new Vector2(observerGridX, observerGridY);

        for (int yOffset = maxRange; yOffset >= -maxRange; yOffset--)
        {
            for (int xOffset = maxRange; xOffset >= -maxRange; xOffset--)
            {
                Vector2 currentChunkGridPos = new Vector2(observerGridPos.x + xOffset, observerGridPos.y + yOffset);

                float currentChunkPosX = (currentChunkGridPos.x + 0.5f) * CHUNK_SIZE;
                float currentChunkPosY = (currentChunkGridPos.y + 0.5f) * CHUNK_SIZE;
                Vector3 currentChunkPos = new Vector3(currentChunkPosX, currentChunkPosY);

                Bounds currentChunkPerimeter = new Bounds(currentChunkPos, Vector2.one * CHUNK_SIZE);
                float currentChunkMinSqrDistance = currentChunkPerimeter.SqrDistance(observer.position);

                if (!nearbyChunks.ContainsKey(currentChunkGridPos))
                {
                    nearbyChunks.Add(currentChunkGridPos, new MapChunk(currentChunkGridPos, tilesGenerator, proceduralPrefabs));
                }
                bool inRenderRange = false;
                for (int i = progressThresholds.Count - 1; i >= 0; i--)
                {
                    if (currentChunkMinSqrDistance < Mathf.Pow(progressThresholds[i].distance, 2))
                    {
                        nearbyChunks[currentChunkGridPos].ObtainStage(progressThresholds[i].stage);
                        if (i == 3)
                        {
                            if (nearbyChunks[currentChunkGridPos].tilesPrepared)
                                nearbyChunks[currentChunkGridPos].Update(observer.position);
                        }
                        inRenderRange = true;
                        break;
                    }
                }
                if (!inRenderRange)
                {
                    //nearbyChunks[currentChunkGridPos].Deactivate();
                }
            }
        }
    }

    private class MapChunk
    {
        public bool[,] tiles;
        //later attributes go here as bool[,] arrays

        private TilesGenerator tilesGenerator;
        private int chunkX, chunkY;
        private int chunkSize;
        private float cellWidth, cellHeight;
        private bool upBoundary, rightBoundary, upRightCell;

        private GameObject[][,] prefabs;
        private PrefabData[,] marchingSquares;
        private const int FINAL_STAGE = 3;
        private int nextStage, goalStage;
        public bool tilesPrepared;

        private struct PrefabData {
            public int floorPrefabType;
            public int floorRotation;
            public int wallPrefabType;
            public int wallRotation;
        }

        public MapChunk(Vector2 chunkPos, TilesGenerator tilesGenerator, ProceduralPrefabs proceduralPrefabs)
        {
            this.tilesGenerator = tilesGenerator;

            chunkX = (int)chunkPos.x;
            chunkY = (int)chunkPos.y;
            chunkSize = CHUNK_SIZE;
            cellWidth = ProceduralPrefabs.CELL_WIDTH;
            cellHeight = ProceduralPrefabs.CELL_HEIGHT;
            upBoundary = rightBoundary = upRightCell = false;

            marchingSquares = new PrefabData[chunkSize, chunkSize];
            nextStage = 0;
            goalStage = -1;
            tilesPrepared = false;

            prefabs = proceduralPrefabs.GetPackageSubscription();
        }
        public void Update(Vector3 observerPos)
        {
            int windowWidth = RENDER_WINDOW_WIDTH / 2;
            int windowHeight = RENDER_WINDOW_HEIGHT / 2;
            int obsX = (int)observerPos.x - 1;
            int obsY = (int)observerPos.y - 1;

            int winX = obsX - windowWidth;
            int winY = obsY - windowHeight;

            int minX = Mathf.Max(obsX - windowWidth, chunkX * chunkSize);
            int maxX = Mathf.Min(obsX + windowWidth, chunkX * chunkSize + chunkSize - 1);
            int minY = Mathf.Max(obsY - windowHeight, chunkY * chunkSize);
            int maxY = Mathf.Min(obsY + windowHeight, chunkY * chunkSize + chunkSize - 1);
            int winXOffset = minX - winX;
            int winYOffset = minY - winY;
            if (maxX >= minX && maxY >= minY)
            {
                minX = ((minX % chunkSize) + chunkSize) % chunkSize;
                maxX = ((maxX % chunkSize) + chunkSize) % chunkSize;
                minY = ((minY % chunkSize) + chunkSize) % chunkSize;
                maxY = ((maxY % chunkSize) + chunkSize) % chunkSize;
                FollowPlayer(minX, maxX, minY, maxY, winXOffset, winYOffset);
            }

            ObtainNeighbors();
        }
        private void FollowPlayer(int minX, int maxX, int minY, int maxY, int winXOffset, int winYOffset)
        {
            Vector3 basePosition = Vector3.zero, midCell = Vector3.zero, axis = Vector3.back;

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    if (marchingSquares[x, y].floorPrefabType == -1)
                        continue;

                    basePosition.x = (chunkX * chunkSize + x) * cellWidth;
                    basePosition.y = (chunkY * chunkSize + y) * cellHeight;
                    midCell.x = basePosition.x + cellWidth / 2f;
                    midCell.y = basePosition.y + cellHeight / 2f;

                    prefabs[marchingSquares[x, y].floorPrefabType][winXOffset + x - minX, winYOffset + y - minY].
                        transform.SetPositionAndRotation(basePosition, Quaternion.identity);
                    prefabs[marchingSquares[x, y].floorPrefabType][winXOffset + x - minX, winYOffset + y - minY].
                        transform.RotateAround(midCell, axis, 90f * marchingSquares[x, y].floorRotation);

                    if (marchingSquares[x, y].wallPrefabType != -1)
                    {
                        prefabs[marchingSquares[x, y].wallPrefabType][winXOffset + x - minX, winYOffset + y - minY].
                            transform.SetPositionAndRotation(basePosition, Quaternion.identity);
                        prefabs[marchingSquares[x, y].wallPrefabType][winXOffset + x - minX, winYOffset + y - minY].
                            transform.RotateAround(midCell, axis, 90f * marchingSquares[x, y].wallRotation);
                    }
                }
            }
        }
        private void ObtainNeighbors()
        {
            if (tilesPrepared)
            {
                if (upBoundary && rightBoundary && upRightCell)
                    return;
                if (!upBoundary)
                {
                    bool[,] upChunk = MapDataSaver.instance.GetCorrectedTiles(new Vector2(chunkX, chunkY + 1));
                    if (upChunk != null)
                    {
                        UpperCells(upChunk);
                        upBoundary = true;
                    }
                }
                if (!rightBoundary)
                {
                    bool[,] rightChunk = MapDataSaver.instance.GetCorrectedTiles(new Vector2(chunkX + 1, chunkY));
                    if (rightChunk != null)
                    {
                        RightCells(rightChunk);
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
                        UpRightCell(upRightChunk[0, 0], upChunk[chunkSize - 1, 0], rightChunk[0, chunkSize - 1]);
                        upRightCell = true;
                    }
                }
            }
        }
        public void ObtainStage(int stage)
        {
            //This logic will change after the implementation of threading.
            //No while loop will be required, the next stage tiles will be requested in OnTilesRecieved function.

            goalStage = stage;
            
            while(goalStage >= nextStage)
            {
                if (nextStage != FINAL_STAGE)
                    tiles = tilesGenerator.ExecuteGenerationStep(chunkX, chunkY, nextStage++);
                if (nextStage == FINAL_STAGE)
                {
                    tilesPrepared = true;
                    nextStage++;
                    MidCells();
                }
            }
            ObtainNeighbors();
        }
        private void MidCells()
        {
            for (int y = 0; y < chunkSize - 1; y++)
            {
                for (int x = 0; x < chunkSize - 1; x++)
                {
                    int binaryToDecimal = (tiles[x, y] ? 1 : 0) * 8 + (tiles[x + 1, y] ? 1 : 0) * 4 + (tiles[x + 1, y + 1] ? 1 : 0) * 2 + (tiles[x, y + 1] ? 1 : 0);
                    if (binaryToDecimal == 0)
                    {
                        marchingSquares[x, y].floorPrefabType = -1;
                        continue;
                    }
                    if (binaryToDecimal == 15)
                    {
                        marchingSquares[x, y].floorPrefabType = (int)ProceduralPrefabs.PrefabType.fullFloor;
                        marchingSquares[x, y].floorRotation = 0;
                        marchingSquares[x, y].wallPrefabType = -1;
                        continue;
                    }
                    MarchingSquareWall(binaryToDecimal, x, y);
                    MarchingSquareFloor(15 - binaryToDecimal, x, y);  
                }
            }
        }
        private void UpperCells(bool[,] upChunk)
        {
            int y = chunkSize - 1;
            for (int x = 0; x < chunkSize - 1; x++)
            {
                int binaryToDecimal = (tiles[x, y] ? 1 : 0) * 8 + (tiles[x + 1, y] ? 1 : 0) * 4 + (upChunk[x + 1, 0] ? 1 : 0) * 2 + (upChunk[x, 0] ? 1 : 0);
                if (binaryToDecimal == 0)
                {
                    marchingSquares[x, y].floorPrefabType = -1;
                    continue;
                }
                if (binaryToDecimal == 15)
                {
                    marchingSquares[x, y].floorPrefabType = (int)ProceduralPrefabs.PrefabType.fullFloor;
                    marchingSquares[x, y].floorRotation = 0;
                    marchingSquares[x, y].wallPrefabType = -1;
                    continue;
                }
                MarchingSquareWall(binaryToDecimal, x, y);
                MarchingSquareFloor(15 - binaryToDecimal, x, y);
            }

        }
        private void RightCells(bool[,] rightChunk)
        {
            int x = chunkSize - 1;
            for (int y = 0; y < chunkSize - 1; y++)
            {
                int binaryToDecimal = (tiles[x, y] ? 1 : 0) * 8 + (rightChunk[0, y] ? 1 : 0) * 4 + (rightChunk[0, y + 1] ? 1 : 0) * 2 + (tiles[x, y + 1] ? 1 : 0);
                if (binaryToDecimal == 0)
                {
                    marchingSquares[x, y].floorPrefabType = -1;
                    continue;
                }
                if (binaryToDecimal == 15)
                {
                    marchingSquares[x, y].floorPrefabType = (int)ProceduralPrefabs.PrefabType.fullFloor;
                    marchingSquares[x, y].floorRotation = 0;
                    marchingSquares[x, y].wallPrefabType = -1;
                    continue;
                }
                MarchingSquareWall(binaryToDecimal, x, y);
                MarchingSquareFloor(15 - binaryToDecimal, x, y);
            }
        }
        private void UpRightCell(bool cornerTile, bool upTile, bool rightTile)
        {
            int x = chunkSize - 1;
            int y = chunkSize - 1;

            int binaryToDecimal = (tiles[x, y] ? 1 : 0) * 8 + (rightTile ? 1 : 0) * 4 + (cornerTile ? 1 : 0) * 2 + (upTile ? 1 : 0);
            if (binaryToDecimal == 0)
                return;
            if (binaryToDecimal == 15)
            {
                marchingSquares[x, y].floorPrefabType = (int)ProceduralPrefabs.PrefabType.fullFloor;
                marchingSquares[x, y].floorRotation = 0;
                marchingSquares[x, y].wallPrefabType = -1;
                return;
            }
            MarchingSquareWall(binaryToDecimal, x, y);
            MarchingSquareFloor(binaryToDecimal, x, y);
        }

        private void MarchingSquareWall(int code, int x, int y)
        {
            switch (code)
            {
                case 1:
                    marchingSquares[x, y].wallPrefabType = (int)ProceduralPrefabs.PrefabType.cornerWall;
                    marchingSquares[x, y].wallRotation = 3;
                    break;
                case 2:
                    marchingSquares[x, y].wallPrefabType = (int)ProceduralPrefabs.PrefabType.cornerWall;
                    marchingSquares[x, y].wallRotation = 0;
                    break;
                case 3:
                    marchingSquares[x, y].wallPrefabType = (int)ProceduralPrefabs.PrefabType.rectangleWall;
                    marchingSquares[x, y].wallRotation = 0;
                    break;
                case 4:
                    marchingSquares[x, y].wallPrefabType = (int)ProceduralPrefabs.PrefabType.cornerWall;
                    marchingSquares[x, y].wallRotation = 1;
                    break;
                case 5:
                    marchingSquares[x, y].wallPrefabType = (int)ProceduralPrefabs.PrefabType.diagonalWall;
                    marchingSquares[x, y].wallRotation = 0;
                    break;
                case 6:
                    marchingSquares[x, y].wallPrefabType = (int)ProceduralPrefabs.PrefabType.rectangleWall;
                    marchingSquares[x, y].wallRotation = 1;
                    break;
                case 7:
                    marchingSquares[x, y].wallPrefabType = (int)ProceduralPrefabs.PrefabType.triangleWall;
                    marchingSquares[x, y].wallRotation = 0;
                    break;
                case 8:
                    marchingSquares[x, y].wallPrefabType = (int)ProceduralPrefabs.PrefabType.cornerWall;
                    marchingSquares[x, y].wallRotation = 2;
                    break;
                case 9:
                    marchingSquares[x, y].wallPrefabType = (int)ProceduralPrefabs.PrefabType.rectangleWall;
                    marchingSquares[x, y].wallRotation = 3;
                    break;
                case 10:
                    marchingSquares[x, y].wallPrefabType = (int)ProceduralPrefabs.PrefabType.diagonalWall;
                    marchingSquares[x, y].wallRotation = 1;
                    break;
                case 11:
                    marchingSquares[x, y].wallPrefabType = (int)ProceduralPrefabs.PrefabType.triangleWall;
                    marchingSquares[x, y].wallRotation = 3;
                    break;
                case 12:
                    marchingSquares[x, y].wallPrefabType = (int)ProceduralPrefabs.PrefabType.rectangleWall;
                    marchingSquares[x, y].wallRotation = 2;
                    break;
                case 13:
                    marchingSquares[x, y].wallPrefabType = (int)ProceduralPrefabs.PrefabType.triangleWall;
                    marchingSquares[x, y].wallRotation = 2;
                    break;
                case 14:
                    marchingSquares[x, y].wallPrefabType = (int)ProceduralPrefabs.PrefabType.triangleWall;
                    marchingSquares[x, y].wallRotation = 1;
                    break;
            }
        }
        private void MarchingSquareFloor(int code, int x, int y)
        {
            switch (code)
            {
                case 1:
                    marchingSquares[x, y].floorPrefabType = (int)ProceduralPrefabs.PrefabType.cornerFloor;
                    marchingSquares[x, y].floorRotation = 3;
                    break;
                case 2:
                    marchingSquares[x, y].floorPrefabType = (int)ProceduralPrefabs.PrefabType.cornerFloor;
                    marchingSquares[x, y].floorRotation = 0;
                    break;
                case 3:
                    marchingSquares[x, y].floorPrefabType = (int)ProceduralPrefabs.PrefabType.rectangleFloor;
                    marchingSquares[x, y].floorRotation = 0;
                    break;
                case 4:
                    marchingSquares[x, y].floorPrefabType = (int)ProceduralPrefabs.PrefabType.cornerFloor;
                    marchingSquares[x, y].floorRotation = 1;
                    break;
                case 5:
                    marchingSquares[x, y].floorPrefabType = (int)ProceduralPrefabs.PrefabType.crossFloor;
                    marchingSquares[x, y].floorRotation = 1;
                    break;
                case 6:
                    marchingSquares[x, y].floorPrefabType = (int)ProceduralPrefabs.PrefabType.rectangleFloor;
                    marchingSquares[x, y].floorRotation = 1;
                    break;
                case 7:
                    marchingSquares[x, y].floorPrefabType = (int)ProceduralPrefabs.PrefabType.triangleFloor;
                    marchingSquares[x, y].floorRotation = 0;
                    break;
                case 8:
                    marchingSquares[x, y].floorPrefabType = (int)ProceduralPrefabs.PrefabType.cornerFloor;
                    marchingSquares[x, y].floorRotation = 2;
                    break;
                case 9:
                    marchingSquares[x, y].floorPrefabType = (int)ProceduralPrefabs.PrefabType.rectangleFloor;
                    marchingSquares[x, y].floorRotation = 3;
                    break;
                case 10:
                    marchingSquares[x, y].floorPrefabType = (int)ProceduralPrefabs.PrefabType.crossFloor;
                    marchingSquares[x, y].floorRotation = 0;
                    break;
                case 11:
                    marchingSquares[x, y].floorPrefabType = (int)ProceduralPrefabs.PrefabType.triangleFloor;
                    marchingSquares[x, y].floorRotation = 3;
                    break;
                case 12:
                    marchingSquares[x, y].floorPrefabType = (int)ProceduralPrefabs.PrefabType.rectangleFloor;
                    marchingSquares[x, y].floorRotation = 2;
                    break;
                case 13:
                    marchingSquares[x, y].floorPrefabType = (int)ProceduralPrefabs.PrefabType.triangleFloor;
                    marchingSquares[x, y].floorRotation = 2;
                    break;
                case 14:
                    marchingSquares[x, y].floorPrefabType = (int)ProceduralPrefabs.PrefabType.triangleFloor;
                    marchingSquares[x, y].floorRotation = 1;
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

    public void OnDrawGizmos()
    {
        if (!debug)
            return;
        if (!nearbyChunks.ContainsKey(debugChunkPos))
            return;
        MapChunk debugChunk = nearbyChunks[debugChunkPos];
        if (!debugChunk.tilesPrepared)
            return;
        Gizmos.color = new Color(Color.yellow.r, Color.yellow.g, Color.yellow.b, 0.2f);
        for (int y = 0; y < CHUNK_SIZE; y++)
        {
            for (int x = 0; x < CHUNK_SIZE; x++)
            {
                Vector3 centre = new Vector3(debugChunkPos.x * CHUNK_SIZE, debugChunkPos.y * CHUNK_SIZE, 0);
                centre.x += ProceduralPrefabs.CELL_WIDTH * x;
                centre.y += ProceduralPrefabs.CELL_HEIGHT * y;
                if (debugChunk.tiles[x, y])
                    Gizmos.DrawCube(centre, Vector2.one);
            }
        }
    }
}
