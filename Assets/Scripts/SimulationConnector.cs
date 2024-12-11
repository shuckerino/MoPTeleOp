using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;
using System.Linq;

public class SimulationConnector : MonoBehaviour
{
    /// <summary> 	
    /// TCPListener to listen for incomming TCP connection 	
    /// requests. 	
    /// </summary> 	
    private TcpListener tcpListener;
    /// <summary> 
    /// Background thread for TcpServer workload. 	
    /// </summary> 	
    private Thread tcpListenerThread;
    /// <summary> 	
    /// Create handle to connected tcp client. 	
    /// </summary> 	
    private TcpClient connectedTcpClient;

    private const string MESSAGE_SEPARATOR = "$;$";
    public int connectionPort = 11002;
    private PhysicsSimulator physicsSimulator;

    public void StartSimulationServer()
    {
        tcpListenerThread = new Thread(new ThreadStart(ListenForIncommingRequests));
        tcpListenerThread.IsBackground = true;
        tcpListenerThread.Start();
    }

    /// <summary> 	
    /// Runs in background TcpServerThread; Handles incomming TcpClient requests 	
    /// </summary> 	
    private void ListenForIncommingRequests()
    {
        try
        {
            Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture; // set default culture
            tcpListener = new TcpListener(IPAddress.Parse("127.0.0.1"), connectionPort);
            tcpListener.Start();
            Debug.Log("Simulation is listening");
            Byte[] bytes = new Byte[1024];
            while (true)
            {
                using (connectedTcpClient = tcpListener.AcceptTcpClient())
                {
                    // Get a stream object for reading 					
                    using (NetworkStream stream = connectedTcpClient.GetStream())
                    {
                        int length;
                        // Read incomming stream into byte arrary. 						
                        while ((length = stream.Read(bytes, 0, bytes.Length)) != 0)
                        {
                            var incommingData = new byte[length];
                            Array.Copy(bytes, 0, incommingData, 0, length);
                            // Convert byte array to string message. 							
                            string clientMessage = Encoding.ASCII.GetString(incommingData);
                            Debug.Log("client message received as: " + clientMessage);
                            string[] messages = clientMessage.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                            for (int i = 0; i < messages.Length; i++)
                            {
                                string reply = HandleClientMessage(messages[i]);
                                SendServerMessage(reply);
                            }
                        }
                    }
                }
            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("SocketException " + socketException.ToString());
        }
    }


    private string HandleClientMessage(string message)
    {
        physicsSimulator = FindObjectOfType<PhysicsSimulator>();

        // take received joint values
        string[] jointValues = message.Split(',');
        float[] convertedJointValues = new float[jointValues.Length];
        for (int i = 0; i < jointValues.Length; i++)
        {
            if (float.TryParse(jointValues[i], out float res))
            {
                convertedJointValues[i] = res;
            }
            else
            {
                Debug.Log($"Error while converting joint angle {jointValues[i]}");
            }
        }

        // run simulation
        Dictionary<int, List<float>> collisionAngles = physicsSimulator.SimulateJointAngles(convertedJointValues);
        List<string> messages = new List<string>();
        foreach (var keyValuePair in collisionAngles)
        {
            messages.Add(string.Join(",", keyValuePair.Value));
        }

        // send result back
        string resultMessage = string.Join(MESSAGE_SEPARATOR, messages.ToArray());
        Debug.Log(resultMessage);
        return resultMessage;
    }

    /// <summary> 	
	/// Send message to client using socket connection. 	
	/// </summary> 	
	private void SendServerMessage(string message)
    {
        if (connectedTcpClient == null)
        {
            return;
        }

        try
        {
            // Get a stream object for writing. 			
            NetworkStream stream = connectedTcpClient.GetStream();
            if (stream.CanWrite)
            {
                string serverMessage = message;
                // Convert string message to byte array.                 
                byte[] serverMessageAsByteArray = Encoding.ASCII.GetBytes(serverMessage);
                // Write byte array to socketConnection stream.               
                stream.Write(serverMessageAsByteArray, 0, serverMessageAsByteArray.Length);
                Debug.Log("Server sent his message - should be received by client");
            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("Socket exception: " + socketException);
        }
    }
}
