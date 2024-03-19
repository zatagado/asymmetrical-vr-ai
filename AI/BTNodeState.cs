/// <summary>
/// The current state of a behavior tree behavior.
/// </summary>
public enum BTNodeState
{
    SUCCESS, // The behavior successfully occurred.
    FAILURE, // The behavior failed.
    RUNNING // The behavior is still running and has not succeeded or failed.
}
