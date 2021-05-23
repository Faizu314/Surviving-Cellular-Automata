using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Connector
{
    public static void RandomDirectedWalk(List<PreviewAutomaton.TestFiller> fillers, List<int> fillerIds, bool[,] tileMap)
    {
        PreviewAutomaton.TestFiller baseFiller = fillers[0];
        int[] minDistances = new int[fillerIds.Count - 1];
        minDistances[baseFiller.fillerIndex] = int.MaxValue;
        Vector2[] respectivePoints = new Vector2[fillerIds.Count - 1];

        Vector2 currPos = baseFiller.GetPoint();
        while (fillers.Count != 1)
        {
            //Start from i = 1. 0th index is for baseFiller
            for (int i = 1; i < fillers.Count; i++)
            {
                if (fillerIds[fillers[i].fillerIndex] != fillerIds[baseFiller.fillerIndex])
                {
                    minDistances[i] = (int)Mathf.Abs(currPos.x - fillers[i].GetPoint().x) + (int)Mathf.Abs(currPos.y - fillers[i].GetPoint().y);
                }
            }
        }
    }
}
