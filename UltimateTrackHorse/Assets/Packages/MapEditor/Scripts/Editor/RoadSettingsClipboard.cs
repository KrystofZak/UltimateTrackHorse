using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace AK.MapEditorTools
{
    /// <summary>
    /// Clipboard for storing road settings between different RoadEditor instances
    /// </summary>
    public static class RoadSettingsClipboard
    {
        // Road settings
        public static float roadWidth;
        public static Material roadMaterial;
        public static float heightOffset;
        
        // Terrain settings
        public static Material terrainMaterial;
        public static float terrainSize;
        public static float terrainHeightOffset;
        
        // Texture settings
        public static float uvTilingDensity;
        public static float uvTilingWidth;
        public static bool flipNormals;

        // Railing settings
        public static RailingGenerator.RailingType leftRailingType;
        public static RailingGenerator.RailingType rightRailingType;
        public static Material leftRailingMaterial;
        public static Material rightRailingMaterial;
        public static float leftRailingOffset;
        public static float rightRailingOffset;
        public static float wallHeight;
        public static float planeHeight;
        public static float leftUvRepeatFactor;
        public static float rightUvRepeatFactor;

        // Traffic lines settings
        public static bool showTrafficLines;
        public static List<RoadEditor.TrafficLine> trafficLines = new List<RoadEditor.TrafficLine>();

        public static bool hasSettings = false;

        public static void CopyFrom(RoadEditor roadEditor)
        {
            if (roadEditor == null || roadEditor.roadGenerator == null) return;
            
            var generator = roadEditor.roadGenerator;
            
            // Copy road settings
            roadWidth = generator.roadWidth;
            roadMaterial = generator.roadMaterial;
            heightOffset = generator.heightOffset;

            // Copy terrain settings
            terrainMaterial = generator.terrainMaterial;

            // Copy texture settings
            uvTilingDensity = generator.uvTilingDensity;
            uvTilingWidth = generator.uvTilingWidth;
            flipNormals = generator.flipNormals;
            
            // Copy railing settings
            leftRailingType = roadEditor.leftRailingType;
            rightRailingType = roadEditor.rightRailingType;
            leftRailingMaterial = roadEditor.leftRailingMaterial;
            rightRailingMaterial = roadEditor.rightRailingMaterial;
            leftRailingOffset = roadEditor.leftRailingOffset;
            rightRailingOffset = roadEditor.rightRailingOffset;
            wallHeight = roadEditor.wallHeight;
            planeHeight = roadEditor.planeHeight;
            leftUvRepeatFactor = roadEditor.leftUvRepeatFactor;
            rightUvRepeatFactor = roadEditor.rightUvRepeatFactor;

            // Copy traffic lines settings
            showTrafficLines = roadEditor.showTrafficLines;
            trafficLines = new List<RoadEditor.TrafficLine>(roadEditor.trafficLines);

            hasSettings = true;
        }
        
        public static void PasteTo(RoadEditor roadEditor)
        {
            if (!hasSettings || roadEditor == null || roadEditor.roadGenerator == null) return;
            
            var generator = roadEditor.roadGenerator;
            Undo.RecordObjects(new Object[] { roadEditor, generator }, "Paste Road Settings");
            
            // Paste road settings
            generator.roadWidth = roadWidth;
            generator.roadMaterial = roadMaterial;
            generator.heightOffset = heightOffset;

            // Paste terrain settings
            generator.terrainMaterial = terrainMaterial;

            // Paste texture settings
            generator.uvTilingDensity = uvTilingDensity;
            generator.uvTilingWidth = uvTilingWidth;
            generator.flipNormals = flipNormals;
            
            // Paste railing settings
            roadEditor.leftRailingType = leftRailingType;
            roadEditor.rightRailingType = rightRailingType;
            roadEditor.leftRailingMaterial = leftRailingMaterial;
            roadEditor.rightRailingMaterial = rightRailingMaterial;
            roadEditor.leftRailingOffset = leftRailingOffset;
            roadEditor.rightRailingOffset = rightRailingOffset;
            roadEditor.wallHeight = wallHeight;
            roadEditor.planeHeight = planeHeight;
            roadEditor.leftUvRepeatFactor = leftUvRepeatFactor;
            roadEditor.rightUvRepeatFactor = rightUvRepeatFactor;

            // Paste traffic lines settings
            roadEditor.showTrafficLines = showTrafficLines;
            roadEditor.trafficLines = new List<RoadEditor.TrafficLine>(trafficLines);

            // Apply changes
            generator.RegenerateMesh();
            roadEditor.RegenerateRailings();
            roadEditor.RegenerateTrafficLines();
        }
    }
}