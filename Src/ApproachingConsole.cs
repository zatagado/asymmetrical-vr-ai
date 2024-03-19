using UnityEngine;

/// <summary>
/// Used to tell when a PC player is approaching a console with a button. 
/// </summary>
public class ApproachingConsole : MonoBehaviour
{
    private ConsoleTargetObjective consoleTarget; // Used for AI targeting the console

    /// <summary>
    /// Sets up a trigger collider for the console.
    /// </summary>
    /// <param name="consoleTarget"></param>
    public void Setup(ConsoleTargetObjective consoleTarget)
    {
        this.consoleTarget = consoleTarget;
        SphereCollider collider = gameObject.AddComponent<SphereCollider>();
        collider.isTrigger = true;
        collider.radius = GameController.ConsoleApproachRadius;
    }

    /// <summary>
    /// Triggers the OnPlayerApproaching event on the console target objective if the player color is the same as the 
    /// console color. This is used by the player with the white game color.
    /// </summary>
    /// <param name="other">The collider entering the trigger collider. We only care if it is a PC player.</param>
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PC Player") && consoleTarget.ConsoleType == 
            (ConsoleType)other.GetComponent<PlayerData>().Color)
        {
            //Debug.Log("<color=blue>Player entered approaching radius.</color>");
            consoleTarget.OnPlayerApproaching?.Invoke(other.GetComponent<PlayerData>().ID, consoleTarget);
        }
    }
}
