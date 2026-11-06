using System;
using System.Threading;
using System.Threading.Tasks;

namespace MouseKeyboardRecorder.Tests
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            // recoding
            var cts = new CancellationTokenSource();
            var recordingTask = MouseKeyRecorder.StartRecording(saveToFile: "rec.json", cts.Token);
            Console.WriteLine("Recording started. Press any key to stop...");
            Console.ReadKey();
            cts.Cancel();
            var results = await recordingTask;
            Console.WriteLine($"Recording stopped. Count: {results.Count}");

            // playback
            Console.WriteLine("PLayback started.");
            await MouseKeyRecorder.PlaybackRecording(actions: null, file: "rec.json", loop: false, CancellationToken.None);
        }
    }
}