using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace AK.MapEditorTools
{
    /// <summary>
    /// Utility class for drawing traffic lines along a path
    /// </summary>
    public class TrafficLineDrawer
    {
        /// <summary>
        /// Traffic Line properties class
        /// </summary>
        [System.Serializable]
        public class TrafficLineProperties
        {
            public bool enabled = true;
            public bool reverseDirection = false;  // Direction of traffic flow
            public float lineWidth = 2f;       // Width of the line
            public float arrowSpacing = 5f;    // Distance between arrows
            public float arrowSize = 2f;       // Size of direction arrows

            public TrafficLineProperties() { }

            // Copy constructor
            public TrafficLineProperties(TrafficLineProperties source)
            {
                if (source == null) return;

                this.enabled = source.enabled;
                this.reverseDirection = source.reverseDirection;
                this.lineWidth = source.lineWidth;
                this.arrowSpacing = source.arrowSpacing;
                this.arrowSize = source.arrowSize;
            }
        }

        /// <summary>
        /// Draw a set of traffic lines along a path
        /// </summary>
        /// <param name="pathPoints">The points that define the path</param>
        /// <param name="trafficLines">The traffic line properties to draw</param>
        /// <param name="pathWidth">The width of the path/road</param>
        /// <param name="isClosed">Whether the path forms a closed loop</param>
        public void DrawTrafficLines(List<Vector3> pathPoints, List<TrafficLineProperties> trafficLines, float pathWidth)
        {
            if (pathPoints == null || pathPoints.Count < 2 || trafficLines == null || trafficLines.Count == 0)
                return;

            // Draw each traffic line
            foreach (var line in trafficLines)
            {
                if (!line.enabled)
                    continue;

                DrawTrafficLine(pathPoints, line);
            }
        }

        /// <summary>
        /// Draw a single traffic line along a path
        /// </summary>
        private void DrawTrafficLine(List<Vector3> pathPoints, TrafficLineProperties line)
        {
            if (pathPoints.Count < 2)
                return;

            Vector3 lastPoint = Vector3.zero;
            Vector3 lastDirection = Vector3.zero;
            float accumulatedDistance = 0;

            // Draw the line itself
            Handles.color = Color.green;
            int stepCount = pathPoints.Count;

            for (int i = 0; i < stepCount; i++)
            {
                Vector3 currentPoint = pathPoints[i];

                // Get direction at this point (only used for arrows)
                Vector3 direction;
                if (i == 0)
                {
                    direction = (pathPoints[1] - pathPoints[0]).normalized;
                }
                else if (i == stepCount - 1)
                {
                    direction = (pathPoints[i] - pathPoints[i - 1]).normalized;
                }
                else
                {
                    // Average direction at this point
                    direction = (pathPoints[i + 1] - pathPoints[i - 1]).normalized;
                }

                // Flip direction if needed
                if (line.reverseDirection)
                    direction = -direction;

                // Draw line segment
                if (i > 0)
                {
                    Handles.DrawAAPolyLine(line.lineWidth, lastPoint, currentPoint);

                    // Calculate distance for this segment
                    float segmentDistance = Vector3.Distance(lastPoint, currentPoint);
                    accumulatedDistance += segmentDistance;

                    // Draw direction arrows
                    if (line.arrowSpacing > 0)
                    {
                        // Draw arrows based on accumulated distance
                        float arrowsToPlace = accumulatedDistance / line.arrowSpacing;
                        if (arrowsToPlace >= 1)
                        {
                            int arrowCount = Mathf.FloorToInt(arrowsToPlace);
                            accumulatedDistance -= arrowCount * line.arrowSpacing;

                            // Draw an arrow at the midpoint of the segment
                            Vector3 arrowPos = Vector3.Lerp(lastPoint, currentPoint, 0.5f);
                            DrawDirectionArrow(arrowPos, direction, line.arrowSize);
                        }
                    }
                }

                lastPoint = currentPoint;
                lastDirection = direction;
            }
        }

        /// <summary>
        /// Draw an arrow to indicate direction
        /// </summary>
        private void DrawDirectionArrow(Vector3 position, Vector3 direction, float size)
        {
            Vector3 right = Quaternion.Euler(0, -30, 0) * -direction * size;
            Vector3 left = Quaternion.Euler(0, 30, 0) * -direction * size;

            Handles.DrawAAPolyLine(2f, position, position + right);
            Handles.DrawAAPolyLine(2f, position, position + left);
        }
    }
}