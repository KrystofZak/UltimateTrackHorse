using System.Collections.Generic;
using UnityEngine;

namespace MapGeneration
{
    public class Cell
    {
        public Vector2Int GridPosition; // Position of the cell in the grid
        public bool IsCollapsed; // Indicates whether the cell has been collapsed to a single tile or not

        public readonly List<TileVariant> AvailableVariants; // List of possible tiles that can still be placed in this cell based on the WFC constraints

        public TileVariant CollapsedVariant; // Final tile that was chosen

        // Constructor of the cell, initializes it with all possible tiles at the beginning
        public Cell(Vector2Int pos, List<TileVariant> allVariants)
        {
            GridPosition = pos;
            IsCollapsed = false;

            // All tile variations are initially available
            AvailableVariants = new List<TileVariant>(allVariants);
        }

        // Entropy - a number of possible tiles that can be placed
        public int Entropy => AvailableVariants.Count;
    }
}