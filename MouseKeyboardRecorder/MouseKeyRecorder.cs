using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using SharpHook;
using SharpHook.Simulation;

namespace MouseKeyboardRecorder
{
    public static class MouseKeyRecorder
    {
        private static readonly long Ticks15Ms = TimeSpan.FromMilliseconds(15).Ticks;

        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions(JsonSerializerOptions.Default)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
            WriteIndented = true,
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
                await JsonSerializer.SerializeAsync(fs, ordered, Options, default);
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
                        actionsList = await JsonSerializer.DeserializeAsync<List<Action>>(fs, Options)
                            ?? throw new ArgumentException("Failed to deserialize actions from file.");
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
            concurrentQueue.Add(CreateKeyAction(sw, ActionType.KeyPress, e));
        }

        private static void OnKeyRelease(Stopwatch sw, ConcurrentBag<Action> concurrentQueue, KeyboardHookEventArgs e)
        {
            concurrentQueue.Add(CreateKeyAction(sw, ActionType.KeyRelease, e));
        }

        private static void OnMouseMove(Stopwatch sw, ConcurrentBag<Action> concurrentQueue, MouseHookEventArgs e)
        {
            var action = new Action
            {
                ActionType = ActionType.MouseMove,
                Offset = sw.ElapsedTicks,
                MouseInfo = new MouseInfo
                {
                    X = e.Data.X,
                    Y = e.Data.Y,
                }
            };

            concurrentQueue.Add(action);
        }

        private static void OnMouseClick(Stopwatch sw, ConcurrentBag<Action> concurrentQueue, MouseHookEventArgs e)
        {
            var action = new Action
            {
                ActionType = ActionType.MouseClick,
                Offset = sw.ElapsedTicks,
                MouseInfo = new MouseInfo
                {
                    Button = e.Data.Button,
                    ClickSequenceCounter = e.Data.Clicks,
                    X = e.Data.X,
                    Y = e.Data.Y,
                }
            };

            concurrentQueue.Add(action);
        }

        private static Action CreateKeyAction(Stopwatch sw, ActionType actionType, KeyboardHookEventArgs e)
        {
            return new Action
            {
                ActionType = actionType,
                Offset = sw.ElapsedTicks,
                KeyCode = e.Data.KeyCode,
            };
        }
    }
}