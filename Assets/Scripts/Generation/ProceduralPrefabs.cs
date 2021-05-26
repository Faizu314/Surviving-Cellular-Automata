using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Faizu.ChunkGrid;

public class ProceduralPrefabs : MonoBehaviour
{
    public List<PrefabPackage> packages = new List<PrefabPackage>();

    public const float CELL_WIDTH = 1;
    public const float CELL_HEIGHT = 1;
    public const float WALL_HEIGHT = 1f;

    private PoolManager[,] poolManagers;
    public enum PrefabType { fullFloor, rectangleFloor, crossFloor, triangleFloor, cornerFloor, triangleWall, rectangleWall, diagonalWall, cornerWall };

    private void Start()
    {
        GenerateDefaultTriangleWall();
        GenerateDefaultRectangleWall();
        GenerateDefaultDiagonalWall();
        GenerateDefaultCornerWall();

        GameObject pool = new GameObject("Prefab Pool");
        poolManagers = new PoolManager[packages.Count, packages[0].biomePrefabs.Count];
        for (int i = 0; i < packages.Count; i++)
        {
            for (int j = 0; j < packages[0].biomePrefabs.Count; j++)
            {
                PrefabType currType = (PrefabType)j;
                switch (currType)
                {
                    case PrefabType.fullFloor:
                        poolManagers[i, j] = new PoolManager(packages[0].biomePrefabs[j], 1300, pool);
                        break;
                    case PrefabType.rectangleFloor:
                        poolManagers[i, j] = new PoolManager(packages[0].biomePrefabs[j], 600, pool);
                        break;
                    case PrefabType.crossFloor:
                        poolManagers[i, j] = new PoolManager(packages[0].biomePrefabs[j], 100, pool);
                        break;
                    case PrefabType.triangleFloor:
                        poolManagers[i, j] = new PoolManager(packages[0].biomePrefabs[j], 400, pool);
                        break;
                    case PrefabType.cornerFloor:
                        poolManagers[i, j] = new PoolManager(packages[0].biomePrefabs[j], 750, pool);
                        break;
                    case PrefabType.triangleWall:
                        poolManagers[i, j] = new PoolManager(packages[0].biomePrefabs[j], 750, pool);
                        break;
                    case PrefabType.rectangleWall:
                        poolManagers[i, j] = new PoolManager(packages[0].biomePrefabs[j], 500, pool);
                        break;
                    case PrefabType.diagonalWall:
                        poolManagers[i, j] = new PoolManager(packages[0].biomePrefabs[j], 100, pool);
                        break;
                    case PrefabType.cornerWall:
                        poolManagers[i, j] = new PoolManager(packages[0].biomePrefabs[j], 400, pool);
                        break;
                }
            }
        }
    }

    private void GenerateDefaultTriangleWall()
    {
        Mesh triangleMesh = new Mesh();

        Vector3 _0 = Vector3.zero;
        Vector3 _1 = new Vector3(0, 0, -WALL_HEIGHT);
        Vector3 _2 = new Vector3(CELL_WIDTH / 2f, 0, 0);
        Vector3 _3 = new Vector3(CELL_WIDTH / 2f, 0, -WALL_HEIGHT);
        Vector3 _4 = new Vector3(0, CELL_HEIGHT / 2f, 0);
        Vector3 _5 = new Vector3(0, CELL_HEIGHT / 2f, -WALL_HEIGHT);

        Vector3[] vertices = new Vector3[15];
        vertices[0] = _0;
        vertices[1] = _1;
        vertices[2] = _2;
        vertices[3] = _3;
        vertices[4] = _4;
        vertices[5] = _5;
        vertices[6] = _0;
        vertices[7] = _1;
        vertices[8] = _2;
        vertices[9] = _3;
        vertices[10] = _4;
        vertices[11] = _5;
        vertices[12] = _1;
        vertices[13] = _5;
        vertices[14] = _3;

        int[] triangles = new int[7 * 3];
        triangles[0] = 0;
        triangles[1] = 3;
        triangles[2] = 2;

        triangles[3] = 0;
        triangles[4] = 1;
        triangles[5] = 3;

        triangles[6] = 4;
        triangles[7] = 7;
        triangles[8] = 6;

        triangles[9] = 4;
        triangles[10] = 5;
        triangles[11] = 7;

        triangles[12] = 9;
        triangles[13] = 10;
        triangles[14] = 8;

        triangles[15] = 9;
        triangles[16] = 11;
        triangles[17] = 10;

        triangles[18] = 12;
        triangles[19] = 13;
        triangles[20] = 14;

        Vector2[] uv = new Vector2[15];
        uv[0] = Vector2.zero;
        uv[1] = new Vector2(0, 1);
        uv[2] = new Vector2(1, 0);
        uv[3] = new Vector2(1, 1);

        uv[4] = Vector2.zero;
        uv[5] = new Vector2(0, 1);
        uv[6] = new Vector2(1, 0);
        uv[7] = new Vector2(1, 1);

        uv[8] = Vector2.zero;
        uv[9] = new Vector2(0, 1);
        uv[10] = new Vector2(1, 0);
        uv[11] = new Vector2(1, 1);

        uv[12] = new Vector2(0, 0);
        uv[13] = new Vector2(1, 0);
        uv[14] = new Vector2(0, 1);


        triangleMesh.vertices = vertices;
        triangleMesh.triangles = triangles;
        triangleMesh.uv = uv;
        triangleMesh.RecalculateNormals();

        packages[0].biomePrefabs[5].GetComponent<MeshFilter>().sharedMesh = triangleMesh;
        packages[0].biomePrefabs[5].GetComponent<MeshRenderer>().sharedMaterial = packages[0].wallMaterial;
        packages[0].biomePrefabs[5].GetComponent<MeshCollider>().sharedMesh = triangleMesh;
    }
    private void GenerateDefaultRectangleWall()
    {
        Mesh rectangleMesh = new Mesh();

        Vector3 _0 = Vector3.zero;
        Vector3 _1 = new Vector3(0, 0, -WALL_HEIGHT);
        Vector3 _2 = new Vector3(CELL_WIDTH, 0, 0);
        Vector3 _3 = new Vector3(CELL_WIDTH, 0, -WALL_HEIGHT);
        Vector3 _4 = new Vector3(0, CELL_HEIGHT / 2f, 0);
        Vector3 _5 = new Vector3(0, CELL_HEIGHT / 2f, -WALL_HEIGHT);
        Vector3 _6 = new Vector3(CELL_WIDTH, CELL_HEIGHT / 2f, 0);
        Vector3 _7 = new Vector3(CELL_WIDTH, CELL_HEIGHT / 2f, -WALL_HEIGHT);

        Vector3[] vertices = new Vector3[20];
        vertices[0] = _0;
        vertices[1] = _1;
        vertices[2] = _2;
        vertices[3] = _3;

        vertices[4] = _4;
        vertices[5] = _5;
        vertices[6] = _0;
        vertices[7] = _1;

        vertices[8] = _2;
        vertices[9] = _3;
        vertices[10] = _6;
        vertices[11] = _7;

        vertices[12] = _6;
        vertices[13] = _7;
        vertices[14] = _4;
        vertices[15] = _5;

        vertices[16] = _1;
        vertices[17] = _5;
        vertices[18] = _7;
        vertices[19] = _3;

        int[] triangles = new int[10 * 3];
        triangles[0] = 0;
        triangles[1] = 1;
        triangles[2] = 3;
        triangles[3] = 0;
        triangles[4] = 3;
        triangles[5] = 2;

        triangles[6] = 6;
        triangles[7] = 4;
        triangles[8] = 7;
        triangles[9] = 4;
        triangles[10] = 5;
        triangles[11] = 7;

        triangles[12] = 8;
        triangles[13] = 9;
        triangles[14] = 10;
        triangles[15] = 10;
        triangles[16] = 9;
        triangles[17] = 11;

        triangles[18] = 12;
        triangles[19] = 13;
        triangles[20] = 14;
        triangles[21] = 14;
        triangles[22] = 13;
        triangles[23] = 15;

        triangles[24] = 19;
        triangles[25] = 16;
        triangles[26] = 17;
        triangles[27] = 19;
        triangles[28] = 17;
        triangles[29] = 18;

        Vector2[] uv = new Vector2[20];
        uv[0] = Vector2.zero;
        uv[1] = new Vector2(0, 1);
        uv[2] = new Vector2(1, 0);
        uv[3] = new Vector2(1, 1);

        uv[4] = new Vector2(1, 0);
        uv[5] = new Vector2(1, 1);
        uv[6] = new Vector2(0, 0);
        uv[7] = new Vector2(0, 1);

        uv[8] = new Vector2(0, 1);
        uv[9] = new Vector2(1, 0);
        uv[10] = new Vector2(1, 1);
        uv[11] = new Vector2(1, 0);

        uv[12] = new Vector2(1, 1);
        uv[13] = new Vector2(0, 0);
        uv[14] = new Vector2(0, 1);
        uv[15] = new Vector2(1, 0);

        uv[16] = new Vector2(1, 1);
        uv[17] = new Vector2(0, 0);
        uv[18] = new Vector2(0, 1);
        uv[19] = new Vector2(0, 1);

        rectangleMesh.vertices = vertices;
        rectangleMesh.triangles = triangles;
        rectangleMesh.uv = uv;
        rectangleMesh.RecalculateNormals();

        packages[0].biomePrefabs[6].GetComponent<MeshFilter>().sharedMesh = rectangleMesh;
        packages[0].biomePrefabs[6].GetComponent<MeshRenderer>().sharedMaterial = packages[0].wallMaterial;
        packages[0].biomePrefabs[6].GetComponent<MeshCollider>().sharedMesh = rectangleMesh;
    }
    private void GenerateDefaultDiagonalWall()
    {
        Mesh diagonalMesh = new Mesh();

        Vector3 _0 = Vector3.zero;
        Vector3 _1 = new Vector3(0, 0, -WALL_HEIGHT);
        Vector3 _2 = new Vector3(CELL_WIDTH / 2f, 0, 0);
        Vector3 _3 = new Vector3(CELL_WIDTH / 2f, 0, -WALL_HEIGHT);
        Vector3 _4 = new Vector3(0, CELL_HEIGHT / 2f, 0);
        Vector3 _5 = new Vector3(0, CELL_HEIGHT / 2f, -WALL_HEIGHT);
        Vector3 _6 = new Vector3(CELL_WIDTH, CELL_HEIGHT / 2f, 0);
        Vector3 _7 = new Vector3(CELL_WIDTH, CELL_HEIGHT / 2f, -WALL_HEIGHT);        
        Vector3 _8 = new Vector3(CELL_WIDTH, CELL_HEIGHT, 0);
        Vector3 _9 = new Vector3(CELL_WIDTH, CELL_HEIGHT, -WALL_HEIGHT);
        Vector3 _10 = new Vector3(CELL_WIDTH / 2f, CELL_HEIGHT, 0);
        Vector3 _11 = new Vector3(CELL_WIDTH / 2f, CELL_HEIGHT, -WALL_HEIGHT);

        Vector3[] vertices = new Vector3[30];
        //Surface #1
        vertices[0] = _0;
        vertices[1] = _1;
        vertices[2] = _2;
        vertices[3] = _3;

        //Surface #2
        vertices[4] = _2;
        vertices[5] = _3;
        vertices[6] = _6;
        vertices[7] = _7;

        //Surface #3
        vertices[8] = _6;
        vertices[9] = _7;
        vertices[10] = _8;
        vertices[11] = _9;

        //Surface #4
        vertices[12] = _8;
        vertices[13] = _9;
        vertices[14] = _10;
        vertices[15] = _11;

        //Surface #5
        vertices[16] = _10;
        vertices[17] = _11;
        vertices[18] = _4;
        vertices[19] = _5;

        //Surface #6
        vertices[20] = _4;
        vertices[21] = _5;
        vertices[22] = _0;
        vertices[23] = _1;

        //Surface #7
        vertices[24] = _1;
        vertices[25] = _3;
        vertices[26] = _7;
        vertices[27] = _9;
        vertices[28] = _11;
        vertices[29] = _5;

        int[] triangles = new int[16 * 3];
        //Surface #1
        triangles[0] = 0;
        triangles[1] = 1;
        triangles[2] = 3;
        triangles[3] = 0;
        triangles[4] = 3;
        triangles[5] = 2;

        //Surface #2
        triangles[6] = 4;
        triangles[7] = 5;
        triangles[8] = 6;
        triangles[9] = 5;
        triangles[10] = 7;
        triangles[11] = 6;

        //Surface #3
        triangles[12] = 8;
        triangles[13] = 9;
        triangles[14] = 10;
        triangles[15] = 11;
        triangles[16] = 10;
        triangles[17] = 9;

        //Surface #4
        triangles[18] = 12;
        triangles[19] = 13;
        triangles[20] = 14;
        triangles[21] = 14;
        triangles[22] = 13;
        triangles[23] = 15;

        //Surface #5
        triangles[24] = 18;
        triangles[25] = 16;
        triangles[26] = 17;
        triangles[27] = 19;
        triangles[28] = 18;
        triangles[29] = 17;

        //Surface #6
        triangles[30] = 20;
        triangles[31] = 21;
        triangles[32] = 23;
        triangles[33] = 20;
        triangles[34] = 23;
        triangles[35] = 22;

        //Surface #7
        triangles[36] = 24;
        triangles[37] = 29;
        triangles[38] = 25;
        triangles[39] = 25;
        triangles[40] = 29;
        triangles[41] = 28;
        triangles[42] = 25;
        triangles[43] = 28;
        triangles[44] = 26;
        triangles[45] = 26;
        triangles[46] = 28;
        triangles[47] = 27;

        Vector2[] uv = new Vector2[30];
        uv[0] = Vector2.zero;
        uv[1] = new Vector2(0, 1);
        uv[2] = new Vector2(1, 0);
        uv[3] = new Vector2(1, 1);

        uv[4] = Vector2.zero;
        uv[5] = new Vector2(0, 1);
        uv[6] = new Vector2(1, 0);
        uv[7] = new Vector2(1, 1);

        uv[8] = Vector2.zero;
        uv[9] = new Vector2(0, 1);
        uv[10] = new Vector2(1, 0);
        uv[11] = new Vector2(1, 1);

        uv[12] = Vector2.zero;
        uv[13] = new Vector2(0, 1);
        uv[14] = new Vector2(1, 0);
        uv[15] = new Vector2(1, 1);

        uv[16] = Vector2.zero;
        uv[17] = new Vector2(0, 1);
        uv[18] = new Vector2(1, 0);
        uv[19] = new Vector2(1, 1);

        uv[20] = Vector2.zero;
        uv[21] = new Vector2(0, 1);
        uv[22] = new Vector2(1, 0);
        uv[23] = new Vector2(1, 1);

        uv[24] = new Vector2(-1, 0.5f);
        uv[25] = new Vector2(0, 0);
        uv[26] = new Vector2(1, 0);
        uv[27] = new Vector2(2, 0.5f);
        uv[28] = new Vector2(1, 1);
        uv[29] = new Vector2(0, 1);

        diagonalMesh.vertices = vertices;
        diagonalMesh.triangles = triangles;
        diagonalMesh.uv = uv;
        diagonalMesh.RecalculateNormals();

        packages[0].biomePrefabs[7].GetComponent<MeshFilter>().sharedMesh = diagonalMesh;
        packages[0].biomePrefabs[7].GetComponent<MeshRenderer>().sharedMaterial = packages[0].wallMaterial;
        packages[0].biomePrefabs[7].GetComponent<MeshCollider>().sharedMesh = diagonalMesh;
    }
    private void GenerateDefaultCornerWall()
    {
        Mesh cornerMesh = new Mesh();

        Vector3 _0 = Vector3.zero;
        Vector3 _1 = new Vector3(0, 0, -WALL_HEIGHT);
        Vector3 _2 = new Vector3(CELL_WIDTH, 0, 0);
        Vector3 _3 = new Vector3(CELL_WIDTH, 0, -WALL_HEIGHT);
        Vector3 _4 = new Vector3(0, CELL_HEIGHT, 0);
        Vector3 _5 = new Vector3(0, CELL_HEIGHT, -WALL_HEIGHT);
        Vector3 _6 = new Vector3(CELL_WIDTH, CELL_HEIGHT / 2f, 0);
        Vector3 _7 = new Vector3(CELL_WIDTH, CELL_HEIGHT / 2f, -WALL_HEIGHT);
        Vector3 _8 = new Vector3(CELL_WIDTH / 2f, CELL_HEIGHT, 0);
        Vector3 _9 = new Vector3(CELL_WIDTH / 2f, CELL_HEIGHT, -WALL_HEIGHT);

        Vector3[] vertices = new Vector3[25];
        //Surface #1
        vertices[0] = _0;
        vertices[1] = _1;
        vertices[2] = _2;
        vertices[3] = _3;

        //Surface #2
        vertices[4] = _2;
        vertices[5] = _3;
        vertices[6] = _6;
        vertices[7] = _7;

        //Surface #3
        vertices[8] = _6;
        vertices[9] = _7;
        vertices[10] = _8;
        vertices[11] = _9;

        //Surface #4
        vertices[12] = _8;
        vertices[13] = _9;
        vertices[14] = _4;
        vertices[15] = _5;

        //Surface #5
        vertices[16] = _4;
        vertices[17] = _5;
        vertices[18] = _0;
        vertices[19] = _1;

        //Surface #6
        vertices[20] = _1;
        vertices[21] = _3;
        vertices[22] = _7;
        vertices[23] = _9;
        vertices[24] = _5;

        int[] triangles = new int[13 * 3];
        //Surface #1
        triangles[0] = 0;
        triangles[1] = 1;
        triangles[2] = 3;
        triangles[3] = 0;
        triangles[4] = 3;
        triangles[5] = 2;

        //Surface #2
        triangles[6] = 4;
        triangles[7] = 5;
        triangles[8] = 6;
        triangles[9] = 5;
        triangles[10] = 7;
        triangles[11] = 6;

        //Surface #3
        triangles[12] = 8;
        triangles[13] = 9;
        triangles[14] = 10;
        triangles[15] = 11;
        triangles[16] = 10;
        triangles[17] = 9;

        //Surface #4
        triangles[18] = 12;
        triangles[19] = 13;
        triangles[20] = 14;
        triangles[21] = 14;
        triangles[22] = 13;
        triangles[23] = 15;

        //Surface #5
        triangles[24] = 19;
        triangles[25] = 16;
        triangles[26] = 17;
        triangles[27] = 16;
        triangles[28] = 19;
        triangles[29] = 18;

        //Surface #6
        triangles[30] = 21;
        triangles[31] = 20;
        triangles[32] = 22;
        triangles[33] = 22;
        triangles[34] = 20;
        triangles[35] = 23;
        triangles[36] = 23;
        triangles[37] = 20;
        triangles[38] = 24;

        Vector2[] uv = new Vector2[25];
        uv[0] = Vector2.zero;
        uv[1] = new Vector2(0, 1);
        uv[2] = new Vector2(1, 0);
        uv[3] = new Vector2(1, 1);

        uv[4] = Vector2.zero;
        uv[5] = new Vector2(0, 1);
        uv[6] = new Vector2(1, 0);
        uv[7] = new Vector2(1, 1);

        uv[8] = Vector2.zero;
        uv[9] = new Vector2(0, 1);
        uv[10] = new Vector2(1, 0);
        uv[11] = new Vector2(1, 1);

        uv[12] = Vector2.zero;
        uv[13] = new Vector2(0, 1);
        uv[14] = new Vector2(1, 0);
        uv[15] = new Vector2(1, 1);

        uv[16] = Vector2.zero;
        uv[17] = new Vector2(0, 1);
        uv[18] = new Vector2(1, 0);
        uv[19] = new Vector2(1, 1);

        uv[20] = Vector2.zero;
        uv[21] = new Vector2(1, 0);
        uv[22] = new Vector2(1, 0.5f);
        uv[23] = new Vector2(0.5f, 1);
        uv[24] = new Vector2(0, 1);

        cornerMesh.vertices = vertices;
        cornerMesh.triangles = triangles;
        cornerMesh.uv = uv;
        cornerMesh.RecalculateNormals();

        packages[0].biomePrefabs[8].GetComponent<MeshFilter>().sharedMesh = cornerMesh;
        packages[0].biomePrefabs[8].GetComponent<MeshRenderer>().sharedMaterial = packages[0].wallMaterial;
        packages[0].biomePrefabs[8].GetComponent<MeshCollider>().sharedMesh = cornerMesh;
    }

    public void GetPackageSubscription(int chunkX, int chunkY, int biomeID = 0)
    {
        for (int i = 0; i < packages[biomeID].biomePrefabs.Count; i++)
        {
            poolManagers[biomeID, i].Subscribe(chunkX, chunkY);
        }
    }
    public void UnSubscribe(int chunkX, int chunkY, int biomeID = 0)
    {
        for (int i = 0; i < packages[biomeID].biomePrefabs.Count; i++)
        {
            poolManagers[biomeID, i].UnSubscribe(chunkX, chunkY);
        }
    }

    public GameObject GetPrefab(int chunkX, int chunkY, PrefabType prefabType, int biomeID = 0)
    {
        return poolManagers[biomeID, (int)prefabType].GetPrefab(chunkX, chunkY);
    }
}


[System.Serializable]
public class PrefabPackage
{
    public string biomeName;
    public List<GameObject> biomePrefabs;

    public Material wallMaterial;
}