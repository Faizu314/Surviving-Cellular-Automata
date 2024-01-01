using System.Collections.Generic;
using UnityEngine;
using Faizan314.Mathematics.Geometry;

namespace ProceduralChemistry.TessellatedMatter.Reactions
{
    public class Reactor
    {
        private Dictionary<int[], Reactant> reactants = new Dictionary<int[], Reactant>();
        private class ReactionRecords
        {
            private Dictionary<int[][], List<Edge>> records;
            private Dictionary<int[][], float> potentialEnergies;
            public struct Edge
            {
                public int[][] reactantsChemicalSignature;
                public float activationEnergy;
            }

            public void AddRecord(int[][] chemicalSignature, List<Edge> edges)
            {
                records.Add(chemicalSignature, edges);
            }
            public List<Edge> GetRecord(int[][] key)
            {
                return records[key];
            }
            public int[][] GetSystemChemicalSignature(List<Reactant> reactants)
            {
                int[][] chemicalSignatureHash = new int[reactants.Count][];
                for (int i = 0; i < reactants.Count; i++)
                {
                    chemicalSignatureHash[i] = reactants[i].chemicalFormula;
                }
                return chemicalSignatureHash;
            }
            public float GetSystemPotentialEnergy(int[][] chemicalSignature)
            {
                return potentialEnergies[chemicalSignature];
            }
        }
        private Reactant AtomToReactant(Atom atom)
        {
            float[][] edges = new float[1][];
            edges[0] = (float[])atom.edges.Clone();
            float[][] interiorAngles = new float[1][];
            interiorAngles[0] = (float[])atom.interiorAngles.Clone();
            Vector2[][] vertices = new Vector2[1][];
            vertices[0] = (Vector2[])atom.vertices.Clone();
            Vector2[] atomicCentres = new Vector2[1];
            Bond[] bonds = new Bond[0];
            int[] chemicalFormula = new int[1];
            chemicalFormula[0] = atom.atomicNumber;
            Reactant reactant = new Reactant(chemicalFormula, edges, interiorAngles, vertices, atomicCentres, bonds);
            int[] hash = new int[1];
            hash[0] = atom.atomicNumber;
            reactants.Add(hash, reactant);
            return reactant;
        }
        private Reactant MoleculeToReactant(Molecule molecule)
        {
            int[] hash = molecule.chemicalFormula;
            if (reactants.ContainsKey(hash))
                return reactants[hash];
            return default;
        }
        public List<Reactant> React(List<Reactant> reactants)
        {
            //Make a copy of the reactants

            //Loop through all the reactants
            //For each set of reactants:
                //Remove them from the copy
                //Get all the potential bond sites
                //Try the reactions one by one
                //If the reaction is possible:
                    //Add them to the copy
                    //If the copy of the reactants are now not present in the ReactionRecords add them

            List<Reactant> reactantsCopy = new List<Reactant>(reactants);
            for (int i = 0; i < reactants.Count; i++)
            {
                for (int j = i; j < reactants.Count; j++)
                {
                    reactantsCopy.RemoveAt(i);
                    reactantsCopy.RemoveAt(j);
                    int iAtomsCount = reactants[i].chemicalFormula.Length;
                    int jAtomsCount = reactants[j].chemicalFormula.Length;
                    for (int p = 0; p < iAtomsCount; p++)
                    {
                        for (int q = p; q < jAtomsCount; q++)
                        {
                            int pEdgesCount = reactants[i].edges[p].Length;
                            int qEdgesCount = reactants[j].edges[q].Length;
                            for (int e1 = 0; e1 < pEdgesCount; e1++)
                            {
                                for (int e2 = e1; e2 < qEdgesCount; e2++)
                                {
                                    if (e1 != e2)
                                        continue;
                                    List<Reactant> products =
                                        TryReaction(reactants[i], reactants[j], p, e1, q, e2, out float activation, out float potential);
                                    reactantsCopy.AddRange(products);
                                    //If this list of reactants is not present in the reactionRecords then add them and then call
                                    //React(reactantsCopy);
                                    //If they are not present then remove them from reactantsCopy and move on to the next one
                                }
                            }
                        }
                    }
                }
            }

            return null;
        }
        private List<Reactant> TryReaction(Reactant a, Reactant b, int atom1, int edge1, int atom2, int edge2, out float activation, out float potential)
        {
            potential = 0f;
            activation = 0f;
            return null;
        }
        private float GetSystemPotentialEnergy(Reactant a)
        {
            return 0f;
        }
    }
}
