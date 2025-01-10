// Modified from https://github.com/qian256/ur5_unity
// ORIGINAL
// Author: Long Qian
// Email: lqian8@jhu.edu

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class UR10Controller : MonoBehaviour
{
    public GameObject RobotBase;

    //public PincherController pincherController;
    public GripperController gripperController;
    private PhysicsSimulator simulator;

    public float[] jointValuesInDegrees = new float[6];
    private GameObject[] jointList = new GameObject[6];
    private float[] upperLimit = { 180f, 180f, 180f, 180f, 180f, 180f };
    private float[] lowerLimit = { -180f, -180f, -180f, -180f, -180f, -180f };
    private float[] jointSign = { -1f, 1f, 1f, 1f, -1f, 1f };

    float _interval = 0.25f;
    float _time;

    public TMP_Text Joint0Limits;
    public TMP_Text Joint1Limits;
    public TMP_Text Joint2Limits;
    public TMP_Text Joint3Limits;
    public TMP_Text Joint4Limits;


    public void UpdateJointValues(float[] newJointValues)
    {
        lock (jointValuesInDegrees)
        {
            //jointValuesInDegrees = newJointValues;
            for (int i = 0; i < newJointValues.Length; i++)
            {
                jointValuesInDegrees[i] = newJointValues[i];
            }
        }
    }

    // Use this for initialization
    void Start()
    {
        simulator = FindObjectOfType<PhysicsSimulator>();
        initializeJoints();
    }

    // Update is called once per frame
    void LateUpdate()
    {
        float[] currentAngles = new float[6];
        // joint values are in degrees
        for (int i = 0; i < 6; i++)
        {
            float angleInDegrees = jointValuesInDegrees[i];
            currentAngles[i] = angleInDegrees;
            // Determine axis of rotation based on joint index
            Vector3 rotationAxis = (i == 0 || i == 4) ? Vector3.up : Vector3.right;

            // Convert to quaternion
            Quaternion targetRotation = Quaternion.AngleAxis(angleInDegrees, rotationAxis);

            // Apply the rotation to the joint
            jointList[i].transform.localRotation = targetRotation;
        }
        //_time += Time.deltaTime;
        //while (_time >= _interval)
        //{
        simulator.SimulateJointAngles(currentAngles);
        Dictionary<int, List<float>> collisionAngles = simulator.GetLastSimulationResult();
        for (int k = 0; k < jointValuesInDegrees.Length; k++)
        {
            int posIndex, negIndex;
            if (k < 6 && jointSign[k] == -1f)
            {
                posIndex = 1;
                negIndex = 0;
            }
            else
            {
                posIndex = 0;
                negIndex = 1;
            }

            if (collisionAngles.ContainsKey(k))
            {
                Debug.Log($"Joint Limits for Joint_{k}: pos_{collisionAngles[k][posIndex]},  neg_{collisionAngles[k][negIndex]}");
            }
            else
            {
                Debug.Log($"Joint Limits for Joint_{k}: Free");
            }

            if (k == 0)
            {
                Joint0Limits.text = $"Joint 0: +{collisionAngles[k][posIndex]}, -{collisionAngles[k][negIndex]}";
            }
            else if (k == 1)
            {
                Joint1Limits.text = $"Joint 1: +{collisionAngles[k][posIndex]}, -{collisionAngles[k][negIndex]}";

            }
            else if (k == 2)
            {
                Joint2Limits.text = $"Joint 2: +{collisionAngles[k][posIndex]}, -{collisionAngles[k][negIndex]}";

            }
            else if (k == 3)
            {
                Joint3Limits.text = $"Joint 3: +{collisionAngles[k][posIndex]}, -{collisionAngles[k][negIndex]}";
            }
            else if (k == 4)
            {
                Joint4Limits.text = $"Joint 4: +{collisionAngles[k][posIndex]}, -{collisionAngles[k][negIndex]}";
            }
        }

        //    _time -= _interval;
        //}

        //pincherController.grip = jointValues[6];
        //gripperController.gripRatio = jointValues[6];
    }

    void OnGUI()
    {
        int boundary = 20;

#if UNITY_EDITOR
        int labelHeight = 20;
        GUI.skin.label.fontSize = GUI.skin.box.fontSize = GUI.skin.button.fontSize = 20;
#else
        int labelHeight = 40;
        GUI.skin.label.fontSize = GUI.skin.box.fontSize = GUI.skin.button.fontSize = 40;
#endif
        GUI.skin.label.alignment = TextAnchor.MiddleLeft;
        for (int i = 0; i < 6; i++)
        {
            GUI.Label(new Rect(boundary, boundary + (i * 2 + 1) * labelHeight, labelHeight * 4, labelHeight), "Joint " + i + ": ");
            jointValuesInDegrees[i] = GUI.HorizontalSlider(new Rect(boundary + labelHeight * 4, boundary + (i * 2 + 1) * labelHeight + labelHeight / 4, labelHeight * 5, labelHeight), jointValuesInDegrees[i], lowerLimit[i], upperLimit[i]);
            GUI.Label(new Rect(boundary + labelHeight * 4, boundary + (i * 2 + 2) * labelHeight, labelHeight * 5, labelHeight), $"{jointValuesInDegrees[i]:F1}°"); // Display angle to 1 decimal place
        }

        // Gripper value
        //    GUI.Label(new Rect(boundary, boundary + (6 * 2 + 1) * labelHeight, labelHeight * 4, labelHeight), "Gripper: ");
        //    jointValuesInDegrees[6] = GUI.HorizontalSlider(new Rect(boundary + labelHeight * 4, boundary + (6 * 2 + 1) * labelHeight + labelHeight / 4, labelHeight * 5, labelHeight), jointValuesInDegrees[6], 0, 1);
    }


    // Create the list of GameObjects that represent each joint of the robot
    void initializeJoints()
    {
        var RobotChildren = RobotBase.GetComponentsInChildren<Transform>();
        for (int i = 0; i < RobotChildren.Length; i++)
        {
            if (RobotChildren[i].name == "Joint_1")
            {
                jointList[0] = RobotChildren[i].gameObject;
                Joint0Limits.text = $"Joint 0: +{180.0f}, -{-180.0f}";
            }
            else if (RobotChildren[i].name == "Joint_2")
            {
                jointList[1] = RobotChildren[i].gameObject;
                Joint1Limits.text = $"Joint 1: +{180.0f}, -{-180.0f}";
            }
            else if (RobotChildren[i].name == "Joint_3")
            {
                jointList[2] = RobotChildren[i].gameObject;
                Joint2Limits.text = $"Joint 2: +{180.0f}, -{-180.0f}";
            }
            else if (RobotChildren[i].name == "Joint_4")
            {
                jointList[3] = RobotChildren[i].gameObject;
                Joint3Limits.text = $"Joint 3: +{180.0f}, -{-180.0f}";
            }
            else if (RobotChildren[i].name == "Joint_5")
            {
                jointList[4] = RobotChildren[i].gameObject;
                Joint4Limits.text = $"Joint 4: +{180.0f}, -{-180.0f}";
            }
            else if (RobotChildren[i].name == "Joint_6")
            {
                jointList[5] = RobotChildren[i].gameObject;
            }
        }
    }
}