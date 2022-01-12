using MessagePack;

namespace RollbackBalls
{
    [MessagePackObject]
    public class Player
    {
        [Key(0)]
        public IntVec2 Position;
        [Key(1)]
        public IntVec2 Velocity;
        [Key(2)]
        public IntVec2 Acceleration;

        public Player()
        {
            Position = new IntVec2();
            Velocity = new IntVec2();
            Acceleration = new IntVec2();
        }

        public Player(IntVec2 position, IntVec2 velocity, IntVec2 acceleration)
        {
            Position = position;
            Velocity = velocity;
            Acceleration = acceleration;
        }
    }

    public struct PlayerInput
    {
        public byte InputState;// 8 bits so 8 bools/buttons. index 0 to 7
        public PlayerInput(byte input = 0)
        {
            InputState = input;
        }
        public PlayerInput(PlayerInput input)
        {
            InputState = input.InputState;
        }
        public void SetInputBit(int bitIndex, bool state)
        {
            if (bitIndex < 0 || bitIndex > 7) return; // invalid bit dont do any actions
            if (state)
            {
                //set the bit to true
                InputState |= (byte)(1 << bitIndex);
            }
            else
            {
                //set the bit to false
                //~ will return a negative number, so casting to int is necessary
                int i = InputState;
                i &= ~(1 << bitIndex);
                InputState = (byte)i;
            }
        }
        public bool IsInputBitSet(int bitIndex)
        {
            if (bitIndex < 0 || bitIndex > 7) return false;
            return (InputState & (1 << bitIndex)) > 0;
        }
    }
}