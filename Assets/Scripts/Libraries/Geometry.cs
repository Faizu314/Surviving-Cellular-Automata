using UnityEngine;
using System;
using System.Linq;

namespace Faizan314 
{
    namespace Mathematics
    {
        namespace Geometry
        {
            public static class Polygons
            {
                private static bool AreAllVerticesBelowDiameter(float[] sideLengths)
                {
                    float minRadius = sideLengths.Max() / 2f;
                    float approximation = 0f;
                    for (int i = 0; i < sideLengths.Length; i++)
                    {
                        approximation += Mathf.Asin(sideLengths[i] / (2 * minRadius));
                    }
                    return approximation < Mathf.PI;
                }
                public static float FindRadiusOfCircumscribedCircle(in float[] sideLengths, out bool allVerticesBelowDiameter, float precision = 0.0001f)
                {
                    int ITERATIONS = 0;
                    allVerticesBelowDiameter = AreAllVerticesBelowDiameter(sideLengths);
                    float approximation = 0f;
                    float maxLength = sideLengths.Max();
                    float minRadius = maxLength / 2f;
                    float maxRadius = sideLengths.Sum();
                    float radius = (minRadius + maxRadius) / 2f;
                    while (Mathf.Abs(approximation - Mathf.PI) > precision)
                    {
                        radius = (minRadius + maxRadius) / 2f;
                        approximation = 0f;
                        for (int i = 0; i < sideLengths.Length; i++)
                        {
                            float x = sideLengths[i] / (2f * radius);
                            if (allVerticesBelowDiameter && sideLengths[i] == maxLength)
                                approximation += Mathf.PI - Mathf.Asin(x);
                            else
                                approximation += Mathf.Asin(x);
                        }
                        if (approximation < Mathf.PI ^ allVerticesBelowDiameter)
                            maxRadius = radius;
                        else
                            minRadius = radius;
                        ITERATIONS++;
                        if (minRadius == maxRadius || ITERATIONS >= 1000)
                            break;
                    }
                    Debug.Log("Iterations: " + ITERATIONS);
                    return radius;
                }
            }
        }
    }
}