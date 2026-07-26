using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Leaf : Node
{
    public delegate Status LeafFunction();
    public LeafFunction function;
    public Leaf() { }

    public Leaf(string name, LeafFunction function) : base(name)
    {
        this.function = function;
    }

    public override Status Process()
    {
        // Debug.Log("Processing " + name);
        if (function != null)
        {
            return function();
        }
        return Status.failure;
    }
}