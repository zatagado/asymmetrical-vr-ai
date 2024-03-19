/// <summary>
/// Checks if the AI player is currently in jail. 
/// </summary>
public class IsPlayerInJailConditional : BTNode
{
    private AIController aiController = null;
    private PCAbilityController abilityController = null;

    /// <summary>
    /// Constructor for IsPlayerInJailConditional node.
    /// Checks if the AI player is currently in jail.
    /// </summary>
    /// <param name="aiController">The aiController object for the AI.</param>
    /// <param name="abilityController">The abilityController for the PC player.</param>
    public IsPlayerInJailConditional(AIController aiController, PCAbilityController abilityController)
    {
        this.aiController = aiController;
        this.abilityController = abilityController;
    }

    /// <summary>
    /// Runs when the behavior tree reaches this node.
    /// Checks if the AI is in jail.
    /// </summary>
    /// <returns>Success if the AI is in jail, otherwise failure.</returns>
    public override BTNodeState Tick()
    {
        aiController.treeNodes += "IsPlayerNotInJailConditional\n";
        return abilityController.InJail ? BTNodeState.SUCCESS : BTNodeState.FAILURE;
    }
}
