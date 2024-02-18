using System;

namespace MameOutput_Test
{
    /// <summary>
    /// Structure contenant les informations d'un client connecté au serveur des Outputs de MAME
    /// </summary>
    public struct OutputClient
    {
        /// <summary>
        /// ID
        /// </summary>
        public UInt32 Id;
        /// <summary>
        /// Handle
        /// </summary>
        public IntPtr hWnd;
    };
}
