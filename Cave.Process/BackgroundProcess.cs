#region CopyRight 2018
/*
    Copyright (c) 2003-2018 Andreas Rohleder (andreas@rohleder.cc)
    All rights reserved
*/
#endregion
#region License AGPL
/*
    This program/library/sourcecode is free software; you can redistribute it
    and/or modify it under the terms of the GNU Affero General Public License
    version 3 as published by the Free Software Foundation subsequent called
    the License.

    You may not use this program/library/sourcecode except in compliance
    with the License. The License is included in the LICENSE.AGPL30 file
    found at the installation directory or the distribution package.

    Permission is hereby granted, free of charge, to any person obtaining
    a copy of this software and associated documentation files (the
    "Software"), to deal in the Software without restriction, including
    without limitation the rights to use, copy, modify, merge, publish,
    distribute, sublicense, and/or sell copies of the Software, and to
    permit persons to whom the Software is furnished to do so, subject to
    the following conditions:

    The above copyright notice and this permission notice shall be included
    in all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
    MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
    NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
    LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
    OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
    WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
#endregion
#region Authors & Contributors
/*
   Author:
     Andreas Rohleder <andreas@rohleder.cc>

   Contributors:
 */
#endregion Authors & Contributors

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Cave.IO;

namespace Cave
{
	/// <summary>
	/// Provides a process implementation handling stdout, stderr and stdin with events.
	/// </summary>
    public sealed class BackgroundProcess : IDisposable
    {
        Process m_Process;
        Task m_OutputTask;
        Task m_ErrorTask;
        bool m_ExitEventCalled;

        void ReadOutput()
        {
            try
            {
                while (!m_Process.StandardOutput.EndOfStream)
                {
                    string line = m_Process.StandardOutput.ReadLine();
                    if (line == null) continue;

                    EventHandler<LineEventArgs> evt = ReadLineOutput;
                    if (evt != null)
                    {
                        evt.Invoke(this, new LineEventArgs(line));
                    }
                }
            }
            catch (Exception ex)
            {
                lock (this) if (!m_ExitEventCalled) ReadException(ex);
            }
            finally
            {
                lock (this) if (!m_ExitEventCalled) ProcessExited(this, new EventArgs());
            }
        }

        void ReadError()
        {
            try
            {
                while (!m_Process.StandardError.EndOfStream)
                {
                    string line = m_Process.StandardError.ReadLine();
                    if (line == null) continue;

                    EventHandler<LineEventArgs> evt = ReadLineError;
                    if (evt != null)
                    {
                        evt.Invoke(this, new LineEventArgs(line));
                    }
                }
            }
            catch (Exception ex)
            {
                lock (this) if (!m_ExitEventCalled) ReadException(ex);
            }
            finally
            {
                lock (this) if (!m_ExitEventCalled) ProcessExited(this, new EventArgs());
            }
        }

        void ReadException(Exception ex)
        {
            EventHandler<ExceptionEventArgs> error = Error;
            if (error != null)
            {
                try { error.Invoke(this, new ExceptionEventArgs(ex)); }
                catch { }
            }
        }

        void ProcessExited(object sender, EventArgs e)
        {
            lock (this)
            {
                if (m_Process == null) return;
                if (m_ExitEventCalled) return;
                m_ExitEventCalled = true;
            }
            EventHandler<ExitEventArgs> evt = Exited;
            if (evt != null)
            {
                try { evt.Invoke(this, new ExitEventArgs(m_Process.ExitCode)); }
                catch { }
            }
        }

        /// <summary>
        /// Callback on process exit
        /// </summary>
        public EventHandler<ExitEventArgs> Exited;

        /// <summary>
        /// Callback on process stdout.writeline
        /// </summary>
        public EventHandler<LineEventArgs> ReadLineOutput;

        /// <summary>
        /// Callback on process stderr.writeline
        /// </summary>
        public EventHandler<LineEventArgs> ReadLineError;

        /// <summary>
        /// Callback on process exception
        /// </summary>
        public EventHandler<ExceptionEventArgs> Error;

        /// <summary>
        /// Creates a new background process
        /// </summary>
        public BackgroundProcess()
        {
        }

        /// <summary>
        /// Obtains whether the process is still running, or not
        /// </summary>
        public bool Running { get { return Started && (m_Process != null) && (!m_Process.HasExited); } }

        /// <summary>
        /// Obtains whether the proces was already started or not
        /// </summary>
        public bool Started { get; private set; }

        /// <summary>
        /// Obtains the fileName of the started process
        /// </summary>
        public string FileName { get { return m_Process.StartInfo.FileName; } }

        /// <summary>
        /// Obtains the arguments of the started process
        /// </summary>
        public string Arguments { get { return m_Process.StartInfo.Arguments; } }

        /// <summary>
        /// Starts a new processes
        /// </summary>
        /// <param name="fileName">fileName of the process</param>
        /// <param name="args">arguments of the process</param>
        public void Start(string fileName, string args)
        {
            lock (this)
            {
                if (Started) throw new InvalidOperationException("Process already started!");
                m_Process = new Process();
                m_Process.StartInfo = new ProcessStartInfo(fileName, args)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                };
                m_Process.Exited += ProcessExited;
                m_Process.Start();
                Started = true;
                m_OutputTask = Task.Factory.StartNew(ReadOutput);
                m_ErrorTask = Task.Factory.StartNew(ReadError);
            }
        }

        /// <summary>
        /// Waits for process completion
        /// </summary>
        /// <param name="timeout">maximum time to wait</param>
        /// <param name="throwEx">Throw exception if the process does not exit in time</param>
        /// <returns></returns>
        public bool Wait(TimeSpan timeout, bool throwEx)
        {
            return Wait(timeout, null, throwEx);
        }

        /// <summary>
        /// Waits for process completion
        /// </summary>
        /// <param name="timeout">maximum time to wait</param>
        /// <param name="waitCallBack">Callback while waiting for process exit (should not use up more then 1000ms)</param>
        /// <param name="throwEx">Throw exception if the process does not exit in time</param>
        /// <returns></returns>
        public bool Wait(TimeSpan timeout, WaitAction waitCallBack, bool throwEx)
        {
            IStopWatch watch = DateTimeStopWatch.StartNew();
            bool exit = false;
            while (!exit && (watch.Elapsed < timeout))
            {
                if (Task.WaitAll(new Task[] { m_ErrorTask, m_OutputTask }, 1)) return true;
                waitCallBack?.Invoke(out exit);
            }
            if (throwEx) throw new TimeoutException();
            return false;
        }

        /// <summary>
        /// Writes to the process' stdin
        /// </summary>
        /// <param name="text"></param>
        public void Write(string text)
        {
            m_Process.StandardInput.Write(text);
        }

        /// <summary>
        /// Writes to the process' stdin
        /// </summary>
        /// <param name="text"></param>
        public void WriteLine(string text)
        {
            m_Process.StandardInput.WriteLine(text);
        }

        /// <summary>
        /// Closes this instance. May kill the process if it is still running and returns the exit code.
        /// </summary>
        public int Close()
        {
            int exitCode = -1;
            if (m_Process != null)
            {
                //do not call exit event when user kills the process
                m_ExitEventCalled = true;
                try { if (!m_Process.HasExited) m_Process.Kill(); }
                catch { }
                try { Task.WaitAll(m_ErrorTask, m_OutputTask); }
                catch { }
                exitCode = m_Process.ExitCode;
                m_Process.Close();
            }
            Dispose();
            return exitCode;
        }

        /// <summary>
        /// Disposes this instance
        /// </summary>
        public void Dispose()
        {
            if (m_Process != null)
            {
                m_Process.Dispose();
                m_Process = null;
            }
        }
    }
}
