using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Selector : Node
{
    public Selector() { }

    public Selector(string name) : base(name) { }

    public override Status Process()
    {
        Debug.Log("Processing " + name);
        Status childStatus = children[currentChild].Process();
        Debug.Log(name + " processing child " + children[currentChild].name + " with status " + childStatus);
        if (childStatus == Status.running) return Status.running;
        if (childStatus == Status.success)
        {
            currentChild = 0;
            Debug.Log(name + " child " + children[currentChild].name + " succeeded");
            return Status.success;
        }
        ;
        
        Debug.Log(name + " child " + children[currentChild].name + " failed");
        currentChild++;
        if (currentChild >= children.Count)
        {
            currentChild = 0;
        }

        return Status.running;
    }
}