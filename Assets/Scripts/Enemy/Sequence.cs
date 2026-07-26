using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sequence : Node
{
    public Sequence() { }

    public Sequence(string name) : base(name) { }

    public override Status Process()
    {
        Debug.Log("Processing " + name);
        Status childStatus = children[currentChild].Process();
        Debug.Log(name + " processing child " + children[currentChild].name + " with status " + childStatus);
        if (childStatus == Status.running) return Status.running;
        if (childStatus == Status.failure) return childStatus;
        
        Debug.Log(name + " child " + children[currentChild].name + " succeeded");
        currentChild++;
        if (currentChild >= children.Count)
        {
            currentChild = 0;
            return Status.success;
        }

        return Status.running;
    }
}