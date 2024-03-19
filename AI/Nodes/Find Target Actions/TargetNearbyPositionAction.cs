using System.Collections;
using UnityEngine;

/// <summary>
/// The player with the white game color moves to positions nearby a console after choosing to go to it (see 
/// TargetBarrierConsoleObserveAction) and waits for the PC player with the same color as the console to approach it.
/// </summary>
public class TargetNearbyPositionAction : BTNode
{
    private AIController ai = null;
    private TargetObjective nearbyTarget = null;
    private ConsoleTargetObjective consoleTarget = null;
    private Coroutine findingNewTarget = null;

    /// <summary>
    /// Constructor for TargetNearbyPositionAction node.
    /// The player with the white game color moves to positions nearby a console after choosing to go to it (see 
    /// TargetBarrierConsoleObserveAction) and waits for the PC player with the same color as the console to approach it.
    /// </summary>
    /// <param name="aiController">The aiController object for the AI.</param>
    public TargetNearbyPositionAction(AIController aiController)
    {
        ai = aiController;
    }

    /// <summary>
    /// Runs when the behavior tree reaches this node.
    /// Moves to a position nearby the console chosen in TargetBarrierConsoleObserveAction.
    /// </summary>
    /// <returns>Success if a position near a console is the current target, failure otherwise.</returns>
    public override BTNodeState Tick()
    {
        ai.treeNodes += "TargetNearbyPositionAction\n";
        if (nearbyTarget != null)
        {
            ai.Target = nearbyTarget;
            return BTNodeState.SUCCESS;
        }
        else
        {
            return BTNodeState.FAILURE;
        }
    }

    /// <summary>
    /// Preparation for finding a target near the previously targeted console to target.
    /// Function is run by the an event marking when a target is reached.
    /// </summary>
    public void StartFindingTarget()
    {
        consoleTarget = (ConsoleTargetObjective)ai.Target;
        nearbyTarget = ai.Target; // the target is the console position
        ai.OnTargetChanged += StopFindingTarget;
    }

    /// <summary>
    /// Finds a target near the previously targeted console to target.
    /// Function is run by the some events marking when a target is reached.
    /// This function will repeat until the other PC player approaches the nearby console.
    /// </summary>
    public void FindNewTarget()
    {
        if (findingNewTarget != null)
        {
            ai.StopCoroutine(findingNewTarget);
        }
        findingNewTarget = ai.StartCoroutine(FindNewTargetCoroutine());
    }

    /// <summary>
    /// Finds a target close to the button so the special player is always moving.
    /// When the special player reaches the button, MoveToTargetAction calls OnReachedTarget.
    /// This function listens to OnReachedTarget.
    /// </summary>
    /// <returns>Coroutine for finding a target position asynchronously.</returns>
    public IEnumerator FindNewTargetCoroutine() // async method better?
    {
        float height = 2;
        float maxRadius = 5;
        float minRadius = 2;
        
        yield return new WaitForSeconds(Random.Range(0.5f, 2f));

        // measure calculation distance
        Vector2 randomXZ = Random.insideUnitCircle.normalized * Random.Range(minRadius, maxRadius); // random direction times random magnitude
        float randomY = Random.Range(0, height);
        Vector3 targetPosition = new Vector3(randomXZ.x, randomY - (height / 2), randomXZ.y) + consoleTarget.Position;

        nearbyTarget = new TargetObjective(targetPosition, ObjectiveType.RandomPosition);
        ai.Target = nearbyTarget;
        findingNewTarget = null;
    }

    /// <summary>
    /// Stops finding targets. This function should subscribe to the AIController OnTargetChanged.
    /// If a different target than the nearbyTarget is found then unsubscribe this function.
    /// </summary>
    public void StopFindingTarget()
    {
        if (ai.Target != nearbyTarget)
        {
            if (findingNewTarget != null)
            {
                ai.StopCoroutine(findingNewTarget);
                findingNewTarget = null;
            }
            nearbyTarget = null;
            ai.OnTargetChanged -= StopFindingTarget;
        }
    }

    /// <summary>
    /// Handles when the console target colors swap. The ai.Target will not change from nearbyTarget so StopFindingTarget 
    /// cannot handle this.
    /// </summary>
    public void ResetConsoleTargets()
    {
        if (findingNewTarget != null)
        {
            ai.StopCoroutine(findingNewTarget);
            findingNewTarget = null;
        }
        nearbyTarget = null;
        ai.OnTargetChanged -= StopFindingTarget;
    }
}
