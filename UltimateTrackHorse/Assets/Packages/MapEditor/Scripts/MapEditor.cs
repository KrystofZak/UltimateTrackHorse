using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace AK.MapEditorTools
{
    [ExecuteInEditMode]
    public class MapEditor : MonoBehaviour
    {
        // Keep only road management functionality
        private void OnEnable() 
        {
            #if UNITY_EDITOR
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
            #endif
        }

        private void OnDisable() 
        {
            #if UNITY_EDITOR
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
            #endif
        }

        #if UNITY_EDITOR
        private void OnUndoRedoPerformed() => NotifyRoadGeneratorsToUpdate();

        public void NotifyRoadGeneratorsToUpdate()
        {
            foreach (var roadEditor in GetComponentsInChildren<RoadEditor>())
            {
                roadEditor.UpdateRoadGenerator();
            }
        }
        #endif

        // Road management methods
        public RoadEditor CreateNewRoad(string name = "New Road", Material roadMaterial = null, Material terrainMaterial = null)
        {
            GameObject roadObj = new GameObject(name);
            roadObj.tag = "Road";
            roadObj.transform.SetParent(transform, false);
            roadObj.transform.localPosition = Vector3.zero;
            roadObj.transform.localRotation = Quaternion.identity;

            RoadEditor roadEditor = roadObj.AddComponent<RoadEditor>();
            
            // Setup road generator
            RoadGenerator roadGen = roadObj.AddComponent<RoadGenerator>();
            roadGen.roadEditor = roadEditor;
            roadEditor.roadGenerator = roadGen;
            
            if (roadMaterial != null)
                roadGen.roadMaterial = roadMaterial;
                
            if (terrainMaterial != null)
                roadGen.terrainMaterial = terrainMaterial;

            return roadEditor;
        }

        public RoadEditor[] GetAllRoads()
        {
            return GetComponentsInChildren<RoadEditor>();
        }
        
        public void RemoveAllRoads()
        {
            var roads = GetAllRoads();
            for (int i = roads.Length - 1; i >= 0; i--)
            {
                if (Application.isEditor && !Application.isPlaying)
                    DestroyImmediate(roads[i].gameObject);
                else
                    Destroy(roads[i].gameObject);
            }
        }
    }
}