using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class RatPathfinderTests
{
    private GameObject _graphObj;
    private RatGraph _graph;
    private List<GameObject> _nodeObjs;

    [SetUp]
    public void SetUp()
    {
        _graphObj = new GameObject("TestGraph");
        _graph = _graphObj.AddComponent<RatGraph>();
        _nodeObjs = new List<GameObject>();
    }

    [TearDown]
    public void TearDown()
    {
        for (int i = 0; i < _nodeObjs.Count; i++)
        {
            if (_nodeObjs[i] != null)
                Object.DestroyImmediate(_nodeObjs[i]);
        }
        if (_graphObj != null)
            Object.DestroyImmediate(_graphObj);
    }

    private RatNode CreateNode(string name, Vector3 pos)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        var node = go.AddComponent<RatNode>();
        _graph.AddNode(node);
        _nodeObjs.Add(go);
        return node;
    }

    [Test]
    public void ShortestPath_FindsOptimalRoute()
    {
        // A -> B -> C (cost 1 + 1 = 2)
        // A -> D -> C (cost 4 + 4 = 8)
        var nodeA = CreateNode("A", new Vector3(0, 0, 0));
        var nodeB = CreateNode("B", new Vector3(1, 0, 0));
        var nodeC = CreateNode("C", new Vector3(2, 0, 0));
        var nodeD = CreateNode("D", new Vector3(1, 0, 2));

        _graph.AddEdge(nodeA, nodeB, EdgeType.WALK, 1f);
        _graph.AddEdge(nodeB, nodeC, EdgeType.WALK, 1f);
        _graph.AddEdge(nodeA, nodeD, EdgeType.WALK, 4f);
        _graph.AddEdge(nodeD, nodeC, EdgeType.WALK, 4f);

        var path = RatPathfinder.FindPath(_graph, nodeA, nodeC);

        Assert.IsNotNull(path, "Path should not be null");
        Assert.AreEqual(3, path.Count, "Path should contain 3 nodes: A -> B -> C");
        Assert.AreEqual(nodeA, path[0].node);
        Assert.AreEqual(nodeB, path[1].node);
        Assert.AreEqual(nodeC, path[2].node);
    }

    [Test]
    public void BlockedEdge_IsAvoided()
    {
        // A -> B -> C (A->B is blocked)
        // A -> D -> C (fallback alternative)
        var nodeA = CreateNode("A", new Vector3(0, 0, 0));
        var nodeB = CreateNode("B", new Vector3(1, 0, 0));
        var nodeC = CreateNode("C", new Vector3(2, 0, 0));
        var nodeD = CreateNode("D", new Vector3(1, 0, 2));

        var edgeAB = _graph.AddEdge(nodeA, nodeB, EdgeType.WALK, 1f);
        edgeAB.blocked = true;
        _graph.AddEdge(nodeB, nodeC, EdgeType.WALK, 1f);
        _graph.AddEdge(nodeA, nodeD, EdgeType.WALK, 4f);
        _graph.AddEdge(nodeD, nodeC, EdgeType.WALK, 4f);

        var path = RatPathfinder.FindPath(_graph, nodeA, nodeC);

        Assert.IsNotNull(path, "Path should not be null when alternative exists");
        Assert.AreEqual(3, path.Count);
        Assert.AreEqual(nodeA, path[0].node);
        Assert.AreEqual(nodeD, path[1].node);
        Assert.AreEqual(nodeC, path[2].node);
    }

    [Test]
    public void NoPath_ReturnsNull()
    {
        var nodeA = CreateNode("A", new Vector3(0, 0, 0));
        var nodeB = CreateNode("B", new Vector3(5, 0, 0));

        var path = RatPathfinder.FindPath(_graph, nodeA, nodeB);

        Assert.IsNull(path, "Disconnected graph should return null");
    }

    [MenuItem("Tools/Run Pathfinder Tests")]
    public static void RunAllTests()
    {
        var tests = new RatPathfinderTests();
        try
        {
            tests.SetUp();
            tests.ShortestPath_FindsOptimalRoute();
            tests.TearDown();

            tests.SetUp();
            tests.BlockedEdge_IsAvoided();
            tests.TearDown();

            tests.SetUp();
            tests.NoPath_ReturnsNull();
            tests.TearDown();

            Debug.Log("<color=green>[RatPathfinderTests] ALL TESTS PASSED!</color>");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[RatPathfinderTests] TEST FAILED: {ex}");
            tests.TearDown();
        }
    }
}
