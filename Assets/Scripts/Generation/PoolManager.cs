using UnityEngine;

namespace Faizan314.ChunkGrid
{
    public class PoolManager
    {
        private const int POOL_OFFSET_Z = 10000;

        private GameObject[,] window;

        public PoolManager(GameObject subject, int windowWidth, int windowHeight, GameObject parent)
        {
            window = new GameObject[windowWidth, windowHeight];
            for (int y = 0; y < windowHeight; y++)
            {
                for (int x = 0; x < windowWidth; x++)
                {
                    window[x, y] = Object.Instantiate(subject);
                    window[x, y].transform.SetPositionAndRotation(new Vector3(0, 0, POOL_OFFSET_Z), Quaternion.identity);
                    window[x, y].transform.parent = parent.transform;
                }
            }
        }

        public GameObject[,] Subscribe()
        {
            return window;
        }
    }
}
