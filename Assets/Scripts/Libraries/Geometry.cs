using System.Linq;
using UnityEngine;

namespace Faizan314.Mathematics.Geometry
{
    public static class Polygons
    {
        private static bool AreAllVerticesBelowDiameter_(float[] sideLengths)
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
            allVerticesBelowDiameter = AreAllVerticesBelowDiameter_(sideLengths);
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
        public static bool DoPolygonsIntersect(in Vector2[] polygon1, in Vector2[] polygon2)
        {
            for (int i = 0; i < polygon1.Length; i++)
            {
                Vector2 p1 = polygon1[i];
                Vector2 p2 = i == polygon1.Length - 1 ? polygon1[0] : polygon1[i + 1];
                for (int j = 0; j < polygon2.Length; j++)
                {
                    Vector2 q1 = polygon2[j];
                    Vector2 q2 = j == polygon2.Length - 1 ? polygon2[0] : polygon2[j + 1];
                    if (DoLinesIntersect(p1, p2, q1, q2, out Vector2 intersection))
                        return true;
                }
            }
            return false;
        }
        public static bool DoLinesIntersect(in Vector2 p1, in Vector2 p2, in Vector2 q1, in Vector2 q2, out Vector2 intersection)
        {
            intersection = default;
            if (p1.x < q1.x && p1.x < q2.x && p2.x < q1.x && p2.x < q2.x)
                return false;
            if (p1.y < q1.y && p1.y < q2.y && p2.y < q1.y && p2.y < q2.y)
                return false;
            float m1 = p2.x - p1.x == 0 ? float.MaxValue : (p2.y - p1.y) / (p2.x - p1.x);
            float m2 = q2.x - q1.x == 0 ? float.MaxValue : (q2.y - q1.y) / (q2.x - q1.x);
            if (m1 == m2)
                return false;
            float c1 = p1.y - m1 * p1.x;
            float c2 = q1.y - m2 * q1.x;
            float determinant = m1 - m2;
            intersection = new Vector2((c1 - c2) / determinant, (m1 * c2 - m2 * c1) / determinant);
            if (intersection.x < Mathf.Max(p1.x, p2.x) && intersection.x > Mathf.Min(p1.x, p2.x))
                if (intersection.x < Mathf.Max(q1.x, q2.x) && intersection.x < Mathf.Min(q1.x, q2.x))
                    return true;
            return false;
        }
        public static float GetPolygonArea(in Vector2[] points)
        {
            float area = 0f;
            for (int i = 0; i < points.Length; i++)
            {
                if (i != points.Length - 1)
                    area += GetSignedTriangleArea_(points[i], points[i + 1]);
                else
                    area += GetSignedTriangleArea_(points[i], points[0]);
            }
            return Mathf.Abs(area);
        }
        private static float GetSignedTriangleArea_(Vector2 p1, Vector2 p2)
        {
            return (p1.x * p2.y - p1.y * p2.x) / 2f;
        }
    }
}