using System;
using UnityEngine;

/// <summary>
/// A target objective for a pattern arena type console.
/// Extends the functionality of ConsoleTargetObjective.
/// </summary>
[Serializable]
public class PatternConsoleTargetObjective : ConsoleTargetObjective
{
    private PatternConsole console = null;

    /// <summary>
    /// Getter for the pattern console object.
    /// </summary>
    public PatternConsole Console => console;

    /// <summary>
    /// Constructor for PatternConsoleTargetObjective.
    /// A target objective for a pattern arena type console.
    /// Extends the functionality of ConsoleTargetObjective.
    /// </summary>
    /// <param name="console">The pattern console object.</param>
    /// <param name="target">Object you want to target.</param>
    /// <param name="consoleType">Type of console target.</param>
    public PatternConsoleTargetObjective(PatternConsole console, GameObject target, ConsoleType consoleType) : 
        base(target, consoleType)
    {
        this.console = console;
    }

    /// <summary>
    /// Constructor for PatternConsoleTargetObjective.
    /// A target objective for a pattern arena type console.
    /// Extends the functionality of ConsoleTargetObjective.
    /// </summary>
    /// <param name="console">The pattern console object.</param>
    /// <param name="target">Object you want to target.</param>
    /// <param name="consoleType">Type of console target.</param>
    /// <param name="knowsLocation">Marks whether AI PC players know the location of the target.</param>
    public PatternConsoleTargetObjective(PatternConsole console, GameObject target, ConsoleType consoleType, bool knowsLocation) :
        base(target, consoleType, knowsLocation) 
    {
        this.console = console;
    }
}
