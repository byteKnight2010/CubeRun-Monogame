using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using static Cube_Run_C_.Globals;
using static Cube_Run_C_.Tools.BitMask;


namespace Cube_Run_C_ {
  public static class Debugger {
    public static class DebugLogger {
      private static readonly Process CPUProcess = Process.GetCurrentProcess();
      private static readonly Stopwatch SessionTimer = Stopwatch.StartNew();
      private static TimeSpan LastCPUTime = TimeSpan.Zero;
      private static byte DebugStats = 0x00;


      public static void Log(DebugInfo info, bool log = true) => Set(ref DebugStats, (byte)info, log);


      public static void Update(GameTime gameTime) {
        if (IsSet(DebugStats, (uint)DebugInfo.FPS))
          Console.WriteLine($"FPS: {1f / (float)gameTime.ElapsedGameTime.TotalSeconds}");

        if (IsSet(DebugStats, (uint)DebugInfo.DeltaTime))
          Console.WriteLine($"DELTA TIME: {gameTime.ElapsedGameTime.TotalMilliseconds} Milliseconds");

        if (IsSet(DebugStats, (uint)DebugInfo.RAM))
          Console.WriteLine($"RAM USAGE: {CPUProcess.WorkingSet64} Bytes");

        if (IsSet(DebugStats, (uint)DebugInfo.CPU)) {
          TimeSpan CurrentCPUTime = CPUProcess.TotalProcessorTime;
          Console.WriteLine($"CPU TIME: {(CurrentCPUTime - LastCPUTime).TotalMicroseconds} Microseconds");
          LastCPUTime = CurrentCPUTime;
        }
        if (IsSet(DebugStats, (uint)DebugInfo.SessionTime))
          Console.WriteLine($"SESSION TIME: {SessionTimer.Elapsed}");
      }
    }
  }
}