using Raylib_cs;

namespace RollbackBalls
{
    class Program
    {
        static void Main(string[] args)
        {
            Raylib.InitWindow(600, 600, "RollbackBalls");
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
}
