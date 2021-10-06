using UnityEngine;
using ProceduralChemistry.TessellatedMatter;
using UnityEditor;

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
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            GenerateAtom();
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
