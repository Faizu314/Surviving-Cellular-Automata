using UnityEngine;



public class MapChunk
{
    const int Initial_Map_Stage = 0,
        Automata_Stage = 1,
        Correction_Stage = 2,
        Mesh_Generation_Stage = 3;

    private ChunkData chunkData;
    private GameObject chunk;
    private bool isReady;

    public bool IsReady { get { return isReady; } }
    public bool[,] RawData { get { return chunkData.Data; } }

    public MapChunk(Vector2 chunkPos, TilesGenerator tilesGenerator)
    {
        chunkData = new ChunkData(chunkPos, tilesGenerator);
        chunk = new GameObject("Chunk (" + chunkPos.x + ", " + chunkPos.y + ")");
        chunk.transform.position = chunkPos;
        chunk.SetActive(false);
        isReady = false;
    }

    //This will be changed to generate mesh if prepared
    public void RenderIfPrepared(Vector3 observerPos)
    {
        if (!isReady)
            return;
        chunk.SetActive(true);
    }
    public void PrepareStage(int stage)
    {
        switch (stage)
        {
            case Initial_Map_Stage:
                chunkData.RequestData(stage);
                break;
            case Automata_Stage:
                chunkData.RequestData(stage);
                break;
            case Correction_Stage:
                chunkData.RequestData(stage);
                break;
            case Mesh_Generation_Stage:
                // use mesh generator
                break;
            default:
                Debug.LogError("MapChunk: Stage index out of bounds");
                break;
        }
       
    }
}