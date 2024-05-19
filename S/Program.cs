using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using System.Timers;

public class Server
{
    private UdpClient udpServer;
    private int listenPort = 11000;
    private Dictionary<IPEndPoint, string> clients= new Dictionary<IPEndPoint, string>();
    private Dictionary<int, (string, IPEndPoint)> pendingMessages = new Dictionary<int, (string, IPEndPoint)>();
    private bool running = true; // Flag to control the running of the server loop
    private System.Timers.Timer retryTimer;
    private Random random;
    private int messageSId = 0;

    public Server()
    {
        udpServer = new UdpClient(listenPort);
        clients = new Dictionary<IPEndPoint, string>();
        random = new Random();
        retryTimer = new System.Timers.Timer(3000); // Using System.Timers.Timer
        retryTimer.Elapsed += HandleRetry;
        retryTimer.AutoReset = true; // Ensure the timer fires repeatedly
    }

    public void Start()
    {
        Console.WriteLine("Server started...");
        Thread consoleThread = new Thread(new ThreadStart(ConsoleListener));
        consoleThread.Start();

        try
        {
            while (running)
            {
                if (udpServer.Available > 0)
                {
                    IPEndPoint remoteEP = null;
                    byte[] data = udpServer.Receive(ref remoteEP);
                    HandleMessage(data, remoteEP);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred: " + ex.Message);
        }
        finally
        {
            udpServer.Close();
            Console.WriteLine("Server shut down.");
        }
    }

    private void ConsoleListener()
    {
        while (running)
        {
            string command = Console.ReadLine();
            if (command.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                running = false;
            }
        }
    }

    private void HandleMessage(byte[] data, IPEndPoint sender)
    {
        string message = Encoding.ASCII.GetString(data);
        Console.WriteLine($"Receive: {message}");
        
        // Send ACK back to sender
        if(message.StartsWith("ACK:"))
        {
            string[] parts = message.Split(new char[] { ':' }, 2);
            int msgContent = int.Parse(parts[1]);
            HandleAck(msgContent);
        }

        if(!message.StartsWith("ACK:"))
        {
            
            string[] parts = message.Split(new char[] { ':' }, 2);
            int msgId = int.Parse(parts[0]);
            string msgContent = parts[1];

            SendAck(msgId, sender);
            Console.WriteLine("--------------------------");
            if (msgContent.StartsWith("JOIN "))
            {
                string userName = msgContent.Substring(5);
                if (!clients.ContainsKey(sender))
                {
                    clients[sender] = userName;
                    Console.WriteLine($"{userName} joined the chat.");
                    SendMessage("SERVER: " + userName + " has joined the chat.", sender);
                }
            }
            else if (msgContent.StartsWith("LEAVE"))
            {
                if (clients.ContainsKey(sender))
                {
                    string userName = clients[sender];
                    clients.Remove(sender);
                    Console.WriteLine($"{userName} has left the chat.");
                    SendMessage("SERVER: " + userName + " has left the chat.", sender);
                }
            }
            else if (msgContent.StartsWith("MSG "))
            {
                string userName = clients.ContainsKey(sender) ? clients[sender] : "Unknown";
                string userMessage = msgContent.Substring(4);
                Console.WriteLine($"{userName}: {userMessage}");
                SendMessage(userName + ": " + userMessage, sender);
            
            }
        }
    }

    private void SendMessage(string messageContent, IPEndPoint sender)
    {
        foreach (var client in clients)
        {
            string message = $"{messageSId}:{messageContent}";
            byte[] buffer = Encoding.ASCII.GetBytes(message);
            udpServer.Send(buffer, buffer.Length, client.Key);
            Console.WriteLine($"Send: {message}");
            
            pendingMessages[messageSId] = (message, client.Key);
            messageSId++;
            
        }
        
    }

    private void SendAck(int messageId, IPEndPoint sender)
    {
        if (random.Next(3) == 0) // 50% chance to send or drop
        {
            string ackMessage = $"ACK:{messageId}";
            Console.WriteLine($"SENDACK: {ackMessage}");
            byte[] ackBuffer = Encoding.ASCII.GetBytes(ackMessage);
            udpServer.Send(ackBuffer, ackBuffer.Length, sender);
        }
    }
    private void HandleAck(int ackMessage)
    {

        if (pendingMessages.ContainsKey(ackMessage))
        {
            pendingMessages.Remove(ackMessage);
        }
        Console.WriteLine(pendingMessages.Count);
        if (pendingMessages.Count == 0)
        {
            retryTimer.Stop();
        }
    }

    private void HandleRetry(object sender, ElapsedEventArgs e)
    {
        foreach (var client in clients)
        {
            foreach (KeyValuePair<int, (string, IPEndPoint)> messageData in pendingMessages)
            {
                int messageId = messageData.Key;
                string message = messageData.Value.Item1;
                IPEndPoint clientKey = client.Key;

                byte[] buffer = Encoding.ASCII.GetBytes(message);
                udpServer.Send(buffer, buffer.Length, client.Key);
                Console.WriteLine($"Resending message to {client.Key}: " + message);
            }
        }
    }


    private void Retry()
    {
        if (!retryTimer.Enabled)
        {
            Console.WriteLine("Retry start");
            retryTimer.Stop();
            retryTimer.Start();
        }
        else
        {
            Console.WriteLine("Retry start");
            retryTimer.Start();
        }
    }
}


class Program
{
    static void Main(string[] args)
    {
        Server server = new Server();
        server.Start();
    }
}


