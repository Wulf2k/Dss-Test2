using System;
using System.Diagnostics;
using System.Windows;
using System.Threading;
using DarkSoulsScripting;
using System.Runtime.InteropServices;
using Iced.Intel;
using DarkSoulsScripting.Injection.Structures;
using static Iced.Intel.AssemblerRegisters;
using System.IO;

namespace Dss_Test2
{

    public partial class MainWindow : Window
    {
        [DllImport("ntdll.dll", SetLastError = true)]
        public static extern IntPtr NtResumeProcess(IntPtr ProcessHandle);
        [DllImport("ntdll.dll", SetLastError = false)]
        public static extern IntPtr NtSuspendProcess(IntPtr ProcessHandle);


        public System.Windows.Forms.Timer refresh = new System.Windows.Forms.Timer();
        Process DS = new Process();


        public string ipc = "";

        void Launch()
        {
            string currDir = Microsoft.Win32.Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\Steam App 570940", "InstallLocation", null).ToString();
            string Application = $"{currDir}\\DarkSoulsRemastered.exe";

            DS.StartInfo.FileName = Application;
            DS.StartInfo.RedirectStandardError = true;
            DS.StartInfo.RedirectStandardOutput = true;
            DS.StartInfo.UseShellExecute = false;
            DS.StartInfo.WorkingDirectory = currDir;

            DS.Start();

            Hook.DARKSOULS.TryAttachToDarkSouls(DS.Id);
            output($"Launched PID {Hook.DARKSOULS.GetHandle()}\n");
        }
        void SetNoLogo()
        {
            output($"Setting NoLogo\n");
            Hook.WByte(0x14070c599, 1);
        }
        void WaitFrpgSysInit()
        {
            while ((Hook.RIntPtr(0x141C04E28) == IntPtr.Zero) && (Hook.RInt32(0x140000000) == 0x905a4d)) { }
        }
        void WaitForTitle()
        {
            output("Waiting for titlescreen\n");
            bool loop = true;
            while (loop && (Hook.RInt32(0x140000000) == 0x905a4d))
            {
                loop = (Hook.RInt32(Hook.RIntPtr(Hook.RIntPtr(Hook.RIntPtr(Hook.RIntPtr(Hook.RIntPtr(0x141c04e28) + 0x8) + 0x20) + 0x58) + 0x20) + 0x10) < 0xB);
                Thread.Sleep(33);
            }
            output("Reached titlescreen\n");
        }
        void AbortIfOnlineLoop()
        {
            output("AbortIfOnlineLoop starting\n");
            while (Hook.RInt32(0x140000000) == 0x905a4d)
            {
                if (GameMan.IsOnlineMode)
                {
                    output($"WOAH, dude.  Play in offline mode instead.\n(Ignore this on normal game exit)\n");
                    if (Hook.RInt32(0x140000000) == 0x905a4d)
                    {
                        //In case kill fails for some reason, will still attempt exit
                        Hook.WInt32(Hook.RIntPtr(Hook.RIntPtr(Hook.RIntPtr(Hook.RIntPtr(Hook.RIntPtr(0x141c04e28) + 0x8) + 0x20) + 0x58) + 0x20) + 0x10, 0x80);

                        try
                        {
                            DS.Kill();
                        }
                        catch { }
                    };
                    return;
                }
                Thread.Sleep(50);
            }
            output("AbortIfOnlineLoop exiting, ReadInt 0x140000000 = 0\n");
        }
        void NukeServerNames()
        {
            output($"Nuking FromSoft server names.\n");
            Hook.WAsciiStr(0x1413ff8c8, "tcp://nope.nope\0");
            Hook.WAsciiStr(0x1413ff900, "tcp://nope.nope\0");
            Hook.WAsciiStr(0x1413ff938, "tcp://nope.nope\0");
            Hook.WAsciiStr(0x1413ff970, "tcp://nope.nope\0");
            Hook.WAsciiStr(0x1713ff9a8, "tcp://nope.nope\0");
            Hook.WAsciiStr(0x1413ff9e0, "tcp://nope.nope\0");
            Hook.WAsciiStr(0x1413ffa18, "tcp://nope.nope\0");
            Hook.WAsciiStr(0x1413ffa50, "tcp://nope.nope\0");
            Hook.WAsciiStr(0x1413ffa88, "tcp://nope.nope\0");
        }


        public MainWindow()
        {
            InitializeComponent();

            refresh.Tick += OnTimedEvent;
            refresh.Interval = 50;
            refresh.Enabled = true;

            if (Hook.RInt32(0x140000000) == 0x905a4d)
            {
                output("Existing DSR session found, please exit and relaunch.");
            }

        }

        public void OnTimedEvent(object sender, EventArgs e)
        {

            lock (ipc)
            {
                txtOutput.AppendText(ipc);
                ipc = "";
            }

            if (Hook.DARKSOULS.Attached)
            {
                try
                {
                    if (!DS.StandardOutput.EndOfStream)
                    {
                        output($"DSStandardOutput: {DS.StandardOutput.ReadLine()}\n");
                    }
                    if (!DS.StandardError.EndOfStream)
                    {
                        output($"DSErrorOutput: {DS.StandardError.ReadLine()}\n");
                    }

                }
                catch { }

            }



            /*
            txtOutput.Text = $"SysStep: {FrpgSystem.Sys.Step}\n";
            txtOutput.AppendText($"TitleStep: {FrpgSystem.Title.Step}\n");
            txtOutput.AppendText($"InGameStep: {FrpgSystem.InGame.Step}\n");
            txtOutput.AppendText($"InGameStayStep: {FrpgSystem.InGameStay.Step}\n");
            txtOutput.AppendText($"CommonMenuStep: {FrpgSystem.CommonMenu.Step}\n");
            txtOutput.AppendText($"ChrBegin: {WorldChrMan.ChrsBegin.ToString("x")}\n");
            txtOutput.AppendText($"MapEntry: {Map.GetCurrent().GetName()}\n");
            */
        }
        public void output(string txt)
        {
            lock (ipc) ipc += txt;
        }

        private void BtnPooromancer_Click(object sender, RoutedEventArgs e)
        {
            new Thread(Poo).Start();
        }
        void Poo()
        {
            Launch();
            WaitFrpgSysInit();
            SetNoLogo();
            NukeServerNames();

            output("Redirecting save to DSPOO\n");
            Hook.WUnicodeStr(0x1412d7a38, "DSPOO");

            WaitForTitle();
            new Thread(AbortIfOnlineLoop).Start();

            //String Replacements
            {
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
                //MsgMan.MenuOthersMsg.Msg(401304, ""); //System
                MsgMan.MenuOthersMsg.Msg(401306, "Log"); //Log In
                //MsgMan.MenuOthersMsg.Msg(401309, ""); //Quit
                MsgMan.MenuOthersMsg.Msg(401311, "Dispoonected");
                MsgMan.MenuOthersMsg.Msg(401320, "Poo ver.");
                MsgMan.MenuOthersMsg.Msg(401322, "Irregular ver.");

                //DungPies.isConsume = false;
                Params goods = ParamMan.FindParams("EquipParamGoods");
                IntPtr dungPie = goods.getOffset(293);
                Hook.WBit(dungPie + 0x45, 7, false);

                Params charInit = ParamMan.FindParams("CharaInitParam");
                //Item1 = 293, quantity = 1, for each starting class
                //Last 3 gifts reported not working, confirm item category
                IntPtr gift;
                for (int i = 0; i < 10; i++)
                {
                    gift = charInit.getOffset(2400 + i);
                    Hook.WInt32(gift + 0x7c, 293);
                    Hook.WByte(gift + 0xCC, 1);
                }
            }

        }

        private void BtnNoLogo_Click(object sender, RoutedEventArgs e)
        {
            new Thread(NoLogo).Start();
        }
        void NoLogo()
        {
            Launch();
            WaitFrpgSysInit();
            SetNoLogo();
            NukeServerNames();

            output("Redirecting save to XLOGO\n");
            Hook.WUnicodeStr(0x1412d7a38, "XLOGO");

            WaitForTitle();
            new Thread(AbortIfOnlineLoop).Start();
        }

        private void BtnDbgMenu_Click(object sender, RoutedEventArgs e)
        {
            Launch();
            WaitFrpgSysInit();
            NukeServerNames();

            output("Redirecting save to DEBUG\n");
            Hook.WUnicodeStr(0x1412d7a38, "DEBUG");

            UInt32 jmp = 0x2773FB;
            UInt32 dbgmenu = 0x25db50;

            output($"Setting DbgMenu start\n");
            Hook.WUInt32(0x1402773f7, dbgmenu - jmp);
        }

        private void BtnProto_Click(object sender, RoutedEventArgs e)
        {
            Launch();
            WaitFrpgSysInit();
            NukeServerNames();

            output("Redirecting save to PROTO\n");
            Hook.WUnicodeStr(0x1412d7a38, "PROTO");

            UInt32 jmp = 0x2773FB;
            UInt32 prototype = 0x25a130;

            output($"Setting Prototype start\n");
            Hook.WUInt32(0x1402773f7, prototype - jmp);
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnRelaunch_Click(object sender, RoutedEventArgs e)
        {
            new Thread(testing).Start();
        }
        private void testing()
        { 
            Launch();
            WaitFrpgSysInit();
            SetNoLogo();
            WaitForTitle();


            /*
            UInt32 jmp = 0x2773FB;
            UInt32 dbgmenu = 0x25db50;

            output($"Setting DbgMenu start\n");
            Hook.WUInt32(0x1402773f7, dbgmenu - jmp);
            */





            //activate hook branch
            Hook.WByte(0x141d173d2, 1);
            Hook.WByte(0x14015cf6a, 0x60);

            //nodead + silence
            Hook.WByte(0x141d151cf, 1);
            Hook.WByte(0x141d151d0, 1);

            SafeRemoteHandle drawString_ = new SafeRemoteHandle(0x1000);
            SafeRemoteHandle num_ = new SafeRemoteHandle(8);
            SafeRemoteHandle strings_ = new SafeRemoteHandle(0x800);
            SafeRemoteHandle pos_ = new SafeRemoteHandle(0x100);


            SafeRemoteHandle ezdrawptr_ = new SafeRemoteHandle(8);
            SafeRemoteHandle ezdrawstringptr_ = new SafeRemoteHandle(8);
            SafeRemoteHandle drawstringptr_ = new SafeRemoteHandle(8);
            SafeRemoteHandle hgmanptr_ = new SafeRemoteHandle(8);
            SafeRemoteHandle returnptr_ = new SafeRemoteHandle(8);
            SafeRemoteHandle hookptr_ = new SafeRemoteHandle(8);


            
            ulong num = (ulong)num_.GetHandle();
            ulong drawString = (ulong)drawString_.GetHandle();

            ulong strings = (ulong)strings_.GetHandle();
            ulong pos = (ulong)pos_.GetHandle();

            ulong ezdrawptr = (ulong)ezdrawptr_.GetHandle();
            ulong ezdrawstringptr = (ulong)ezdrawstringptr_.GetHandle();
            ulong drawstringptr = (ulong)drawstringptr_.GetHandle();
            ulong hgmanptr = (ulong)hgmanptr_.GetHandle();
            ulong returnptr = (ulong)returnptr_.GetHandle();
            ulong hookptr = (ulong)hookptr_.GetHandle();

            ulong hookspot = 0x1403D08AD;
            ulong returnspot = 0x1403d0b57;


            Hook.WUInt64((long)drawstringptr, drawString);
            Hook.WUInt64((long)ezdrawstringptr, 0x1401d6bf0);
            Hook.WUInt64((long)returnptr, returnspot);
            Hook.WUInt64((long)hookptr, hookspot);
            Hook.WUInt64((long)hgmanptr, 0x141c04ed8);



            Hook.WInt32((long)num, 3);

            Hook.WUnicodeStr((long)strings, "Test");
            Hook.WFloat((long)pos, 100);
            Hook.WFloat((long)pos +4, 100);

            Hook.WUnicodeStr((long)strings + 0x80, "Test2");
            Hook.WFloat((long)pos + 0x10, 100);
            Hook.WFloat((long)pos + 0x14, 200);

            Hook.WUnicodeStr((long)strings + 0x100, "Test3");
            Hook.WFloat((long)pos + 0x20, 100);
            Hook.WFloat((long)pos + 0x24, 200);

            var c = new Assembler(64);
            
            var startloop = c.CreateLabel();
            var doneloop = c.CreateLabel();

            c.push(rax);
            c.push(rcx);
            c.push(rdx);
            c.push(rbx);
            c.push(rbp);
            c.push(rsi);
            c.push(rdi);
            c.pushfq();
            c.sub(rsp, 0x100);

            c.mov(rcx, __[hgmanptr]);
            c.mov(rcx, __[rcx]);
            c.mov(rcx, __[rcx + 0x60]);
            c.mov(__[ezdrawptr], rcx);

            c.xor(r11, r11);
            c.xor(r12, r12);
            c.xor(r13, r13);
            c.xor(r14, r14);

            c.mov(r11, __[num]);
            c.cmp(r11, r12);
            c.je(doneloop);

            c.Label(ref startloop);
            c.mov(rcx, __[ezdrawptr]);

            c.mov(r8, strings);
            c.mov(r13, 0x80);
            c.imul(r13, r12);
            c.add(r8, r13);

            c.mov(rdx, pos);
            c.mov(r13, 0x10);
            c.imul(r13, r12);
            c.add(rdx, r13);

            c.push(r11);
            c.push(r12);
            c.sub(rsp, 0x40);

            c.call(__qword_ptr[ezdrawstringptr]);

            c.add(rsp, 0x40);
            c.pop(r12);
            c.pop(r11);

            c.inc(r12);
            c.cmp(r11, r12);
            c.jne(startloop);

            c.Label(ref doneloop);
            c.add(rsp, 0x100);
            c.popfq();
            c.pop(rdi);
            c.pop(rsi);
            c.pop(rbp);
            c.pop(rbx);
            c.pop(rdx);
            c.pop(rcx);
            c.pop(rax);

            c.jmp(__qword_ptr[returnptr]);

            var stream = new MemoryStream();
            c.Assemble(new StreamCodeWriter(stream), drawString);

            Hook.WBytes((IntPtr)drawString, stream.ToArray());




            c = new Assembler(64);
            c.PreferVex = false;
            c.jmp(__qword_ptr[drawstringptr]);

            stream = new MemoryStream();
            c.Assemble(new StreamCodeWriter(stream), hookspot);
            Hook.WBytes((IntPtr)hookspot, stream.ToArray());




            while (true)
            {

                try
                {
                    //Hook.WFloat((long)pos, MouseMan.X);
                    //Hook.WFloat((long)pos + 4, MouseMan.Y);

                    //Hook.WFloat((long)pos + 0x10, MouseMan.X);
                    //Hook.WFloat((long)pos + 0x14, MouseMan.Y + 50);

                    Hook.WFloat((long)pos + 0, 100);
                    Hook.WFloat((long)pos + 4, 600);

                    Hook.WFloat((long)pos + 0x10, 100);
                    Hook.WFloat((long)pos + 0x14, 630);

                    Hook.WFloat((long)pos + 0x20, 600);
                    Hook.WFloat((long)pos + 0x24, 900);

                    Hook.WUnicodeStr((long)strings + 0x100, Hook.RUnicodeStr(Hook.RIntPtr(Hook.RIntPtr(Hook.RIntPtr(Hook.RIntPtr(Hook.RIntPtr(0x141d151b0) + 0x68) + 0x370) + 0x78) + 0x8), 0x1f));


                    if (WorldChrMan.LocalPlayer.TargetHandle > 0)
                    {
                        Hook.WUnicodeStr((long)strings, WorldChrMan.LocalPlayer.GetTargetAsEnemy().ModelName);
                        Hook.WUnicodeStr((long)strings + 0x80, WorldChrMan.LocalPlayer.GetTargetAsEnemy().BleedResist.ToString() + " / " + WorldChrMan.LocalPlayer.GetTargetAsEnemy().MaxBleedResist.ToString());

                    }
                    else
                    {
                        Hook.WUnicodeStr((long)strings, " ");
                        Hook.WUnicodeStr((long)strings + 0x80, " ");
                    }
                }
               catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

            }

        }

        private void BtnBleedDisp_Click(object sender, RoutedEventArgs e)
        {
            new Thread(BleedDisp).Start();
            
        }

        void BleedDisp()
        {
            Launch();
            WaitFrpgSysInit();
            Hook.WByte(0x14015af49, 0xc3);
            SetNoLogo();
            NukeServerNames();
            WaitForTitle();

            try
            {
                while (true)
                {

                    ChrDbg.PlayerHide = true;
                    ChrDbg.PlayerNoDead = true;
                    ChrDbg.ShowAltimeter = MenuMan.LockIconVisible;
                    GameRend.DisplayVelocityMap = MenuMan.LockIconVisible;
                    RenderTargetMan.VelocityMapFilter = 0x8000002e;

                    //01 crash
                    //02 07 0d 0f 11
                    //13
                    //1b yellow, velocity map
                    //25 28 2a
                    //2e red
                    //0x79 = white visible
                    //0x7c, 7d, 8a, 

                    IntPtr altGraphX = (IntPtr)0x141335890;

                    IntPtr l1p1x = (IntPtr)0x1413358E0;
                    IntPtr l1p1y = (IntPtr)0x1413358e4;

                    IntPtr l1p2x = (IntPtr)0x141335910;
                    IntPtr l1p2y = (IntPtr)0x141335914;

                    IntPtr l2p1x = (IntPtr)0x1413358f0;
                    IntPtr l2p1y = (IntPtr)0x1413358f4;

                    IntPtr l2p2x = (IntPtr)0x141335900;
                    IntPtr l2p2y = (IntPtr)0x141335904;

                    IntPtr altTextX = (IntPtr)0x141335920;

                    Hook.WFloat(altGraphX, -500);
                    Hook.WFloat(altTextX, -500);

                    Point tgt = new Point(0, 0);

                    GameRend.VelocityMapHeight = 10;
                    if (MenuMan.BossGauge1_Enabled > -1)
                    {
                        //Enemy nmes = WorldChrMan.GetEnemies().Find(x => x

                    }

                    if (MenuMan.LockIconVisible)
                    {
                        Enemy target = WorldChrMan.LocalPlayer.GetTargetAsEnemy();
                        float BleedRatio = ((float)target.BleedResist / (float)target.MaxBleedResist);


                        if (MenuMan.BossGauge1_Enabled > -1)
                        {
                            GameRend.VelocityMapWidth = 875 * (1 - BleedRatio);
                            Console.WriteLine(target.Address.ToString("X"));

                            tgt.X = 560;
                            tgt.Y = 900;


                            GameRend.VelocityMapXPos = (float)tgt.X + 830 -  (875 - GameRend.VelocityMapWidth / 2);
                            GameRend.VelocityMapYPos = (float)tgt.Y + 25;

                            Hook.WFloat(l1p1x, (float)tgt.X - 45);
                            Hook.WFloat(l1p1y, (float)tgt.Y + 30);

                            Hook.WFloat(l1p2x, (float)tgt.X - 45);
                            Hook.WFloat(l1p2y, (float)tgt.Y + 20);

                            Hook.WFloat(l2p1x, (float)tgt.X + 830);
                            Hook.WFloat(l2p1y, (float)tgt.Y + 30);

                            Hook.WFloat(l2p2x, (float)tgt.X + 830);
                            Hook.WFloat(l2p2y, (float)tgt.Y + 20);
                        }
                        else
                        {

                            GameRend.VelocityMapWidth = 120 * (1 - BleedRatio);
                            Console.WriteLine(target.Address.ToString("X"));

                            tgt.X = MenuMan.FloatingHPBar1xpos / (float)1280 * (float)HgMan.ScreenWidth;
                            tgt.Y = MenuMan.FloatingHPBar1ypos / (float)720 * (float)HgMan.ScreenHeight;

                            GameRend.VelocityMapXPos = (float)tgt.X + 75 - (120 - GameRend.VelocityMapWidth / 2);
                            GameRend.VelocityMapYPos = (float)tgt.Y + 25;

                            Hook.WFloat(l1p1x, (float)tgt.X - 45);
                            Hook.WFloat(l1p1y, (float)tgt.Y + 30);

                            Hook.WFloat(l1p2x, (float)tgt.X - 45);
                            Hook.WFloat(l1p2y, (float)tgt.Y + 20);

                            Hook.WFloat(l2p1x, (float)tgt.X + 75);
                            Hook.WFloat(l2p1y, (float)tgt.Y + 30);

                            Hook.WFloat(l2p2x, (float)tgt.X + 75);
                            Hook.WFloat(l2p2y, (float)tgt.Y + 20);
                        }
                        
                        
                    }

                    Thread.Sleep(10);
                }
            }
            catch
            {

            }
        }
    }

}

