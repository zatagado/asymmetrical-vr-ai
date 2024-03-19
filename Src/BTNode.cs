/// <summary>
/// The base node type for a behavior tree. All behavior tree nodes extend this abstract class.
/// </summary>
public abstract class BTNode
{
    protected BTNodeState currentNodeState = BTNodeState.FAILURE;
    /// <summary>
    /// Getter for the node state of this node.
    /// </summary>
    public BTNodeState NodeState => currentNodeState;

    /// <summary>
    /// Constructor for BTNode.
    /// The base node type for a behavior tree. All behavior tree nodes extend this abstract class.
    /// </summary>
    public BTNode() { }

    /// <summary>
    /// Runs when the behavior tree reaches this node.
    /// </summary>
    /// <returns>A BTNodeState representing if the behavior was successful, running, or a failure.</returns>
    public abstract BTNodeState Tick();
}
