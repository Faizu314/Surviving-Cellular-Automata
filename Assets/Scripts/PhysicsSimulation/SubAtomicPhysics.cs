using UnityEngine;
using ProceduralChemistry.SubAtomicParticles;

namespace PhysicsSimulation
{
    public static class SubAtomicPhysics
    {
        private static int[] particleTypes;
        private static Vector3[] positions;
        private static Vector3[] velocities;

        public static void Initialize(int[] particleTypes, Vector3[] positions, Vector3[] velocities, int particleCount)
        {
            SubAtomicPhysics.particleTypes = new int[particleCount];
            SubAtomicPhysics.positions = new Vector3[particleCount];
            SubAtomicPhysics.velocities = new Vector3[particleCount];

            for (int i = 0; i < particleTypes.Length; i++)
            {
                SubAtomicPhysics.particleTypes[i] = particleTypes[i];
                SubAtomicPhysics.positions[i] = positions[i];
                SubAtomicPhysics.velocities[i] = velocities[i];
            }
        }
        public static void Initialize(int[] particleTypes, Vector3[] positions, int particleCount)
        {
            SubAtomicPhysics.particleTypes = new int[particleCount];
            SubAtomicPhysics.positions = new Vector3[particleCount];
            SubAtomicPhysics.velocities = new Vector3[particleCount];

            for (int i = 0; i < particleTypes.Length; i++)
            {
                SubAtomicPhysics.particleTypes[i] = particleTypes[i];
                SubAtomicPhysics.positions[i] = positions[i];
            }
        }
        public static Vector3[] GetPositions()
        {
            return positions;
        }

        public static void Step(int stepCount, float stepDeltaTime)
        {
            for (int step = 0; step < stepCount; step++)
            {
                for (int i = 0; i < particleTypes.Length; i++)
                {
                    Vector3 acceleration = GetAccelerationOnParticle(i);
                    Vector3 oldPosition = positions[i];

                    velocities[i] += acceleration * stepDeltaTime;
                    positions[i] += velocities[i] * stepDeltaTime;

                    if (DoesParticleCollide(i, out int colliderID))
                        positions[i] = oldPosition;
                }
            }
        }
        public static void Step(float stepDeltaTime)
        {
            for (int i = 0; i < particleTypes.Length; i++)
            {
                Vector3 acceleration = GetAccelerationOnParticle(i);
                Vector3 oldPosition = positions[i];

                velocities[i] += acceleration * stepDeltaTime;
                positions[i] += velocities[i] * stepDeltaTime;

                int colliderID;
                if (DoesParticleCollide(i, out colliderID))
                {
                    positions[i] = oldPosition;
                    velocities[i] = Vector3.zero;
                    Vector3 forceTransferred = acceleration * ParticleModel.masses[particleTypes[i]];
                    velocities[colliderID] += (forceTransferred / ParticleModel.masses[particleTypes[colliderID]]) * stepDeltaTime;
                }
            }
        }
        private static void CollideParticles(int a_particleID, int b_particleID)
        {

        }
        private static Vector3 GetAccelerationOnParticle(int particleID)
        {
            Vector3 acceleration = Vector3.zero;
            for (int j = 0; j < particleTypes.Length; j++)
                if (j != particleID)
                    acceleration += GetAccelerationDueToForce(particleID, GetForceBetweenParticles(particleID, j));
            return acceleration;
        }
        private static bool DoesParticleCollide(int particleID, out int colliderID)
        {
            for (int j = 0; j < particleTypes.Length; j++)
            {
                if (j != particleID && AreParticlesColliding(particleID, j))
                {
                    colliderID = j;
                    return true;
                }
            }
            colliderID = -1;
            return false;
        }

        private static Vector3 GetForceBetweenParticles(int a_particleID, int b_particleID)
        {
            float distance = Vector3.Distance(positions[a_particleID], positions[b_particleID]);
            float forceStrength = ParticleModel.forces[particleTypes[a_particleID], particleTypes[b_particleID]];
            Vector3 forceDir = positions[b_particleID] - positions[a_particleID];
            return forceDir.normalized * forceStrength / (distance * distance);
        }
        private static Vector3 GetAccelerationDueToForce(int particleID, Vector3 force)
        {
            return force / ParticleModel.masses[particleTypes[particleID]];
        }
        private static bool AreParticlesColliding(int a_particleID, int b_particleID)
        {
            float sumOfRadiiSqr = ParticleModel.radii[particleTypes[a_particleID]] + ParticleModel.radii[particleTypes[b_particleID]];
            sumOfRadiiSqr *= sumOfRadiiSqr;
            float sqrDistance = Vector3.SqrMagnitude(positions[a_particleID] - positions[b_particleID]);
            return sqrDistance <= sumOfRadiiSqr;
        }
    }
}