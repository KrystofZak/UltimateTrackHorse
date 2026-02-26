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

        private Cell[,] grid; // 2D array representing the map
        private List<TileVariant> allVariants; // List of all possible tile variants

        void Start()
        {
            InitializeGrid(); 
            RunWFC();
            //StartCoroutine(RunWFCAnimated());
        }

        void InitializeGrid()
        {
            // Create a list of all possible tile variants
            allVariants = new List<TileVariant>();
            foreach (var tile in allAvailableTiles)
            {
                for (int r = 0; r < 4; r++)
                {
                    allVariants.Add(new TileVariant(tile, r));
                }
            }

            // Initialize the grid
            grid = new Cell[mapWidth, mapHeight];
            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    grid[x, y] = new Cell(new Vector2Int(x, y), allVariants);
                }
            }
        }
        
        void SetStartAndFinish()
        {
            // Find all road variants
            var roadVariants = allVariants.Where(v => v.Sockets.Contains("road")).ToList();

            // Set start to [0, 0]
            Cell startCell = grid[0, 0];
            startCell.AvailableVariants = new List<TileVariant>(roadVariants);
            CollapseCell(startCell); // Collapse random road 
            Propagate(startCell);    // Propagate to neighbors

            // Set finish to [mapWidth-1, mapHeight-1]
            Cell endCell = grid[mapWidth - 1, mapHeight - 1];
            endCell.AvailableVariants = new List<TileVariant>(roadVariants);
            CollapseCell(endCell);
            Propagate(endCell);
        }
        
        // Wave Function Collapse Algorithm
        public void RunWFC()
        {
            SetStartAndFinish();
            
            while (!IsFullyCollapsed())
            {
                Cell nextCell = GetCellWithLowestEntropy();
                
                // No possible cell to collapse
                if (nextCell == null || nextCell.Entropy == 0)
                {
                    Debug.LogError("Error: No possible cell to collapse.");
                    return; 
                }

                CollapseCell(nextCell);
                Propagate(nextCell);
            }

            InstantiateTiles();
        }

        // Checks if all cells are collapsed
        bool IsFullyCollapsed()
        {
            foreach (var cell in grid)
            {
                if (!cell.IsCollapsed) return false;
            }
            return true;
        }

        // Choose the cell with the lowest entropy
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

        // Randomly selects a variant from the available variants and collapses the cell
        void CollapseCell(Cell cell)
        {
            int randomIndex = Random.Range(0, cell.AvailableVariants.Count);
            cell.CollapsedVariant = cell.AvailableVariants[randomIndex];
            cell.AvailableVariants.Clear();
            cell.AvailableVariants.Add(cell.CollapsedVariant);
            cell.IsCollapsed = true;
        }

        // Propagates the collapsed cell's variant to its neighbors
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
                    // Cheeck for socket compatibility
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

        // Instantiate the final map
        void InstantiateTiles()
        {
            foreach (var cell in grid)
            {
                if (cell.CollapsedVariant != null)
                {
                    Vector3 pos = new Vector3(cell.GridPosition.x * tileSize, 0, cell.GridPosition.y * tileSize);
                    Quaternion rot = Quaternion.Euler(0, cell.CollapsedVariant.Rotation * 90, 0);
                    Instantiate(cell.CollapsedVariant.Data.prefab, pos, rot, transform);
                }
            }
        }
        

        // Animated version of the WFC algorithm - debug only
        public IEnumerator RunWFCAnimated()
        {
            SetStartAndFinish();
            while (!IsFullyCollapsed())
            {
                Cell nextCell = GetCellWithLowestEntropy();
        
                if (nextCell == null || nextCell.Entropy == 0)
                {
                    Debug.LogError("Error: No possible cell to collapse!");
                    yield break; 
                }

                CollapseCell(nextCell);
                Propagate(nextCell);
                
                VisualizeCell(nextCell); 
                yield return new WaitForSeconds(0.05f); // Small delay
            }
        }
        
        // Helper function for animated visualization
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