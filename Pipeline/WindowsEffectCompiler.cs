using System;
using System.ComponentModel;
using System.IO;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;

// TODO: replace these with the processor input and output types.
using TInput = Microsoft.Xna.Framework.Content.Pipeline.Graphics.EffectContent;
using TOutput = Microsoft.Xna.Framework.Content.Pipeline.Processors.CompiledEffectContent;

namespace WindowsEffectCompiler
{
  /// <summary>
  /// This class will be instantiated by the XNA Framework Content Pipeline
  /// to apply custom processing to content data, converting an object of
  /// type TInput to TOutput. The input and output types may be the same if
  /// the processor wishes to alter data without changing its type.
  ///
  /// This should be part of a Content Pipeline Extension Library project.
  ///
  /// TODO: change the ContentProcessor attribute to specify the correct
  /// display name for this processor.
  /// </summary>
  [ContentProcessor(DisplayName = "Windows Effect Compiler")]
  public class WindowsEffectCompiler : EffectProcessor
  {
    public override TOutput Process(TInput input, ContentProcessorContext context)
    {
      if (context.TargetPlatform == TargetPlatform.Windows)
      {
        Process p = new Process();
        p.EnableRaisingEvents = false;
        p.StartInfo.FileName = FXCName;
        p.StartInfo.CreateNoWindow = true;
        string x = Path.GetFileNameWithoutExtension(input.Identity.SourceFilename) + "_fxc";
        x = Path.Combine(context.IntermediateDirectory, x);
        OptLevel opt;
#if DEBUG
        opt = debugOptLevel_;
#else
        opt = releaseOptLevel_;
#endif
        p.StartInfo.Arguments = String.Format("/T fx_2_0 /Zi /O{3} {5} /Fo \"{0}\" /Fe \"{1}\" /I \"{4}\" \"{2}\"", 
            x + ".dat", x + ".err", input.Identity.SourceFilename, (int)opt, Path.GetDirectoryName(input.Identity.SourceFilename),
            opt == OptLevel.None ? "/Od" : "");
        context.Logger.LogMessage("{0} {1}", p.StartInfo.FileName, p.StartInfo.Arguments);
        p.Start();
        if (!p.WaitForExit(1000 * timeoutSeconds_))
        {
          throw new TimeoutException("FXC took more than 20 seconds to compile!");
        }
        if (p.ExitCode != 0)
        {
          Trace.WriteLine("Exit code: " + p.ExitCode.ToString());
          context.Logger.LogImportantMessage("Exit code: {0}", p.ExitCode);
        }
        byte[] bytes = File.ReadAllBytes(x + ".dat");
        string text = File.ReadAllText(x + ".err");
        if (p.ExitCode > 1 || p.ExitCode < 0 || text.Contains("error"))
        {
          if (text.Length == 0)
            text = String.Format("FXC.EXE returned exit code {0}", p.ExitCode);
          throw new InvalidContentException(text, input.Identity);
        }
        else if (text.Length != 0)
        {
          foreach (string ss in text.Split('\n', '\r'))
            context.Logger.LogWarning("", input.Identity, "{0}", ss);
        }
        return new CompiledEffectContent(bytes);
      }
      else
      {
        return base.Process(input, context);
      }
    }
    
    public enum OptLevel { None = 0, One = 1, Two = 2, Three = 3 };
    OptLevel debugOptLevel_ = OptLevel.None;
    [DefaultValue(OptLevel.None)]
    public OptLevel DebugOptimization { get { return debugOptLevel_; } set { debugOptLevel_ = value; } }
    OptLevel releaseOptLevel_ = OptLevel.Three;
    [DefaultValue(OptLevel.Three)]
    public OptLevel ReleaseOptimization { get { return releaseOptLevel_; } set { releaseOptLevel_ = value; } }
    int timeoutSeconds_ = 20;
    [DefaultValue(20)]
    public int TimeoutSeconds { get { return timeoutSeconds_; } set { timeoutSeconds_ = value; } }

    static string FXCName;

    static WindowsEffectCompiler()
    {
      //  look for some fxc.exe executable in some of the known places where it may live
      string[] tests = new string[] { "Utilities\\bin\\x86\\fxc.exe", "Utilities\\bin\\x64\\fxc.exe" };
      foreach (string path in new string[] {
        "C:\\Program Files", 
        "C:\\Program Files (x86)", 
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), 
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
        })
      {
        string[] dirs = Directory.GetDirectories(path, "Microsoft DirectX*");
        foreach (string dir in dirs)
        {
          foreach (string test in tests)
          {
            string str = Path.Combine(dir, test);
            if (File.Exists(str))
            {
              FXCName = str;
              Trace.WriteLine("Found FXC.EXE: " + FXCName);
              return;
            }
          }
        }
      }
      //  If not there, then just hope it's in the path passed by Visual Studio
      FXCName = "FXC.EXE";
    }
  }
}
