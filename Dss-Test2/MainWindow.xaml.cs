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
using System.Numerics;
using System.Drawing;
using DarkSoulsScripting.Injection.DLL;

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
            WaitForBoot();
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
            WaitForBoot();
            SetNoLogo();
            WaitForTitle();


            EzDrawHook.Hook();
            EzDrawHook.SetHook(1);

            EzDrawHook.Text[] txts = new EzDrawHook.Text[8];
            EzDrawHook.Box[] boxes = new EzDrawHook.Box[8];

            Vector2 scrRatio = HgMan.ScreenSize / new Vector2(1280, 720);
            

            for (int x = 0; x < 8; x++)
            {
                boxes[x] = new EzDrawHook.Box();
                txts[x] = new EzDrawHook.Text();
                boxes[x].Size = new Vector2(120, 10);

            }

            EzDrawHook.Sphere sph = new EzDrawHook.Sphere();
            EzDrawHook.Cylinder cyl = new EzDrawHook.Cylinder();

            cyl.Pos = new Vector3(30, 30, 0);

            IntPtr test = IntPtr.Zero;

            while (true)
            {
                try
                {

                    //Enemy target = WorldChrMan.LocalPlayer.GetTargetAsEnemy();
                    //float BleedRatio = ((float)target.BleedResist / (float)target.MaxBleedResist);
                    float BleedRatio = (float)1;
                    
                    for (int x = 0; x < 8; x++)
                        if ((MenuMan.HpBars[x].Visible > -1) && (MenuMan.HpBars[x].Pos.X > 0) && (MenuMan.HpBars[x].Pos.Y > 0) && (MenuMan.HpBars[x].Handle > -1))
                        {
                            boxes[x].State = 7;
                            boxes[x].Pos = MenuMan.HpBars[x].Pos * scrRatio + new Vector2(15, 25);

                            txts[x].TextColor = Color.Red;
                            txts[x].Pos = MenuMan.HpBars[x].Pos * scrRatio + new Vector2(15, 45);
                            //txts[x].Txt = MenuMan.HpBars[x].Handle.ToString("X");

                            try
                            {
                                Enemy nme = Enemy.FromPtr(GetPlayerInsFromHandle(MenuMan.HpBars[x].Handle));
                                txts[x].Txt = MenuMan.HpBars[x].Handle.ToString("X") + " " + nme.BleedResist + " / " + nme.MaxBleedResist;
                                
                            }
                            catch { }

                        }
                        else
                        {
                            boxes[x].State = 0;
                            txts[x].TextColor = Color.FromArgb(0);
                        }

                    if (Hook.RByte(0x141d151c9) == 1)
                    {
                        test = GetPlayerInsFromHandle(WorldChrMan.LocalPlayer.TargetHandle);
                        Hook.WByte(0x141d151c9, 0);
                    }


                        
                }
               catch (Exception ex)
               {
                   Console.WriteLine(ex.Message);
                }

            }

        }

        public IntPtr GetPlayerInsFromHandle(Int32 Handle)
        {
            //Console.WriteLine($@"Handle: {Handle}");
            ulong FuncAddr = 0x140371e30;

            SafeRemoteHandle codeptr_ = new SafeRemoteHandle(0x1000);
            SafeRemoteHandle valptr_ = new SafeRemoteHandle(0x10);
            IntPtr codeptr = codeptr_.GetHandle();
            IntPtr valptr = valptr_.GetHandle();

            var c = new Assembler(64);
            c.push(rax);
            c.push(rbx);
            c.push(rcx);
            c.push(rdx);
            c.push(rbp);
            c.push(rsi);
            c.push(rdi);
            c.push(r8);
            c.push(r9);
            c.push(r10);
            c.push(r11);
            c.push(r12);
            c.push(r13);
            c.push(r14);
            c.push(r15);
            c.pushfq();
            c.sub(rsp, 0x100);

            c.mov(rbx, (ulong)valptr);
            c.mov(__qword_ptr[rbx], 0);
            c.mov(__qword_ptr[rbx+8], 0);
            c.xor(rbx, rbx);


            c.mov(rcx, (ulong)WorldChrMan.Address);
            c.mov(rdx, Handle);


            c.call(FuncAddr);

            c.mov(rbx, (ulong)valptr);
            c.mov(__qword_ptr[rbx], rax);
            c.mov(__qword_ptr[rbx + 8], 1);


            c.add(rsp, 0x100);
            c.popfq();
            c.pop(r8);
            c.pop(r9);
            c.pop(r10);
            c.pop(r11);
            c.pop(r12);
            c.pop(r13);
            c.pop(r14);
            c.pop(r15);
            c.pop(rdi);
            c.pop(rsi);
            c.pop(rbp);
            c.pop(rdx);
            c.pop(rcx);
            c.pop(rbx);
            c.pop(rax);
            c.ret();

            var stream = new MemoryStream();
            c.Assemble(new StreamCodeWriter(stream), (ulong)codeptr);
            Hook.WBytes(codeptr, stream.ToArray());

            //Console.WriteLine(codeptr.ToString("X"));
            uint MAX_WAIT = 1000;
            var threadHandle = new SafeRemoteThreadHandle(codeptr_);
            if (!threadHandle.IsClosed & !threadHandle.IsInvalid)
            {
                Kernel32.WaitForSingleObject(threadHandle.GetHandle(), MAX_WAIT);
            }

            UInt64 result = Hook.RUInt64(valptr);
            threadHandle.Close();
            threadHandle.Dispose();
            threadHandle = null;

            return (IntPtr)result;
        }

        

        private void WaitForBoot()
        {
            while (Hook.RUInt32(0x141d06ef8) == 0)
            { }
        }




        private void BtnBleedDisp_Click(object sender, RoutedEventArgs e)
        {
            new Thread(BleedDisp).Start();

        }
        void BleedDisp()
        {
            Launch();
            WaitForBoot();
            SetNoLogo();
            WaitForTitle();


            EzDrawHook.Hook();
            EzDrawHook.SetHook(1);

            EzDrawHook.Box[] bars = new EzDrawHook.Box[8];
            EzDrawHook.Box[] bgs = new EzDrawHook.Box[8];

            Vector2 scrRatio = HgMan.ScreenSize / new Vector2(1280, 720);

            for (int x = 0; x < 8; x++)
            {
                bgs[x] = new EzDrawHook.Box();
                bgs[x].Size = new Vector2(120, 10);
                bgs[x].Color1 = Color.Black;
                bgs[x].Color2 = Color.Black;
                bgs[x].IgnoreCulling = true;

                bars[x] = new EzDrawHook.Box();
                bars[x].Size = new Vector2(120, 10);
                bars[x].Color1 = Color.Red;
                bars[x].Color2 = Color.Red;
                bars[x].IgnoreCulling = true;
            }

            while (true)
            {
                try
                {
                    for (int x = 0; x < 8; x++)
                        if ((MenuMan.HpBars[x].Visible > -1) && (MenuMan.HpBars[x].Pos.X > 0) && (MenuMan.HpBars[x].Pos.Y > 0) && (MenuMan.HpBars[x].Handle > -1))
                        {
                            float BleedRatio = (float)1;
                            bgs[x].Pos = MenuMan.HpBars[x].Pos * scrRatio;

                            try
                            {
                                Enemy nme = Enemy.FromPtr(GetPlayerInsFromHandle(MenuMan.HpBars[x].Handle));
                                BleedRatio = (float)1 - ((float)nme.BleedResist / (float)nme.MaxBleedResist);
                                //BleedRatio = (float)0.33;
                                bars[x].Size = new Vector2(bgs[x].Size.X * BleedRatio, bgs[x].Size.Y);
                                bars[x].Pos = bgs[x].Pos - new Vector2((bgs[x].Size.X - bars[x].Size.X) / 2, 0);
                                
                            }
                            catch (Exception ex) { Console.WriteLine(ex.Message); }

                        }
                        else
                        {
                            bgs[x].Pos = new Vector2(-2000, -2000);
                            bars[x].Pos = bgs[x].Pos;
                        }

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

            }

        }
    

        private void BtnDbgRestore_Click(object sender, RoutedEventArgs e)
        {
            Launch();



            Hook.WUnicodeStr(0x1412d1f20, "./CAPTURE");



            while (Hook.RUInt32(0x141d06ef8) == 0)
            { }

            Hook.DARKSOULS.Suspend();
            Thread.Sleep(10000);
            Hook.DARKSOULS.Resume();
        }
    }

}

