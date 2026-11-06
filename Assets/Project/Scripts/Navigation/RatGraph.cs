using System.Collections.Generic;
using UnityEngine;

public class RatGraph : MonoBehaviour
{
    [SerializeField] private List<RatNode> _nodes = new();
    [SerializeField] private List<RatEdge> _edges = new();

    public IReadOnlyList<RatNode> Nodes => _nodes;
    public IReadOnlyList<RatEdge> Edges => _edges;

    public void AddNode(RatNode node)
    {
        if (node != null && !_nodes.Contains(node))
            _nodes.Add(node);
    }

    public void RemoveNode(RatNode node)
    {
        _nodes.Remove(node);
        _edges.RemoveAll(e => e.from == node || e.to == node);
    }

    public RatEdge AddEdge(RatNode from, RatNode to, EdgeType type, float cost)
    {
        if (from == null || to == null) return null;
        var edge = new RatEdge(from, to, type, cost);
        _edges.Add(edge);
        return edge;
    }

    public void RemoveEdge(RatEdge edge)
    {
        _edges.Remove(edge);
    }

    public List<RatEdge> GetOutgoingEdges(RatNode node)
    {
        var result = new List<RatEdge>();
        for (int i = 0; i < _edges.Count; i++)
        {
            if (_edges[i].from == node)
                result.Add(_edges[i]);
        }
        return result;
    }

    public RatNode FindNearestNode(Vector3 pos, Collider surfaceCollider = null)
    {
        RatNode bestSameCollider = null;
        float bestSameDist = float.MaxValue;
        RatNode bestOverall = null;
        float bestOverallDist = float.MaxValue;

        for (int i = 0; i < _nodes.Count; i++)
        {
            var node = _nodes[i];
            if (node == null) continue;

            float dist = Vector3.Distance(pos, node.Position);

            bool hitObstacle = Physics.Linecast(pos + Vector3.up * 0.05f, node.Position + node.normal * 0.05f, out var hit) &&
                               IsObstacle(hit.collider, surfaceCollider, node.surfaceCollider);

            if (!hitObstacle)
            {
                if (surfaceCollider != null && node.surfaceCollider == surfaceCollider && dist < bestSameDist)
                {
                    bestSameCollider = node;
                    bestSameDist = dist;
                }
                if (dist < bestOverallDist)
                {
                    bestOverall = node;
                    bestOverallDist = dist;
                }
            }
        }

        return bestSameCollider != null ? bestSameCollider : bestOverall;
    }

    private static bool IsObstacle(Collider hit, Collider surfaceA, Collider surfaceB)
    {
        if (hit == null || hit.isTrigger) return false;
        if (hit == surfaceA || hit == surfaceB) return false;
        if (hit.GetComponentInParent<MovementController>() != null) return false;
        return true;
    }

    [ContextMenu("Rebuild Node List From Children")]
    public void RebuildFromChildren()
    {
        _nodes.Clear();
        var childNodes = GetComponentsInChildren<RatNode>();
        for (int i = 0; i < childNodes.Length; i++)
        {
            childNodes[i].id = i;
            _nodes.Add(childNodes[i]);
        }
    }

    private void OnDrawGizmos()
    {
        for (int i = 0; i < _edges.Count; i++)
        {
            var edge = _edges[i];
            if (edge.from == null || edge.to == null) continue;

            if (edge.blocked)
                Gizmos.color = Color.red;
            else
            {
                Gizmos.color = edge.type switch
                {
                    EdgeType.WALK => Color.green,
                    EdgeType.JUMP_ATTACH => Color.magenta,
                    EdgeType.JUMP_GAP => Color.yellow,
                    EdgeType.WALL_WALK => Color.blue,
                    EdgeType.DROP => Color.gray,
                    EdgeType.CORNER => Color.cyan,
                    _ => Color.white
                };
            }

            Gizmos.DrawLine(edge.from.Position, edge.to.Position);
        }
    }
}
