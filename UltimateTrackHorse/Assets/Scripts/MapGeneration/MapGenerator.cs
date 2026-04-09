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

        /// <summary>
        /// Sets the track length to a custom value entered by the user.
        /// </summary>
        /// <param name="lengthString">The string input from the UI, expected to be an integer between 1 and 99.</param>
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

        /// <summary>
        /// Generates a new map with the current settings.
        /// </summary>
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

        /// <summary>
        /// Sets the seed for the map generation.
        /// </summary>
        /// <param name="seed">The seed string</param>
        public void SetSeed(string seed)
        {
            if (string.IsNullOrWhiteSpace(seed))
            {
                useManualSeed = false;
                Debug.Log("Manual seed cleared. Map generation will use random seeds.");
                return;
            }

            // Parse the seed string to extract the track length and seed value
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

        /// <summary>
        /// Resets the seed and map settings to their default values.
        /// </summary>
        public void ResetSeed()
        {
            useManualSeed = false;
            manualSeed = 0;
            mapWidth = 10;
            mapHeight = 10;
            Debug.Log("Seed and map settings have been reset.");
        }

        /// <summary>
        /// Generates a new map with the current settings.
        /// If a manual seed is set, it will use that seed; otherwise,
        /// it will generate a random seed and adjust the map size based on the target track length.
        /// </summary>
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

        /// <summary>
        /// Generates a new map with the specified seed.
        /// </summary>
        /// <param name="seed"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Generates a unique signature based on the current map state.
        /// </summary>
        /// <returns>A string representing the unique signature of the generated map.</returns>
        private string BuildGenerationSignature()
        {
            StringBuilder sb = new StringBuilder();

            // Add the map width and height to the signature
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

            // Add the track length to the signature
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

        /// <summary>
        /// Initializes the WFC grid and categorizes tile variants
        /// based on their types (start, finish, checkpoint, standard).
        /// </summary>
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
        
        /// <summary>
        /// Runs the Wave Function Collapse algorithm to collapse the grid
        /// based on the constraints defined by the tile variants.
        /// </summary>
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
        
        /// <summary>
        /// Applies the path to the WFC grid, setting the available variants
        /// for each cell along the path to ensure that the path is preserved in the final collapsed state.
        /// </summary>
        /// <param name="path"></param>
        private void ApplyPathToWFC(List<Vector2Int> path)
        {
            for (int i = 0; i < path.Count; i++)
            {
                Vector2Int current = path[i];
                Cell cell = grid[current.x, current.y];

                Vector2Int? prev = i > 0 ? path[i - 1] : (Vector2Int?)null;
                Vector2Int? next = i < path.Count - 1 ? path[i + 1] : (Vector2Int?)null;

                List<TileVariant> validForPath = new List<TileVariant>();

                // Determine the source variants based on the path position
                List<TileVariant> sourceVariants = standardVariants; 
                if (i == 0) sourceVariants = startVariants;
                else if (i == path.Count - 1) sourceVariants = finishVariants;
                else if (i % 5 == 0 && targetTrackLength > 5) sourceVariants = checkpointVariants;

                // Filter the variants based on the path connection and road socket status
                foreach (var variant in sourceVariants)
                {
                    bool matches = true;
                    Vector2Int[] dirs = { new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(0, -1), new Vector2Int(-1, 0) };

                    // Check if the variant matches the path connection and road socket status
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

                // Set the available variants for the cell based on the path variants
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
        
        /// <summary>
        /// Instantiates the path and scenery tiles on the map,
        /// ensuring that the path is placed correctly and that
        /// scenery tiles are placed around it without blocking the path.
        /// </summary>
        /// <param name="path"></param>
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

            // Instantiate the path tiles
            for (int i = 0; i < path.Count; i++)
            {
                Vector2Int pos = path[i];
                Cell cell = grid[pos.x, pos.y];
        
                if (cell.CollapsedVariant != null)
                {
                    Vector3 worldPos = new Vector3(pos.x * tileSize, 0, pos.y * tileSize);
                    Quaternion rot = Quaternion.Euler(0, cell.CollapsedVariant.Rotation * 90f, 0);
            
                    GameObject spawnedTile = Instantiate(cell.CollapsedVariant.Data.prefab, worldPos, rot, transform);

                    // Check if the tile is a checkpoint and set the correct rotation
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

            // Instantiate the scenery tiles
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

        /// <summary>
        /// Gets the cell at the specified grid coordinates.
        /// </summary>
        /// <param name="x">X coordinate of the cell</param>
        /// <param name="y">Y coordinate of the cell</param>
        /// <returns>The Cell object at the specified coordinates, or null if out of bounds.</returns>
        public Cell GetCell(int x, int y)
        {
            if (x >= 0 && x < mapWidth && y >= 0 && y < mapHeight) return grid[x, y];
            return null;
        }

        /// <summary>
        /// Gets the cell at the specified grid coordinates.
        /// </summary>
        /// <returns>True if the map is fully collapsed, false otherwise.</returns>
        private bool IsFullyCollapsed()
        {
            foreach (var cell in grid)
            {
                if (!cell.IsCollapsed) return false;
            }
            return true;
        }

        /// <summary>
        /// Gets the cell with the lowest entropy.
        /// </summary>
        /// <returns>The Cell object with the lowest entropy, or null if all cells are collapsed.</returns>
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

        /// <summary>
        /// Collapses the specified cell by randomly selecting one of its available variants
        /// </summary>
        /// <param name="cell">The Cell object to collapse</param>
        private void CollapseCell(Cell cell)
        {
            int randomIndex = Random.Range(0, cell.AvailableVariants.Count);
            cell.CollapsedVariant = cell.AvailableVariants[randomIndex];
            cell.AvailableVariants.Clear();
            cell.AvailableVariants.Add(cell.CollapsedVariant);
            cell.IsCollapsed = true;
        }
        
        /// <summary>
        /// Propagates the changes in the collapsed state of neighboring cells
        /// based on the constraints defined by the tile variants.
        /// </summary>
        /// <param name="collapsedCell">The Cell object that was just collapsed, which will be the starting point for propagation</param>
        private void Propagate(Cell collapsedCell)
        {
            Stack<Cell> stack = new Stack<Cell>();
            stack.Push(collapsedCell);

            // Propagate changes to neighboring cells
            while (stack.Count > 0)
            {
                Cell current = stack.Pop();
                Vector2Int[] directions = { new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(0, -1), new Vector2Int(-1, 0) };

                // Check if the current cell is a valid neighbor and apply constraints
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

        /// <summary>
        /// Applies the constraints between the current cell and its neighbor based on their available variants.
        /// </summary>
        /// <param name="current">The Cell object that has just been collapsed, which will be used to constrain the neighboring cell</param>
        /// <param name="neighbor">The neighboring Cell object that will be constrained based on the collapsed state of the current cell</param>
        /// <param name="directionIndex">The index representing the direction from the current cell to the neighbor</param>
        /// <returns></returns>
        private bool ConstrainNeighbor(Cell current, Cell neighbor, int directionIndex)
        {
            bool changed = false;
            int neighborSideIndex = (directionIndex + 2) % 4;
            List<TileVariant> toRemove = new List<TileVariant>();

            // Check if the current cell is a start cell
            foreach (var neighborVariant in neighbor.AvailableVariants)
            {
                bool possible = false;
                
                // Check if the neighbor variant is a start variant
                foreach (var currentVariant in current.AvailableVariants)
                {
                    if (currentVariant.Sockets[directionIndex] == neighborVariant.Sockets[neighborSideIndex])
                    {
                        possible = true;
                        break;
                    }
                }

                // If the neighbor variant is a start variant, remove it from the neighbor's available variants'
                if (!possible)
                {
                    toRemove.Add(neighborVariant);
                    changed = true;
                }
            }

            // Remove the removed variants from the neighbor's available variants'
            foreach (var variant in toRemove) neighbor.AvailableVariants.Remove(variant);

            return changed;
        }
        
        /// <summary>
        /// Generates a valid map by first clearing the scene and obstacles, initializing the WFC grid,
        /// generating a path using the TrackPathfinder, applying the path to the WFC grid,
        /// running the WFC algorithm, and finally instantiating the path and scenery tiles on the map
        /// </summary>
        /// <returns>True if the map was successfully generated, false otherwise.</returns>
        private bool GenerateValidMap()
        {
            if (transform.localScale.sqrMagnitude < 0.001f)
            {
                transform.localScale = Vector3.one;
            }

            // Clear the scene and obstacles
            ClearScene();
            obstacleManager.ClearAllObstacles();
            InitializeGrid(); 
            
            // Generate a path with desired length + 2 (start and finish)
            TrackPathfinder pathfinder = new TrackPathfinder(mapWidth, mapHeight);
            GeneratedPath = pathfinder.GeneratePath(new Vector2Int(1, 1), targetTrackLength + 2);

            // Apply the path to the WFC grid and run the algorithm
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
        
        /// <summary>
        /// Clears the scene by destroying all game objects in the scene.
        /// </summary>
        public void ClearScene()
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }
    }
}