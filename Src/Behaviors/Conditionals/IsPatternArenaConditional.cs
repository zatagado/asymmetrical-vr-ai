/// <summary>
/// Checks if the current arena is the pattern arena type.
/// </summary>
public class IsPatternArenaConditional : BTNode
{
    private AIController aiController = null;

    /// <summary>
    /// Constructor for IsPatternArenaConditional node.
    /// Checks if the current arena is the pattern arena type.
    /// </summary>
    /// <param name="aiController">The aiController object for the AI.</param>
    public IsPatternArenaConditional(AIController aiController)
    {
        this.aiController = aiController;
    }

    /// <summary>
    /// Runs when the behavior tree reaches this node.
    /// Checks for pattern arena.
    /// </summary>
    /// <returns>Success if the current arena is pattern type, otherwise failure.</returns>
    public override BTNodeState Tick()
    {
        aiController.treeNodes += "IsPatternArenaConditional\n";
        return GameController.Arenas[GameController.ArenaNum] is PatternArena ? BTNodeState.SUCCESS : BTNodeState.FAILURE;
    }
}