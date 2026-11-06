using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SharpHook;
using SharpHook.Simulation;

namespace MouseKeyboardRecorder
{
    public static class MouseKeyRecorder
    {
        private static readonly long Ticks15Ms = TimeSpan.FromMilliseconds(15).Ticks;

        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            TypeNameHandling = TypeNameHandling.None,
        };

        public static async Task<IReadOnlyCollection<Action>> StartRecording(string? saveToFile = null, CancellationToken token = default)
        {
            var concurrentBag = new ConcurrentBag<Action>();
            var hook = new EventLoopGlobalHook();
            var sw = new Stopwatch();

            hook.KeyPressed += (_, e) => OnKeyPress(sw, concurrentBag, e);
            hook.KeyReleased += (_, e) => OnKeyRelease(sw, concurrentBag, e);
            hook.MouseClicked += (_, e) => OnMouseClick(sw, concurrentBag, e);
            hook.MouseMoved += (_, e) => OnMouseMove(sw, concurrentBag, e);

            using var _ = token.Register(hook.Dispose);

            sw.Start();
            await hook.RunAsync(useBackgroundThread: true);
            sw.Stop();

            var ordered = concurrentBag.OrderBy(x => x.Offset).ToList();
            if (saveToFile != null)
            {
                using var fs = File.Create(saveToFile);
                using var wr = new StreamWriter(fs);
                using var jtw = new JsonTextWriter(wr);
                JsonSerializer.Create(Settings).Serialize(jtw, ordered);
            }

            return ordered;
        }

        public static Task PlaybackRecording(IReadOnlyCollection<Action>? actions = null, string? file = null, bool loop = false, CancellationToken token = default)
        {
            return Task.Run(async () =>
            {
                do
                {
                    List<Action> actionsList;
                    if (actions != null)
                    {
                        actionsList = actions.ToList();
                    }
                    else if (file != null)
                    {
                        using var fs = File.OpenRead(file);
                        using var sr = new StreamReader(fs);
                        using var jtr = new JsonTextReader(sr);
                        actionsList = JsonSerializer.Create(Settings).Deserialize<List<Action>>(jtr)
                            ?? throw new ArgumentException("Invalid stream provided. Serialization resulted in null.");
                    }
                    else
                    {
                        throw new ArgumentException("Nothing to playback.");
                    }

                    using var simulator = EventSimulator.Create("MouseRecorder");
                    var sw = Stopwatch.StartNew();
                    for (var i = 0; i < actionsList.Count; i++)
                    {
                        if (token.IsCancellationRequested)
                        {
                            break;
                        }

                        var action = actionsList[i];
                        var remaining = action.Offset - sw.ElapsedTicks;

                        if (remaining > Ticks15Ms)
                        {
                            Thread.Sleep(TimeSpan.FromTicks(remaining - Ticks15Ms));
                        }
                        else if (remaining > 0)
                        {
                            while (sw.ElapsedTicks < action.Offset)
                            {
                                Thread.SpinWait(20);
                            }
                        }

                        switch (action.ActionType)
                        {
                            case ActionType.MouseMove when action.MouseInfo is { } mouse:
                                simulator.SimulateMouseMovement(mouse.X, mouse.Y);
                                break;

                            case ActionType.MouseClick when action.MouseInfo is { } mouse:
                                simulator.SimulateMousePress(mouse.X, mouse.Y, mouse.Button, mouse.ClickSequenceCounter);
                                simulator.SimulateMouseRelease(mouse.X, mouse.Y, mouse.Button, mouse.ClickSequenceCounter);
                                break;

                            case ActionType.KeyPress when action.KeyCode is { } code:
                                simulator.SimulateKeyPress(code);
                                break;

                            case ActionType.KeyRelease when action.KeyCode is { } code:
                                simulator.SimulateKeyRelease(code);
                                break;
                        }
                    }
                }
                while (loop && !token.IsCancellationRequested);
            },
            default);
        }

        private static void OnKeyPress(Stopwatch sw, ConcurrentBag<Action> concurrentQueue, KeyboardHookEventArgs e)
        {
            concurrentQueue.Add(new Action(ActionType.KeyPress, sw.ElapsedTicks, e.Data.KeyCode));
        }

        private static void OnKeyRelease(Stopwatch sw, ConcurrentBag<Action> concurrentQueue, KeyboardHookEventArgs e)
        {
            concurrentQueue.Add(new Action(ActionType.KeyRelease, sw.ElapsedTicks, e.Data.KeyCode));
        }

        private static void OnMouseMove(Stopwatch sw, ConcurrentBag<Action> concurrentQueue, MouseHookEventArgs e)
        {
            concurrentQueue.Add(new Action(ActionType.MouseMove, sw.ElapsedTicks, new MouseInfo(e.Data.X, e.Data.Y)));
        }

        private static void OnMouseClick(Stopwatch sw, ConcurrentBag<Action> concurrentQueue, MouseHookEventArgs e)
        {
            concurrentQueue.Add(new Action(ActionType.MouseClick, sw.ElapsedTicks, new MouseInfo(e.Data.X, e.Data.Y, e.Data.Button, e.Data.Clicks)));
        }
    }
}