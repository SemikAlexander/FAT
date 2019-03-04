using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FileOS
{
    class ASFS
    {
        public bool File_init = false;
        WorkingPart.ASFS FS;
        WorkingPart.Boot BOOT;
        WorkingPart.MFT MFT;
        WorkingPart.Groups GR;
        WorkingPart.Users US;
        public ASFS()
        {
            FS = new WorkingPart.ASFS();
            if (FS.Check_fs_Files())
            {
                File_init = true;
            }
            else
            {
                File_init = false;
            }
        }
        private void WriteBlock(byte[] data,FileStream FileStream,uint block,uint Size_struct_block,int Size_struct_boot_sector)
        {
            FileStream.Position = Size_struct_boot_sector + block * Size_struct_block;
            FileStream.Write(data, 0, data.Length);
        }
        public void Init(uint Size_structDisk,string admin_login,string admin_password,string NameUser)
        {
            BOOT = new WorkingPart.Boot();
            MFT = new WorkingPart.MFT();
            GR = new WorkingPart.Groups();
            US = new WorkingPart.Users();
            ulong _Size_struct_disk_bytes = Convert.ToUInt32(Size_structDisk * 1024 * 1024) - Convert.ToUInt32(BOOT.Size_struct * 2);
            BOOT.Init_Boot(FS.Run_podbor_Size_struct_block(_Size_struct_disk_bytes), 0, FS.Number_Allocated_Blocks / 2);
            GR.AddGroup("System", 0);
            GR.AddGroup("Administrator", 1);
            GR.AddGroup("User", 2);
            US.AddUser(NameUser, admin_login, admin_password,1);
            uint FirstMFT5Record = Convert.ToUInt32(MFT.StructRecSize(8, BOOT.Get_Size_struct_block()));
            MFT.AddRecordInMFT(FirstMFT5Record, "MFT", Convert.ToUInt64(MFT.StructRecSize(8)), "-", 0, "rwxrwxrwx", 0, 111, DateTime.Now, DateTime.Now, 0);
            FS.Add_record(0, FirstMFT5Record);
            FS.Add_record(FS.Number_Allocated_Blocks / 2, 1);
            uint fs_block = FS.Get_Size_struct_Block(BOOT.Get_Size_struct_block());
            MFT.AddRecordInMFT(fs_block, "ASFS", Convert.ToUInt64(FS.StructByteSize()), "-", 0, "rwxrwxrwx", 0, 111, DateTime.Now, DateTime.Now, FirstMFT5Record);
            FS.Add_record(FirstMFT5Record, fs_block);
            uint gr_block = GR.SizeStructInBlock(BOOT.Get_Size_struct_block());
            MFT.AddRecordInMFT(gr_block, "GROUP", Convert.ToUInt64(GR.StructByteSize()), "-", 0, "rwxrwxrwx", 0, 111, DateTime.Now, DateTime.Now, FirstMFT5Record + fs_block);
            FS.Add_record(FirstMFT5Record + fs_block, gr_block);
            uint usr_block = US.GetBlockSizeStruct(BOOT.Get_Size_struct_block());
            MFT.AddRecordInMFT(usr_block, "USERS", Convert.ToUInt64(US.StructByteSize()), "-", 0, "rwxrwxrwx", 0, 111, DateTime.Now, DateTime.Now, FirstMFT5Record + fs_block+ gr_block);
            FS.Add_record(FirstMFT5Record + fs_block+ gr_block, usr_block);
            MFT.AddRecordInMFT(0, ".", 0, "-", 0, "rwxrwxrwx", 0, 111, DateTime.Now, DateTime.Now, 0);
            MFT.AddRecordInMFT(0, "USERS", 0, "d", 5, "rwx---rwx", 0,000, DateTime.Now, DateTime.Now,0);
            MFT.AddRecordInMFT(0, "SYSTEM", 0, "d", 6, "rwx---rwx", 0, 000, DateTime.Now, DateTime.Now, 0);
            MFT.AddRecordInMFT(0, admin_login, 0, "d", 6, "rwx------", 1, 000, DateTime.Now, DateTime.Now, 0);
            using (FileStream fw = new FileStream(WorkingPart.ASFS.PathTofile, FileMode.Create))
            {
                fw.SetLength(Convert.ToInt64(Size_structDisk * 1024 * 1024));
                fw.Write(BOOT.ConvertBytes(), 0, BOOT.Size_struct);
                WriteBlock(MFT.ConvertBytes(), fw, 0, BOOT.Get_Size_struct_block(), BOOT.Size_struct);
                WriteBlock(MFT.ConvertBytes(), fw, FS.Number_Allocated_Blocks/2, BOOT.Get_Size_struct_block(), BOOT.Size_struct);
                WriteBlock(FS.ConvertBytes(), fw, FirstMFT5Record, BOOT.Get_Size_struct_block(), BOOT.Size_struct);
                WriteBlock(GR.ConvertBytes(), fw, FirstMFT5Record + fs_block, BOOT.Get_Size_struct_block(), BOOT.Size_struct);
                WriteBlock(US.ConvertBytes(), fw, FirstMFT5Record + fs_block + gr_block, BOOT.Get_Size_struct_block(), BOOT.Size_struct);
                fw.Position = Convert.ToInt64(Size_structDisk * 1024 * 1024) - BOOT.Size_struct;
                fw.Write(BOOT.ConvertBytes(), 0, BOOT.Size_struct);
            }
        }
        public bool StartSystem()
        {
            BOOT = new WorkingPart.Boot();
            MFT = new WorkingPart.MFT();
            US = new WorkingPart.Users();
            FS = new WorkingPart.ASFS();
            if(!BOOT.Read_BootSect())
            {
                Console.WriteLine("Файловая система некорректна. Система будет завершена"); Console.Read();
                return false;
            }
            FS.InitializeSystem(BOOT.Get_Size_struct_block(),BOOT.Size_struct);
            MFT.ReadSysRec(Convert.ToUInt32(BOOT.Size_struct), BOOT.Get_number_mft_block(), BOOT.Get_number_copy_mft_block(), BOOT.Get_Size_struct_block(), 7, FS);
            Struct.MFT fs_info, group_info, user_info;
            fs_info = MFT.SystemInfoAboutFile("ASFS");
            if (fs_info.FileName == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Файловая система некорректна. Система будет завершена");
                Console.ResetColor();
                Console.Read();
                return false;
            }
            FS.InitializeSystem_2(fs_info.AdressBlockFile,fs_info.BusyByte);//производим инициализацию файловой системы
            group_info = MFT.SystemInfoAboutFile("GROUP");
            user_info = MFT.SystemInfoAboutFile("USERS");
            if (group_info.FileName == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Cистемный файл групп пользователей отсутствует!");
                Console.ResetColor();
                Console.Read();
                return false;
            }
            if (user_info.FileName == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Cистемный файл пользователей отсутствует!"); Console.Read();
                Console.ResetColor();
                return false;
            }
            US.InitializeUser(FS, user_info);
            GR = new WorkingPart.Groups();
            GR.InitializeGroups(FS, group_info);
            if (!Authorization()) return false;
            Console.Clear();
            string s = new string(US.user.User_Name);
            Console.WriteLine("Добро пожаловать " + s);
            Console.Clear();
            ConsoleCommand(new string(US.user.Home_Directory).Trim(' ') + "/");
            return true;
        }
        public bool Authorization()
        {
            Console.Title = "Authorization";
            ConsoleKeyInfo key;
            while (true)
            {
                Console.Clear();
                Console.Write("Логин: ");
                Console.ForegroundColor = ConsoleColor.Green;
                string InputLogin = Console.ReadLine();
                Console.ResetColor();
                Console.Write("Пароль: ");
                Console.ForegroundColor = ConsoleColor.Green;
                string InputPassword = Console.ReadLine();
                Console.ResetColor();
                if (US.LoginInSystem(InputLogin, InputPassword))
                    return true;
                Console.Clear();
                Console.WriteLine("Такого пользователя не существует. \nНажмите любой символ для повторной попытки. \n(Чтобы выйти нажмите [ESC])");
                Console.ResetColor();
                if ((key = Console.ReadKey()).Key == ConsoleKey.Escape)
                    return false;
            }
        }
        void ConsoleCommand(string Directory)
        {
            Console.Title = new string(US.user.User_Name);
            while (true)
            {
                MFT.gr = GR;
                MFT.usr = US;
                Console.Title = new string(US.user.User_Name).Trim(' ');
                Console.Write("#" + Directory.Trim(' ') + ">>");
                Console.ForegroundColor = ConsoleColor.Green;
                string ConsoleCommand = Console.ReadLine();
                Console.ResetColor();
                string[] Command = ConsoleCommand.Split(' ');
                string error = "";
                switch (Command[0])
                {
                    case "cd":
                        if (Command[1][0] == '/' && Command[1][Command[1].Length - 1] == '/')
                        {
                            if (MFT.IsPathExist(Command[1]))
                            {
                                Directory = Command[1];
                            }
                            else
                                Console.WriteLine("Ошибка. Такого каталога не существует");
                        }
                        else if (Command[1][Command[1].Length - 1] != '/')
                        {
                            if (MFT.IsPathExist(Directory + Command[1] + "/"))
                            {
                                Directory = Directory + Command[1] + "/";
                            }
                            else
                                Console.WriteLine("Каталога не существует!");
                        }
                        else if (Command.Length == 2 && (Command[1][0] == '.' && Command[1][1] == '.' && Command[1][2] == '/'))
                        {
                            int i = 0;
                            for (i = Directory.Length - 1; i > 0; i--)
                                if (Directory[i - 1] == '/')
                                    break;
                            char[] s = new char[i];
                            Directory.CopyTo(0, s, 0, i);
                            Directory = new string(s);
                            if (!MFT.IsPathExist(Directory))
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("Каталога не существует!");
                                Console.ResetColor();
                            }
                        }
                        else
                        {
                            if (MFT.IsPathExist(Directory + Command[1]))
                            {
                                Directory = Directory + Command[1];
                            }
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("Каталога не существует");
                                Console.ResetColor();
                            }
                        }
                        break;
                    case "ls":
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write(MFT.ShowFiles(Directory, US.user.Id_User));
                        Console.ResetColor();
                        break;
                    case "mkdir":
                        if (Command.Length == 2)
                        {
                            error = "";
                            MFT.MakeDirectory(Command[1], Directory, US.user.Id_User);
                        }
                        else
                        if (Command.Length == 3)
                        {
                            error = "";
                            MFT.MakeDirectory(Command[2], Command[1], US.user.Id_User);
                        }
                        break;
                    case "rmdir":
                        if (Command.Length == 2)
                        {
                            error = "";
                            MFT.RemoveDirectory(Command[1], Directory, US.user.Id_User, US);
                        }
                        else
                        if (Command.Length == 3)
                        {
                            MFT.RemoveDirectory(Command[2], Command[1], US.user.Id_User, US);
                        }
                        break;
                    case "cat":
                        List<char> Info = new List<char>();
                        ConsoleKeyInfo ReadInput;
                        char[] NeedArrayToUnicode;
                        byte[] ArrayForCreateFile;
                        if (Command.Length == 3)
                        {
                            if (Command[1] == ">")
                            {
                                do
                                {
                                    ReadInput = Console.ReadKey();
                                    if ((ConsoleModifiers.Control & ReadInput.Modifiers) != 0)
                                        if (ReadInput.Key == ConsoleKey.D)
                                            break;                               /*Выход при нажатии Ctrl + D*/
                                    if (ReadInput.Key == ConsoleKey.Backspace)
                                    {
                                        if (Info.Count != 0)
                                        {
                                            Info.Remove(Info[Info.Count - 1]);
                                        }
                                    }
                                    else
                                    {
                                        Info.Add(ReadInput.KeyChar);
                                    }
                                } while (true);
                                NeedArrayToUnicode = Info.ToArray();
                                ArrayForCreateFile = Encoding.Unicode.GetBytes(NeedArrayToUnicode);
                                try
                                {
                                    if (Command[2][0] == '/')
                                    {
                                        if (!MFT.CreateFile(Command[2], ArrayForCreateFile, 000, "rwxrwxrwx", US.user.Id_User))
                                            Console.WriteLine(error);
                                    }
                                    else
                                    {
                                        MFT.CreateFile(Directory + Command[2], ArrayForCreateFile, 000, "rwxrwxrwx", US.user.Id_User);
                                        Console.WriteLine();
                                    }
                                }
                                catch (Exception)
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine("Неверный параметр!");
                                    Console.ResetColor();
                                }
                            }
                            if (Command[1] == "<")
                            {
                                if (Command[2][0] == '/')
                                {
                                    MFT.EditContentFile(Command[2]);
                                }
                                else
                                {
                                    MFT.EditContentFile(Directory + Command[2]);
                                }
                            }
                        }
                        else if (Command.Length == 2)
                        {
                            if (Command[1][0] == '/')
                                MFT.ViewContentFile(Command[1]);
                            else
                                MFT.ViewContentFile(Directory + Command[1]);
                        }
                        break;
                    case "chmod":
                        string Level = "";
                        bool problem = false;
                        if (Command.Length == 3)
                        {
                            if (Command[1] != null & Command[1].Length == 3 & Command[2] != null)
                            {
                                if (Command[1].Length == 3)
                                {
                                    string s = Command[1], input = "";
                                    for(int i = 0; i < s.Length; i++)
                                    {
                                        if (Char.IsDigit(Convert.ToChar(s[i])))
                                        {
                                            input = Convert.ToString(s[i]);
                                            Level += Functions.Control.ConvertAccessLevel(input);
                                        }
                                        else
                                        {
                                            Console.ForegroundColor = ConsoleColor.Red;
                                            Console.WriteLine("При задании приоритета была допущена ошибка!");
                                            Console.ResetColor();
                                            problem = true;
                                        }
                                    }
                                    if (Level.Length != 9 & !problem)
                                    {
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine("Приоритет задаётся от 0 до 7!");
                                        Console.ResetColor();
                                        problem = true;
                                    }
                                }
                                if (!problem)
                                {
                                    if (!MFT.IsPathExist(Command[2] + Directory.Trim(' ')))
                                    {
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine("Файла не существует!");
                                        Console.ResetColor();
                                    }
                                    else
                                    {
                                        MFT.ChangeMode(Directory.Trim(' ') + Command[2], Level, US.user.Id_User);
                                    }
                                }
                            }
                        }
                        else if(Command.Length == 4)
                        {
                            if (Command[1] == "dir")
                            {
                                if (Command[2].Length == 3)
                                {
                                    string s = Command[2], input = "";
                                    for (int i = 0; i < s.Length; i++)
                                    {
                                        if (Char.IsDigit(Convert.ToChar(s[i])))
                                        {
                                            input = Convert.ToString(s[i]);
                                            Level += Functions.Control.ConvertAccessLevel(input);
                                        }
                                        else
                                        {
                                            Console.ForegroundColor = ConsoleColor.Red;
                                            Console.WriteLine("При задании приоритета была допущена ошибка!");
                                            Console.ResetColor();
                                            problem = true;
                                        }
                                    }
                                    if (Level.Length != 9 & !problem)
                                    {
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine("Приоритет задаётся от 0 до 7!");
                                        Console.ResetColor();
                                        problem = true;
                                    }
                                }
                                if (!problem)
                                {
                                    if (!MFT.IsPathExist(Directory.Trim(' ') + Command[3] + "/"))
                                    {
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine("Файла не существует!");
                                        Console.ResetColor();
                                    }
                                    else
                                    {
                                        MFT.ChangeModeDir(Directory.Trim(' ') + Command[3], Level, US.user.Id_User);
                                    }
                                }
                            }
                        }
                        break;
                    case "rm":
                        if (Command.Length == 2)
                        {
                            if (Command[1][0] == '/')
                            {
                                string[] el = Command[1].Split('/');
                                if (el[el.Length - 1] == "")
                                {
                                    Console.WriteLine("Имя файла не указано!");
                                    break;
                                }
                                else
                                {
                                    string path = "/";
                                    for (int i = 0; i < el.Length - 1; i++)
                                    {
                                        if (el[i] == "")
                                            continue;
                                        path += el[i] + '/';
                                    }
                                    if (!MFT.DeleteFile(el[el.Length - 1], path, US.user.Id_User))
                                    {
                                        Console.WriteLine(error);
                                    }
                                }
                            }
                            else
                            {
                                if (!MFT.DeleteFile(Command[1], Directory.Trim(' '), US.user.Id_User))
                                {
                                    Console.WriteLine(error);
                                }
                            }
                        }
                        break;
                    case "mv":
                        if (Command.Length == 3)
                        {

                            if (Command[1] == "" || Command[2] == "")
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("Введите исходную и удаленную папку!");
                                Console.ResetColor();
                            }
                            else if (Command[1][0] == '/' && Command[2][0] == '/')
                            {
                                MFT.MoveFile(Command[1], Directory + Command[2]);
                            }
                            else if (Command[1][0] == '/' && Command[2][0] != '/')
                            {
                                MFT.MoveFile(Command[1], Directory + Command[2]);
                            }
                            if (Command[1][0] != '/' && Command[2][0] == '/')
                            {
                                MFT.MoveFile(Directory + Command[1], Command[2]);
                            }
                            else
                                MFT.MoveFile(Directory + Command[1], Directory + Command[2]);
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Введите исходную и удаленную папку!");
                            Console.ResetColor();
                        }
                        break;
                    case "mvdir":
                        if (Command.Length == 3)
                        {
                            if (Command[1] == "" || Command[2] == "")
                            {
                                Console.WriteLine("Введите исходную и удаленную папку!");
                            }
                            else if (Command[1][0] == '/' && Command[1][0] == '/')
                            {
                                MFT.MoveDirectory(Command[1], Command[2], US.user.Id_User, false);
                            }
                            else if (Command[1][0] == '/' && Command[2][0] != '/')
                            {
                                MFT.MoveDirectory(Command[1], Directory + Command[2] + "/", US.user.Id_User, false);
                            }
                            if (Command[1][0] != '/' && Command[2][0] == '/')
                            {
                                MFT.MoveDirectory(Directory + Command[1] + "/", Command[2], US.user.Id_User,false);
                            }
                            else
                                MFT.MoveDirectory(Directory + Command[1] + "/", Directory + Command[2] + "/", US.user.Id_User, false);
                        }
                        else
                        {
                            Console.WriteLine("Введите исходную и удаленную папку!");
                        }
                        break;
                    case "adduser":
                        if (US.user.Id_User == 1)
                            US.CreateNewUser(GR, MFT, BOOT.Get_number_copy_mft_block());
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("У вас нет прав администратора!");
                            Console.ResetColor();
                        }
                        break;
                    case "rmuser":
                        if (US.user.Id_User == 1)
                        {
                            Console.Write("Введите логин: ");
                            string login = Console.ReadLine();
                            Console.Write("Введите пароль: ");
                            string password = Console.ReadLine();
                            if (US.DeleteUser(login, password, BOOT.Get_number_copy_mft_block(), MFT))
                            {
                                MFT.MoveDirectory($"/USERS/{login}/", "/USERS/SYSTEM/", 1, true);
                            }
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("У вас нет прав администратора!");
                            Console.ResetColor();
                        }
                        break;
                    case "out":
                        Console.Clear();
                        if(!Authorization())
                            return;
                        Console.Clear();
                        Directory = new string(US.user.Home_Directory).Trim(' ') + "/";
                        break;
                    case "help":
                        Console.WriteLine($"{"ls"} {"Утилита вывода информации о файлах или каталогах ",76}");
                        Console.WriteLine($"{"out"} {"Выход из учётной записи",49}");
                        Console.WriteLine($"{"exit"}{"Закрытие программы",44}");
                        Console.WriteLine($"{"info"}{"Информация о системе",46}");
                        Console.WriteLine($"{"rmuser"}{"Команда для удаления пользователя",57}");
                        Console.WriteLine($"{"adduser"}{"Команда для создания нового пользователя",63}");
                        Console.WriteLine($"{"cd + path"} {"Команда для изменения текущего рабочего каталога",68}");
                        Console.WriteLine($"{"rm + name_file"} {"Команда вывода информации о файлах или каталогах ",64}");
                        Console.WriteLine($"{"cat + name_file",15} {"Команда, выводящая содержание некоторых файлов",60}");
                        Console.WriteLine($"{"cat + \">\" name_file",10} {"Команда, записывающая данные в файл",45}");
                        Console.WriteLine($"{"cat + \"<\" name_file",10} {"Команда, изменияющая данные в файле",45}");
                        Console.WriteLine($"{"mkdir + name_directory"}{"Команда для создания новых каталогов",44}");
                        Console.WriteLine($"{"rmdir + name_directory"}{"Команда для удаления каталогов",38}");
                        Console.WriteLine($"{"chmod + priority + file"}{"Изменения прав доступа к файлам пользователя.",52}");
                        Console.WriteLine($"{"mv + source + destination"}{"Команда, для перемещения файлов и каталогов.",49}");
                        break;
                    case "info":
                        if (Command.Length == 1)
                        {
                            Console.WriteLine($"{"Файловая система:"}{"ASFS",13}");
                            Console.WriteLine($"{"Разработчик:",17}{"ст.гр.ПИ 16 - а Семик А.О",34}");
                            Console.WriteLine($"{"Прототип ФС:",17}{"FAT",12}");
                            Console.WriteLine($"{"Город:",17}{"Донецк",15}");
                        }
                        else if (Command.Length == 2)
                        {
                            if (Command[1] == "user")
                            {
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($"{"ID"}{"ИМЯ ПОЛЬЗОВАТЕЛЯ",20}{"ГРУППА",16}{"ДИРЕКТОРИЯ",20}");
                                Console.ResetColor();
                                if (US.user.Id_Group == 1)
                                    Console.WriteLine($"{US.user.Id_User}{new string(US.user.User_Name).Trim(' '),17}{"Admin",19}{new string(US.user.Home_Directory).Trim(' '),21}");
                                else
                                    Console.WriteLine($"{US.user.Id_User}{new string(US.user.User_Name).Trim(' '),17}{"USERS",19}{new string(US.user.Home_Directory).Trim(' '),21}");
                            }
                            if (Command[1] == "users")
                            {

                            }
                        }
                        break;
                    case "clear":
                        Console.Clear();
                        break;
                    case "exit":
                        Environment.Exit(0);
                        break;
                    case "format":
                        if (US.user.Id_User != 1)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("У вас нет прав администратора!");
                            Console.ResetColor();
                        }
                        else
                        {
                            try
                            {
                                File.Delete("ASFS.cn");
                                Environment.Exit(0);
                            }
                            catch (Exception)
                            {
                                Console.WriteLine("Something go wrong...");
                            }
                        }
                        break;
                }
            }
        }
    }
}