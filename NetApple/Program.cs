﻿using Claunia.PropertyList;
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
            config.BuildDirectory = Environment.GetEnvironmentVariable("BUILD_DMG") ?? config.BuildDirectory;
            WriteAppleApp(config);
            var args = new List<string>();
            args.Add("-V");
            args.Add(Quote(config.BundleName));
            args.Add("-D");
            args.Add("-R");
            args.Add("-apple");
            args.Add("-no-pad");
            args.Add("-o");
            args.Add(Quote(config.DiskImageFile));
            args.Add(Quote(config.AppTemp));
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

        static void WriteAppleApp(AppleConfig config)
        {
            var volicon = Path.Combine(config.AppTemp, ".volumeicon.icns");
            File.Copy(config.AppIcon, volicon, true);
            var appFolder = Path.Combine(config.AppTemp, $"{config.BundleName}.app");
            Directory.CreateDirectory(appFolder);
            var contentsFolder = Path.Combine(appFolder, "Contents");
            Directory.CreateDirectory(contentsFolder);
            var infoPlist = Path.Combine(contentsFolder, "Info.plist");
            using (var file = File.Create(infoPlist))
                WriteApplePlist(file, config);
            var macOsFolder = Path.Combine(contentsFolder, "MacOS");
            Directory.CreateDirectory(macOsFolder);
            var shellFile = Path.Combine(macOsFolder, "MonoAppLauncher");
            var shellLines = new List<string>
            {
                "#!/usr/bin/env bash",
                "echo whoami"
            };
            File.WriteAllLines(shellFile, shellLines, Encoding.UTF8);
            var resFolder = Path.Combine(contentsFolder, "Resources");
            Directory.CreateDirectory(resFolder);
            var iconFile = Path.Combine(resFolder, "app.icns");
            File.Copy(config.AppIcon, iconFile, true);
            var realRoot = Path.Combine(contentsFolder, "Mono");
            Directory.CreateDirectory(realRoot);
            IOHelper.CloneDirectory(config.BuildDirectory, realRoot);
        }

        static void WriteApplePlist(Stream stream, AppleConfig config)
        {
            using (stream)
            {
                var plist = new NSDictionary();
                plist.Add("CFBundleDevelopmentRegion", "German");
                plist.Add("CFBundleExecutable", "MonoLauncher");
                plist.Add("CFBundleHelpBookFolder", $"{config.BundleName} Help");
                plist.Add("CFBundleHelpBookName", $"{config.BundleName} Help");
                plist.Add("CFBundleIconFile", "app.icns");
                plist.Add("CFBundleIdentifier", config.BundleId);
                plist.Add("CFBundleInfoDictionaryVersion", "6.0");
                plist.Add("CFBundleName", config.BundleName);
                plist.Add("CFBundleShortVersionString", config.BundleVersion);
                var purl = new NSDictionary();
                purl.Add("CFBundleURLName", $"{config.BundleName} URL");
                var parray = new NSArray(new NSString(config.UrlPrefix));
                purl.Add("CFBundleURLSchemes", parray);
                var purls = new NSArray(purl);
                plist.Add("CFBundleURLTypes", purls);
                plist.Add("CFBundleVersion", config.BundleVersion);
                plist.Add("LSApplicationCategoryType", "public.app-category.business");
                plist.Add("LSMinimumSystemVersion", "10.7");
                var pMinVer = new NSDictionary();
                pMinVer.Add("i386", "10.7.0");
                pMinVer.Add("x86_64", "10.7.0");
                plist.Add("LSMinimumSystemVersionByArchitecture", pMinVer);
                plist.Add("NSHumanReadableCopyright", config.Copyright);
                PropertyListParser.SaveAsXml(plist, stream);
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