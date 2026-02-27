using System.Collections.Generic;
using UnityEngine;

namespace AK.MapEditorTools
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    [ExecuteInEditMode]
    public class RoadGenerator : MonoBehaviour
    {
        public RoadEditor roadEditor;
        public float roadWidth = 4.0f;
        public Material roadMaterial;
        public Material terrainMaterial;
        [Tooltip("Width of terrain on each side")] public float terrainSize = 2f;
        [Tooltip("Vertical offset for terrain")] public float terrainHeightOffset = -0.8f;
        [Tooltip("Custom horizontal UV tiling for terrain")] public float terrainUvHorizontalTile = 1f;
        public float uvTilingDensity = 0.5f;
        public float uvTilingWidth = 1.0f;
        public bool flipNormals = false;
        public float heightOffset = 0f;

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Mesh roadMesh;
        private GameObject leftTerrainObj, rightTerrainObj;
        private Mesh leftTerrainMesh, rightTerrainMesh;
        private Vector3[] lastGeneratedPoints;
        private bool isClosedPath = false;

        private void OnEnable()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();

            if (meshFilter.sharedMesh == null)
            {
                roadMesh = new Mesh { name = "Road Mesh" };
                meshFilter.sharedMesh = roadMesh;
            }
            else
                roadMesh = meshFilter.sharedMesh;

            if (roadMaterial != null)
                meshRenderer.material = roadMaterial;

            EnsureTerrainObjectsExist();

            // Try to find mapEditor in parent if not assigned
            if (roadEditor == null)
            {
                roadEditor = GetComponentInParent<RoadEditor>();
            }

            if (roadEditor != null)
                RegenerateMesh();
        }

        private void EnsureTerrainObjectsExist()
        {
            if (leftTerrainObj == null)
            {
                leftTerrainObj = FindOrCreateTerrainObject("Left Terrain");
                leftTerrainMesh = new Mesh { name = "Left Terrain Mesh" };
                leftTerrainObj.GetComponent<MeshFilter>().sharedMesh = leftTerrainMesh;
            }

            if (rightTerrainObj == null)
            {
                rightTerrainObj = FindOrCreateTerrainObject("Right Terrain");
                rightTerrainMesh = new Mesh { name = "Right Terrain Mesh" };
                rightTerrainObj.GetComponent<MeshFilter>().sharedMesh = rightTerrainMesh;
            }

            if (terrainMaterial != null)
            {
                if (leftTerrainObj != null)
                    leftTerrainObj.GetComponent<MeshRenderer>().sharedMaterial = terrainMaterial;
                if (rightTerrainObj != null)
                    rightTerrainObj.GetComponent<MeshRenderer>().sharedMaterial = terrainMaterial;
            }
        }

        private GameObject FindOrCreateTerrainObject(string name)
        {
            Transform child = transform.Find(name);
            GameObject terrainObject;

            if (child != null)
                terrainObject = child.gameObject;
            else
            {
                terrainObject = new GameObject(name);
                terrainObject.transform.SetParent(transform, false);
                terrainObject.transform.localPosition = Vector3.zero;
                terrainObject.transform.localRotation = Quaternion.identity;
                terrainObject.AddComponent<MeshFilter>();
                terrainObject.AddComponent<MeshRenderer>();
            }

            return terrainObject;
        }

        private void Update()
        {
            // Only update if we have a valid mapEditor reference
            if (roadEditor != null && ShouldRegenerateMesh())
                RegenerateMesh();
        }

        public void SetClosedPath(bool isClosed)
        {
            if (isClosedPath != isClosed)
            {
                isClosedPath = isClosed;
                RegenerateMesh();
            }
        }

        private bool ShouldRegenerateMesh()
        {
            List<Vector3> currentPoints = roadEditor.GetBezierCurvePoints();
            if (lastGeneratedPoints == null) return true;
            if (lastGeneratedPoints.Length != currentPoints.Count) return true;

            for (int i = 0; i < currentPoints.Count; i++)
                if (lastGeneratedPoints[i] != currentPoints[i])
                    return true;
            return false;
        }

        public void RegenerateMesh()
        {
            if (roadEditor == null || roadMesh == null) return;
            if (roadMaterial != null) meshRenderer.material = roadMaterial;
            EnsureTerrainObjectsExist();

            List<Vector3> pathPoints = roadEditor.GetBezierCurvePoints();
            if (pathPoints.Count < 2)
            {
                if (roadMesh.vertexCount > 0) roadMesh.Clear();
                if (leftTerrainMesh != null) leftTerrainMesh.Clear();
                if (rightTerrainMesh != null) rightTerrainMesh.Clear();
                return;
            }

            lastGeneratedPoints = pathPoints.ToArray();
            GenerateRoadMesh(pathPoints);
            GenerateTerrainMeshes(pathPoints);

            // Update the collider after regenerating the mesh
            GenerateCollider();
        }

        private void GenerateRoadMesh(List<Vector3> pathPoints)
        {
            int pointCount = pathPoints.Count;
            if (pointCount < 2) return;

            roadMesh.Clear();

            int segmentCount = pointCount - 1;
            Vector3[] vertices = new Vector3[pointCount * 3]; // Changed from 2 to 3 vertices per point
            Vector2[] uvs = new Vector2[pointCount * 3];     // Changed from 2 to 3 UVs per point
            int[] triangles = new int[segmentCount * 12];    // Changed from 6 to 12 triangle indices per segment

            float[] accumulatedDistances = new float[pointCount];
            accumulatedDistances[0] = 0f;
            float totalPathLength = 0f;

            for (int i = 1; i < pointCount; i++)
            {
                totalPathLength += Vector3.Distance(pathPoints[i], pathPoints[i - 1]);
                accumulatedDistances[i] = totalPathLength;
            }

            for (int i = 0; i < pointCount; i++)
            {
                Vector3 forward;
                if (i < pointCount - 1)
                    forward = (pathPoints[i + 1] - pathPoints[i]).normalized;
                else
                    forward = (pathPoints[i] - pathPoints[i - 1]).normalized;

                Vector3 right = Vector3.Cross(forward, Vector3.up).normalized;
                Vector3 pos = pathPoints[i];
                pos.y += heightOffset;

                vertices[i * 3] = pos - right * (roadWidth * 0.5f);     // Left vertex
                vertices[i * 3 + 1] = pos;                              // Middle vertex
                vertices[i * 3 + 2] = pos + right * (roadWidth * 0.5f); // Right vertex

                float uvY = accumulatedDistances[i] * uvTilingDensity;
                uvs[i * 3] = new Vector2(uvTilingWidth, uvY);   // Left UV
                uvs[i * 3 + 1] = new Vector2(uvTilingWidth / 2, uvY); // Middle UV
                uvs[i * 3 + 2] = new Vector2(0, uvY);           // Right UV
            }

            for (int i = 0; i < segmentCount; i++)
            {
                int nextI = (i + 1) % pointCount;
                int baseIndex = i * 12; // Changed from 6 to 12

                // Define the 6 vertex indices for the current segment
                int v1 = i * 3;       // Current segment's left vertex
                int v2 = i * 3 + 1;   // Current segment's middle vertex
                int v3 = i * 3 + 2;   // Current segment's right vertex
                int v4 = nextI * 3;   // Next segment's left vertex
                int v5 = nextI * 3 + 1; // Next segment's middle vertex
                int v6 = nextI * 3 + 2; // Next segment's right vertex

                if (!flipNormals)
                {
                    // Left side triangles (left-middle quadrant)
                    triangles[baseIndex] = v1;
                    triangles[baseIndex + 1] = v2;
                    triangles[baseIndex + 2] = v4;
                    triangles[baseIndex + 3] = v2;
                    triangles[baseIndex + 4] = v5;
                    triangles[baseIndex + 5] = v4;

                    // Right side triangles (middle-right quadrant)
                    triangles[baseIndex + 6] = v2;
                    triangles[baseIndex + 7] = v3;
                    triangles[baseIndex + 8] = v5;
                    triangles[baseIndex + 9] = v3;
                    triangles[baseIndex + 10] = v6;
                    triangles[baseIndex + 11] = v5;
                }
                else
                {
                    // Left side triangles (left-middle quadrant)
                    triangles[baseIndex] = v1;
                    triangles[baseIndex + 1] = v4;
                    triangles[baseIndex + 2] = v2;
                    triangles[baseIndex + 3] = v2;
                    triangles[baseIndex + 4] = v4;
                    triangles[baseIndex + 5] = v5;

                    // Right side triangles (middle-right quadrant)
                    triangles[baseIndex + 6] = v2;
                    triangles[baseIndex + 7] = v5;
                    triangles[baseIndex + 8] = v3;
                    triangles[baseIndex + 9] = v3;
                    triangles[baseIndex + 10] = v5;
                    triangles[baseIndex + 11] = v6;
                }
            }

            roadMesh.vertices = vertices;
            roadMesh.uv = uvs;
            roadMesh.triangles = triangles;
            roadMesh.RecalculateNormals();
            roadMesh.RecalculateBounds();
        }

        private void GenerateTerrainMeshes(List<Vector3> pathPoints)
        {
            int pointCount = pathPoints.Count;
            if (pointCount < 2) return;

            leftTerrainMesh.Clear();
            rightTerrainMesh.Clear();

            int segmentCount = pointCount - 1;
            Vector3[] rightVertices = new Vector3[pointCount * 2];
            Vector3[] leftVertices = new Vector3[pointCount * 2];
            Vector2[] rightUvs = new Vector2[pointCount * 2];
            Vector2[] leftUvs = new Vector2[pointCount * 2];
            int[] rightTriangles = new int[segmentCount * 6];
            int[] leftTriangles = new int[segmentCount * 6];

            float[] accumulatedDistances = new float[pointCount];
            accumulatedDistances[0] = 0f;
            float totalPathLength = 0f;

            for (int i = 1; i < pointCount; i++)
            {
                totalPathLength += Vector3.Distance(pathPoints[i], pathPoints[i - 1]);
                accumulatedDistances[i] = totalPathLength;
            }

            for (int i = 0; i < pointCount; i++)
            {
                Vector3 forward;
                if (i < pointCount - 1)
                    forward = (pathPoints[i + 1] - pathPoints[i]).normalized;
                else
                    forward = (pathPoints[i] - pathPoints[i - 1]).normalized;

                Vector3 right = Vector3.Cross(forward, Vector3.up).normalized;
                Vector3 pos = pathPoints[i];
                pos.y += heightOffset;

                Vector3 leftOuterPos = pos - right * (roadWidth * 0.5f) - right * terrainSize;
                leftOuterPos.y += terrainHeightOffset;
                rightVertices[i * 2] = leftOuterPos;
                rightVertices[i * 2 + 1] = pos - right * (roadWidth * 0.5f);

                Vector3 rightOuterPos = pos + right * (roadWidth * 0.5f) + right * terrainSize;
                rightOuterPos.y += terrainHeightOffset;
                leftVertices[i * 2] = pos + right * (roadWidth * 0.5f);
                leftVertices[i * 2 + 1] = rightOuterPos;

                // Calculate the UV coordinates for the lengthwise tiling
                float uvY = accumulatedDistances[i] * uvTilingDensity;

                // Set UVs so the texture is not stretched regardless of terrain size
                rightUvs[i * 2] = new Vector2(terrainUvHorizontalTile, uvY); // Flipped horizontally, using custom tile
                rightUvs[i * 2 + 1] = new Vector2(0, uvY); // Flipped horizontally, using custom tile

                leftUvs[i * 2] = new Vector2(0, uvY);
                leftUvs[i * 2 + 1] = new Vector2(terrainUvHorizontalTile, uvY);
            }

            for (int i = 0; i < segmentCount; i++)
            {
                int nextI = (i + 1) % pointCount;
                int baseIndex = i * 6;
                int v1 = i * 2, v2 = i * 2 + 1, v3 = nextI * 2, v4 = nextI * 2 + 1;

                // Fix the winding order for left terrain triangles to correctly face upward
                rightTriangles[baseIndex] = v1;
                rightTriangles[baseIndex + 1] = v2;
                rightTriangles[baseIndex + 2] = v3;
                rightTriangles[baseIndex + 3] = v2;
                rightTriangles[baseIndex + 4] = v4;
                rightTriangles[baseIndex + 5] = v3;

                leftTriangles[baseIndex] = v1;
                leftTriangles[baseIndex + 1] = v2;
                leftTriangles[baseIndex + 2] = v3;
                leftTriangles[baseIndex + 3] = v2;
                leftTriangles[baseIndex + 4] = v4;
                leftTriangles[baseIndex + 5] = v3;
            }

            leftTerrainMesh.vertices = leftVertices;
            leftTerrainMesh.uv = leftUvs;
            leftTerrainMesh.triangles = leftTriangles;
            leftTerrainMesh.RecalculateNormals();
            leftTerrainMesh.RecalculateBounds();

            rightTerrainMesh.vertices = rightVertices;
            rightTerrainMesh.uv = rightUvs;
            rightTerrainMesh.triangles = rightTriangles;
            rightTerrainMesh.RecalculateNormals();
            rightTerrainMesh.RecalculateBounds();
        }

        public void GenerateCollider()
        {
            // Get or add a MeshCollider component
            MeshCollider meshCollider = GetComponent<MeshCollider>();
            if (meshCollider == null)
            {
                meshCollider = gameObject.AddComponent<MeshCollider>();
            }

            // Assign the current road mesh to the collider
            if (roadMesh != null)
            {
                meshCollider.sharedMesh = roadMesh;
            }
        }

        private void OnDisable()
        {
            void DestroyMeshSafely(ref Mesh mesh)
            {
                if (mesh != null)
                {
                    if (Application.isEditor && !Application.isPlaying)
                        DestroyImmediate(mesh);
                    else
                        Destroy(mesh);
                    mesh = null;
                }
            }

            void DestroyObjSafely(ref GameObject obj)
            {
                if (obj != null && Application.isEditor && !Application.isPlaying)
                {
                    DestroyImmediate(obj);
                    obj = null;
                }
            }

            DestroyMeshSafely(ref roadMesh);
            DestroyMeshSafely(ref leftTerrainMesh);
            DestroyMeshSafely(ref rightTerrainMesh);
            DestroyObjSafely(ref leftTerrainObj);
            DestroyObjSafely(ref rightTerrainObj);
        }

        public List<Vector3> GetPathPoints()
        {
            return roadEditor != null ? roadEditor.GetBezierCurvePoints() : new List<Vector3>();
        }

        public bool IsClosedPath()
        {
            return isClosedPath;
        }
    }
}