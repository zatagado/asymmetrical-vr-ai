using System.Collections.Generic;

/// <summary>
/// Continues until it reaches a success or running. Also known as a fallback node.
/// Nodes should be ordered in the tree by priority of the node.
/// </summary>
public class BTSelector : BTNode
{
    protected List<BTNode> nodes = new List<BTNode>();

    /// <summary>
    /// Constructor for BTSelector.
    /// Continues until it reaches a success or running. Also known as a fallback node.
    /// Nodes should be ordered in the tree by priority of the node.
    /// </summary>
    /// <param name="nodes">A list of nodes that you would like the BTSelector to run.</param>
    public BTSelector(List<BTNode> nodes)
    {
        this.nodes = nodes;
    }

    /// <summary>
    /// Runs when the behavior tree reaches this node.
    /// Continues until it reaches a success or running.
    /// </summary>
    /// <returns>A BTNodeState representing if the behavior was successful, running, or a failure.</returns>
    public override BTNodeState Tick()
    {
        foreach (BTNode node in nodes)
        {
            switch (node.Tick())
            {
                case BTNodeState.SUCCESS:
                    currentNodeState = BTNodeState.SUCCESS;
                    return currentNodeState;
                case BTNodeState.FAILURE:
                    continue;
                case BTNodeState.RUNNING:
                    currentNodeState = BTNodeState.RUNNING;
                    return currentNodeState;
            }
        }
        currentNodeState = BTNodeState.FAILURE;
        return currentNodeState;
    }
}
