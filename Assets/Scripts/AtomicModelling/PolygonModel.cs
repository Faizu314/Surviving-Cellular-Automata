using System.Collections.Generic;
using UnityEngine;
using Faizan314.Mathematics.Geometry;

namespace ProceduralChemistry
{
    namespace TessellatedMatter
    {
        public class PeriodicTable
        {
            private AtomFactory atomFactory = new AtomFactory();
            private float[] fundamentalEdges;
            private List<Atom> atoms = new List<Atom>();
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

        public class Reactor
        {
            private Dictionary<int, Reactant> reactants = new Dictionary<int, Reactant>();
            public Reactant AtomToReactant(Atom atom)
            {
                float[][] edges = new float[1][];
                edges[0] = (float[])atom.edges.Clone();
                float[][] interiorAngles = new float[1][];
                interiorAngles[0] = (float[])atom.interiorAngles.Clone();
                Vector2[][] vertices = new Vector2[1][];
                vertices[0] = (Vector2[])atom.vertices.Clone();
                Vector2[] atomicCentres = new Vector2[1];
                Bond[] bonds = new Bond[0];
                Reactant reactant = new Reactant(edges, interiorAngles, vertices, atomicCentres, bonds);
                reactants.Add(atom.atomicNumber, reactant);
                return reactant;
            }
            public Reactant MoleculeToReactant(Molecule molecule)
            {
                int hash = ChemicalFormulaHash(molecule.chemicalFormula);
                if (reactants.ContainsKey(hash))
                    return reactants[hash];
                return default;
            }
            private int ChemicalFormulaHash(in int[] chemicalFormula)
            {
                int hash = 0;
                for (int i = 0; i < chemicalFormula.Length; i++)
                {
                    hash *= 10;
                    hash += chemicalFormula[i];
                }
                return hash;
            }
            public void React(Reactant a, Reactant b)
            {
                

            }
            private List<Reactant> TryReaction(Reactant a, Reactant b, out float[] potentials)
            {

            }
            private float GetBondPotential(Reactant a, Reactant b, int edge1, int edge2)
            {

            }
        }
        public struct Atom
        {
            public readonly int atomicNumber;
            public readonly float[] edges;
            public readonly float[] interiorAngles;
            public readonly Vector2[] vertices;
            public readonly float radius;
            public Atom(int atomicNumber, float[] edges, float[] interiorAngles, Vector2[] vertices, float radius)
            {
                int N = atomicNumber + 2;
                this.atomicNumber = atomicNumber;
                this.radius = radius;
                this.edges = this.interiorAngles = new float[N];
                this.vertices = new Vector2[N];
                for (int i = 0; i < N; i++)
                {
                    this.edges[i] = edges[i];
                    this.interiorAngles[i] = interiorAngles[i];
                    this.vertices[i] = vertices[i];
                }
            }
        }
        public struct Molecule
        {
            public readonly int[] chemicalFormula;
            public readonly float[] edges;
            public readonly float[] interiorAngles;
            public readonly Vector2[] vertices;
        }
        public struct Reactant
        {
            public readonly float[][] edges;
            public readonly float[][] interiorAngles;
            public readonly Vector2[][] vertices;
            public readonly Vector2[] atomicCentres;
            public readonly Bond[] bonds;
            public Reactant(float[][] edges, float[][] interiorAngles, Vector2[][] vertices, Vector2[] atomicCentres, Bond[] bonds)
            {
                this.edges = edges;
                this.interiorAngles = interiorAngles;
                this.vertices = vertices;
                this.atomicCentres = atomicCentres;
                this.bonds = bonds;
            }
        }
        public struct Bond
        {
            public readonly int atom1;
            public readonly int edge1;
            public readonly int atom2;
            public readonly int edge2;
            public readonly float potential;
            public Bond(int a1, int e1, int a2, int e2, float potential)
            {
                atom1 = a1;
                edge1 = e1;
                atom2 = a2;
                edge2 = e2;
                this.potential = potential;
            }
        }
    }
}