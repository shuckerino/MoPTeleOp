using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;
using System.Linq;
using System.IO;
using System.Net.Http;
using UnityEngine.Analytics;
using System.Threading.Tasks;

public class PythonServerConnector : MonoBehaviour
{
    #region Member

    private TcpClient tcpClient;
    private NetworkStream networkStream;
    private StreamReader reader;
    private StreamWriter writer;
    private Thread receiveThread;

    // The IP and Port of the Python server
    public string serverIP = "127.0.0.1";  // Python server IP
    public int serverPort = 11002;         // Python server port

    private const string MESSAGE_SEPARATOR = "$;$";
    private PhysicsSimulator physicsSimulator;
    private UR10Controller ur10Controller;

    #endregion

    #region Connect

    public void ConnectToPythonServer()
    {
        physicsSimulator = FindObjectOfType<PhysicsSimulator>();
        ur10Controller = FindObjectOfType<UR10Controller>();

        try
        {
            tcpClient = new TcpClient(serverIP, serverPort);
            networkStream = tcpClient.GetStream();
            reader = new StreamReader(networkStream);
            writer = new StreamWriter(networkStream);

            // Start a new thread to listen for messages from the server
            receiveThread = new Thread(new ThreadStart(ReceiveMessagesFromPythonServer));
            receiveThread.IsBackground = true;
            receiveThread.Start();

            Debug.Log("Connected to Python server.");
        }
        catch (Exception e)
        {
            Debug.LogError("Error connecting to server: " + e.Message);
        }

    }

    #endregion

    #region Receive

    void ReceiveMessagesFromPythonServer()
    {
        while (true)
        {
            //if (physicsSimulator.GetLastSimulationResult().Count == 0) continue;

            try
            {
                if (tcpClient.Available > 0)
                {
                    string message = reader.ReadLine();
                    if (!string.IsNullOrEmpty(message))
                    {
                        Debug.Log("Received: " + message);
                        string[] messages = message.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                        //if (physicsSimulator != null && physicsSimulator.IsNewResultAvailable())
                        //{
                        for (int i = 0; i < messages.Length; i++)
                        {
                            string jointLimits = SynchronizeReceivedDataWithSimulation(messages[i]);
                            SendJointLimits(jointLimits);
                        }
                        //}
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error receiving message: " + e.Message);
            }
        }
    }

    #endregion

    #region Run simulation

    /// <summary>
    /// Running the simulation with the received data
    /// </summary>
    /// <param name="message"></param>
    /// <returns>Message that contains all joint limits</returns>
    private string SynchronizeReceivedDataWithSimulation(string message)
    {
        Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture; // set default culture
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

    #endregion

    #region Send

    /// <summary> 	
    /// Send message to client using socket connection. 	
    /// </summary> 	
    private void SendJointLimits(string message)
    {
        if (tcpClient == null)
        {
            return;
        }

        try
        {
            // Get a stream object for writing. 			
            if (networkStream.CanWrite)
            {
                string serverMessage = string.IsNullOrEmpty(message) ? "Empty message" : message;

                // Convert string message to byte array.
                byte[] serverMessageAsByteArray = Encoding.ASCII.GetBytes(serverMessage);

                // Prepend the message length as a 4-byte integer.
                int messageLength = serverMessageAsByteArray.Length;
                byte[] messageLengthBytes = BitConverter.GetBytes(messageLength);

                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(messageLengthBytes); // Ensure big-endian format for cross-platform compatibility.
                }

                // Combine the length header and the message.
                byte[] dataToSend = new byte[messageLengthBytes.Length + serverMessageAsByteArray.Length];
                Buffer.BlockCopy(messageLengthBytes, 0, dataToSend, 0, messageLengthBytes.Length);
                Buffer.BlockCopy(serverMessageAsByteArray, 0, dataToSend, messageLengthBytes.Length, serverMessageAsByteArray.Length);

                // Write the data to the network stream.
                networkStream.Write(dataToSend, 0, dataToSend.Length);

            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("Socket exception: " + socketException);
        }
    }

    #endregion

    #region Disconnect

    void OnApplicationQuit()
    {
        if (tcpClient != null && tcpClient.Connected)
        {
            reader.Close();
            writer.Close();
            networkStream.Close();
            tcpClient.Close();
        }

        receiveThread?.Abort();
    }

    #endregion

}
