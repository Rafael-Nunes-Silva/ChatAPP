using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;

class Client
{
    static TcpClient tcpConn = new TcpClient();
    static void Main(string[] args)
    {
        Console.Write("type in your name: ");
        string name = Console.ReadLine();

        connect:
        try
        {
            Console.Write("Type in server IP: ");
            string ip = Console.ReadLine();
            Console.Write("Type in server port: ");
            int port = Convert.ToInt32(Console.ReadLine());

            Console.WriteLine("Trying to connect...");
            tcpConn.Connect(System.Net.IPAddress.Parse(ip), port);
            SendMessage(name);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            goto connect;
        }
        Console.WriteLine("Connected");

        if (tcpConn.Connected)
            Task.Run(ListenForMessages);

        while (tcpConn.Connected)
        {
            string input = Console.ReadLine();
            if (input.ToLower() == "-disconnect")
            {
                tcpConn.Close();
                break;
            }

            SendMessage(input);
        }
        Console.WriteLine("Connection ended");

        Console.Write("Try again?(Y/n): ");
        if (Console.ReadLine().ToLower() == "y")
            goto connect;
    }

    static void SendMessage(string msg)
    {
        try
        {
            Byte[] sentMsgData = Encoding.UTF8.GetBytes(msg);
        tcpConn.GetStream().Write(sentMsgData, 0, sentMsgData.Length);
        tcpConn.GetStream().Flush();
        }
        catch (Exception e)
        {
            // Console.WriteLine(e);
            Console.WriteLine("Connection Lost");
        }
    }

    static void ListenForMessages()
    {
        while (tcpConn.Connected)
        {
            try
            {
                Byte[] receivedMsgData = new Byte[256];
                int len = tcpConn.GetStream().Read(receivedMsgData, 0, receivedMsgData.Length);
                if (len > 0)
                    Console.WriteLine(Encoding.UTF8.GetString(receivedMsgData).Substring(0, len));
            }
            catch (Exception e)
            {
                // Console.WriteLine(e);
                Console.WriteLine("Connection Lost");
            }
        }
    }
}