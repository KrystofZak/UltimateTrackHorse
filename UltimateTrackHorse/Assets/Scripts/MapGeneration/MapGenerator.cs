using System.Collections;
using System.Collections.Generic;
using System.Linq; 
using UnityEngine;

namespace MapGeneration
{
    public class MapGenerator : MonoBehaviour
    {
        [Header("Map Settings")] 
        public int mapWidth = 10;
        public int mapHeight = 10;
        public float tileSize = 20f; 

        [Header("WFC Data")] 
        public List<TileData> allAvailableTiles; // List of all available tiles
        
        // Scenery tiles for areas around the track
        [Header("Scenery Tiles")]
        public TileData roadPlain; 
        public TileData roadPlainHouse;
        
        // Start and finish tiles
        [Header("Special Tiles")]
        public TileData startTileData;
        public TileData finishTileData;

        private Cell[,] grid; // 2D array representing the map
        private List<TileVariant> standardVariants; // List of all possible tile variants
        private List<TileVariant> startVariants; // List of possible start tile variants
        private List<TileVariant> finishVariants; // List of possible finish tile variants

        /// <summary>
        /// Initializes the map generator and generates a valid map with start and finish cells
        /// </summary>
        void Start()
        {
            InitializeGrid(); 
            GenerateValidMap();
        }

        /// <summary>
        /// Initializes the grid with empty cells and splits each tile into its 4 possible rotations
        /// </summary>
        void InitializeGrid()
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
        
        /// <summary>
        /// Sets the start and finish cells to valid positions
        /// </summary>
        /// <param name="visualize"></param>
        void SetStartAndFinish(bool visualize = false)
        {
            // Set start
            Cell startCell = grid[1, 1];
    
            // Choose valid rotations for start
            startCell.AvailableVariants = startVariants
                .Where(v => v.Sockets[0] == "road" || v.Sockets[1] == "road")
                .ToList();
    
            CollapseCell(startCell);
            Propagate(startCell);
            if (visualize) VisualizeCell(startCell);


            // Set a finish
            Cell endCell = grid[mapWidth - 2, mapHeight - 2];
    
            // Choose valid rotations for finish
            endCell.AvailableVariants = finishVariants
                .Where(v => v.Sockets[2] == "road" || v.Sockets[3] == "road")
                .ToList();
    
            CollapseCell(endCell);
            Propagate(endCell);
            if (visualize) VisualizeCell(endCell);
        }
        
        // Wave Function Collapse Algorithm
        public void RunWFC()
        {
            SetStartAndFinish();
            
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
        /// BFS algorithm for finding a path between two points on the generated map,
        /// considering only tiles that have "road" sockets connecting them
        /// </summary>
        /// <param name="start">Start of the track</param>
        /// <param name="end">Finish of the track</param>
        /// <returns></returns>
        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int end)
        {
            Queue<Vector2Int> frontier = new Queue<Vector2Int>();
            frontier.Enqueue(start);

            // Dictionary to store the path
            Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            cameFrom[start] = start; 

            while (frontier.Count > 0)
            {
                Vector2Int current = frontier.Dequeue();

                if (current == end) 
                {
                    // Finish was found, build the path
                    List<Vector2Int> path = new List<Vector2Int>();
                    Vector2Int curr = end;
                    while (curr != start) 
                    {
                        path.Add(curr);
                        curr = cameFrom[curr];
                    }
                    path.Add(start);
                    path.Reverse(); // Revert the path to get it from start to finish
                    return path; 
                }

                // 
                foreach (Vector2Int next in GetRoadNeighbors(current))
                {
                    if (!cameFrom.ContainsKey(next))
                    {
                        cameFrom[next] = current;
                        frontier.Enqueue(next);
                    }
                }
            }

            return null; // Cesta neexistuje
        }
        
        /// <summary>
        /// Draws the path and scenery on the map.
        /// The path is drawn using the WFC-generated tiles,
        /// while the scenery is placed around the path with a simple random distribution of scenery tiles.
        /// </summary>
        /// <param name="path">Generated track</param>
        void InstantiatePathAndScenery(List<Vector2Int> path)
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
            foreach (Vector2Int pos in pathSet)
            {
                Cell cell = grid[pos.x, pos.y];
                if (cell.CollapsedVariant != null)
                {
                    Vector3 worldPos = new Vector3(pos.x * tileSize, 0, pos.y * tileSize);
                    Quaternion rot = Quaternion.Euler(0, cell.CollapsedVariant.Rotation * 90f, 0);
                    Instantiate(cell.CollapsedVariant.Data.prefab, worldPos, rot, transform);
                }
            }

            // Draw the scenery - random scenery tiles
            foreach (Vector2Int pos in scenerySet)
            {
                Vector3 worldPos = new Vector3(pos.x * tileSize, 0, pos.y * tileSize);
                
                // Choose a random scenery tile (80% chance for plain, 20% chance for house)
                TileData sceneryToPlace = (Random.value > 0.2f) ? roadPlain : roadPlainHouse;
                
                // Random rotation
                float randomYRot = Random.Range(0, 4) * 90f;
                Quaternion rot = Quaternion.Euler(0, randomYRot, 0);

                Instantiate(sceneryToPlace.prefab, worldPos, rot, transform);
            }
        }

        /// <summary>
        /// Checks if the map is fully collapsed (all cells are collapsed and have no available variants)
        /// </summary>
        /// <returns>True if collapsed, false otherwise</returns>
        bool IsFullyCollapsed()
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
        Cell GetCellWithLowestEntropy()
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
        void CollapseCell(Cell cell)
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
        void Propagate(Cell collapsedCell)
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
        bool ConstrainNeighbor(Cell current, Cell neighbor, int directionIndex)
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
        /// Helper function to get all neighbors of a cell that have "road" sockets.
        /// </summary>
        /// <param name="pos">Current position</param>
        /// <returns></returns>
        List<Vector2Int> GetRoadNeighbors(Vector2Int pos)
        {
            List<Vector2Int> neighbors = new List<Vector2Int>();
            Cell currentCell = grid[pos.x, pos.y];
            
            if (currentCell.CollapsedVariant == null) return neighbors;

            Vector2Int[] directions = { 
                new Vector2Int(0, 1),  // N (index 0)
                new Vector2Int(1, 0),  // E (index 1)
                new Vector2Int(0, -1), // S (index 2)
                new Vector2Int(-1, 0)  // W (index 3)
            };

            for (int i = 0; i < 4; i++)
            {
                
                if (currentCell.CollapsedVariant.Sockets[i] == "road")
                {
                    Vector2Int neighborPos = pos + directions[i];
                    
                    if (neighborPos.x >= 0 && neighborPos.x < mapWidth && neighborPos.y >= 0 && neighborPos.y < mapHeight)
                    {
                        Cell neighborCell = grid[neighborPos.x, neighborPos.y];
                        int oppositeSide = (i + 2) % 4;
                        
                        if (neighborCell.CollapsedVariant != null && 
                            neighborCell.CollapsedVariant.Sockets[oppositeSide] == "road")
                        {
                            neighbors.Add(neighborPos);
                        }
                    }
                }
            }
            return neighbors;
        }
        
        /// <summary>
        /// Generates a valid map with start and finish cells.
        /// 100 attempts are made to generate a map with a valid path between the start and finish.
        /// </summary>
        public void GenerateValidMap()
        {
            int attempts = 0;
            List<Vector2Int> validPath = null;

            while (validPath == null && attempts < 100)
            {
                attempts++;
                ClearScene();
                InitializeGrid(); 
                
                RunWFC();

                // Hledáme cestu mezi tvými novými body (1,1) a (width-2, height-2)
                validPath = FindPath(new Vector2Int(1, 1), new Vector2Int(mapWidth - 2, mapHeight - 2));

                if (validPath != null)
                {
                    Debug.Log($"Map generated with {attempts} attempts");
                    
                    // Zavoláme naši novou sjednocenou funkci
                    InstantiatePathAndScenery(validPath);
                }
            }
            
            if (validPath == null)
            {
                Debug.LogError("Nepodařilo se najít průjezdnou mapu ani po 100 pokusech.");
            }
        }
        
        /// <summary>
        /// Clears the scene by destroying all game objects in the scene.
        /// </summary>
        void ClearScene()
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }
        
        
        /// <summary>
        /// Visualizes a cell in the scene.
        /// </summary>
        /// <param name="cell">Cell to be drawn</param>
        void VisualizeCell(Cell cell)
        {
            if (cell.CollapsedVariant != null)
            {
                Vector3 pos = new Vector3(cell.GridPosition.x * tileSize, 0, cell.GridPosition.y * tileSize);
                Quaternion rot = Quaternion.Euler(0, cell.CollapsedVariant.Rotation * 90, 0);
                Instantiate(cell.CollapsedVariant.Data.prefab, pos, rot, transform);
            }
        }
        
    }
}