using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Threading;

public class Server
{
    private UdpClient udpServer;
    private int listenPort = 11000;
    private Dictionary<IPEndPoint, string> clients;
    private bool running = true; // Flag to control the running of the server loop

    public Server()
    {
        udpServer = new UdpClient(listenPort);
        clients = new Dictionary<IPEndPoint, string>();
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
        string[] parts = message.Split(new char[] { ':' }, 2);
        int msgId = int.Parse(parts[0]);
        string msgContent = parts[1];

        // Send ACK back to sender
        SendAck(msgId, sender);

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

    private void SendMessage(string message, IPEndPoint sender)
    {
        byte[] buffer = Encoding.ASCII.GetBytes(message);
        foreach (var client in clients)
        {
            
            udpServer.Send(buffer, buffer.Length, client.Key);
            
        }
    }

    private void SendAck(int messageId, IPEndPoint sender)
    {
        string ackMessage = $"ACK:{messageId}";
        byte[] ackBuffer = Encoding.ASCII.GetBytes(ackMessage);
        udpServer.Send(ackBuffer, ackBuffer.Length, sender);
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


