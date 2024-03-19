using UnityEngine;
using Pathfinding;
using System;

/// <summary>
/// Moves the AI to the current target objective position.
/// </summary>
public class MoveToTargetAction : BTNode
{
    /// <summary>
    /// A raycast confined to the navigation mesh.
    /// </summary>
    protected struct LinecastObject
    {
        public bool hit;
        public float distance;

        /// <summary>
        /// Constructor for LineCast.
        /// A raycast confined to the navigation mesh.
        /// </summary>
        /// <param name="hit">Boolean for if the linecast hit an edge of the navmesh.</param>
        /// <param name="distance">The maximum distance that the linecast can check for hits.</param>
        public LinecastObject(bool hit, float distance)
        {
            this.hit = hit;
            this.distance = distance;
        }
    }

    protected AIController ai = null;

    protected AstarPath astar = null;
    protected NNConstraint graphConstraint = null;

    protected float stoppingDistance = 0f;
    protected bool firstReachedTarget = false;

    public Action OnReachedTarget = null; // event triggered when the target is reached 

    /// <summary>
    /// Constructor for the MoveToTargetAction node.
    /// Moves the AI to the current target objective position.
    /// </summary>
    /// <param name="aiController">The aiController object for the AI.</param>
    /// <param name="stoppingDistance">The distance from the target position that the AI is allowed to stop at.</param>
    public MoveToTargetAction(AIController aiController, float stoppingDistance)
    {
        ai = aiController;
        astar = AstarPath.active;
        graphConstraint = new NNConstraint() // information about which graph the AI can pathfind on, etc.
        {
            graphMask = ai.AISeeker.graphMask,
            constrainArea = false,
            distanceXZ = false,
            constrainTags = false,
            constrainDistance = false,
        };
        this.stoppingDistance = stoppingDistance;
        firstReachedTarget = false;

        ai.AIPathfinder.maxSpeed = ai.MoveCon.WalkSpeed;
    }

    /// <summary>
    /// Runs when the behavior tree reaches this node.
    /// Moves to the target position.
    /// </summary>
    /// <returns>Success when the AI reaches the target position, running otherwise.</returns>
    public override BTNodeState Tick()
    {
        ai.treeNodes += "MoveToTargetAction\n";
        ai.CalculatePath(); // Where all the important stuff happens

        if (ai.PathCalc.vectorPath != null)
        {
            GraphNode targetNode = astar.GetNearest(ai.PathCalc.target.Position, graphConstraint).node; // Node the target position is closes to
            GraphNode finalPathNode = ai.PathCalc.nodePath[ai.PathCalc.nodePath.Count - 1]; // Nodes that is last in the calculated path
            if (finalPathNode == targetNode) // Pathfinding calculation successful
            {
                Vector3 navPosition = finalPathNode.ClosestPointOnNode(ai.PathCalc.target.Position);
                float navPlayerDistance = Vector3.Distance(navPosition, ai.PlayerTransform.position);
                if (navPlayerDistance <= stoppingDistance) // Stop the player from moving
                {
                    if (!firstReachedTarget) // The first frame the target is reached
                    {
                        Stop();
                        OnReachedTarget?.Invoke(); // trigger event
                        firstReachedTarget = true;
                    }
                    return BTNodeState.SUCCESS;
                }
                else // Continue moving the player
                {
                    Move(ai.PathCalc);
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
    /// Stop the AI from moving or turning.
    /// </summary>
    protected void Stop()
    {
        ai.JumpInput = false;
        ai.LookX = 0.0f;
        ai.VerticalInput = 0.0f;
        ai.HorizontalInput = 0.0f;
    }

    /// <summary>
    /// Makes the player move to a target destination.
    /// </summary>
    /// <param name="pathCalc">The pathfinding calculation the AI is following to the current target.</param>
    protected virtual void Move(PathCalculation pathCalc)
    {
        LinecastObject linecast = Linecast(6);

        if (pathCalc.nodePath.Count > 1 && NodeLink2.GetNodeLink(pathCalc.nodePath[1]) &&
            Vector3.Distance(ai.PathCalc.vectorPath[1], ai.PlayerTransform.position) < 0.3f && 
            ai.MoveCon.Velocity.y < 0)
        {
            Jump(ai.PlayerTransform.position, pathCalc.vectorPath[2]);
        }
        else
        {
            Walk(pathCalc, ai.AIPathfinder.maxSpeed, 200f, linecast);
        }
    }

    /// <summary>
    /// Make the AI jump.
    /// </summary>
    /// <param name="startPos">Starting vector for the jump.</param>
    /// <param name="endPos">Ending vector for the jump.</param>
    protected virtual void Jump(Vector3 startPos, Vector3 endPos)
    {
        ai.JumpInput = true;

        // Calculate input needed to achieve correct end position for jump
        Vector3 jumpDistanceVector = new Vector3(endPos.x, 0, endPos.z) - new Vector3(startPos.x, 0, startPos.z);

        float deltaY = endPos.y - startPos.y;
        float speedY = ai.MoveCon.JumpVelocity;
        float deltaXZ = Vector3.Distance(new Vector3(startPos.x, 0, startPos.z), new Vector3(endPos.x, 0, endPos.z));
        float gravity = -ai.MoveCon.Gravity;

        float airTime = (-speedY - Mathf.Sqrt(Mathf.Pow(speedY, 2) - (4 * (gravity / 2) * -deltaY))) / (2 * (gravity / 2));

        if (float.IsNaN(airTime))
        {
            SetVelocityInput(jumpDistanceVector.normalized * ai.AIPathfinder.maxSpeed);
        }
        else
        {
            SetVelocityInput(jumpDistanceVector.normalized * (deltaXZ / airTime));
        }
    }

    /// <summary>
    /// Calculates input necessary to move and rotate the AI physics body and from the pathfinding AI gameObject.
    /// </summary>
    /// <param name="pathCalc">The current path calculation.</param>
    /// <param name="speed">The desired speed.</param>
    /// <param name="angularSpeed">The desired rotation speed.</param>
    /// <param name="linecast">A linecast in the direction of the movement.</param>
    protected virtual void Walk(PathCalculation pathCalc, float speed, float angularSpeed, LinecastObject linecast) // add case for walking off of navmesh
    {
        // ROTATION SECTION
        Vector3 cornerDist = pathCalc.vectorPath[1] - pathCalc.vectorPath[0];
        cornerDist.y = 0;
        Vector3 nextPathPointDir = cornerDist.normalized; 

        float actualSpeed = speed;
        float actualAngularSpeed = cornerDist.magnitude > 0.1f ? angularSpeed / Mathf.Clamp01(linecast.distance) : angularSpeed;

        // Rotate the agent towards the next corner
        float angleBetween = Vector3.SignedAngle(ai.MovementDir, nextPathPointDir, Vector3.up); 
        float distance = 1 / Mathf.Clamp01(cornerDist.magnitude); // as magnitude gets smaller the multiplier increases, smallest value is one

        if (angleBetween > actualAngularSpeed * distance * Time.deltaTime)
        {
            ai.MovementDir = (Quaternion.Euler(0, actualAngularSpeed * distance * Time.deltaTime, 0) * ai.MovementDir).normalized;
            actualSpeed = speed * Mathf.Clamp01(linecast.distance);
        }
        else if (angleBetween < -actualAngularSpeed * distance * Time.deltaTime)
        {
            ai.MovementDir = (Quaternion.Euler(0, -actualAngularSpeed * distance * Time.deltaTime, 0) * ai.MovementDir).normalized;
            actualSpeed = speed * Mathf.Clamp01(linecast.distance);
        }
        else
        {
            ai.MovementDir = nextPathPointDir.normalized;
        }

        // WALK SECTION
        ai.JumpInput = false;

        // Move the agent towards the next corner
        float heightDiff = ai.PlayerTransform.position.y - ai.AIPathfinder.position.y;

        ai.AIPathfinder.Move((ai.MovementDir + new Vector3(0, heightDiff, 0)) * actualSpeed * Time.deltaTime);

        // Calculate the vertical and horizontal input based on the position of the agent
        SetVelocityInput((ai.AIPathfinder.position - ai.PlayerTransform.position) / Time.deltaTime);
    }

    /// <summary>
    /// Sets the input on the movement controller provided a desired speed and a normalized direction.
    /// </summary>
    /// <param name="velocityVector">The desired velocity as a vector.</param>
    protected void SetVelocityInput(Vector3 velocityVector)
    {
        ai.VerticalInput = Vector3.Dot(velocityVector, ai.PlayerTransform.forward) / ai.AIPathfinder.maxSpeed; // may need to change
        ai.HorizontalInput = Vector3.Dot(velocityVector, ai.PlayerTransform.right) / ai.AIPathfinder.maxSpeed;
    }

    /// <summary>
    /// Wrapper for a linecast on the navigation mesh.
    /// </summary>
    /// <param name="lineLength">The maximum length of the linecast.</param>
    /// <returns>LinecastObject telling if there was a hit and the distance to the hit.</returns>
    protected virtual LinecastObject Linecast(float lineLength) // this method fixes issues with the AI going off the navmesh. probably
    {
        bool hit = ((NavmeshBase)astar.graphs[0]).Linecast(ai.PlayerTransform.position,
            ai.PlayerTransform.position + ai.MovementDir * lineLength, astar.GetNearest(ai.PlayerTransform.position,
            graphConstraint).node, out GraphHitInfo hitInfo);

        return new LinecastObject(hit, hitInfo.distance);
    }
}
