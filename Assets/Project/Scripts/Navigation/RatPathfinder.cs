using System.Collections.Generic;
using UnityEngine;

public static class RatPathfinder
{
    public static List<(RatNode node, RatEdge edge)> FindPath(RatGraph graph, RatNode start, RatNode goal, float maxSpeed = 1.4f)
    {
        if (graph == null || start == null || goal == null) return null;
        if (start == goal) return new List<(RatNode, RatEdge)> { (start, null) };

        var openSet = new List<RatNode> { start };
        var cameFrom = new Dictionary<RatNode, (RatNode fromNode, RatEdge viaEdge)>();
        var gScore = new Dictionary<RatNode, float> { [start] = 0f };
        var fScore = new Dictionary<RatNode, float> { [start] = Heuristic(start, goal, maxSpeed) };

        while (openSet.Count > 0)
        {
            int bestIndex = 0;
            float bestF = GetScore(fScore, openSet[0]);
            for (int i = 1; i < openSet.Count; i++)
            {
                float score = GetScore(fScore, openSet[i]);
                if (score < bestF)
                {
                    bestF = score;
                    bestIndex = i;
                }
            }

            var current = openSet[bestIndex];
            if (current == goal)
                return ReconstructPath(cameFrom, current);

            openSet.RemoveAt(bestIndex);

            var edges = graph.GetOutgoingEdges(current);
            for (int i = 0; i < edges.Count; i++)
            {
                var edge = edges[i];
                if (edge == null || edge.blocked || edge.to == null) continue;

                var neighbor = edge.to;
                float tentativeG = GetScore(gScore, current) + edge.cost;

                if (tentativeG < GetScore(gScore, neighbor))
                {
                    cameFrom[neighbor] = (current, edge);
                    gScore[neighbor] = tentativeG;
                    fScore[neighbor] = tentativeG + Heuristic(neighbor, goal, maxSpeed);

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }

        return null;
    }

    private static float Heuristic(RatNode a, RatNode b, float maxSpeed)
    {
        return Vector3.Distance(a.Position, b.Position) / Mathf.Max(maxSpeed, 0.001f);
    }

    private static float GetScore(Dictionary<RatNode, float> scoreMap, RatNode node)
    {
        return scoreMap.TryGetValue(node, out float score) ? score : float.MaxValue;
    }

    private static List<(RatNode node, RatEdge edge)> ReconstructPath(
        Dictionary<RatNode, (RatNode fromNode, RatEdge viaEdge)> cameFrom,
        RatNode current)
    {
        var path = new List<(RatNode node, RatEdge edge)>();
        var curr = current;

        while (cameFrom.TryGetValue(curr, out var step))
        {
            path.Add((curr, step.viaEdge));
            curr = step.fromNode;
        }

        path.Add((curr, null));
        path.Reverse();
        return path;
    }
}
