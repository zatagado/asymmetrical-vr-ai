using Pathfinding;
using System;
using System.Collections;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

/// <summary>
/// Allows the AI to avoid the VR player in an elliptical radius if they get too close. If the AI is unable 
/// to avoid the VR player for a certain amount of time, then dynamically reduce the ellipse avoidance radius 
/// until the AI is able to escape.
/// 
/// There are three ellipses: the center ellipse, inner ellipse, and outer ellipse. 
/// When the AI is inside the outer ellipse, its movement path is checked for intersections with the inner ellipse.
/// When the AI is inside the inner ellipse, it 
/// When the AI is 
/// </summary>
public class AvoidCloseVRAction : BTNode
{
    private AIController ai = null;
    private float maxYDiff = 0f;

    private AstarPath astar = null;
    private NNConstraint graphConstraint = null;

    private float timeNearby = 0f;
    private float timeAway = 0f;
    private float timeAwayThreshold = 0f;
    private int timeIndex = 0;

    private Action<float>[] ellipseSizes = null; // Events to change the size of the ellipse after the AI player is in the avoidance ellipse of the VR player for too long

    private bool moveClockwise = true; // if false then is moving counterclockwise

    private Vector2 currFoci = Vector2.zero;
    private Vector2 maxFoci = Vector2.zero;

    private Vector2 currInnerFociOffset = Vector2.zero;
    private Vector2 innerFociOffset = Vector2.zero;

    private Vector2 currOuterFociOffset = Vector2.zero;
    private Vector2 outerFociOffset = Vector2.zero;

    private float currCenterOffset = 0f;
    private float centerOffset = 0f;
    private Vector3 center = Vector2.zero;

    private float[] ellipseLUT = null; // Must be even length.

    /// <summary>
    /// Multithreaded job calculates the distances between constant angle intervals on the ellipse.
    /// </summary>
    public struct EllipseLUTJob : IJob
    {
        public float samples;
        public float fociX;
        public float fociY;
        public NativeArray<float> result;

        /// <summary>
        /// Fill the result array with constant angle distances based on number of samples and the foci of the ellipse.
        /// </summary>
        public void Execute()
        {
            // only need to calculate a quarter of ellipse distances to get all the information we need
            int sampleQuarter = Mathf.CeilToInt(samples / 4);
            Vector3[] samplePoints = new Vector3[sampleQuarter + 1];

            for (int i = 0; i < samplePoints.Length; i++)
            {
                float signedAngle = (360 / samples * i) - 180;
                signedAngle = Mathf.Abs(signedAngle) >= 180 ? 360 : signedAngle;

                samplePoints[i].x = (signedAngle >= 0 ? 1 : -1) * (fociX * fociY / Mathf.Sqrt(Mathf.Pow(fociY, 2) + 
                    (Mathf.Pow(fociX, 2) * Mathf.Pow(Mathf.Tan(Mathf.Deg2Rad * (signedAngle + 90)), 2))));
                samplePoints[i].z = -samplePoints[i].x * Mathf.Tan(Mathf.Deg2Rad * (signedAngle + 90));
            }

            for (int i = 0; i < sampleQuarter; i++)
            {
                result[i] = Vector3.Distance(samplePoints[i], samplePoints[i + 1]);
            }
        }
    }

    /// <summary>
    /// Constructor for AvoidCloseVRAction node.
    /// Allows the AI to avoid the VR player in an elliptical radius if they get too close. If the AI is unable 
    /// to avoid the VR player for a certain amount of time, then dynamically reduce the ellipse avoidance radius 
    /// until the AI is able to escape.
    /// </summary>
    /// <param name="ai">The aiController object for the AI.</param>
    /// <param name="maxFoci"></param>
    /// <param name="innerFociOffset"></param>
    /// <param name="outerFociOffset"></param>
    /// <param name="centerOffset">Offset of the player from the center of the ellipse on the z axis.</param>
    /// <param name="maxYDiff">The maximum difference in y coordinate to check if the AI is in the ellipse.</param>
    /// <param name="ellipseLUTLength">Length for the ellipse look up table.</param>
    /// <param name="times">A float tuple array with the first float in the tuple representing the time the AI can be too 
    /// close to the VR player before the ellipse shrinks. The second float in the tuple is the ratio of the original 
    /// ellipse to the new ellipse.</param>
    /// <param name="timeAwayThreshold">Amount of time the AI has to be away from the VR player ellipse before the ellipse 
    /// around the VR player will reset.</param>
    public AvoidCloseVRAction(AIController ai, Vector2 maxFoci, Vector2 innerFociOffset, 
        Vector2 outerFociOffset, float centerOffset, float maxYDiff, int ellipseLUTLength, Tuple<float, float>[] times,
        float timeAwayThreshold)
    {
        this.ai = ai;
        this.maxYDiff = maxYDiff;

        astar = AstarPath.active;
        graphConstraint = new NNConstraint() // information about which graph the AI can pathfind on, etc.
        {
            graphMask = ai.AISeeker.graphMask,
            constrainArea = false,
            distanceXZ = false,
            constrainTags = false,
            constrainDistance = false,
        };

        this.maxFoci = maxFoci;
        this.innerFociOffset = innerFociOffset;
        this.outerFociOffset = outerFociOffset;
        this.centerOffset = centerOffset;

        ellipseLUT = new float[ellipseLUTLength];
        moveClockwise = true;

        ai.StartCoroutine(ChangeEllipseSize(maxFoci, innerFociOffset, outerFociOffset, centerOffset)); // need to use aiController object for coroutines since it extends monobehaviour

        ellipseSizes = new Action<float>[times.Length + 1];

        // fill ellipseSizes event array with functions that shrink the size of the ellipse after the AI is too close for a certain period of time
        for (int i = 0; i < times.Length - 1; i++)
        {
            Tuple<float, float> normalTuple = times[i];
            ellipseSizes[i] = (time) =>
            {
                if (time > normalTuple.Item1)
                {
                    Vector2 newFoci = new Vector2(maxFoci.x * normalTuple.Item2, maxFoci.y * normalTuple.Item2);
                    float newCenterOffset = centerOffset * normalTuple.Item2;
                    ai.StartCoroutine(ChangeEllipseSize(newFoci, innerFociOffset, outerFociOffset, newCenterOffset));
                    timeIndex++;
                }
            };
        }

        // final ellipse has foci of zero
        Tuple<float, float> finalTuple = times[times.Length - 1];
        ellipseSizes[times.Length - 1] = (time) =>
        {
            if (time > finalTuple.Item1)
            {
                ai.StartCoroutine(ChangeEllipseSize(Vector2.zero, Vector2.zero, Vector2.one, 0));
                timeIndex++;
            }
        };

        ellipseSizes[times.Length] = (time) => {}; // need empty function to prevent errors

        this.timeAwayThreshold = timeAwayThreshold;

        ai.OnTargetChanged += ResetEllipse;
    }

    /// <summary>
    /// Resets the ellipse to the default size after a certain period of time if the ellipse size has changed.
    /// </summary>
    private void ResetEllipse()
    {
        if (currFoci != maxFoci)
        {
            ai.StartCoroutine(ChangeEllipseSize(maxFoci, innerFociOffset, outerFociOffset, centerOffset));
            timeIndex = 0;
            timeNearby = 0;
        }
    }

    /// <summary>
    /// Runs when the behavior tree reaches this node.
    /// If the VR player is too close, the AI will avoid the VR player in an elliptical radius.
    /// </summary>
    /// <returns>Success if the AI needs to avoid the VR player, failure if they do not.</returns>
    public override BTNodeState Tick()
    {
        ai.treeNodes += "AvoidCloseVRAction\n";

        center = ai.VRPlayerPosition + (ai.VRPlayerForward * currCenterOffset);

        // checking if the target is inside of the ellipse
        if (ai.PathCalc.vectorPath != null) // if the AI has a path it's following
        {
            Vector3 targetEndpoint = ai.PathCalc.vectorPath[ai.PathCalc.vectorPath.Count - 1];
            if (-maxYDiff < targetEndpoint.y - center.y && maxYDiff > targetEndpoint.y - center.y &&
                IsPointInEllipse2D(targetEndpoint, currFoci + currOuterFociOffset, center, ai.VRPlayerRotation)) // check if the final target position for the AI is too close to the VR player (within outer ellipse)
            {
                ai.PathCalc.target.OnRequestNewTarget?.Invoke(ai.PathCalc.target); // request a different target if possible
            }
        }

        if (-maxYDiff < ai.AIPathfinder.GetFeetPosition().y - center.y && ai.AIPathfinder.GetFeetPosition().y - center.y < maxYDiff && 
            IsPointInEllipse2D(ai.transform.position, currFoci + currOuterFociOffset, center, ai.VRPlayerRotation)) // check if AI is currently within outer ellipse
        {
            timeAway = 0;
            timeNearby += Time.deltaTime;
            ellipseSizes[timeIndex](timeNearby); // change ellipse size if AI can't escape VR player

            if (IsPointInEllipse2D(ai.transform.position, currFoci + currInnerFociOffset, center, ai.VRPlayerRotation)) // check if AI is within inner cellipse
            {
                Vector3 avoidPoint = FindPointOnEllipse2D(Vector3.SignedAngle(ai.VRPlayerForward,
                    ai.transform.position - center, Vector3.up), currFoci, center, ai.VRPlayerRotation); // avoid VR player moving around the middle ellipse

                ai.AvoidTarget = new TargetObjective(avoidPoint, ObjectiveType.AvoidPosition);
                return BTNodeState.SUCCESS;
            }
            else if (ai.PathCalc.vectorPath != null && IsRayIntersectingEllipse((ai.PathCalc.vectorPath[1] - ai.transform.position).normalized, 
                ai.transform.position, currFoci + currInnerFociOffset, center, ai.VRPlayerRotation)) // check if AI path to final target position intersects with inner ellipse
            {
                Vector3 avoidPoint = FindPointOnEllipse2D(FindNextEllipseAngle(Vector3.SignedAngle(
                    ai.VRPlayerForward, new Vector3(ai.transform.position.x, center.y, ai.transform.position.z) - center,
                    Vector3.up), ai.AIPathfinder.maxSpeed), currFoci, center, ai.VRPlayerRotation); // avoid VR player moving around the middle ellipse

                ((NavmeshBase)astar.graphs[0]).Linecast(ai.PlayerTransform.position, avoidPoint + 
                    (avoidPoint - ai.PlayerTransform.position).normalized, 
                    astar.GetNearest(ai.PlayerTransform.position, graphConstraint).node, out GraphHitInfo hitInfo);

                if (hitInfo.distance < 0.1f) // check if AI can't move any further along current avoid path due to running out of space on navmesh
                {
                    moveClockwise = !moveClockwise; // switch avoid direction of travel

                    avoidPoint = FindPointOnEllipse2D(FindNextEllipseAngle(Vector3.SignedAngle(
                    ai.VRPlayerForward, new Vector3(ai.transform.position.x, center.y, ai.transform.position.z) - center,
                    Vector3.up), ai.AIPathfinder.maxSpeed), currFoci, center, ai.VRPlayerRotation);

                    ai.AvoidTarget = new TargetObjective(avoidPoint, ObjectiveType.AvoidPosition); // choose target position along middle ellipse
                }
                else // otherwise can continue moving along path because there is room on the navmesh
                {
                    ai.AvoidTarget = new TargetObjective(avoidPoint, ObjectiveType.AvoidPosition); // choose target position along middle ellipse
                }

                return BTNodeState.SUCCESS;
            }
            else // AI is within outer ellipse but not in inner ellipse and path does not intersect with inner ellipse so no avoiding is necessary
            {
                moveClockwise = !moveClockwise; // switch up the direction of travel so we dont need to check an if statement
                return BTNodeState.FAILURE;
            }
        }
        else // AI is not in any ellipse and does not need to avoid
        {
            timeAway += Time.deltaTime;
            if (timeAway > timeAwayThreshold) // check reset ellipse if enough time passed being not near the VR player
            {
                timeNearby = 0;
                ResetEllipse();
            }

            moveClockwise = !moveClockwise; // switch up the direction of travel so we dont need to check an if statement
            return BTNodeState.FAILURE;
        }
    }

    /// <summary>
    /// Changes the size of the ellipse. Starts a multithreaded job that creates a look up table 
    /// for lengths of the array.
    /// </summary>
    /// <param name="newFoci">The new foci of the ellipse.</param>
    /// <param name="newInnerFociOffset"></param>
    /// <param name="newOuterFociOffset"></param>
    /// <param name="newCenterOffset"></param>
    /// <returns>Coroutine allowing for asynchronous execution of this function.</returns>
    private IEnumerator ChangeEllipseSize(Vector2 newFoci, Vector2 newInnerFociOffset, Vector2 newOuterFociOffset, float newCenterOffset)
    {
        NativeArray<float> result = new NativeArray<float>(ellipseLUT.Length, Allocator.TempJob);

        // set up the job
        EllipseLUTJob job = new EllipseLUTJob();
        job.samples = ellipseLUT.Length;
        job.fociX = newFoci.x;
        job.fociY = newFoci.y;
        job.result = result;

        JobHandle handle = job.Schedule(); // start job execution

        while (!handle.IsCompleted) // continue normal execution while waiting for job to complete
        {
            yield return null;
        }

        handle.Complete(); // ensure job completion. may be redundant
        result.CopyTo(ellipseLUT); // only actually need to calculate 1/4
        result.Dispose();

        // since an ellipse can be split into four quarters, calculate the rest
        int n = Mathf.CeilToInt((float)ellipseLUT.Length / 4);
        for (int i = (ellipseLUT.Length / 4) - 1; i >= 0; i--)
        {
            ellipseLUT[n + i] = ellipseLUT[(ellipseLUT.Length / 4) - 1 - i];
        }
        for (int i = 0; i < ellipseLUT.Length / 2; i++)
        {
            ellipseLUT[(ellipseLUT.Length / 2) + i] = ellipseLUT[i];
        }
        currFoci = newFoci;
        currInnerFociOffset = newInnerFociOffset;
        currOuterFociOffset = newOuterFociOffset;
        currCenterOffset = newCenterOffset;
    }

    /// <summary>
    /// Finds the next angle on the ellipse according to the speed specified.
    /// The calculated angle is only approximate using the ellipse look up table.
    /// </summary>
    /// <param name="signedAngle">Current angle needed to get the next angle.</param>
    /// <param name="speed">The speed is used to determine how far away the next angle is.</param>
    /// <returns></returns>
    private float FindNextEllipseAngle(float signedAngle, float speed)
    {
        float distance = (speed + 4.0f) * Time.deltaTime; // to account for curves, distance must be greater than what is possible to travel

        if (moveClockwise)
        {
            float rel = (signedAngle + 180) / 360 * ellipseLUT.Length; // converted to a number that is within 0 - ellipseLUT.Length
            int relInt = ((int)rel) % ellipseLUT.Length;
            float currDistance = ellipseLUT[relInt] - (ellipseLUT[relInt] * (rel % 1)); // upper section //! index out of bounds possibly due to floating point error
            int i = 0;
            while (currDistance < distance)
            {
                currDistance += ellipseLUT[(relInt + ++i) % ellipseLUT.Length]; // add another entire array element
            }
            // currdistance >= distance subtract upper end from current
            // something to get angle
            float overflow = currDistance - distance; // the amount that is too much on the upper end
            float actual = ellipseLUT[(relInt + i) % ellipseLUT.Length] - overflow; // lower end amount
            float ratio = actual / ellipseLUT[(relInt + i) % ellipseLUT.Length]; // ratio relative to size of ellipseLUT segment, ratio should never be over 1
            float ret = ((ratio + ((relInt + i) % ellipseLUT.Length)) / ellipseLUT.Length * 360) - 180;
            return ret; 
        }
        else // is moving right //! add to distance
        {
            float rel = (signedAngle + 180) / 360 * ellipseLUT.Length;
            int relInt = ((int)rel) % ellipseLUT.Length;
            float currDistance = ellipseLUT[relInt] * (rel % 1); // lower section
            int i = 0;
            while (currDistance < distance)
            {
                currDistance += ellipseLUT[(relInt - ++i + ellipseLUT.Length) % ellipseLUT.Length];
            }
            float overflow = currDistance - distance;
            float ratio = overflow / ellipseLUT[(relInt - i + ellipseLUT.Length) % ellipseLUT.Length];
            float ret = ((ratio + ((relInt - i + ellipseLUT.Length) % ellipseLUT.Length)) / ellipseLUT.Length * 360) - 180;
            return ret; 
        }
    }

    /// <summary>
    /// Finds a point on the border of a 2D ellipse on the xz plane.
    /// </summary>
    /// <param name="signedAngle">The signed angle (-180 - 180) where 0 is the front and center of the ellipse. This angle is only around the y axis.</param>
    /// <param name="foci">The foci of the ellipse.</param>
    /// <param name="center">The center point of the ellipse.</param>
    /// <param name="rotation">The rotation of the ellipse. Ensure that this rotation is only around the y axis.</param>
    /// <returns>The point on the border of the ellipse.</returns>
    private Vector3 FindPointOnEllipse2D(float signedAngle, Vector2 foci, Vector3 center, Quaternion rotation)
    {
        signedAngle = Mathf.Abs(signedAngle) >= 180 ? 360 : signedAngle;

        Vector3 localPoint = Vector3.zero;

        localPoint.x = (signedAngle >= 0 ? 1 : -1) * (foci.x * foci.y / Mathf.Sqrt(Mathf.Pow(foci.y, 2) + 
            (Mathf.Pow(foci.x, 2) * Mathf.Pow(Mathf.Tan(Mathf.Deg2Rad * (signedAngle + 90)), 2))));
        localPoint.z = -localPoint.x * Mathf.Tan(Mathf.Deg2Rad * (signedAngle + 90));

        Vector3 worldPoint = Matrix4x4.Rotate(rotation).MultiplyPoint3x4(localPoint);
        worldPoint = Matrix4x4.Translate(center).MultiplyPoint3x4(worldPoint);
        return worldPoint;
    }

    /// <summary>
    /// Finds if a point is inside of the specified 2D ellipse.
    /// </summary>
    /// <param name="point"></param>
    /// <param name="foci">The foci of the ellipse.</param>
    /// <param name="center">The center point of the ellipse.</param>
    /// <param name="rotation">The rotation of the ellipse. Ensure that this rotation is only around the y axis.</param>
    /// <returns>Boolean if the point is within the ellipse.</returns>
    private bool IsPointInEllipse2D(Vector3 point, Vector2 foci, Vector3 center, Quaternion rotation)
    {
        Vector3 relPosition = Matrix4x4.Rotate(Quaternion.Inverse(rotation)).MultiplyPoint3x4(point - center);
        return (Mathf.Pow(relPosition.x, 2) / Mathf.Pow(foci.x, 2)) + (Mathf.Pow(relPosition.z, 2) / Mathf.Pow(foci.y, 2)) <= 1;
    }

    /// <summary>
    /// Finds if a ray intersects with a specified 2D ellipse.
    /// </summary>
    /// <param name="rayDirection">Direction vector of the ray.</param>
    /// <param name="rayOrigin">Origin point of the ray.</param>
    /// <param name="ellipseFoci">Foci of the ellipse.</param>
    /// <param name="ellipseOrigin">Origin point of the ellipse.</param>
    /// <param name="ellipseRotation">Rotation of the ellipse.</param>
    /// <returns>Boolean if the ray intersects with the ellipse.</returns>
    private bool IsRayIntersectingEllipse(Vector3 rayDirection, Vector3 rayOrigin, Vector2 ellipseFoci, Vector3 ellipseOrigin, Quaternion ellipseRotation)
    {
        ellipseRotation = Quaternion.Inverse(ellipseRotation);

        Vector3 rayPoint1 = rayOrigin;
        Vector3 rayPoint2 = rayOrigin + rayDirection;

        rayPoint1 = Matrix4x4.Translate(-ellipseOrigin).MultiplyPoint3x4(rayPoint1);
        rayPoint2 = Matrix4x4.Translate(-ellipseOrigin).MultiplyPoint3x4(rayPoint2);
        rayPoint1 = Matrix4x4.Rotate(ellipseRotation).MultiplyPoint3x4(rayPoint1);
        rayPoint2 = Matrix4x4.Rotate(ellipseRotation).MultiplyPoint3x4(rayPoint2);

        float m = (rayPoint2.z - rayPoint1.z) / (rayPoint2.x - rayPoint1.x != 0 ? rayPoint2.x - rayPoint1.x : Mathf.Epsilon); // make certain m cannot be undefined
        float c = rayPoint1.z - (m * rayPoint1.x);

        bool lineIntersects = (c * c) < ((ellipseFoci.x * ellipseFoci.x) * (m * m)) + (ellipseFoci.y * ellipseFoci.y);

        if (lineIntersects)
        {
            float qa = (1 / (ellipseFoci.x * ellipseFoci.x)) + ((m * m) / (ellipseFoci.y * ellipseFoci.y));
            float qb = (2 * m * c) / (ellipseFoci.y * ellipseFoci.y);

            float x = (-qb + Mathf.Sqrt((qb * qb) - (4 * qa * (((c * c) / (ellipseFoci.y * ellipseFoci.y)) - 1)))) / (2 * qa);
            float y = (m * x) + c;
            Vector2 output = new Vector2(x, y);

            return Vector2.Dot(output - new Vector2(rayPoint1.x, rayPoint1.z), new Vector2(rayPoint2.x, rayPoint2.z) - new Vector2(rayPoint1.x, rayPoint1.z)) > 0;
        }
        else
        {
            return false;
        }
    }
}
