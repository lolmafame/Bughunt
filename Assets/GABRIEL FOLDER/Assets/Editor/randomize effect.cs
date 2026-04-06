using UnityEngine;
using UnityEditor;

public class RandomizeTransforms : EditorWindow
{
    float minScale = 0.5f;
    float maxScale = 2f;
    bool randomizeRotation = true;
    bool randomizeScale = true;

    [MenuItem("Tools/Randomize Selected Objects")]
    public static void ShowWindow()
    {
        GetWindow<RandomizeTransforms>("Randomize");
    }

    void OnGUI()
    {
        GUILayout.Label("Scale Range");
        minScale = EditorGUILayout.FloatField("Min Scale", minScale);
        maxScale = EditorGUILayout.FloatField("Max Scale", maxScale);
        randomizeRotation = EditorGUILayout.Toggle("Random Rotation (Y)", randomizeRotation);
        randomizeScale = EditorGUILayout.Toggle("Random Scale", randomizeScale);

        if (GUILayout.Button("Randomize!"))
        {
            foreach (GameObject obj in Selection.gameObjects)
            {
                Undo.RecordObject(obj.transform, "Randomize Transform");

                if (randomizeRotation)
                    obj.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

                if (randomizeScale)
                {
                    float s = Random.Range(minScale, maxScale);
                    obj.transform.localScale = new Vector3(s, s, s);
                }
            }
        }
    }
}