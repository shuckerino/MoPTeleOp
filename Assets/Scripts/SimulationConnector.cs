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
    private TcpListener server;
    /// <summary> 
    /// Background thread for TcpServer workload. 	
    /// </summary> 	
    private Thread serverThread;
    /// <summary> 	
    /// Create handle to connected tcp client. 	
    /// </summary> 	
    private TcpClient connectedTcpClient;

    private const string MESSAGE_SEPARATOR = "$;$";
    public int connectionPort = 11002;
    private PhysicsSimulator physicsSimulator;
    private UR10Controller ur10Controller;

    public void StartSimulationServer()
    {
        physicsSimulator = FindObjectOfType<PhysicsSimulator>();
        ur10Controller = FindObjectOfType<UR10Controller>();

        serverThread = new Thread(new ThreadStart(ListenForIncommingRequests));
        serverThread.IsBackground = true;
        serverThread.Start();
    }

    /// <summary> 	
    /// Runs in background TcpServerThread; Handles incomming TcpClient requests 	
    /// </summary> 	
    private void ListenForIncommingRequests()
    {
        try
        {
            Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture; // set default culture
            server = new TcpListener(IPAddress.Parse("127.0.0.1"), connectionPort);
            server.Start();
            Debug.Log("Simulation is listening");
            Byte[] bytes = new Byte[1024];
            while (true)
            {
                using (connectedTcpClient = server.AcceptTcpClient())
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
                                string jointLimits = SynchronizeReceivedDataWithSimulation(messages[i]);
                                SendJointLimits(jointLimits);
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


    /// <summary>
    /// Running the simulation with the received data
    /// </summary>
    /// <param name="message"></param>
    /// <returns>Message that contains all joint limits</returns>
    private string SynchronizeReceivedDataWithSimulation(string message)
    {
        // take received joint values
        string[] jointValues = message.Split(',');
        float[] convertedJointValues = new float[jointValues.Length];
        ur10Controller.UpdateJointValues(jointValues.Select(float.Parse).ToArray());

        List<string> messages = new List<string>();

        try
        {
            foreach (var keyValuePair in physicsSimulator.GetLastSimulationResult())
            {
                messages.Add(string.Join(",", keyValuePair.Value));
            }
        }
        catch (Exception)
        {
            Debug.LogWarning("Collection was modified exception!");
        }


        // send result back
        string resultMessage = string.Join(MESSAGE_SEPARATOR, messages.ToArray());
        Debug.Log(resultMessage);
        return resultMessage;
    }

    /// <summary> 	
	/// Send message to client using socket connection. 	
	/// </summary> 	
	private void SendJointLimits(string message)
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

    void OnDestroy()
    {
        server?.Stop();
        serverThread?.Abort();
    }
}
