using System.Runtime.InteropServices;

namespace FileOS.Struct
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]

    public struct Users
    {
        public uint Id_User;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)]
        public char[] User_Name;//= new char[50];
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
        public char[] Login;//= new char[15];
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public char[] Hesh_Password;//= new char[20];
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 22)]
        public char[] Home_Directory;// = new char[28];/users/
        public uint Id_Group;
    }
}