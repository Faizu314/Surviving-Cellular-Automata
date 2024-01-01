using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class EndlessCavern : MonoBehaviour
{
    public static int CHUNK_SIZE;
    public static int SEED;
    public static int RENDER_WINDOW_WIDTH = 27;
    public static int RENDER_WINDOW_HEIGHT = 27;

    [SerializeField] private Transform observer;

    public bool debug;
    public Vector2 debugChunkPos;

    public List<ProgressThreshold> progressThresholds;
    public int maxRange;

    private Dictionary<Vector2, MapChunk> nearbyChunks = new Dictionary<Vector2, MapChunk>();
    private Vector3 previousObserverPos;
    private TilesGenerator tilesGenerator;
    private ProceduralPrefabs proceduralPrefabs;


    private void LoadCavernData()
    {
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
    private void LoadRenderWindowDimensions()
    {
        //orthographicSize * 2 * (1 + (sin(angleOfIncident))^2) = height of screen in world units.
        //height of screen in world units * aspectRatio = width of screen in world units.
        //Use this to calculate RENDER_WINDOW dimensions at startup.
    }
    private void Start()
    {
        tilesGenerator = gameObject.GetComponent<TilesGenerator>();
        proceduralPrefabs = gameObject.GetComponent<ProceduralPrefabs>();
        previousObserverPos = observer.position + Vector3.one * 4;

        LoadRenderWindowDimensions();
        LoadCavernData();
    }

    private Vector2 WorldToGridPosition(Vector3 coordinates)
    {
        int observerGridX = ((int)coordinates.x / CHUNK_SIZE) - (coordinates.x < 0 ? 1 : 0);
        int observerGridY = ((int)coordinates.y / CHUNK_SIZE) - (coordinates.y < 0 ? 1 : 0);
        return new Vector2(observerGridX, observerGridY);
    }
    private Vector3 GridToWorldPosition(Vector2 coordinates)
    {
        float chunkPosX = (coordinates.x + 0.5f) * CHUNK_SIZE;
        float chunkPosY = (coordinates.y + 0.5f) * CHUNK_SIZE;
        return new Vector3(chunkPosX, chunkPosY);
    }
    private float GetMinChunkToPlayerDist(Vector2 chunkGridPos)
    {
        Vector3 currentChunkPos = GridToWorldPosition(chunkGridPos);
        Bounds currentChunkPerimeter = new Bounds(currentChunkPos, Vector2.one * CHUNK_SIZE);
        return currentChunkPerimeter.SqrDistance(observer.position);
    }
    private void UpdateChunk(Vector2 chunkGridPos, float minSqrDistFromPlayer)
    {
        for (int i = progressThresholds.Count - 1; i >= 0; i--)
        {
            if (minSqrDistFromPlayer < Mathf.Pow(progressThresholds[i].distance, 2))
            {
                nearbyChunks[chunkGridPos].PrepareStage(progressThresholds[i].stage);
                if (i == progressThresholds.Count - 1)
                    nearbyChunks[chunkGridPos].RenderIfPrepared(observer.position);
                return;
            }
        }
    }

    private void Update()
    {
        if (Vector2.SqrMagnitude(previousObserverPos - observer.position) < 0.5f)
            return;
        previousObserverPos = observer.position;
        UpdateNearbyChunks();
    }

    private void UpdateNearbyChunks()
    {
        Vector2 observerGridPos = WorldToGridPosition(observer.position);

        for (int yOffset = maxRange; yOffset >= -maxRange; yOffset--)
        {
            for (int xOffset = maxRange; xOffset >= -maxRange; xOffset--)
            {
                Vector2 currentChunkGridPos = observerGridPos + new Vector2(xOffset, yOffset);
                if (!nearbyChunks.ContainsKey(currentChunkGridPos))
                    nearbyChunks.Add(currentChunkGridPos, new MapChunk(currentChunkGridPos, tilesGenerator));

                float currentChunkMinSqrDistance = GetMinChunkToPlayerDist(currentChunkGridPos);
                UpdateChunk(currentChunkGridPos, currentChunkMinSqrDistance);
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
        if (!debugChunk.IsReady)
            return;
        Gizmos.color = new Color(Color.yellow.r, Color.yellow.g, Color.yellow.b, 0.2f);
        for (int y = 0; y < CHUNK_SIZE; y++)
        {
            for (int x = 0; x < CHUNK_SIZE; x++)
            {
                Vector3 centre = new Vector3(debugChunkPos.x * CHUNK_SIZE, debugChunkPos.y * CHUNK_SIZE, 0);
                centre.x += ProceduralPrefabs.CELL_WIDTH * x;
                centre.y += ProceduralPrefabs.CELL_HEIGHT * y;
                if (debugChunk.RawData[x, y])
                    Gizmos.DrawCube(centre, Vector2.one);
            }
        }
    }
}
