using System.Collections.Generic;
using UnityEngine;

namespace MapGeneration
{
    public class MapGenerator : MonoBehaviour
    {
        [Header("Map Settings")] public int mapWidth = 10;
        public int mapHeight = 10;
        public float tileSize = 10f; // Distance between tiles

        [Header("WFC Data")] public List<TileData> allAvailableTiles; // List of all possible tile types

        private Cell[,] grid; // 2D array representing the map grid

        void Start()
        {
            InitializeGrid();
            // SetStartAndFinishBoundary();
            // RunWFC();
        }

        // Creates the grid and initializes each cell with all possible tiles (maximum entropy)
        void InitializeGrid()
        {
            grid = new Cell[mapWidth, mapHeight];

            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    grid[x, y] = new Cell(new Vector2Int(x, y), allAvailableTiles);
                }
            }

            Debug.Log($"Grid {mapWidth}x{mapHeight} was initialized with max entropy.");
        }
    }
}