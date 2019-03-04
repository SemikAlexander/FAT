using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileOS
{
    class Program
    {
        ASFS _fs = new ASFS();
        static void Main(string[] args)
        {
            new Program().Start();
            Console.ReadKey();
        }

       public void Start()
        {
            if (!_fs.File_init) InitializeSystem();
            Console.SetCursorPosition(Console.WindowWidth / 3, Console.WindowHeight / 2);
            Console.Write("СИСТЕМА ЗАГРУЖАЕТСЯ...");
            if (!_fs.StartSystem())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.SetCursorPosition(Console.WindowWidth / 4, Console.WindowHeight / 2);
                Console.Clear();
                Console.Write("Файловая система некорректна... :(");
                Console.ResetColor();
                Environment.Exit(0);
            }
        }

        void InitializeSystem()
        {
            Console.Title = "Initialize System";
            UInt32 Size = 0;
            Console.Write("Введите размер вашего жёсткого диска (MB): ");
            while ((!UInt32.TryParse(Console.ReadLine(), out Size)) | Size < 0 | Size > 3072)
            {
                Console.Clear();
                Console.Write("Введите размер вашего жёсткого диска (MB): ");
            }
            Console.Write("Введите имя пользователя: ");
            string UserName = Console.ReadLine();
            Console.Write("Введите логин: ");
            string UserLogin = Console.ReadLine();
            Console.Write("Введите пароль: ");
            string UserPassword = Console.ReadLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Установка системы...");
            Console.ResetColor();
            _fs.Init(Size, UserLogin, UserPassword, UserName);
            return;
        }
    }
}
