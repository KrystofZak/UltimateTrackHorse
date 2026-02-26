using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace AK.MapEditorTools
{
    [CustomEditor(typeof(TrafficLineGenerator))]
    public class TrafficLineGeneratorEditor : Editor
    {
        private TrafficLineGenerator generator;
        private TrafficLineDrawer lineDrawer;
        private SerializedProperty pathWidthProperty;
        private SerializedProperty showLineProperty;
        private SerializedProperty closedPathProperty;
        private SerializedProperty linePropertiesProperty;
        private SerializedProperty pathPointsProperty;
        private SerializedProperty pathModeProperty;

        // Foldouts
        private bool generalSettingsFoldout = true;
        private bool pathPointsFoldout = false;
        private bool lineSettingsFoldout = true;

        // Manual path editing variables
        private bool isAddingPoints = false;
        private int selectedPointIndex = -1;

        private GUIStyle buttonStyle;

        private void OnEnable()
        {
            generator = (TrafficLineGenerator)target;

            if (lineDrawer == null)
                lineDrawer = new TrafficLineDrawer();

            pathWidthProperty = serializedObject.FindProperty("pathWidth");
            showLineProperty = serializedObject.FindProperty("showLine");
            closedPathProperty = serializedObject.FindProperty("closedPath");
            linePropertiesProperty = serializedObject.FindProperty("lineProperties");
            pathPointsProperty = serializedObject.FindProperty("pathPoints");
            pathModeProperty = serializedObject.FindProperty("pathMode");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Traffic Line Generator", EditorStyles.boldLabel);

            DrawGeneralSettings();
            DrawPathModeSettings();
            DrawPathPointsSettings();
            DrawLineSettings();

            EditorGUILayout.Space(10);
            if (generator.pathMode == TrafficLineGenerator.PathMode.Automatic)
            {
                EditorGUILayout.HelpBox("Automatic Mode: Path points are provided by the RoadEditor with offsets already applied.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("Manual Mode: Use the buttons below to add, edit, or remove path points. Points are stored in local space relative to this object.", MessageType.Info);
                DrawManualPathControls();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void OnSceneGUI()
        {
            if (generator == null) return;

            // Always draw path points for visualization
            List<Vector3> worldPoints = generator.GetWorldPathPoints();

            if (worldPoints != null && worldPoints.Count >= 2)
            {
                // Draw the base path
                Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
                DrawPath(worldPoints);

                // Draw the traffic line
                if (generator.showLine)
                {
                    generator.DrawLine();
                }
            }

            // Handle manual path editing
            if (generator.pathMode == TrafficLineGenerator.PathMode.Manual)
            {
                HandleManualPathEditing();
            }
        }

        private void DrawPath(List<Vector3> points)
        {
            if (points.Count < 2) return;

            // Draw the path as a polyline
            Handles.DrawPolyLine(points.ToArray());
        }

        private void DrawGeneralSettings()
        {
            generalSettingsFoldout = EditorGUILayout.Foldout(generalSettingsFoldout, "General Settings", true);
            if (!generalSettingsFoldout) return;

            EditorGUI.indentLevel++;

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(pathWidthProperty, new GUIContent("Path Width", "Width of the path in world units"));
            EditorGUILayout.PropertyField(showLineProperty, new GUIContent("Show Line", "Whether to display the traffic line in the scene view"));
            EditorGUILayout.PropertyField(closedPathProperty, new GUIContent("Closed Path", "Whether the path forms a closed loop"));

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                SceneView.RepaintAll();
            }

            EditorGUI.indentLevel--;
        }

        private void DrawPathModeSettings()
        {
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(pathModeProperty, new GUIContent("Path Mode", "Automatic: Points provided by RoadEditor. Manual: Create and edit points manually."));

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();

                // Reset selection if switching modes
                isAddingPoints = false;
                selectedPointIndex = -1;

                SceneView.RepaintAll();
            }
        }

        private void DrawPathPointsSettings()
        {
            pathPointsFoldout = EditorGUILayout.Foldout(pathPointsFoldout, "Path Points", true);
            if (!pathPointsFoldout) return;

            EditorGUI.indentLevel++;

            // Path points array
            SerializedProperty pointsArray = pathPointsProperty;

            if (generator.pathMode == TrafficLineGenerator.PathMode.Automatic)
            {
                // In automatic mode, show as read-only
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.PropertyField(pointsArray, new GUIContent("Points (from RoadEditor)"), true);
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.HelpBox("These points are provided by the RoadEditor with offsets already applied.", MessageType.Info);
            }
            else
            {
                // In manual mode, allow editing
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(pointsArray, new GUIContent("Points (Manual)"), true);

                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    SceneView.RepaintAll();
                }

                EditorGUILayout.HelpBox("You can add, move, and remove points directly in the Scene view.", MessageType.Info);
            }

            EditorGUI.indentLevel--;
        }

        private void DrawLineSettings()
        {
            lineSettingsFoldout = EditorGUILayout.Foldout(lineSettingsFoldout, "Traffic Line Settings", true);
            if (!lineSettingsFoldout) return;

            EditorGUI.indentLevel++;

            if (generator.showLine)
            {
                EditorGUI.BeginChangeCheck();

                SerializedProperty enabledProp = linePropertiesProperty.FindPropertyRelative("enabled");
                SerializedProperty reverseProp = linePropertiesProperty.FindPropertyRelative("reverseDirection");
                SerializedProperty widthProp = linePropertiesProperty.FindPropertyRelative("lineWidth");
                SerializedProperty spacingProp = linePropertiesProperty.FindPropertyRelative("arrowSpacing");
                SerializedProperty sizeProp = linePropertiesProperty.FindPropertyRelative("arrowSize");

                EditorGUILayout.PropertyField(enabledProp, new GUIContent("Enabled"));
                EditorGUILayout.PropertyField(reverseProp, new GUIContent("Reverse Direction"));
                EditorGUILayout.Slider(widthProp, 0.05f, 2f, new GUIContent("Line Width"));
                EditorGUILayout.Slider(spacingProp, 1f, 10f, new GUIContent("Arrow Spacing"));
                EditorGUILayout.Slider(sizeProp, 0.1f, 2f, new GUIContent("Arrow Size"));

                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    SceneView.RepaintAll();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Enable 'Show Line' to edit traffic line settings.", MessageType.Info);
            }

            EditorGUI.indentLevel--;
        }

        private void DrawManualPathControls()
        {
            if (generator.pathMode != TrafficLineGenerator.PathMode.Manual)
                return;

            // Get button style
            if (buttonStyle == null)
            {
                buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    fixedHeight = 30
                };
            }

            EditorGUILayout.BeginHorizontal();

            // Add/Stop Adding button
            GUI.backgroundColor = isAddingPoints ? Color.red : Color.green;
            if (GUILayout.Button(isAddingPoints ? "Stop Adding Points" : "Add Points", buttonStyle))
            {
                isAddingPoints = !isAddingPoints;
                selectedPointIndex = -1;
                SceneView.RepaintAll();
            }

            GUI.backgroundColor = Color.white;

            // Insert point button (only active when a point is selected)
            EditorGUI.BeginDisabledGroup(selectedPointIndex < 0 || isAddingPoints);
            if (GUILayout.Button("Insert Point After Selected", buttonStyle))
            {
                InsertPointAfterSelected();
                SceneView.RepaintAll();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            // Delete selected point button
            EditorGUI.BeginDisabledGroup(selectedPointIndex < 0 || isAddingPoints);
            GUI.backgroundColor = new Color(1f, 0.6f, 0.6f); // Light red
            if (GUILayout.Button("Delete Selected Point", buttonStyle))
            {
                DeleteSelectedPoint();
                SceneView.RepaintAll();
            }
            GUI.backgroundColor = Color.white;
            EditorGUI.EndDisabledGroup();

            // Clear all points button
            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f); // Stronger red
            if (GUILayout.Button("Clear All Points", buttonStyle))
            {
                if (EditorUtility.DisplayDialog("Clear All Points",
                    "Are you sure you want to delete all path points?", "Yes", "No"))
                {
                    Undo.RecordObject(generator, "Clear All Points");
                    generator.ClearPoints();
                    selectedPointIndex = -1;
                    isAddingPoints = false;
                    SceneView.RepaintAll();
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            // Show selected point index
            if (selectedPointIndex >= 0)
            {
                EditorGUILayout.HelpBox($"Selected Point: {selectedPointIndex}", MessageType.Info);
            }
        }

        private void HandleManualPathEditing()
        {
            if (generator.pathMode != TrafficLineGenerator.PathMode.Manual)
                return;

            Event e = Event.current;
            List<Vector3> worldPoints = generator.GetWorldPathPoints();

            // Draw handles for existing points
            for (int i = 0; i < worldPoints.Count; i++)
            {
                // Draw point handle
                Handles.color = (i == selectedPointIndex) ? Color.yellow : Color.white;
                float handleSize = (i == selectedPointIndex) ? 0.3f : 0.2f;

                EditorGUI.BeginChangeCheck();
                Vector3 newPos = Handles.FreeMoveHandle(worldPoints[i],
                    handleSize, Vector3.zero, Handles.SphereHandleCap);

                // Display index
                Handles.Label(worldPoints[i] + Vector3.up * 0.5f, $"Point {i}");

                // Handle moved
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(generator, "Move Path Point");
                    generator.UpdatePoint(i, newPos);
                    selectedPointIndex = i;
                    isAddingPoints = false;
                    SceneView.RepaintAll();
                }

                // Handle selection
                if (e.type == EventType.MouseDown && e.button == 0)
                {
                    float handleSizeInScreenSpace = HandleUtility.GetHandleSize(worldPoints[i]) * 0.2f;
                    if (Vector2.Distance(e.mousePosition, HandleUtility.WorldToGUIPoint(worldPoints[i])) < handleSizeInScreenSpace * 10)
                    {
                        selectedPointIndex = i;
                        isAddingPoints = false;
                        e.Use();
                        Repaint();
                        SceneView.RepaintAll();
                    }
                }
            }

            // Handle adding new points on click
            if (isAddingPoints && e.type == EventType.MouseDown && e.button == 0)
            {
                // Raycast to where the user clicked
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit))
                {
                    AddPointAt(hit.point);
                }
                else
                {
                    // If no hit, use a plane at y=0
                    Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
                    float distance;
                    if (groundPlane.Raycast(ray, out distance))
                    {
                        Vector3 point = ray.GetPoint(distance);
                        AddPointAt(point);
                    }
                }

                e.Use();
            }
        }

        private void AddPointAt(Vector3 position)
        {
            Undo.RecordObject(generator, "Add Path Point");
            generator.AddPoint(position);
            selectedPointIndex = generator.GetWorldPathPoints().Count - 1;
            SceneView.RepaintAll();
        }

        private void InsertPointAfterSelected()
        {
            if (selectedPointIndex < 0)
                return;

            List<Vector3> worldPoints = generator.GetWorldPathPoints();
            if (worldPoints.Count == 0 || selectedPointIndex >= worldPoints.Count - 1)
                return;

            // Calculate position between selected point and next point
            Vector3 current = worldPoints[selectedPointIndex];
            Vector3 next = worldPoints[selectedPointIndex + 1];
            Vector3 midPoint = Vector3.Lerp(current, next, 0.5f);

            Undo.RecordObject(generator, "Insert Path Point");

            // Add a new point at the midpoint position (will be converted to local internally)
            List<Vector3> localPoints = generator.GetLocalPathPoints();
            Vector3 midLocalPoint = generator.transform.InverseTransformPoint(midPoint);

            // Insert point after the selected point
            serializedObject.Update();
            pathPointsProperty.arraySize = localPoints.Count + 1;

            // Copy existing points
            for (int i = 0; i < localPoints.Count; i++)
            {
                if (i <= selectedPointIndex)
                {
                    // Copy points before insertion point
                    pathPointsProperty.GetArrayElementAtIndex(i).vector3Value = localPoints[i];
                }
                else
                {
                    // Shift points after insertion point
                    pathPointsProperty.GetArrayElementAtIndex(i + 1).vector3Value = localPoints[i];
                }
            }

            // Insert the new point
            pathPointsProperty.GetArrayElementAtIndex(selectedPointIndex + 1).vector3Value = midLocalPoint;
            serializedObject.ApplyModifiedProperties();

            selectedPointIndex = selectedPointIndex + 1;
        }

        private void DeleteSelectedPoint()
        {
            if (selectedPointIndex < 0)
                return;

            Undo.RecordObject(generator, "Delete Path Point");
            generator.RemovePoint(selectedPointIndex);

            // Update selection
            if (generator.GetWorldPathPoints().Count == 0)
            {
                selectedPointIndex = -1;
            }
            else
            {
                selectedPointIndex = Mathf.Clamp(selectedPointIndex, 0, generator.GetWorldPathPoints().Count - 1);
            }
        }
    }
}