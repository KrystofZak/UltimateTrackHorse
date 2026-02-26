using System.Collections.Generic;
using UnityEngine;

namespace AK.MapEditorTools
{
    public class RailingGenerator : MonoBehaviour
    {
        public enum RailingType { None, Wall, Plane }
        public enum RailingSide { Left, Right }

        public RailingType railingType = RailingType.None;
        public RailingSide railingSide = RailingSide.Left;
        public float offset = 1.0f;
        public float wallHeight = 1.0f;
        public float planeHeight = 1.0f;
        public Material material;
        [Tooltip("Controls how many times the texture repeats horizontally along the railing. Lower values = more repetition.")]
        public float uvRepeatFactor = 2.0f;
        public bool addColliders = true;

        // Name for the railing object
        private const string RAILING_NAME = "Railing";

        public void GenerateRailing(List<Vector3> pathPoints)
        {
            if (pathPoints == null || pathPoints.Count < 2) return;

            // Clean up existing railing by name
            CleanupRailingByName(RAILING_NAME);

            if (railingType != RailingType.None)
                GenerateRailingInternal(pathPoints, offset, railingType, railingSide == RailingSide.Left);
        }

        /// <summary>
        /// Clean up all railings - used when resetting or deleting the path
        /// </summary>
        public void CleanupRailings()
        {
            CleanupRailingByName(RAILING_NAME);
        }

        private void CleanupRailingByName(string railingName)
        {
            // Find child by name
            Transform railingTransform = transform.Find(railingName);
            if (railingTransform != null)
            {
                if (Application.isEditor && !Application.isPlaying)
                    DestroyImmediate(railingTransform.gameObject);
                else
                    Destroy(railingTransform.gameObject);
            }
        }

        private GameObject GenerateRailingInternal(List<Vector3> pathPoints, float offset, RailingType type, bool isLeft)
        {
            GameObject railingParent = new GameObject(RAILING_NAME);
            railingParent.transform.SetParent(transform, false);

            // Pre-calculate all offset points to handle corner connections properly
            List<Vector3> offsetPoints = new List<Vector3>();
            List<Vector3> directions = new List<Vector3>();

            // Calculate all offset points and directions first
            for (int i = 0; i < pathPoints.Count; i++)
            {
                Vector3 direction;

                if (i < pathPoints.Count - 1)
                {
                    // Normal segment
                    direction = (pathPoints[i + 1] - pathPoints[i]).normalized;
                }
                else
                {
                    // Last point with no connection (use previous direction)
                    direction = (pathPoints[i] - pathPoints[i - 1]).normalized;
                }

                directions.Add(direction);

                // Calculate perpendicular vector (cross product with up to get perpendicular)
                // For left railing we want -perpendicular, for right railing we want perpendicular
                Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized;
                if (isLeft) perpendicular = -perpendicular;

                // Apply offset in the perpendicular direction
                Vector3 offsetPoint = pathPoints[i] + perpendicular * offset;
                offsetPoints.Add(offsetPoint);
            }

            // Create railing segments
            for (int i = 0; i < pathPoints.Count - 1; i++)
            {
                if (type == RailingType.Wall)
                {
                    CreateWallSegment(offsetPoints[i], offsetPoints[i + 1], railingParent);
                }
                else if (type == RailingType.Plane)
                {
                    CreatePlaneSegment(offsetPoints[i], offsetPoints[i + 1], railingParent, isLeft);
                }
            }

            return railingParent;
        }

        private void CreateWallSegment(Vector3 start, Vector3 end, GameObject parent)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.SetParent(parent.transform, false);

            // Position the wall's bottom at the road level instead of centered
            Vector3 midpoint = (start + end) / 2;
            // Raise position by half the wall height so bottom is at road level
            wall.transform.position = new Vector3(midpoint.x, midpoint.y + wallHeight / 2, midpoint.z);

            wall.transform.rotation = Quaternion.LookRotation(end - start);
            wall.transform.localScale = new Vector3(0.1f, wallHeight, Vector3.Distance(start, end));

            // Use the assigned material
            if (material != null)
            {
                wall.GetComponent<Renderer>().material = material;
            }

            if (!addColliders)
            {
                Destroy(wall.GetComponent<Collider>());
            }
        }

        private void CreatePlaneSegment(Vector3 start, Vector3 end, GameObject parent, bool isLeft)
        {
            GameObject plane = new GameObject("RailingPlane");
            plane.transform.SetParent(parent.transform, false);

            MeshFilter meshFilter = plane.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = plane.AddComponent<MeshRenderer>();

            // Use the assigned material
            if (material != null)
            {
                meshRenderer.material = material;
            }

            // Create the mesh
            Mesh mesh = new Mesh();

            // Position the plane between the start and end points
            Vector3 midpoint = (start + end) / 2;
            plane.transform.position = midpoint;

            // Calculate the direction and apply rotation
            Vector3 direction = (end - start).normalized;
            plane.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

            // Convert points to local space
            Vector3 localStart = plane.transform.InverseTransformPoint(start);
            Vector3 localEnd = plane.transform.InverseTransformPoint(end);

            // Create mesh in local space
            mesh.vertices = new Vector3[]
            {
                localStart,
                localEnd,
                localStart + Vector3.up * planeHeight,
                localEnd + Vector3.up * planeHeight
            };

            // Set triangles with correct winding order based on side
            if (railingSide == RailingSide.Right)
            {
                mesh.triangles = new int[]
                {
                    0, 2, 1,
                    1, 2, 3
                };
            }
            else
            {
                mesh.triangles = new int[]
                {
                    1, 2, 0,
                    3, 2, 1
                };
            }

            // Calculate segment length for UV tiling
            float segmentLength = Vector3.Distance(start, end);
            float uvRepeat = segmentLength / uvRepeatFactor;

            // Set UVs
            if (railingSide == RailingSide.Right)
            {
                mesh.uv = new Vector2[]
                {
                    new Vector2(uvRepeat, 0),
                    new Vector2(0, 0),
                    new Vector2(uvRepeat, 1),
                    new Vector2(0, 1)
                };
            }
            else
            {
                mesh.uv = new Vector2[]
                {
                    new Vector2(0, 0),
                    new Vector2(uvRepeat, 0),
                    new Vector2(0, 1),
                    new Vector2(uvRepeat, 1)
                };
            }

            mesh.RecalculateNormals();
            meshFilter.mesh = mesh;

            if (addColliders)
            {
                // Add a MeshCollider that uses the same mesh as the visual component
                MeshCollider meshCollider = plane.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = mesh;
            }
        }

        private void Regenerate()
        {
            // Find the RoadGenerator to get the path points
            RoadGenerator roadGen = GetComponent<RoadGenerator>();
            if (roadGen != null)
            {
                var pathPoints = roadGen.GetPathPoints();

                // Only regenerate if we have valid path points
                if (pathPoints != null && pathPoints.Count >= 2)
                {
                    GenerateRailing(pathPoints);
                }
            }
        }

        // Unity Editor only: Call Regenerate when the script is validated in the editor
#if UNITY_EDITOR
        private void OnValidate()
        {
            // Use delayed call to avoid issues with multiple OnValidate calls
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null) return; // Object might have been destroyed

                Regenerate();
            };
        }
#endif

        private void OnEnable()
        {
            // Regenerate the railings when the component is enabled
            Regenerate();
        }
    }
}