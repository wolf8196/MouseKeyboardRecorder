# MouseKeyboardRecorder

Record mouse and keyboard actions, then play them back.

## Record

`StartRecording` can capture:
- key presses
- mouse moves
- mouse clicks 

Recording ends when the token is cancelled. Pass an optional file path to save the recording as JSON or get recordings back and manage them yourself.

```csharp
using MouseKeyboardRecorder;

using var cts = new CancellationTokenSource();
var recordingTask = MouseKeyRecorder.StartRecording(saveToFile: "rec.json", cts.Token);
// wait/block in whatever way
cts.Cancel();
var actions = await recording;
```

## Play back

`PlaybackRecording` accepts action list or file name containing action list and replays it with the original timing. Set `loop` to `true` to repeat until the token is cancelled.

```csharp
var task = MouseKeyRecorder.PlaybackRecording(actions: null, file: "rec.json", loop: false, CancellationToken.None);s
```

