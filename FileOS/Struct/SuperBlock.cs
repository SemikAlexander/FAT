using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileOS.Struct
{
   public struct SuperBlock
    {
        public uint SectorSize;     //= 512 - Размер кластера
        public uint Num_Block;      // = 0 Номер блока файла MFT
        public uint Num_Copy_MFT;   // = 0 Копия MFT
        public uint CRC;            // = 1 Контроль данных        
    }
}