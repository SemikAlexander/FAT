using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileOS.Functions
{
    class Control
    {
        public static string[] AL = { "---", "--x", "-w-", "-wx", "r--", "r-x", "rw-", "rwx" };
        public static string ConvertAccessLevel(string Input)
        {
            string level = "";
            for (int i = 0; i < Input.Length; i++)
            {
                for (int j = 0; j < AL.Length; j++)
                {
                    if (Convert.ToInt32(Convert.ToString(Input[i])) == j)
                        level += AL[j];
                }
            }
            return level;
        }
    }
}
