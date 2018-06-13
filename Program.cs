using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace MultiServer
{
    public class Player
    {
        public EndPoint PlayerIP;
        public string PlayerName;
        public int PlayerStatus;
        public int PlayerAnswer;

        public Player(EndPoint x, string y, int z, int p)
        {
            this.PlayerIP = x;
            this.PlayerName = y;
            this.PlayerStatus = z;
            this.PlayerAnswer = p;
        }
    }

    class Program
    {
        private static readonly Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static readonly List<Socket> clientSockets = new List<Socket>();
        private const int BUFFER_SIZE = 2048;
        private const int PORT = 100;
        private static readonly byte[] buffer = new byte[BUFFER_SIZE];
        public static List<Player> PlayerList = new List<Player>();
        Socket lama;
        private static int MaxPlayer = 3;
        static bool inprogress = false;
        static void Main()
        {
            Console.Title = "Server";
            SetupServer();
            Console.ReadLine();
            CloseAllSockets();
        }

        private static void SetupServer()
        {
            bool WaitingForPlayers = true;

            serverSocket.Bind(new IPEndPoint(IPAddress.Any, PORT));
            serverSocket.Listen(0);
            serverSocket.BeginAccept(AcceptCallback, null);
            IPAddress[] ipv4Addresses = Array.FindAll(
            Dns.GetHostEntry(string.Empty).AddressList,
            a => a.AddressFamily == AddressFamily.InterNetwork);
            while (WaitingForPlayers)
            {
                Console.Clear();
                Console.WriteLine("Server online");
                foreach (var item in ipv4Addresses)
                {
                    Console.WriteLine(item);
                }
                Console.WriteLine("Numer of ready players:" + PlayersStatusCount(1).ToString());
                Thread.Sleep(500);
                if (PlayersStatusCount(1) == MaxPlayer)
                {
                    WaitingForPlayers = false;
                    inprogress = true;
                    Console.WriteLine("Starting game");
                    ChangeStatusFromTo(1, 7);
                }
            }

            while (inprogress)
            {
                if (PlayersStatusCount(2) == PlayerList.Count)
                {
                    int rocks = PlayersAnswersCount(1);
                    int papers = PlayersAnswersCount(2);
                    int scissors = PlayersAnswersCount(3);


                    //warunki remisu i wygranej
                    if (rocks > 1)
                    {
                        ChangeStatusForAnswerTo(1, 3);
                        ChangeStatusForAnswerTo(2, 5);
                        ChangeStatusForAnswerTo(3, 4);
                    }
                    else if (papers > 1)
                    {
                        ChangeStatusForAnswerTo(1, 4);
                        ChangeStatusForAnswerTo(2, 3);
                        ChangeStatusForAnswerTo(3, 5);

                    }
                    else if (scissors > 1)
                    {
                        ChangeStatusForAnswerTo(1, 5);
                        ChangeStatusForAnswerTo(2, 4);
                        ChangeStatusForAnswerTo(3, 3);
                    }


                    //warunki przegranej
                    if (rocks > 0)
                    {
                        ChangeStatusForAnswerTo(3, 4);
                    }
                    if (papers > 0)
                    {
                        ChangeStatusForAnswerTo(1, 4);
                    }
                    if (scissors > 0)
                    {
                        ChangeStatusForAnswerTo(2, 4);
                    }
                    ChangeStatusFromTo(6, 5);
                    //inprogress = false;
                    //WaitingForPlayers = true;
                }
            }
        }

 

        static int PlayersStatusCount(int s)
        {
            int n = 0;
            for(int i = PlayerList.Count - 1; i >= 0; i--)
            {

                if (PlayerList[i].PlayerStatus == s)
                {
                    n++;
                }
            }
            return n;
        }

        static int PlayersAnswersCount(int a)
        {
            int n = 0;
            for (int i = PlayerList.Count - 1; i >= 0; i--)
            {
                if (PlayerList[i].PlayerAnswer == a)
                {
                    n++;
                }
            }
            return n;
        }

        static void ChangeStatusForAnswerTo(int pick, int to)
        {
            for (int i = PlayerList.Count - 1; i >= 0; i--)
            {
                if (PlayerList[i].PlayerAnswer == pick)
                {
                    PlayerList[i].PlayerStatus = to;
                }
            }
        }
        
        static void ChangeStatusFromTo(int from, int to)
        {
            for (int i = PlayerList.Count - 1; i >= 0; i--)
            {
                if (PlayerList[i].PlayerStatus == from)
                {
                    PlayerList[i].PlayerStatus = to;
                }
            }
        }

        
        private static void CloseAllSockets()
        {
            foreach (Socket socket in clientSockets)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }

            serverSocket.Close();
        }

        public static void AcceptCallback(IAsyncResult AR)
        {
            Socket socket;
            try
            {
                socket = serverSocket.EndAccept(AR);
            }
            catch (ObjectDisposedException)
            {
                return;
            }

           clientSockets.Add(socket);
           socket.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, socket);
           Console.WriteLine("Client connected");
           serverSocket.BeginAccept(AcceptCallback, null);
        }


        static String ChangeStatusForSocket(List<Player> PL, Socket t, int status)
        {
            string s = "";
                foreach (var P in PL)
                {
                    if (t.RemoteEndPoint.Equals(P.PlayerIP))
                    {
                    P.PlayerStatus = status;
                    s = P.PlayerName;
                    }
                }
            return s;
        }

        static string StatusforPlayer(List<Player> PL, Socket t)
        {
            string s = "";
            foreach (var P in PL)
            {
                if (t.RemoteEndPoint.Equals(P.PlayerIP))
                {
                    
                    s = P.PlayerStatus.ToString();
                }
            }
            return s;
        }

        static String ChangeAnswer(List<Player> PL, Socket t, int answer)
        {
            string s = "";
            foreach (var P in PL)
            {
                if (t.RemoteEndPoint.Equals(P.PlayerIP))
                {
                    P.PlayerAnswer = answer;
                    s = P.PlayerName;
                }
            }
            return s;
        }

        static void DelPlayer(ref List<Player> PL, Socket t)
        {

            for (int i = PL.Count - 1; i >= 0; i--)
            {
                if (t.RemoteEndPoint.Equals(PL[i].PlayerIP))
                {
                    PL.Remove(PL[i]);
                }
            }

        }


        public static void ReceiveCallback(IAsyncResult AR)
        {
            Socket current = (Socket)AR.AsyncState;
            int received;
            
            try
            {
                received = current.EndReceive(AR);
            }
            catch (SocketException)
            {
                Console.WriteLine("Client forcefully disconnected");
                DelPlayer(ref PlayerList, current);
                current.Close();
                clientSockets.Remove(current);
                return;
            }

            byte[] recBuf = new byte[received];
            Array.Copy(buffer, recBuf, received);
            string text = Encoding.ASCII.GetString(recBuf);
            //Console.WriteLine("Received Text: " + text);

            if (text.ToLower() == "exit") 
            {
                DelPlayer(ref PlayerList, current);
                current.Shutdown(SocketShutdown.Both);
                current.Close();
                clientSockets.Remove(current);
                Console.WriteLine("Player disconnected");
                return;
            }
            else if (text.ToLower() == "playernumber")
            {
                byte[] data = Encoding.ASCII.GetBytes(PlayerList.Count.ToString()+".");
                current.Send(data);
            }
            else if (text.ToLower() == "readynumber")
            {
                byte[] data = Encoding.ASCII.GetBytes(PlayersStatusCount(1).ToString()+",");
                current.Send(data);
            }
            else if(text.ToLower() == "ready")
            {
                if (inprogress)
                {
                    byte[] data = Encoding.ASCII.GetBytes("6");
                    current.Send(data);
                }
                else
                {
                    Console.WriteLine("Player " + ChangeStatusForSocket(PlayerList, current, 1) + " is ready.");
                }
            }
            else if (text.ToLower() == "1")
            {
                Console.WriteLine("Player "+ ChangeAnswer(PlayerList,current,1)+ " pick is rock");
                ChangeStatusForSocket(PlayerList, current, 2);
            }
            else if (text.ToLower() == "2")
            {
                Console.WriteLine("Player " + ChangeAnswer(PlayerList, current, 2) + " pick is paper");
                ChangeStatusForSocket(PlayerList, current, 2);
            }
            else if (text.ToLower() == "3")
            {
                Console.WriteLine("Player " + ChangeAnswer(PlayerList, current, 3) + " pick is scissors");
                ChangeStatusForSocket(PlayerList, current, 2);
            }
            else if(text.ToLower() == "status")
            {
                byte[] data = Encoding.ASCII.GetBytes(StatusforPlayer(PlayerList,current));
                current.Send(data);
            }
            else if (text.ToLower() == "playercount")
            {
                byte[] data = Encoding.ASCII.GetBytes(PlayerList.Count.ToString());
                current.Send(data);
            }
            else if (text.ToLower() == "readycount")
            {
                byte[] data = Encoding.ASCII.GetBytes(PlayersStatusCount(2).ToString());
                current.Send(data);
            }
            else
            {
                Player NewPlayer = new Player(current.RemoteEndPoint, text, 0,0);
                PlayerList.Add(NewPlayer);
            }

            current.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, current);
        }




    }
}
