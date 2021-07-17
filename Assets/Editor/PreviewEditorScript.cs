using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PreviewAutomaton))]
public class PreviewEditorScript : Editor
{
    PreviewAutomaton pr;

    private void OnEnable()
    {
        pr = (PreviewAutomaton)target;
        pr.ApplyRules();
        pr._Reset();
        pr.LoadChunks();
        pr.Draw();
    }
    public override void OnInspectorGUI()
    {
        pr = (PreviewAutomaton)target;
        DrawDefaultInspector();
        if (GUILayout.Button("Randomize Rules"))
        {
            pr.RandomizeRules();
        }
        else if (GUILayout.Button("Apply Rules"))
        {
            pr.ApplyRules();
            pr.LoadChunks();
            pr.Draw();
        }
        else if (GUILayout.Button("Reset Offset and Zoom"))
        {
            pr._Reset();
            pr.Draw();
        }
    }
    public void OnSceneGUI()
    {
        Event e = Event.current;
        Vector2 direction = Vector2.zero;
        bool zoomSign = false;
        if (e.type == EventType.KeyDown)
        {
            bool hasScrolled = false;
            bool hasZoomed = false;
            if (e.keyCode == KeyCode.Keypad6)
            {
                direction.x += 1;
                hasScrolled = true;
            }
            if (e.keyCode == KeyCode.Keypad4)
            {
                direction.x += -1;
                hasScrolled = true;
            }
            if (e.keyCode == KeyCode.Keypad8)
            {
                direction.y += 1;
                hasScrolled = true;
            }
            if (e.keyCode == KeyCode.Keypad2)
            {
                direction.y += -1;
                hasScrolled = true;
            }
            if (e.keyCode == KeyCode.Keypad3)
            {
                zoomSign = true;
                hasZoomed = true;
            }
            if (e.keyCode == KeyCode.Keypad1)
            {
                zoomSign = false;
                hasZoomed = true;
            }
            if (hasScrolled)
            {
                pr.Scroll(direction);
            }
            if (hasZoomed)
            {
                pr.Zoom(zoomSign);
            }
            if (hasScrolled || hasZoomed)
            {
                pr.LoadChunks();
                pr.Draw();
            }
        }
    }
}
