using UnityEngine;
using UnityEditor;

namespace AK.MapEditorTools
{
    [CustomEditor(typeof(MapEditor))]
    public class MapEditorInspector : Editor
    {
        private MapEditor mapEditor;
        private Material defaultRoadMaterial;
        private Material defaultTerrainMaterial;

        private void OnEnable()
        {
            mapEditor = (MapEditor)target;
            
            // You can set default materials here or leave them null
            // defaultRoadMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Path/To/DefaultRoadMaterial.mat");
            // defaultTerrainMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Path/To/DefaultTerrainMaterial.mat");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Road Management", EditorStyles.boldLabel);

            if (GUILayout.Button("Add New Road"))
            {
                string roadName = "Road " + (mapEditor.GetAllRoads().Length + 1);
                RoadEditor newRoad = mapEditor.CreateNewRoad(roadName, defaultRoadMaterial, defaultTerrainMaterial);
                Selection.activeGameObject = newRoad.gameObject;
            }

            if (GUILayout.Button("Remove All Roads"))
            {
                if (EditorUtility.DisplayDialog("Remove All Roads", 
                    "Are you sure you want to delete all roads?", "Yes", "No"))
                {
                    Undo.RecordObject(mapEditor, "Remove All Roads");
                    mapEditor.RemoveAllRoads();
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Existing Roads", EditorStyles.boldLabel);
            
            RoadEditor[] roads = mapEditor.GetAllRoads();
            if (roads.Length == 0)
            {
                EditorGUILayout.HelpBox("No roads created yet. Click 'Add New Road' to create one.", MessageType.Info);
            }
            else
            {
                foreach (var road in roads)
                {
                    EditorGUILayout.BeginHorizontal();
                    
                    EditorGUILayout.ObjectField(road, typeof(RoadEditor), false);
                    
                    if (GUILayout.Button("Edit", GUILayout.Width(60)))
                    {
                        Selection.activeGameObject = road.gameObject;
                    }
                    
                    if (GUILayout.Button("Delete", GUILayout.Width(60)))
                    {
                        if (EditorUtility.DisplayDialog("Delete Road", 
                            "Are you sure you want to delete this road?", "Yes", "No"))
                        {
                            Undo.DestroyObjectImmediate(road.gameObject);
                            EditorGUILayout.EndHorizontal();
                            break;
                        }
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
            }
        }
    }
}
