using UnityEngine;

public class ChunkDebugger : MonoBehaviour
{
    private bool[,] currChunkData = null;

    public void SetChunkData(bool[,] chunkData)
    {
        currChunkData = chunkData;
    }

    private void OnDrawGizmos()
    {
        if (currChunkData == null)
            return;
        for (int y = 0; y < currChunkData.GetLength(0); y++)
        {
            for (int x = 0; x < currChunkData.GetLength(1); x++)
            {
                Vector3 tileOrigin = new(x, y, 0);
                Gizmos.color = currChunkData[x, y] ? Color.green : Color.grey;
                Gizmos.DrawSphere(transform.position + tileOrigin, 0.1f);
            }
        }
    }
}
