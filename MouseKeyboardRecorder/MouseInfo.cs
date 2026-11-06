using SharpHook.Data;

namespace MouseKeyboardRecorder
{
    public class MouseInfo
    {
        public MouseInfo(short x, short y)
            : this(x, y, MouseButton.NoButton, 0)
        {
        }

        public MouseInfo(short x, short y, MouseButton button, ushort clickSequenceCounter)
        {
            X = x;
            Y = y;
            Button = button;
            ClickSequenceCounter = clickSequenceCounter;
        }

        public short X { get; }

        public short Y { get; }

        public MouseButton Button { get; }

        public ushort ClickSequenceCounter { get; }
    }
}