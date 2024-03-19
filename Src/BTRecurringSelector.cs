using System.Collections.Generic;

/// <summary>
/// Continues until it reaches a success or running. Also known as a fallback node.
/// If there is a previously running node, then it is automatically run.
/// </summary>
public class BTRecurringSelector : BTSelector
{
    protected BTNode runningNode = null;

    /// <summary>
    /// Constructor for BTRecurringSelector.
    /// Continues until it reaches a success or running. Also known as a fallback node.
    /// If there is a previously running node, then it is automatically run.
    /// </summary>
    /// <param name="nodes">A list of nodes that you would like the BTRecurringSelector to run.</param>
    public BTRecurringSelector(List<BTNode> nodes) : base(nodes)
    {
        runningNode = null;
    }

    /// <summary>
    /// Runs when the behavior tree reaches this node.
    /// Automatically runs previously running node, otherwise continues until it reaches a success or running.
    /// </summary>
    /// <returns>A BTNodeState representing if the behavior was successful, running, or a failure.</returns>
    public override BTNodeState Tick()
    {
        if (runningNode != null)
        {
            currentNodeState = runningNode.Tick();
            runningNode = currentNodeState == BTNodeState.RUNNING ? runningNode : null;
            return currentNodeState;
        }
        else
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
                        runningNode = node;
                        currentNodeState = BTNodeState.RUNNING;
                        return currentNodeState;
                    default:
                        continue;
                }
            }
            currentNodeState = BTNodeState.FAILURE;
            return currentNodeState;
        }
    }
}
