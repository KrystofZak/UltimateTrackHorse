using System.Collections.Generic;
using UnityEngine;

namespace AK.MapEditorTools
{
    [ExecuteInEditMode]
    public class RoadEditor : MonoBehaviour
    {
        public enum EditMode { Disabled, AddPoints, EditPoints }
        public enum HandleType { None, Anchor, InHandle, OutHandle }

        // Traffic Line class for defining traffic line properties
        [System.Serializable]
        public class TrafficLine : TrafficLineDrawer.TrafficLineProperties
        {
            // Road-specific property for traffic line offset
            public float offset = 0f;  // Offset from center of path

            // Default constructor
            public TrafficLine() { }

            // Copy constructor
            public TrafficLine(TrafficLine source) : base(source)
            {
                if (source == null) return;
                this.offset = source.offset;
            }
        }

        [HideInInspector] public EditMode currentMode = EditMode.AddPoints;
        [HideInInspector] public int selectedPointIndex = -1;
        [HideInInspector] public bool showControlPoints = true;
        [HideInInspector] public int curveResolution = 10;
        [HideInInspector] public HandleType selectedHandleType = HandleType.None;
        [HideInInspector] public bool addPointsToStart = false; // Whether to add new points to the start instead of the end
        [Range(0.1f, 0.9f)]
        [HideInInspector]
        public float curveTension = 0.4f;

        [HideInInspector] public RoadGenerator roadGenerator;
        [HideInInspector] public RailingGenerator leftRailingsGenerator;
        [HideInInspector] public RailingGenerator rightRailingsGenerator;

        // List of traffic line generators - one per traffic line
        [HideInInspector] public List<TrafficLineGenerator> trafficLineGenerators = new List<TrafficLineGenerator>();

        // Parent transform for traffic line generators
        [HideInInspector] public Transform trafficLinesParent;

        [SerializeField, HideInInspector] private List<Vector3> savedPoints = new List<Vector3>();
        [SerializeField, HideInInspector] private List<BezierPoint> bezierPoints = new List<BezierPoint>();

        // Reference to the bezier curve drawer - created on demand
        private BezierCurveDrawer curveDrawer;


        [Header("Railings Settings")]
        public Material leftRailingMaterial;
        public Material rightRailingMaterial;
        public float leftRailingOffset = 1.0f;
        public float rightRailingOffset = 1.0f;
        public RailingGenerator.RailingType leftRailingType = RailingGenerator.RailingType.None;
        public RailingGenerator.RailingType rightRailingType = RailingGenerator.RailingType.None;
        public float wallHeight = 1.0f;
        public float planeHeight = 1.0f;
        public float leftUvRepeatFactor = 2.0f;
        public float rightUvRepeatFactor = 2.0f;

        [Header("Traffic Lines Settings")]
        public List<TrafficLine> trafficLines = new List<TrafficLine>();
        public bool showTrafficLines = true;

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

            public BezierPoint(Vector3 pos, Vector3 inHandle, Vector3 outHandle)
            {
                position = pos;
                handleIn = inHandle;
                handleOut = outHandle;
            }
        }

        private void OnEnable()
        {
            // Set up left railings generator
            if (leftRailingsGenerator == null)
            {
                // Try to find existing left generator
                Transform leftGen = transform.Find("LeftRailingsGenerator");
                if (leftGen != null)
                {
                    leftRailingsGenerator = leftGen.GetComponent<RailingGenerator>();
                }

                // Create new one if not found
                if (leftRailingsGenerator == null)
                {
                    GameObject leftGenObj = new GameObject("LeftRailingsGenerator");
                    leftGenObj.transform.SetParent(transform, false);
                    leftRailingsGenerator = leftGenObj.AddComponent<RailingGenerator>();
                    leftRailingsGenerator.railingSide = RailingGenerator.RailingSide.Left;
                }
            }

            // Set up right railings generator
            if (rightRailingsGenerator == null)
            {
                // Try to find existing right generator
                Transform rightGen = transform.Find("RightRailingsGenerator");
                if (rightGen != null)
                {
                    rightRailingsGenerator = rightGen.GetComponent<RailingGenerator>();
                }

                // Create new one if not found
                if (rightRailingsGenerator == null)
                {
                    GameObject rightGenObj = new GameObject("RightRailingsGenerator");
                    rightGenObj.transform.SetParent(transform, false);
                    rightRailingsGenerator = rightGenObj.AddComponent<RailingGenerator>();
                    rightRailingsGenerator.railingSide = RailingGenerator.RailingSide.Right;
                }
            }

            // Set up parent object for traffic line generators if needed
            if (trafficLinesParent == null)
            {
                Transform trafficParent = transform.Find("TrafficLines");
                if (trafficParent != null)
                {
                    trafficLinesParent = trafficParent;
                }
                else
                {
                    GameObject parentObj = new GameObject("TrafficLines");
                    parentObj.transform.SetParent(transform, false);
                    trafficLinesParent = parentObj.transform;
                }
            }

            // Ensure we have at least one traffic line by default
            if (trafficLines.Count == 0)
            {
                trafficLines.Add(new TrafficLine());
            }

            // Update railings generator properties
            UpdateRailingsGeneratorProperties();
            RegenerateRailings();
            RegenerateTrafficLines();

            // Ensure we have a curve drawer
            if (curveDrawer == null)
            {
                curveDrawer = new BezierCurveDrawer();
            }
        }

        /// <summary>
        /// Force regeneration of railings without changing the road mesh
        /// </summary>
        public void RegenerateRailings()
        {
            if (roadGenerator != null)
            {
                // Update railing generators properties
                UpdateRailingsGeneratorProperties();

                List<Vector3> pathPoints = roadGenerator.GetPathPoints();
                bool isClosed = roadGenerator.IsClosedPath();

                // Generate railings with current path points
                if (leftRailingsGenerator != null && leftRailingType != RailingGenerator.RailingType.None)
                {
                    leftRailingsGenerator.GenerateRailing(pathPoints);
                }

                if (rightRailingsGenerator != null && rightRailingType != RailingGenerator.RailingType.None)
                {
                    rightRailingsGenerator.GenerateRailing(pathPoints);
                }
            }
        }

        private void UpdateRailingsGeneratorProperties()
        {
            // Update left railings generator
            if (leftRailingsGenerator != null)
            {
                leftRailingsGenerator.offset = leftRailingOffset;
                leftRailingsGenerator.railingType = leftRailingType;
                leftRailingsGenerator.material = leftRailingMaterial;
                leftRailingsGenerator.wallHeight = wallHeight;
                leftRailingsGenerator.planeHeight = planeHeight;
                leftRailingsGenerator.uvRepeatFactor = leftUvRepeatFactor;
                leftRailingsGenerator.railingSide = RailingGenerator.RailingSide.Left;
            }

            // Update right railings generator
            if (rightRailingsGenerator != null)
            {
                rightRailingsGenerator.offset = rightRailingOffset;
                rightRailingsGenerator.railingType = rightRailingType;
                rightRailingsGenerator.material = rightRailingMaterial;
                rightRailingsGenerator.wallHeight = wallHeight;
                rightRailingsGenerator.planeHeight = planeHeight;
                rightRailingsGenerator.uvRepeatFactor = rightUvRepeatFactor;
                rightRailingsGenerator.railingSide = RailingGenerator.RailingSide.Right;
            }
        }

        public void UpdateRoadGenerator()
        {
            if (roadGenerator != null)
            {
                roadGenerator.RegenerateMesh();
                RegenerateRailings();
                RegenerateTrafficLines();
            }
        }

        public void AddPoint(Vector3 point)
        {
            if (savedPoints == null)
            {
                savedPoints = new List<Vector3>();
                bezierPoints = new List<BezierPoint>();
            }

            BezierPoint newPoint;

            if (addPointsToStart)
            {
                savedPoints.Insert(0, point);
                newPoint = new BezierPoint(point);
                bezierPoints.Insert(0, newPoint);
            }
            else
            {
                savedPoints.Add(point);
                newPoint = new BezierPoint(point);
                bezierPoints.Add(newPoint);
            }

            if (bezierPoints.Count > 1)
            {
                SmoothLastPoints();
            }

            if (currentMode == EditMode.EditPoints)
                SelectPoint(addPointsToStart ? 0 : GetPointCount() - 1);

            UpdateRoadGenerator();
        }

        // Helper method to smooth the last 2-3 points of the path
        private void SmoothLastPoints()
        {
            int pointCount = bezierPoints.Count;
            if (pointCount < 2) return;

            // Special case for 2 points - create a simple curve
            if (pointCount == 2)
            {
                BezierPoint p0 = bezierPoints[0];
                BezierPoint p1 = bezierPoints[1];
                Vector3 direction = (p1.position - p0.position).normalized;
                float segmentLength = Vector3.Distance(p1.position, p0.position);
                float handleLength = segmentLength * curveTension;

                p0.handleOut = p0.position + direction * handleLength;
                p1.handleIn = p1.position - direction * handleLength;
                p1.handleOut = p1.position + direction * handleLength;

                bezierPoints[0] = p0;
                bezierPoints[1] = p1;
                return;
            }

            // For 3+ points
            int lastIndex = pointCount - 1;

            if (pointCount >= 3)
            {
                // Last 3 points
                BezierPoint lastPoint = bezierPoints[lastIndex];
                BezierPoint prevPoint = bezierPoints[lastIndex - 1];
                BezierPoint prevPrevPoint = bezierPoints[lastIndex - 2];

                // Directions
                Vector3 dirPrevToCurrent = (lastPoint.position - prevPoint.position).normalized;
                Vector3 dirPrevPrevToPrev = (prevPoint.position - prevPrevPoint.position).normalized;

                // Lengths
                float lastSegmentLength = Vector3.Distance(lastPoint.position, prevPoint.position);
                float prevSegmentLength = Vector3.Distance(prevPoint.position, prevPrevPoint.position);

                // Handle lengths
                float lastHandleLength = lastSegmentLength * curveTension;
                float prevHandleLength = prevSegmentLength * curveTension;

                // Average direction for smooth transition at the middle point
                Vector3 avgDirection = ((dirPrevPrevToPrev + dirPrevToCurrent) * 0.5f).normalized;

                // Set handles for smooth curves
                //prevPrevPoint.handleOut = prevPrevPoint.position + dirPrevPrevToPrev * prevHandleLength;

                prevPoint.handleIn = prevPoint.position - avgDirection * prevHandleLength;
                prevPoint.handleOut = prevPoint.position + avgDirection * lastHandleLength;

                lastPoint.handleIn = lastPoint.position - dirPrevToCurrent * lastHandleLength;
                lastPoint.handleOut = lastPoint.position + dirPrevToCurrent * lastHandleLength;

                // Update points
                bezierPoints[lastIndex - 2] = prevPrevPoint;
                bezierPoints[lastIndex - 1] = prevPoint;
                bezierPoints[lastIndex] = lastPoint;
            }
        }

        public void UpdatePointPosition(int index, Vector3 newPosition)
        {
            if (index >= 0 && index < savedPoints.Count)
            {
                Vector3 offset = newPosition - savedPoints[index];
                savedPoints[index] = newPosition;

                BezierPoint bezierPoint = bezierPoints[index];
                bezierPoint.position = newPosition;
                bezierPoint.handleIn += offset;
                bezierPoint.handleOut += offset;
            }
            UpdateRoadGenerator();
        }

        public void UpdateControlPoint(int pointIndex, HandleType handleType, Vector3 newPosition)
        {
            if (pointIndex < 0 || pointIndex >= bezierPoints.Count) return;

            BezierPoint point = bezierPoints[pointIndex];
            switch (handleType)
            {
                case HandleType.Anchor:
                    Vector3 offset = newPosition - point.position;
                    savedPoints[pointIndex] = newPosition;
                    point.position = newPosition;
                    point.handleIn += offset;
                    point.handleOut += offset;
                    break;
                case HandleType.InHandle: point.handleIn = newPosition; break;
                case HandleType.OutHandle: point.handleOut = newPosition; break;
            }
            bezierPoints[pointIndex] = point;
            UpdateRoadGenerator();
        }

        /// <summary>
        /// Updates a bezier point at the specified index
        /// </summary>
        public void UpdateBezierPoint(int pointIndex, BezierPoint newPoint)
        {
            if (pointIndex < 0 || pointIndex >= bezierPoints.Count) return;

            bezierPoints[pointIndex] = newPoint;
            savedPoints[pointIndex] = newPoint.position;
            UpdateRoadGenerator();
        }

        public void SelectPoint(int index) => selectedPointIndex = (index >= -1 && index < savedPoints.Count) ? index : -1;

        public void SelectHandle(int pointIndex, HandleType handleType)
        {
            selectedPointIndex = pointIndex;
            selectedHandleType = handleType;
        }

        public List<Vector3> GetSavedPoints() => new List<Vector3>(savedPoints);
        public List<BezierPoint> GetBezierPoints() => bezierPoints;
        public int GetPointCount() => savedPoints.Count;
        public Vector3 GetPointAt(int index) => (index >= 0 && index < savedPoints.Count) ? savedPoints[index] : Vector3.zero;
        public BezierPoint GetBezierPointAt(int index) => (index >= 0 && index < bezierPoints.Count) ? bezierPoints[index] : null;

        public void ClearPoints()
        {
            savedPoints.Clear();
            bezierPoints.Clear();
            selectedPointIndex = -1;
            selectedHandleType = HandleType.None;
            UpdateRoadGenerator();
        }

        public List<Vector3> GetBezierCurvePoints()
        {
            // Ensure we have a curve drawer
            if (curveDrawer == null)
            {
                curveDrawer = new BezierCurveDrawer();
            }

            if (bezierPoints.Count < 2)
            {
                List<Vector3> simplePoints = new List<Vector3>();
                foreach (var point in bezierPoints)
                    simplePoints.Add(point.position);
                return simplePoints;
            }

            // Convert our bezier points to the format expected by the curve drawer
            List<BezierCurveDrawer.BezierPoint> convertedPoints = new List<BezierCurveDrawer.BezierPoint>();
            foreach (var point in bezierPoints)
            {
                convertedPoints.Add(new BezierCurveDrawer.BezierPoint(
                    point.position, point.handleIn, point.handleOut));
            }

            // Use the curve drawer to calculate the points
            bool isClosedPath = roadGenerator != null && roadGenerator.IsClosedPath();
            return curveDrawer.CalculateBezierCurvePoints(convertedPoints, curveResolution, isClosedPath);
        }

        public void SyncPointsWithBezierPoints()
        {
            if (savedPoints != null && savedPoints.Count > 0 &&
                (bezierPoints == null || bezierPoints.Count != savedPoints.Count))
            {
                bezierPoints = new List<BezierPoint>(savedPoints.Count);
                foreach (var point in savedPoints)
                    bezierPoints.Add(new BezierPoint(point));
            }
            else if (bezierPoints != null && bezierPoints.Count > 0 &&
                    (savedPoints == null || savedPoints.Count != bezierPoints.Count))
            {
                savedPoints = new List<Vector3>(bezierPoints.Count);
                foreach (var bezierPoint in bezierPoints)
                    savedPoints.Add(bezierPoint.position);
            }
        }

        // This method recalculates all bezier handles based on current curveTension
        public void RecalculateAllBezierHandles()
        {
            if (bezierPoints.Count < 2) return;

            // Ensure we have a curve drawer
            if (curveDrawer == null)
            {
                curveDrawer = new BezierCurveDrawer();
            }

            // Convert our bezier points to the format expected by the curve drawer
            List<BezierCurveDrawer.BezierPoint> convertedPoints = new List<BezierCurveDrawer.BezierPoint>();
            foreach (var point in bezierPoints)
            {
                convertedPoints.Add(new BezierCurveDrawer.BezierPoint(
                    point.position, point.handleIn, point.handleOut));
            }

            // Use the curve drawer to recalculate the handles
            curveDrawer.RecalculateHandles(convertedPoints, curveTension);

            // Convert back to our format
            for (int i = 0; i < bezierPoints.Count; i++)
            {
                if (i < convertedPoints.Count)
                {
                    bezierPoints[i].handleIn = convertedPoints[i].handleIn;
                    bezierPoints[i].handleOut = convertedPoints[i].handleOut;
                }
            }

            UpdateRoadGenerator();
        }

        /// <summary>
        /// Deletes the currently selected point
        /// </summary>
        public bool DeleteSelectedPoint()
        {
            // Check if there's a selected point and it's a valid index
            if (selectedPointIndex >= 0 && selectedPointIndex < savedPoints.Count)
            {
                // Remove the point from both lists
                savedPoints.RemoveAt(selectedPointIndex);
                bezierPoints.RemoveAt(selectedPointIndex);

                // Reset the selected point and handle
                selectedHandleType = HandleType.None;

                // If there are still points, select the previous point or the last point if we deleted the first one
                if (savedPoints.Count > 0)
                {
                    // If we deleted the last point, select the new last point
                    if (selectedPointIndex >= savedPoints.Count)
                    {
                        selectedPointIndex = savedPoints.Count - 1;
                    }
                    // Otherwise keep the same index (which now points to the next point)
                }
                else
                {
                    // No more points
                    selectedPointIndex = -1;
                }

                // If we have at least 2 points left, recalculate the bezier handles
                // to ensure smooth curves after deletion
                if (bezierPoints.Count >= 2)
                {
                    RecalculateAllBezierHandles();
                }

                // Update the road mesh
                UpdateRoadGenerator();
                return true;
            }

            return false;
        }

        private void Update()
        {
            // Check for Delete key press when in edit mode and a point is selected
            if (currentMode == EditMode.EditPoints && selectedPointIndex >= 0)
            {
                // Check for Delete/Cancel key press (Delete on Windows, typically)
                if (Input.GetKeyDown(KeyCode.K))
                {
                    DeleteSelectedPoint();
                }
            }
        }

#if UNITY_EDITOR
        // Draw traffic lines in the scene view
        public void DrawTrafficLines()
        {
            if (!showTrafficLines || trafficLines.Count == 0 || roadGenerator == null || bezierPoints.Count < 2)
                return;

            // Tell each generator to draw its line
            foreach (var generator in trafficLineGenerators)
            {
                if (generator != null && generator.showLine)
                {
                    generator.DrawLine();
                }
            }
        }

        // Calculate an offset path based on the base path and an offset distance
        private List<Vector3> CalculateOffsetPath(List<Vector3> basePath, float offset)
        {
            if (basePath == null || basePath.Count < 2)
                return basePath;

            List<Vector3> offsetPath = new List<Vector3>(basePath.Count);

            for (int i = 0; i < basePath.Count; i++)
            {
                // Calculate direction at this point
                Vector3 direction;
                if (i == 0)
                {
                    direction = (basePath[1] - basePath[0]).normalized;
                }
                else if (i == basePath.Count - 1)
                {

                    direction = (basePath[i] - basePath[i - 1]).normalized;
                }
                else
                {
                    // Average direction at this point
                    direction = (basePath[i + 1] - basePath[i - 1]).normalized;
                }

                // Calculate perpendicular vector (cross product with up vector)
                Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized;

                // Calculate the offset point
                Vector3 offsetPoint = basePath[i] + perpendicular * offset;

                offsetPath.Add(offsetPoint);
            }

            return offsetPath;
        }

        // Add a new traffic line
        public void AddTrafficLine()
        {
            if (trafficLines == null)
                trafficLines = new List<TrafficLine>();

            // Create a new traffic line with offset based on existing lines
            TrafficLine newLine = new TrafficLine();

            // If we have existing lines, offset this one slightly
            if (trafficLines.Count > 0)
            {
                newLine.offset = trafficLines[trafficLines.Count - 1].offset + 1.0f;
            }

            trafficLines.Add(newLine);
            RegenerateTrafficLines();
        }

        // Remove a traffic line
        public void RemoveTrafficLine(int index)
        {
            if (index >= 0 && index < trafficLines.Count)
            {
                trafficLines.RemoveAt(index);
            }

            RegenerateTrafficLines();
        }
#endif

        /// <summary>
        /// Synchronizes the traffic line generators with the traffic lines list
        /// </summary>
        private void RegenerateTrafficLinesStructure()
        {
            if (trafficLinesParent == null)
                return;

            // Remove all null generators
            for (int i = trafficLineGenerators.Count - 1; i >= 0; i--)
            {
                if (trafficLineGenerators[i] == null)
                {
                    trafficLineGenerators.RemoveAt(i);
                }
            }

            // Clean up existing generators if there are too many
            while (trafficLineGenerators.Count > trafficLines.Count)
            {
                int lastIndex = trafficLineGenerators.Count - 1;
                if (trafficLineGenerators[lastIndex] != null)
                {
                    DestroyImmediate(trafficLineGenerators[lastIndex].gameObject);
                }
                trafficLineGenerators.RemoveAt(lastIndex);
            }

            // Find existing generators that might have been created before
            if (trafficLineGenerators.Count == 0)
            {
                foreach (Transform child in trafficLinesParent)
                {
                    TrafficLineGenerator generator = child.GetComponent<TrafficLineGenerator>();
                    if (generator != null)
                    {
                        trafficLineGenerators.Add(generator);
                    }
                }
            }

            // Create new generators if needed
            while (trafficLineGenerators.Count < trafficLines.Count)
            {
                int index = trafficLineGenerators.Count;
                GameObject lineObj = new GameObject($"TrafficLine_{index + 1}");
                lineObj.transform.SetParent(trafficLinesParent, false);
                var generator = lineObj.AddComponent<TrafficLineGenerator>();
                trafficLineGenerators.Add(generator);
            }
        }

        /// <summary>
        /// Updates all traffic line generators with the current properties and path points
        /// </summary>
        public void RegenerateTrafficLines()
        {
            // Get path and path properties
            var basePath = GetBezierCurvePoints();
            float roadWidth = roadGenerator != null ? roadGenerator.roadWidth : 4f;

            // Skip if no path points or no traffic lines
            if (trafficLines == null || trafficLines.Count == 0 || basePath == null || basePath.Count < 2)
                return;

            // First make sure we have the right number of generators
            RegenerateTrafficLinesStructure();

            // Update each traffic line generator
            for (int i = 0; i < trafficLines.Count; i++)
            {
                if (i >= trafficLineGenerators.Count)
                    break;

                var line = trafficLines[i];
                var generator = trafficLineGenerators[i];

                // Skip disabled lines
                if (!line.enabled)
                {
                    generator.showLine = false;
                    continue;
                }

                // Calculate offset path for this traffic line
                List<Vector3> offsetPath = CalculateOffsetPath(basePath, line.offset);

                // Update general properties
                generator.pathWidth = roadWidth;
                generator.showLine = showTrafficLines;

                // Update line properties
                generator.lineProperties.enabled = line.enabled;
                generator.lineProperties.reverseDirection = line.reverseDirection;
                generator.lineProperties.lineWidth = line.lineWidth;
                generator.lineProperties.arrowSpacing = line.arrowSpacing;
                generator.lineProperties.arrowSize = line.arrowSize;

                // Set the offset path
                generator.SetPathPoints(offsetPath);
            }
        }
    }
}