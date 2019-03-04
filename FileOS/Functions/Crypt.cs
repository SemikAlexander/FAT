
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;

namespace FileOS.Functions
{
    static class Heshing
    {
        public static char[] Hash(string input, int lenght)
        {
            using (SHA1Managed sha1 = new SHA1Managed())
            {
                var HASH = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
                var SB = new StringBuilder(HASH.Length * 2);

                foreach (byte B in HASH)
                {
                    SB.Append(B.ToString("X2"));
                }
                return SB.ToString().Substring(0, lenght).ToCharArray();
            }
        }
    }
    public static class Converts
    {
        public static byte[] GetBytes<T>(T str)
        {
            int Size_struct = Marshal.SizeOf(typeof(T));
            byte[] arr = new byte[Size_struct];

            IntPtr ptr = Marshal.AllocHGlobal(Size_struct);
            Marshal.StructureToPtr(str, ptr, true);
            Marshal.Copy(ptr, arr, 0, Size_struct);
            Marshal.FreeHGlobal(ptr);
            return arr;
        }
        public static void GetStrucFromBytes<T>(byte[] arr, out T t)
        {
            int Size_struct = Marshal.SizeOf(typeof(T));
            IntPtr ptr = Marshal.AllocHGlobal(Size_struct);

            Marshal.Copy(arr, 0, ptr, Size_struct);

            t = (T)Marshal.PtrToStructure(ptr, typeof(T));
            Marshal.FreeHGlobal(ptr);
        }
    }
    enum StatusCodeFS
    {
        SetNextBlock,
        EndFile,
        SectorEmpty
    }
}
