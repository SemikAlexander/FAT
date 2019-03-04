using FileOS.Functions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace FileOS.WorkingPart
{
    public enum Operations
    {
        Read,
        Write,
        ReadWrite,
    }
    class MFT
    {
        List<Struct.MFT> _mft = new List<Struct.MFT>();
        uint copy_mft_block;
        uint mft_block;
        public Groups gr;
        public Users usr;
        #region Установка файловой системы
        int Sizе = 0;
        public MFT()
        {
            Sizе = Marshal.SizeOf(typeof(Struct.MFT));
        }
        public bool AddRecordInMFT(uint Number_Allocated_Block, string FN, ulong bytes, string Type, uint Base_Record_Number, string Access_Level, uint UserID, uint ATTR, DateTime Time, DateTime edit_File, uint AdressBlockFile)
        {
            Struct.MFT u = new Struct.MFT
            {
                AdressBlockFile = AdressBlockFile,
                Time = Time.ToBinary(),
                File_Attributes = ATTR,
                UserID = UserID,
                Access_Level = Access_Level.ToCharArray(),
                Base_Record_Number = Base_Record_Number,
                FileType = Convert.ToChar(Type),
                BusyByte = bytes,
                Number_Allocated_Blocks = Number_Allocated_Block,
                Record_Number = Convert.ToUInt32(_mft.Count) + 1
            };
            if (FN.Length > 25) u.FileName = FN.Substring(0, 25).ToCharArray();
            else if (FN.Length == 25) u.FileName = FN.ToCharArray();
            else
            {
                u.FileName = new char[25];
                string tmp = "";
                for (int i = 0; i < 25 - FN.Length; i++) tmp += ' ';
                FN.ToCharArray().CopyTo(u.FileName, 0);
                tmp.ToCharArray().CopyTo(u.FileName, FN.Length);
            }
            Struct.MFT k = _mft.Find((x) => x.Base_Record_Number == u.Base_Record_Number && x.FileName == u.FileName && x.FileType == u.FileType);
            if (k.FileName != null)
                return false;
            _mft.Add(u);
            return true;
        }
        public int StructByteSize()
        {
            return Sizе * _mft.Count;
        }
        public int StructRecSize(int count, uint _Size_struct_claster)
        {
            return Convert.ToInt32(Math.Ceiling(Convert.ToDouble(Sizе * count) / _Size_struct_claster));
        }
        public int StructRecSize(int count)
        {
            return Convert.ToInt32(Sizе * count);
        }
        public byte[] ConvertBytes()
        {
            byte[] bt = new byte[Sizе * _mft.Count];
            int k = 0;
            byte[] b;
            for (int i = 0; i < _mft.Count; i++)
            {
                b = Converts.GetBytes(_mft[i]);
                b.CopyTo(bt, k);
                k += b.Length;
            }
            return bt;
        }
        #endregion
        ASFS asfs;
        public void ReadSysRec(uint Size_struct_boot_sector, uint start_block, uint copy_mft, uint SectorSize, int count_Files, ASFS f)
        {
            mft_block = start_block;
            copy_mft_block = copy_mft;
            asfs = f;
            _mft.Clear();
            byte[] bt;
            using (FileStream fw = new FileStream(ASFS.PathTofile, FileMode.Open))
            {
                fw.Position = Size_struct_boot_sector + start_block * SectorSize;
                for (int i = 0; i < count_Files; i++)
                {
                    bt = new byte[Sizе];
                    fw.Read(bt, 0, Sizе);
                    Converts.GetStrucFromBytes(bt, out Struct.MFT tmp);
                    _mft.Add(tmp);
                }
            }
        }
        private Struct.MFT SearchSystemInformation(string Filename)
        {
            char[] k = new char[25];
            if (Filename.Length > 25) k = Filename.Substring(0, 25).ToCharArray();
            else if (Filename.Length == 25) k = Filename.ToCharArray();
            else
            {
                string tmp = "";
                for (int i = 0; i < 25 - Filename.Length; i++) tmp += ' ';
                Filename.ToCharArray().CopyTo(k, 0);
                tmp.ToCharArray().CopyTo(k, Filename.Length);
            }
            foreach (Struct.MFT m in _mft)
            {
                if (m.FileName.SequenceEqual(k))
                {
                    return m;
                }
            }
            return new Struct.MFT() { FileName = null };
        }
        public Struct.MFT SystemInfoAboutFile(string Filename)
        {
            Struct.MFT m = SearchSystemInformation(Filename);

            if (m.FileName == null)
            {
                _mft.Clear();
                byte[] b = asfs.ReadFile(copy_mft_block, Convert.ToUInt32(Sizе) * 5);
                byte[] tmp;
                for (int i = 0; i < 5; i++)
                {
                    tmp = new byte[Sizе];
                    for (int k = i * Sizе; k < i * Sizе + Sizе; k++)
                        tmp[k - i * Sizе] = b[k];

                    Converts.GetStrucFromBytes(tmp, out m);
                    _mft.Add(m);
                }
                m = SearchSystemInformation(Filename);
            }
            return m;
        }
        public Struct.MFT Get_File_info(string Filename, uint Base_Record_Number, bool check_dir)
        {
            char[] k = new char[25];
            if (Filename.Length > 25) k = Filename.Substring(0, 25).ToCharArray();
            else if (Filename.Length == 25) k = Filename.ToCharArray();
            else
            {
                string tmp = "";

                for (int i = 0; i < 25 - Filename.Length; i++) tmp += ' ';
                Filename.ToCharArray().CopyTo(k, 0);
                tmp.ToCharArray().CopyTo(k, Filename.Length);
            }
            var mft = this.SearchSystemInformation("MFT");
            uint count_read = 0; byte[] b;
            while (count_read < mft.BusyByte)
            {
                var m = new Struct.MFT();
                b = new byte[Sizе];
                b = asfs.ReadByBytes(mft.AdressBlockFile, Convert.ToUInt32(Sizе), count_read);
                Converts.GetStrucFromBytes(b, out m);
                if (m.Base_Record_Number == Base_Record_Number && m.FileName.SequenceEqual(k))
                {
                    if (check_dir)
                    {
                        if (m.FileType == 'd')
                            return m;
                    }
                    else
                        return m;
                }
                count_read += Convert.ToUInt32(this.Sizе);
            }
            return new Struct.MFT();
        }
        public bool IsPathExist(string path)
        {
            uint id = SearchSystemInformation(".").Record_Number;
            string[] elem = path.Split('/');
            for (int i = 1; i < elem.Length; i++)
            {
                if (elem[i] == "") continue;
                var st = Get_File_info(elem[i], id, true);
                if (st.FileName == null | st.FileType == '-')
                {
                    return false;
                }
                id = st.Record_Number;
            }
            return true;
        }
        public bool AccessFile(Struct.MFT FileInf, uint IDUser, Operations operations)
        {
            var gr_id_usr = gr.GetAccessLevelGroup(usr, usr.user.Id_User);
            var gr_id_dir = gr.GetAccessLevelGroup(usr, FileInf.UserID);
            if (IDUser != 1)
            {
                switch (operations)
                {
                    case Operations.Read:
                        if (IDUser == FileInf.UserID)
                        {
                            if (FileInf.Access_Level[0] == 'r')
                                return true;
                            else
                                return false;
                        }
                        else if (gr_id_usr == gr_id_dir)
                        {
                            if (FileInf.Access_Level[3] == 'r')
                                return true;
                            else
                                return false;
                        }
                        else
                        {
                            if (FileInf.Access_Level[6] == 'r')
                                return true;
                            else
                                return false;
                        }
                    case Operations.Write:
                        if (IDUser == FileInf.UserID | IDUser == 1)
                        {
                            if (FileInf.Access_Level[1] == 'w')
                                return true;
                            else
                                return false;
                        }
                        else if (gr_id_usr == gr_id_dir)
                        {
                            if (FileInf.Access_Level[4] == 'w')
                                return true;
                            else
                                return false;
                        }
                        else
                        {
                            if (FileInf.Access_Level[7] == 'w')
                                return true;
                            else
                                return false;
                        }
                    case Operations.ReadWrite:
                        if (IDUser == FileInf.UserID | IDUser == 1)
                        {
                            if (FileInf.Access_Level[0] == 'r' & FileInf.Access_Level[1] == 'w')
                                return true;
                            else
                                return false;
                        }
                        else if (gr_id_usr == gr_id_dir)
                        {
                            if (FileInf.Access_Level[3] == 'r' & FileInf.Access_Level[4] == 'w')
                                return true;
                            else
                                return false;
                        }
                        else
                        {
                            if (FileInf.Access_Level[6] == 'r' & FileInf.Access_Level[7] == 'w')
                                return true;
                            else
                                return false;
                        }
                }
            }
            return true;
        }
        private uint GetCurrentPathID(string path)
        {
            uint id = SearchSystemInformation(".").Record_Number;
            string[] elem = path.Split('/');
            for (int i = 1; i < elem.Length; i++)
            {
                if (elem[i] == "")
                    continue;
                id = Get_File_info(elem[i], id, false).Record_Number;
            }
            return id;
        }
        private uint GetCurrentPathDirectory(string path, out Struct.MFT mFT)
        {
            uint id = SearchSystemInformation(".").Record_Number;
            mFT = new Struct.MFT();
            string[] elem = path.Split('/');
            for (int i = 1; i < elem.Length; i++)
            {
                if (elem[i] == "")
                    continue;
                mFT = Get_File_info(elem[i], id, false);
                id = mFT.Record_Number;
            }
            return id;
        }
        public List<Struct.MFT> GetFilesDirectory(string path, out uint last_id)
        {
            List<Struct.MFT> Files = new List<Struct.MFT>();
            var gr_id_usr = gr.GetAccessLevelGroup(usr, usr.user.Id_User);
            uint id = GetCurrentPathID(path);
            var mft = SearchSystemInformation("MFT");
            uint count_read = 0; byte[] b; 
            last_id = 0;
            while (count_read < mft.BusyByte)
            {
                var m = new Struct.MFT();
                b = new byte[Sizе];
                b = asfs.ReadByBytes(mft.AdressBlockFile, Convert.ToUInt32(Sizе), count_read);
                Converts.GetStrucFromBytes(b, out m);
                var gr_id_dir = gr.GetAccessLevelGroup(usr, m.UserID);
                if (m.Base_Record_Number == id & (gr_id_dir >= gr_id_usr | gr_id_dir == 1))
                    Files.Add(m);
                last_id = (m.Record_Number > last_id) ? m.Record_Number : last_id;
                count_read += Convert.ToUInt32(Sizе);
            }
            return Files;
        }
        private bool IsFileExistInDir(string path, string Filename, out char[] k)
        {
            if (Filename.Length > 25) k = Filename.Substring(0, 25).ToCharArray();
            else if (Filename.Length == 25) k = Filename.ToCharArray();
            else
            {
                k = new char[25];
                string tmp = "";

                for (int i = 0; i < 25 - Filename.Length; i++) tmp += ' ';
                Filename.ToCharArray().CopyTo(k, 0);
                tmp.ToCharArray().CopyTo(k, Filename.Length);
            }
            uint id = GetCurrentPathID(path);
            var mft = this.SearchSystemInformation("MFT");
            uint count_read = 0; byte[] b;
            while (count_read < mft.BusyByte)
            {
                var m = new Struct.MFT();
                b = new byte[Sizе];
                b = asfs.ReadByBytes(mft.AdressBlockFile, Convert.ToUInt32(Sizе), count_read);
                Converts.GetStrucFromBytes(b, out m);
                if (m.Base_Record_Number == id && m.FileType == '-' && m.FileName.SequenceEqual(k))
                    return true;
                count_read += Convert.ToUInt32(this.Sizе);
            }
            return false;
        }
        private bool IsFileExistInDir(string path, string Filename, out Struct.MFT m)
        {
            char[] k;
            if (Filename.Length > 25) k = Filename.Substring(0, 25).ToCharArray();
            else if (Filename.Length == 25) k = Filename.ToCharArray();
            else
            {
                k = new char[25];
                string tmp = "";

                for (int i = 0; i < 25 - Filename.Length; i++) tmp += ' ';
                Filename.ToCharArray().CopyTo(k, 0);
                tmp.ToCharArray().CopyTo(k, Filename.Length);
            }
            uint id = GetCurrentPathID(path);
            var mft = this.SearchSystemInformation("MFT");
            uint count_read = 0; byte[] b;
            while (count_read < mft.BusyByte)
            {
                m = new Struct.MFT();
                b = new byte[Sizе];
                b = asfs.ReadByBytes(mft.AdressBlockFile, Convert.ToUInt32(Sizе), count_read);
                Converts.GetStrucFromBytes(b, out m);
                if (m.Base_Record_Number == id && m.FileType == '-' && m.FileName.SequenceEqual(k))
                    return true;

                count_read += Convert.ToUInt32(this.Sizе);
            }
            m = new Struct.MFT();
            return false;
        }
        #region Command which user input in console
        public string ShowFiles(string Path, uint UserID)
        {
            string result = "";
            List<Struct.MFT> f = GetFilesDirectory(Path, out uint last_id);
            foreach (Struct.MFT mFT in f)
            {
                if (AccessFile(mFT, UserID, Operations.Read))
                {
                    result += $"{mFT.FileType}{new string(mFT.Access_Level),5} {new string(mFT.FileName).Trim(' '),5} {mFT.BusyByte,5} {mFT.Number_Allocated_Blocks * asfs.SectorSize,5} {DateTime.FromBinary(mFT.Time).ToString(),20}\n";
                }
            }
            return result;
        }
        public bool MoveFile(string FromWhere, string Where)
        {
            //проверки пути
            string[] s_path_elem = FromWhere.Split('/');
            string[] d_path_elem = Where.Split('/');

            string s_path = ""; string d_path = "";

            for (int i = 0; i < s_path_elem.Length - 1; i++)
                s_path += s_path_elem[i] + '/';
            if (!IsPathExist(s_path))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Исходный путь не найден");
                Console.ResetColor();
                return false;
            }
            for (int i = 0; i < d_path_elem.Length - 1; i++)
                d_path += d_path_elem[i] + '/';
            if (!IsPathExist(d_path))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Удаленный путь не найден");
                Console.ResetColor();
                return false;
            }
            Struct.MFT mft2;
            if (!IsFileExistInDir(s_path, s_path_elem[s_path_elem.Length - 1], out Struct.MFT _mft_rec))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("В данной папке файла не существует!");
                Console.ResetColor();
                return false;
            }
            if (IsFileExistInDir(d_path, d_path_elem[d_path_elem.Length - 1], out mft2.FileName))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Такой файл уже существует в папке!");
                Console.ResetColor();
                return false;
            }
            _mft_rec.Base_Record_Number = GetCurrentPathID(d_path);
            var mft = SearchSystemInformation("MFT");
            uint count_read = 0; byte[] b;
            while (count_read < mft.BusyByte)
            {
                var m = new Struct.MFT();
                b = new byte[Sizе];
                b = asfs.ReadByBytes(mft.AdressBlockFile, Convert.ToUInt32(Sizе), count_read);
                Converts.GetStrucFromBytes(b, out m);
                if (m.Record_Number == _mft_rec.Record_Number && m.FileType == '-' && m.FileName.SequenceEqual(_mft_rec.FileName))
                {
                    break;
                }
                count_read += Convert.ToUInt32(this.Sizе);
            }
            mft2.FileName.CopyTo(_mft_rec.FileName, 0);
            b = Converts.GetBytes(_mft_rec);
            asfs.WriteBytesToFile(b, mft.AdressBlockFile, count_read, mft.BusyByte, mft.Number_Allocated_Blocks, 0, out mft2.File_Attributes, out string error);
            return true;
        }
        public bool MakeDirectory(string NameForNewDir, string PathToDir, uint UserID)
        {
            char[] k;
            if (NameForNewDir.Length > 25) k = NameForNewDir.Substring(0, 25).ToCharArray();
            else if (NameForNewDir.Length == 25) k = NameForNewDir.ToCharArray();
            else
            {
                k = new char[25];
                string tmp = "";
                for (int i = 0; i < 25 - NameForNewDir.Length; i++) tmp += ' ';
                NameForNewDir.ToCharArray().CopyTo(k, 0);
                tmp.ToCharArray().CopyTo(k, NameForNewDir.Length);
            }
            var list_f = GetFilesDirectory(PathToDir, out uint last_id);
            foreach (var q in list_f)
            {
                if (q.FileType == '-' && q.FileName.SequenceEqual(k))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Такой каталог уже существует!");
                    Console.ResetColor();
                    return false;
                }
            }
            var _mft_rec = SystemInfoAboutFile("MFT");
            var Base_Record_Number = GetCurrentPathID(PathToDir);
            Struct.MFT mf = new Struct.MFT
            {
                UserID = UserID,
                Time = DateTime.Now.ToBinary(),
                Access_Level = "rwxr-----".ToCharArray(),//
                Base_Record_Number = Base_Record_Number,
                Record_Number = last_id + 1,
                FileName = k,
                FileType = 'd',
                Number_Allocated_Blocks = 0,
                BusyByte = 0,
                File_Attributes = 000,
                AdressBlockFile = 0
            };
            byte[] b = Converts.GetBytes(mf);
            if (!asfs.WriteBytesToFile(b, _mft_rec.AdressBlockFile, _mft_rec.BusyByte, _mft_rec.BusyByte, _mft_rec.Number_Allocated_Blocks, 0, out uint add_block, out string error))
                return false;
            if (add_block != 0)
                _mft_rec.Number_Allocated_Blocks += add_block;
            _mft_rec.BusyByte += Convert.ToUInt64(b.LongLength);
            byte[] b1 = Converts.GetBytes(_mft_rec);
            asfs.WriteBytesToFile(b1, _mft_rec.AdressBlockFile, 0, Convert.ToUInt64(b.Length), 1, 0, out add_block, out error);
            asfs.WriteBytesToFile(b1, copy_mft_block, 0, Convert.ToUInt64(b.Length), 1, 0, out add_block, out error);
            ReadSysRec(Convert.ToUInt32(asfs.boot_SectorSize), this.mft_block, this.copy_mft_block, asfs.SectorSize, 7, asfs);
            return true;
        }
        public bool RemoveDirectory(string DirName, string PathToDir, uint UserID, Users USER)
        {
            char[] k;
            if (DirName.Length > 25) k = DirName.Substring(0, 25).ToCharArray();
            else if (DirName.Length == 25) k = DirName.ToCharArray();
            else
            {
                k = new char[25];
                string tmp = "";
                for (int i = 0; i < 25 - DirName.Length; i++) tmp += ' ';
                DirName.ToCharArray().CopyTo(k, 0);
                tmp.ToCharArray().CopyTo(k, DirName.Length);
            }
            char[] b2 = new char[25];
            string tmp1 = "";
            for (int i = 0; i < 25 - usr.user.Login.Length; i++) tmp1 += ' ';
            usr.user.Login.CopyTo(b2, 0);
            tmp1.ToCharArray().CopyTo(b2, usr.user.Login.Length);
            char[] ks = new char[22];
            PathToDir.CopyTo(0, ks, 0, PathToDir.Length);
            DirName.CopyTo(0, ks, PathToDir.Length, DirName.Length);
            for (int i = PathToDir.Length + DirName.Length; i < ks.Length; i++)
                ks[i] = ' ';
            var users = USER.GetUsers();
            foreach (var a in users)
            {
                if (a.Home_Directory.SequenceEqual(PathToDir + DirName))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Вы не можете удалить директорию, которая принадлежит пользователю!");
                    Console.ResetColor();
                    return false;
                }
            }
            string error = "";
            var list_f = GetFilesDirectory(PathToDir, out uint last_id);
            bool Find = false;
            Struct.MFT d = new Struct.MFT();
            foreach (var q in list_f)
            {
                if (q.FileType == 'd' & q.FileName.SequenceEqual(k) /*& q.UserID != 1*/ & !(q.FileName.SequenceEqual(b2) & PathToDir.SequenceEqual("/USERS/")))
                {
                    d = q;
                    if (!AccessFile(q, usr.user.Id_User, Operations.ReadWrite))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Вы не можете удалить данный каталог!");
                        Console.ResetColor();
                        return false;
                    }
                    Find = true;
                    break;
                }
            }
            if (!Find)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Такого каталога не существует!");
                Console.ResetColor();
                return false;
            }
            var INFO = GetFilesDirectory(PathToDir + DirName, out uint last);
            foreach (var item in INFO)
            {
                if (!AccessFile(item, usr.user.Id_User, Operations.ReadWrite))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("У вас нет доступа к каталогу или файлу!");
                    Console.ResetColor();
                    return false;
                }
                if (item.FileType == '-')
                    {
                        if (!DeleteFile(new string(item.FileName).Trim(' '), PathToDir + DirName + "/", usr.user.Id_User))
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Ошибка при удалении!");
                            Console.ResetColor();
                            return false;
                        }
                    }
                if (item.FileType == 'd')
                {
                    if (!RemoveDirectory(new string(item.FileName).Trim(' '), PathToDir + DirName + "/", usr.user.Id_User, usr))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Ошибка при удалении!");
                        Console.ResetColor();
                        return false;
                    }
                }
            }
            var mft = SearchSystemInformation("MFT");
            uint count_read = 0; byte[] b;
            while (count_read < mft.BusyByte)
            {
                var m = new Struct.MFT();
                b = new byte[Sizе];
                b = asfs.ReadByBytes(mft.AdressBlockFile, Convert.ToUInt32(Sizе), count_read);
                Converts.GetStrucFromBytes(b, out m);
                if (m.Base_Record_Number == d.Base_Record_Number && m.FileType == '-' && m.FileName.SequenceEqual(d.FileName))
                {
                    break;
                }
                count_read += Convert.ToUInt32(Sizе);
            }
            uint block;
            do
            {
                count_read += Convert.ToUInt32(this.Sizе);
                b = new byte[Sizе];
                b = asfs.ReadByBytes(mft.AdressBlockFile, Convert.ToUInt32(Sizе), count_read);
                asfs.WriteBytesToFile(b, 0, count_read - Convert.ToUInt32(this.Sizе), mft.BusyByte - Convert.ToUInt64(b.LongLength), mft.Number_Allocated_Blocks, 0, out block, out error);
            } while (count_read < mft.BusyByte);
            mft.BusyByte -= Convert.ToUInt64(Sizе);
            byte[] b1 = Converts.GetBytes(mft);
            asfs.WriteBytesToFile(b1, mft.AdressBlockFile, 0, Convert.ToUInt64(b.Length), 1, 0, out block, out error);
            asfs.WriteBytesToFile(b1, copy_mft_block, 0, Convert.ToUInt64(b.Length), 1, 0, out block, out error);
            ReadSysRec(Convert.ToUInt32(asfs.boot_SectorSize), mft_block, copy_mft_block, asfs.SectorSize, 7, asfs);
            return true;
        }
        public bool CreateFile(string PathToFile, byte[] Content, uint Atributes, string Access_Level, uint UserID)
        {
            string[] elem = PathToFile.Split('/');
            if (elem[elem.Length - 1] == "")
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\nИмя файла не указано!");
                Console.ResetColor();
                return false;
            }
            if (Content == null | Content.Length == 0) Content = new byte[0];
            if (IsFileExistInDir(PathToFile.Remove(PathToFile.Length - elem[elem.Length - 1].Length), elem[elem.Length - 1], out char[] k))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\nФайл с таким именем уже существует!");
                Console.ResetColor();
                return false;
            }
            if (asfs.WriteBytesToFile(Content, 0, 0, 0, 0, copy_mft_block * 2 / 100 * 12, out uint blocks, out string Error))
            {
                Struct.MFT u = new Struct.MFT
                {
                    AdressBlockFile = Convert.ToUInt32(Error),
                    Time = DateTime.Now.ToBinary(),
                    File_Attributes = Atributes,
                    UserID = UserID,
                    Access_Level = Access_Level.ToCharArray(),
                    Base_Record_Number = GetCurrentPathID(PathToFile.Substring(0, PathToFile.Length - elem[elem.Length - 1].Length)),
                    FileType = '-',
                    BusyByte = Convert.ToUInt64(Content.LongLength),
                    Number_Allocated_Blocks = Convert.ToUInt32(Math.Ceiling(Convert.ToDouble(Content.Length) / Convert.ToDouble(asfs.SectorSize))),
                    Record_Number = Convert.ToUInt32(_mft.Count) + 1
                };
                if (elem[elem.Length - 1].Length > 25) u.FileName = elem[elem.Length - 1].Substring(0, 25).ToCharArray();
                else if (elem[elem.Length - 1].Length == 25) u.FileName = elem[elem.Length - 1].ToCharArray();
                else
                {
                    u.FileName = new char[25];
                    string tmp = "";
                    for (int i = 0; i < 25 - elem[elem.Length - 1].Length; i++) tmp += ' ';
                    elem[elem.Length - 1].ToCharArray().CopyTo(u.FileName, 0);
                    tmp.ToCharArray().CopyTo(u.FileName, elem[elem.Length - 1].Length);
                }
                byte[] b = Converts.GetBytes(u);
                var _mft_rec = SystemInfoAboutFile("MFT");
                if (!asfs.WriteBytesToFile(b, _mft_rec.AdressBlockFile, _mft_rec.BusyByte, _mft_rec.BusyByte, _mft_rec.Number_Allocated_Blocks, 0, out uint add_block, out Error))      /*Указание информации о файле*/
                    return false;
                if (add_block != 0)
                    _mft_rec.Number_Allocated_Blocks += add_block;
                _mft_rec.BusyByte += Convert.ToUInt64(b.LongLength);
                byte[] b1 = Converts.GetBytes(_mft_rec);
                asfs.WriteBytesToFile(b1, _mft_rec.AdressBlockFile, 0, Convert.ToUInt64(b.Length), 1, 0, out add_block, out Error);     /*Обновление файла MFT*/
                asfs.WriteBytesToFile(b1, copy_mft_block, 0, Convert.ToUInt64(b.Length), 1, 0, out add_block, out Error);
                ReadSysRec(Convert.ToUInt32(asfs.boot_SectorSize), mft_block, copy_mft_block, asfs.SectorSize, 7, asfs);
                return true;
            }
            else
            {
                Console.WriteLine(Error);
            }
            return false;
        }
        public bool DeleteFile(string NameFile, string PathToFile, uint UserID)
        {
            char[] k;
            if (NameFile.Length > 25) k = NameFile.Substring(0, 25).ToCharArray();
            else if (NameFile.Length == 25) k = NameFile.ToCharArray();
            else
            {
                k = new char[25];
                string tmp = "";

                for (int i = 0; i < 25 - NameFile.Length; i++) tmp += ' ';
                NameFile.ToCharArray().CopyTo(k, 0);
                tmp.ToCharArray().CopyTo(k, NameFile.Length);
            }
            string error = "";
            var list_f = GetFilesDirectory(PathToFile, out uint last_id);
            bool Find = false;
            Struct.MFT d = new Struct.MFT();
            foreach (var q in list_f)
            {
                if (q.FileType == '-' && q.FileName.SequenceEqual(k))
                {
                    d = q;
                    Find = true;
                    break;
                }
            }
            if (!Find)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Файла не существует!");
                Console.ResetColor();
                return false;
            }
            if (usr.user.Id_User != 1)
            {
                if (!AccessFile(d, usr.user.Id_User, Operations.ReadWrite))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("У вас нет доступа к файлу!");
                    Console.ResetColor();
                    return false;
                }
            }
            var mft = SearchSystemInformation("MFT");
            uint count_read = 0; byte[] b; Struct.MFT m = new Struct.MFT();
            while (count_read < mft.BusyByte)
            {
                m = new Struct.MFT();
                b = new byte[Sizе];
                b = asfs.ReadByBytes(mft.AdressBlockFile, Convert.ToUInt32(Sizе), count_read);
                Converts.GetStrucFromBytes(b, out m);
                if (m.Base_Record_Number == d.Base_Record_Number && m.FileType == '-' && m.FileName.SequenceEqual(d.FileName))
                {
                    break;
                }
                count_read += Convert.ToUInt32(Sizе);
            }
            if (m.AdressBlockFile == 0) return false;
            asfs.DeleteFile(m.AdressBlockFile);
            uint block;
            do
            {
                count_read += Convert.ToUInt32(Sizе);
                b = new byte[Sizе];
                b = asfs.ReadByBytes(mft.AdressBlockFile, Convert.ToUInt32(Sizе), count_read);
                asfs.WriteBytesToFile(b, 0, count_read - Convert.ToUInt32(Sizе), mft.BusyByte - Convert.ToUInt64(b.LongLength), mft.Number_Allocated_Blocks, 0, out block, out error);
            } while (count_read < mft.BusyByte);
            mft.BusyByte -= Convert.ToUInt64(b.LongLength);
            byte[] b1 = Converts.GetBytes(mft);
            asfs.WriteBytesToFile(b1, mft.AdressBlockFile, 0, Convert.ToUInt64(b.Length), 1, 0, out block, out error);
            asfs.WriteBytesToFile(b1, copy_mft_block, 0, Convert.ToUInt64(b.Length), 1, 0, out block, out error);
            ReadSysRec(Convert.ToUInt32(asfs.boot_SectorSize), this.mft_block, this.copy_mft_block, asfs.SectorSize, 7, asfs);
            return true;
        }
        public bool MoveDirectory(string FromWhere, string Where, uint UserID, bool change_id)
        {
            if (!IsPathExist(FromWhere))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Каталога не существует по заданному пути!");
                Console.ResetColor();
                return false;
            }
            if (!IsPathExist(Where))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Заданного нового пути не существует!");
                Console.ResetColor();
                return false;
            }
            char[] k;
            if ("USERS".Length > 25) k = "USERS".Substring(0, 25).ToCharArray();
            else if ("USERS".Length == 25) k = "USERS".ToCharArray();
            else
            {
                k = new char[25];
                string tmp = "";
                for (int i = 0; i < 25 - "USERS".Length; i++) tmp += ' ';
                "USERS".ToCharArray().CopyTo(k, 0);
                tmp.ToCharArray().CopyTo(k, "USERS".Length);
            }
            uint id = GetCurrentPathDirectory(FromWhere, out Struct.MFT mf_s);
            if (mf_s.FileName.SequenceEqual(k))
            {
                Console.WriteLine("Это системная папка. Ее нельзя создавать или удалять");
                return false;
            }
            uint id_d = GetCurrentPathDirectory(Where, out Struct.MFT mf_d);
            if (mf_d.FileName.SequenceEqual(k))
            {
                Console.WriteLine("Это системная папка.В нее нельзя копировать");
                return false;
            }
            if (usr.user.Id_User != 1)
            {
                if (!AccessFile(mf_s, usr.user.Id_User, Operations.Write))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("У вас нет доступа к файлу!");
                    Console.ResetColor();
                    return false;
                }
            }
            var mft = SearchSystemInformation("MFT");
            uint count_read = 0; byte[] b;
            while (count_read < mft.BusyByte)
            {
                var m = new Struct.MFT();
                b = new byte[Sizе];
                b = asfs.ReadByBytes(mft.AdressBlockFile, Convert.ToUInt32(Sizе), count_read);
                Converts.GetStrucFromBytes(b, out m);
                if (m.Record_Number == mf_s.Record_Number)
                {
                    m.Base_Record_Number = mf_d.Record_Number;
                    if (change_id)
                        m.UserID = UserID;
                    b = new byte[Sizе];
                    b = Converts.GetBytes(m);
                    asfs.WriteBytesToFile(b, mft.AdressBlockFile, count_read, mft.BusyByte, mft.Number_Allocated_Blocks, 0, out uint block_add, out string error);
                    return true;
                }
                count_read += Convert.ToUInt32(this.Sizе);
            }
            return false;
        }
        public bool ViewContentFile(string FileName)
        {
            string[] s_path_elem = FileName.Split('/');
            string s_path = "";
            for (int i = 0; i < s_path_elem.Length - 1; i++)
                s_path += s_path_elem[i] + '/';
            if (!IsPathExist(s_path))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Файл не найден!");
                Console.ResetColor();
                return false;
            }
            if (!IsFileExistInDir(s_path, s_path_elem[s_path_elem.Length - 1], out Struct.MFT _mft_rec))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("В директории файл не найден!");
                Console.ResetColor();
                return false;
            }
            if (usr.user.Id_User != 1)
            {
                if (!AccessFile(_mft_rec, usr.user.Id_User, Operations.Read))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("У вас нет доступа к файлу!");
                    Console.ResetColor();
                    return false;
                }
            }
            byte[] b = asfs.ReadByBytes(_mft_rec.AdressBlockFile, Convert.ToUInt32(_mft_rec.BusyByte), 0);
            char[] str = Encoding.Unicode.GetChars(b);
            Console.WriteLine(new string(str));
            return true;
        }
        public bool EditContentFile(string FileName)
        {
            string error = "";
            string[] s_path_elem = FileName.Split('/');
            string s_path = "";
            for (int i = 0; i < s_path_elem.Length - 1; i++)
                s_path += s_path_elem[i] + '/';
            if (!IsPathExist(s_path))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Файл не найден!");
                Console.ResetColor();
                return false;
            }
            if (!IsFileExistInDir(s_path, s_path_elem[s_path_elem.Length - 1], out Struct.MFT _mft_rec))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("В директории файл не найден!");
                Console.ResetColor();
                return false;
            }
            if (usr.user.Id_User != 1)
            {
                if (!AccessFile(_mft_rec, usr.user.Id_User, Operations.ReadWrite))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("У вас нет доступа к файлу!");
                    Console.ResetColor();
                    return false;
                }
            }
            byte[] b = asfs.ReadByBytes(_mft_rec.AdressBlockFile, Convert.ToUInt32(_mft_rec.BusyByte), 0);
            char[] ContentFile = Encoding.Unicode.GetChars(b);
            Console.Write(new string(ContentFile));
            List<char> EditContent = new List<char>();
            for (int i = 0; i < ContentFile.Length; i++)                    /*Копируем контент файла*/
                EditContent.Add(ContentFile[i]);
            ConsoleKeyInfo ReadInput;
            do
            {
                ReadInput = Console.ReadKey();
                if ((ConsoleModifiers.Control & ReadInput.Modifiers) != 0)
                    if (ReadInput.Key == ConsoleKey.D)
                        break;
                if (ReadInput.Key == ConsoleKey.Backspace)
                {
                    if (EditContent.Count != 0)
                        EditContent.Remove(EditContent[EditContent.Count - 1]);
                }
                else
                    EditContent.Add(ReadInput.KeyChar);
            }
            while (true);
            string[] el = FileName.Split('/');
            string curr_path = "/";
            for (int i = 0; i < el.Length - 1; i++)
            {
                if (el[i] == "")
                    continue;
                curr_path += el[i] + "/";
            }
            string name_File = el[el.Length - 1];
            char[] k;
            if (name_File.Length > 25) k = name_File.Substring(0, 25).ToCharArray();
            else if (name_File.Length == 25) k = name_File.ToCharArray();
            else
            {
                k = new char[25];
                string tmp = "";
                for (int i = 0; i < 25 - name_File.Length; i++) tmp += ' ';
                name_File.ToCharArray().CopyTo(k, 0);
                tmp.ToCharArray().CopyTo(k, name_File.Length);
            }
            error = "";
            var list_f = GetFilesDirectory(curr_path, out uint last_id);
            bool Find = false;
            Struct.MFT d = new Struct.MFT();
            foreach (var q in list_f)
            {
                if (q.FileType == '-' && q.FileName.SequenceEqual(k))
                {
                    d = q;
                    Find = true;
                    break;
                }
            }
            if (!Find)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Файла не существует!");
                Console.ResetColor();
                return false;
            }
            var mft = SearchSystemInformation("MFT");
            uint count_read = 0; Struct.MFT m = new Struct.MFT();
            while (count_read < mft.BusyByte)
            {
                m = new Struct.MFT();
                b = new byte[Sizе];
                b = asfs.ReadByBytes(mft.AdressBlockFile, Convert.ToUInt32(Sizе), count_read);
                Converts.GetStrucFromBytes(b, out m);
                if (m.Base_Record_Number == d.Base_Record_Number && m.FileType == '-' && m.FileName.SequenceEqual(d.FileName))
                {
                    break;
                }
                count_read += Convert.ToUInt32(Sizе);
            }
            if (m.AdressBlockFile == 0) return false;
            asfs.DeleteFile(m.AdressBlockFile);
            byte[] b2 = Encoding.Unicode.GetBytes(EditContent.ToArray());
            m.BusyByte = Convert.ToUInt32(b2.Length);
            m.Number_Allocated_Blocks = Convert.ToUInt32(Math.Ceiling(Convert.ToDouble(b2.Length) / asfs.SectorSize));
            asfs.WriteBytesToFile(b2, 0, 0, 0, 0, copy_mft_block * 2 / 100 * 12, out uint blocks, out error);
            m.AdressBlockFile = Convert.ToUInt32(error);
            b = Converts.GetBytes(m);
            asfs.WriteBytesToFile(b, mft.AdressBlockFile, count_read, mft.BusyByte, mft.Number_Allocated_Blocks, 0, out blocks, out error);
            Console.WriteLine();
            return true;
        }
        public bool ChangeMode(string Path, string AccessLevel, uint UserId)
        {
            string[] PathToFile = Path.Split('/');
            string NameFile = PathToFile[PathToFile.Length - 1];
            char[] Value;
            if (NameFile.Length > 25)
                Value = NameFile.Substring(0, 25).ToCharArray();
            else if (NameFile.Length == 25)
                Value = NameFile.ToCharArray();
            else
            {
                Value = new char[25];
                string str = "";
                for (int i = 0; i < 25 - NameFile.Length; i++)
                    str += ' ';
                NameFile.ToCharArray().CopyTo(Value, 0);
                str.ToCharArray().CopyTo(Value, NameFile.Length);
            }
            string path = "";
            for(int i=0; i < PathToFile.Length; i++)
            {
                if (i == PathToFile.Length - 1)
                    break;
                path += PathToFile[i] + "/";
            }
            byte[] ReadArray;
            var _INFO = GetFilesDirectory(path, out uint Last);
            foreach (var Inf in _INFO)
            {
                if (Inf.FileType == '-' & Inf.FileName.SequenceEqual(Value) & Inf.UserID == UserId)
                {
                    var SysMFT = SearchSystemInformation("MFT");
                    UInt32 ReadBytes = 0;
                    Struct.MFT editMFT = new Struct.MFT();
                    while (ReadBytes < SysMFT.BusyByte)
                    {
                        editMFT = new Struct.MFT();
                        ReadArray = new byte[Sizе];
                        ReadArray = asfs.ReadByBytes(SysMFT.AdressBlockFile, Convert.ToUInt32(Sizе), ReadBytes);
                        Converts.GetStrucFromBytes(ReadArray, out editMFT);
                        if (editMFT.FileName.SequenceEqual(Value) & editMFT.FileType == '-' & editMFT.UserID == UserId)
                            break;
                        ReadBytes += Convert.ToUInt32(Sizе);
                    }
                    editMFT.Access_Level = AccessLevel.ToCharArray();
                    ReadArray = Converts.GetBytes(editMFT);
                    asfs.WriteBytesToFile(ReadArray, SysMFT.AdressBlockFile, ReadBytes, SysMFT.BusyByte, SysMFT.Number_Allocated_Blocks, 0, out UInt32 Block, out string ErrorMessage);
                }
            }
            return true;
        }
        public bool ChangeModeDir(string Path, string AccessLevel, uint UserId)
        {
            string[] PathToFile = Path.Split('/');
            string NameFile = PathToFile[PathToFile.Length - 1];
            char[] Value;
            if (NameFile.Length > 25)
                Value = NameFile.Substring(0, 25).ToCharArray();
            else if (NameFile.Length == 25)
                Value = NameFile.ToCharArray();
            else
            {
                Value = new char[25];
                string str = "";
                for (int i = 0; i < 25 - NameFile.Length; i++)
                    str += ' ';
                NameFile.ToCharArray().CopyTo(Value, 0);
                str.ToCharArray().CopyTo(Value, NameFile.Length);
            }
            string path = "";
            for (int i = 0; i < PathToFile.Length; i++)
            {
                if (i == PathToFile.Length - 1)
                    break;
                path += PathToFile[i] + "/";
            }
            byte[] ReadArray;
            var _INFO = GetFilesDirectory(path, out uint Last);
            foreach (var Inf in _INFO)
            {
                if (Inf.FileType == 'd' & Inf.FileName.SequenceEqual(Value) & Inf.UserID == UserId)
                {
                    if (!AccessFile(Inf, usr.user.Id_User, Operations.ReadWrite))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Вы не можете изменить приоритет данному каталогу!");
                        Console.ResetColor();
                        return false;
                    }
                    var SysMFT = SearchSystemInformation("MFT");
                    UInt32 ReadBytes = 0;
                    Struct.MFT editMFT = new Struct.MFT();
                    while (ReadBytes < SysMFT.BusyByte)
                    {
                        editMFT = new Struct.MFT();
                        ReadArray = new byte[Sizе];
                        ReadArray = asfs.ReadByBytes(SysMFT.AdressBlockFile, Convert.ToUInt32(Sizе), ReadBytes);
                        Converts.GetStrucFromBytes(ReadArray, out editMFT);
                        if (editMFT.FileName.SequenceEqual(Value) & editMFT.FileType == 'd' & editMFT.UserID == UserId)
                            break;
                        ReadBytes += Convert.ToUInt32(Sizе);
                    }
                    editMFT.Access_Level = AccessLevel.ToCharArray();
                    ReadArray = Converts.GetBytes(editMFT);
                    asfs.WriteBytesToFile(ReadArray, SysMFT.AdressBlockFile, ReadBytes, SysMFT.BusyByte, SysMFT.Number_Allocated_Blocks, 0, out UInt32 Block, out string ErrorMessage);
                }
            }
            return true;
        }
        #endregion
    }
}