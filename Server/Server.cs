using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;

public class Client
{
    string name;
    TcpClient tcpConn;
    NetworkStream stream;

    Task listenTask;
    Action<string, string> listenCallback;
    bool listen = false;

    public Client(TcpClient connection)
    {
        tcpConn = connection;
        stream = tcpConn.GetStream();

        Byte[] receivedMsgData = new Byte[256];
        int len = tcpConn.GetStream().Read(receivedMsgData, 0, receivedMsgData.Length);
        name = Encoding.UTF8.GetString(receivedMsgData).Substring(0, len);
    }

    public string GetName()
    {
        return name;
    }

    public bool IsConnected()
    {
        return SendMsg("");
    }
    public void Disconnect()
    {
        tcpConn.Close();
    }

    public bool SendMsg(string msg)
    {
        try
        {
            Byte[] sentMsgData = Encoding.UTF8.GetBytes(msg);
            stream.Write(sentMsgData, 0, sentMsgData.Length);
            stream.Flush();
        }
        catch (Exception e)
        {
            return false;
        }
        return true;
    }

    public void StartListening(Action<string, string> callback)
    {
        listenCallback = callback;
        listen = true;
        listenTask = new Task(ListenFunction);
        listenTask.Start();
    }
    public void StopListening()
    {
        listen = false;
    }
    
    void ListenFunction()
    {
        while (listen)
        {
            try
            {
                Byte[] receivedMsgData = new Byte[256];
                int len = tcpConn.GetStream().Read(receivedMsgData, 0, receivedMsgData.Length);
                if (len <= 0)
                    continue;

                string msg = Encoding.UTF8.GetString(receivedMsgData).Substring(0, len);
                listenCallback(name, msg);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}

class Server
{
    static TcpListener listener;
    static List<Client> tcpConns = new List<Client>(0);
    static int maxConns = 16;

    static bool running = true;
    static string name = "Server";

    static Dictionary<string, Action> commands = new Dictionary<string, Action>()
    {
        { "-help", () => { foreach(var command in commands) Console.WriteLine(command.Key); } },
        { "-shutdown", () => {
            running = false;
            for (int i = 0; i < tcpConns.Count; i++)
            {
                tcpConns[i].Disconnect();
                tcpConns.RemoveAt(i);
            }
        } },
        { "-countclients", () => { Console.WriteLine($"{tcpConns.Count} users connected"); } },
        { "-listclients", () => {
            Console.WriteLine("Users connected:");
            for (int i = 0; i < tcpConns.Count; i++)
                Console.WriteLine(tcpConns[i].GetName());
        } },
        { "-showname", () => { Console.WriteLine(name); } },
        { "-changename", () => {
            Console.Write("Type in the new name: ");
            name = Console.ReadLine();
        } },
        { "-setmaxconns", () =>
        {
            Console.Write("Type in the max ammount of connections permited(2-128): ");
            try{
                maxConns = Math.Min(Math.Max(Convert.ToInt32(Console.ReadLine()), 2), 128);
            }
            catch(Exception e)
            {
                Console.WriteLine("Invalid value");
                // Console.WriteLine(e);
            }
        } }
    };

    static void Main(string[] args)
    {
        getport:
        Console.Write("Port: ");
        int port = 6778;
        try
        {
            port = Convert.ToInt32(Console.ReadLine());
        }
        catch(Exception e)
        {
            Console.WriteLine(e);
            goto getport;
        }

        listener = new TcpListener(System.Net.IPAddress.Any, port);

        Console.WriteLine("Server started...");
        Task.Run(ListenForConnection);

        while (running)
        {
            string input = Console.ReadLine();
            bool command = true;
            try
            {
                commands[input].Invoke();
            }
            catch(Exception e)
            {
                // Console.WriteLine(e);
                command = false;
            }

            if (!running)
                break;

            if (command)
                continue;

            for (int i = 0; i < tcpConns.Count; i++)
                SendMessage(tcpConns[i], $"{name}: {input}");
        }
        Console.WriteLine("Server closed");

        listener.Stop();
    }

    static void SendMessage(Client client, string msg)
    {
        if (!client.SendMsg(msg))
            tcpConns.Remove(client);
    }

    static void ListenForConnection()
    {
        listener.Start();
        while (running)
        {
            try
            {
                Client newClient = new Client(listener.AcceptTcpClient());
                tcpConns.Add(newClient);
                newClient.StartListening(ReceivedMessage);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }

    static void ReceivedMessage(string name, string msg)
    {
        for (int i = 0; i < tcpConns.Count; i++)
        {
            if (tcpConns[i].GetName() == name)
                continue;
            SendMessage(tcpConns[i], $"{name}: {msg}");
        }
        Console.WriteLine($"{name}: {msg}");
    }
}