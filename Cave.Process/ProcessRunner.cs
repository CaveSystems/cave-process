﻿using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cave.Collections;
using Cave.IO;

namespace Cave
{
    /// <summary>
    /// Provides a process runner able to read stdout and stderr at the same time without blocking.
    /// </summary>
    public class ProcessRunner
    {
        /// <summary>Runs the specified command.</summary>
        /// <param name="command">The command.</param>
        /// <param name="arguments">The arguments.</param>
        /// <param name="timeoutMilliSeconds">The timeout milli seconds.</param>
        /// <returns></returns>
        public static ProcessResult Run(string command, string arguments, int timeoutMilliSeconds = 0)
        {
            try
            {
                ProcessRunner runner = new ProcessRunner(command, arguments);
                if (!runner.WaitForExit(timeoutMilliSeconds))
                {
                    runner.Kill();
                }

                return new ProcessResult(runner.Combined, runner.StdOut, runner.StdErr, runner.ExitCode);
            }
            catch (Exception ex)
            {
                return new ProcessResult(ex);
            }
        }

        /// <summary>Runs the specified command.</summary>
        /// <param name="command">The command.</param>
        /// <param name="arguments">The arguments.</param>
        /// <param name="environmentVariables">The environment variables.</param>
        /// <returns></returns>
        public static ProcessResult Run(string command, string arguments, params Option[] environmentVariables)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(command, arguments);
            foreach (Option opt in environmentVariables)
            {
                startInfo.EnvironmentVariables[opt.Name] = opt.Value;
            }
            return Run(startInfo);
        }

        /// <summary>Runs the specified start information.</summary>
        /// <param name="startInfo">The start information.</param>
        /// <param name="timeoutMilliSeconds">The timeout milli seconds.</param>
        /// <returns></returns>
        public static ProcessResult Run(ProcessStartInfo startInfo, int timeoutMilliSeconds = 0)
        {
            try
            {
                ProcessRunner runner = new ProcessRunner(startInfo);
                if (!runner.WaitForExit(timeoutMilliSeconds))
                {
                    runner.Kill();
                }

                return new ProcessResult(runner.Combined, runner.StdOut, runner.StdErr, runner.ExitCode);
            }
            catch (Exception ex)
            {
                return new ProcessResult(ex);
            }
        }

        string fileName;
        StringBuilder stdout = new StringBuilder();
        StringBuilder stderr = new StringBuilder();
        StringBuilder combined = new StringBuilder();
        Process process;
        bool completedStdOut;
        bool completedStdErr;

        /// <summary>Initializes a new instance of the <see cref="ProcessRunner"/> class by starting the specified command.</summary>
        /// <param name="cmd">The command.</param>
        /// <param name="parameter">The parameter.</param>
        public ProcessRunner(string cmd, string parameter)
            : this(new ProcessStartInfo(cmd, parameter))
        {
        }

        /// <summary>Initializes a new instance of the <see cref="ProcessRunner"/> class by executing a new process with the specified start info.</summary>
        /// <param name="processStartInfo">The process start information.</param>
        public ProcessRunner(ProcessStartInfo processStartInfo)
        {
            processStartInfo.CreateNoWindow = true;
            processStartInfo.RedirectStandardError = true;
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.UseShellExecute = false;
            fileName = processStartInfo.FileName;
            process = new Process()
            {
                EnableRaisingEvents = true,
                StartInfo = processStartInfo,
            };
            process.Start();
            Task.Factory.StartNew(() => { ReadStandardOutput(); });
            Task.Factory.StartNew(() => { ReadStandardError(); });
        }

        private void ReadStandardError()
        {
            try
            {
                while (true)
                {
                    string s = process.StandardError.ReadLine();
                    if (s == null)
                    {
                        break;
                    }

                    lock (combined)
                    {
                        combined.AppendLine(s);
                        lock (stderr)
                        {
                            stderr.AppendLine(s);
                        }
                    }
                }
            }
            catch
            {
            }
            finally
            {
                completedStdErr = true;
            }
        }

        private void ReadStandardOutput()
        {
            try
            {
                while (true)
                {
                    string s = process.StandardOutput.ReadLine();
                    if (s == null)
                    {
                        break;
                    }

                    lock (combined)
                    {
                        combined.AppendLine(s);
                        lock (stdout)
                        {
                            stdout.AppendLine(s);
                        }
                    }
                }
            }
            catch
            {
            }
            finally
            {
                completedStdOut = true;
            }
        }

        /// <summary>Waits for exit.</summary>
        /// <param name="timeSpan">The time span.</param>
        /// <returns></returns>
        public bool WaitForExit(TimeSpan timeSpan)
        {
            return WaitForExit((int)(timeSpan.Ticks / TimeSpan.TicksPerMillisecond));
        }

        /// <summary>Waits for exit.</summary>
        /// <param name="milliSeconds">The milli seconds.</param>
        /// <returns></returns>
        public bool WaitForExit(int milliSeconds = 0)
        {
            if (milliSeconds > 0)
            {
                if (!process.WaitForExit(milliSeconds))
                {
                    return false;
                }
            }
            process.WaitForExit();
            while (!completedStdErr || !completedStdOut)
            {
                Thread.Sleep(1);
            }

            return true;
        }

        /// <summary>Kills this instance.</summary>
        public void Kill()
        {
            do
            {
                process.Kill();
                WaitForExit(1000);
            }
            while (!process.HasExited);
        }

        /// <summary>Gets the standard output.</summary>
        /// <value>The output.</value>
        public string StdOut
        {
            get
            {
                lock (stdout)
                {
                    return stdout.ToString();
                }
            }
        }

        /// <summary>Gets the error output.</summary>
        /// <value>The error.</value>
        public string StdErr
        {
            get
            {
                lock (stderr)
                {
                    return stderr.ToString();
                }
            }
        }

        /// <summary>Gets the combined output.</summary>
        /// <value>The combined.</value>
        public string Combined
        {
            get
            {
                lock (combined)
                {
                    return combined.ToString();
                }
            }
        }

        /// <summary>Gets a value indicating whether this instance has exited.</summary>
        /// <value>
        /// <c>true</c> if this instance has exited; otherwise, <c>false</c>.
        /// </value>
        public bool HasExited => process.HasExited;

        /// <summary>Gets the exit code.</summary>
        /// <value>The exit code.</value>
        public int ExitCode => process.ExitCode;
    }
}
