﻿using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace NuGet.CommandLine.Test
{
    public class CommandRunner
    {
        // Item1 of the returned tuple is the exit code. Item2 is the standard output, and Item3
        // is the error output.
        public static CommandRunnerResult Run(
            string process,
            string workingDirectory,
            string arguments,
            bool waitForExit,
            int timeOutInMilliseconds = 60000,
           Action<StreamWriter> inputAction = null)
        {
            ProcessStartInfo psi = new ProcessStartInfo(Path.GetFullPath(process), arguments)
            {
                WorkingDirectory = Path.GetFullPath(workingDirectory),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = inputAction != null
            };

            int exitCode = 1;

            var output = new StringBuilder();
            var errors = new StringBuilder();

            using (Process p = new Process())
            {
                p.OutputDataReceived += (o, e) =>
                {
                    if (e.Data != null)
                    {
                        output.AppendLine(e.Data);
                    }
                };

                p.ErrorDataReceived += (o, e) =>
                {
                    if (e.Data != null)
                    {
                        errors.AppendLine(e.Data);
                    }
                };

                p.StartInfo = psi;
                p.Start();

                p.BeginErrorReadLine();
                p.BeginOutputReadLine();

                if (inputAction != null)
                {
                    inputAction(p.StandardInput);
                }

                if (waitForExit)
                {
                    bool processExited = p.WaitForExit(timeOutInMilliseconds);
                    if (!processExited)
                    {
                        p.Kill();
                        throw new TimeoutException("nuget.exe timed out: " + psi.Arguments);
                    }

                    if (processExited)
                    {
                        exitCode = p.ExitCode;
                    }
                }
            }

            return new CommandRunnerResult(exitCode, output.ToString(), errors.ToString());
        }
    }
}