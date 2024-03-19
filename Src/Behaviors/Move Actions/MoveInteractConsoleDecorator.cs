/// <summary>
/// Moves to the target console and presses the button.
/// Extends the functionality of the MoveToTargetAction.
/// </summary>
public class MoveInteractConsoleDecorator : MoveToTargetAction
{
    /// <summary>
    /// Constructor for the MoveInteractConsoleDecorator node.
    /// Moves to the target console and presses the button.
    /// Extends the functionality of the MoveToTargetAction.
    /// </summary>
    /// <param name="aiController">The aiController object for the AI.</param>
    /// <param name="stoppingDistance">The distance from the target position that the AI is allowed to stop at.</param>
    public MoveInteractConsoleDecorator(AIController aiController, float stoppingDistance) : 
        base(aiController, stoppingDistance) { }

    /// <summary>
    /// Runs when the behavior tree reaches this node.
    /// Moves to the target position and inputs for button interaction when it reaches the target.
    /// </summary>
    /// <returns>Success when the AI reaches the target position, running otherwise.</returns>
    public override BTNodeState Tick()
    {
        ai.treeNodes += "MoveInteractConsoleDecorator\n";
        switch (base.Tick())
        {
            case BTNodeState.SUCCESS:
                ai.InteractInput = true;
                return BTNodeState.SUCCESS;
            case BTNodeState.FAILURE:
                return BTNodeState.FAILURE;
            case BTNodeState.RUNNING:
                return BTNodeState.RUNNING;
            default:
                return BTNodeState.FAILURE;
        }
    }
}
