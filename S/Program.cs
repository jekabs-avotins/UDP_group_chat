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
        consoleThread.Start(); // Start the console listener in a separate thread

        try
        {
            while (running) // Use the running flag to control the loop
            {
                if (udpServer.Available > 0) // Check if data is available to avoid blocking
                {
                    IPEndPoint remoteEP = null;
                    byte[] data = udpServer.Receive(ref remoteEP); // Block until data arrives
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
        if (message.StartsWith("JOIN "))
        {
            string userName = message.Substring(5);
            clients[sender] = userName;
            Console.WriteLine($"{userName} joined the chat.");
            SendMessage("SERVER: " + userName + " has joined the chat.", sender);
        }
        else if (message.StartsWith("LEAVE"))
        {
            if (clients.ContainsKey(sender))
            {
                string userName = clients[sender];
                clients.Remove(sender);
                Console.WriteLine($"{userName} has left the chat.");
                SendMessage("SERVER: " + userName + " has left the chat.", sender);
            }
        }
        else if (message.StartsWith("MSG "))
        {
            string userName = clients.ContainsKey(sender) ? clients[sender] : "Unknown";
            string userMessage = message.Substring(4);
            Console.WriteLine($"{userName}: {userMessage}");
            SendMessage(userName + ": " + userMessage, sender);
        }
    }

    private void SendMessage(string message, IPEndPoint sender)
    {
        byte[] buffer = Encoding.ASCII.GetBytes(message);
        foreach (var client in clients)
        {
            if (!client.Key.Equals(sender)) // do not send the message back to the sender
            {
                udpServer.Send(buffer, buffer.Length, client.Key);
            }
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


