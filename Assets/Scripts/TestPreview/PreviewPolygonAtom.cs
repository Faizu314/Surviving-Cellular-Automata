using UnityEngine;
using UnityEditor;
using ProceduralChemistry.TessellatedMatter;
using ProceduralChemistry.TessellatedMatter.Elements;
using Faizan314.Mathematics.Geometry;

public class PreviewPolygonAtom : MonoBehaviour
{
    [SerializeField] private int seed;
    [SerializeField] private int fundamentalEdgesCount;
    [SerializeField] private int atomicNumber;

    private PeriodicTable periodicTable;
    private Atom atom;

    private void GenerateAtom()
    {
        periodicTable = new PeriodicTable(fundamentalEdgesCount, seed);
        atom = periodicTable.GenerateAtom(atomicNumber);
    }
    private void GetAtomArea()
    {
        Debug.Log("Area = " + Polygons.GetPolygonArea(atom.vertices));
    }
    private void Update()
    {
        PreviewAtomGeneration();
    }
    private void PreviewAtomGeneration()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            seed = Random.Range(int.MinValue, int.MaxValue);
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            GenerateAtom();
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            GetAtomArea();
        }
    }
    private void OnDrawGizmos()
    {
        if (atom.atomicNumber == 0)
            return;
        Vector2[] points = atom.vertices;
        for (int i = 0; i < points.Length - 1; i++)
        {
            Gizmos.DrawLine(points[i], points[i + 1]);
        }
        Gizmos.DrawLine(points[points.Length - 1], points[0]);
        Handles.DrawWireDisc(Vector3.zero, Vector3.forward, atom.radius);
    }
}
