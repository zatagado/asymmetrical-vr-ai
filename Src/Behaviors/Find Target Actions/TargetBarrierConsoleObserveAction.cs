using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The player with the white game color moves near a console to observe its color so it can point the color out to 
/// other players.
/// </summary>
public class TargetBarrierConsoleObserveAction : BTNode
{
    private AIController ai = null;

    private ConsoleTargetObjective[] redConsoleTargets = null;
    private ConsoleTargetObjective[] greenConsoleTargets = null;
    private ConsoleTargetObjective[] blueConsoleTargets = null;
    private ConsoleTargetObjective currConsoleTarget = null;

    private bool redBarrierActive = true;
    private bool greenBarrierActive = true;
    private bool blueBarrierActive = true;

    /// <summary>
    /// Constructor for TargetBarrierConsoleObserveAction node.
    /// The AI player with the white game color moves near a console to observe its color so it can point the color out to 
    /// other players.
    /// </summary>
    /// <param name="aiController">The aiController object for the AI.</param>
    public TargetBarrierConsoleObserveAction(AIController aiController) 
    {
        this.ai = aiController;
    }

    /// <summary>
    /// Runs when the behavior tree reaches this node.
    /// Chooses a random barrier console as the target to move to. Will only choose consoles with colors that still need to 
    /// be pressed (the barrier force field of that color is still active).
    /// </summary>
    /// <returns>Success if a barrier console is the current target or chosen as a target, failure otherwise.</returns>
    public override BTNodeState Tick()
    {
        ai.treeNodes += "TargetBarrierConsoleObserveAction\n";
        if (currConsoleTarget != null) // Target has already been decided.
        {
            ai.Target = currConsoleTarget;
            
            return BTNodeState.SUCCESS;
        }
        else // Deciding to choose a target or not
        {
            // Choosing a randomized color out of the availible colors
            GameColor[] consolesFree = GetConsoleOptions(
                redBarrierActive, greenBarrierActive, blueBarrierActive, out int consoleOptionsLength);

            if (consoleOptionsLength > 0) // choose a console to move to if there are any available
            {
                int consoleColor = Random.Range(0, consoleOptionsLength);
                // TODO update to target closest / direction facing console instead

                switch (consolesFree[consoleColor]) 
                {
                    case GameColor.Red:
                        currConsoleTarget = redConsoleTargets[Random.Range(0, redConsoleTargets.Length)];
                        break;
                    case GameColor.Green:
                        currConsoleTarget = greenConsoleTargets[Random.Range(0, greenConsoleTargets.Length)];
                        break;
                    case GameColor.Blue:
                        currConsoleTarget = blueConsoleTargets[Random.Range(0, blueConsoleTargets.Length)];
                        break;
                }
                currConsoleTarget.OnPlayerApproaching += PlayerApproachingConsole;
                ai.Target = currConsoleTarget;
                return BTNodeState.SUCCESS; 
            }
            else
            {
                return BTNodeState.FAILURE;
            }
        }
    }

    /// <summary>
    /// Gets an array of console colors for the AI to choose from.
    /// </summary>
    /// <param name="targetRedConsoles">Boolean for if red consoles need to be pressed.</param>
    /// <param name="targetGreenConsoles">Boolean for if green consoles need to be pressed.</param>
    /// <param name="targetBlueConsoles">Boolean for if blue consoles need to be pressed.</param>
    /// <param name="consoleOptionsLength">Output of the number of elements in the returned array. The array may be larger 
    /// than the number of elements it contains.</param>
    /// <returns>An array of different consoles colors available to be pressed.</returns>
    private GameColor[] GetConsoleOptions(bool targetRedConsoles, bool targetGreenConsoles, bool targetBlueConsoles, out int consoleOptionsLength)
    {
        GameColor[] consoleOptions = new GameColor[3];
        consoleOptionsLength = 0;
        if (targetRedConsoles)
        {
            consoleOptions[consoleOptionsLength++] = GameColor.Red;
        }
        if (targetGreenConsoles)
        {
            consoleOptions[consoleOptionsLength++] = GameColor.Green;
        }
        if (targetBlueConsoles)
        {
            consoleOptions[consoleOptionsLength++] = GameColor.Blue;
        }
        return consoleOptions;
    }

    /// <summary>
    /// Adds ConsoleTargetObjectives as options to be targeted by the AI.
    /// Function is run by an event from BarrierArena.
    /// </summary>
    /// <param name="consoleTargets">A list of console targets with the same colors.</param>
    public void AddConsoleTargets(List<ConsoleTargetObjective> consoleTargets)
    {
        // For debugging purposes, throw out colors that are not represented
        switch (consoleTargets[0].ConsoleType)
        {
            case ConsoleType.Red:
                redConsoleTargets = new ConsoleTargetObjective[consoleTargets.Count];
                consoleTargets.CopyTo(redConsoleTargets);
                break;
            case ConsoleType.Green:
                greenConsoleTargets = new ConsoleTargetObjective[consoleTargets.Count];
                consoleTargets.CopyTo(greenConsoleTargets);
                break;
            case ConsoleType.Blue:
                blueConsoleTargets = new ConsoleTargetObjective[consoleTargets.Count];
                consoleTargets.CopyTo(blueConsoleTargets);
                break;
        }
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

        redConsoleTargets = null;
        greenConsoleTargets = null;
        blueConsoleTargets = null;
        currConsoleTarget = null;
    }

    /// <summary>
    /// Lets the AI know when the red barrier changes state (either from active to inactive, or inactive to active).
    /// This allows the AI to stop targeting red consoles when the red barrier changes to inactive.
    /// </summary>
    /// <param name="barrierActive">Boolean for the current state of the red barrier.</param>
    public void RedBarrierStateChanged(bool barrierActive)
    {
        redBarrierActive = barrierActive;
        if (!barrierActive && currConsoleTarget != null && currConsoleTarget.ConsoleType == ConsoleType.Red)
        {
            currConsoleTarget.OnPlayerApproaching = null;
            currConsoleTarget = null;
            ai.Target = null;
        }
    }

    /// <summary>
    /// Lets the AI know when the green barrier changes state (either from active to inactive, or inactive to active).
    /// This allows the AI to stop targeting green consoles when the green barrier changes to inactive.
    /// </summary>
    /// <param name="barrierActive">Boolean for the current state of the green barrier.</param>
    public void GreenBarrierStateChanged(bool barrierActive)
    {
        greenBarrierActive = barrierActive;
        if (!barrierActive && currConsoleTarget != null && currConsoleTarget.ConsoleType == ConsoleType.Green)
        {
            currConsoleTarget.OnPlayerApproaching = null;
            currConsoleTarget = null;
            ai.Target = null;
        }
    }

    /// <summary>
    /// Lets the AI know when the blue barrier changes state (either from active to inactive, or inactive to active).
    /// This allows the AI to stop targeting blue consoles when the blue barrier changes to inactive.
    /// </summary>
    /// <param name="barrierActive">Boolean for the current state of the blue barrier.</param>
    public void BlueBarrierStateChanged(bool barrierActive)
    {
        blueBarrierActive = barrierActive;
        if (!barrierActive && currConsoleTarget != null && currConsoleTarget.ConsoleType == ConsoleType.Blue)
        {
            currConsoleTarget.OnPlayerApproaching = null;
            currConsoleTarget = null;
            ai.Target = null;
        }
    }

    /// <summary>
    /// Tells the AI (who is responsible for pointing out colors to other PC players) that a PC player is approaching the 
    /// console that the AI is currently targeting. The AI will then choose a new console with a different color. This 
    /// function is triggered by an event in ApproachingConsole that is triggered when a PC player of the same color of the 
    /// console is approaching it.
    /// </summary>
    /// <param name="index">ID of the PC player that is approaching the console.</param>
    /// <param name="consoleTarget">ConsoleTargetObjective unique to the console being approached.</param>
    public void PlayerApproachingConsole(int index, ConsoleTargetObjective consoleTarget)
    {
        if (index != ai.ID && consoleTarget == currConsoleTarget)
        {
            GameColor[] consoleOptions = null;
            int consoleOptionsLength = 0;
            switch (currConsoleTarget.ConsoleType)
            {
                case ConsoleType.Red:
                    consoleOptions = GetConsoleOptions(
                    false, greenBarrierActive, blueBarrierActive, out consoleOptionsLength);
                    break;
                case ConsoleType.Green:
                    consoleOptions = GetConsoleOptions(
                    redBarrierActive, false, blueBarrierActive, out consoleOptionsLength); ;
                    break;
                case ConsoleType.Blue:
                    consoleOptions = GetConsoleOptions(
                    redBarrierActive, greenBarrierActive, false, out consoleOptionsLength);
                    break;
            }

            currConsoleTarget.OnPlayerApproaching = null;
            currConsoleTarget = null;
            ai.Target = null;

            if (consoleOptionsLength > 0)
            {
                int consoleColor = Random.Range(0, consoleOptionsLength);
                // TODO update to target closest / direction facing console instead

                switch (consoleOptions[consoleColor]) 
                {
                    case GameColor.Red:
                        currConsoleTarget = redConsoleTargets[Random.Range(0, redConsoleTargets.Length)];
                        break;
                    case GameColor.Green:
                        currConsoleTarget = greenConsoleTargets[Random.Range(0, greenConsoleTargets.Length)];
                        break;
                    case GameColor.Blue:
                        currConsoleTarget = blueConsoleTargets[Random.Range(0, blueConsoleTargets.Length)];
                        break;
                }
                currConsoleTarget.OnPlayerApproaching += PlayerApproachingConsole;
                ai.Target = currConsoleTarget;
            }
        }
    }
}
