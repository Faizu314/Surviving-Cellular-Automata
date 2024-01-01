using UnityEngine;

namespace ProceduralChemistry.SubAtomicParticles
{
        public static class ParticleModel
        {
            public static SubAtomicParticle[] particles;
            public static float[,] forces;
            public static float[] masses;
            public static float[] radii;
        }

        public static class PeriodicTable
        {
            public static Compound[] elements;
        }

        public struct Compound
        {
            public float density;
            public float malleability;
            public float strength;
            public float magnetism;
            public float reactivity;
            public Color color;
        }

        public struct Shell
        {
            public Sublevel[] sublevels;
        }

        public struct Sublevel
        {
            public int orbitalsCount;
            public float radius;
        }

        public struct SubAtomicParticle
        {
            public float charge;
            public float size;
            public float spin;
        }
}