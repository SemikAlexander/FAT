using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FileOS.Struct
{
    /// <summary>
    /// Max_value //битый блок
    /// Max_value - 1 //конец файла
    /// Max_value -2 //свободный блок
    /// </summary>
    public struct FS
    {
        /// <summary>
        /// Max_value     //битый блок
        /// Max_value - 1 //конец файла
        /// Max_value - 2 //свободный блок
        /// </summary>
        public UInt32 ValueNextBlock;
    }
}