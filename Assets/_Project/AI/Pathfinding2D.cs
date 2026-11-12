using UnityEngine;
using System.Collections.Generic;
using System;

namespace DebtDungeon.AI
{
    /// <summary>
    /// Static tile-grid A* pathfinder evaluating walkable cells against collision masks.
    /// </summary>
    public static class Pathfinding2D
    {
        private const int MaxSearchIterations = 400;
        private const float NodeRadius = 0.38f;
        private const float TileBoxCheckSize = 0.75f;
        private const float GoalFreeCheckSize = 0.7f;

        private static readonly Vector2Int[] Directions =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        /// <summary>
        /// Finds the shortest grid path between two points while avoiding obstacle colliders.
        /// </summary>
        public static List<Vector2> FindPath(Vector2 startPos, Vector2 targetPos, LayerMask obstacleMask, Func<Vector2Int, bool> floorWalkableCheck = null)
        {
            Vector2Int startNode = Vector2Int.RoundToInt(startPos);
            Vector2Int targetNode = Vector2Int.RoundToInt(targetPos);

            if (startNode == targetNode)
            {
                return new List<Vector2> { targetPos };
            }

            var openSet = new PriorityQueue<Vector2Int>();
            var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            var gScore = new Dictionary<Vector2Int, float> { [startNode] = 0f };
            var closedSet = new HashSet<Vector2Int>();

            openSet.Enqueue(startNode, Heuristic(startNode, targetNode));

            int iterations = 0;
            Vector2Int closestNode = startNode;
            float closestDistance = float.MaxValue;

            while (openSet.Count > 0 && iterations++ < MaxSearchIterations)
            {
                Vector2Int current = openSet.Dequeue();

                if (current == targetNode)
                {
                    return RetracePath(cameFrom, current, targetPos);
                }

                closedSet.Add(current);

                float currentDist = Heuristic(current, targetNode);
                if (currentDist < closestDistance)
                {
                    closestDistance = currentDist;
                    closestNode = current;
                }

                for (int i = 0; i < Directions.Length; i++)
                {
                    Vector2Int neighbor = current + Directions[i];

                    if (closedSet.Contains(neighbor))
                    {
                        continue;
                    }

                    // 1. Check FloorQuery if available, else Physics overlap on Walls layer
                    if (floorWalkableCheck != null)
                    {
                        if (!floorWalkableCheck(neighbor))
                        {
                            continue;
                        }
                    }
                    else if (Physics2D.OverlapBox((Vector2)neighbor, Vector2.one * NodeRadius * 2f, 0f, obstacleMask) != null)
                    {
                        continue;
                    }

                    float tentativeG = gScore[current] + 1f;

                    if (!gScore.TryGetValue(neighbor, out float currentG) || tentativeG < currentG)
                    {
                        gScore[neighbor] = tentativeG;
                        cameFrom[neighbor] = current;
                        openSet.Enqueue(neighbor, tentativeG + Heuristic(neighbor, targetNode));
                    }
                }
            }

            // Fallback to the closest reachable tile if target is inside an obstacle
            return RetracePath(cameFrom, closestNode, targetPos);
        }

        private static float Heuristic(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y); // Manhattan distance
        }

        private static List<Vector2> RetracePath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int endNode, Vector2 exactTarget)
        {
            var path = new List<Vector2>();
            Vector2Int current = endNode;

            while (cameFrom.ContainsKey(current))
            {
                path.Add((Vector2)current);
                current = cameFrom[current];
            }

            path.Reverse();
            if (path.Count > 0)
            {
                path[path.Count - 1] = exactTarget; // Snap final waypoint to exact target coordinate
            }
            return path;
        }

        private class PriorityQueue<T>
        {
            private readonly List<(T item, float priority)> _elements = new List<(T, float)>();
            public int Count => _elements.Count;

            public void Enqueue(T item, float priority)
            {
                _elements.Add((item, priority));
                _elements.Sort((a, b) => a.priority.CompareTo(b.priority));
            }

            public T Dequeue()
            {
                T item = _elements[0].item;
                _elements.RemoveAt(0);
                return item;
            }
        }
    }
}
