using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace AK.MapEditorTools
{
    /// <summary>
    /// Custom inspector for the RoadEditor component.
    /// Provides tools for editing roads in the Unity scene view.
    /// </summary>
    [CustomEditor(typeof(RoadEditor))]
    public class RoadInspector : Editor
    {
        #region Fields

        private RoadEditor roadEditor;
        private bool editingEnabled;
        private bool isDraggingAnchorPoint = false;

        // Foldout states
        private bool roadSettingsFoldout = true;
        private bool roadGeneratorFoldout = true;  // New foldout for road generator settings
        private bool terrainSettingsFoldout = true; // New foldout for terrain settings
        private bool textureSettingsFoldout = true; // New foldout for texture settings
        private bool railingSettingsFoldout = true;
        private bool trafficLineSettingsFoldout = true; // New foldout for traffic lines

        // Snapping parameters
        private bool enableSnapping = true;
        private float snapDistance = 1.0f; // Distance in units for snapping
        private Vector3 lastSnappedDirection;
        private bool hasSnappedDirection = false;

        // UI styles
        private GUIStyle buttonStyle;

        // Textures for button backgrounds (moved from DrawEditModeControls)
        private Texture2D activeTexture;
        private Texture2D inactiveTexture;

        // Static clipboard for copying/pasting road points between road editors
        private static List<RoadEditor.BezierPoint> clipboardPoints = new List<RoadEditor.BezierPoint>();

        // Bezier curve drawer utility
        private BezierCurveDrawer curveDrawer;

        // Path closing
        private bool isClosedPath = false;

        #endregion

        #region Unity Methods

        private void OnEnable()
        {
            roadEditor = (RoadEditor)target;
            curveDrawer = new BezierCurveDrawer();
            Tools.hidden = editingEnabled;

            // Initialize closed path value from generator if it exists
            if (roadEditor.roadGenerator != null)
            {
                isClosedPath = roadEditor.roadGenerator.IsClosedPath();
            }

            // Initialize textures for button backgrounds if not already created
            if (activeTexture == null)
            {
                activeTexture = new Texture2D(1, 1);
                activeTexture.SetPixel(0, 0, new Color(0.4f, 0.8f, 0.4f, 1f)); // Green
                activeTexture.Apply();
            }
            if (inactiveTexture == null)
            {
                inactiveTexture = new Texture2D(1, 1);
                inactiveTexture.SetPixel(0, 0, new Color(0.7f, 0.7f, 0.7f, 1f)); // Gray
                inactiveTexture.Apply();
            }
        }

        private void OnDisable()
        {
            Tools.hidden = false;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Road Editing", EditorStyles.boldLabel);

            DrawEditModeControls();
            DrawSnapSettings();
            DrawRoadSettings();
            DrawRoadGeneratorSettings();
            DrawRailingSettings();
            DrawTrafficLineSettings();

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(10);
            DrawActionButtons();
        }

        private void OnSceneGUI()
        {
            if (roadEditor == null) return;

            // Drawing and editing logic for roads in the scene view
            DrawRoad();

            if (roadEditor.currentMode != RoadEditor.EditMode.Disabled)
            {
                HandlePointEditing();
            }
        }

        #endregion

        #region UI Drawing Methods

        /// <summary>
        /// Draw edit mode selection controls
        /// </summary>
        private void DrawEditModeControls()
        {
            EditorGUI.BeginChangeCheck();
            RoadEditor.EditMode newEditMode = (RoadEditor.EditMode)EditorGUILayout.EnumPopup("Edit Mode", roadEditor.currentMode);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(roadEditor, "Change Road Edit Mode");
                roadEditor.currentMode = newEditMode;
                editingEnabled = newEditMode != RoadEditor.EditMode.Disabled;
                Tools.hidden = editingEnabled;
            }

            EditorGUI.BeginChangeCheck();
            bool showControlPoints = EditorGUILayout.Toggle("Show Control Points", roadEditor.showControlPoints);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(roadEditor, "Toggle Control Points");
                roadEditor.showControlPoints = showControlPoints;
                SceneView.RepaintAll();
            }

            // Add option to choose where new points are added
            if (roadEditor.currentMode == RoadEditor.EditMode.AddPoints)
            {
                EditorGUILayout.BeginHorizontal();
                GUIStyle leftButton = new GUIStyle(GUI.skin.button);
                GUIStyle rightButton = new GUIStyle(GUI.skin.button);

                // Use pre-created textures for button backgrounds
                if (roadEditor.addPointsToStart)
                {
                    leftButton.normal.background = activeTexture;
                    leftButton.fontStyle = FontStyle.Bold;
                    leftButton.normal.textColor = Color.white;
                    leftButton.active = leftButton.normal;

                    rightButton.normal.background = inactiveTexture;
                    rightButton.normal.textColor = Color.black;
                }
                else
                {
                    rightButton.normal.background = activeTexture;
                    rightButton.fontStyle = FontStyle.Bold;
                    rightButton.normal.textColor = Color.white;
                    rightButton.active = rightButton.normal;

                    leftButton.normal.background = inactiveTexture;
                    leftButton.normal.textColor = Color.black;
                }

                if (GUILayout.Button("Add to Start", leftButton))
                {
                    Undo.RecordObject(roadEditor, "Set Add Points To Start");
                    roadEditor.addPointsToStart = true;
                }
                if (GUILayout.Button("Add to End", rightButton))
                {
                    Undo.RecordObject(roadEditor, "Set Add Points To End");
                    roadEditor.addPointsToStart = false;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.HelpBox(
                    roadEditor.addPointsToStart ?
                    "New points will be added to the START of the road." :
                    "New points will be added to the END of the road.",
                    MessageType.Info);
            }

            // Add closed path option
            EditorGUI.BeginChangeCheck();
            isClosedPath = EditorGUILayout.Toggle("Closed Path", isClosedPath);
            if (EditorGUI.EndChangeCheck() && roadEditor.roadGenerator != null)
            {
                Undo.RecordObject(roadEditor.roadGenerator, "Toggle Closed Path");
                roadEditor.roadGenerator.SetClosedPath(isClosedPath);
                SceneView.RepaintAll();
            }
        }

        /// <summary>
        /// Draw snapping settings controls
        /// </summary>
        private void DrawSnapSettings()
        {
            EditorGUI.BeginChangeCheck();
            enableSnapping = EditorGUILayout.Toggle("Enable Vertex Snapping", enableSnapping);
            if (enableSnapping)
            {
                EditorGUI.indentLevel++;
                snapDistance = EditorGUILayout.Slider("Snap Distance", snapDistance, 0.1f, 5.0f);
                EditorGUI.indentLevel--;
            }
            if (EditorGUI.EndChangeCheck())
            {
                SceneView.RepaintAll();
            }
        }

        /// <summary>
        /// Draw road curve settings controls
        /// </summary>
        private void DrawRoadSettings()
        {
            roadSettingsFoldout = EditorGUILayout.Foldout(roadSettingsFoldout, "Road Curve Settings", true);
            if (roadSettingsFoldout)
            {
                EditorGUI.indentLevel++;

                EditorGUI.BeginChangeCheck();
                int resolution = EditorGUILayout.IntSlider("Curve Resolution", roadEditor.curveResolution, 1, 20);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(roadEditor, "Change Curve Resolution");
                    roadEditor.curveResolution = resolution;
                    roadEditor.UpdateRoadGenerator();
                    SceneView.RepaintAll();
                }

                EditorGUI.BeginChangeCheck();
                float tension = EditorGUILayout.Slider("Curve Smoothness", roadEditor.curveTension, 0.1f, 0.9f);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(roadEditor, "Change Curve Tension");
                    roadEditor.curveTension = tension;
                    roadEditor.RecalculateAllBezierHandles();
                    SceneView.RepaintAll();
                }

                EditorGUI.indentLevel--;
            }
        }

        /// <summary>
        /// Draw road generator appearance settings
        /// </summary>
        private void DrawRoadGeneratorSettings()
        {
            if (roadEditor.roadGenerator == null)
            {
                EditorGUILayout.HelpBox("Road Generator component is missing. Some settings will be unavailable.", MessageType.Warning);
                return;
            }

            // Get reference to the generator
            var generator = roadEditor.roadGenerator;

            // Road appearance settings
            roadGeneratorFoldout = EditorGUILayout.Foldout(roadGeneratorFoldout, "Road Appearance Settings", true);
            if (roadGeneratorFoldout)
            {
                EditorGUI.indentLevel++;

                // Road settings
                EditorGUI.BeginChangeCheck();
                float roadWidth = EditorGUILayout.FloatField("Road Width", generator.roadWidth);
                Material roadMaterial = (Material)EditorGUILayout.ObjectField("Road Material", generator.roadMaterial, typeof(Material), false);
                float heightOffset = EditorGUILayout.FloatField("Height Offset", generator.heightOffset);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(generator, "Change Road Appearance Settings");
                    generator.roadWidth = roadWidth;
                    generator.roadMaterial = roadMaterial;
                    generator.heightOffset = heightOffset;
                    generator.RegenerateMesh();
                }

                EditorGUI.indentLevel--;
            }

            // Terrain settings
            terrainSettingsFoldout = EditorGUILayout.Foldout(terrainSettingsFoldout, "Terrain Settings", true);
            if (terrainSettingsFoldout)
            {
                EditorGUI.indentLevel++;

                EditorGUI.BeginChangeCheck();
                Material terrainMaterial = (Material)EditorGUILayout.ObjectField("Terrain Material", generator.terrainMaterial, typeof(Material), false);
                float terrainHeight = EditorGUILayout.FloatField("Terrain Height", generator.terrainHeightOffset);
                float terrainSize = EditorGUILayout.FloatField("Terrain Size", generator.terrainSize);
                float terrainTiling = EditorGUILayout.FloatField("Terrain Tiling", generator.terrainUvHorizontalTile);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(generator, "Change Terrain Settings");
                    generator.terrainMaterial = terrainMaterial;
                    generator.terrainHeightOffset = terrainHeight;
                    generator.terrainSize = terrainSize;
                    generator.terrainUvHorizontalTile = terrainTiling;
                    generator.RegenerateMesh();
                }

                EditorGUI.indentLevel--;
            }

            // Texture settings
            textureSettingsFoldout = EditorGUILayout.Foldout(textureSettingsFoldout, "Texture Settings", true);
            if (textureSettingsFoldout)
            {
                EditorGUI.indentLevel++;

                EditorGUI.BeginChangeCheck();
                float uvTilingDensity = EditorGUILayout.FloatField("UV Tiling Density", generator.uvTilingDensity);
                float uvTilingWidth = EditorGUILayout.FloatField("UV Tiling Width", generator.uvTilingWidth);
                bool flipNormals = EditorGUILayout.Toggle("Flip Normals", generator.flipNormals);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(generator, "Change Texture Settings");
                    generator.uvTilingDensity = uvTilingDensity;
                    generator.uvTilingWidth = uvTilingWidth;
                    generator.flipNormals = flipNormals;
                    generator.RegenerateMesh();
                }

                EditorGUILayout.HelpBox("UV Tiling Density controls repetition along road length.\nUV Tiling Width controls repetition across road width.", MessageType.Info);

                EditorGUI.indentLevel--;
            }
        }

        /// <summary>
        /// Draw railing settings controls
        /// </summary>
        private void DrawRailingSettings()
        {
            railingSettingsFoldout = EditorGUILayout.Foldout(railingSettingsFoldout, "Railing Settings", true);
            if (railingSettingsFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();

                DrawSideRailingSettings("Left Railing", "leftRailingType", "leftRailingMaterial", "leftRailingOffset", "leftUvRepeatFactor");

                EditorGUILayout.Space();

                DrawSideRailingSettings("Right Railing", "rightRailingType", "rightRailingMaterial", "rightRailingOffset", "rightUvRepeatFactor");

                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    roadEditor.RegenerateRailings();
                }

                EditorGUI.indentLevel--;
            }
        }

        /// <summary>
        /// Draw traffic line settings
        /// </summary>
        private void DrawTrafficLineSettings()
        {
            trafficLineSettingsFoldout = EditorGUILayout.Foldout(trafficLineSettingsFoldout, "Traffic Lines", true);
            if (trafficLineSettingsFoldout)
            {
                EditorGUI.indentLevel++;

                // Toggle for showing traffic lines
                EditorGUI.BeginChangeCheck();
                bool showTrafficLines = EditorGUILayout.Toggle("Show Traffic Lines", roadEditor.showTrafficLines);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(roadEditor, "Toggle Traffic Lines");
                    roadEditor.showTrafficLines = showTrafficLines;
                    SceneView.RepaintAll();
                }

                if (roadEditor.showTrafficLines)
                {
                    // Draw each traffic line
                    for (int i = 0; i < roadEditor.trafficLines.Count; i++)
                    {
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"Traffic Line {i + 1}", EditorStyles.boldLabel);

                        // Remove button
                        if (GUILayout.Button("X", GUILayout.Width(25)))
                        {
                            if (EditorUtility.DisplayDialog("Remove Traffic Line",
                                $"Are you sure you want to remove Traffic Line {i + 1}?", "Yes", "No"))
                            {
                                Undo.RecordObject(roadEditor, "Remove Traffic Line");
                                roadEditor.RemoveTrafficLine(i);
                                SceneView.RepaintAll();
                                EditorGUILayout.EndHorizontal();
                                EditorGUILayout.EndVertical();
                                break; // Exit loop since we've modified the collection
                            }
                        }
                        EditorGUILayout.EndHorizontal();

                        var line = roadEditor.trafficLines[i];

                        // Enable toggle
                        EditorGUI.BeginChangeCheck();
                        bool enabled = EditorGUILayout.Toggle("Enabled", line.enabled);
                        float offset = EditorGUILayout.Slider("Offset", line.offset, -roadEditor.roadGenerator.roadWidth / 2 + 0.1f, roadEditor.roadGenerator.roadWidth / 2 - 0.1f);
                        bool reverseDirection = EditorGUILayout.Toggle("Reverse Direction", line.reverseDirection);
                        float lineWidth = EditorGUILayout.Slider("Line Width", line.lineWidth, 0.05f, 2f);
                        float arrowSpacing = EditorGUILayout.Slider("Arrow Spacing", line.arrowSpacing, 1f, 10f);
                        float arrowSize = EditorGUILayout.Slider("Arrow Size", line.arrowSize, 0.1f, 2f);

                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(roadEditor, "Edit Traffic Line");
                            line.enabled = enabled;
                            line.offset = offset;
                            line.reverseDirection = reverseDirection;
                            line.lineWidth = lineWidth;
                            line.arrowSpacing = arrowSpacing;
                            line.arrowSize = arrowSize;
                            roadEditor.trafficLines[i] = line; // Update the list item
                            roadEditor.RegenerateTrafficLines();
                            SceneView.RepaintAll();
                        }

                        EditorGUILayout.EndVertical();
                        EditorGUILayout.Space(5);
                    }

                    // Add new traffic line button
                    if (GUILayout.Button("Add Traffic Line", GUILayout.Height(25)))
                    {
                        Undo.RecordObject(roadEditor, "Add Traffic Line");
                        roadEditor.AddTrafficLine();
                        SceneView.RepaintAll();
                    }
                }

                EditorGUI.indentLevel--;
            }
        }

        /// <summary>
        /// Draw settings for one side of railings (left or right)
        /// </summary>
        private void DrawSideRailingSettings(string title, string typeProperty, string materialProperty, string offsetProperty, string uvFactorProperty)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            SerializedProperty typePropertyObj = serializedObject.FindProperty(typeProperty);
            EditorGUILayout.PropertyField(typePropertyObj, new GUIContent("Type"));

            // Only show additional properties if a railing type is selected
            if (typePropertyObj.enumValueIndex > 0) // Not "None"
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(materialProperty), new GUIContent("Material"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(offsetProperty), new GUIContent("Offset"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(uvFactorProperty), new GUIContent("UV Repeat Factor"));

                // Show height property specific to the railing type
                if (typePropertyObj.enumValueIndex == 1) // Wall
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("wallHeight"), new GUIContent("Wall Height"));
                }
                else if (typePropertyObj.enumValueIndex == 2) // Plane
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("planeHeight"), new GUIContent("Plane Height"));
                }
            }
        }

        /// <summary>
        /// Draw action buttons for road operations
        /// </summary>
        private void DrawActionButtons()
        {
            if (buttonStyle is null)
            {
                buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    fixedHeight = 30
                };
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Recalculate Curves", buttonStyle))
            {
                Undo.RecordObject(roadEditor, "Recalculate Bezier Handles");
                roadEditor.RecalculateAllBezierHandles();
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("Regenerate Road", buttonStyle))
            {
                if (roadEditor.roadGenerator != null)
                {
                    Undo.RecordObject(roadEditor.roadGenerator, "Regenerate Road");
                    roadEditor.roadGenerator.RegenerateMesh();
                }
                roadEditor.RegenerateRailings();
                SceneView.RepaintAll();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Clear All Points", buttonStyle))
            {
                ClearAllPoints();
            }
            EditorGUILayout.EndHorizontal();

            // Copy/Paste Settings buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Copy Settings", buttonStyle))
            {
                CopyRoadSettings();
            }

            EditorGUI.BeginDisabledGroup(!RoadSettingsClipboard.hasSettings);
            if (GUILayout.Button("Paste Settings", buttonStyle))
            {
                PasteRoadSettings();
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            // Delete selected point button - only enabled when a point is selected in Edit mode
            EditorGUI.BeginDisabledGroup(roadEditor.currentMode != RoadEditor.EditMode.EditPoints ||
                                         roadEditor.selectedPointIndex < 0);
            if (GUILayout.Button("Delete Selected Point", buttonStyle))
            {
                Undo.RecordObject(roadEditor, "Delete Road Point");
                if (roadEditor.DeleteSelectedPoint())
                {
                    SceneView.RepaintAll();
                }
            }
            EditorGUI.EndDisabledGroup();

            // Help text
            if (roadEditor.currentMode == RoadEditor.EditMode.EditPoints && roadEditor.selectedPointIndex >= 0)
            {
                EditorGUILayout.HelpBox("You can also press the Delete key to remove the selected point.", MessageType.Info);
            }

            // Display settings clipboard status
            if (RoadSettingsClipboard.hasSettings)
            {
                EditorGUILayout.HelpBox("Road settings are ready to paste to other roads.", MessageType.Info);
            }
        }

        /// <summary>
        /// Clear all road points after confirmation
        /// </summary>
        private void ClearAllPoints()
        {
            if (EditorUtility.DisplayDialog("Clear All Points",
                "Are you sure you want to delete all road points?", "Yes", "No"))
            {
                Undo.RecordObject(roadEditor, "Clear All Points");
                roadEditor.ClearPoints();
                SceneView.RepaintAll();
            }
        }

        #endregion

        #region Road Drawing and Editing Methods

        /// <summary>
        /// Draw the road path and control points in scene view
        /// </summary>
        private void DrawRoad()
        {
            // Draw the path using the curve drawer
            List<Vector3> points = roadEditor.GetBezierCurvePoints();
            curveDrawer.DrawPath(points);

            // Draw traffic lines if enabled
            if (roadEditor.showTrafficLines)
            {
                roadEditor.DrawTrafficLines();
            }

            if (roadEditor.showControlPoints)
            {
                // Convert RoadEditor.BezierPoint to BezierCurveDrawer.BezierPoint
                List<BezierCurveDrawer.BezierPoint> convertedPoints = new List<BezierCurveDrawer.BezierPoint>();
                foreach (var point in roadEditor.GetBezierPoints())
                {
                    convertedPoints.Add(new BezierCurveDrawer.BezierPoint(point.position, point.handleIn, point.handleOut));
                }

                // Draw control points using the curve drawer
                curveDrawer.DrawControlPoints(convertedPoints, roadEditor.selectedPointIndex,
                    (index, handleType) =>
                    {
                        RoadEditor.HandleType roadHandleType = (RoadEditor.HandleType)(int)handleType;
                        if (handleType == BezierCurveDrawer.HandleType.Anchor)
                        {
                            roadEditor.SelectPoint(index);
                            roadEditor.selectedHandleType = roadHandleType;
                            isDraggingAnchorPoint = true;
                            Repaint();
                        }
                        else
                        {
                            roadEditor.SelectHandle(index, roadHandleType);
                            Repaint();
                        }
                    });
            }
        }

        /// <summary>
        /// Handle road point editing based on current edit mode
        /// </summary>
        private void HandlePointEditing()
        {
            Event e = Event.current;

            // Handle Delete key press when a point is selected in Edit mode
            if (e.type == EventType.KeyDown && roadEditor.currentMode == RoadEditor.EditMode.EditPoints)
            {
                if (e.keyCode == KeyCode.Delete && roadEditor.selectedPointIndex >= 0)
                {
                    Undo.RecordObject(roadEditor, "Delete Road Point");
                    if (roadEditor.DeleteSelectedPoint())
                    {
                        e.Use(); // Consume the event
                        SceneView.RepaintAll();
                    }
                }
            }

            if (roadEditor.currentMode == RoadEditor.EditMode.AddPoints)
            {
                HandleAddPointMode(e);
            }
            else if (roadEditor.currentMode == RoadEditor.EditMode.EditPoints)
            {
                HandleEditPointMode();
            }
        }

        /// <summary>
        /// Handle point addition mode
        /// </summary>
        private void HandleAddPointMode(Event e)
        {
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                RaycastHit hit;

                Vector3 pointToAdd;
                if (Physics.Raycast(ray, out hit))
                {
                    pointToAdd = hit.point;
                }
                else
                {
                    Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
                    float distance;
                    if (!groundPlane.Raycast(ray, out distance))
                        return;

                    pointToAdd = ray.GetPoint(distance);
                }

                AddNewRoadPoint(pointToAdd);
                e.Use();
            }
        }

        /// <summary>
        /// Add a new road point at the specified position
        /// </summary>
        private void AddNewRoadPoint(Vector3 position)
        {
            bool shouldSnap = roadEditor.GetPointCount() == 0; // Only snap the first point
            Vector3 finalPosition = position;

            // Maintain the same height as previous point if available
            if (roadEditor.GetPointCount() > 0)
            {
                Vector3 previousPoint = roadEditor.GetPointAt(roadEditor.GetPointCount() - 1);
                finalPosition.y = previousPoint.y; // Keep the same Y coordinate
            }

            // Try to snap if enabled and this is a valid point for snapping
            if (enableSnapping && shouldSnap)
            {
                Vector3 snappedPos;
                Vector3 snappedDirection;
                bool snappedToRoadAttach;
                if (TrySnapToRoadVertex(position, out snappedPos, out snappedDirection, out snappedToRoadAttach))
                {
                    finalPosition = snappedPos;
                    lastSnappedDirection = snappedDirection;
                    hasSnappedDirection = true;
                }
            }

            Undo.RecordObject(roadEditor, "Add Road Point");
            roadEditor.AddPoint(finalPosition);

        }

        /// <summary>
        /// Handle point editing mode
        /// </summary>
        private void HandleEditPointMode()
        {
            if (roadEditor.selectedPointIndex < 0)
                return;

            if (roadEditor.selectedHandleType == RoadEditor.HandleType.Anchor && isDraggingAnchorPoint)
            {
                HandleAnchorPointMovement();
            }
            else if (roadEditor.selectedHandleType == RoadEditor.HandleType.InHandle ||
                     roadEditor.selectedHandleType == RoadEditor.HandleType.OutHandle)
            {
                HandleControlPointMovement();
            }
        }

        /// <summary>
        /// Handle anchor point movement
        /// </summary>
        private void HandleAnchorPointMovement()
        {
            var point = roadEditor.GetPointAt(roadEditor.selectedPointIndex);
            EditorGUI.BeginChangeCheck();

            // Use curve drawer to draw the position handle
            Vector3 newPos = curveDrawer.DrawPositionHandle(point);

            if (EditorGUI.EndChangeCheck())
            {
                // Only enable snapping for first and last points
                bool shouldSnap = IsEndPoint(roadEditor.selectedPointIndex);

                // Try to snap if enabled and appropriate
                if (enableSnapping && shouldSnap)
                {
                    Vector3 snappedPos;
                    Vector3 snappedDirection;
                    bool snappedToRoadAttach = false;

                    if (TrySnapToRoadVertex(newPos, out snappedPos, out snappedDirection, out snappedToRoadAttach))
                    {
                        newPos = snappedPos;
                        lastSnappedDirection = snappedDirection;
                        hasSnappedDirection = true;

                        // Store the snapped-to-attach-point information
                        if (snappedToRoadAttach)
                        {
                            // Remember we snapped to a road attach point
                            Undo.RecordObject(roadEditor, "Move Road Point and Align");

                            // Update point position first
                            roadEditor.UpdatePointPosition(roadEditor.selectedPointIndex, newPos);

                            // Align the penultimate point to create a straight segment
                            AlignPenultimatePoint(roadEditor.selectedPointIndex, lastSnappedDirection);

                            // Skip the regular update since we've already done it
                            SceneView.RepaintAll();
                            return;
                        }
                    }
                }

                Undo.RecordObject(roadEditor, "Move Road Point");
                roadEditor.UpdatePointPosition(roadEditor.selectedPointIndex, newPos);

                // If we have a snapped direction, align the bezier handles
                if (hasSnappedDirection)
                {
                    //AlignBezierHandlesToDirection(roadEditor.selectedPointIndex, lastSnappedDirection);
                    hasSnappedDirection = false;
                }

                SceneView.RepaintAll();
            }
        }

        /// <summary>
        /// Handle control point (handle) movement
        /// </summary>
        private void HandleControlPointMovement()
        {
            var bezierPoint = roadEditor.GetBezierPointAt(roadEditor.selectedPointIndex);
            if (bezierPoint == null)
                return;

            Vector3 handlePos = roadEditor.selectedHandleType == RoadEditor.HandleType.InHandle
                ? bezierPoint.handleIn
                : bezierPoint.handleOut;

            EditorGUI.BeginChangeCheck();

            // Use curve drawer to draw the position handle
            Vector3 newPos = curveDrawer.DrawPositionHandle(handlePos);

            if (EditorGUI.EndChangeCheck())
            {
                // Only snap handles of first and last points
                bool shouldSnap = IsEndPoint(roadEditor.selectedPointIndex);

                // Try to snap if enabled and appropriate
                if (enableSnapping && shouldSnap)
                {
                    Vector3 snappedPos;
                    Vector3 snappedDirection;
                    bool snappedToRoadAttach;
                    if (TrySnapToRoadVertex(newPos, out snappedPos, out snappedDirection, out snappedToRoadAttach))
                    {
                        newPos = snappedPos;
                    }
                }

                Undo.RecordObject(roadEditor, "Move Handle");
                roadEditor.UpdateControlPoint(roadEditor.selectedPointIndex, roadEditor.selectedHandleType, newPos);

                SceneView.RepaintAll();
            }
        }

        /// <summary>
        /// Check if the point at the given index is an end point (first or last)
        /// </summary>
        private bool IsEndPoint(int pointIndex)
        {
            int pointCount = roadEditor.GetPointCount();
            return pointIndex == 0 || pointIndex == pointCount - 1;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Tries to snap the given position to a nearby road vertex
        /// </summary>
        /// <param name="position">The current position</param>
        /// <param name="snappedPosition">The snapped position if found</param>
        /// <param name="snappedDirection">The direction of the snapped position</param>
        /// <param name="snappedToRoadAttach">Whether the snap was to a road attach point</param>
        /// <returns>True if snapping occurred, false otherwise</returns>
        private bool TrySnapToRoadVertex(Vector3 position, out Vector3 snappedPosition, out Vector3 snappedDirection, out bool snappedToRoadAttach)
        {
            snappedPosition = position;
            snappedDirection = Vector3.zero;
            snappedToRoadAttach = false;
            float closestDistance = snapDistance;
            bool foundSnappingPoint = false;

            // Find all GameObjects with tag 'Road'
            foreach (GameObject roadObj in GameObject.FindGameObjectsWithTag("Road"))
            {
                // Skip the current road we're editing to avoid self-snapping issues
                RoadEditor otherRoad = roadObj.GetComponent<RoadEditor>();
                if (otherRoad != null && otherRoad == roadEditor)
                    continue;

                if (otherRoad != null)
                {
                    // Try snapping to road bezier points
                    if (TrySnapToRoadBezierPoints(otherRoad, position, ref closestDistance, ref snappedPosition, ref snappedDirection))
                    {
                        foundSnappingPoint = true;
                    }

                    // Try snapping to control points if visible
                    if (otherRoad.showControlPoints &&
                    TrySnapToRoadControlPoints(otherRoad, position, ref closestDistance, ref snappedPosition, ref snappedDirection))
                    {
                        foundSnappingPoint = true;
                    }
                }
                else
                {
                    // Try snapping to road attachment points
                    if (TrySnapToRoadAttachPoints(roadObj, position, ref closestDistance, ref snappedPosition, ref snappedDirection))
                    {
                        foundSnappingPoint = true;
                        snappedToRoadAttach = true;
                    }
                }
            }

            return foundSnappingPoint;
        }

        /// <summary>
        /// Try to snap to a road's bezier curve points
        /// </summary>
        private bool TrySnapToRoadBezierPoints(RoadEditor otherRoad, Vector3 position, ref float closestDistance,
                                             ref Vector3 snappedPosition, ref Vector3 snappedDirection)
        {
            bool foundSnappingPoint = false;
            List<Vector3> roadPoints = otherRoad.GetBezierCurvePoints();

            for (int i = 0; i < roadPoints.Count; i++)
            {
                Vector3 roadPoint = roadPoints[i];
                float distance = Vector3.Distance(position, roadPoint);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    snappedPosition = roadPoint;
                    foundSnappingPoint = true;

                    // Determine the direction based on the next or previous point in the curve
                    if (i < roadPoints.Count - 1)
                    {
                        snappedDirection = (roadPoints[i + 1] - roadPoint).normalized;
                    }
                    else if (i > 0)
                    {
                        snappedDirection = (roadPoint - roadPoints[i - 1]).normalized;
                    }
                }
            }

            return foundSnappingPoint;
        }

        /// <summary>
        /// Try to snap to a road's control points
        /// </summary>
        private bool TrySnapToRoadControlPoints(RoadEditor otherRoad, Vector3 position, ref float closestDistance,
                                              ref Vector3 snappedPosition, ref Vector3 snappedDirection)
        {
            bool foundSnappingPoint = false;
            List<RoadEditor.BezierPoint> bezierPoints = otherRoad.GetBezierPoints();

            foreach (var bezierPoint in bezierPoints)
            {
                // Check the handle positions
                if (TrySnapToHandle(position, bezierPoint.handleIn, bezierPoint.position, ref closestDistance,
                                   ref snappedPosition, ref snappedDirection, true))
                {
                    foundSnappingPoint = true;
                }

                if (TrySnapToHandle(position, bezierPoint.handleOut, bezierPoint.position, ref closestDistance,
                                   ref snappedPosition, ref snappedDirection, false))
                {
                    foundSnappingPoint = true;
                }
            }

            return foundSnappingPoint;
        }

        /// <summary>
        /// Try to snap to a specific handle
        /// </summary>
        private bool TrySnapToHandle(Vector3 position, Vector3 handlePos, Vector3 anchorPos, ref float closestDistance,
                                   ref Vector3 snappedPosition, ref Vector3 snappedDirection, bool isInHandle)
        {
            float distance = Vector3.Distance(position, handlePos);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                snappedPosition = handlePos;

                // For in handles, direction is from handle to anchor
                // For out handles, direction is from anchor to handle
                if (isInHandle)
                {
                    snappedDirection = (anchorPos - handlePos).normalized;
                }
                else
                {
                    snappedDirection = (handlePos - anchorPos).normalized;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Try to snap to road attachment points
        /// </summary>
        private bool TrySnapToRoadAttachPoints(GameObject roadObj, Vector3 position, ref float closestDistance,
                                             ref Vector3 snappedPosition, ref Vector3 snappedDirection)
        {
            bool foundSnappingPoint = false;
            Transform[] children = roadObj.GetComponentsInChildren<Transform>();

            foreach (Transform child in children)
            {
                if (child.CompareTag("RoadAttach"))
                {
                    float distance = Vector3.Distance(position, child.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        snappedPosition = child.position;
                        foundSnappingPoint = true;

                        // Get the parent road's direction instead of calculating from center
                        // The attach point is designed to connect to other roads
                        snappedDirection = child.forward;

                        // Visualize the connection in the editor
                        Debug.DrawLine(child.position, child.position + snappedDirection * 2f, Color.yellow, 1.0f);
                    }
                }
            }

            return foundSnappingPoint;
        }

        /// <summary>
        /// Align the penultimate point to create a straight section at the end of the road
        /// </summary>
        private void AlignPenultimatePoint(int endPointIndex, Vector3 endPointDirection)
        {
            // Only proceed if we have at least 2 points (the end point and a previous point)
            int pointCount = roadEditor.GetPointCount();
            if (pointCount < 2)
                return;

            // Determine which point is the end point (first or last)
            bool isFirstPoint = endPointIndex == 0;
            int penultimateIndex = isFirstPoint ? 1 : pointCount - 2;

            // Get the positions of both points
            Vector3 endPosition = roadEditor.GetPointAt(endPointIndex);
            Vector3 penultimatePosition = roadEditor.GetPointAt(penultimateIndex);

            // Calculate the ideal position for the penultimate point based on the direction
            // We want it to be at a reasonable distance from the end point
            float penultimateDistance = 10.0f;

            // Use the correct direction for positioning the penultimate point
            // For first point, we need to invert the direction since we're going away from the start
            Vector3 idealPenultimatePosition = endPosition + -endPointDirection * penultimateDistance;

            // Maintain the Y position of the original penultimate point
            idealPenultimatePosition.y = penultimatePosition.y;

            // Update the penultimate point position
            Undo.RecordObject(roadEditor, "Align Penultimate Point");
            roadEditor.UpdatePointPosition(penultimateIndex, idealPenultimatePosition);

            // Update the bezier handles to create a straight line
            var penultimateBezier = roadEditor.GetBezierPointAt(penultimateIndex);
            var endBezier = roadEditor.GetBezierPointAt(endPointIndex);

            if (penultimateBezier != null && endBezier != null)
            {
                Vector3 directionVector = endPointDirection;
                if (endPointIndex == 0)
                    directionVector = -endPointDirection;

                // Determine distances for handles (use existing handle lengths when possible)
                float penultimateInLength = 3;
                float penultimateOutLength = 3;
                float endInLength = 3;
                float endOutLength = 3;

                // Update penultimate point handles
                // Out handle should point toward the end point
                Vector3 penultimateOutPos = penultimateBezier.position + directionVector * penultimateOutLength;
                roadEditor.UpdateControlPoint(penultimateIndex, RoadEditor.HandleType.OutHandle, penultimateOutPos);

                // In handle should point away from the end point
                Vector3 penultimateInPos = penultimateBezier.position - directionVector * penultimateInLength;
                roadEditor.UpdateControlPoint(penultimateIndex, RoadEditor.HandleType.InHandle, penultimateInPos);

                // Out handle should point away from penultimate point
                Vector3 endOutPos = endBezier.position + directionVector * endOutLength;
                roadEditor.UpdateControlPoint(endPointIndex, RoadEditor.HandleType.OutHandle, endOutPos);

                // Update end point handles
                // In handle should point toward penultimate point
                Vector3 endInPos = endBezier.position - directionVector * endInLength;
                roadEditor.UpdateControlPoint(endPointIndex, RoadEditor.HandleType.InHandle, endInPos);

            }

            Debug.DrawLine(endPosition, idealPenultimatePosition, Color.green, 2.0f);
            SceneView.RepaintAll();
        }

        /// <summary>
        /// Copy the current road settings to the clipboard
        /// </summary>
        private void CopyRoadSettings()
        {
            if (roadEditor == null || roadEditor.roadGenerator == null)
            {
                Debug.LogWarning("Cannot copy settings: Road Generator is not assigned.");
                return;
            }

            RoadSettingsClipboard.CopyFrom(roadEditor);
            Debug.Log("Road settings copied to clipboard. Use 'Paste Settings' on another road to apply them.");
            Repaint();
        }

        /// <summary>
        /// Paste the copied road settings to the current road
        /// </summary>
        private void PasteRoadSettings()
        {
            if (!RoadSettingsClipboard.hasSettings)
            {
                Debug.LogWarning("No settings to paste. Copy settings from another road first.");
                return;
            }

            if (roadEditor == null || roadEditor.roadGenerator == null)
            {
                Debug.LogWarning("Cannot paste settings: Road Generator is not assigned.");
                return;
            }

            RoadSettingsClipboard.PasteTo(roadEditor);
            Debug.Log("Road settings applied successfully.");
            Repaint();
        }

        #endregion
    }
}
