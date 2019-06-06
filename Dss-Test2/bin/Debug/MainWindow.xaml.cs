using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Numerics;
using System.Threading;
using DarkSoulsScripting;
using System.Runtime.InteropServices;

namespace Dss_Test2
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct SECURITY_ATTRIBUTES
        {
            public int nLength;
            public IntPtr lpSecurityDescriptor;
            public int bInheritHandle;
        }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct STARTUPINFO
        {
            public Int32 cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public Int32 dwX;
            public Int32 dwY;
            public Int32 dwXSize;
            public Int32 dwYSize;
            public Int32 dwXCountChars;
            public Int32 dwYCountChars;
            public Int32 dwFillAttribute;
            public Int32 dwFlags;
            public Int16 wShowWindow;
            public Int16 cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }
        internal struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool CreateProcess(
           string lpApplicationName,
           string lpCommandLine,
           ref SECURITY_ATTRIBUTES lpProcessAttributes,
           ref SECURITY_ATTRIBUTES lpThreadAttributes,
           bool bInheritHandles,
           uint dwCreationFlags,
           IntPtr lpEnvironment,
           string lpCurrentDirectory,
           [In] ref STARTUPINFO lpStartupInfo,
           out PROCESS_INFORMATION lpProcessInformation);
        [DllImport("ntdll.dll", SetLastError = true)]
        public static extern IntPtr NtResumeProcess(IntPtr ProcessHandle);
        [DllImport("ntdll.dll", SetLastError = false)]
        public static extern IntPtr NtSuspendProcess(IntPtr ProcessHandle);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern uint ResumeThread(IntPtr hThread);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetCursorPos(int x, int y);
        public System.Windows.Forms.Timer refresh = new System.Windows.Forms.Timer();

        public struct Coords
        {
            public int x, y;

            public Coords(int p1, int p2)
            {
                x = p1;
                y = p2;
            }
        }

        public string ipc = "";

        PROCESS_INFORMATION pInfo = new PROCESS_INFORMATION();

        void Poo()
        {

            //change save file name



            bool retvalue;
            const uint NORMAL_PRIORITY_CLASS = 0x0020;
            const int CREATE_SUSPENDED = 0x00000004;

            STARTUPINFO sInfo = new STARTUPINFO();
            SECURITY_ATTRIBUTES pSec = new SECURITY_ATTRIBUTES();
            SECURITY_ATTRIBUTES tSec = new SECURITY_ATTRIBUTES();
            pSec.nLength = Marshal.SizeOf(pSec);
            tSec.nLength = Marshal.SizeOf(tSec);
            sInfo.cb = Marshal.SizeOf(sInfo);


            //HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 570940\InstallLocation
            string currDir = Microsoft.Win32.Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\Steam App 570940", "InstallLocation", null).ToString();
            string Application = $"{currDir}\\DarkSoulsRemastered.exe";

            System.IO.File.WriteAllText($"{currDir}\\steam_appid.txt", "570940");

                    retvalue = CreateProcess(Application, "",
             ref pSec, ref tSec, false, NORMAL_PRIORITY_CLASS,
             IntPtr.Zero, currDir, ref sInfo, out pInfo);


            //   lock (ipc) ipc += "Process ID {pInfo.dwProcessId} started, suspended\n\n";
            //lock (ipc) ipc += $"Select your options then resume.\n";

            Hook.DARKSOULS.TryAttachToDarkSouls(pInfo.dwProcessId);

            //loggerman 141D06848
            //Wait till UserMan exists
            //while ((Hook.RIntPtr(0x141D06738) == IntPtr.Zero) && ((int)Hook.DARKSOULS.GetHandle() > 0))
            //Wait till FrpgSystem exists
            while ((Hook.RIntPtr(0x141C04E28) == IntPtr.Zero) && ((int) Hook.DARKSOULS.GetHandle() > 0))
            {

            }
            //NtSuspendProcess(pInfo.hProcess);

            lock (ipc) ipc += $"Launched PID {Hook.DARKSOULS.GetHandle()}\n";
            lock (ipc) ipc += "Redirecting save to DSPOO\n";
            Hook.WUnicodeStr(0x1412d7a38, "DSPOO");
            lock (ipc) ipc +=  "Preventing CT thread-start\n";
            Hook.WByte(0x14015af49, 0xC3);
            lock (ipc) ipc += "Applying NoLogo\n";
            Hook.WByte(0x14070C599, 1);



            lock (ipc) ipc +=  "Waiting for titlescreen\n";
            bool loop = true;
            while (loop && ((int) Hook.DARKSOULS.GetHandle() > 0))
            {
                loop = (Hook.RInt32(Hook.RIntPtr(Hook.RIntPtr(Hook.RIntPtr(Hook.RIntPtr(Hook.RIntPtr(0x141c04e28) + 0x8) + 0x20) + 0x58) + 0x20) + 0x10) < 65);
                Thread.Sleep(33);
            //lock (ipc) ipc += $"{Hook.RInt32(Hook.RIntPtr(Hook.RIntPtr(Hook.RIntPtr(Hook.RIntPtr(Hook.RIntPtr(0x141c04e28) + 0x8) + 0x20) + 0x58) + 0x20) + 0x10).ToString("x")}\n";
            //lock (ipc) ipc += $"{Hook.DARKSOULS.GetHandle()}\n";
            }
            lock (ipc) ipc +=  "Reached titlescreen\n";


                //Charinit ids
                //2400 = None, 2408 = Ring of old witch
                //3000 = Initial warrior, 3009 = Deprived

                //Genders
                //MsgMan.MenuOthersMsg.Msg(132010, "Dung"); //Male
                MsgMan.MenuOthersMsg.Msg(132011, "Fecal"); //Female

                //Gender Descriptions
                MsgMan.MenuOthersMsg.Msg(132310, "Male Defecator"); //Male
                MsgMan.MenuOthersMsg.Msg(132311, "Female Flinger"); //Female

                //Classes
                MsgMan.MenuOthersMsg.Msg(132020, "Stoolie"); //Warrior
                MsgMan.MenuOthersMsg.Msg(132021, "Turdle"); //Knight
                MsgMan.MenuOthersMsg.Msg(132022, "sCrapper"); //Wanderer
                MsgMan.MenuOthersMsg.Msg(132023, "jAnus"); //Thief
                MsgMan.MenuOthersMsg.Msg(132024, "BumBle"); //Bandit
                MsgMan.MenuOthersMsg.Msg(132025, "Poodle"); //Hunter
                MsgMan.MenuOthersMsg.Msg(132026, "Dumpling"); //Sorcerer
                MsgMan.MenuOthersMsg.Msg(132027, "Pooromancer"); //Pyromancer
                MsgMan.MenuOthersMsg.Msg(132028, ""); //Cleric
                MsgMan.MenuOthersMsg.Msg(132029, "AssAssIn"); //Deprived


                //Class Descriptions
                MsgMan.MenuOthersMsg.Msg(132320, "Gives a crap.\nDung-eon Expert."); //Warrior
                MsgMan.MenuOthersMsg.Msg(132321, "Tough as shit.\nDifficult to bowel over."); //Knight
                MsgMan.MenuOthersMsg.Msg(132322, "Refuses to\nDie. Or, he... uh...\nIs in denial."); //Wanderer
                MsgMan.MenuOthersMsg.Msg(132323, "Named after the god.\nProbably."); //Thief
                MsgMan.MenuOthersMsg.Msg(132324, "Has a silly name.\nDon't over-analYze it."); //Bandit
                MsgMan.MenuOthersMsg.Msg(132325, "Named after his dog.\nHe used to own a shih tzu."); //Hunter
                MsgMan.MenuOthersMsg.Msg(132326, "Knows magic.\nFavorite Spell:\nRing of Fire.");  //Sorcerer
                MsgMan.MenuOthersMsg.Msg(132327, "Master of the fecal arts."); //Pyromancer
                MsgMan.MenuOthersMsg.Msg(132328, "Gave up his name.\nNow travels the world\nInclognito."); //Cleric
                MsgMan.MenuOthersMsg.Msg(132329, "Studied Ninjitspoo\nSilent but deadly."); //Deprived



                //Gifts
                MsgMan.MenuOthersMsg.Msg(132050, "Poop"); //None
                MsgMan.MenuOthersMsg.Msg(132051, "Dung"); //Goddess Blessing
                MsgMan.MenuOthersMsg.Msg(132052, "Crap"); //Black Firebomb
                MsgMan.MenuOthersMsg.Msg(132053, "Doodoo"); //Twin Humanities
                MsgMan.MenuOthersMsg.Msg(132054, "Scheisse"); //Binoculars
                MsgMan.MenuOthersMsg.Msg(132055, "Merde"); //Pendant
                MsgMan.MenuOthersMsg.Msg(132056, "Dritt"); //Master Key
                MsgMan.MenuOthersMsg.Msg(132057, "дерьмо"); //Tiny Being's Ring
                MsgMan.MenuOthersMsg.Msg(132058, "Brown Gold"); //Old Witch's Ring




                //Menu Text

                MsgMan.MenuOthersMsg.Msg(401001, "Dung Souls: Dungmastered & 2012 DUNGAI POOCO Enterpooment Inc. / 2011-2018 PooSoftware Inc.");
                MsgMan.MenuOthersMsg.Msg(401301, "Pootinue"); //Continue
                MsgMan.MenuOthersMsg.Msg(401302, "Load Dump"); //Load Game
                MsgMan.MenuOthersMsg.Msg(401303, "Poo Game"); //New Game
                                                              //MsgMan.MenuOthersMsg.Msg(401304, "Scatstem"); //System
                MsgMan.MenuOthersMsg.Msg(401306, "Log"); //Log In
                                                         //MsgMan.MenuOthersMsg.Msg(401309, "Drop Deuce"); //Quit
                MsgMan.MenuOthersMsg.Msg(401311, "Dispoonected");
                MsgMan.MenuOthersMsg.Msg(401320, "Poo ver.");
                MsgMan.MenuOthersMsg.Msg(401322, "Irregular ver.");






                //isConsume = false;
                Params goods = ParamMan.FindParams("EquipParamGoods");
                IntPtr dungPie = goods.getOffset(293);
                Hook.WBit(dungPie + 0x45, 7, false);

                Params charInit = ParamMan.FindParams("CharaInitParam");
            lock (ipc) ipc +=  $"{ParamMan.Address.ToString("x")}\n";


                IntPtr gift;
                //Item1 = 293, quantity = 1
                for (int i = 0; i< 10; i++)
                {
                    gift = charInit.getOffset(2400 + i);
                    Hook.WInt32(gift + 0x7c, 293);
                    Hook.WByte(gift + 0xCC, 1);
                }


                gift = charInit.getOffset(2401);
                Hook.WInt32(gift + 0x84, 293);
                Hook.WByte(gift + 0xCe, 1);
            lock (ipc) ipc +=  $"{gift.ToString("x")}\n";

                //txtOutput.AppendText($"{charInit.getOffset(6740).ToString("x")}");

            }


        public MainWindow()
        {
            InitializeComponent();



            refresh.Tick += OnTimedEvent;
            refresh.Interval = 50;
            refresh.Enabled = true;

            this.Topmost = true;

        }

        public void Pooromancer()
        {



            UInt32 jmp = 0x2773FB;
            UInt32 facegen = 0x27aac0;
            UInt32 prototype = 0x25a130;
            UInt32 DbgDispStep = 0x25d590;
            //UInt32 DbgDispStep = 0x25d5f0;
            UInt32 dbgmenu = 0x25db50;
            UInt32 LoadGameStep = 0x27e680;
            UInt32 titlereserve = 0x282e20;
            UInt32 TitleMenuStep = 0x287940;

            UInt32 frpgNetDbgStep = 0x28f650;
            UInt32 bgmTestStep = 0x253350;
            UInt32 mapview = 0x24b9a0;
            UInt32 ChrFollowCam = 0x23cc40;
            UInt32 ChrExFollowCam = 0x234cb0;
            UInt32 normal = 0x7fd070;
            //uint32 normal = 0x25d1f0;  //simple ret func?

            //Hook.WUInt32(0x1402773f7, dbgmenu - jmp);

            //first tested 1edc80
            //search 48 89 4c 24 08 57 48 83 ec 30
            //last tested 2addb0






            //14015BC79
            //Hook.WUInt32(0x14015BC75,  0x276d50 -0x15BC79 );


            /*
            txtOutput.AppendText($"Setting Dung Pies to not be consumed\n");
            Params goods = ParamMan.EquipParamGoods;
            IntPtr dungPie = goods.getOffset(293);
            //isConsume = false;
            Hook.WBit(dungPie + 0x45, 7, false);
            txtOutput.AppendText($"{dungPie.ToString("x")}");
            */


        }



        public void OnTimedEvent(object sender, EventArgs e)
        {
            //ChrDbg.PlayerHide = true;
            //ChrDbg.PlayerNoDead = true;

            lock (ipc)
            {
                txtOutput.AppendText(ipc);
                ipc = "";
            }

            

            //txtOutput.Text = goods.getOffset(293).ToString("x");
            //txtOutput.Text = goods.Address.ToString("x");


            //Vector3 pos = new Vector3(-13.75f, 184.74f, -44.66f);    //Undead Asylum Spawn point pos

            //Coords xy = new Coords();
            //xy = WorldToScreen(pos);

            //SetCursorPos(xy.x, xy.y);

            //txtOutput.Text = xy.x.ToString() + ", " + xy.y.ToString();
            //txtOutput.Text = ChrCam.ProjectionMatrix.ToString();
        }



        private void BtnLaunch_Click(object sender, RoutedEventArgs e)
        {


            bool retvalue;
            const uint NORMAL_PRIORITY_CLASS = 0x0020;
            const int CREATE_SUSPENDED = 0x00000004;

            STARTUPINFO sInfo = new STARTUPINFO();
            SECURITY_ATTRIBUTES pSec = new SECURITY_ATTRIBUTES();
            SECURITY_ATTRIBUTES tSec = new SECURITY_ATTRIBUTES();
            pSec.nLength = Marshal.SizeOf(pSec);
            tSec.nLength = Marshal.SizeOf(tSec);
            sInfo.cb = Marshal.SizeOf(sInfo);


            //HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 570940\InstallLocation
            string currDir = Microsoft.Win32.Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\Steam App 570940", "InstallLocation", null).ToString();
            string Application = $"{currDir}\\DarkSoulsRemastered.exe";

            System.IO.File.WriteAllText($"{currDir}\\steam_appid.txt", "570940");

            retvalue = CreateProcess(Application, "",
     ref pSec, ref tSec, false, NORMAL_PRIORITY_CLASS,
     IntPtr.Zero, currDir, ref sInfo, out pInfo);


            txtOutput.AppendText($"Process ID {pInfo.dwProcessId} started, suspended\n\n");
            txtOutput.AppendText($"Select your options then resume.\n");

            Hook.DARKSOULS.TryAttachToDarkSouls(pInfo.dwProcessId);

            //loggerman 141D06848
            //Wait till UserMan exists
            //while ((Hook.RIntPtr(0x141D06738) == IntPtr.Zero) && ((int)Hook.DARKSOULS.GetHandle() > 0))
            //while ((Hook.RIntPtr(0x141c04e28) == IntPtr.Zero) && ((int)Hook.DARKSOULS.GetHandle() > 0))
            {

            }
            NtSuspendProcess(pInfo.hProcess);


        }

        private void BtnPooromancer_Click(object sender, RoutedEventArgs e)
        {
            new Thread(Poo).Start();
        }

        private void BtnResume_Click(object sender, RoutedEventArgs e)
        {
            NtResumeProcess(pInfo.hProcess);
        }

        private void BtnNoLogo_Click(object sender, RoutedEventArgs e)
        {
            txtOutput.AppendText($"Applying NoLogo\n");
            Hook.WByte(0x14070C599, 1);
        }

        private void BtnDbgMenu_Click(object sender, RoutedEventArgs e)
        {
            UInt32 jmp = 0x2773FB;
            UInt32 dbgmenu = 0x25db50;

            txtOutput.AppendText($"Setting DbgMenu start\n");
            Hook.WUInt32(0x1402773f7, dbgmenu - jmp);
        }

        private void BtnProto_Click(object sender, RoutedEventArgs e)
        {
            UInt32 jmp = 0x2773FB;
            UInt32 prototype = 0x25a130;

            txtOutput.AppendText($"Setting Prototype start\n");
            Hook.WUInt32(0x1402773f7, prototype - jmp);
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

}

