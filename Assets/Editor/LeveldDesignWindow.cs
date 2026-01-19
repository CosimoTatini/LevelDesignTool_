using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;

public class LeveldDesignWindow : EditorWindow
{
    [MenuItem("Tools/Level Design Editor %#d")]
    public static void OpenWindow()
    {
        GetWindow<LeveldDesignWindow>("Level Editor");
    }

    private enum ShapeType { Room, Corridor };
    private ShapeType typology = ShapeType.Room;
    private int elementNumber = 1;

    // Dimensioni di default
    private float roomWidth = 5f;
    private float roomDepth = 5f;
    private float corridorWidth = 2f;  // X axis (solitamente stretto)
    private float corridorLength = 8f; // Z axis (solitamente lungo)

    private bool randomize = false;
    private enum RotationType { Fixed, Randomic, North_0, East_90, South_180, West_270 }

    private RotationType rotationType = RotationType.Fixed;

    private bool snapToGrid = false;
    private float gridSize = 1f;

    private int floorNumbers = 1;
    private float floorsOffsetY = 3f;

    public GameObject roomPrefab;
    public GameObject corridorPrefab;

    private void OnGUI()
    {
        GUILayout.Label("Create Room or Corridor", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Set parameters and create layout", MessageType.Info);

        typology = (ShapeType)EditorGUILayout.EnumPopup("Create Shape", typology);
        elementNumber = EditorGUILayout.IntField("Number of " + (typology == ShapeType.Room ? "room" : "corridor"), elementNumber);

        if (elementNumber < 1)
        {
            elementNumber = 1;
        }

        // --- SEZIONE DIMENSIONI CORRETTA ---
        if (typology == ShapeType.Room)
        {
            GUILayout.Label("Room Dimensions (Local Scale)", EditorStyles.boldLabel);
            roomWidth = EditorGUILayout.FloatField("Width (X Axis)", roomWidth);
            roomDepth = EditorGUILayout.FloatField("Depth (Z Axis)", roomDepth);
        }
        else
        {
            GUILayout.Label("Corridor Dimensions (Local Scale)", EditorStyles.boldLabel);
            // Corretto: La larghezza è X, la lunghezza è Z
            corridorWidth = EditorGUILayout.FloatField("Width (X Axis)", corridorWidth);
            corridorLength = EditorGUILayout.FloatField("Length (Z Axis)", corridorLength);
        }
        // ------------------------------------

        randomize = EditorGUILayout.Toggle(new GUIContent("Randomize dimension/position"), randomize);
        EditorGUILayout.Space();
        GUILayout.Label("Multi-Level Settings", EditorStyles.boldLabel);
        floorNumbers = EditorGUILayout.IntField("Number of floors", floorNumbers);

        if(floorNumbers < 1)
        {
            floorNumbers = 1;
        }
        if(floorNumbers>1)
        {
            floorsOffsetY=EditorGUILayout.FloatField("Floor offset",floorsOffsetY);
            if (floorsOffsetY < 0.1f) floorsOffsetY = 0.1f;
        }


        EditorGUILayout.Space();
        GUILayout.Label("Rotation Settings", EditorStyles.boldLabel);
        rotationType = (RotationType)EditorGUILayout.EnumPopup(new GUIContent("Rotation Type", "Choose how object should be rotated"), rotationType);

        string rotationInfo = "";

        switch (rotationType)
        {
            case RotationType.Fixed:
                rotationInfo = "All objects will have 0 degrees rotation";
                break;
            case RotationType.Randomic:
                rotationInfo = "Objects will have a random rotation between 0 and 360";
                break;
            case RotationType.North_0:
                rotationInfo = "Objects will face North (0°)";
                break;
            case RotationType.East_90:
                rotationInfo = "Objects will face East (+90°)";
                break;
            case RotationType.South_180:
                rotationInfo = "Objects will face South (+180°)";
                break;
            case RotationType.West_270:
                rotationInfo = "Objects will face West (+270°)";
                break;
        }
        EditorGUILayout.HelpBox(rotationInfo, MessageType.None);


        EditorGUILayout.Space();
        GUILayout.Label("Grid Settings", EditorStyles.boldLabel);
        snapToGrid = EditorGUILayout.Toggle(new GUIContent("Snap To Grid", "Enable snapping objects to grid"), snapToGrid);

        if (snapToGrid)
        {
            gridSize = EditorGUILayout.FloatField(new GUIContent("Grid Size", "Size of the Grid Cells"), gridSize);

            if (gridSize < 0.1f)
            {
                gridSize = 0.1f;
            }
            EditorGUILayout.HelpBox($"Objects will snap to multiples of {gridSize} units", MessageType.None);
        }


        roomPrefab = (GameObject)EditorGUILayout.ObjectField("Room Prefab", roomPrefab, typeof(GameObject), false);
        corridorPrefab = (GameObject)EditorGUILayout.ObjectField("Corridor Prefab", corridorPrefab, typeof(GameObject), false);
        EditorGUILayout.Space();

        if (GUILayout.Button("Create"))
        {
            CreateElements();
        }

        if (GUILayout.Button("Reset"))
        {
            ResetLayout();
        }

        if (GUILayout.Button("Save Layout As Prefab"))
        {
            SaveLayoutPrefab();
        }

        EditorGUILayout.Space();
        GUILayout.Label("Alignement Tools", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Select object in the generated layout to align them", MessageType.Info);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Align X"))
        {
            AlignObjects(true, false);
        }

        if (GUILayout.Button("Align Z"))
        {
            AlignObjects(false, true);
        }

        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Distribute Evenly"))
        {
            DistributeObjects();
        }

    }

    private void AlignObjects(bool alignX, bool alignZ)
    {
        GameObject layoutParent = GameObject.Find("Generated Layout");

        if (layoutParent == null)
        {
            EditorUtility.DisplayDialog("No layout found", "No layout in scene, create it please", "Ok");
            return;
        }



        List<Transform> allChildrens= new List<Transform>();

        foreach (Transform floor in layoutParent.transform)
        {
            if(floor.name.StartsWith("Floor_"))
            {
                foreach(Transform child in floor)
                {
                    allChildrens.Add(child);
                }

            }
        }
        Transform[] childrens= allChildrens.ToArray();



        if (childrens.Length == 0)
        {
            EditorUtility.DisplayDialog("No Objects", "The Generated Layout has no children to align.", "OK");
            return;
        }

        float averageX = 0f;
        float averageZ = 0f;

        foreach (Transform child in childrens)
        {
            averageX += child.position.x;
            averageZ += child.position.z;
        }

        averageX /= childrens.Length;
        averageZ /= childrens.Length;

        Undo.RecordObjects(childrens, "Align Objects");

        foreach (Transform child in childrens)
        {
            Vector3 newPosition = child.position;

            if (alignX)
            {
                newPosition.x = averageX;
            }

            if (alignZ)
            {
                newPosition.z = averageZ;
            }

            newPosition = SnaptoGrid(newPosition);
            child.position = newPosition;
        }
        Debug.Log($"Aligned {childrens.Length} objects. Avg X: {averageX:F2}, Avg Z: {averageZ:F2}");
    }

    private void DistributeObjects()
    {
        GameObject layoutParent = GameObject.Find("Generated Layout");

        if (layoutParent == null)
        {
            EditorUtility.DisplayDialog("No Layout Found", "There is no Generated Layout in the scene. Please create one first.", "OK");
            return;
        }

        List<Transform> allChildren= new List<Transform>();

        foreach(Transform floor in layoutParent.transform)
        {
            if (floor.name.StartsWith("Floor_"))
            {
                foreach(Transform child in floor)
                {
                    allChildren.Add(child);
                }

            }
        }
        Transform[] childrens= allChildren.ToArray();

        for (int i = 0; i < layoutParent.transform.childCount; i++)
        {
            childrens[i] = layoutParent.transform.GetChild(i);
        }

        if (childrens.Length < 2)
        {
            EditorUtility.DisplayDialog("Not Enough Objects", "Need at least 2 objects to distribute.", "OK");
            return;
        }

        System.Array.Sort(childrens, (a, b) => a.position.x.CompareTo(b.position.x));

        float minX = childrens[0].position.x;
        float maxX = childrens[childrens.Length - 1].position.x;

        float totalDistance = maxX - minX;
        float spacing = totalDistance / (childrens.Length - 1);

        Undo.RecordObjects(childrens, "Distribute Objects");

        for (int i = 0; i < childrens.Length; i++)
        {
            Vector3 newPosition = childrens[i].position;
            newPosition.x = minX + (i * spacing);
            newPosition = SnaptoGrid(newPosition);
            childrens[i].position = newPosition;

        }
        Debug.Log($"Distributed {childrens.Length} objects with spacing: {spacing:F2} units");
    }

    /// <summary>
    /// Create the grid via Mathf.Round
    /// </summary>
    private Vector3 SnaptoGrid(Vector3 position)
    {
        if (snapToGrid)
        {
            position.x = Mathf.Round(position.x / gridSize) * gridSize;
            position.z = Mathf.Round(position.z / gridSize) * gridSize;
        }
        return position;
    }

    private Quaternion GetRotation()
    {
        float yRotation = 0f;

        switch (rotationType)
        {
            case RotationType.Fixed:
                yRotation = 0f;
                break;
            case RotationType.Randomic:
                yRotation = UnityEngine.Random.Range(0f, 360f);
                break;
            case RotationType.North_0:
                yRotation = 0f;
                break;
            case RotationType.East_90:
                yRotation = 90f;
                break;
            case RotationType.South_180:
                yRotation = -180f;
                break;
            case RotationType.West_270:
                yRotation = yRotation+270f;
                break;
        }
        return Quaternion.Euler(0, yRotation, 0);
    }

    /// <summary>
    /// Create elements in the scene, also in a grid
    /// </summary>
    private void CreateElements()
    {
        GameObject previous = GameObject.Find("Generated Layout");

        if (previous != null)
        {
            Undo.DestroyObjectImmediate(previous);
        }

        GameObject parent = new GameObject("Generated Layout");
        Undo.RegisterCreatedObjectUndo(parent, "Generate Layout");

        for( int floorIndex=0; floorIndex<floorNumbers;floorIndex++)
        {
            GameObject floorParent = new GameObject($"Floor_{floorIndex + 1}");
            floorParent.transform
        }

        float baseWidth;
        float baseDepth;

        // Logica corretta: Width va sempre su X, Depth/Length va sempre su Z
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

        // Calcolo per lo spacing in modalità griglia
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
                    newGameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                }
                newGameObject.name = "Room" + (i + 1);
            }
            else
            {
                if (corridorPrefab != null)
                {
                    newGameObject = (GameObject)PrefabUtility.InstantiatePrefab(corridorPrefab);
                }
                else
                {
                    newGameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                }
                newGameObject.name = "Corridor" + (i + 1);
            }

            Undo.RegisterCreatedObjectUndo(newGameObject, "Create Layout");
            newGameObject.transform.SetParent(parent.transform);

            // 1. ROTAZIONE: La applichiamo subito
            newGameObject.transform.rotation = GetRotation();

            // 2. POSIZIONE
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
                int column = i % columns;
                // Spaziamo in base alle dimensioni originali + un piccolo margine
                position.x = column * (maxDim + 2f);
                position.z = row * (maxDim + 2f);
            }

            position.y = 0f;
            position = SnaptoGrid(position);
            newGameObject.transform.position = position;

            // 3. DIMENSIONE (LOCAL SCALE)
            // Applichiamo la dimensione locale. Essendo locale, se l'oggetto è ruotato,
            // la larghezza (Width) seguirà l'asse rosso locale dell'oggetto.
            float width = baseWidth;
            float depth = baseDepth;

            if (randomize)
            {
                width *= UnityEngine.Random.Range(0.5f, 1.5f);
                depth *= UnityEngine.Random.Range(0.5f, 1.5f);
            }

            newGameObject.transform.localScale = new Vector3(width, 1f, depth);
        }
    }

    /// <summary>
    /// Clear the scene
    /// </summary>
    private void ResetLayout()
    {
        GameObject previous = GameObject.Find("Generated Layout");
        if (previous != null)
        {
            Undo.DestroyObjectImmediate(previous);
        }
    }

    /// <summary>
    /// Create a .asset of the room or corridor
    /// </summary>
    private void SaveLayoutPrefab()
    {
        GameObject layouGameObject = GameObject.Find("Generated Layout");

        if (layouGameObject == null)
        {
            EditorUtility.DisplayDialog("No layout to save", "There isn't any GeneratedLayout in the scene, please create some before saving", "Ok");
            return;
        }

        string path = EditorUtility.SaveFilePanelInProject("Save Layout as Prefab", "LevelLayout", "prefab", "Choose name and folder where you want to save the prefab layout");

        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        bool success;
        PrefabUtility.SaveAsPrefabAsset(layouGameObject, path, out success);

        if (success)
        {
            Debug.Log("Layout saved with success in the prefab " + path);
        }
        else
        {
            Debug.LogError("Error: Impossible to save prefab layout");
        }
    }
}