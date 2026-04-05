using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameLogic;
using UnityEngine;

namespace MapGeneration
{
    public class MapGenerator : MonoBehaviour
    {
        [Header("Map Settings")] 
        public int mapWidth = 10;
        public int mapHeight = 10;
        public float tileSize = 20f; 
        
        [Header("Track Settings")]
        public int targetTrackLength = 15; 
        
        [Header("Logic References")]
        public GameManager gameManager; 
        
        [Header("Obstacle Management")] 
        public ObstacleManager obstacleManager;

        [Header("WFC Data")] 
        public List<TileData> allAvailableTiles; 
        
        [Header("Scenery Tiles")]
        public List<TileData> sceneryTiles; 
        
        [Header("Special Tiles")] 
        public TileData startTileData; 
        public TileData finishTileData; 
        public List<TileData> checkpointTiles; 

        private Cell[,] grid; 
        private List<TileVariant> standardVariants; 
        private List<TileVariant> startVariants; 
        private List<TileVariant> finishVariants; 
        private List<TileVariant> checkpointVariants; 
        private bool useManualSeed;
        private int manualSeed;

        public int LastUsedSeed { get; private set; }
        public string LastGenerationSignature { get; private set; }
        public List<Vector2Int> GeneratedPath { get; private set; }

        public string GetSeedDisplayValue()
        {
            return $"{targetTrackLength:00}{LastUsedSeed}";
        }

        #region UI Toolkit Integration for Track Length
        public void SetTrackLengthFive() { targetTrackLength = 5; }
        public void SetTrackLengthTen() { targetTrackLength = 10; }
        public void SetTrackLengthFifteen() { targetTrackLength = 15; }

        public void SetCustomTrackLengthFromString(string lengthString)
        {
            if (int.TryParse(lengthString, out int parsedLength))
            {
                targetTrackLength = Mathf.Clamp(parsedLength, 1, 99); 
            }
            else
            {
                Debug.LogWarning("Invalid track length inputted: " + lengthString);
            }
        }
        #endregion

        public void OnPlayClicked()
        {
            if (gameManager == null) gameManager = FindObjectOfType<GameManager>();

            GenerateMapWithCurrentSeed();
            
            if (gameManager != null)
            {
                gameManager.SetupNewTrack();
            }
            else
            {
                Debug.LogError("MapGenerator: Cannot find GameManager to SetupNewTrack!");
            }
        }

        public void SetSeed(string seed)
        {
            if (string.IsNullOrWhiteSpace(seed))
            {
                useManualSeed = false;
                Debug.Log("Manual seed cleared. Map generation will use random seeds.");
                return;
            }

            string trimmedSeed = seed.Trim();
            if (trimmedSeed.Length > 2 && int.TryParse(trimmedSeed.Substring(0, 2), out int length) && int.TryParse(trimmedSeed.Substring(2), out int parsedSeed))
            {
                targetTrackLength = Mathf.Clamp(length, 1, 99); 
                manualSeed = parsedSeed;
                useManualSeed = true;
                
                mapWidth = mapHeight = Mathf.Max(10, (int)(targetTrackLength * 0.7f));
                Debug.Log($"Manual seed set. Track length: {targetTrackLength}, Seed: {manualSeed}. Grid size set to {mapWidth}x{mapHeight}.");
            }
            else
            {
                useManualSeed = false;
                Debug.LogWarning($"Invalid seed format: '{trimmedSeed}'. Using random seed instead.");
            }
        }

        public void ResetSeed()
        {
            useManualSeed = false;
            manualSeed = 0;
            mapWidth = 10;
            mapHeight = 10;
            Debug.Log("Seed and map settings have been reset.");
        }

        private void GenerateMapWithCurrentSeed()
        {
            if (useManualSeed)
            {
                GenerateMapFromSeed(manualSeed);
            }
            else
            {
                mapWidth = mapHeight = Mathf.Max(10, (int)(targetTrackLength * 0.7f));
                int randomPart = Random.Range(0, 1000000); 
                GenerateMapFromSeed(randomPart);
            }
        }

        public bool GenerateMapFromSeed(int seed)
        {
            LastUsedSeed = seed;
            Random.State previousRandomState = Random.state;

            try
            {
                Random.InitState(LastUsedSeed);
                bool success = GenerateValidMap();
                LastGenerationSignature = success ? BuildGenerationSignature() : string.Empty;
                return success;
            }
            finally
            {
                Random.state = previousRandomState;
            }
        }

        private string BuildGenerationSignature()
        {
            StringBuilder sb = new StringBuilder();

            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    Cell cell = grid[x, y];
                    if (cell?.CollapsedVariant == null) continue;

                    string tileName = cell.CollapsedVariant.Data != null ? cell.CollapsedVariant.Data.tileName : "null";
                    sb.Append(x).Append(',').Append(y).Append(',').Append(tileName).Append(',').Append(cell.CollapsedVariant.Rotation).Append('|');
                }
            }

            List<string> placements = new List<string>();
            foreach (Transform child in transform)
            {
                Vector3 position = child.position;
                int rotY = Mathf.RoundToInt(child.rotation.eulerAngles.y) % 360;
                string prefabName = child.name.Replace("(Clone)", string.Empty);
                placements.Add($"{Mathf.RoundToInt(position.x)}:{Mathf.RoundToInt(position.z)}:{rotY}:{prefabName}");
            }

            placements.Sort();
            for (int i = 0; i < placements.Count; i++)
            {
                sb.Append(placements[i]).Append('|');
            }

            return sb.ToString();
        }

        private void InitializeGrid()
        {
            standardVariants = new List<TileVariant>();
            startVariants = new List<TileVariant>();
            finishVariants = new List<TileVariant>();
            checkpointVariants = new List<TileVariant>();

            foreach (var tile in allAvailableTiles)
            {
                for (int r = 0; r < 4; r++)
                {
                    TileVariant variant = new TileVariant(tile, r);
                    
                    if (tile == startTileData) startVariants.Add(variant);
                    else if (tile == finishTileData) finishVariants.Add(variant);
                    else if (checkpointTiles != null && checkpointTiles.Contains(tile)) checkpointVariants.Add(variant);
                    else standardVariants.Add(variant); 
                }
            }

            grid = new Cell[mapWidth, mapHeight];
            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    grid[x, y] = new Cell(new Vector2Int(x, y), standardVariants);
                }
            }
        }
        
        private void RunWFC()
        {
            while (!IsFullyCollapsed())
            {
                Cell nextCell = GetCellWithLowestEntropy();
                if (nextCell == null || nextCell.Entropy == 0) return; 

                CollapseCell(nextCell);
                Propagate(nextCell);
            }
        }
        
        private void ApplyPathToWFC(List<Vector2Int> path)
        {
            for (int i = 0; i < path.Count; i++)
            {
                Vector2Int current = path[i];
                Cell cell = grid[current.x, current.y];

                Vector2Int? prev = i > 0 ? path[i - 1] : (Vector2Int?)null;
                Vector2Int? next = i < path.Count - 1 ? path[i + 1] : (Vector2Int?)null;

                List<TileVariant> validForPath = new List<TileVariant>();

                List<TileVariant> sourceVariants = standardVariants; 
                if (i == 0) sourceVariants = startVariants;
                else if (i == path.Count - 1) sourceVariants = finishVariants;
                else if (i % 5 == 0 && targetTrackLength > 5) sourceVariants = checkpointVariants;

                foreach (var variant in sourceVariants)
                {
                    bool matches = true;
                    Vector2Int[] dirs = { new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(0, -1), new Vector2Int(-1, 0) };

                    for (int d = 0; d < 4; d++)
                    {
                        Vector2Int neighborPos = current + dirs[d];
                        bool isPathConnection = (prev.HasValue && prev.Value == neighborPos) || (next.HasValue && next.Value == neighborPos);
                        bool hasRoadSocket = variant.Sockets[d] == "road";

                        if (isPathConnection && !hasRoadSocket) matches = false;
                        if (!isPathConnection && hasRoadSocket) matches = false;
                    }

                    if (matches) validForPath.Add(variant);
                }

                if (validForPath.Count > 0)
                {
                    cell.AvailableVariants = new List<TileVariant> { validForPath[Random.Range(0, validForPath.Count)] };
                    cell.CollapsedVariant = cell.AvailableVariants[0];
                    cell.IsCollapsed = true;
                    Propagate(cell);
                }
                else
                {
                    Debug.LogError($"Missing prefab for path cell at {current.x}, {current.y}");
                }
            }
        }
        
        private void InstantiatePathAndScenery(List<Vector2Int> path)
        {
            HashSet<Vector2Int> pathSet = new HashSet<Vector2Int>(path);
            HashSet<Vector2Int> scenerySet = new HashSet<Vector2Int>();
            
            foreach (Vector2Int pathPos in path)
            {
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        if (x == 0 && y == 0) continue; 
                        Vector2Int neighborPos = new Vector2Int(pathPos.x + x, pathPos.y + y);
                        
                        if (neighborPos.x >= 0 && neighborPos.x < mapWidth && neighborPos.y >= 0 && neighborPos.y < mapHeight)
                        {
                            if (!pathSet.Contains(neighborPos)) scenerySet.Add(neighborPos);
                        }
                    }
                }
            }

            for (int i = 0; i < path.Count; i++)
            {
                Vector2Int pos = path[i];
                Cell cell = grid[pos.x, pos.y];
        
                if (cell.CollapsedVariant != null)
                {
                    Vector3 worldPos = new Vector3(pos.x * tileSize, 0, pos.y * tileSize);
                    Quaternion rot = Quaternion.Euler(0, cell.CollapsedVariant.Rotation * 90f, 0);
            
                    GameObject spawnedTile = Instantiate(cell.CollapsedVariant.Data.prefab, worldPos, rot, transform);

                    if (i > 0 && i % 5 == 0 && i < path.Count - 1)
                    {
                        Checkpoint cp = spawnedTile.GetComponentInChildren<Checkpoint>();
                        if (cp != null)
                        {
                            Vector2Int gridDir = path[i + 1] - path[i - 1];
                            Vector3 worldDir = new Vector3(gridDir.x, 0, gridDir.y);
                            cp.correctRotation = Quaternion.LookRotation(worldDir);
                        }
                    }
                }
            }

            List<Vector2Int> orderedScenery = scenerySet.OrderBy(p => p.x).ThenBy(p => p.y).ToList();

            foreach (Vector2Int pos in orderedScenery)
            {
                if (sceneryTiles == null || sceneryTiles.Count == 0) continue;
                
                Vector3 worldPos = new Vector3(pos.x * tileSize, 0, pos.y * tileSize);
                TileData sceneryToPlace = sceneryTiles[Random.Range(0, sceneryTiles.Count)];
                float randomYRot = Random.Range(0, 4) * 90f;
                Quaternion rot = Quaternion.Euler(0, randomYRot, 0);

                Instantiate(sceneryToPlace.prefab, worldPos, rot, transform);
            }
        }

        public Cell GetCell(int x, int y)
        {
            if (x >= 0 && x < mapWidth && y >= 0 && y < mapHeight) return grid[x, y];
            return null;
        }

        private bool IsFullyCollapsed()
        {
            foreach (var cell in grid)
            {
                if (!cell.IsCollapsed) return false;
            }
            return true;
        }

        private Cell GetCellWithLowestEntropy()
        {
            Cell bestCell = null;
            int lowestEntropy = int.MaxValue;

            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    Cell cell = grid[x, y];
                    if (!cell.IsCollapsed && cell.Entropy < lowestEntropy)
                    {
                        lowestEntropy = cell.Entropy;
                        bestCell = cell;
                    }
                }
            }
            return bestCell;
        }

        private void CollapseCell(Cell cell)
        {
            int randomIndex = Random.Range(0, cell.AvailableVariants.Count);
            cell.CollapsedVariant = cell.AvailableVariants[randomIndex];
            cell.AvailableVariants.Clear();
            cell.AvailableVariants.Add(cell.CollapsedVariant);
            cell.IsCollapsed = true;
        }
        
        private void Propagate(Cell collapsedCell)
        {
            Stack<Cell> stack = new Stack<Cell>();
            stack.Push(collapsedCell);

            while (stack.Count > 0)
            {
                Cell current = stack.Pop();
                Vector2Int[] directions = { new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(0, -1), new Vector2Int(-1, 0) };

                for (int i = 0; i < 4; i++)
                {
                    Vector2Int neighborPos = current.GridPosition + directions[i];

                    if (neighborPos.x >= 0 && neighborPos.x < mapWidth && neighborPos.y >= 0 && neighborPos.y < mapHeight)
                    {
                        Cell neighbor = grid[neighborPos.x, neighborPos.y];
                        if (neighbor.IsCollapsed) continue;

                        bool changed = ConstrainNeighbor(current, neighbor, i);
                        if (changed) stack.Push(neighbor);
                    }
                }
            }
        }

        private bool ConstrainNeighbor(Cell current, Cell neighbor, int directionIndex)
        {
            bool changed = false;
            int neighborSideIndex = (directionIndex + 2) % 4;
            List<TileVariant> toRemove = new List<TileVariant>();

            foreach (var neighborVariant in neighbor.AvailableVariants)
            {
                bool possible = false;
                foreach (var currentVariant in current.AvailableVariants)
                {
                    if (currentVariant.Sockets[directionIndex] == neighborVariant.Sockets[neighborSideIndex])
                    {
                        possible = true;
                        break;
                    }
                }

                if (!possible)
                {
                    toRemove.Add(neighborVariant);
                    changed = true;
                }
            }

            foreach (var variant in toRemove) neighbor.AvailableVariants.Remove(variant);

            return changed;
        }
        
        private bool GenerateValidMap()
        {
            if (transform.localScale.sqrMagnitude < 0.001f)
            {
                transform.localScale = Vector3.one;
            }

            ClearScene();
            obstacleManager.ClearAllObstacles();
            InitializeGrid(); 
            
            // TADY POUŽIJEME NOVOU TŘÍDU:
            TrackPathfinder pathfinder = new TrackPathfinder(mapWidth, mapHeight);
            GeneratedPath = pathfinder.GeneratePath(new Vector2Int(1, 1), targetTrackLength + 2);

            if (GeneratedPath != null)
            {
                ApplyPathToWFC(GeneratedPath);
                RunWFC(); 
                InstantiatePathAndScenery(GeneratedPath);

                if (gameManager == null) gameManager = FindObjectOfType<GameManager>();
                if (gameManager != null) gameManager.PlaceCarOnStart();
                
                Debug.Log($"Track generated with length {GeneratedPath.Count}. Seed: {LastUsedSeed}");
                return true;
            }

            Debug.LogError($"Failed to generate a valid path. Seed: {LastUsedSeed}");
            return false;
        }
        
        public void ClearScene()
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }
    }
}