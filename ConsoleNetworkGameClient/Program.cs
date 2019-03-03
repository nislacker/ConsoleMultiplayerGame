using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleNetworkGameClient
{

    class Program
    {
        class Player
        {
            public int X { get; set; }
            public int Y { get; set; }
            public char Sprite { get; private set; }
            public ConsoleColor Color { get; private set; }
            public int ID { get; private set; }

            public Player(int x, int y, char sprite, ConsoleColor color, int id)
            {
                X = x;
                Y = y;
                Sprite = sprite;
                Color = color;
                ID = id;
            }

            public void Draw()
            {
                Console.ForegroundColor = Color;
                Console.SetCursorPosition(X, Y);
                Console.Write(Sprite);
                Console.ResetColor();
            }

            public void Remove()
            {
                Console.SetCursorPosition(X, Y);
                Console.Write(' ');
            }
        }

        static Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        static MemoryStream ms = new MemoryStream(new byte[256], 0, 256, true, true);
        static BinaryWriter writer = new BinaryWriter(ms);
        static BinaryReader reader = new BinaryReader(ms);

        static List<Player> players = new List<Player>();

        static Random random = new Random();

        static Player player;

        enum PacketInfo
        {
            ID, Position
        }

        static void Main(string[] args)
        {
            Console.Title = "Client";
            Console.CursorVisible = false;

            Console.WriteLine("Подключение к серверу...");
            socket.Connect("127.0.0.1", 2048);
            Console.WriteLine("Подключено");
            Thread.Sleep(1000);
            Console.Clear();

            Console.Write("Введите спрайт: ");
            char spr = Convert.ToChar(Console.ReadLine());
            Console.Clear();

            Console.WriteLine("Выберите цвет:");
            for (int i = 0; i <= 14; i++)
            {
                Console.ForegroundColor = (ConsoleColor)i;
                Console.WriteLine(i);
            }
            Console.ResetColor();
            ConsoleColor clr = (ConsoleColor)int.Parse(Console.ReadLine());
            Console.Clear();

            int x = random.Next(1, 5);
            int y = random.Next(1, 5);

            Console.WriteLine("Получение идентификатора");
            SendPacket(PacketInfo.ID);
            int id = ReceivePacket();
            Console.WriteLine("Получен ID : " + id);
            Thread.Sleep(1000);
            Console.Clear();

            player = new Player(x, y, spr, clr, id);
            SendPacket(PacketInfo.Position);

            Task.Run(() => { while (true) ReceivePacket(); });

            while (true)
            {
                switch (Console.ReadKey(true).Key)
                {
                    case ConsoleKey.LeftArrow: player.Remove(); player.X--; SendPacket(PacketInfo.Position); goto case 252;
                    case ConsoleKey.RightArrow: player.Remove(); player.X++; goto case 252;
                    case ConsoleKey.UpArrow: player.Remove(); player.Y--; goto case 252;
                    case ConsoleKey.DownArrow: player.Remove(); player.Y++; goto case 252;

                    case (ConsoleKey)252:
                        player.Draw();
                        SendPacket(PacketInfo.Position);
                        break;
                }
            }
        }

        static void SendPacket(PacketInfo info)
        {
            ms.Position = 0;

            switch (info)
            {
                case PacketInfo.ID:
                    writer.Write(0);
                    socket.Send(ms.GetBuffer());
                    break;
                case PacketInfo.Position:
                    writer.Write(1);
                    writer.Write(player.ID);
                    writer.Write(player.X);
                    writer.Write(player.Y);
                    writer.Write(player.Sprite);
                    writer.Write((int)player.Color);
                    socket.Send(ms.GetBuffer());
                    break;
                default:
                    break;
            }
        }

        static int ReceivePacket()
        {
            ms.Position = 0;

            socket.Receive(ms.GetBuffer());

            int code = reader.ReadInt32();

            int id;
            int x;
            int y;
            char sprite;
            ConsoleColor color;

            switch (code)
            {
                case 0: return reader.ReadInt32();

                case 1:
                    id = reader.ReadInt32();
                    x = reader.ReadInt32();
                    y = reader.ReadInt32();

                    Player plr = players.Find(p => p.ID == id);

                    if (plr != null)
                    {
                        plr.Remove();
                        plr.X = x;
                        plr.Y = y;
                        plr.Draw();
                    }
                    else
                    {
                        sprite = reader.ReadChar();
                        color = (ConsoleColor)reader.ReadInt32();
                        plr = new Player(x, y, sprite, color, id);
                        players.Add(plr);
                        plr.Draw();
                    }
                    break;
            }

            return -1;
        }
    }
}
