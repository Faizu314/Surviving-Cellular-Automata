using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapDataSaver : MonoBehaviour
{
    public static MapDataSaver instance;

    public Dictionary<Vector3, bool[,]> allTiles = new Dictionary<Vector3, bool[,]>();

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(this);
    }

    public void SaveTiles(Vector3 ID, bool[,] tiles)
    {
        allTiles[ID] = tiles;
    }

    public bool[,] GetTiles(Vector3 ID)
    {
        if (allTiles.ContainsKey(ID))
        {
            return allTiles[ID];
        }
        else
            return null;
    }

}
