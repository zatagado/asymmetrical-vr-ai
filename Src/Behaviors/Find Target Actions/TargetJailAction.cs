using UnityEngine;

/// <summary>
/// AI player moves to free other PC players from the jail they are locked in.
/// </summary>
public class TargetJailAction : BTNode
{
    // how to get positon of jail
    // how to tell if there are players inside that jail
    // if one player in jail is the special player
    private AIController ai = null;

    private TargetObjective[] jailTargets = null;
    private TargetObjective currJailTarget = null;

    // as the player gets closer to the target, make them less likely to go towards the jail
    // for future reference, this probability is flawed because it will be lower when the framerate is lower
    private readonly float consoleProbability = 0.001f; // at this probability about 60% chance of going in the first 10 seconds
    private readonly float relicProbability = 0.005f; // at this probability about 74% chance of going in the first 3 seconds
    private readonly float doorProbability = 0.005f; // at this probability about 74% chance of going in the first 3 seconds
    private readonly float baseProbability = 0.002f;

    private readonly float jailButtonProbMulti = 0.5f;

    private float currJailButtonProbMulti = 0f;

    /// <summary>
    /// Constructor for TargetJailAction node.
    /// AI player moves to free other PC players from the jail they are locked in.
    /// </summary>
    /// <param name="aiController">The aiController object for the AI.</param>
    public TargetJailAction(AIController aiController)
    {
        ai = aiController;
    }

    /// <summary>
    /// Runs when the behavior tree reaches this node.
    /// Chooses a random jail button as the target to move to. Only chooses to target a jail if another PC player is stuck in a 
    /// jail. It doesn't matter which jail button is targeted because it will free both jails.
    /// </summary>
    /// <returns>Success if a jail is the current target or chosen as a target, failure otherwise.</returns>
    public override BTNodeState Tick()
    {
        ai.treeNodes += "TargetJailAction\n";
        if (currJailTarget != null) // Target has already been decided.
        {
            ai.Target = currJailTarget;
            return BTNodeState.SUCCESS;
        }
        else // Deciding to choose a target or not
        {
            if (jailTargets != null && ai.PathCalc.vectorPath != null)
            {
                // if far away from target then dont go
                float distance = Vector3.Distance(ai.PathCalc.vectorPath[ai.PathCalc.vectorPath.Count - 1], 
                    ai.AIPathfinder.GetFeetPosition());
                float finalProbability;
                switch (ai.PathCalc.target.ObjectiveType) // different probability of choosing a jail target based on the current target objective
                {
                    case ObjectiveType.Button:
                        finalProbability = Mathf.Min(Mathf.Pow(1.1f, distance - 30), 1) * consoleProbability * 
                            currJailButtonProbMulti; // at distance 30 it is max probability
                        break;
                    case ObjectiveType.Relic:
                        finalProbability = Mathf.Min(Mathf.Pow(2f, distance - 15), 1) * relicProbability * 
                            currJailButtonProbMulti; // at distance 15 it is max probability
                        break;
                    case ObjectiveType.Door:
                        finalProbability = Mathf.Min(Mathf.Pow(2f, distance - 15), 1) * doorProbability * 
                            currJailButtonProbMulti; // at distance 15 it is max probability
                        break;
                    default:
                        finalProbability = baseProbability * currJailButtonProbMulti;
                        break;
                }
                if (Random.Range(0f, 1f) < finalProbability)
                {
                    currJailTarget = jailTargets[Random.Range(0, jailTargets.Length)]; // Eventually change to closest has highest chance
                    ai.Target = currJailTarget;
                    return BTNodeState.SUCCESS;
                }
            }
            return BTNodeState.FAILURE;
        }
    }

    /// <summary>
    /// Adds jail TargetObjectives as options to be targeted by the AI.
    /// Function is run by an event from BarrierArena.
    /// </summary>
    /// <param name="jailedPlayer">GameObject of the player that is in jail.</param>
    /// <param name="jailTargets">A list of jail targets.</param>
    public void AddJailTargets(GameObject jailedPlayer, TargetObjective[] jailTargets)
    {
        this.jailTargets = jailTargets;
    }

    /// <summary>
    /// Resets the list of TargetObjectives so the AI cannot target any jails.
    /// Function is run by several events from BarrierArena.
    /// </summary>
    public void ResetJailTargets() 
    {
        if (ai.Target != null && ai.Target.Equals(currJailTarget))
        {
            ai.Target = null;
        }

        jailTargets = null;
        currJailTarget = null;
    }

    /// <summary>
    /// Lets the AI know when the jail button has changed state. If the jail was recently freed it will be inactive.
    /// </summary>
    /// <param name="isActive">Boolean for the current state of the jail button.</param>
    public void ChangeJailButtonState(bool isActive)
    {
        currJailButtonProbMulti = isActive ? 1f : jailButtonProbMulti;
    }
}