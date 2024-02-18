﻿using System;
using System.Runtime.InteropServices;

namespace MameOutput_Test
{
    /// <summary>
    /// Structure used to transfer String ID data to MameHooker
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct OutputDataStruct
    {
        /// <summary>
        /// Requested ID
        /// </summary>
        public UInt32 Id;
        // The string is included inline in the data.
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
        public string lpStr;
    }
}
