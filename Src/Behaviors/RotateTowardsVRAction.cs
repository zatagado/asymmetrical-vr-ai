using UnityEngine;

/// <summary>
/// Rotate to face the VR player. Used when the VR player is too close to the AI player.
/// </summary>
public class RotateTowardsVRAction : BTNode
{
    private AIController ai = null;
    private float maxRotSpeed = 200f;

    /// <summary>
    /// Constructor for RotateTowardsVRAction node.
    /// Rotate to face the VR player. Used when the VR player is too close to the AI player.
    /// </summary>
    /// <param name="aiController">The aiController object for the AI.</param>
    public RotateTowardsVRAction(AIController aiController)
    {
        ai = aiController;
    }

    /// <summary>
    /// Runs when the behavior tree reaches this node.
    /// Rotates the AI player model towards the VR player.
    /// </summary>
    /// <returns>A BTNodeState representing if the behavior was successful, running, or a failure.</returns>
    public override BTNodeState Tick()
    {
        float angleBetween = Vector3.SignedAngle(ai.PlayerTransform.forward, 
            new Vector3(ai.VRPlayerPosition.x, 0, ai.VRPlayerPosition.z) - 
            new Vector3(ai.AIPathfinder.GetFeetPosition().x, 0, ai.AIPathfinder.GetFeetPosition().z), Vector3.up);

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

        return BTNodeState.SUCCESS;
    }
}
