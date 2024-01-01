using UnityEngine;

public class MeshGenerationPreview : MonoBehaviour
{
    [SerializeField] private ChunkDebugger mainDebugger;
    [SerializeField] private ChunkDebugger neighborDebugger;
    [SerializeField] private TilesGenerator tilesGenerator;

    private bool[,] mainChunkData;
    private bool[,] neighborChunkData;

    private void Start()
    {
        tilesGenerator.SetChunkSize(27);

        tilesGenerator.ExecuteGenerationStep(0, 0, 0);
        tilesGenerator.ExecuteGenerationStep(0, 0, 1);
        mainChunkData = tilesGenerator.ExecuteGenerationStep(0, 0, 2);

        tilesGenerator.ExecuteGenerationStep(0, 1, 0);
        tilesGenerator.ExecuteGenerationStep(0, 1, 1);
        neighborChunkData = tilesGenerator.ExecuteGenerationStep(0, 1, 2);

        DisplayData();
    }

    private void DisplayData()
    {
        if (mainDebugger != null)
        {
            mainDebugger.SetChunkData(mainChunkData);
        }
        if (neighborDebugger != null)
        {
            neighborDebugger.SetChunkData(neighborChunkData);
        }
    }

    private void Update()
    {

    }
}
