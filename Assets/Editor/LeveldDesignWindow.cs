using UnityEngine;
using UnityEditor;
using System;
using UnityEngine.Rendering;
using System.Diagnostics.Eventing.Reader;

public class LeveldDesignWindow : EditorWindow
{
    [MenuItem("Tools/Level Design Editor %#d")]
    public static void OpenWindow()
    {
        GetWindow<LeveldDesignWindow>("Level Editor");
    }

    private enum ShapeType {Room,Corridor};
    private ShapeType typology = ShapeType.Room;
    private int elementNumber = 1;
    private float roomWidth = 5f;
    private float roomDepth = 5f;
    private float corridorWidth = 8f;
    private float corridorLength = 2f;
    private bool randomize=false;

    public GameObject roomPrefab;
    public GameObject corridorPrefab;

    private void OnGUI()
    {
        GUILayout.Label("Create Room or Corridor",EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Set parameters and create layout",MessageType.Info);

        typology = (ShapeType)EditorGUILayout.EnumPopup("Create Shape", typology);
        elementNumber = EditorGUILayout.IntField("Number of " + (typology == ShapeType.Room ? "room" : "corridor"), elementNumber);

        if(elementNumber<1)
        {
            elementNumber = 1;
        }

        if (typology == ShapeType.Room) 
        {
            GUILayout.Label("Room dimension(unity scene unit)", EditorStyles.boldLabel);
            roomWidth = EditorGUILayout.FloatField("Width(X)", roomWidth);
            roomDepth = EditorGUILayout.FloatField("Depth(Z)", roomDepth);
        }

        else 
        {
            GUILayout.Label("Corridor Dimension(unity scene unit)",EditorStyles.boldLabel);
            corridorLength=EditorGUILayout.FloatField("Length(X)",corridorLength);
            corridorWidth = EditorGUILayout.FloatField("Width(Z)", corridorWidth);

        }
        randomize= EditorGUILayout.Toggle(new GUIContent("Randomize dimension/position"),randomize);

        roomPrefab=(GameObject)EditorGUILayout.ObjectField("Room Prefab",roomPrefab, typeof(GameObject),false);
        corridorPrefab= (GameObject)EditorGUILayout.ObjectField("Corridor Prefab",corridorPrefab, typeof(GameObject),false);
        EditorGUILayout.Space();

        if(GUILayout.Button("Create"))
        {
            CreateElements();
        }

        if(GUILayout.Button("Reset"))
        {
            ResetLayout();
        }

        if(GUILayout.Button("Save Layout As Prefab"))
        {
            SaveLayoutPrefab();
        }

    }
    private void CreateElements()
    {
        GameObject previous = GameObject.Find("Generated Layout");

        if (previous != null)
        {
            Undo.DestroyObjectImmediate(previous);
        }

        GameObject parent = new GameObject("Generated Layout");
        Undo.RegisterCreatedObjectUndo(parent, "Generate Layout");

        float baseWidth;
        float baseDepth;

        if (typology == ShapeType.Room)
        {
            baseWidth = roomWidth;
            baseDepth = roomDepth;
        }

        else
        {
            baseWidth = corridorWidth;
            baseDepth = corridorLength;
        }
        float maxDim = Mathf.Max(baseWidth, baseDepth);
        float rangePos = elementNumber * maxDim * 1.5f;

        for (int i = 0; i < elementNumber; i++)
        {
            GameObject newGameObject;

            if (typology == ShapeType.Room)
            {
                if (roomPrefab != null)
                {
                    newGameObject = (GameObject)PrefabUtility.InstantiatePrefab(roomPrefab);
                    
                }
                else
                {
                    newGameObject= GameObject.CreatePrimitive(PrimitiveType.Cube);
                }
                newGameObject.name= "Room"+(i+1);
            }

            else
            {
                if(corridorPrefab != null)
                {
                    newGameObject= (GameObject) PrefabUtility.InstantiatePrefab(corridorPrefab);

                }

                else
                {
                    newGameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                }

                newGameObject.name= "Corridor" + (i+1);
            }

            Undo.RegisterCreatedObjectUndo(newGameObject, "Create Layout");

            newGameObject.transform.SetParent(parent.transform);

            Vector3 position = Vector3.zero;

            if (randomize)
            {
                position.x = UnityEngine.Random.Range(-rangePos, rangePos);
                position.z = UnityEngine.Random.Range(-rangePos, rangePos);
            }
            else
            {
                int columns = Mathf.CeilToInt(Mathf.Sqrt(elementNumber));
                int row = i / columns;
                int column= i% columns;
                position.x = column * (baseWidth + 2f);
                position.z = row * (baseDepth + 2f);


            }

            position.y = 0f;
            newGameObject.transform.position = position;

            float width = baseWidth;
            float depth = baseDepth;

            if(randomize)
            {
                width *= UnityEngine.Random.Range(0.5f,1.5f);
                depth *= UnityEngine.Random.Range(0.5f, 1.5f);
            }

            newGameObject.transform.localScale= new Vector3(width,1f,depth);
        }
       
    }
    private void ResetLayout()
    {
        GameObject previous = GameObject.Find("Generated Layout");
        if (previous != null) 
        { 
          Undo.DestroyObjectImmediate(previous);
        }
    }
    private void SaveLayoutPrefab()
    {
        GameObject layouGameObject = GameObject.Find("Generated Layout");

        if (layouGameObject == null)
        {
            EditorUtility.DisplayDialog("No layout to save", "There isn't any GeneratedLayout in the scene, please create some before saving", "Ok");
            return;
        }

        string path = EditorUtility.SaveFilePanelInProject("Save Layout as Prefab", "LevelLayout", "prefab", "Choose name and folder where you want to save the prefab layout");

        if(string.IsNullOrEmpty(path))
        {
            return;
        }

        bool success;

        PrefabUtility.SaveAsPrefabAsset(layouGameObject, path, out success);

        if (success) 
        {
            Debug.Log("Layout saved with success in the prefab" + path);
        }

        else
        {
            Debug.LogError("Error:Impossible to save prefab layout");
        }

    }

    

    

    
}
