using System;
using System.Collections.Generic;
using UnityEngine;

namespace Behaviour
{
    public enum Status {Success, Failed, Running}
    
    [CreateAssetMenu(fileName = "NewNode", menuName = "Nodes/Node")]
    public class Node : ScriptableObject
    {
        public string nodeName;
        public List<Node> Children = new();
    
        public Node(string name)
        {
            this.name = name;
        }
        public virtual Status Run() => Status.Success;
    }

    [CreateAssetMenu(fileName = "NewSequenceNode", menuName = "Nodes/Sequence Node")]
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
    
    public class MoveTo
}
