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
#endregion License
#region Authors & Contributors
/*
   Author:
     Andreas Rohleder <andreas@rohleder.cc>

   Contributors:
 */
#endregion Authors & Contributors

using System;

namespace Cave
{
    /// <summary>
    /// Provides the result of a process execution.
    /// </summary>
    public class ProcessResult
    {
        /// <summary>Initializes a new instance of the <see cref="ProcessResult"/> class.</summary>
        /// <param name="startException">The start exception.</param>
        public ProcessResult(Exception startException)
        {
            StartException = startException;
            ExitCode = int.MinValue;
            Combined = startException.Message;
            StdErr = startException.Message;
        }

        /// <summary>Initializes a new instance of the <see cref="ProcessResult"/> class.</summary>
        /// <param name="combined">The combined.</param>
        /// <param name="output">The output.</param>
        /// <param name="error">The error.</param>
        /// <param name="exitCode">The exit code.</param>
        public ProcessResult(string combined, string output, string error, int exitCode)
        {
            Combined = combined;
            StdOut = output;
            StdErr = error;
            ExitCode = exitCode;
        }

        /// <summary>Gets the exit code.</summary>
        /// <value>The exit code.</value>
        public int ExitCode { get; private set; }

        /// <summary>Gets the complete standard output of the process.</summary>
        /// <value>The output.</value>
        public string StdOut { get; private set; }

        /// <summary>Gets the complete error output of the process.</summary>
        /// <value>The error.</value>
        public string StdErr { get; private set; }

        /// <summary>Gets the combined output of the process.</summary>
        /// <value>The combined.</value>
        public string Combined { get; private set; }

        /// <summary>Gets the start exception.</summary>
        /// <value>The start exception.</value>
        public Exception StartException { get; private set; }
    }
}
