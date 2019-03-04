using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FileOS.WorkingPart
{
    class Boot
    {
        Struct.SuperBlock SB;
        int size_struct = 0;
        #region Install
        public int Size_struct
        {
            get
            {
                return size_struct;
            }
        }
        public Boot()
        {
            size_struct = Marshal.SizeOf(typeof(Struct.SuperBlock));
        }
        public uint Get_Size_struct_block()
        {
            return SB.SectorSize;
        }
        public void Init_Boot(uint Size_struct_block, uint start_mft, uint copy_mft)
        {
            SB.SectorSize = Size_struct_block;
            SB.Num_Block = start_mft;
            SB.Num_Copy_MFT = copy_mft;
            SB.CRC = Convert.ToUInt32(Math.Floor((double)(SB.Num_Copy_MFT + Size_struct_block + start_mft) / 4));
        }
        public int StructByteSize()
        {
            return Size_struct;
        }
        public uint Get_number_mft_block()
        {
            return SB.Num_Block;
        }
        public uint Get_number_copy_mft_block()
        {
            return SB.Num_Copy_MFT;
        }
        public byte[] ConvertBytes()
        {
            byte[] b;
            {
                b = Functions.Converts.GetBytes(SB);
            }
            return b;
        }      
        #endregion
        public bool Read_BootSect()
        {
            byte[] bt = new byte[Size_struct];
            using (FileStream fw = new FileStream(ASFS.PathTofile, FileMode.Open))
            {
                fw.Position = 0;
                fw.Read(bt, 0, Size_struct);
            }
            Functions.Converts.GetStrucFromBytes(bt, out SB);
            if (Convert.ToUInt32(Math.Floor((double)(SB.SectorSize + SB.Num_Block) / 3)) == SB.CRC)
                return true;
            else
            {
                using (FileStream fw = new FileStream(ASFS.PathTofile, FileMode.Open))
                {
                    fw.Position = new FileInfo(ASFS.PathTofile).Length - Size_struct;
                    
                    fw.Read(bt, 0, Size_struct);
                }
                if (Convert.ToUInt32(Math.Floor((double)(SB.SectorSize + SB.Num_Copy_MFT + SB.Num_Block) / 4)) == SB.CRC)
                    return true;
                else return false;
            }
        }       
    }
}