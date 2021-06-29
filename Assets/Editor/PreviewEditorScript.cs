using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PreviewAutomaton))]
public class PreviewEditorScript : Editor
{
    public override void OnInspectorGUI()
    {
        PreviewAutomaton pr = (PreviewAutomaton)target;
        DrawDefaultInspector();
        if (pr.autoUpdate)
        {
            pr.Preview();
        }
        else if (GUILayout.Button("Populate Rules"))
        {
            pr.PopulateRules();
        }
        else if (GUILayout.Button("Test"))
        {
            pr.Preview();
        }
    }
}
