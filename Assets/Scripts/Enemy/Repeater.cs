using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Repeater : Node
{
    public Repeater() { }

    public Repeater(string name) : base(name) { }

    public override Status Process()
    {
        Debug.Log("Processing " + name);
        Status childStatus = children[currentChild].Process();
        Debug.Log(name + " processing child " + children[currentChild].name + " with status " + childStatus);
        
        if (childStatus == Status.running) 
            return Status.running;
        
        if (childStatus == Status.failure) 
            return Status.failure;
        
        // Child succeeded, reset and loop
        Debug.Log(name + " child " + children[currentChild].name + " succeeded, restarting loop");
        currentChild = 0;
        return Status.running;
    }
}
