using System;
using UnityEngine;

/// <summary>
/// Information about the type of objective for the TargetObjective.
/// </summary>
public enum ObjectiveType
{
    Button,
    Relic,
    Door,
    Jail,
    AvoidPosition,
    RandomPosition
}

/// <summary>
/// Represents a position that the AI can pathfind to. Usually used to represent arena landmarks.
/// </summary>
[System.Serializable]
public class TargetObjective
{
    protected Vector3 position = Vector3.zero;
    protected ObjectiveType objectiveType = ObjectiveType.RandomPosition;
    [SerializeField] protected bool[] knowsLocation = null;
    [SerializeField] protected float[] timeSinceNotified = null;

    public Action<int, ConsoleTargetObjective> OnPlayerApproaching = null;
    public Action<TargetObjective> OnRequestNewTarget = null;

    /// <summary>
    /// Getter for position of the target objective.
    /// </summary>
    public virtual Vector3 Position => position;
    /// <summary>
    /// Setter and getter for the objective type of the target objective.
    /// </summary>
    public ObjectiveType ObjectiveType { get => objectiveType; set => objectiveType = value; }

    /// <summary>
    /// Constructor for TargetObjective.
    /// Represents a position that the AI can pathfind to. Usually used to represent arena landmarks.
    /// </summary>
    /// <param name="target">Object you want to target.</param>
    /// <param name="objectiveType">Type of target.</param>
    public TargetObjective(GameObject target, ObjectiveType objectiveType)
    {
        if (target)
        {
            position = target.transform.position;
        }
        else
        {
            position = Vector3.zero;
        }
        this.objectiveType = objectiveType;
        knowsLocation = new bool[GameController.NumCPU];
        timeSinceNotified = new float[GameController.NumCPU];
    }

    /// <summary>
    /// Constructor for TargetObjective.
    /// Represents a position that the AI can pathfind to. Usually used to represent arena landmarks.
    /// </summary>
    /// <param name="target">Object you want to target.</param>
    /// <param name="objectiveType">Type of target.</param>
    /// <param name="knowsLocation">Marks whether AI PC players know the location of the target.</param>
    public TargetObjective(GameObject target, ObjectiveType objectiveType, bool knowsLocation)
    {
        if (target)
        {
            position = target.transform.position;
        }
        else
        {
            position = Vector3.zero;
        }
        this.objectiveType = objectiveType;
        this.knowsLocation = new bool[GameController.NumCPU];
        for (int i = 0; i < this.knowsLocation.Length; i++)
        {
            this.knowsLocation[i] = knowsLocation;
        }

        timeSinceNotified = new float[GameController.NumCPU];
    }

    /// <summary>
    /// Constructor for TargetObjective.
    /// Represents a position that the AI can pathfind to. Usually used to represent arena landmarks.
    /// </summary>
    /// <param name="position">Position you want to target.</param>
    /// <param name="objectiveType">Type of target.</param>
    public TargetObjective(Vector3 position, ObjectiveType objectiveType)
    {
        this.position = position;
        this.objectiveType = objectiveType;
        knowsLocation = new bool[GameController.NumCPU];
        timeSinceNotified = new float[GameController.NumCPU];
    }

    /// <summary>
    /// Sets the knowsLocation variable and resets timeSinceNotified.
    /// </summary>
    /// <param name="value">Value knowsLocation is set to.</param>
    /// <param name="index">Which AI PC player knows the location.</param>
    public void SetKnowsLocation(bool value, int index)
    {
        knowsLocation[index] = value;
        timeSinceNotified[index] = 0;
    }
    
    /// <summary>
    /// Sets the knowsLocation variable and resets timeSinceNotified for all AI.
    /// </summary>
    /// <param name="value">Value knowsLocation is set to.</param>
    public void SetKnowsLocation(bool value)
    {
        for (int i = 0; i < knowsLocation.Length; i++)
        {
            knowsLocation[i] = value;
            timeSinceNotified[i] = 0;
        }
    }

    /// <summary>
    /// Getter for whether the AI PC player of the specified index knows the location of the target objective.
    /// </summary>
    /// <param name="index">Which AI PC player knows the location.</param>
    /// <returns>Boolean whether the AI PC player knows the location.</returns>
    public bool GetKnowsLocation(int index)
    {
        return knowsLocation[index];
    }

    /// <summary>
    /// Getter for the time since the AI PC players were notified about the position of the target objective.
    /// </summary>
    /// <param name="index">Which AI PC player knows the location.</param>
    /// <returns>Float time since the AI was notified of the target position.</returns>
    public float GetTimeSinceNotified(int index)
    {
        return timeSinceNotified[index];
    }

    /// <summary>
    /// Updates the time since the AI PC players were notified about the position of the target objective.
    /// </summary>
    public void UpdateTime()
    {
        for (int i = 0; i < timeSinceNotified.Length; i++)
        {
            timeSinceNotified[i] += Time.deltaTime;
        }
    }

    /// <summary>
    /// Gets string representation of the target objective.
    /// </summary>
    /// <returns>String representation of the target objective.</returns>
    public override string ToString()
    {
        return objectiveType.ToString();
    }
}