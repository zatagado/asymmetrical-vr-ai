using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AI players with assigned colors red, green, and blue move towards a console and press the button.
/// </summary>
public class TargetBarrierConsolePressAction : BTNode
{
    private AIController ai = null;

    private ConsoleTargetObjective[] consoleTargets = null;
    private ConsoleTargetObjective currConsoleTarget = null;

    private bool barrierActive = true;

    private float actionProbability = 0f;

    /// <summary>
    /// Constructor for TargetBarrierConsolePressAction node.
    /// AI players with assigned colors red, green, and blue move towards a console and press the button.
    /// </summary>
    /// <param name="aiController">The aiController object for the AI.</param>
    /// <param name="actionProbability">The probability of the AI to choose to perform this action each tick.</param>
    public TargetBarrierConsolePressAction(AIController aiController, float actionProbability)
    {
        ai = aiController;
        this.actionProbability = actionProbability;
    }

    /// <summary>
    /// Runs when the behavior tree reaches this node.
    /// Chooses a random barrier console of the same game color as the AI as the target to move to.
    /// Only chooses to target a barrier console based on the actionProbability float even if there are consoles available.
    /// </summary>
    /// <returns>Success if a barrier console is the current target or chosen as a target, failure otherwise.</returns>
    public override BTNodeState Tick()
    {
        ai.treeNodes += "TargetBarrierConsolePressAction\n";
        if (currConsoleTarget != null) // Target has already been decided.
        {
            if (barrierActive)
            {
                ai.Target = currConsoleTarget;
                return BTNodeState.SUCCESS;
            }
            else // if barrier is inactive
            {
                currConsoleTarget = null;
                return BTNodeState.FAILURE;
            }
        }
        else // Deciding to choose a target or not
        {
            if (barrierActive && consoleTargets != null)
            {
                float random = Random.Range(0f, 1f);
                if (random < actionProbability) // only perform action if random is less than actionProbability
                {
                    ConsoleTargetObjective[] knownTargets = new ConsoleTargetObjective[consoleTargets.Length];
                    int knownTargetsSize = 0;
                    for (int i = 0; i < consoleTargets.Length; i++)
                    {
                        if (consoleTargets[i].GetKnowsLocation(ai.ID))
                        {
                            knownTargets[knownTargetsSize++] = consoleTargets[i];
                        }
                    }

                    if (knownTargetsSize > 0)
                    {
                        currConsoleTarget = knownTargets[Random.Range(0, knownTargetsSize)]; // Eventually change to closest has highest chance
                        ai.Target = currConsoleTarget;
                        return BTNodeState.SUCCESS;
                    }
                }
            }
            return BTNodeState.FAILURE;
        }
    }

    /// <summary>
    /// Adds ConsoleTargetObjectives as options to be targeted by the AI.
    /// Function is run by an event from BarrierArena.
    /// </summary>
    /// <param name="consoleTargets">A list of console targets with the same colors.</param>
    public void AddConsoleTargets(List<ConsoleTargetObjective> consoleTargets)
    {
        this.consoleTargets = new ConsoleTargetObjective[consoleTargets.Count];
        consoleTargets.CopyTo(this.consoleTargets);
    }

    /// <summary>
    /// Resets the list of ConsoleTargetObjectives so the AI cannot target any consoles.
    /// Function is run by several events from BarrierArena.
    /// </summary>
    public void ResetConsoleTargets()
    {
        if (ai.Target != null && ai.Target.Equals(currConsoleTarget))
        {
            ai.Target = null;
        }

        consoleTargets = null;
        currConsoleTarget = null;
    }

    /// <summary>
    /// Lets the AI know when the barrier of the same color as the AI changes state (either from active to inactive, or 
    /// inactive to active).
    /// </summary>
    /// <param name="barrierActive">Boolean for the current state of the barrier.</param>
    public void BarrierStateChanged(bool barrierActive)
    {
        this.barrierActive = barrierActive;
    }
}
