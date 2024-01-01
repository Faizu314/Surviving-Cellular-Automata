using UnityEngine;

namespace ProceduralChemistry.TessellatedMatter
{
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
        public readonly int[] chemicalFormula;
        public readonly float[][] edges;
        public readonly float[][] interiorAngles;
        public readonly Vector2[][] vertices;
        public readonly Vector2[] atomicCentres;
        public readonly Bond[] bonds;
        public Reactant(int[] chemicalFormula, float[][] edges, float[][] interiorAngles, Vector2[][] vertices, Vector2[] atomicCentres, Bond[] bonds)
        {
            this.chemicalFormula = chemicalFormula;
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
        public readonly float strength;
        public Bond(int a1, int e1, int a2, int e2, float strength)
        {
            atom1 = a1;
            edge1 = e1;
            atom2 = a2;
            edge2 = e2;
            this.strength = strength;
        }
    }
}