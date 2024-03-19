using System.Collections.Generic;

/// <summary>
/// Stops if failure, continues on success or running.
/// </summary>
public class BTSequence : BTNode
{
    protected List<BTNode> nodes = new List<BTNode>();

    /// <summary>
    /// Constructor for BTSequence.
    /// Stops if failure, continues on success or running.
    /// </summary>
    /// <param name="nodes">A list of nodes that you would like the BTSequence to run.</param>
    public BTSequence(List<BTNode> nodes)
    {
        this.nodes = nodes;
    }

    /// <summary>
    /// Runs when the behavior tree reaches this node.
    /// Continues until it reaches a failure.
    /// </summary>
    /// <returns>A BTNodeState represeting if the behavior was successful, running, or a failure.</returns>
    public override BTNodeState Tick()
    {
        bool childNodeRunning = false;

        foreach (BTNode node in nodes)
        {
            switch (node.Tick())
            {
                case BTNodeState.SUCCESS:
                    continue;
                case BTNodeState.FAILURE:
                    currentNodeState = BTNodeState.FAILURE;
                    return currentNodeState;
                case BTNodeState.RUNNING:
                    childNodeRunning = true;
                    continue;
                default:
                    currentNodeState = BTNodeState.SUCCESS;
                    return currentNodeState;
            }
        }
        currentNodeState = childNodeRunning ? BTNodeState.RUNNING : BTNodeState.SUCCESS;
        return currentNodeState;
    }
}
