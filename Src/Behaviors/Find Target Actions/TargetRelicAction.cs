using UnityEngine;

/// <summary>
/// AI targets the relic.
/// </summary>
public class TargetRelicAction : BTNode
{
    private AIController ai = null;

    private TargetObjective relicTarget = null;
    private TargetObjective currRelicTarget = null;

    private float actionProbability = 0;

    /// <summary>
    /// Constructor for TargetRelicAction node.
    /// AI targets the relic.
    /// </summary>
    /// <param name="aiController">The aiController object for the AI.</param>
    /// /// <param name="actionProbability">The probability of the AI to choose to perform this action each tick.</param>
    public TargetRelicAction(AIController aiController, float actionProbability)
    {
        ai = aiController;
        this.actionProbability = actionProbability;
    }

    /// <summary>
    /// Runs when the behavior tree reaches this node.
    /// Chooses to move to the relic.
    /// Only chooses to target the relic based on the actionProbability float even if there are consoles available.
    /// </summary>
    /// <returns>Success if the relic is the current target or chosen as a target, failure otherwise.</returns>
    public override BTNodeState Tick()
    {
        ai.treeNodes += "TargetRelicAction\n";
        if (currRelicTarget != null)
        {
            ai.currentAction = "Targeting Relic";
            ai.Target = currRelicTarget;
            return BTNodeState.SUCCESS;
        }
        else
        {
            if (relicTarget != null)
            {
                float random = Random.Range(0f, 1f);
                if (random < actionProbability)
                {
                    currRelicTarget = relicTarget;
                    ai.Target = currRelicTarget;
                    return BTNodeState.SUCCESS;
                }
            }
            return BTNodeState.FAILURE;
        }
    }

    /// <summary>
    /// Adds the relic as an option to be targeted by the AI/
    /// </summary>
    /// <param name="relicTarget">The relic TargetObjective.</param>
    public void AddRelicTarget(TargetObjective relicTarget)
    {
        this.relicTarget = relicTarget;
    }

    /// <summary>
    /// Resets the relic target objective.
    /// </summary>
    public void ResetRelicTarget()
    {
        if (ai.Target != null && ai.Target.Equals(currRelicTarget))
        {
            ai.Target = null;
        }

        relicTarget = null;
        currRelicTarget = null;
    }
}
