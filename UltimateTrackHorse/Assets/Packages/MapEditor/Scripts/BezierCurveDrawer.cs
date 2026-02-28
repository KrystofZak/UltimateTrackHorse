using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace AK.MapEditorTools
{
    /// <summary>
    /// Utility class for drawing and interacting with bezier curves in the scene view.
    /// Can be used by any editor that needs to draw and manipulate spline paths.
    /// </summary>
    public class BezierCurveDrawer
    {
        #region Enums & Structs

        /// <summary>
        /// Represents a single bezier control point with position and handles
        /// </summary>
        public struct BezierPoint
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

        public enum HandleType { None, Anchor, InHandle, OutHandle }

        #endregion

        #region Fields

        private Color pathColor = Color.green;
        private Color selectedPointColor = Color.yellow;
        private Color normalPointColor = Color.white;
        private Color handleLineColor = Color.blue;
        private Color handlePointColor = Color.red;

        #endregion

        #region Public Methods

        /// <summary>
        /// Draws a bezier curve in the scene view based on the provided points
        /// </summary>
        /// <param name="curvePoints">List of calculated curve points to display</param>
        /// <param name="showCurvePoints">Whether to show debug points along the curve</param>
        public void DrawPath(List<Vector3> curvePoints, bool showCurvePoints = false)
        {
            if (curvePoints == null || curvePoints.Count < 2)
                return;

            Handles.color = pathColor;
            Handles.DrawPolyLine(curvePoints.ToArray());

            // Optionally draw points along the curve for debugging
            if (showCurvePoints)
            {
                Handles.color = Color.green;
                float size = 0.05f;
                foreach (var point in curvePoints)
                {
                    Handles.SphereHandleCap(0, point, Quaternion.identity, size, EventType.Repaint);
                }
            }
        }

        /// <summary>
        /// Draws all bezier control points and handles for editing
        /// </summary>
        /// <param name="bezierPoints">List of bezier points to draw</param>
        /// <param name="selectedPointIndex">Index of the currently selected point (-1 if none)</param>
        /// <param name="onPointSelected">Callback when a point is selected (parameters: index, handle type)</param>
        public void DrawControlPoints(List<BezierPoint> bezierPoints, int selectedPointIndex,
                                      System.Action<int, HandleType> onPointSelected)
        {
            if (bezierPoints == null)
                return;

            for (int i = 0; i < bezierPoints.Count; i++)
            {
                var point = bezierPoints[i];
                bool isSelected = selectedPointIndex == i;

                // Draw anchor point
                Handles.color = isSelected ? selectedPointColor : normalPointColor;
                float size = HandleUtility.GetHandleSize(point.position) * 0.1f;

                if (Handles.Button(point.position, Quaternion.identity, size, size, Handles.SphereHandleCap))
                {
                    if (onPointSelected != null)
                        onPointSelected(i, HandleType.Anchor);
                }

                // Draw handle lines and points
                DrawPointHandles(point, i, selectedPointIndex, onPointSelected);
            }
        }

        /// <summary>
        /// Draws position handle for editing a point's position
        /// </summary>
        /// <param name="position">Current position</param>
        /// <returns>New position after editing</returns>
        public Vector3 DrawPositionHandle(Vector3 position)
        {
            return Handles.PositionHandle(position, Quaternion.identity);
        }

        /// <summary>
        /// Calculates a smooth bezier curve between specified points
        /// </summary>
        /// <param name="bezierPoints">Control points defining the curve</param>
        /// <param name="resolution">Resolution of the curve (points per segment)</param>
        /// <param name="isClosed">Whether the curve forms a closed loop</param>
        /// <returns>List of points along the curve</returns>
        public List<Vector3> CalculateBezierCurvePoints(List<BezierPoint> bezierPoints, int resolution, bool isClosed = false)
        {
            List<Vector3> curvePoints = new List<Vector3>();

            if (bezierPoints == null || bezierPoints.Count < 2)
            {
                if (bezierPoints != null && bezierPoints.Count == 1)
                    curvePoints.Add(bezierPoints[0].position);
                return curvePoints;
            }

            curvePoints.Add(bezierPoints[0].position);
            int segmentCount = isClosed ? bezierPoints.Count : bezierPoints.Count - 1;

            for (int i = 0; i < segmentCount; i++)
            {
                BezierPoint startPoint = bezierPoints[i];
                BezierPoint endPoint = bezierPoints[(i + 1) % bezierPoints.Count]; // Wrap around for closed curves


                for (int j = 1; j <= resolution; j++)
                {
                    float t = j / (float)resolution;
                    curvePoints.Add(CalculateBezierPoint(
                    startPoint.position,
                    startPoint.handleOut,
                    endPoint.handleIn,
                    endPoint.position,
                    t));
                }
            }

            return curvePoints;
        }

        /// <summary>
        /// Automatically set the bezier handles to create a smooth curve
        /// </summary>
        /// <param name="bezierPoints">List of bezier points to recalculate handles for</param>
        /// <param name="tension">Smoothness factor (0-1), higher values create tighter curves</param>
        public void RecalculateHandles(List<BezierPoint> bezierPoints, float tension)
        {
            if (bezierPoints == null || bezierPoints.Count < 2)
                return;

            int pointCount = bezierPoints.Count;
            bool isClosed = false; // Assume open path by default

            // For each point, calculate the tangent direction based on the previous and next points
            for (int i = 0; i < pointCount; i++)
            {
                BezierPoint point = bezierPoints[i];
                Vector3 tangentDirection = Vector3.zero;

                // Get previous and next point indices, handling edge cases
                int prevIndex = i - 1;
                int nextIndex = i + 1;

                if (prevIndex < 0)
                {
                    if (isClosed)
                        prevIndex = pointCount - 1;
                    else
                        prevIndex = -1; // No previous point
                }

                if (nextIndex >= pointCount)
                {
                    if (isClosed)
                        nextIndex = 0;
                    else
                        nextIndex = -1; // No next point
                }

                // Calculate tangent based on available points
                if (prevIndex >= 0 && nextIndex >= 0)
                {
                    // Middle point - use surrounding points to determine tangent
                    Vector3 prevPos = bezierPoints[prevIndex].position;
                    Vector3 nextPos = bezierPoints[nextIndex].position;
                    tangentDirection = (nextPos - prevPos).normalized;
                }
                else if (prevIndex >= 0)
                {
                    // End point - use direction from previous point
                    Vector3 prevPos = bezierPoints[prevIndex].position;
                    tangentDirection = (point.position - prevPos).normalized;
                }
                else if (nextIndex >= 0)
                {
                    // Start point - use direction to next point
                    Vector3 nextPos = bezierPoints[nextIndex].position;
                    tangentDirection = (nextPos - point.position).normalized;
                }

                // Calculate handle lengths based on distances to adjacent points
                float inHandleLength = 0f;
                float outHandleLength = 0f;

                if (prevIndex >= 0)
                {
                    float prevDistance = Vector3.Distance(point.position, bezierPoints[prevIndex].position);
                    inHandleLength = prevDistance * tension;
                }

                if (nextIndex >= 0)
                {
                    float nextDistance = Vector3.Distance(point.position, bezierPoints[nextIndex].position);
                    outHandleLength = nextDistance * tension;
                }

                // Set the handles using the calculated tangent and lengths
                if (prevIndex >= 0)
                    point.handleIn = point.position - tangentDirection * inHandleLength;

                if (nextIndex >= 0)
                    point.handleOut = point.position + tangentDirection * outHandleLength;

                bezierPoints[i] = point;
            }

            // Special case: if only 2 points, make sure handles are properly aligned
            if (pointCount == 2)
            {
                Vector3 direction = (bezierPoints[1].position - bezierPoints[0].position).normalized;
                float distance = Vector3.Distance(bezierPoints[0].position, bezierPoints[1].position);
                float handleLength = distance * tension;

                BezierPoint start = bezierPoints[0];
                BezierPoint end = bezierPoints[1];

                start.handleOut = start.position + direction * handleLength;
                end.handleIn = end.position - direction * handleLength;

                bezierPoints[0] = start;
                bezierPoints[1] = end;
            }
        }

        /// <summary>
        /// Align bezier handles along a specified direction
        /// </summary>
        /// <param name="point">The point to modify</param>
        /// <param name="direction">The direction to align to</param>
        /// <param name="handleLength">Length of the handles</param>
        /// <param name="isEndpoint">Whether this is the first or last point in the curve</param>
        /// <param name="isFirstPoint">True if this is the first point, False if it's the last point</param>
        /// <returns>Modified BezierPoint with aligned handles</returns>
        public BezierPoint AlignHandlesToDirection(BezierPoint point, Vector3 direction, float handleLength)
        {

            // First point: handleOut points toward the road, handleIn points away
            point.handleOut = point.position + direction * handleLength;
            point.handleIn = point.position - direction * handleLength;

            return point;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Draw the bezier handles for a control point
        /// </summary>
        private void DrawPointHandles(BezierPoint point, int pointIndex, int selectedPointIndex,
                                    System.Action<int, HandleType> onHandleSelected)
        {
            // Draw handle lines
            Handles.color = handleLineColor;
            Handles.DrawLine(point.position, point.handleIn);
            Handles.DrawLine(point.position, point.handleOut);

            // Draw handle points
            Handles.color = handlePointColor;
            float handleSize = HandleUtility.GetHandleSize(point.handleIn) * 0.075f;

            if (Handles.Button(point.handleIn, Quaternion.identity, handleSize, handleSize, Handles.SphereHandleCap))
            {
                if (onHandleSelected != null)
                    onHandleSelected(pointIndex, HandleType.InHandle);
            }

            if (Handles.Button(point.handleOut, Quaternion.identity, handleSize, handleSize, Handles.SphereHandleCap))
            {
                if (onHandleSelected != null)
                    onHandleSelected(pointIndex, HandleType.OutHandle);
            }

            // Draw labels for the handles
            GUIStyle style = new GUIStyle();
            style.normal.textColor = handlePointColor;
            style.fontSize = 12;
            style.fontStyle = FontStyle.Bold;

            // Position the labels slightly offset from the handles
            Vector3 inLabelPos = point.handleIn + Vector3.up * handleSize * 2;
            Vector3 outLabelPos = point.handleOut + Vector3.up * handleSize * 2;

            Handles.Label(inLabelPos, "IN", style);
            Handles.Label(outLabelPos, "OUT", style);
        }

        /// <summary>
        /// Calculate a point along a cubic bezier curve
        /// </summary>
        private Vector3 CalculateBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float u = 1 - t;
            float uu = u * u, tt = t * t;
            float uuu = uu * u, ttt = tt * t;

            Vector3 point = uuu * p0;
            point += 3 * uu * t * p1;
            point += 3 * u * tt * p2;
            point += ttt * p3;

            return point;
        }

        #endregion
    }
}