using Pathfinding;
using UnityEngine;

/// <summary>
/// Moves to the a target position that avoids the VR player.
/// Extends the functionality of the MoveToTargetAction.
/// </summary>
public class MoveToAvoidTargetDecorator : MoveToTargetAction
{
    /// <summary>
    /// Constructor for the MoveToAvoidTargetDecorator node.
    /// Moves to the a target position that avoids the VR player.
    /// Extends the functionality of the MoveToTargetAction.
    /// </summary>
    /// <param name="ai">The aiController object for the AI.</param>
    /// <param name="stoppingDistance">The distance from the target position that the AI is allowed to stop at.</param>
    public MoveToAvoidTargetDecorator(AIController ai, float stoppingDistance) :
    base(ai, stoppingDistance) { }

    /// <summary>
    /// Runs when the behavior tree reaches this node.
    /// Calculates the path for both the VR player avoidance path and the regular target path. 
    /// Moves to the targeted VR player avoidance path.
    /// </summary>
    /// <returns>Success when the AI reaches the target position, running otherwise.</returns>
    public override BTNodeState Tick()
    {
        ai.treeNodes += "MoveToAvoidTargetDecorator\n";

        ai.CalculateMultiplePaths();

        if (ai.AvoidPathCalc.vectorPath != null)
        {
            GraphNode targetNode = astar.GetNearest(ai.AvoidTarget.Position, graphConstraint).node; // Node the target position is closes to
            GraphNode finalPathNode = ai.AvoidPathCalc.nodePath[ai.AvoidPathCalc.nodePath.Count - 1]; // Nodes that is last in the calculated path
            if (finalPathNode == targetNode) // Pathfinding calculation successful
            {
                Vector3 navPosition = finalPathNode.ClosestPointOnNode(ai.AvoidTarget.Position);
                float navPlayerDistance = Vector3.Distance(navPosition, ai.PlayerTransform.position);
                if (navPlayerDistance <= stoppingDistance) // Stop the player from moving
                {
                    if (!firstReachedTarget) // The first frame the target is reached
                    {
                        Stop();
                        OnReachedTarget?.Invoke();
                        firstReachedTarget = true;
                    }
                    return BTNodeState.SUCCESS;
                }
                else // Continue moving the player
                {
                    Move(ai.AvoidPathCalc);
                    firstReachedTarget = false;
                }
            }
            else // Pathfinding calculation failed, stop the player from moving
            {
                Stop();
                firstReachedTarget = false;
            }
        }
        return BTNodeState.RUNNING;
    }

    /// <summary>
    /// Calculates input necessary to move the AI physics body and from the pathfinding AI gameObject.
    /// </summary>
    /// <param name="pathCalc">The current path calculation.</param>
    /// <param name="speed">The desired speed.</param>
    /// <param name="angularSpeed">The desired rotation speed.</param>
    /// <param name="linecast">A linecast in the direction of the movement.</param>
    protected override void Walk(PathCalculation pathCalc, float speed, float angularSpeed, LinecastObject linecast)
    {
        Vector3 cornerDist = pathCalc.vectorPath[pathCalc.vectorPath.Count - 1] - ai.AIPathfinder.GetFeetPosition(); 
        cornerDist.y = 0;
        Vector3 nextPathPointDir = cornerDist.normalized; // eventually change to constantly update only if the VR player is in sight and close to the player 

        ai.MovementDir = nextPathPointDir.normalized;

        // WALK SECTION
        ai.JumpInput = false;

        // Move the agent towards the next corner
        float heightDiff = ai.PlayerTransform.position.y - ai.AIPathfinder.position.y;

        Vector3 delta = (ai.MovementDir + new Vector3(0, heightDiff, 0)) * speed * Time.deltaTime;
        ai.AIPathfinder.Move(Vector3.Dot(nextPathPointDir, delta) > Vector3.Dot(nextPathPointDir, cornerDist) ? cornerDist : delta);

        // Calculate the vertical and horizontal input based on the position of the agent
        SetVelocityInput((ai.AIPathfinder.position - ai.PlayerTransform.position) / Time.deltaTime);
    }
}