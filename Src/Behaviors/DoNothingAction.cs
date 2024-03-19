/// <summary>
/// The node for the AI behavior of sitting still. Used when waiting or as a fallback behavior.
/// </summary>
public class DoNothingAction : BTNode
{
    private AIController aiController = null;

    /// <summary>
    /// Constructor for DoNothingAction node.
    /// The node for the AI behavior of sitting still. Used when waiting or as a fallback behavior.
    /// </summary>
    /// <param name="aiController">The aiController object for the AI.</param>
    public DoNothingAction(AIController aiController)
    {
        this.aiController = aiController;
    }

    /// <summary>
    /// Runs when the behavior tree reaches this node.
    /// Prevents AI from moving or rotating.
    /// </summary>
    /// <returns>A BTNodeState representing if the behavior was successful, running, or a failure.</returns>
    public override BTNodeState Tick()
    {
        aiController.treeNodes += "DoNothingAction\n";
        aiController.InteractInput = false;
        aiController.VerticalInput = 0.0f;
        aiController.HorizontalInput = 0.0f;
        aiController.LookX = 0.0f;
        aiController.LookY = 0.0f;
        // aiController.Target = null;
        return BTNodeState.SUCCESS;
    }
}
