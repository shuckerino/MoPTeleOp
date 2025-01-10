using Meta.WitAi.Utilities;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class PhysicsSimulator : MonoBehaviour
{
    public Transform[] jointTransforms; // Assign joints in Inspector
    public Collider[] colliderToCheck;
    public float angleStep = 2.0f;        // Increment step for angles
    public float physicsTimeStep = 0.2f; // Simulation time step
    float startingAngle = -180.0f;          // Starting angle for simulation
    float endingAngle = 180.0f;            // Ending angle for simulation

    bool newResultAvailable = false;
    ConcurrentDictionary<int, List<float>> collisionAngles = new ConcurrentDictionary<int, List<float>>();
    Dictionary<int, List<float>> lastResult = new Dictionary<int, List<float>>();

    void Start()
    {
        Physics.autoSimulation = false;
    }

    public void SimulateJointAngles(float[] currentAngles)
    {
        newResultAvailable = false;
        int jointCount = jointTransforms.Length;
        collisionAngles.Clear();
        // run simulation for each joint
        for (int a = 0; a < jointCount; a++)
        {
            List<float> collisionAngleValues = new List<float>();

            bool doneWithForwardSimulation = false;
            bool doneWithBackwardSimulation = false;
            float angleToSimulate = currentAngles[a];
            Quaternion initialJointRotation = jointTransforms[a].localRotation;

            // simulation in both directions -> first positive, then negative
            while (!doneWithForwardSimulation || !doneWithBackwardSimulation)
            {
                // Determine axis of rotation based on joint index
                Vector3 rotationAxis = (a == 0 || a == 4) ? Vector3.up : Vector3.right;
                jointTransforms[a].localRotation = Quaternion.AngleAxis(angleToSimulate, rotationAxis);

                // Simulate physics
                Physics.Simulate(physicsTimeStep);
                //yield return new WaitForFixedUpdate();

                // Check for collisions
                //if (a < colliderToCheck.Length && CheckForCollision(colliderToCheck[a]))
                if (a < colliderToCheck.Length && CheckForCollision())
                {
                    collisionAngleValues.Add(Mathf.Abs(angleToSimulate - currentAngles[a]));

                    // break forward simulation after first collision found
                    if (!doneWithForwardSimulation)
                        doneWithForwardSimulation = true;
                    else
                        doneWithBackwardSimulation = true;
                }

                // forward simulation
                if (!doneWithForwardSimulation)
                {
                    // adjust the simulated angle in positive direction
                    if (angleToSimulate <= endingAngle)
                        angleToSimulate += angleStep;
                    else
                        doneWithForwardSimulation = true;
                }
                else // backward simulation
                {
                    // adjust the simulated angle in negative direction
                    if (angleToSimulate >= startingAngle)
                        angleToSimulate -= angleStep;
                    else
                        doneWithBackwardSimulation = true;
                }

            }

            jointTransforms[a].localRotation = initialJointRotation;

            if (collisionAngleValues.Count == 2)
                collisionAngles.TryAdd(a, collisionAngleValues);
            else if (collisionAngleValues.Count == 1)
            {
                collisionAngleValues.Add(0.0f);
                collisionAngles.TryAdd(a, collisionAngleValues);
            }
            else
            {
                List<float> list = new List<float> { 180.0f, 180.0f };
                collisionAngles.TryAdd(a, list);

            }
        }
        lock (lastResult)
        {
            lastResult = new Dictionary<int, List<float>>(collisionAngles);
        }
        newResultAvailable = true;
    }

    private bool CheckForCollision()
    {
        foreach (var currentCollider in colliderToCheck)
        {
            foreach (var colliderA in colliderToCheck)
            {
                if (colliderA != currentCollider)
                {
                    Vector3 direction;
                    float distance;

                    if (Physics.ComputePenetration(
                        colliderA, colliderA.transform.position, colliderA.transform.rotation,
                        currentCollider, currentCollider.transform.position, currentCollider.transform.rotation,
                        out direction, out distance))
                    {
                        return true; // Collision detected
                    }
                }
            }
        }

        return false; // No collision
    }

    public Dictionary<int, List<float>> GetLastSimulationResult()
    {
        var result = new Dictionary<int, List<float>>();
        lock (lastResult)
        {
            // Return a copy to ensure thread safety
            result = new Dictionary<int, List<float>>(lastResult);
        }

        return result;
    }

    public bool IsNewResultAvailable()
    {
        return newResultAvailable;
    }

}


