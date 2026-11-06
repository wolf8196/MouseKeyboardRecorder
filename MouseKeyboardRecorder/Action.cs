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

        public Action()
        {
        }

        public ActionType ActionType { get; set; }

        public long Offset { get; set; }

        public KeyCode? KeyCode { get; set; }

        public MouseInfo? MouseInfo { get; set; }
    }
}