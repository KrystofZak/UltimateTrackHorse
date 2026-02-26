using UnityEngine;
using System.Collections.Generic;

namespace AK.MapEditorTools
{
    [ExecuteInEditMode]
    public class RailingEditor : MonoBehaviour
    {
        public enum EditMode { Disabled, AddPoints, EditPoints }
        public enum HandleType { None, Anchor, InHandle, OutHandle }

        [HideInInspector] public EditMode currentMode = EditMode.Disabled;
        [HideInInspector] public int selectedPointIndex = -1;
        [HideInInspector] public bool showControlPoints = true;
        [HideInInspector] public int curveResolution = 10;
        [HideInInspector] public HandleType selectedHandleType = HandleType.None;
        [Range(0.1f, 0.9f)]
        [HideInInspector]
        public float curveTension = 0.4f;

        [HideInInspector] public RailingGenerator railingsGenerator;

        // Railing Settings
        [Header("Railing Settings")]
        public RailingGenerator.RailingType railingType = RailingGenerator.RailingType.Wall;
        public Material railingMaterial;
        public float railingOffset = 0.0f; // For standalone railings, default to 0 offset
        public float wallHeight = 1.0f;
        public float planeHeight = 1.0f;
        public float uvRepeatFactor = 2.0f;
        public bool isClosed = false;

        [SerializeField, HideInInspector] private List<Vector3> savedPoints = new List<Vector3>();
        [SerializeField, HideInInspector] private List<BezierPoint> bezierPoints = new List<BezierPoint>();

        // Reference to the bezier curve drawer - created on demand
        private BezierCurveDrawer curveDrawer;

        [System.Serializable]
        public class BezierPoint
        {
            public Vector3 position;
            public Vector3 handleIn;
            public Vector3 handleOut;

            public BezierPoint(Vector3 pos)
            {
                position = pos;
                handleIn = pos + new Vector3(-1, 0, 0);
                handleOut = pos + new Vector3(1, 0, 0);
            }

            public BezierPoint(Vector3 position, Vector3 handleIn, Vector3 handleOut)
            {
                this.position = position;
                this.handleIn = handleIn;
                this.handleOut = handleOut;
            }
        }

        private void OnEnable()
        {
            // Set up railings generator
            if (railingsGenerator == null)
            {
                // Try to find existing generator
                Transform gen = transform.Find("RailingsGenerator");
                if (gen != null)
                {
                    railingsGenerator = gen.GetComponent<RailingGenerator>();
                }
                
                // Create new one if not found
                if (railingsGenerator == null)
                {
                    GameObject genObj = new GameObject("RailingsGenerator");
                    genObj.transform.SetParent(transform, false);
                    railingsGenerator = genObj.AddComponent<RailingGenerator>();
                    railingsGenerator.railingSide = RailingGenerator.RailingSide.Left; // Default side
                }
            }

            // Update railings generator properties
            UpdateRailingsGeneratorProperties();

            // Ensure we have a curve drawer
            if (curveDrawer == null)
            {
                curveDrawer = new BezierCurveDrawer();
            }
        }

        /// <summary>
        /// Force regeneration of railings 
        /// </summary>
        public void RegenerateRailings()
        {
            // Update railing generator properties
            UpdateRailingsGeneratorProperties();

            List<Vector3> pathPoints = GetBezierCurvePoints();

            // Generate railing with current path points
            if (railingsGenerator != null && railingType != RailingGenerator.RailingType.None) 
            {
                railingsGenerator.GenerateRailing(pathPoints);
            }
        }

        private void UpdateRailingsGeneratorProperties()
        {
            // Update railings generator
            if (railingsGenerator != null)
            {
                railingsGenerator.offset = railingOffset;
                railingsGenerator.railingType = railingType;
                railingsGenerator.material = railingMaterial;
                railingsGenerator.wallHeight = wallHeight;
                railingsGenerator.planeHeight = planeHeight;
                railingsGenerator.uvRepeatFactor = uvRepeatFactor;
                // We use left side by default for standalone railings
                railingsGenerator.railingSide = RailingGenerator.RailingSide.Left;
            }
        }

        public void AddPoint(Vector3 point)
        {
            // Add point to stored lists
            savedPoints.Add(point);
            BezierPoint newBezierPoint = new BezierPoint(point);
            
            // If this is not the first point, calculate handles based on the previous point
            if (bezierPoints.Count > 0)
            {
                BezierPoint prevPoint = bezierPoints[bezierPoints.Count - 1];
                Vector3 direction = (point - prevPoint.position).normalized;
                float distance = Vector3.Distance(point, prevPoint.position);
                float handleLength = distance * curveTension;
                
                // Set the out handle of the previous point
                prevPoint.handleOut = prevPoint.position + direction * handleLength;
                bezierPoints[bezierPoints.Count - 1] = prevPoint;
                
                // Set the in handle of the new point
                newBezierPoint.handleIn = point - direction * handleLength;
            }
            
            bezierPoints.Add(newBezierPoint);

            // Generate the mesh if we have at least 2 points
            if (bezierPoints.Count >= 2)
            {
                RegenerateRailings();
            }
        }

        public void UpdatePointPosition(int index, Vector3 newPosition)
        {
            if (index < 0 || index >= bezierPoints.Count)
                return;

            Vector3 oldPosition = bezierPoints[index].position;
            Vector3 delta = newPosition - oldPosition;

            // Update the position in both lists
            savedPoints[index] = newPosition;
            
            // Update bezier point and its handles
            BezierPoint point = bezierPoints[index];
            point.position = newPosition;
            point.handleIn += delta;
            point.handleOut += delta;
            bezierPoints[index] = point;

            RegenerateRailings();
        }

        public void UpdateControlPoint(int pointIndex, HandleType handleType, Vector3 newPosition)
        {
            if (pointIndex < 0 || pointIndex >= bezierPoints.Count)
                return;

            BezierPoint point = bezierPoints[pointIndex];

            if (handleType == HandleType.InHandle)
            {
                point.handleIn = newPosition;
            }
            else if (handleType == HandleType.OutHandle)
            {
                point.handleOut = newPosition;
            }

            bezierPoints[pointIndex] = point;
            RegenerateRailings();
        }
        
        /// <summary>
        /// Updates a bezier point at the specified index
        /// </summary>
        public void UpdateBezierPoint(int index, BezierPoint point)
        {
            if (index < 0 || index >= bezierPoints.Count)
                return;
            
            bezierPoints[index] = point;
            savedPoints[index] = point.position;
            
            RegenerateRailings();
        }

        /// <summary>
        /// Recalculates all bezier handles to create a smooth curve
        /// </summary>
        public void RecalculateAllBezierHandles()
        {
            if (curveDrawer == null)
                curveDrawer = new BezierCurveDrawer();

            // Convert our bezier points to the format used by BezierCurveDrawer
            List<BezierCurveDrawer.BezierPoint> drawerPoints = new List<BezierCurveDrawer.BezierPoint>();
            foreach (var point in bezierPoints)
            {
                drawerPoints.Add(new BezierCurveDrawer.BezierPoint(point.position, point.handleIn, point.handleOut));
            }

            // Use the drawer to recalculate handles
            curveDrawer.RecalculateHandles(drawerPoints, curveTension);

            // Convert back to our format
            for (int i = 0; i < drawerPoints.Count; i++)
            {
                bezierPoints[i] = new BezierPoint(drawerPoints[i].position, drawerPoints[i].handleIn, drawerPoints[i].handleOut);
            }

            RegenerateRailings();
        }

        /// <summary>
        /// Delete the currently selected bezier point
        /// </summary>
        /// <returns>True if a point was deleted</returns>
        public bool DeleteSelectedPoint()
        {
            if (selectedPointIndex >= 0 && selectedPointIndex < bezierPoints.Count)
            {
                bezierPoints.RemoveAt(selectedPointIndex);
                savedPoints.RemoveAt(selectedPointIndex);
                
                // Reset selection
                selectedPointIndex = -1;
                selectedHandleType = HandleType.None;

                // If we still have points, update the railing
                if (bezierPoints.Count > 1)
                {
                    // Recalculate handles for smooth connections
                    RecalculateAllBezierHandles();
                }
                else
                {
                    // Clear the railing if there aren't enough points
                    CleanupRailing();
                }
                
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Remove all points and clear the railing
        /// </summary>
        public void ClearPoints()
        {
            bezierPoints.Clear();
            savedPoints.Clear();
            
            selectedPointIndex = -1;
            selectedHandleType = HandleType.None;
            
            CleanupRailing();
        }

        /// <summary>
        /// Cleanup the generated railing
        /// </summary>
        private void CleanupRailing()
        {
            if (railingsGenerator != null)
            {
                railingsGenerator.CleanupRailings();
            }
        }

        /// <summary>
        /// Select a bezier point by index
        /// </summary>
        public void SelectPoint(int index)
        {
            selectedPointIndex = index;
            selectedHandleType = HandleType.Anchor;
        }

        /// <summary>
        /// Select a specific handle for editing
        /// </summary>
        public void SelectHandle(int pointIndex, HandleType handleType)
        {
            selectedPointIndex = pointIndex;
            selectedHandleType = handleType;
        }

        /// <summary>
        /// Get the total number of points
        /// </summary>
        public int GetPointCount()
        {
            return bezierPoints.Count;
        }

        /// <summary>
        /// Get a point position at the specified index
        /// </summary>
        public Vector3 GetPointAt(int index)
        {
            if (index >= 0 && index < bezierPoints.Count)
            {
                return bezierPoints[index].position;
            }
            return Vector3.zero;
        }

        /// <summary>
        /// Get a bezier point at the specified index
        /// </summary>
        public BezierPoint GetBezierPointAt(int index)
        {
            if (index >= 0 && index < bezierPoints.Count)
            {
                return bezierPoints[index];
            }
            return null;
        }

        /// <summary>
        /// Get all bezier points
        /// </summary>
        public List<BezierPoint> GetBezierPoints()
        {
            return bezierPoints;
        }

        /// <summary>
        /// Get calculated bezier curve points for drawing and mesh generation
        /// </summary>
        public List<Vector3> GetBezierCurvePoints()
        {
            if (curveDrawer == null)
                curveDrawer = new BezierCurveDrawer();

            // Convert our bezier points to the format used by BezierCurveDrawer
            List<BezierCurveDrawer.BezierPoint> drawerPoints = new List<BezierCurveDrawer.BezierPoint>();
            foreach (var point in bezierPoints)
            {
                drawerPoints.Add(new BezierCurveDrawer.BezierPoint(point.position, point.handleIn, point.handleOut));
            }

            return curveDrawer.CalculateBezierCurvePoints(drawerPoints, curveResolution, isClosed);
        }
    }
}