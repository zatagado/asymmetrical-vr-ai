using UnityEngine;

/// <summary>
/// Smoothly rotates the AI player model towards its movement direction.
/// </summary>
public class RotateForwardAction : BTNode
{
    private AIController ai = null;
    private float maxRotSpeed = 200f; // TODO make dynamic

    /// <summary>
    /// Constructor for RotateForwardAction node.
    /// Smoothly rotates the AI player model towards its movement direction.
    /// </summary>
    /// <param name="aiController">The aiController object for the AI.</param>
    public RotateForwardAction(AIController aiController)
    {
        ai = aiController;
    }

    /// <summary>
    /// Runs when the behavior tree reaches this node.
    /// Rotates the AI player model.
    /// </summary>
    /// <returns>A BTNodeState representing if the behavior was successful, running, or a failure.</returns>
    public override BTNodeState Tick()
    {
        if (ai.PathCalc.vectorPath != null) // check if AI is moving to a postion
        {
            // Gradually rotate towards movement direction.
            float angleBetween = Vector3.SignedAngle(ai.PlayerTransform.forward, 
                (ai.PathCalc.vectorPath[1] - ai.PathCalc.vectorPath[0]).normalized, Vector3.up);

            if (angleBetween > maxRotSpeed * Time.deltaTime)
            {
                ai.LookX = maxRotSpeed;
            }
            else if (angleBetween < -maxRotSpeed * Time.deltaTime)
            {
                ai.LookX = -maxRotSpeed;
            }
            else // the angle between is less than the angularspeed * distance * deltatime. The player angle will be the target angle in the next frame
            {
                ai.LookX = angleBetween / Time.deltaTime;
            }
        }
        else
        {
            // Immediately rotate to previous movement direction
            float angleBetween = Vector3.SignedAngle(ai.PlayerTransform.forward, ai.MovementDir, Vector3.up);
            ai.LookX = angleBetween / Time.deltaTime;
        }
        
        return BTNodeState.SUCCESS;
    }
}
