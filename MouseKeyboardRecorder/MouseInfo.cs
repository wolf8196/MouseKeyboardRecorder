using SharpHook.Data;

namespace MouseKeyboardRecorder
{
    public class MouseInfo
    {
        public MouseButton Button { get; init; }

        public ushort ClickSequenceCounter { get; init; }

        public short X { get; init; }

        public short Y { get; init; }
    }
}