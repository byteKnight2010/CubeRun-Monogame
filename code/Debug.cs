using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using static Cube_Run_C_.Tools.BitMask;


namespace Cube_Run_C_ {
  public static class Debugger {
    [Flags]
    public enum DebugInfo : byte {
      FPS = 1 << 0,
      RAM = 1 << 1,
      CPU = 1 << 2,
      SessionTime = 1 << 3,
      DeltaTime = 1 << 4
    }


    public static class DebugLogger {
      private static Process CPUProcess = Process.GetCurrentProcess();
      private static Stopwatch SessionTimer = Stopwatch.StartNew();
      private static TimeSpan LastCPUTime = TimeSpan.Zero;
      public static byte LogValues = 0b00000000;


      public static void Log(DebugInfo info, bool log = true) => Set(ref LogValues, (byte)info, log);


      public static void Update(GameTime gameTime) {
        if ((LogValues & (byte)DebugInfo.FPS) != 0)
          Console.WriteLine($"FPS: {1f / (float)gameTime.ElapsedGameTime.TotalSeconds}");

        if ((LogValues & (byte)DebugInfo.DeltaTime) != 0)
          Console.WriteLine($"DELTA TIME: {gameTime.ElapsedGameTime.TotalMilliseconds} Milliseconds");

        if ((LogValues & (byte)DebugInfo.RAM) != 0)
          Console.WriteLine($"RAM USAGE: {GC.GetTotalMemory(false)} Bytes");

        if ((LogValues & (byte)DebugInfo.CPU) != 0) {
          TimeSpan CurrentCPUTime = CPUProcess.TotalProcessorTime;
          Console.WriteLine($"CPU TIME: {(CurrentCPUTime - LastCPUTime).TotalMicroseconds} Microseconds");
          LastCPUTime = CurrentCPUTime;
        }
        if ((LogValues & (byte)DebugInfo.SessionTime) != 0)
          Console.WriteLine($"SESSION TIME: {SessionTimer.Elapsed}");
      }
    }
  }
}