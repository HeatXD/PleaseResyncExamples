using Raylib_cs;
using MessagePack;
using System;

namespace RollbackBalls
{
    public class Program
    {
        public const int ScreenWidth = 600;
        public const int ScreenHeight = 600;
        static void Main(string[] args)
        {
            var gamestate = new Gamestate(2);
            // var bytes = MessagePackSerializer.Serialize(state);
            // state = MessagePackSerializer.Deserialize<Gamestate>(bytes);
            Raylib.InitWindow(ScreenWidth, ScreenHeight, "RollbackBalls");
            Raylib.SetTargetFPS(60);

            while (!Raylib.WindowShouldClose())
            {
                Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.BLACK);

                Raylib.DrawText("Hello, world!", 12, 12, 20, Color.WHITE);

                Raylib.EndDrawing();
            }

            Raylib.CloseWindow();
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

                if (dir.X != 0 && dir.Y != 0)
                {
                    dir.Norm();

                    player.Velocity.X += dir.X * 10;
                    player.Velocity.Y += dir.Y * 10;
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
