/// <summary>
/// Checks if the current arena is the lock arena type.
/// </summary>
public class IsLockArenaConditional : BTNode
{
    private AIController aiController = null;

    /// <summary>
    /// Constructor for IsLockArenaConditional node.
    /// Checks if the current arena is the lock arena type.
    /// </summary>
    /// <param name="aiController">The aiController object for the AI.</param>
    public IsLockArenaConditional(AIController aiController)
    {
        this.aiController = aiController;
    }

    /// <summary>
    /// Runs when the behavior tree reaches this node.
    /// Checks for lock arena.
    /// </summary>
    /// <returns>Success if the current arena is lock type, otherwise failure.</returns>
    public override BTNodeState Tick()
    {
        aiController.treeNodes += "IsLockArenaConditional\n";
        return GameController.Arenas[GameController.ArenaNum] is LockArena ? BTNodeState.SUCCESS : BTNodeState.FAILURE;
    }
}