/// <summary>
/// Root node of the behavior tree. More for readability to easily find the root of a behavior tree.
/// </summary>
public class BTRootNode : BTNode
{
    private BTNode startNode = null;

    /// <summary>
    /// Constructor for BTRootNode.
    /// Root node of the behavior tree. More for readability to easily find the root of a behavior tree.
    /// </summary>
    /// <param name="startNode">The first behavior node in the behavior tree.</param>
    public BTRootNode(BTNode startNode)
    {
        this.startNode = startNode;
    }

    /// <summary>
    /// Runs when the behavior tree reaches this node.
    /// </summary>
    /// <returns>A BTNodeState representing if the behavior was successful, running, or a failure.</returns>
    public override BTNodeState Tick()
    {
        return startNode.Tick();
    }
}
