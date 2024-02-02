
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEditor.Build.Reporting;

namespace Unity.Jenkins
{
    public static class Utils
    {
        static readonly string s_eol = Environment.NewLine;
        static readonly string[] s_secrets = { "androidKeystorePass", "androidKeyaliasName", "androidKeyaliasPass" };

        public static Dictionary<string, string> GetValidatedOptions(bool isPrint)
        {
            ParseCommandLineArguments(out var validatedOptions, isPrint);
            
            if (!validatedOptions.TryGetValue("projectPath", out string projectPath)
                || string.IsNullOrEmpty(projectPath))
            {
                PrintErrorLog("Missing argument -projectPath", 110);
            }
            
            if (!validatedOptions.TryGetValue("buildTarget", out string buildTarget)
                || string.IsNullOrEmpty(projectPath)
                || !Enum.IsDefined(typeof(BuildTarget), buildTarget))
            {
                PrintErrorLog("Missing argument -buildTarget", 110);
            }
            
            if (!validatedOptions.TryGetValue("buildPath", out string buildPath)
                || string.IsNullOrEmpty(buildPath))
            {
                PrintErrorLog("Missing argument -buildPath", 110);
            }

            if (buildTarget == "iOS")
            {
                iOSSettings.Validate(validatedOptions);
            }
            else if (buildTarget == "Android")
            {
                AndroidSettings.Validate(validatedOptions);
            }
            
            return validatedOptions;
        }
        
        static void ParseCommandLineArguments(out Dictionary<string, string> providedArguments, bool isPrint)
        {
            providedArguments = new Dictionary<string, string>();
            var args = Environment.GetCommandLineArgs();

            if (isPrint)
            {
                Console.WriteLine(
                    $"{s_eol}" +
                    $"###########################{s_eol}" +
                    $"#    Parsing settings     #{s_eol}" +
                    $"###########################{s_eol}" +
                    $"{s_eol}"
                );
            }

            // Extract flags with optional values
            for (int current = 0, next = 1; current < args.Length; current++, next++)
            {
                if (!args[current].StartsWith("-"))
                {
                    continue;
                }
                var flag = args[current].TrimStart('-');
                // Parse optional value
                var flagHasValue = next < args.Length && !args[next].StartsWith("-");
                var value = flagHasValue ? args[next].TrimStart('-') : "";
                var isSecret = s_secrets.Contains(flag);
                if (isPrint)
                {
                    var displayValue = isSecret ? "*HIDDEN*" : "\"" + value + "\"";
                    Console.WriteLine("Found flag \"" + flag + "\" with value " + displayValue);
                }
                providedArguments.Add(flag, value);
            }
        }
        
        public static void ReportSummary(BuildSummary summary)
        {
            Console.WriteLine(
                $"{s_eol}" +
                $"###########################{s_eol}" +
                $"#      Build results      #{s_eol}" +
                $"###########################{s_eol}" +
                $"{s_eol}" +
                $"Duration: {summary.totalTime.ToString()}{s_eol}" +
                $"Warnings: {summary.totalWarnings.ToString()}{s_eol}" +
                $"Errors: {summary.totalErrors.ToString()}{s_eol}" +
                $"Size: {summary.totalSize.ToString()} bytes{s_eol}" +
                $"{s_eol}"
            );
        }

        public static void ExitWithResult(BuildResult result)
        {
            switch (result)
            {
                case BuildResult.Succeeded:
                    PrintLog("Build succeeded!");
                    EditorApplication.Exit(0);
                    break;
                case BuildResult.Failed:
                    PrintErrorLog("Build failed!", 101);
                    break;
                case BuildResult.Cancelled:
                    PrintErrorLog("Build cancelled!", 102);
                    break;
                case BuildResult.Unknown:
                    PrintErrorLog("Build result is unknown!", 103);
                    break;
            }
        }
        
        public static void PrintLog(string msg)
        {
            if (Application.isBatchMode)
            {
                if (msg.Contains("\n"))
                {
                    msg = msg.Replace("\n", s_eol);
                }
                Console.WriteLine(msg);
            }
            else Debug.Log(msg);
        }

        public static void PrintWarningLog(string msg)
        {
            if (Application.isBatchMode)
            {
                if (msg.Contains("\n"))
                {
                    msg = msg.Replace("\n", s_eol);
                }
                Console.WriteLine($"::warning:: {msg}");
            }
            else Debug.LogWarning(msg);
        }

        public static void PrintErrorLog(string msg, int errorCode)
        {
            if (Application.isBatchMode)
            {
                if (msg.Contains("\n"))
                {
                    msg = msg.Replace("\n", s_eol);
                }
                Console.WriteLine($"::error:: {msg}");
                EditorApplication.Exit(errorCode);
            }
            else Debug.LogError(msg);
        }
    }
}