using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace FileOS.Struct
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct Groups
    {
        public uint IDGroup;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public char[] NameGroup;// = new char[20];
        public uint AccessLevel;// = 0;
    }
}