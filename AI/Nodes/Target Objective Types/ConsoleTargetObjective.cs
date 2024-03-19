using UnityEngine;

/// <summary>
/// Information about the type of console for the ConsoleTargetObjective.
/// </summary>
public enum ConsoleType
{
    White,
    Red,
    Green,
    Blue,
    RedGreen,
    GreenBlue,
    RedBlue
}

/// <summary>
/// A target objective for a console.
/// Extends the functionality of TargetObjective.
/// </summary>
[System.Serializable]
public class ConsoleTargetObjective : TargetObjective
{
    protected ConsoleType consoleType = ConsoleType.White;

    /// <summary>
    /// Setter and getter for the console type.
    /// </summary>
    public ConsoleType ConsoleType { get => consoleType; set => consoleType = value; }

    /// <summary>
    /// Constructor for ConsoleTargetObjective.
    /// A target objective for a console.
    /// Extends the functionality of TargetObjective.
    /// </summary>
    /// <param name="target">Object you want to target.</param>
    /// <param name="consoleType">Type of console target.</param>
    public ConsoleTargetObjective(GameObject target, ConsoleType consoleType) : base(target, ObjectiveType.Button)
    {
        this.consoleType = consoleType;
    }

    /// <summary>
    /// Constructor for ConsoleTargetObjective.
    /// A target objective for a console.
    /// Extends the functionality of TargetObjective.
    /// </summary>
    /// <param name="target">Object you want to target.</param>
    /// <param name="consoleType">Type of console target.</param>
    /// <param name="knowsLocation">Marks whether AI PC players know the location of the target.</param>
    public ConsoleTargetObjective(GameObject target, ConsoleType consoleType, bool knowsLocation) : 
        base(target, ObjectiveType.Button, knowsLocation)
    {
        this.consoleType = consoleType;
    }

    /// <summary>
    /// Gets string representation of the console target objective.
    /// </summary>
    /// <returns>String representation of the console target objective.</returns>
    public override string ToString()
    {
        return consoleType.ToString() + " " + objectiveType.ToString();
    }
}