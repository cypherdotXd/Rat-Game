using System;
using System.Collections.Generic;
using UnityEngine;

namespace Behaviour
{
    public enum Status {Success, Failed, Running}
    
    [CreateAssetMenu(fileName = "NewNode", menuName = "Nodes/Node")]
    public class Node
    {
        protected readonly string nodeName;
        protected readonly List<Node> Children = new();

        protected Node(string name)
        {
            nodeName = name;
        }

        public Node AddChild(Node node)
        {
            Children.Add(node);
            return this;
        }
        
        public virtual Status Run() => Status.Success;
    }

    public class BehaviourTree : Node
    {
        public BehaviourTree(string name) : base(name)
        {
        }

        public override Status Run()
        {
            foreach (var child in Children)
            {
                var status = child.Run();
                if (status != Status.Success) return status;
            }
            return Status.Success;
        }
    }

    public class SequenceNode : Node
    {
        public SequenceNode(string name) : base(name)
        {
        }

        public override Status Run()
        {
            foreach (var child in Children)
            {
                switch (child.Run())
                {
                    case Status.Success:
                        continue;
                    case Status.Failed:
                        return Status.Failed;
                    case Status.Running:
                        return Status.Running;
                }
            }
            
            return Status.Success;
        }
    }

    [CreateAssetMenu(fileName = "NewSelectorNode", menuName = "Nodes/Selector Node")]
    public class SelectorNode : Node
    {
        public SelectorNode(string name) : base(name)
        {
        }

        public override Status Run()
        {
            foreach (var child in Children)
            {
                switch (child.Run())
                {
                    case Status.Success:
                        return Status.Success;
                    case Status.Failed:
                        return Status.Failed;
                    case Status.Running:
                        return Status.Running;
                }
            }

            return Status.Failed;
        }
    }
    
    public class LeafNode : Node
    {
        private IStrategy strategy;
        
        public LeafNode(string name, IStrategy strategy) : base(name)
        {
            this.strategy = strategy;
        }

        public override Status Run()
        {
            var result = strategy.Run();
            Debug.Log($"Node {nodeName}, result {result}");
            return result;
        } 
    }
    
}
