/// <summary>
/// Moves to the target console and tells the other PC players what color it is and where it is.
/// Extends the functionality of the MoveToTargetAction.
/// </summary>
public class MoveObserveConsoleDecorator : MoveToTargetAction
{
    /// <summary>
    /// Constructor for the MoveObserveConsoleDecorator node.
    /// Moves to the target console and tells the other PC players what color it is and where it is.
    /// Extends the functionality of the MoveToTargetAction.
    /// </summary>
    /// <param name="ai">The aiController object for the AI.</param>
    /// <param name="stoppingDistance">The distance from the target position that the AI is allowed to stop at.</param>
    public MoveObserveConsoleDecorator(AIController ai, float stoppingDistance) : 
        base(ai, stoppingDistance) {}

    /// <summary>
    /// Runs when the behavior tree reaches this node.
    /// Moves to the target position.
    /// </summary>
    /// <returns>Success when the AI reaches the target position, running otherwise.</returns>
    public override BTNodeState Tick()
    {
        ai.treeNodes += "MoveObserveConsoleDecorator\n";
        return base.Tick();
    }

    /// <summary>
    /// Tells the human PC players using the text chat where the console that the AI player is at currently is.
    /// Function listens for OnReachedTarget event inherited from MoveToTargetAction.
    /// </summary>
    public void ChatLocation()
    {
        GameController.HeadsUpDisplay.TextChat.ChatClient.PublishMessageWithName(
                "MainChannel", ai.gameObject.name, $"I'm right next to the {ai.Target}");
    }

    /// <summary>
    /// Tells all other AI (internally via code) where the console that the current AI targeted is. This is how AI PC players 
    /// know where the console is.
    /// Function listens for OnReachedTarget event inherited from MoveToTargetAction.
    /// </summary>
    public void SetKnowsLocation()
    {
        if (ai.Target != null && ai.Target.ObjectiveType == ObjectiveType.Button)
        {
            ai.Target.SetKnowsLocation(true);
        }
    }
}
