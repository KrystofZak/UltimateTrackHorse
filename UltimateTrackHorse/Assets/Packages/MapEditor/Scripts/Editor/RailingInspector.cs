using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace AK.MapEditorTools
{
    /// <summary>
    /// Custom inspector for the RailingEditor component.
    /// Provides tools for editing standalone railings in the Unity scene view.
    /// </summary>
    [CustomEditor(typeof(RailingEditor))]
    public class RailingInspector : Editor
    {
        #region Fields

        private RailingEditor railingEditor;
        private bool editingEnabled;
        private bool isDraggingAnchorPoint = false;

        // Foldout states
        private bool railingSettingsFoldout = true;

        // Snapping parameters
        private bool enableSnapping = true;
        private float snapDistance = 1.0f; // Distance in units for snapping

        // UI styles
        private GUIStyle buttonStyle;

        // Bezier curve drawer utility
        private BezierCurveDrawer curveDrawer;

        #endregion

        #region Unity Methods

        private void OnEnable()
        {
            railingEditor = (RailingEditor)target;
            curveDrawer = new BezierCurveDrawer();
            Tools.hidden = editingEnabled;
        }

        private void OnDisable()
        {
            Tools.hidden = false;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Railing Editing", EditorStyles.boldLabel);

            DrawEditModeControls();
            DrawSnapSettings();
            DrawRailingSettings();

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(10);
            DrawActionButtons();
        }

        private void OnSceneGUI()
        {
            if (railingEditor == null) return;

            // Drawing and editing logic for railings in the scene view
            DrawRailing();

            if (railingEditor.currentMode != RailingEditor.EditMode.Disabled)
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
            RailingEditor.EditMode newEditMode = (RailingEditor.EditMode)EditorGUILayout.EnumPopup("Edit Mode", railingEditor.currentMode);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(railingEditor, "Change Railing Edit Mode");
                railingEditor.currentMode = newEditMode;
                editingEnabled = newEditMode != RailingEditor.EditMode.Disabled;
                Tools.hidden = editingEnabled;
            }

            EditorGUI.BeginChangeCheck();
            bool showControlPoints = EditorGUILayout.Toggle("Show Control Points", railingEditor.showControlPoints);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(railingEditor, "Toggle Control Points");
                railingEditor.showControlPoints = showControlPoints;
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

            EditorGUI.BeginChangeCheck();
            bool isClosed = EditorGUILayout.Toggle("Closed Path", railingEditor.isClosed);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(railingEditor, "Toggle Closed Path");
                railingEditor.isClosed = isClosed;
                railingEditor.RegenerateRailings();
                SceneView.RepaintAll();
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

                // Curve settings
                EditorGUILayout.LabelField("Curve Settings", EditorStyles.boldLabel);
                
                EditorGUI.BeginChangeCheck();
                int resolution = EditorGUILayout.IntSlider("Curve Resolution", railingEditor.curveResolution, 1, 20);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(railingEditor, "Change Curve Resolution");
                    railingEditor.curveResolution = resolution;
                    railingEditor.RegenerateRailings();
                    SceneView.RepaintAll();
                }

                EditorGUI.BeginChangeCheck();
                float tension = EditorGUILayout.Slider("Curve Smoothness", railingEditor.curveTension, 0.1f, 0.9f);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(railingEditor, "Change Curve Tension");
                    railingEditor.curveTension = tension;
                    railingEditor.RecalculateAllBezierHandles();
                    SceneView.RepaintAll();
                }

                EditorGUILayout.Space();
                
                // Appearance settings
                EditorGUILayout.LabelField("Appearance", EditorStyles.boldLabel);
                
                EditorGUILayout.PropertyField(serializedObject.FindProperty("railingType"), new GUIContent("Type"));
                
                // Only show additional properties if a railing type is selected
                if (railingEditor.railingType != RailingGenerator.RailingType.None)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("railingMaterial"), new GUIContent("Material"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("railingOffset"), new GUIContent("Offset"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("uvRepeatFactor"), new GUIContent("UV Repeat Factor"));

                    // Show height property specific to the railing type
                    if (railingEditor.railingType == RailingGenerator.RailingType.Wall)
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("wallHeight"), new GUIContent("Wall Height"));
                    }
                    else if (railingEditor.railingType == RailingGenerator.RailingType.Plane)
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("planeHeight"), new GUIContent("Plane Height"));
                    }
                }

                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    railingEditor.RegenerateRailings();
                }

                EditorGUI.indentLevel--;
            }
        }

        /// <summary>
        /// Draw action buttons for railing operations
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
                Undo.RecordObject(railingEditor, "Recalculate Bezier Handles");
                railingEditor.RecalculateAllBezierHandles();
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("Regenerate Railings", buttonStyle))
            {
                Undo.RecordObject(railingEditor, "Regenerate Railings");
                railingEditor.RegenerateRailings();
                SceneView.RepaintAll();
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Clear All Points", buttonStyle))
            {
                ClearAllPoints();
            }

            // Delete selected point button - only enabled when a point is selected in Edit mode
            EditorGUI.BeginDisabledGroup(railingEditor.currentMode != RailingEditor.EditMode.EditPoints ||
                                        railingEditor.selectedPointIndex < 0);
            if (GUILayout.Button("Delete Selected Point", buttonStyle))
            {
                Undo.RecordObject(railingEditor, "Delete Railing Point");
                if (railingEditor.DeleteSelectedPoint())
                {
                    SceneView.RepaintAll();
                }
            }
            EditorGUI.EndDisabledGroup();

            // Help text
            if (railingEditor.currentMode == RailingEditor.EditMode.EditPoints && railingEditor.selectedPointIndex >= 0)
            {
                EditorGUILayout.HelpBox("You can also press the Delete key to remove the selected point.", MessageType.Info);
            }
        }

        /// <summary>
        /// Clear all railing points after confirmation
        /// </summary>
        private void ClearAllPoints()
        {
            if (EditorUtility.DisplayDialog("Clear All Points",
                "Are you sure you want to delete all railing points?", "Yes", "No"))
            {
                Undo.RecordObject(railingEditor, "Clear All Points");
                railingEditor.ClearPoints();
                SceneView.RepaintAll();
            }
        }

        #endregion

        #region Railing Drawing and Editing Methods

        /// <summary>
        /// Draw the railing path and control points in scene view
        /// </summary>
        private void DrawRailing()
        {
            // Draw the path using the curve drawer
            List<Vector3> points = railingEditor.GetBezierCurvePoints();
            curveDrawer.DrawPath(points);

            if (railingEditor.showControlPoints)
            {
                // Convert RailingEditor.BezierPoint to BezierCurveDrawer.BezierPoint
                List<BezierCurveDrawer.BezierPoint> convertedPoints = new List<BezierCurveDrawer.BezierPoint>();
                foreach (var point in railingEditor.GetBezierPoints())
                {
                    convertedPoints.Add(new BezierCurveDrawer.BezierPoint(point.position, point.handleIn, point.handleOut));
                }

                // Draw control points using the curve drawer
                curveDrawer.DrawControlPoints(convertedPoints, railingEditor.selectedPointIndex,
                    (index, handleType) =>
                    {
                        RailingEditor.HandleType railingHandleType = (RailingEditor.HandleType)(int)handleType;
                        if (handleType == BezierCurveDrawer.HandleType.Anchor)
                        {
                            railingEditor.SelectPoint(index);
                            railingEditor.selectedHandleType = railingHandleType;
                            isDraggingAnchorPoint = true;
                            Repaint();
                        }
                        else
                        {
                            railingEditor.SelectHandle(index, railingHandleType);
                            Repaint();
                        }
                    });
            }
        }

        /// <summary>
        /// Handle railing point editing based on current edit mode
        /// </summary>
        private void HandlePointEditing()
        {
            Event e = Event.current;

            // Reset dragging state when mouse is released
            /*if (e.type == EventType.MouseUp)
            {
                isDraggingAnchorPoint = false;
            }*/

            // Handle Delete key press when a point is selected in Edit mode
            if (e.type == EventType.KeyDown && railingEditor.currentMode == RailingEditor.EditMode.EditPoints)
            {
                if (e.keyCode == KeyCode.Delete && railingEditor.selectedPointIndex >= 0)
                {
                    Undo.RecordObject(railingEditor, "Delete Railing Point");
                    if (railingEditor.DeleteSelectedPoint())
                    {
                        e.Use(); // Consume the event
                        SceneView.RepaintAll();
                    }
                }
            }

            if (railingEditor.currentMode == RailingEditor.EditMode.AddPoints)
            {
                HandleAddPointMode(e);
            }
            else if (railingEditor.currentMode == RailingEditor.EditMode.EditPoints)
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

                AddNewRailingPoint(pointToAdd);
                e.Use();
            }
        }

        /// <summary>
        /// Add a new railing point at the specified position
        /// </summary>
        private void AddNewRailingPoint(Vector3 position)
        {
            Vector3 finalPosition = position;

            // Maintain the same height as previous point if available
            if (railingEditor.GetPointCount() > 0)
            {
                Vector3 previousPoint = railingEditor.GetPointAt(railingEditor.GetPointCount() - 1);
                finalPosition.y = previousPoint.y; // Keep the same Y coordinate
            }

            Undo.RecordObject(railingEditor, "Add Railing Point");
            railingEditor.AddPoint(finalPosition);
            SceneView.RepaintAll();
        }

        /// <summary>
        /// Handle point editing mode
        /// </summary>
        private void HandleEditPointMode()
        {
            if (railingEditor.selectedPointIndex < 0)
                return;

            if (railingEditor.selectedHandleType == RailingEditor.HandleType.Anchor && isDraggingAnchorPoint)
            {
                HandleAnchorPointMovement();
            }
            else if (railingEditor.selectedHandleType == RailingEditor.HandleType.InHandle ||
                     railingEditor.selectedHandleType == RailingEditor.HandleType.OutHandle)
            {
                HandleControlPointMovement();
            }
        }

        /// <summary>
        /// Handle anchor point movement
        /// </summary>
        private void HandleAnchorPointMovement()
        {
            var point = railingEditor.GetPointAt(railingEditor.selectedPointIndex);
            EditorGUI.BeginChangeCheck();

            // Use curve drawer to draw the position handle
            Vector3 newPos = curveDrawer.DrawPositionHandle(point);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(railingEditor, "Move Railing Point");
                railingEditor.UpdatePointPosition(railingEditor.selectedPointIndex, newPos);
                SceneView.RepaintAll();
            }
        }

        /// <summary>
        /// Handle control point (handle) movement
        /// </summary>
        private void HandleControlPointMovement()
        {
            var bezierPoint = railingEditor.GetBezierPointAt(railingEditor.selectedPointIndex);
            if (bezierPoint == null)
                return;

            Vector3 handlePos = railingEditor.selectedHandleType == RailingEditor.HandleType.InHandle
                ? bezierPoint.handleIn
                : bezierPoint.handleOut;

            EditorGUI.BeginChangeCheck();

            // Use curve drawer to draw the position handle
            Vector3 newPos = curveDrawer.DrawPositionHandle(handlePos);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(railingEditor, "Move Handle");
                railingEditor.UpdateControlPoint(railingEditor.selectedPointIndex, railingEditor.selectedHandleType, newPos);
                SceneView.RepaintAll();
            }
        }

        #endregion
    }
}