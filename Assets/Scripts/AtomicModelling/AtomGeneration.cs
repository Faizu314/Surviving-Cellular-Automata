using System.Collections.Generic;
using UnityEngine;
using Faizan314.Mathematics.Geometry;

namespace ProceduralChemistry.TessellatedMatter.Elements
{
    public class PeriodicTable
    {
        private AtomFactory atomFactory = new AtomFactory();
        private float[] fundamentalEdges;
        private static List<Atom> atoms = new List<Atom>();
        private List<int> atomsGenerated = new List<int>();
        public static int seed;

        public PeriodicTable(int amountOfEdges, int seed)
        {
            PeriodicTable.seed = seed;
            fundamentalEdges = new float[amountOfEdges];
            var prng = new System.Random(seed);
            for (int i = 0; i < amountOfEdges; i++)
            {
                fundamentalEdges[i] = prng.Next(100, 1000) / 1000f;
            }
        }
        public Atom GenerateAtom(int atomicNumber)
        {
            if (fundamentalEdges == null)
                return default;
            if (atomsGenerated.Contains(atomicNumber))
                return default;
            Atom atom = atomFactory.CreateAtom(atomicNumber, fundamentalEdges);
            atoms.Add(atom);
            atomsGenerated.Add(atomicNumber);
            return atom;
        }
        public static float GetAtomCircleRadius(int atomicNumber)
        {
            return atoms[atomicNumber].radius;
        }
    }

    public class AtomFactory
    {
        private int atomicNumber;
        private int N;
        private float[] assignedEdges;
        private float[] edges;
        private float[] interiorAngles; //Radians
        private Vector2[] vertices; //Relative to the circle centre
        private float[] originToVertexAngles; //Degrees
        private Vector2[] Vertices => vertices;
        private float radius;

        public Atom CreateAtom(int atomicNumber, float[] edgeLengths)
        {
            InitializeVariables(atomicNumber);
            AssignEdges(edgeLengths);
            ConstructPolygon();
            return ExtractAtomData();
        }
        private void AssignEdges(float[] edgeLengths)
        {
            var prng = new System.Random(PeriodicTable.seed + atomicNumber);
            List<int> indices = new List<int>();
            for (int i = 0; i < edgeLengths.Length; i++)
                indices.Add(i);
            assignedEdges[0] = edgeLengths[prng.Next(0, edgeLengths.Length)];
            assignedEdges[1] = edgeLengths[prng.Next(0, edgeLengths.Length)];
            float sum = Mathf.Min(assignedEdges[0], assignedEdges[1]);
            float largestEdge;
            int assignedEdgeIndex = 2;
            Queue<int> currentlyInvalid = new Queue<int>();
            while (assignedEdgeIndex < N)
            {
                while (true)
                {
                    largestEdge = Mathf.Max(assignedEdges);
                    int index = indices[prng.Next(0, indices.Count)];
                    float currentEdge = edgeLengths[index];
                    if (currentEdge <= largestEdge)
                    {
                        if (largestEdge < sum + currentEdge)
                        {
                            assignedEdges[assignedEdgeIndex] = currentEdge;
                            sum += currentEdge;
                            break;
                        }
                    }
                    else if (currentEdge < sum + largestEdge)
                    {
                        assignedEdges[assignedEdgeIndex] = currentEdge;
                        sum += largestEdge;
                        break;
                    }
                    currentlyInvalid.Enqueue(index);
                    indices.Remove(index);
                }
                assignedEdgeIndex++;
                while (currentlyInvalid.Count != 0)
                    indices.Add(currentlyInvalid.Dequeue());
            }
        }
        private void InitializeVariables(int atomicNumber)
        {
            this.atomicNumber = atomicNumber;
            N = atomicNumber + 2;
            assignedEdges = new float[N];
            edges = new float[N];
            interiorAngles = new float[N];
            vertices = new Vector2[N];
            originToVertexAngles = new float[N];
        }
        private void ConstructPolygon()
        {
            radius = Polygons.FindRadiusOfCircumscribedCircle(assignedEdges, out bool allVerticesBelowDiameter, 0.0001f);
            float currentOriginToVertexAngle = 270f * Mathf.Deg2Rad;
            float maxLength = Mathf.Max(assignedEdges);
            float chordAngle;
            for (int i = 0; i < N; i++)
            {
                chordAngle = 2 * Mathf.Asin(assignedEdges[i] / (2 * radius));
                chordAngle = allVerticesBelowDiameter && assignedEdges[i] == maxLength ? 2 * Mathf.PI - chordAngle : chordAngle;
                currentOriginToVertexAngle += chordAngle / (i == 0 ? 2f : 1f);
                originToVertexAngles[i] = currentOriginToVertexAngle;
                vertices[i] = new Vector2(Mathf.Cos(originToVertexAngles[i]), Mathf.Sin(originToVertexAngles[i])) * radius;
            }
            for (int i = 0; i < N; i++)
            {
                int prev = (i - 1 + N) % N;
                int next = (i + 1) % N;
                interiorAngles[i] = Vector2.Angle(vertices[prev] - vertices[i], vertices[next] - vertices[i]);
                edges[i] = Vector2.Distance(vertices[prev], vertices[i]);
            }
        }
        private Atom ExtractAtomData()
        {
            Atom atom = new Atom(atomicNumber, assignedEdges, interiorAngles, Vertices, radius);
            return atom;
        }
    }
}