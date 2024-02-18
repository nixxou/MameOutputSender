using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Drawing;

namespace MameOutput_Test
{
    static class Program
    {
        private static string GameName { get; set; } = "";
        private static List<string> OutputsList = new List<string>();  
        /// <summary>
        /// Point d'entrée principal de l'application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            List<string> fakeArgs = new List<string>();
            fakeArgs.Add("gamename=ZogZog");
            fakeArgs.Add("outputs=GunRecoil_P1,GunRecoil_P2,GunRecoil_P3,GunRecoil_P4,TriggerPress_P1,TriggerPress_P2,TriggerPress_P3,TriggerPress_P4,Rumble_P1,Rumble_P2,Rumble_P3,Rumble_P4");
            //args = fakeArgs.ToArray();
      
            foreach(string arg in args)
            {
                if (arg.ToLower().StartsWith("gamename="))
                {
                  GameName = arg.ToUpper().Substring(9).Trim();
				        }
                if (arg.ToLower().StartsWith("outputs="))
                {
                  string outputs = arg.Substring(8).Trim();
                  foreach(string output in outputs.Split(','))
                  {
                    if(output.Trim() != string.Empty)
                    {
                      OutputsList.Add(output);
                    }
                  }
				        }
            }

            if(!string.IsNullOrEmpty(GameName) && OutputsList.Count() > 0)
            {


				      Application.EnableVisualStyles();
				      Application.SetCompatibleTextRenderingDefault(false);

							Application.Run(new WndMain(GameName, OutputsList));
			      }
            else
            {
              MessageBox.Show("Invalid Parameters");
            }
				}
	}




}
