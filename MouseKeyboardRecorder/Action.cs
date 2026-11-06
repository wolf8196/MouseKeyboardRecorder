using SharpHook.Data;

namespace MouseKeyboardRecorder
{
    public class Action
    {
        public ActionType ActionType { get; init; }

        public long Offset { get; init; }

        public KeyCode? KeyCode { get; init; }

        public MouseInfo? MouseInfo { get; init; }
    }
}