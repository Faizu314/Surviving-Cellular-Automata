using UnityEngine;
using ProceduralChemistry.SubAtomicParticles;
using PhysicsSimulation;

public class PhysicsSimulationPreview : MonoBehaviour
{
    [Header("Particle Model")]
    [SerializeField] private float[] radii;
    [SerializeField] private float[] masses;
    [SerializeField] private ToSerialize[] forces;

    [Header("Initial Conditions")]
    [SerializeField] private int particleCount;
    [SerializeField] private int[] particleTypes;
    [SerializeField] private Vector3[] initialPositions;

    [Header("Graphics")]
    [SerializeField] private Transform[] particles;

    [Header("Simulation Settings")]
    [SerializeField] [Range(0.01f, 10f)] private float speed;
    [SerializeField] [Range(0.01f, 1f)] private float tickDeltaTime;

    private float deltaTime = 0f;
    private Vector3[] positions;

    private void Start()
    {
        ParticleModel.radii = radii;
        ParticleModel.masses = masses;
        float[,] forcesArray = new float[radii.Length, radii.Length];
        for (int i = 0; i < radii.Length; i++)
            for (int j = 0; j < radii.Length; j++)
                forcesArray[i, j] = forces[i].element[j];
        ParticleModel.forces = forcesArray;
        for (int i = 0; i < particles.Length; i++)
            particles[i].position = initialPositions[i];
        SubAtomicPhysics.Initialize(particleTypes, initialPositions, particleCount);
        positions = SubAtomicPhysics.GetPositions();
    }
    private void Update()
    {
        deltaTime += Time.deltaTime;
        if (deltaTime >= tickDeltaTime / speed)
        {
            SubAtomicPhysics.Step(tickDeltaTime);
            for (int i = 0; i < particles.Length; i++)
                particles[i].position = positions[i];
            deltaTime = 0f;
        }
    }
}

[System.Serializable] public struct ToSerialize
{
    public float[] element;
}