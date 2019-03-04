using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace FileOS.WorkingPart
{
    class ASFS
    {
        public static string PathTofile = "ASFS.cn";
        public uint SectorSize;                      //размер сектора
        private uint _start_fs_block;                //начальный блок fs
        private int Size_struct = 0;                 //размер структуры
        public int boot_SectorSize;                  //размер загрузочного блока
        private uint count_record_on_one_sector = 0; //количество записей на один сектор
        private ulong lenght_bytes = 0;
        List<Struct.FS> address_fs_File = new List<Struct.FS>(); // номера блоков fs
        #region Установка ФС
        Struct.FS[] FAT ;
        public uint Number_Allocated_Blocks=0;
        public ASFS()
        {
            Size_struct = Marshal.SizeOf(typeof(FileOS.Struct.FS));
        }
        public bool Check_fs_Files()
        {
            if (!File.Exists(PathTofile))
                return false;
            return true;
        }
        public uint Get_Size_struct_Block(uint block_sector)
        {
            return Convert.ToUInt32(Math.Ceiling(Convert.ToDouble(Size_struct *FAT.Length/ block_sector)));
        }
        public uint Run_podbor_Size_struct_block(ulong Size_struct_disk)
        {
            uint start_block = 512;

            while (Size_struct_disk / start_block > UInt32.MaxValue - 3)
                start_block *= 2;
            FAT = new Struct.FS[Convert.ToInt32(Size_struct_disk / start_block)];
            Number_Allocated_Blocks = Convert.ToUInt32(Size_struct_disk / start_block);
            for (uint i=0;i<FAT.Length;i++) FAT[i].ValueNextBlock = uint.MaxValue - 2;
            return start_block;
        }
        public int StructByteSize()
        {
            return Size_struct * FAT.Length;
        }
        public void Add_record(uint start_block, uint lenght)
        {
            for (uint i = start_block; i < start_block + lenght; i++)
            {
                if (i+1 == start_block + lenght)
                {
                    FAT[i].ValueNextBlock = uint.MaxValue - 1;
                    break;
                }
                else
                {
                    FAT[i].ValueNextBlock = i + 1;
                }
            }
        }
        public byte[] ConvertBytes()
        {
            byte[] bt = new byte[Size_struct * FAT.Length];
            int k = 0;
            byte[] b;
            for (int i = 0; i < FAT.Length; i++)
            {
                b = FileOS.Functions.Converts.GetBytes(FAT[i]);
                b.CopyTo(bt, k);
                k += b.Length;
            }
            return bt;
        }
        #endregion
        public void InitializeSystem(uint _SectorSize,int boot_Size_struct)
        {
            boot_SectorSize = boot_Size_struct;
            SectorSize = _SectorSize;
            count_record_on_one_sector = _SectorSize / Convert.ToUInt32(Size_struct);
        }
        public void InitializeSystem_2(uint start_fs_block, ulong FileLength)
        {
            _start_fs_block = start_fs_block;
            address_fs_File.Add(new Struct.FS() { ValueNextBlock = _start_fs_block });
            bool read = true;
            using (FileStream fw = new FileStream(PathTofile, FileMode.Open))
                while (read)
                {
                    fw.Position = _start_fs_block * SectorSize + start_fs_block * Size_struct + boot_SectorSize;
                    byte[] b = new byte[Size_struct];
                    fw.Read(b, 0, Size_struct);
                    Functions.Converts.GetStrucFromBytes(b, out Struct.FS _f);
                    start_fs_block = _f.ValueNextBlock;
                    address_fs_File.Add(_f);
                    if (_f.ValueNextBlock == uint.MaxValue - 1)
                        read = false;
                }
            lenght_bytes = FileLength;
        }
        /// ищем в каком блоке файла fs находиться первый блок искомого файла
        private uint Search_fs_ValueNextBlock(uint block_File,out int number_block_fs)
        {
            for (number_block_fs = 0; number_block_fs < address_fs_File.Count; number_block_fs++)
            {
                if ((number_block_fs + 1) * count_record_on_one_sector > block_File)
                {
                    break;
                }
            }
            if (number_block_fs == 0)
                return _start_fs_block;
            return address_fs_File[number_block_fs].ValueNextBlock;
        }
        /// ищет блок данных, в котором находиться заданный байт
        private uint FindBlock(uint start_block_File, uint start_bytes,out uint FindBlock,out uint last_block, FileStream fw)
        {
             FindBlock = Convert.ToUInt32(Math.Floor(Convert.ToDouble(start_bytes) / SectorSize)); //искомый блок 
           
            {
                uint number_fs_block;
                Struct.FS f = new Struct.FS();
                
                for (uint i = 0; i < FindBlock+1; i++)
                {
                    if (i != 0) start_block_File = f.ValueNextBlock;
                    //количество блоков
                    number_fs_block = Search_fs_ValueNextBlock(i, out int Count); //номер блока файла fs
                    byte[] b = new byte[Size_struct];
                    fw.Position=boot_SectorSize+ number_fs_block*SectorSize+ (start_block_File - count_record_on_one_sector * Count) * Size_struct;
                    fw.Read(b, 0, b.Length);
                    Functions.Converts.GetStrucFromBytes(b, out f);
                    last_block = start_block_File;
                    if (f.ValueNextBlock == uint.MaxValue - 1)
                    {
                        return uint.MaxValue - 1;
                    }
                }
                last_block = start_block_File;
                return start_block_File;
            }
        }
        public byte[] ReadByBytes(uint start_block, uint count_bytes, uint start_bytes)
        {           
            byte[] b = new byte[count_bytes];
            uint number_block= 0;//номер блока данных по счету
            int count_read = 0;//количество считано байт
            int count_read_byte = 0; //количество байт, которые необходимо считать
            uint pos= start_block;
            using (FileStream fw = new FileStream(PathTofile, FileMode.Open))
                while (count_read != count_bytes)
                {
                    pos = FindBlock(pos, start_bytes, out number_block, out uint last_block, fw);//блок данных
                    if (pos == uint.MaxValue - 1)
                        pos = last_block;
                uint smeh = start_bytes - number_block * SectorSize; //позиция в блоке данных
                    if (smeh + count_bytes - count_read > SectorSize)
                    {
                        count_read_byte = Convert.ToInt32(SectorSize-smeh);
                        start_bytes += Convert.ToUInt32(count_read_byte);
                    }
                    else
                    {
                        count_read_byte =Convert.ToInt32( count_bytes - count_read);
                    }
                    ///чтение данных
                    fw.Position = boot_SectorSize + pos * SectorSize + smeh;
                    fw.Read(b, count_read, count_read_byte);
                    count_read += count_read_byte;
                }          
            return b;
        }
        public byte[] ReadByBytes(uint start_block, uint count_bytes, uint start_bytes, FileStream fw)
        {
            byte[] b = new byte[count_bytes];
            uint number_block = 0;//номер блока данных по счету
            int count_read = 0;//количество считано байт
            int count_read_byte = 0; //количество байт, которые необходимо считать
            uint pos = start_block;
                while (count_read != count_bytes)
                {
                pos = FindBlock(pos, start_bytes, out number_block, out uint last_block, fw);//блок данных
                if (pos == uint.MaxValue - 1)
                        pos = last_block;
                    uint smeh = start_bytes - number_block * SectorSize; //позиция в блоке данных
                    if (smeh + count_bytes - count_read > SectorSize)
                    {
                        count_read_byte = Convert.ToInt32(SectorSize - smeh);
                        start_bytes += Convert.ToUInt32(count_read_byte);
                        smeh = 0;
                    }
                    else
                    {
                        count_read_byte = Convert.ToInt32(count_bytes - count_read);
                    }
                    ///чтение данных
                    fw.Position = boot_SectorSize + pos * SectorSize + smeh;
                    fw.Read(b, count_read, count_read_byte);
                    count_read += count_read_byte;
                }
            return b;
        }
        public byte[] ReadFile(uint block,uint lenght)
        {
            byte[] b = new byte[lenght];
            using (FileStream fw = new FileStream(PathTofile, FileMode.Open))
            {
                fw.Position = boot_SectorSize + block * SectorSize;
                fw.Read(b, 0, Convert.ToInt32( lenght));
            }
                return b;
        }
        /// <summary>
        /// поиск номера пустого блока
        /// start_block_search - с какого блока поиск начинать.  используется для сохранения 12% под mft
        /// </summary>
        /// <returns></returns>
        private uint FindNULLBlock(uint start_block_search)
        {
            uint q= uint.MaxValue-1;
            byte[] b;
            for (ulong i = start_block_search * Convert.ToUInt64(Size_struct); i < lenght_bytes; i += Convert.ToUInt64(Size_struct))
            {
                b = ReadByBytes(_start_fs_block, Convert.ToUInt32(Size_struct), Convert.ToUInt32(i));
                Functions.Converts.GetStrucFromBytes(b, out Struct.FS f);
                if (f.ValueNextBlock == uint.MaxValue - 2)
                    return Convert.ToUInt32(i) / Convert.ToUInt32(Size_struct);
            }
            return q;
        }
        private uint FindNULLBlock(uint start_block_search, FileStream fw)
        {
            uint q = uint.MaxValue - 1;
            byte[] b;
            for (ulong i = start_block_search * Convert.ToUInt64(Size_struct); i < lenght_bytes; i += Convert.ToUInt64(Size_struct))
            {
                b = ReadByBytes(_start_fs_block, Convert.ToUInt32(Size_struct), Convert.ToUInt32(i), fw);
                Functions.Converts.GetStrucFromBytes(b, out Struct.FS f);
                if (f.ValueNextBlock == uint.MaxValue - 2)
                    return Convert.ToUInt32(i) / Convert.ToUInt32(Size_struct);
            }
            return q;
        }
        /// <summary>
        /// поиск количества пустых блоков
        /// </summary>
        /// <param name="start_block_search">с какого блока поиск начинать.  используется для сохранения 12% под mft</param>
        /// <returns></returns>
        private uint CountNullBlockForWrite(uint start_block_search)
        {
            uint q = 0;
            byte[] b,t;
            b = ReadByBytes(_start_fs_block, Convert.ToUInt32(lenght_bytes - start_block_search * Convert.ToUInt64(Size_struct)), Convert.ToUInt32(start_block_search * Convert.ToUInt64(Size_struct)));
            for (long i = 0; i < b.LongLength; i += Convert.ToInt64(Size_struct))
            {
                t = new byte[Size_struct];
                for (int k = 0; k < Size_struct; k++)
                    t[k] = b[i + k];
                Functions.Converts.GetStrucFromBytes(b, out Struct.FS f);
                if (f.ValueNextBlock == uint.MaxValue - 2)
                    q++;
            }
                return q;
        }
        private uint CountNullBlockForWrite(uint start_block_search, FileStream fw)
        {
            uint q = 0;
            byte[] b, t;
            b = ReadByBytes(_start_fs_block, Convert.ToUInt32(lenght_bytes - start_block_search * Convert.ToUInt64(Size_struct)), Convert.ToUInt32(start_block_search * Convert.ToUInt64(Size_struct)), fw);

            for (long i = 0; i < b.LongLength; i += Convert.ToInt64(Size_struct))
            {
                t = new byte[Size_struct];
                for (int k = 0; k < Size_struct; k++)
                    t[k] = b[i + k];
                Functions.Converts.GetStrucFromBytes(b, out Struct.FS f);
                if (f.ValueNextBlock == uint.MaxValue - 2)
                    q++;
            }
            return q;           
        }
        /// <summary>
        /// изменяет таблицу FS 
        /// </summary>
        private void ChangeStructASFS(Functions.StatusCodeFS code,uint block,uint value,FileStream FileStream)
        {
            uint _value_change = 0;
            switch (code)
            {
                case Functions.StatusCodeFS.EndFile:
                    _value_change = uint.MaxValue - 1;
                    break;
                case Functions.StatusCodeFS.SetNextBlock:
                    _value_change = value;

                    break;
                case Functions.StatusCodeFS.SectorEmpty:
                    _value_change = uint.MaxValue - 2;
                    break;
            }
            var number_fs_block = Search_fs_ValueNextBlock(block, out int coun); //номер блока файла fs, количество блоков
            //произвести запись в файл FS
            FileStream.Position = boot_SectorSize + number_fs_block * SectorSize + (block - count_record_on_one_sector * coun) * Size_struct;
            byte[] b = Functions.Converts.GetBytes(new Struct.FS() { ValueNextBlock = _value_change });
            FileStream.Write(b,0,b.Length);
        }
        /// запись байтов в файл. При создании файла должна передаваться длинна равным 0
        /// <param name="array"></param>
        /// <param name="start_block_File">Начальный блок. Используеться если файл существует</param>
        /// <param name="start_position">Позиция для записи в существующуй файл</param>
        /// <param name="FileLength">Размер файла</param>
        /// <param name="count_block">Количество занятых блоков</param>
        /// <param name="CountAllocatedBlocksForSystemFiles">Если записывается системный файл - значение должно быть равно 0, иначе 12% от общего количества блоков</param>
        public bool WriteBytesToFile(byte[] array,uint start_block_File,ulong start_position,ulong FileLength,uint count_block,uint CountAllocatedBlocksForSystemFiles,out uint block_add,out string error)
        {
            error = "";
            uint number_block=0;            //номер блока данных по счету
            int count_write = 0;            //количество записанных байт
            int count_write_byte = 0;       //количество байт, которые необходимо записать
            //файл только создается
            if (count_block == 0 )
            {
                block_add = 0;
                ///расчитываем сколько нужно блоков
                count_block = Convert.ToUInt32(Math.Floor(Convert.ToDouble(array.Length) / this.SectorSize));
                ///считаем и сравниваем сколько у нас пустых блоков
                if (CountNullBlockForWrite(CountAllocatedBlocksForSystemFiles) >= count_block)
                {
                    //записываем в файл и обновляем структуру FS                 
                    uint ValueNextBlock = 0;
                    using (FileStream FileStream = new FileStream(PathTofile, FileMode.Open))
                    {
                        if (array.Length == 0)
                            number_block = FindNULLBlock(CountAllocatedBlocksForSystemFiles, FileStream);
                        else
                            while (count_write < array.Length)
                            {
                                number_block = ValueNextBlock;
                                if (count_write == 0)
                                {
                                    number_block = FindNULLBlock(CountAllocatedBlocksForSystemFiles, FileStream);
                                    error = number_block.ToString();
                                }
                                else
                                {
                                    ChangeStructASFS(Functions.StatusCodeFS.SetNextBlock, number_block, ValueNextBlock, FileStream);
                                }
                                ValueNextBlock = FindNULLBlock(number_block+1,FileStream);
                                if (array.Length > SectorSize)
                                {
                                    count_write_byte = Convert.ToInt32(SectorSize);
                                }
                                else
                                {
                                    count_write_byte = array.Length - count_write;
                                }
                                FileStream.Position = boot_SectorSize + number_block * SectorSize;
                                FileStream.Write(array, count_write, count_write_byte);
                                count_write += count_write_byte;
                                ChangeStructASFS(Functions.StatusCodeFS.EndFile, number_block, ValueNextBlock, FileStream);
                            }
                        return true;
                    }
                }
                else
                {
                    error = "Не достаточно места на диске";
                    return false;
                }
            }
            //запись в существующий файл
            block_add = 0;
            ////проверка места
           int NeedARRAY=0;
            if (FileLength - start_position - Convert.ToUInt64(array.LongLength) < 0)
                NeedARRAY = (int)Math.Floor(Convert.ToDouble(FileLength - start_position)/SectorSize); //в блоках
            else NeedARRAY = array.Length; //вмещается
            if (NeedARRAY != array.Length)
            {
                if (CountNullBlockForWrite(CountAllocatedBlocksForSystemFiles) < NeedARRAY)
                {
                    error = "Недостаточно места на жестком диске";
                    return false;
                }
            }
            using (FileStream fw = new FileStream(PathTofile, FileMode.Open))
                do
                {
                    uint pos = FindBlock(start_block_File, Convert.ToUInt32(start_position), out number_block, out uint last_block, fw);
                    if (pos == uint.MaxValue - 1)
                    {
                        if (SectorSize * (number_block+1) < start_position + Convert.ToUInt64(array.Length))
                        {
                            pos = FindNULLBlock(CountAllocatedBlocksForSystemFiles, fw);
                            if (pos == uint.MaxValue - 1)
                            {
                                error = "Недостаточно места на жестком диске";
                                return false;
                            }
                            ChangeStructASFS(Functions.StatusCodeFS.SetNextBlock, last_block, pos, fw);
                            ChangeStructASFS(Functions.StatusCodeFS.EndFile, pos, pos, fw);
                            number_block += 1;
                            block_add += 1;
                        }
                        else
                          if (SectorSize * (number_block+1)-SectorSize >= FileLength && start_position+Convert.ToUInt64(array.Length)>=FileLength)
                            {
                            block_add = uint.MaxValue;
                            ChangeStructASFS(Functions.StatusCodeFS.SectorEmpty, last_block, pos, fw);
                            
                            ChangeStructASFS(Functions.StatusCodeFS.EndFile, FindBlock(start_block_File, Convert.ToUInt32((number_block+1)*Size_struct-2), out number_block, out last_block, fw), pos, fw);
                        }
                            else
                            {
                                pos = last_block;
                            }
                    }
                    ulong smeh = start_position - number_block * SectorSize; //позиция в блоке данных
                    if (smeh + Convert.ToUInt64( array.LongLength) - Convert.ToUInt64(count_write) > SectorSize)
                    {
                        count_write_byte = Convert.ToInt32(SectorSize - smeh);
                        start_position += Convert.ToUInt32(count_write_byte);
                    }
                    else
                    {
                        count_write_byte = Convert.ToInt32(array.Length - count_write);
                    }
                    fw.Position = Convert.ToInt64(Convert.ToUInt64(boot_SectorSize) + Convert.ToUInt64(pos * SectorSize) + smeh);
                    fw.Write(array, count_write, count_write_byte);
                    count_write += count_write_byte;    
                }
                while (count_write != array.Length);
            return true;
        }
        /// <summary>
        /// создает копию файла
        /// </summary>
        /// <param name="Firs12PersentForMFT">отступ для 12 % под mft зону</param>
        /// <param name="start_block_source">первый блок исходного файла</param>
        /// <param name="FileLength_source">длина исходного файла</param>
        /// <param name="start_block_CopyFile">возрат начального блока нового файла</param>
        /// <param name="error">если ошибка - содержится тескст ошибки</param>
        /// <returns>лог значение выполнена или нет копия файла</returns>
        public bool CopyFile(uint Firs12PersentForMFT, uint start_block_source, ulong FileLength_source, out uint start_block_CopyFile, out string error)
        {
            start_block_CopyFile = 0;error = "";
            var Number_Allocated_Blocks = Convert.ToUInt32(Math.Floor(Convert.ToDouble(FileLength_source) / this.SectorSize));
            uint count_bl = 0;
            var Number_Allocated_Blocks_free = CountNullBlockForWrite(Firs12PersentForMFT);
            if (Number_Allocated_Blocks_free < Number_Allocated_Blocks){
                error = "Не хватает места на жестком диске";
                return false;
            }
            ///начинаем копировать файл
            start_block_CopyFile = CountNullBlockForWrite(Firs12PersentForMFT);
            uint block = 0; 
            ulong lenght_end_File = 0;
            byte[] b;
            do
            {
                b = new byte[SectorSize];
                b = this.ReadByBytes(start_block_source, Convert.ToUInt32(SectorSize), Convert.ToUInt32(lenght_end_File));
                if (!this.WriteBytesToFile(b, start_block_CopyFile, Convert.ToUInt32(lenght_end_File), FileLength_source, block, Firs12PersentForMFT, out block, out error))
                    return false;
                count_bl += block;
                lenght_end_File += Convert.ToUInt32(SectorSize);
            }
            while (lenght_end_File < FileLength_source);
            if (count_bl == Number_Allocated_Blocks) return true;
            else 
            return false;
        }
        public bool DeleteFile(uint start_block_source)
        {
            uint i = 0, FindBlocks=0,s = uint.MaxValue - 1, last_block = 0;
            using (FileStream fw = new FileStream(PathTofile, FileMode.Open))
                do
                {
                    FindBlocks = FindBlock(start_block_source, i * SectorSize, out FindBlocks, out last_block,fw);
                    if (s == FindBlocks) return false;
                    if (FindBlocks == uint.MaxValue - 1)
                        FindBlocks = last_block;
                    ChangeStructASFS(Functions.StatusCodeFS.SectorEmpty, FindBlocks, 0, fw);
                    if (FindBlocks == uint.MaxValue - 1)
                        return true;
                    s = FindBlocks;                   
                } while (true);
        }
    }
}