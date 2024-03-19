using Pathfinding;
using UnityEngine;

/// <summary>
/// AI targets a random position to go to.
/// </summary>
public class TargetRandomPositionAction : BTNode
{
    private AIController ai = null;

    private AstarPath astar = null;
    private NNConstraint graphConstraint = null;

    /// <summary>
    /// Constructor for TargetRandomPositionAction node.
    /// AI targets a random position to go to.
    /// </summary>
    /// <param name="aiController">The aiController object for the AI.</param>
    public TargetRandomPositionAction(AIController aiController)
    {
        this.ai = aiController;
        astar = AstarPath.active;
        graphConstraint = new NNConstraint() // information about which graph the AI can pathfind on, etc.
        {
            graphMask = ai.AISeeker.graphMask,
            constrainArea = false,
            distanceXZ = false,
            constrainTags = false,
            constrainDistance = false,
        };
    }

    /// <summary>
    /// Runs when the behavior tree reaches this node.
    /// Finds a random positon to target.
    /// </summary>
    /// <returns>A BTNodeState for success. There is no fail condition.</returns>
    public override BTNodeState Tick()
    {
        ai.treeNodes += "TargetRandomPositionAction\n";
        if (ai.Target != null && ai.Target.ObjectiveType == ObjectiveType.RandomPosition)
        {
            if (ai.PathCalc.vectorPath != null)
            {
                if (ai.PathCalc.target != ai.Target || astar.GetNearest(ai.Target.Position, graphConstraint).node != 
                    ai.PathCalc.nodePath[ai.PathCalc.nodePath.Count - 1])
                {
                    FindRandomTarget();
                }
            }
        }
        else // Deciding to choose a target or not
        {
            FindRandomTarget();
        }
        return BTNodeState.SUCCESS;
    }

    /// <summary>
    /// Function to actually choose a random position as a target.
    /// </summary>
    private void FindRandomTarget()
    {
        // some chance of selecting a random position to move to
        GameController.Arenas[GameController.ArenaNum].GetDimensions(out Vector3 center, out float radius, out float height);

        Vector2 randomXZ = Random.insideUnitCircle * radius;
        float randomY = Random.Range(0, height);
        Vector3 targetPosition = new Vector3(randomXZ.x, randomY - (height / 2), randomXZ.y) + center;

        ai.Target = new TargetObjective(targetPosition, ObjectiveType.RandomPosition);
        ai.Target.OnRequestNewTarget += RequestNewTarget;
    }

    /// <summary>
    /// Resets the current random target.
    /// </summary>
    public void ResetRandomTarget()
    {
        ai.Target = null;
    }

    /// <summary>
    /// Requests a new target position.
    /// </summary>
    /// <param name="oldTarget">The previous target objective.</param>
    public void RequestNewTarget(TargetObjective oldTarget)
    {
        if (ai.Target == oldTarget)
        {
            ResetRandomTarget(); // because AvoidCloseVRAction checks ai.Target position, no need to worry about pathcalc
        }
    }
}
