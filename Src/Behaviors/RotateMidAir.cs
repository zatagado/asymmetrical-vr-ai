/// <summary>
/// Rotates the AI player model while in mid air.
/// </summary>
public class RotateMidAir : BTNode
{
    private AIController ai = null;

    /// <summary>
    /// Constructor for RotateMidAir node.
    /// Rotates the AI player model while in mid air.
    /// </summary>
    /// <param name="aiController">The aiController object for the AI.</param>
    public RotateMidAir(AIController aiController)
    {
        ai = aiController;
    }

    /// <summary>
    /// Runs when the behavior tree reaches this node.
    /// Rotates the AI player model and teleports AI pathfinder in air.
    /// </summary>
    /// <returns>A BTNodeState represeting if the behavior was successful, running, or a failure.</returns>
    public override BTNodeState Tick()
    {
        ai.AIPathfinder.Teleport(ai.PlayerTransform.position); // Teleport the AI pathfinder gameobject because it can't navigate off the navmesh but the physics player model can
        // No input
        ai.LookY = 0.0f;
        ai.JumpInput = false;
        ai.LookX = 0.0f;
        ai.VerticalInput = 0.0f;
        ai.HorizontalInput = 0.0f;
        return BTNodeState.SUCCESS;
    }
}
