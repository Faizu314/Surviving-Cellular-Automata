using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplorationSaver : MonoBehaviour
{
    [SerializeField] private Transform player;
    private Dictionary<Vector2, bool[,]> exploredMap;

    public void placeholdername(Vector2 chunkPos, int minX, int maxX, int minY, int maxY)
    {
        // Exploration window will be a subset of the Render window
        // It will also be a square
        // Shrink the bounds to get the exploration window :)
    }
}
