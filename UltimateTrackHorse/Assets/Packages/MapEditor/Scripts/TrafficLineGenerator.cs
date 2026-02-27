using System.Collections.Generic;
using UnityEngine;

namespace AK.MapEditorTools
{
    /// <summary>
    /// Component for generating and displaying a single traffic line along a path
    /// </summary>
    [ExecuteInEditMode]
    public class TrafficLineGenerator : MonoBehaviour
    {
        public enum PathMode { Automatic, Manual }

        // Traffic line settings
        public TrafficLineDrawer.TrafficLineProperties lineProperties = new TrafficLineDrawer.TrafficLineProperties();
        public float pathWidth = 4.0f;
        public bool showLine = true; public PathMode pathMode = PathMode.Automatic;

        // Path points - stored in local space, relative to transform
        [SerializeField]
        private List<Vector3> pathPoints = new List<Vector3>();

        private TrafficLineDrawer lineDrawer;

        private void OnEnable()
        {
            if (lineDrawer == null)
                lineDrawer = new TrafficLineDrawer();
        }

        /// <summary>
        /// Set the path points manually (converts from world to local space)
        /// </summary>
        public void SetPathPoints(List<Vector3> worldPoints)
        {
            // Only update points if in automatic mode
            if (pathMode == PathMode.Automatic)
            {
                // Convert world points to local space
                pathPoints = new List<Vector3>(worldPoints.Count);
                foreach (Vector3 worldPoint in worldPoints)
                {
                    pathPoints.Add(transform.InverseTransformPoint(worldPoint));
                }
            }
        }

        /// <summary>
        /// Get the current path points in world space
        /// </summary>
        public List<Vector3> GetWorldPathPoints()
        {
            List<Vector3> worldPoints = new List<Vector3>(pathPoints.Count);
            foreach (Vector3 localPoint in pathPoints)
            {
                worldPoints.Add(transform.TransformPoint(localPoint));
            }
            return worldPoints;
        }

        /// <summary>
        /// Get the current path points in local space
        /// </summary>
        public List<Vector3> GetLocalPathPoints()
        {
            return new List<Vector3>(pathPoints);
        }

        /// <summary>
        /// Add a point to the manual path (converts from world to local)
        /// </summary>
        public void AddPoint(Vector3 worldPoint)
        {
            if (pathMode == PathMode.Manual)
            {
                pathPoints.Add(transform.InverseTransformPoint(worldPoint));
            }
        }

        /// <summary>
        /// Update a point in the manual path (converts from world to local)
        /// </summary>
        public void UpdatePoint(int index, Vector3 worldPosition)
        {
            if (pathMode == PathMode.Manual && index >= 0 && index < pathPoints.Count)
            {
                pathPoints[index] = transform.InverseTransformPoint(worldPosition);
            }
        }

        /// <summary>
        /// Remove a point from the manual path
        /// </summary>
        public void RemovePoint(int index)
        {
            if (pathMode == PathMode.Manual && index >= 0 && index < pathPoints.Count)
            {
                pathPoints.RemoveAt(index);
            }
        }

        /// <summary>
        /// Clear all points from the manual path
        /// </summary>
        public void ClearPoints()
        {
            if (pathMode == PathMode.Manual)
            {
                pathPoints.Clear();
            }
        }

        /// <summary>
        /// Draw the traffic line in the scene view (used by editor scripts)
        /// </summary>
        public void DrawLine()
        {
            if (lineDrawer == null)
                lineDrawer = new TrafficLineDrawer();

            if (showLine && pathPoints != null && pathPoints.Count >= 2)
            {
                var linesList = new List<TrafficLineDrawer.TrafficLineProperties> { lineProperties };
                // Convert local points to world space for drawing
                List<Vector3> worldPoints = GetWorldPathPoints();
                lineDrawer.DrawTrafficLines(worldPoints, linesList, pathWidth);
            }
        }
    }
}