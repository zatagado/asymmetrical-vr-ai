using UnityEngine;

/// <summary>
/// Teleports the AI pathfinder transform to the physical player model if they get too out of sync.
/// </summary>
public class SnapAIToModelAction : BTNode
{
    private AIController ai = null;
    private float snapDistance = 0;

    /// <summary>
    /// Constructor for SnapAIToModelAction node.
    /// Teleports the AI pathfinder transform to the physical player model if they get too out of sync.
    /// </summary>
    /// <param name="ai">The aiController object for the AI.</param>
    /// <param name="snapDistance">The max distance out of sync before teleporting the AI pathfinder ot the player model.</param>
    public SnapAIToModelAction(AIController ai, float snapDistance)
    {
        this.ai = ai;
        this.snapDistance = snapDistance;
    }

    /// <summary>
    /// Runs when the behavior tree reaches this node.
    /// Syncs the AI player model with the AI pathfinder.
    /// </summary>
    /// <returns>A BTNodeState representing if the behavior was successful, running, or a failure.</returns>
    public override BTNodeState Tick()
    {
        if (Vector3.Distance(ai.transform.position, ai.AIPathfinder.GetFeetPosition()) > snapDistance)
        {
            ai.AIPathfinder.Teleport(ai.transform.position);
        }
        return BTNodeState.SUCCESS;
    }
}
