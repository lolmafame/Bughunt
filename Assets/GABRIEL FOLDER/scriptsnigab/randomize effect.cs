using UnityEngine;
using UnityEditor;

public class RandomizeTransforms : EditorWindow
{
    float minScale = 0.5f;
    float maxScale = 2f;
    float verticalDisplacement = 5f;
    bool randomizeRotation = true;
    bool randomizeScale = true;
    bool randomizeVertical = true;

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

        GUILayout.Space(5);
        GUILayout.Label("Vertical Displacement");
        verticalDisplacement = EditorGUILayout.FloatField("Max Up/Down Units", verticalDisplacement);

        GUILayout.Space(5);
        GUILayout.Label("Options");
        randomizeRotation = EditorGUILayout.Toggle("Random Rotation (X Y Z)", randomizeRotation);
        randomizeScale = EditorGUILayout.Toggle("Random Scale", randomizeScale);
        randomizeVertical = EditorGUILayout.Toggle("Random Vertical Displacement", randomizeVertical);

        GUILayout.Space(10);
        if (GUILayout.Button("Randomize!"))
        {
            foreach (GameObject obj in Selection.gameObjects)
            {
                Undo.RecordObject(obj.transform, "Randomize Transform");

                if (randomizeRotation)
                {
                    float rx = Random.Range(0f, 360f);
                    float ry = Random.Range(0f, 360f);
                    float rz = Random.Range(0f, 360f);
                    obj.transform.rotation = Quaternion.Euler(rx, ry, rz);
                }

                if (randomizeScale)
                {
                    float s = Random.Range(minScale, maxScale);
                    obj.transform.localScale = new Vector3(s, s, s);
                }

                if (randomizeVertical)
                {
                    Vector3 pos = obj.transform.position;
                    pos.y += Random.Range(-verticalDisplacement, verticalDisplacement);
                    obj.transform.position = pos;
                }
            }
        }
    }
}