/// <summary>
/// Checks if the current arena is the barrier arena type.
/// </summary>
public class IsBarrierArenaConditional : BTNode
{
    private AIController aiController = null;

    /// <summary>
    /// Constructor for IsBarrierArenaConditional node.
    /// Checks if the current arena is the barrier arena type.
    /// </summary>
    /// <param name="aiController">The aiController object for the AI.</param>
    public IsBarrierArenaConditional(AIController aiController)
    {
        this.aiController = aiController;
    }

    /// <summary>
    /// Runs when the behavior tree reaches this node.
    /// Checks for barrier arena.
    /// </summary>
    /// <returns>Success if the current arena is barrier type, otherwise failure.</returns>
    public override BTNodeState Tick()
    {
        aiController.treeNodes += "IsBarrierArenaConditional\n";
        return GameController.Arenas[GameController.ArenaNum] is BarrierArena ? BTNodeState.SUCCESS : BTNodeState.FAILURE;
    }
}