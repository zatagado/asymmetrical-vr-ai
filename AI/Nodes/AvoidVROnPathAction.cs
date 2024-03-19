using Pathfinding;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Allows the AI to avoid pathfinding near the VR player unless there are no other available paths to the target position.
/// AI uses the A* search algorithm so nodes nearby the VR player are set to an artificially high cost of traversal.
/// </summary>
public class AvoidVROnPathAction : BTNode
{
    private readonly uint _default_penalty = 0;
    private readonly uint _world_unit_size = 1000;
    private AIController ai = null;
    private AstarPath astar = null;
    private NNConstraint graphConstraint = null;
    private GraphNode prevNode = null; // The previous node the VR player was nearest. The VR player may not alwasy be on a node.

    private float avoidRange = 10f; // TODO switch to constructor or make dynamic

    private List<TriangleMeshNode> validNodes = new List<TriangleMeshNode>();
    private List<TriangleMeshNode> invalidNodes = new List<TriangleMeshNode>();

    /// <summary>
    /// Constructor for AvoidVROnPathAction node.
    /// Allows the AI to avoid pathfinding near the VR player unless there are no other available paths to the target position.
    /// AI uses the A* search algorithm so nodes nearby the VR player are set to an artificially high cost of traversal.
    /// </summary>
    /// <param name="aiController">The aiController object for the AI.</param>
    public AvoidVROnPathAction(AIController aiController)
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
        prevNode = astar.GetNearest(ai.VRPlayerPosition, graphConstraint).node;
    }

    /// <summary>
    /// Runs when the behavior tree reaches this node.
    /// Sets the cost of traversing nodes near the VR player.
    /// </summary>
    /// <returns>A BTNodeState for success. There is no fail condition.</returns>
    public override BTNodeState Tick()
    {
        ai.treeNodes += "AvoidVROnPathAction\n";
        GraphNode node = astar.GetNearest(ai.VRPlayerPosition, graphConstraint).node; // check node nearest to VR player

        // when a work item is added every frame, it lags pathfinding, so only do it when necessary
        if (node != prevNode) // check if nearest node to VR player has changed
        {
            GraphNode currentNode = node;
            astar.AddWorkItem(() => 
            {
                SetAvoidanceRadius();
            }); // necessary to change A* graph information
        }
        prevNode = node;
        return BTNodeState.SUCCESS;
    }

    /// <summary>
    /// Wrapper function for operations necessary to set navigation mesh node costs around the VR player.
    /// </summary>
    private void SetAvoidanceRadius()
    {
        ResetBeforeSearch(validNodes, invalidNodes);
        SetAvoidancePenalty(AstarPath.active.GetNearest(ai.VRPlayerPosition, graphConstraint).node as TriangleMeshNode, 
            avoidRange, validNodes, invalidNodes); // range later
        CleanAfterSearch(validNodes, invalidNodes);
    }

    /// <summary>
    /// Search nodes within range of the VR player to set their penalty. Avoid checking nodes that were already checked.
    /// </summary>
    /// <param name="node">The node we are looking to modify the cost of. </param>
    /// <param name="range">The distance from the VR player to the center of the triangle mesh node that we wish to set a high traversal cost.</param>
    /// <param name="validNodes">Nodes that had their penalty modified.</param>
    /// <param name="invalidNodes">Nodes that were searched by SetAvoidancePenalty() but did not have their penalty modified.</param>
    private void SetAvoidancePenalty(TriangleMeshNode node, float range, List<TriangleMeshNode> validNodes, 
        List<TriangleMeshNode> invalidNodes)
    {
        if (node != null && node.Tag != 2) // got null reference here before
        {
            node.Tag = 2; // mark that the node is searched

            float distance = Vector3.Distance(ai.VRPlayerPosition, (Vector3)node.position);
            if (distance < range)
            {
                validNodes.Add(node);
                // set penalty high but inverse of distance
                uint distancePenalty = (uint)(((range + 1) / (distance + 1)) * 100); // as distance gets larger the penalty gets smaller. the 1 is added so penalty isnt infinitely high at short distances
                uint basePenalty = _world_unit_size * 20;
                node.Penalty = distancePenalty + basePenalty;

                for (int i = 0; i < node.connections.Length; i++) // check all connection to the current node
                {
                    if (node.connections[i].node is TriangleMeshNode) // ignore jump locations which are also node on the navmesh
                    {
                        SetAvoidancePenalty(node.connections[i].node as TriangleMeshNode, range, validNodes, invalidNodes);
                    }
                }
            }
            else
            {
                invalidNodes.Add(node);
            }
        }
    }

    /// <summary>
    /// Reset the penalties of searched nodes for the upcoming search.
    /// </summary>
    /// <param name="validNodes">Nodes that had their penalty modified.</param>
    /// <param name="invalidNodes">Nodes that were searched by SetAvoidancePenalty() but did not have their penalty modified.</param>
    private void ResetBeforeSearch(List<TriangleMeshNode> validNodes, List<TriangleMeshNode> invalidNodes)
    {
        foreach (TriangleMeshNode node in validNodes)
        {
            node.Penalty = 0;
        }
        validNodes.Clear();
        invalidNodes.Clear();
    }

    /// <summary>
    /// Reset the tags of searched nodes for the next search.
    /// </summary>
    /// <param name="validNodes">Nodes that had their penalty modified.</param>
    /// <param name="invalidNodes">Nodes that were searched by SetAvoidancePenalty() but did not have their penalty modified.</param>
    private void CleanAfterSearch(List<TriangleMeshNode> validNodes, List<TriangleMeshNode> invalidNodes)
    {
        foreach (TriangleMeshNode node in validNodes)
        {
            node.Tag = _default_penalty;
        }

        foreach (TriangleMeshNode node in invalidNodes)
        {
            node.Tag = _default_penalty;
        }
    }
}
