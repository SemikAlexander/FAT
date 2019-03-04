using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
namespace FileOS.WorkingPart
{
    class Groups
    {
        List<FileOS.Struct.Groups> gr = new List<Struct.Groups>();

        int Size_struct = 0;
        private ASFS _fs;
        Struct.MFT group_info;
        public Groups()
        {
            Size_struct = Marshal.SizeOf(typeof(Struct.Groups));
        }
        public uint SizeStructInBlock(uint block_sector)
        {
            return Convert.ToUInt32(Math.Ceiling(Convert.ToDouble(Size_struct * gr.Count ) / block_sector));
        }
        public bool AddGroup(string name, uint Access_Level)
        {
            Struct.Groups u = new Struct.Groups
            {
                AccessLevel = Access_Level
            };
            if (name.Length > 20) u.NameGroup = name.Substring(0, 20).ToCharArray();
            else if (name.Length == 20) u.NameGroup = name.ToCharArray();
            else
            {
                u.NameGroup = new char[20];
                string tmp = "";
                for (int i = 0; i < 20 - name.Length; i++) tmp+=' ';

                name.ToCharArray().CopyTo(u.NameGroup, 0);
                tmp.ToCharArray().CopyTo(u.NameGroup, name.Length);
            }
            Struct.Groups k = gr.Find((x) => x.NameGroup == u.NameGroup);
            if (k.NameGroup != null)
                return false;
            u.IDGroup = Convert.ToUInt32(gr.Count);
            gr.Add(u);
            return true;
        }
        public int StructByteSize()
        {
            return Size_struct * gr.Count;
        }
        public byte[] ConvertBytes()
        {
            byte[] bt = new byte[Size_struct * gr.Count];
            byte[] b; int k = 0;

            for (int i = 0; i < gr.Count; i++)
            {
                b = Functions.Converts.GetBytes(gr[i]);
                b.CopyTo(bt, k);
                k += b.Length;
            }
            return bt;
        }
        public uint GetAccessLevelGroup(Users userclass,uint userid)
        {
          return LoadGroup((userclass.GetUsers().Find((x)=>x.Id_User== userid).Id_Group)).AccessLevel;
        }
        public void InitializeGroups(ASFS fs,Struct.MFT group_info1)
        {
            _fs = fs;
            group_info = group_info1;
        }
        public Struct.Groups LoadGroup(uint id)
        {
            Struct.Groups gr=new Struct.Groups();
            if (Convert.ToUInt64(id * Size_struct) > group_info.BusyByte) return new Struct.Groups();
            Functions.Converts.GetStrucFromBytes(_fs.ReadByBytes(group_info.AdressBlockFile,Convert.ToUInt32( Size_struct), Convert.ToUInt32(id *Size_struct)), out gr);
            return gr;
        }
        public Struct.Groups[] LoadGroups()
        {
            Struct.Groups[] t = new Struct.Groups[group_info.BusyByte / Convert.ToUInt32(Size_struct)];
            for (uint i = 0; i < group_info.BusyByte / Convert.ToUInt32(Size_struct); i++)
              t[i] = LoadGroup(i);
            return t;
        }
    }
}