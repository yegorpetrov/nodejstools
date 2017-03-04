// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.NodejsTools.Debugger
{
    [Flags]
    internal enum NodeDebugOptions
    {
        None,
        /// <summary>
        /// Passing this flag to the debugger will cause it to wait for input on an abnormal (non-zero)
        /// exit code.
        /// </summary>
        WaitOnAbnormalExit = 0x01,
        /// <summary>
        /// Passing this flag to the debugger will cause it to wait for input on a normal (zero) exit code.
        /// </summary>
        WaitOnNormalExit = 0x02,
        /// <summary>
        /// Passing this flag will cause output to standard out to be redirected via the debugger
        /// so it can be outputted in the Visual Studio debug output window.
        /// </summary>
        RedirectOutput = 0x04,

        /// <summary>
        /// Set if you do not want to create a window
        /// </summary>
        CreateNoWindow = 0x40
    }
}

