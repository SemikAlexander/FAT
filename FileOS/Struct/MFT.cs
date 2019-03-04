using System;
using System.Runtime.InteropServices;

namespace FileOS.Struct
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    struct MFT
    {
        public uint Record_Number;                                //=0;
        public uint Number_Allocated_Blocks;                      //=0;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
        public char[] FileName;                                  //= new char[25];
        public ulong BusyByte;                                   //=0;
        public char FileType;                                    // = 'd', '-';
        public uint Base_Record_Number;                          // = 0;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
        public char[] Access_Level;                              // = new bool[9]; //доступ к файлу
        public uint UserID;                                      // = 0;
        public uint File_Attributes;                             // = new bool[3] { false, false,false };//зарезервирован, системный, скрыт
        public long Time;                                        // = new DateTime();
        public uint AdressBlockFile;// = 0;

    }
}
