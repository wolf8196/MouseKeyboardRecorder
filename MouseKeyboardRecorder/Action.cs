using SharpHook.Data;

namespace MouseKeyboardRecorder
{
    public class Action
    {
        public Action(ActionType actionType, long offset, MouseInfo mouseInfo)
            : this(actionType, offset)
        {
            MouseInfo = mouseInfo;
        }

        public Action(ActionType actionType, long offset, KeyCode keyCode)
            : this(actionType, offset)
        {
            KeyCode = keyCode;
        }

        private Action(ActionType actionType, long offset)
        {
            ActionType = actionType;
            Offset = offset;
        }

        public ActionType ActionType { get; }

        public long Offset { get; }

        public KeyCode? KeyCode { get; }

        public MouseInfo? MouseInfo { get; }
    }
}