using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapDataSaver : MonoBehaviour
{
    public static MapDataSaver instance;

    public Dictionary<Vector2, bool[,]> CorrectedTilesDictionary = new Dictionary<Vector2, bool[,]>();
    public Dictionary<Vector2, bool[,]> AutomataTilesDictionary = new Dictionary<Vector2, bool[,]>();
    public Dictionary<Vector2, bool[,]> initialTilesDictionary = new Dictionary<Vector2, bool[,]>();

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(this);
    }

    public void SaveCorrectedTiles(Vector2 chunkPostition, bool[,] tiles)
    {
        CorrectedTilesDictionary[chunkPostition] = tiles;
    }
    public void SaveAutomataTiles(Vector2 chunkPosition, bool[,] tiles)
    {
        //chunk was either new or dirty
        AutomataTilesDictionary[chunkPosition] = tiles;
    }
    public void SaveInitialTiles(Vector2 chunkPosition, bool[,] tiles)
    {
        initialTilesDictionary[chunkPosition] = tiles;
    }
    public bool[,] GetCorrectedTiles(Vector2 chunkPosition)
    {
        if (CorrectedTilesDictionary.ContainsKey(chunkPosition))
        {
            return CorrectedTilesDictionary[chunkPosition];
        }
        else
            return null;
    }
    public bool[,] GetAutomataTiles(Vector2 chunkPosition)
    {
        if (AutomataTilesDictionary.ContainsKey(chunkPosition))
        {
            return AutomataTilesDictionary[chunkPosition];
        }
        else
            return null;
    }
    public bool[,] GetInitialTiles(Vector2 chunkPosition)
    {
        if (initialTilesDictionary.ContainsKey(chunkPosition))
        {
            return initialTilesDictionary[chunkPosition];
        }
        else
            return null;
    }

}
