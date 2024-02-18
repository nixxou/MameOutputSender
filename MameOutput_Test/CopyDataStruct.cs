using System;
using System.Runtime.InteropServices;

namespace MameOutput_Test
{
    /// <summary>
    /// Structure WIN32 native utilisée pour passer des information dans les messages windows de type WM_COPYDATA
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    class CopyDataStruct
    {
        public IntPtr dwData;
        public int cbData;
        public IntPtr lpData;
    }
}
