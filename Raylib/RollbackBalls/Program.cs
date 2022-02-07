using Raylib_cs;
using MessagePack;
using PleaseResync;
using System;

namespace RollbackBalls
{
    public class Program
    {
        public const int ScreenWidth = 600;
        public const int ScreenHeight = 600;
        public const uint INPUT_SIZE = 1;
        public const ushort FRAME_DELAY = 1;
        static void Main(string[] args)
        {
            Console.WriteLine("0 = Offline, !0 = Online:");
            ushort gameMode = Convert.ToUInt16(Console.ReadLine());

            if (gameMode == 0)
            {
                RunOfflineGame();
            }
            else
            {
                Console.WriteLine("Local Device Num:");
                ushort localDevice = Convert.ToUInt16(Console.ReadLine());

                Console.WriteLine("Remote Device Num:");
                ushort remoteDevice = Convert.ToUInt16(Console.ReadLine());

                Console.WriteLine("Local Port:");
                ushort localPort = Convert.ToUInt16(Console.ReadLine());

                Console.WriteLine("Remote Port:");
                ushort remotePort = Convert.ToUInt16(Console.ReadLine());

                RunOnlineGame(localDevice, remoteDevice, localPort, remotePort);
            }
        }

        private static void RunOfflineGame()
        {
            Raylib.InitWindow(ScreenWidth, ScreenHeight, "RollbackBalls");

            var gamestate = new Gamestate(2);

            double oldTime = 0.0, accumulator = 0.0;

            while (!Raylib.WindowShouldClose())
            {
                double deltaTime = Raylib.GetTime() - oldTime;
                oldTime = Raylib.GetTime();
                accumulator += deltaTime;

                while (accumulator > 1.0 / 61.0)
                {
                    // execute each action
                    gamestate.Update(new byte[] { GetLocalInput(), 0 });
                    accumulator -= 1.0 / 59.0;
                    if (accumulator < 0) accumulator = 0;
                }
                //render the game
                RenderGame(gamestate);
            }
            Raylib.CloseWindow();
        }

        private static void RunOnlineGame(ushort localId, ushort remoteId, ushort localPort, ushort remotePort)
        {
            Raylib.InitWindow(ScreenWidth, ScreenHeight, "RollbackBalls");

            var gamestate = new Gamestate(2);

            uint localDeviceId = localId;
            uint remoteDeviceId = remoteId;

            var adapter = new UdpSessionAdapter(localPort);
            var session = new Peer2PeerSession(INPUT_SIZE, 2, 2, adapter);

            session.SetLocalDevice(localDeviceId, 1, FRAME_DELAY);
            session.AddRemoteDevice(remoteDeviceId, 1, UdpSessionAdapter.CreateRemoteConfig("127.0.0.1", remotePort));

            double oldTime = 0.0, accumulator = 0.0;

            while (!Raylib.WindowShouldClose())
            {
                double deltaTime = Raylib.GetTime() - oldTime;
                oldTime = Raylib.GetTime();
                accumulator += deltaTime;

                while (accumulator > 1.0 / 61.0)
                {
                    session.Poll();

                    if (session.IsRunning())
                    {
                        var actions = session.AdvanceFrame(new byte[] { GetLocalInput() });
                        // execute each action
                        foreach (var action in actions)
                        {
                            switch (action)
                            {
                                case SessionAdvanceFrameAction AFAction:
                                    gamestate.Update(AFAction.Inputs);
                                    break;
                                case SessionLoadGameAction LGAction:
                                    gamestate = MessagePackSerializer.Deserialize<Gamestate>(LGAction.Load());
                                    break;
                                case SessionSaveGameAction SGAction:
                                    SGAction.Save(MessagePackSerializer.Serialize(gamestate));
                                    break;
                            }
                        }
                    }
                    accumulator -= 1.0 / 59.0;
                    if (accumulator < 0) accumulator = 0;
                }
                //render the game
                RenderGame(gamestate);
            }
            adapter.Close();
            Raylib.CloseWindow();
        }

        private static byte GetLocalInput()
        {
            var input = new PlayerInput();

            if (Raylib.IsKeyDown(KeyboardKey.KEY_W)) input.SetInputBit(0, true);
            if (Raylib.IsKeyDown(KeyboardKey.KEY_S)) input.SetInputBit(1, true);
            if (Raylib.IsKeyDown(KeyboardKey.KEY_A)) input.SetInputBit(2, true);
            if (Raylib.IsKeyDown(KeyboardKey.KEY_D)) input.SetInputBit(3, true);

            return input.InputState;
        }

        private static void RenderGame(Gamestate gamestate)
        {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.BLACK);
            Raylib.DrawText(Raylib.GetFPS().ToString(), 50, 50, 30, Color.WHITE);

            for (int i = 0; i < gamestate.Players.Length; i++)
            {
                var player = gamestate.Players[i];
                Raylib.DrawCircle(player.Position.X, player.Position.Y, 50, Color.SKYBLUE);
                Raylib.DrawText(i.ToString(), player.Position.X, player.Position.Y, 20, Color.BLACK);
            }

            Raylib.EndDrawing();
        }
    }

    [MessagePackObject]
    public class Gamestate
    {
        [Key(0)]
        public int Frame;
        [Key(1)]
        public Player[] Players;

        public Gamestate(int playercount)
        {
            Players = new Player[playercount];

            for (int i = 0; i < playercount; i++)
            {
                Players[i] = new Player();
            }
        }

        public Gamestate(int frame, Player[] players)
        {
            Frame = frame;
            Players = players;
        }

        public void Update(byte[] playerInput)
        {
            Frame++;
            MovePlayer(playerInput);
            ProcessMotion();
            ScreenWrap();
        }

        private void MovePlayer(byte[] playerInput)
        {
            for (int i = 0; i < Players.Length; i++)
            {
                var player = Players[i];

                var dir = new IntVec2();
                var input = new PlayerInput(playerInput[i]);

                if (input.IsInputBitSet(0))
                    dir.Y += -1;
                if (input.IsInputBitSet(1))
                    dir.Y += 1;
                if (input.IsInputBitSet(2))
                    dir.X += -1;
                if (input.IsInputBitSet(3))
                    dir.X += 1;

                dir.Norm();

                if (dir.X != 0)
                {
                    player.Velocity.X = dir.X * 10;
                }

                if (dir.Y != 0)
                {
                    player.Velocity.Y = dir.Y * 10;
                }
            }
        }

        private void ProcessMotion()
        {
            foreach (var player in Players)
            {
                player.Velocity.X += player.Acceleration.X;
                player.Velocity.Y += player.Acceleration.Y;

                player.Position.X += player.Velocity.X;
                player.Position.Y += player.Velocity.Y;
            }
        }

        private void ScreenWrap()
        {
            foreach (var player in Players)
            {
                if (player.Position.X > Program.ScreenWidth)
                    player.Position.X = 0;
                if (player.Position.X < 0)
                    player.Position.X = Program.ScreenWidth;
                if (player.Position.Y > Program.ScreenHeight)
                    player.Position.Y = 0;
                if (player.Position.Y < 0)
                    player.Position.Y = Program.ScreenHeight;
            }
        }
    }

    [MessagePackObject]
    public class IntVec2
    {
        [Key(0)]
        public int X;
        [Key(1)]
        public int Y;

        public IntVec2()
        {
        }

        public IntVec2(int x, int y)
        {
            X = x;
            Y = y;
        }

        public void Norm()
        {
            if (X > 0) X = 1;
            if (X < 0) X = -1;
            if (Y > 0) Y = 1;
            if (Y < 0) Y = -1;
        }
    }
}
