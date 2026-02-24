using System.Collections.Generic;
using UnityEngine;

public class Cell
{
    public Vector2Int gridPosition; // Position of the cell in the grid
    public bool isCollapsed;        // Indicates whether the cell has been collapsed to a single tile or not
    public List<TileData> availableTiles; // List of possible tiles that can still be placed in this cell based on the WFC constraints
    public TileData collapsedTile;  // Final tile that was chosen

    // Constructor of the cell, initializes it with all possible tiles at the beginning
    public Cell(Vector2Int pos, List<TileData> allTiles)
    {
        gridPosition = pos;
        isCollapsed = false;
        
        // All tiles are initialy available
        availableTiles = new List<TileData>(allTiles);
    }

    // Entropy - number of possible tiles that can be placed
    public int Entropy => availableTiles.Count; 
}