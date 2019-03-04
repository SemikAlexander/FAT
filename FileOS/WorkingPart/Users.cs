using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace FileOS.WorkingPart
{
    class Users
    {
        List<Struct.Users> usr = new List<Struct.Users>();
        ASFS f;
        private Struct.MFT _info;
        public Struct.Users user;
        int Size_struct = 0;
        public Users()
        {
            Size_struct = Marshal.SizeOf(typeof(FileOS.Struct.Users));
        }
        public uint GetBlockSizeStruct(uint block_sector)
        {
            return Convert.ToUInt32(Math.Ceiling(Convert.ToDouble(Size_struct * usr.Count) / block_sector));
        }
        public bool AddUser(string user, string login, string password, uint group_id)
        {
            Struct.Users u = new Struct.Users
            {
                Id_Group = group_id,
                Hesh_Password = Functions.Heshing.Hash(password, 20)
            };
            if (user.Length > 50) u.User_Name = user.Substring(0, 50).ToCharArray();
            else if (user.Length == 50) u.User_Name = user.ToCharArray();
            else
            {
                u.User_Name = new char[50];
                string tmp = "";
                for (int i = 0; i < 50 - user.Length; i++) tmp += ' ';
                user.ToCharArray().CopyTo(u.User_Name, 0);
                tmp.ToCharArray().CopyTo(u.User_Name, user.Length);
            }
            if (login.Length > 15) u.Login = login.Substring(0, 15).ToCharArray();
            else if (login.Length == 15) u.Login = login.ToCharArray();
            else
            {
                u.Login = new char[15];
                string tmp = "";
                for (int i = 0; i < 15 - login.Length; i++) tmp += ' ';
                login.ToCharArray().CopyTo(u.Login, 0);
                tmp.ToCharArray().CopyTo(u.Login, login.Length);
            }
            u.Home_Directory = new char[22];
            string t = "/USERS/";
            for (int i = 0; i < t.Length; i++)
                u.Home_Directory[i] = t[i];
            for (int i = t.Length; i < t.Length + u.Login.Length; i++)
                u.Home_Directory[i] = u.Login[i - t.Length];
            Struct.Users k = usr.Find((x) => x.Login == u.Login);
            if (k.Login != null)
                return false;
            u.Id_User = Convert.ToUInt32(usr.Count) + 1;
            usr.Add(u);
            return true;
        }
        public int StructByteSize()
        {
            return Size_struct * usr.Count;
        }
        public byte[] ConvertBytes()
        {
            byte[] bt = new byte[Size_struct * usr.Count];
            byte[] b; int k = 0;
            for (int i = 0; i < usr.Count; i++)
            {
                b = FileOS.Functions.Converts.GetBytes<FileOS.Struct.Users>(usr[i]);
                b.CopyTo(bt, k);
                k += b.Length;
            }
            return bt;
        }
        public void InitializeUser(ASFS _f, Struct.MFT mf)
        {
            f = _f;
            _info = mf;
        }
        public bool LoginInSystem(string login, string password)
        {
            uint count_read = 0; byte[] b;
            char[] hash_password = Functions.Heshing.Hash(password, 20);
            char[] log;

            if (login.Length > 15) log = login.Substring(0, 15).ToCharArray();
            else if (login.Length == 15) log = login.ToCharArray();
            else
            {
                log = new char[15];
                string tmp = "";

                for (int i = 0; i < 15 - login.Length; i++) tmp += ' ';


                login.ToCharArray().CopyTo(log, 0);
                tmp.ToCharArray().CopyTo(log, login.Length);

            }

            //чтение файла и поиск пользователя
            while (count_read < _info.BusyByte)
            {
                user = new Struct.Users();
                b = new byte[Size_struct];
                b = f.ReadByBytes(_info.AdressBlockFile, Convert.ToUInt32(Size_struct), count_read);
                Functions.Converts.GetStrucFromBytes(b, out user);
                if (user.Hesh_Password.SequenceEqual(hash_password) && user.Login.SequenceEqual(log))
                    return true;
                count_read += Convert.ToUInt32(this.Size_struct);
            }
            return false;
        }
        public bool AreUserExist(string login, out uint last_id)
        {
            uint count_read = 0; byte[] b;
            char[] log;

            if (login.Length > 15) log = login.Substring(0, 15).ToCharArray();
            else if (login.Length == 15) log = login.ToCharArray();
            else
            {
                log = new char[15];
                string tmp = "";

                for (int i = 0; i < 15 - login.Length; i++) tmp += ' ';


                login.ToCharArray().CopyTo(log, 0);
                tmp.ToCharArray().CopyTo(log, login.Length);

            }
            last_id = 0;
            //чтение файла и поиск пользователя
            while (count_read < _info.BusyByte)
            {
                user = new Struct.Users();
                b = new byte[Size_struct];
                b = f.ReadByBytes(_info.AdressBlockFile, Convert.ToUInt32(Size_struct), count_read);
                Functions.Converts.GetStrucFromBytes(b, out user);
                last_id = (last_id > user.Id_User) ? last_id : user.Id_User;
                if (user.Login.SequenceEqual(log))
                    return true;
                count_read += Convert.ToUInt32(this.Size_struct);
            }
            return false;
        }
        public bool AreUserExist(string login,string password, out uint last_id)
        {
            uint count_read = 0; byte[] b;
            char[] log;
            char[] hash_password = Functions.Heshing.Hash(password, 20);

            if (login.Length > 15) log = login.Substring(0, 15).ToCharArray();
            else if (login.Length == 15) log = login.ToCharArray();
            else
            {
                log = new char[15];
                string tmp = "";
                for (int i = 0; i < 15 - login.Length; i++) tmp += ' ';
                login.ToCharArray().CopyTo(log, 0);
                tmp.ToCharArray().CopyTo(log, login.Length);
            }
            last_id = 0;
            while (count_read < _info.BusyByte)
            {
               var user1 = new Struct.Users();
                b = new byte[Size_struct];
                b = f.ReadByBytes(_info.AdressBlockFile, Convert.ToUInt32(Size_struct), count_read);
                Functions.Converts.GetStrucFromBytes(b, out user1);
                last_id = (last_id > user.Id_User) ? last_id : user.Id_User;
                if (user1.Login.SequenceEqual(log) && user1.Hesh_Password.SequenceEqual(hash_password))
                    return true;
                count_read += Convert.ToUInt32(this.Size_struct);
            }
            return false;
        }
        public List<Struct.Users> GetUsers()
        {
            List<Struct.Users> ls = new List<Struct.Users>();
            uint count_read = 0; byte[] b;
            
            //чтение файла и поиск пользователя
            while (count_read < _info.BusyByte)
            {
               var users = new Struct.Users();
                b = new byte[Size_struct];
                b = f.ReadByBytes(_info.AdressBlockFile, Convert.ToUInt32(Size_struct), count_read);
                Functions.Converts.GetStrucFromBytes(b, out users);
                ls.Add(users);
                count_read += Convert.ToUInt32(this.Size_struct);
            }
            return ls;
        }
        public bool CreateNewUser(Groups groups, MFT mf, uint copy_mft)
        {
            Console.Write("Введите имя пользователя:");
            string Name = Console.ReadLine();
            Console.Write("Введите логин:");
            string login = Console.ReadLine();
            uint id_usr = 0;
            while (AreUserExist(login, out id_usr))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Ошибка. Данный логин уже используется");
                Console.ResetColor();
                Console.Write("Введите логин:");
                login = Console.ReadLine();
            }
            Console.Write("Введите пароль:");
            string password = Console.ReadLine();
            Struct.Users u = new Struct.Users
            {
                Id_Group = 2,
                Hesh_Password = Functions.Heshing.Hash(password, 20)
            };
            if (Name.Length > 50) u.User_Name = Name.Substring(0, 50).ToCharArray();
            else if (Name.Length == 50) u.User_Name = Name.ToCharArray();
            else
            {
                u.User_Name = new char[50];
                string tmp = "";
                for (int i = 0; i < 50 - Name.Length; i++) tmp += ' ';
                Name.ToCharArray().CopyTo(u.User_Name, 0);
                tmp.ToCharArray().CopyTo(u.User_Name, Name.Length);
            }
            if (login.Length > 15) u.Login = login.Substring(0, 15).ToCharArray();
            else if (login.Length == 15) u.Login = login.ToCharArray();
            else
            {
                u.Login = new char[15];
                string tmp = "";
                for (int i = 0; i < 15 - login.Length; i++) tmp += ' ';
                login.ToCharArray().CopyTo(u.Login, 0);
                tmp.ToCharArray().CopyTo(u.Login, login.Length);
            }
            u.Home_Directory = new char[22];
            string t = "/USERS/";
            for (int i = 0; i < t.Length; i++)
                u.Home_Directory[i] = t[i];
            for (int i = t.Length; i < t.Length + u.Login.Length; i++)
                u.Home_Directory[i] = u.Login[i - t.Length];
            u.Id_User = id_usr + 1;
            byte[] b = Functions.Converts.GetBytes(u);
            if (!f.WriteBytesToFile(b, _info.AdressBlockFile, _info.BusyByte, Convert.ToUInt64(b.Length), _info.Number_Allocated_Blocks, 0, out uint block_add, out string error))
                return false;
            _info.Number_Allocated_Blocks += block_add;
            _info.BusyByte += Convert.ToUInt64(b.Length);
            byte[] content = Functions.Converts.GetBytes(_info);
            var mft = mf.SystemInfoAboutFile("MFT");
            f.WriteBytesToFile(content, mft.AdressBlockFile,  Convert.ToUInt32( mf.StructRecSize(Convert.ToInt32(_info.Record_Number - 1))), mft.BusyByte, 1, 0, out block_add, out error);
            f.WriteBytesToFile(content, copy_mft, Convert.ToUInt32(mf.StructRecSize(Convert.ToInt32(_info.Record_Number - 1))), mft.BusyByte, 1, 0, out block_add, out error);
            mf.GetFilesDirectory("/USERS/", out uint last_id);
            mf.MakeDirectory(login, "/USERS/", u.Id_User);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Пользователь создан.");
            Console.ResetColor();
            return true;
        }
        public bool DeleteUser(string login, string password, uint copy_mft, MFT mf)
        {
            if (!AreUserExist(login, password, out uint id))
            {
                Console.WriteLine("Такого пользователя не существует");
                return false;
            }
            uint count_read = 0;
            byte[] b;
            string error;
            bool Find = false;
            Struct.Users u = new Struct.Users();
            char[] LoginUser;
            if (login.Length > 15) LoginUser = login.Substring(0, 15).ToCharArray();
            else if (login.Length == 15) LoginUser = login.ToCharArray();
            else
            {
                LoginUser = new char[15];
                string tmp = "";
                for (int i = 0; i < 15 - login.Length; i++) tmp += ' ';
                login.ToCharArray().CopyTo(LoginUser, 0);
                tmp.ToCharArray().CopyTo(LoginUser, login.Length);
            }
            do
            {
                b = new byte[Size_struct];
                b = f.ReadByBytes(_info.AdressBlockFile, Convert.ToUInt32(Size_struct), count_read);
                if (!Find)
                {
                    Functions.Converts.GetStrucFromBytes(b, out u);
                    if (u.Login.SequenceEqual(LoginUser))
                    {
                        Find = true;
                    }
                }
                else
                    f.WriteBytesToFile(b, 0, count_read - Convert.ToUInt32(Size_struct), _info.BusyByte - Convert.ToUInt64(b.LongLength), _info.Number_Allocated_Blocks, 0, out uint block, out error);
                count_read += Convert.ToUInt32(Size_struct);
            }
            while (count_read < _info.BusyByte);
            _info.BusyByte -= Convert.ToUInt32(Size_struct);
            _info.Number_Allocated_Blocks = Convert.ToUInt32(Math.Floor(Convert.ToDouble(_info.BusyByte) / f.SectorSize));
            byte[] content = Functions.Converts.GetBytes(_info);
            var mft = mf.SystemInfoAboutFile("MFT");
            f.WriteBytesToFile(content, mft.AdressBlockFile, Convert.ToUInt32((_info.Record_Number - 1) * mf.StructRecSize(1)), Convert.ToUInt32(content.Length), 1, 0, out uint block_add, out error);
            f.WriteBytesToFile(content, copy_mft, Convert.ToUInt32((_info.Record_Number - 1) * mf.StructRecSize(1)), Convert.ToUInt32(content.Length), 1, 0, out block_add, out error);
            mf.ReadSysRec(Convert.ToUInt32(f.boot_SectorSize), mft.AdressBlockFile, copy_mft, f.SectorSize, 7, f);
            return true;
        }
    }
}