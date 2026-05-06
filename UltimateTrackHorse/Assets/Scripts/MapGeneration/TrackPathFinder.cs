using System.Collections.Generic;
using UnityEngine;

namespace MapGeneration
{
    /// <summary>
    /// Generates the track path using Greedy Local Search (GLS).
    /// Iterative crawler — no backtracking. Returns null on dead end.
    /// </summary>
    public class TrackPathfinder
    {
        private readonly int mapWidth;
        private readonly int mapHeight;

        private static readonly Vector2Int[] Dirs =
        {
            new Vector2Int(0,  1),
            new Vector2Int(1,  0),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0)
        };

        public TrackPathfinder(int width, int height)
        {
            mapWidth  = width;
            mapHeight = height;
        }

        /// <summary>
        /// Generates a random contiguous path of a specific length using GLS.
        /// Returns null if the crawler reaches a dead end.
        /// </summary>
        public List<Vector2Int> GeneratePath(Vector2Int startPos, int targetLength)
        {
            return GLSPath(startPos, targetLength);
        }

        /// <summary>
        /// Greedy Local Search crawler: at each step, valid neighbours are scored by
        /// their distance from the start position plus a small random noise for variety.
        /// The highest-scoring candidate is chosen. No backtracking.
        /// </summary>
        private List<Vector2Int> GLSPath(Vector2Int startPos, int targetLength)
        {
            var path = new List<Vector2Int>(targetLength);
            var visited = new HashSet<Vector2Int>();

            path.Add(startPos);
            visited.Add(startPos);

            while (path.Count < targetLength)
            {
                Vector2Int current = path[path.Count - 1];
                var candidates = new List<(Vector2Int pos, float score)>();

                foreach (var dir in Dirs)
                {
                    Vector2Int next = current + dir;

                    if (next.x < 1 || next.x >= mapWidth  - 1 ||
                        next.y < 1 || next.y >= mapHeight - 1) continue;
                    if (visited.Contains(next)) continue;
                    if (CreatesShortcut(next, path)) continue;

                    // Greedy heuristic: prefer cells farther from start.
                    // Noise range must be wide enough to let turns compete with straight moves,
                    // producing curved tracks rather than a straight diagonal trajectory.
                    float score = Vector2Int.Distance(next, startPos) + Random.Range(0f, 10f);
                    candidates.Add((next, score));
                }

                if (candidates.Count == 0)
                    return null; // dead end — no backtracking, caller must retry

                candidates.Sort((a, b) => b.score.CompareTo(a.score));
                Vector2Int chosen = candidates[0].pos;

                path.Add(chosen);
                visited.Add(chosen);
            }

            return path;
        }

        /// <summary>
        /// Returns true if placing newPos would create a shortcut between distant
        /// sections of the path (detected via diagonal adjacency to non-consecutive tiles).
        /// </summary>
        private bool CreatesShortcut(Vector2Int newPos, List<Vector2Int> path)
        {
            int currentIndex = path.Count - 1;

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0) continue;

                    Vector2Int neighbor = new Vector2Int(newPos.x + x, newPos.y + y);
                    int neighborIndex = path.IndexOf(neighbor);

                    if (neighborIndex == -1)
                    {
                        continue;
                    }
                    if (Mathf.Abs(neighborIndex - currentIndex) > 1)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
