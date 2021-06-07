using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Faizu
{
    namespace ChunkGrid
    {
        public class PoolManager
        {
            private const int POOL_OFFSET_Z = 10000;

            private readonly GameObject subject;
            private GameObject parent;
            private List<bool> poolOccupied = new List<bool>();
            private int[,] subscriptionTable = new int[10, 10];
            private List<GameObject[]> pools = new List<GameObject[]>();
            private List<int> lastIndices = new List<int>();

            public PoolManager(GameObject subject, int initialCapacity, GameObject parent)
            {
                this.parent = parent;
                this.subject = subject;
                GameObject[] initialPool = new GameObject[initialCapacity];

                for (int i = 0; i < initialCapacity; i++)
                {
                    GameObject prefab = GameObject.Instantiate(subject);
                    prefab.SetActive(false);
                    prefab.transform.position = new Vector3(0, 0, POOL_OFFSET_Z);
                    initialPool[i] = prefab;
                    prefab.transform.parent = parent.transform;
                }

                pools.Add(initialPool);
                lastIndices.Add(initialCapacity - 1);
                poolOccupied.Add(false);

                for (int i = 0; i < 10; i++)
                {
                    for (int j = 0; j < 10; j++)
                    {
                        subscriptionTable[i, j] = -1;
                    }
                }
            }

            public void Subscribe(int chunkX, int chunkY)
            {
                if (subscriptionTable[((chunkX % 10) + 10) % 10, ((chunkY % 10) + 10) % 10] != -1)
                {
                    Debug.Log("A Subscriber tried to subscribe again at Position: " + new Vector2(chunkX, chunkY));
                    return;
                }

                for (int i = 0; i < pools.Count; i++)
                {
                    if (!poolOccupied[i] && lastIndices[i] == -1)
                    {
                        subscriptionTable[((chunkX % 10) + 10) % 10, ((chunkY % 10) + 10) % 10] = i;
                        poolOccupied[i] = true;
                        return;
                    }
                }

                pools.Add(new GameObject[EndlessCavern.CHUNK_SIZE * EndlessCavern.CHUNK_SIZE]);
                lastIndices.Add(-1);
                poolOccupied.Add(true);
                subscriptionTable[((chunkX % 10) + 10) % 10, ((chunkY % 10) + 10) % 10] = pools.Count - 1;
            }

            public void UnSubscribe(int chunkX, int chunkY)
            {
                if (subscriptionTable[((chunkX % 10) + 10) % 10, ((chunkY % 10) + 10) % 10] == -1)
                {
                    Debug.Log("A non-member tried to unsubscribe to PoolManager at Position: " + new Vector2(chunkX, chunkY));
                    return;
                }

                int subscriberId = subscriptionTable[((chunkX % 10) + 10) % 10, ((chunkY % 10) + 10) % 10];
                subscriptionTable[((chunkX % 10) + 10) % 10, ((chunkY % 10) + 10) % 10] = -1;
                poolOccupied[subscriberId] = false;
            }

            public GameObject GetPrefab(int chunkX, int chunkY)
            {
                int subscriberId = subscriptionTable[((chunkX % 10) + 10) % 10, ((chunkY % 10) + 10) % 10];
                GameObject subscriberObject = null;

                for (int i = pools.Count - 1; i >= 0; i--)
                {
                    if (!poolOccupied[i] && lastIndices[i] != -1)
                    {
                        subscriberObject = pools[i][lastIndices[i]];
                        pools[i][lastIndices[i]] = null;
                        lastIndices[i] = lastIndices[i] - 1;

                        lastIndices[subscriberId] = lastIndices[subscriberId] + 1;
                        pools[subscriberId][lastIndices[subscriberId]] = subscriberObject;
                        break;
                    }
                }

                if (subscriberObject == null)
                {
                    Debug.Log("Pool Manager ran out of " + subject.name + " prefabs");
                    subscriberObject = GameObject.Instantiate(subject);
                    subscriberObject.transform.parent = parent.transform;
                    lastIndices[subscriberId] = lastIndices[subscriberId] + 1;
                    pools[subscriberId][lastIndices[subscriberId]] = subscriberObject;
                }

                subscriberObject.SetActive(true);

                return subscriberObject;
            }

        }
    }
}
