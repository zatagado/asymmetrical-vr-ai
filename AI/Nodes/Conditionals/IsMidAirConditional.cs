/// <summary>
/// Checks if the AI is currently in the air.
/// </summary>
public class IsMidAirConditional : BTNode
{
    private AIController ai = null;

    /// <summary>
    /// Constructor for IsMidAirConditional node.
    /// Checks if the AI is currently in the air.
    /// </summary>
    /// <param name="aiController">The aiController object for the AI.</param>
    public IsMidAirConditional(AIController aiController)
    {
        ai = aiController;
    }

    /// <summary>
    /// Runs when the behavior tree reaches this node.
    /// Checks for if the AI is in the air.
    /// </summary>
    /// <returns>Success if the AI is in the air, otherwise failure.</returns>
    public override BTNodeState Tick()
    {
        return ai.MoveCon.IsGrounded ? BTNodeState.FAILURE : BTNodeState.SUCCESS;
    }
}
