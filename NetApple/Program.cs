using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace NetApple
{
    public static class Program
    {
        public static int Main(string[] parms)
        {
            if (parms.Length != 1)
            {
                Console.WriteLine("Usage: [json-file]");
                return -1;
            }
            var set = new JsonSerializerSettings();
            var text = File.ReadAllText(parms.First(), Encoding.UTF8);
            var config = JsonConvert.DeserializeObject<AppleConfig>(text, set);
            var args = new List<string>();
            args.Add("-V");
            args.Add(Quote(config.BundleName));
            args.Add("-D");
            args.Add("-R");
            args.Add("-apple");
            args.Add("-no-pad");
            args.Add("-o");
            args.Add(Quote(config.DiskImageFile));
            var buildDir = Environment.GetEnvironmentVariable("BUILD_DMG") ?? config.BuildDirectory;
            args.Add(Quote(buildDir));
            var dir = Path.GetDirectoryName(typeof(Program).Assembly.Location);
            string exe;
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                exe = Path.Combine(dir, "mkisofs.exe");
            else
                exe = "genisoimage";
            var info = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = string.Join(" ", args),
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                UseShellExecute = false
            };
            using (var proc = Process.Start(info))
            {
                if (proc == null)
                    throw new NotImplementedException(info.FileName);
                proc.OutputDataReceived += Proc_OutputDataReceived;
                proc.ErrorDataReceived += Proc_ErrorDataReceived;
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
                proc.WaitForExit();
                return proc.ExitCode;
            }
        }

        static string Quote(string text)
            => text.StartsWith("\"") && text.EndsWith("\"") ? text : '"' + text + '"';

        static void Proc_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.Error.WriteLine(e.Data);
        }

        static void Proc_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.Out.WriteLine(e.Data);
        }
    }
}