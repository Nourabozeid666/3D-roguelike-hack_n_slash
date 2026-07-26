using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BehaviourTree : Node
{
    public BehaviourTree()
    {
        name = "Tree";
    }
    public BehaviourTree(string name) : base(name)
    {
    }

    struct NodeLevel
    {
        public Node node;
        public int level;

        public NodeLevel(Node node, int level)
        {
            this.node = node;
            this.level = level;
        }
    }

    public override Status Process()
    {
        return children[currentChild].Process();
    }

    public void PrintTree()
    {
        string treePrintout = "";
        Stack<NodeLevel> stack = new Stack<NodeLevel>();
        Node currentNode = this;
        stack.Push(new NodeLevel(currentNode, 0));

        while (stack.Count > 0)
        {
            NodeLevel currentLevel = stack.Pop();
            currentNode = currentLevel.node;
            treePrintout += new string('-', currentLevel.level * 2) + currentNode.name + "\n";
            foreach (Node child in currentNode.children)
            {
                stack.Push(new NodeLevel(child, currentLevel.level + 1));
            }
        }
        Debug.Log(treePrintout);
    }

}