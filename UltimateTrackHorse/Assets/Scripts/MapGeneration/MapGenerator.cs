using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameLogic;
using UnityEngine;

namespace MapGeneration
{
    /// <summary>
    /// Class that generates a map using the Wave Function Collapse algorithm
    /// with a guaranteed valid path from start to finish using a simple DFS-based path generation.
    /// </summary>
    public class MapGenerator : MonoBehaviour
    {
        [Header("Map Settings")] 
        public int mapWidth = 10;
        public int mapHeight = 10;
        public float tileSize = 20f; 
        
        [Header("Track Settings")]
        public int targetTrackLength = 15; // Number of tiles in the track includes start and finish
        
        [Header("Logic References")]
        public GameManager gameManager; // Reference to the GameManager script

        [Header("WFC Data")] 
        public List<TileData> allAvailableTiles; // List of all available tiles
        
        // Scenery tiles for areas around the track
        [Header("Scenery Tiles")]
        public List<TileData> sceneryTiles; // List of all scenery tiles
        
        // Start and finish tiles
        [Header("Special Tiles")]
        public TileData startTileData;
        public TileData finishTileData;

        private Cell[,] grid; // 2D array representing the map
        private List<TileVariant> standardVariants; // List of all possible tile variants
        private List<TileVariant> startVariants; // List of possible start tile variants
        private List<TileVariant> finishVariants; // List of possible finish tile variants
        private bool useManualSeed;
        private int manualSeed;

        public int LastUsedSeed { get; private set; }
        public string LastGenerationSignature { get; private set; }

        /// <summary>
        /// Initializes the map generator and generates a valid map with start and finish cells
        /// </summary>
        private void Start()
        {
            targetTrackLength += 2; // Account for start and finish tiles
        }

        #region UI Toggle Methods for Track Length
        
        /// <summary>
        /// Sets a short track length. Connect this to the OnValueChanged event of a "Short Track" UI Toggle.
        /// </summary>
        public void SetTrackLengthFive()
        {
            targetTrackLength = 5;
        }

        /// <summary>
        /// Sets a medium track length. Connect this to the OnValueChanged event of a "Medium Track" UI Toggle.
        /// </summary>
        public void SetTrackLengthTen()
        {
            targetTrackLength = 10;
        }

        /// <summary>
        /// Sets a long track length. Connect this to the OnValueChanged event of a "Long Track" UI Toggle.
        /// </summary>
        public void SetTrackLengthFifteen()
        {
            targetTrackLength = 15;
        }

        /// <summary>
        /// Sets a custom track length from a string input. Connect this to the OnEndEdit event of an InputField.
        /// </summary>
        public void SetCustomTrackLengthFromString(string lengthString)
        {
            if (int.TryParse(lengthString, out int parsedLength))
            {
                // Optionally clamp the value to prevent too small or too large maps
                targetTrackLength = Mathf.Clamp(parsedLength, 3, 100); 
            }
            else
            {
                Debug.LogWarning("Invalid track length inputted: " + lengthString);
            }
        }
        
        #endregion

        /// <summary>
        /// Starts the game. Connect this to PlayButton in Main Menu.
        /// </summary>
        public void OnPlayClicked()
        {
            GenerateMapWithCurrentSeed();
        }

        /// <summary>
        /// Overrides random seed generation with a specific seed string.
        /// Empty string disables override and switches back to random seeds.
        /// </summary>
        public void SetSeed(string seed)
        {
            if (string.IsNullOrWhiteSpace(seed))
            {
                useManualSeed = false;
                Debug.Log("Manual seed cleared. Map generation will use random seeds.");
                return;
            }

            string trimmedSeed = seed.Trim();
            if (int.TryParse(trimmedSeed, out int parsedSeed))
            {
                manualSeed = parsedSeed;
            }
            else
            {
                // Stable hash ensures same text input always maps to the same seed.
                manualSeed = ComputeStableSeedFromString(trimmedSeed);
            }

            useManualSeed = true;
            Debug.Log($"Manual seed set to {manualSeed} (input: '{trimmedSeed}').");
        }

        private void GenerateMapWithCurrentSeed()
        {
            int seed = useManualSeed ? manualSeed : unchecked((int)System.DateTime.UtcNow.Ticks);
            GenerateMapFromSeed(seed);
        }

        private int ComputeStableSeedFromString(string seedText)
        {
            unchecked
            {
                // FNV-1a 32-bit hash for deterministic string-to-seed conversion.
                uint hash = 2166136261;
                for (int i = 0; i < seedText.Length; i++)
                {
                    hash ^= seedText[i];
                    hash *= 16777619;
                }

                return (int)hash;
            }
        }

        /// <summary>
        /// Generates a map deterministically from the provided seed.
        /// </summary>
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
                    if (cell?.CollapsedVariant == null)
                    {
                        continue;
                    }

                    string tileName = cell.CollapsedVariant.Data != null ? cell.CollapsedVariant.Data.tileName : "null";
                    sb.Append(x)
                        .Append(',')
                        .Append(y)
                        .Append(',')
                        .Append(tileName)
                        .Append(',')
                        .Append(cell.CollapsedVariant.Rotation)
                        .Append('|');
                }
            }

            // Include instantiated result so scenery differences are caught too.
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
        /// Initializes the grid with empty cells and splits each tile into its 4 possible rotations
        /// </summary>
        private void InitializeGrid()
        {
            standardVariants = new List<TileVariant>();
            startVariants = new List<TileVariant>();
            finishVariants = new List<TileVariant>();

            // Split each tile into its 4 possible rotations and categorize them
            foreach (var tile in allAvailableTiles)
            {
                for (int r = 0; r < 4; r++)
                {
                    TileVariant variant = new TileVariant(tile, r);

                    if (tile == startTileData)
                    {
                        startVariants.Add(variant);
                    }
                    else if (tile == finishTileData)
                    {
                        finishVariants.Add(variant);
                    }
                    else
                    {
                        // All other tiles are standard
                        standardVariants.Add(variant); 
                    }
                }
            }

            // Fill the grid with empty cells
            grid = new Cell[mapWidth, mapHeight];
            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    grid[x, y] = new Cell(new Vector2Int(x, y), standardVariants);
                }
            }
        }
        
        // Wave Function Collapse Algorithm
        private void RunWFC()
        {
            while (!IsFullyCollapsed())
            {
                Cell nextCell = GetCellWithLowestEntropy();
                
                if (nextCell == null || nextCell.Entropy == 0)
                {
                    return; 
                }

                CollapseCell(nextCell);
                Propagate(nextCell);
            }
        }
        
        /// <summary>
        /// Generates a random contiguous path of a specific length using DFS.
        /// (This will be replaced by the ACO algorithm in the future).
        /// </summary>
        private List<Vector2Int> GenerateRandomPath(Vector2Int startPos, int length)
        {
            List<Vector2Int> currentPath = new List<Vector2Int>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
            
            if (DFSPath(startPos, length, currentPath, visited))
            {
                return currentPath;
            }
            return null; 
        }

        /// <summary>
        /// Depth-first search algorithm to generate a random path of a specific length.
        /// </summary>
        /// <param name="current">Current position in the grid</param>
        /// <param name="targetLength">Length of the path to generate</param>
        /// <param name="path">Current path being built</param>
        /// <param name="visited">Set of visited positions to avoid cycles</param>
        /// <returns></returns>
        private bool DFSPath(Vector2Int current, int targetLength, List<Vector2Int> path, HashSet<Vector2Int> visited)
        {
            path.Add(current);
            visited.Add(current);

            if (path.Count == targetLength) return true;

            Vector2Int[] dirs =
            { 
                new Vector2Int(0, 1),
                new Vector2Int(1, 0),
                new Vector2Int(0, -1),
                new Vector2Int(-1, 0)
            };
            
            // Shuffle the directions to randomize the path
            for (int i = 0; i < dirs.Length; i++)
            {
                int rnd = Random.Range(0, dirs.Length);
                (dirs[i], dirs[rnd]) = (dirs[rnd], dirs[i]);
            }

            foreach (var dir in dirs)
            {
                Vector2Int next = current + dir;
                // One tile border around the map for start and finish
                if (next.x >= 1 && next.x < mapWidth - 1 && next.y >= 1 && next.y < mapHeight - 1)
                {
                    if (!visited.Contains(next))
                    {
                        // Check if the next position creates a shortcut (adjacent to older path segments, not just neighbors)
                        if (!CreatesShortcut(next, current, path))
                        {
                            if (DFSPath(next, targetLength, path, visited))
                                return true;
                        }
                    }
                }
            }

            // Backtracking - remove the current position from the path
            path.RemoveAt(path.Count - 1);
            visited.Remove(current);
            return false;
        }

        /// <summary>
        /// Checks if adding a new position would create a shortcut through the path.
        /// A shortcut is when the new position touches a non-adjacent segment of the path.
        /// Adjacent segments (neighbors in sequence) are allowed - these create turns in the path.
        /// </summary>
        private bool CreatesShortcut(Vector2Int newPos, Vector2Int currentPos, List<Vector2Int> path)
        {
            // Check all 8 neighbors
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0) continue;

                    Vector2Int neighbor = new Vector2Int(newPos.x + x, newPos.y + y);
                    
                    // Find the index of this neighbor in the path
                    int neighborIndex = path.IndexOf(neighbor);
                    if (neighborIndex == -1) continue; // Not in path
                    
                    // Find the index of current position
                    int currentIndex = path.IndexOf(currentPos);
                    
                    // Allow if it's a direct neighbor in sequence (e.g., can touch prev segment)
                    // But disallow if it skips segments (creates a shortcut)
                    int distance = Mathf.Abs(neighborIndex - currentIndex);
                    
                    // Allow direct connections in sequence (distance 1) or from start
                    if (distance > 1)
                    {
                        return true; // Creates a shortcut!
                    }
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Restricts the available variants in the WFC grid to match the generated path skeleton.
        /// </summary>
        private void ApplyPathToWFC(List<Vector2Int> path)
        {
            for (int i = 0; i < path.Count; i++)
            {
                Vector2Int current = path[i];
                Cell cell = grid[current.x, current.y];

                Vector2Int? prev = i > 0 ? path[i - 1] : (Vector2Int?)null;
                Vector2Int? next = i < path.Count - 1 ? path[i + 1] : (Vector2Int?)null;

                List<TileVariant> validForPath = new List<TileVariant>();

                // Choose the source variants based on whether it's the start, finish, or a middle cell
                List<TileVariant> sourceVariants = standardVariants;
                if (i == 0) sourceVariants = startVariants;
                else if (i == path.Count - 1) sourceVariants = finishVariants;

                // Choose only the variants with road sockets
                foreach (var variant in sourceVariants)
                {
                    bool matches = true;
                    Vector2Int[] dirs = { new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(0, -1), new Vector2Int(-1, 0) };

                    for (int d = 0; d < 4; d++)
                    {
                        Vector2Int neighborPos = current + dirs[d];
                        bool isPathConnection = (prev.HasValue && prev.Value == neighborPos) || 
                                                (next.HasValue && next.Value == neighborPos);

                        bool hasRoadSocket = variant.Sockets[d] == "road";

                        if (isPathConnection && !hasRoadSocket) matches = false;
                        if (!isPathConnection && hasRoadSocket) matches = false;
                    }

                    if (matches) validForPath.Add(variant);
                }

                if (validForPath.Count > 0)
                {
                    // Randomly choose one of the valid variants for the path cell
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
        /// Draws the path and scenery on the map.
        /// The path is drawn using the WFC-generated tiles,
        /// while the scenery is placed around the path with a simple random distribution of scenery tiles.
        /// </summary>
        /// <param name="path">Generated track</param>
        private void InstantiatePathAndScenery(List<Vector2Int> path)
        {
            HashSet<Vector2Int> pathSet = new HashSet<Vector2Int>(path);
            HashSet<Vector2Int> scenerySet = new HashSet<Vector2Int>();

            
            foreach (Vector2Int pathPos in path)
            {
                // Go through all 8 neighbors of the current position
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        if (x == 0 && y == 0) continue; // Ignore the path position itself

                        Vector2Int neighborPos = new Vector2Int(pathPos.x + x, pathPos.y + y);
                        
                        // Check if the neighbor is within the map bounds
                        if (neighborPos.x >= 0 && neighborPos.x < mapWidth && neighborPos.y >= 0 && neighborPos.y < mapHeight)
                        {
                            // If the neighbor is not part of the path, add it to the scenery set
                            if (!pathSet.Contains(neighborPos))
                            {
                                scenerySet.Add(neighborPos);
                            }
                        }
                    }
                }
            }

            // Draw the path - WFC-generated tiles
            foreach (Vector2Int pos in path)
            {
                Cell cell = grid[pos.x, pos.y];
                if (cell.CollapsedVariant != null)
                {
                    Vector3 worldPos = new Vector3(pos.x * tileSize, 0, pos.y * tileSize);
                    Quaternion rot = Quaternion.Euler(0, cell.CollapsedVariant.Rotation * 90f, 0);
                    Instantiate(cell.CollapsedVariant.Data.prefab, worldPos, rot, transform);
                }
            }

            // Draw the scenery in stable order so random picks stay deterministic with the same seed.
            List<Vector2Int> orderedScenery = scenerySet
                .OrderBy(p => p.x)
                .ThenBy(p => p.y)
                .ToList();

            // Draw the scenery - random scenery tiles
            foreach (Vector2Int pos in orderedScenery)
            {
                if (sceneryTiles == null || sceneryTiles.Count == 0)
                {
                    Debug.LogWarning("No scenery tiles available!");
                    continue;
                }
                
                Vector3 worldPos = new Vector3(pos.x * tileSize, 0, pos.y * tileSize);
                
                // Choose a random scenery tile from the list
                TileData sceneryToPlace = sceneryTiles[Random.Range(0, sceneryTiles.Count)];
                
                // Random rotation
                float randomYRot = Random.Range(0, 4) * 90f;
                Quaternion rot = Quaternion.Euler(0, randomYRot, 0);

                Instantiate(sceneryToPlace.prefab, worldPos, rot, transform);
            }
        }

        /// <summary>
        /// Returns the cell at the specified position.
        /// </summary>
        /// <param name="x">X position</param>
        /// <param name="y">Y position</param>
        /// <returns></returns>
        public Cell GetCell(int x, int y)
        {
            if (x >= 0 && x < mapWidth && y >= 0 && y < mapHeight)
            {
                return grid[x, y];
            }
            return null;
        }

        /// <summary>
        /// Checks if the map is fully collapsed (all cells are collapsed and have no available variants)
        /// </summary>
        /// <returns>True if collapsed, false otherwise</returns>
        private bool IsFullyCollapsed()
        {
            foreach (var cell in grid)
            {
                if (!cell.IsCollapsed) return false;
            }
            return true;
        }

        /// <summary>
        /// Returns the cell with the lowest entropy value.
        /// </summary>
        /// <returns>Cell with the lowest entropy</returns>
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
        /// Collapses a cell by choosing a random variant from its available variants.
        /// </summary>
        /// <param name="cell">Cell to be collapsed</param>
        private void CollapseCell(Cell cell)
        {
            int randomIndex = Random.Range(0, cell.AvailableVariants.Count);
            cell.CollapsedVariant = cell.AvailableVariants[randomIndex];
            cell.AvailableVariants.Clear();
            cell.AvailableVariants.Add(cell.CollapsedVariant);
            cell.IsCollapsed = true;
        }
        
        /// <summary>
        /// Propagates the collapsed cell's variant to its neighboring cells.'
        /// </summary>
        /// <param name="collapsedCell">Collapsed cell</param>
        private void Propagate(Cell collapsedCell)
        {
            Stack<Cell> stack = new Stack<Cell>();
            stack.Push(collapsedCell);

            while (stack.Count > 0)
            {
                Cell current = stack.Pop();

                // Check all 4 directions
                Vector2Int[] directions = { 
                    new Vector2Int(0, 1),  // North
                    new Vector2Int(1, 0),  // East
                    new Vector2Int(0, -1), // South
                    new Vector2Int(-1, 0)  // West
                };

                for (int i = 0; i < 4; i++)
                {
                    Vector2Int neighborPos = current.GridPosition + directions[i];

                    // Check if the neighbor is within the map bounds
                    if (neighborPos.x >= 0 && neighborPos.x < mapWidth && neighborPos.y >= 0 && neighborPos.y < mapHeight)
                    {
                        Cell neighbor = grid[neighborPos.x, neighborPos.y];
                        if (neighbor.IsCollapsed) continue;

                        // Cut down the neighbours variants
                        bool changed = ConstrainNeighbor(current, neighbor, i);
                        
                        if (changed)
                        {
                            stack.Push(neighbor);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Constrains the neighbor's variants to match the current cell's variant.
        /// </summary>
        /// <param name="current">Current cell</param>
        /// <param name="neighbor">Neighbor cell</param>
        /// <param name="directionIndex">Facing direction</param>
        /// <returns></returns>
        private bool ConstrainNeighbor(Cell current, Cell neighbor, int directionIndex)
        {
            bool changed = false;
            // directionIndex: 0:N, 1:E, 2:S, 3:W
            int neighborSideIndex = (directionIndex + 2) % 4;

            List<TileVariant> toRemove = new List<TileVariant>();

            foreach (var neighborVariant in neighbor.AvailableVariants)
            {
                bool possible = false;
                foreach (var currentVariant in current.AvailableVariants)
                {
                    // Check for socket compatibility
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

            foreach (var variant in toRemove)
            {
                neighbor.AvailableVariants.Remove(variant);
            }

            return changed;
        }
        
        /// <summary>
        /// Generates a valid map with start and finish cells.
        /// Using DFS to generate a random path and applying it to the WFC grid.
        /// Then running the WFC algorithm to collapse the cells.
        /// </summary>
        private bool GenerateValidMap()
        {
            ClearScene();
            InitializeGrid(); 
            
            List<Vector2Int> generatedPath = GenerateRandomPath(new Vector2Int(1, 1), targetTrackLength);

            if (generatedPath != null)
            {
                ApplyPathToWFC(generatedPath);
                RunWFC(); 
                InstantiatePathAndScenery(generatedPath);

                if (gameManager != null)
                {
                    gameManager.PlaceCarOnStart();
                }
                
                Debug.Log($"Track generated with length {generatedPath.Count}. Seed: {LastUsedSeed}");
                return true;
            }

            Debug.LogError($"Failed to generate a valid path. Seed: {LastUsedSeed}");
            return false;
        }
        
        /// <summary>
        /// Clears the scene by destroying all game objects in the scene.
        /// </summary>
        private void ClearScene()
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }
        
    }
}