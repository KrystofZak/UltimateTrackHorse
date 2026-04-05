using System.Collections.Generic;
using UnityEngine;

namespace MapGeneration
{
    /// <summary>
    /// Purely logical class responsible for generating the track layout (path).
    /// Currently uses Depth-First Search (DFS).
    /// </summary>
    public class TrackPathfinder
    {
        private int mapWidth;
        private int mapHeight;

        public TrackPathfinder(int width, int height)
        {
            this.mapWidth = width;
            this.mapHeight = height;
        }

        /// <summary>
        /// Generates a random contiguous path of a specific length using DFS.
        /// </summary>
        public List<Vector2Int> GeneratePath(Vector2Int startPos, int targetLength)
        {
            List<Vector2Int> currentPath = new List<Vector2Int>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
            
            if (DFSPath(startPos, targetLength, currentPath, visited))
            {
                return currentPath;
            }
            return null; 
        }

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
                        // Check if the next position creates a shortcut
                        if (!CreatesShortcut(next, current, path))
                        {
                            if (DFSPath(next, targetLength, path, visited))
                                return true;
                        }
                    }
                }
            }

            // Backtracking
            path.RemoveAt(path.Count - 1);
            visited.Remove(current);
            return false;
        }

        private bool CreatesShortcut(Vector2Int newPos, Vector2Int currentPos, List<Vector2Int> path)
        {
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0) continue;

                    Vector2Int neighbor = new Vector2Int(newPos.x + x, newPos.y + y);
                    
                    int neighborIndex = path.IndexOf(neighbor);
                    if (neighborIndex == -1) continue; 
                    
                    int currentIndex = path.IndexOf(currentPos);
                    int distance = Mathf.Abs(neighborIndex - currentIndex);
                    
                    if (distance > 1)
                    {
                        return true; 
                    }
                }
            }
            return false;
        }
    }
}