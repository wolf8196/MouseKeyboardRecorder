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

        public MouseInfo()
        {
        }

        public short X { get; set; }

        public short Y { get; set; }

        public MouseButton Button { get; set; }

        public ushort ClickSequenceCounter { get; set; }
    }
}