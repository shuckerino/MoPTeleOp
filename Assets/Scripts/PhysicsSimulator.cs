using Meta.WitAi.Utilities;
using System.Collections;
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

    void Start()
    {
        //Physics.simulationMode = SimulationMode.Script;
    }

    public Dictionary<int, List<float>> SimulateJointAngles(float[] currentAngles)
    {
        //Physics.simulationMode = SimulationMode.Script;
        Physics.autoSimulation = false;
        int jointCount = jointTransforms.Length;
        Dictionary<int, List<float>> collisionAngles = new Dictionary<int, List<float>>();

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
                if (a < colliderToCheck.Length && CheckForCollision(colliderToCheck[a]))
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
                collisionAngles.Add(a, collisionAngleValues);
            else if (collisionAngleValues.Count == 1)
            {
                collisionAngleValues.Add(0.0f);
                collisionAngles.Add(a, collisionAngleValues);
            }
            else
            {
                List<float> list = new List<float> { 180.0f, 180.0f };
                collisionAngles.Add(a, list);

            }
        }

        return collisionAngles;
    }
    //private bool CheckForCollision()
    //{
    //    foreach (var colliderA in colliderToCheck)
    //    {
    //        foreach (var colliderB in colliderToCheck)
    //        {
    //            if (colliderA != colliderB && colliderA.bounds.Intersects(colliderB.bounds))
    //            {
    //                return true; // Collision detected
    //            }
    //        }
    //    }

    //    return false; // No collision
    //}


    private bool CheckForCollision(Collider currentCollider)
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

        return false; // No collision
    }

}





//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public class PhysicsSimulator : MonoBehaviour
//{
//    public Transform[] jointTransforms; // Assign joints in Inspector
//    public Collider[] colliderToCheck;
//    public float angleStep = 5.0f;        // Increment step for angles
//    public float simulationDelay = 0.1f; // Delay between angle steps
//    private Dictionary<int, List<float>> collisionAngles;

//    void Start()
//    {
//        collisionAngles = new Dictionary<int, List<float>>();
//        StartCoroutine(SimulateJointAngles());
//    }

//    private IEnumerator SimulateJointAngles()
//    {
//        int jointCount = jointTransforms.Length;

//        for (int a = 0; a < jointCount; a++)
//        {
//            List<float> collisionAngleValues = new List<float>();
//            float angleToSimulate = -180.0f;
//            Quaternion initialJointRotation = jointTransforms[a].localRotation;

//            while (angleToSimulate <= 180.0f)
//            {
//                // Determine axis of rotation based on joint index
//                Vector3 rotationAxis = (a == 0 || a == 4) ? Vector3.up : Vector3.right;

//                // Adjust the simulated angle
//                jointTransforms[a].localRotation = Quaternion.AngleAxis(angleToSimulate, rotationAxis);

//                // Wait for physics simulation to process
//                yield return new WaitForSeconds(simulationDelay);

//                // Check for collisions
//                if (CheckForCollision())
//                {
//                    collisionAngleValues.Add(angleToSimulate);
//                }

//                angleToSimulate += angleStep;
//            }

//            // Restore the initial rotation
//            jointTransforms[a].localRotation = initialJointRotation;

//            // Store the collision angles
//            collisionAngles.Add(a, collisionAngleValues);
//        }

//        // Debug the results
//        DebugCollisionAngles();
//    }

//    private bool CheckForCollision()
//    {
//        foreach (var colliderA in colliderToCheck)
//        {
//            foreach (var colliderB in colliderToCheck)
//            {
//                if (colliderA != colliderB)
//                {
//                    Vector3 direction;
//                    float distance;

//                    if (Physics.ComputePenetration(
//                        colliderA, colliderA.transform.position, colliderA.transform.rotation,
//                        colliderB, colliderB.transform.position, colliderB.transform.rotation,
//                        out direction, out distance))
//                    {
//                        return true; // Collision detected
//                    }
//                }
//            }
//        }

//        return false; // No collision
//    }

//    private void DebugCollisionAngles()
//    {
//        foreach (var entry in collisionAngles)
//        {
//            Debug.Log($"Joint {entry.Key} collides at angles: {string.Join(", ", entry.Value)}");
//        }
//    }
//}

