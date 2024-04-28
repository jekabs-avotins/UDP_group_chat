using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class Client
{
    private UdpClient udpClient;
    private string userName;
    private int serverPort = 11000;
    private IPEndPoint serverEP;

    public Client(string userName)
    {
        this.userName = userName;
        udpClient = new UdpClient();
        serverEP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), serverPort);
    }

    public void Start()
    {
        SendJoinRequest();
        Thread receiveThread = new Thread(ReceiveMessages);
        receiveThread.Start();

        Console.WriteLine("Enter messages (type 'exit' to quit):");
        while (true)
        {
            string message = Console.ReadLine();
            if (message.ToLower() == "exit")
            {
                SendLeaveNotification();
                break;
            }
            SendMessage(message);
        }
        udpClient.Close();
    }

    private void SendJoinRequest()
    {
        string message = "JOIN " + userName;
        byte[] buffer = Encoding.ASCII.GetBytes(message);
        udpClient.Send(buffer, buffer.Length, serverEP);
    }

    private void SendLeaveNotification()
    {
        string message = "LEAVE";
        byte[] buffer = Encoding.ASCII.GetBytes(message);
        udpClient.Send(buffer, buffer.Length, serverEP);
    }

    private void SendMessage(string message)
    {
        string formattedMessage = "MSG " + message;
        byte[] buffer = Encoding.ASCII.GetBytes(formattedMessage);
        udpClient.Send(buffer, buffer.Length, serverEP);
    }

    private void ReceiveMessages()
    {
        try
        {
            while (true)
            {
                IPEndPoint from = new IPEndPoint(IPAddress.Any, 0);
                byte[] receivedBytes = udpClient.Receive(ref from);
                string receivedMessage = Encoding.ASCII.GetString(receivedBytes);
                if (from.Equals(serverEP))
                {
                    Console.WriteLine(receivedMessage);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Disconnected from server: " + ex.Message);
        }
    }
}

class Program
{
    static void Main(string[] args)
    {
        Console.Write("Enter your username: ");
        string userName = Console.ReadLine();
        Client client = new Client(userName);
        client.Start();
    }
}
