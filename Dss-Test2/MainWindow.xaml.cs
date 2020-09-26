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

        Thread mainthread = null;

        public string ipc = "";

        void Launch()
        {
            string currDir = Microsoft.Win32.Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\Steam App 570940", "InstallLocation", null).ToString();
            string Application = $"{currDir}\\DarkSoulsRemastered.exe";


            try
            {
                System.IO.File.WriteAllText($@"{currDir}\steam_appid.txt", "570940");
            }
            catch { };
            

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
                output("Existing DSR session found.");
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
                if (DS.Site != null)
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
            if (mainthread != null)
            {
                mainthread.Abort();
                mainthread = null;
            }


            this.Close();
        }

        private void btnRelaunch_Click(object sender, RoutedEventArgs e)
        {
            new Thread(testing2).Start();
        }
        private void testing()
        {
            if (FrpgSystem.Address == IntPtr.Zero)
            {
                Launch();
                WaitForBoot();
                SetNoLogo();
                WaitForTitle();
            }
            
            try
            {

                
                
                EzDrawHook.Hook3();

                EzDrawHook.Box soulbox = new EzDrawHook.Box();
                EzDrawHook.Text soultxt = new EzDrawHook.Text();
                EzDrawHook.Sphere humsph = new EzDrawHook.Sphere();
                EzDrawHook.Text humtxt = new EzDrawHook.Text();
                EzDrawHook.Box maxhpbox = new EzDrawHook.Box();
                EzDrawHook.Box hpbox = new EzDrawHook.Box();
                EzDrawHook.Text hptxt = new EzDrawHook.Text();
                EzDrawHook.Box maxstambox = new EzDrawHook.Box();
                EzDrawHook.Box stambox = new EzDrawHook.Box();
                EzDrawHook.Text stamtxt = new EzDrawHook.Text();
                


                while ((FrpgSystem.Address != IntPtr.Zero) && (MenuMan.GestureMenuState == 0))
                {
                    try
                    {

                        Vector2 pixRatio = FrpgWindow.DisplaySize / new Vector2(1000, 1000);
                        Vector2 txtRatio = FrpgWindow.WindowSize / new Vector2(1000, 1000);



                        soulbox.Color1 = Color.DeepPink;
                        soulbox.Color2 = Color.DeepPink;
                        soulbox.Size = new Vector2(150, 40) * pixRatio;
                        soulbox.Pos = new Vector2(800, 910) * pixRatio;

                        soultxt.TextColor = Color.Azure;
                        soultxt.Size = 20;
                        soultxt.Pos = new Vector2(825, 915) * txtRatio;
                        soultxt.Txt = "00000000";

                        
                        humsph.Color1 = Color.Blue;
                        humsph.Color2 = Color.Red;
                        humsph.Size = new Vector3(50, 50, 0) * new Vector3(pixRatio, 0);
                        humsph.Pos = new Vector3(100, 100, 0) * new Vector3(pixRatio, 0);


                        humtxt.TextColor = Color.Cyan;
                        humtxt.Size = 30;
                        humtxt.Pos = new Vector2(75, 75) * txtRatio;
                        humtxt.Txt = "00";

                        maxhpbox.Color1 = Color.Teal;
                        maxhpbox.Color2 = Color.Purple;
                        maxhpbox.Size = new Vector2(WorldChrMan.LocalPlayer.Stats.MaxHP * 0.75f, 40) * pixRatio;
                        maxhpbox.Pos = new Vector2(165, 50) * pixRatio;

                        hpbox.Color1 = Color.DarkGoldenrod;
                        hpbox.Color2 = Color.DarkGoldenrod;
                        hpbox.Size = new Vector2(WorldChrMan.LocalPlayer.Stats.HP * 0.75f, 40) * pixRatio;
                        hpbox.Pos = new Vector2(165, 50) * pixRatio;

                        hptxt.TextColor = Color.Cyan;
                        hptxt.Size = 20;
                        hptxt.Pos = new Vector2(170, 55) * txtRatio;
                        hptxt.Txt = $@"{WorldChrMan.LocalPlayer.HP}/{WorldChrMan.LocalPlayer.MaxHP}";

                        maxstambox.Color1 = Color.Purple;
                        maxstambox.Color2 = Color.Teal;
                        maxstambox.Size = new Vector2(WorldChrMan.LocalPlayer.Stats.MaxStamina * 2, 40) * pixRatio;
                        maxstambox.Pos = new Vector2(165, 100) * pixRatio;

                        stambox.Color1 = Color.DarkSeaGreen;
                        stambox.Color2 = Color.DarkSeaGreen;
                        stambox.Size = new Vector2(WorldChrMan.LocalPlayer.Stats.Stamina * 2, 40) * pixRatio;
                        stambox.Pos = new Vector2(165, 100) * pixRatio;

                        stamtxt.TextColor = Color.Cyan;
                        stamtxt.Size = 20;
                        stamtxt.Pos = new Vector2(170, 105) * txtRatio;
                        stamtxt.Txt = $@"{WorldChrMan.LocalPlayer.Stats.Stamina}/{WorldChrMan.LocalPlayer.Stats.MaxStamina}";

                        //if (nmecount > 0)
                        //{
                        //if (lastdpad != PadMan.dpad)
                        //{
                        //if (PadMan.dpad == 4)  //left
                        ///idx--;
                        //if (PadMan.dpad == 8)
                        //idx++;
                        //if (idx < 0)
                        ///idx = WorldChrMan.GetEnemies().Count - 1;
                        //if (idx > WorldChrMan.GetEnemies().Count - 1)
                        //idx = 0;
                        //lastdpad = PadMan.dpad;
                        //}

                        //Player player = WorldChrMan.LocalPlayer;
                        //Enemy nme = WorldChrMan.GetEnemies()[idx];

                        //chridx.Txt = idx.ToString();
                        //chrname.Txt = nme.GetName();

                        //nme.ChrCtrl.DebugPlayerControllerPtr = player.ChrCtrl.ControllerPtr;
                        //player.WarpToEnemy(nme);

                        //WorldChrManDbg.DbgViewChrIns = nme.Address;

                        //player.Slot.IsDisable = true;
                        //player.Slot.IsHide = true;
                        //}

                        //Console.WriteLine(nme.Address.ToString("X"));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }


        void testing2()
        {
            if (FrpgSystem.Address == IntPtr.Zero)
            {
                Launch();
                WaitForBoot();
                //DbgNodeRestore();

                UInt32 jmp = 0x2773FB;
                UInt32 prototype = 0x25a130;
                output($"Setting Prototype start\n");
                Hook.WUInt32(0x1402773f7, prototype - jmp);

                Thread.Sleep(500);
            }

            Vector2 pixRatio = FrpgWindow.DisplaySize / new Vector2(1920, 1080);
            Vector2 txtRatio = FrpgWindow.WindowSize / new Vector2(1920, 1080);

            

            UInt32 ProtoStepNum = 0;


            while (ProtoStepNum != 4)
            {
                //[[[[[141c04e28]+8]+20]+58]+20]
                ProtoStepNum = Hook.RUInt32(Hook.RIntPtr(Hook.RIntPtr(Hook.RIntPtr(Hook.RIntPtr(Hook.RIntPtr(0x141c04e28)+8)+0x20)+0x58)+0x20) + 0x10    );
                Thread.Sleep(33);
            }

            EzDrawHook.Hook3();
            

            
            EzDrawHook.Box titlebox = new EzDrawHook.Box();
            EzDrawHook.Text titletxt = new EzDrawHook.Text();

            titlebox.Color1 = Color.Black;
            titlebox.Color2 = Color.Black;
            titlebox.Size = new Vector2(200, 75) * pixRatio;
            titlebox.Pos = new Vector2(1200, 600) * pixRatio;

            titletxt.Pos = new Vector2(1250, 600) * txtRatio;
            titletxt.Size = 45;
            titletxt.TextColor = Color.Red;
            titletxt.Txt = "MEME";
            


            
            int loopcounter = 0;

            while (ProtoStepNum == 4)
            {
                //[[[[[141c04e28]+8]+20]+58]+20]
                ProtoStepNum = Hook.RUInt32(Hook.RIntPtr(Hook.RIntPtr(Hook.RIntPtr(Hook.RIntPtr(Hook.RIntPtr(0x141c04e28) + 8) + 0x20) + 0x58) + 0x20) + 0x10);
                titletxt.TextColor = Color.FromArgb(Convert.ToByte((Hook.RFloat(Hook.RIntPtr(Hook.RIntPtr(Hook.RIntPtr(Hook.RIntPtr(Hook.RIntPtr(0x141c04e28) + 8) + 0x20) + 0x58) + 0x20) + 0x38) / 3) * 255), 255, 0, 0);
                
                Thread.Sleep(33);
                loopcounter++;

                if (loopcounter == 200)
                    Hook.WUInt32(Hook.RIntPtr(Hook.RIntPtr(Hook.RIntPtr(Hook.RIntPtr(Hook.RIntPtr(0x141c04e28) + 8) + 0x20) + 0x58) + 0x20) + 0x10, 0xc);
            }

            loopcounter = 0;

            titlebox.Pos = new Vector2(0, 0);
            titlebox.Size = new Vector2(FrpgWindow.DisplaySize.X, 200) * pixRatio;
            titlebox.Color1 = Color.LightGray;
            titlebox.Color2 = Color.LightGray;

            titletxt.Pos = new Vector2(600, 75) * txtRatio;
            titletxt.TextColor = Color.Yellow;
            titletxt.Txt = "CHOOSE YOUR FIGHTER";

            EzDrawHook.Text beattxt = new EzDrawHook.Text();
            beattxt.Size = 30;
            beattxt.Txt = "Dun";

            Random rnd = new Random();


            EzDrawHook.Box chrbg = new EzDrawHook.Box();
            chrbg.Size = new Vector2(270, 710) * pixRatio;
            chrbg.Pos = new Vector2(395, 225) * pixRatio;
            chrbg.Color1 = Color.DarkGray;
            chrbg.Color2 = Color.DarkGray;
            
            
            EzDrawHook.Text chrname = new EzDrawHook.Text();
            chrname.TextColor = Color.Gold;
            chrname.Size = 25;
            chrname.Pos = new Vector2(400, 850) * txtRatio;
            chrname.Txt = "Sir Notappearing\nInthisfilm";
            


            while (loopcounter <= 40)
            {
                beattxt.Pos = new Vector2(rnd.Next(0, Convert.ToInt32(FrpgWindow.WindowSize.X)), rnd.Next(0, 200)) * txtRatio;
                beattxt.TextColor = Color.FromArgb(255, rnd.Next(0, 255), rnd.Next(0, 255), rnd.Next(0, 255));
                Thread.Sleep(250);
                loopcounter++;

                if ((loopcounter % 10) == 0)
                    Hook.WInt32(Hook.RIntPtr(Hook.RIntPtr(Hook.RIntPtr(Hook.RIntPtr(Hook.RIntPtr(0x141c04e28) + 8) + 0x20) + 0x58) + 0x20) + 0x44, loopcounter / 10);
            }


            chrbg.Cleanup();
            //chrname.Cleanup();
            chrname.Txt = " ";
            //beattxt.Cleanup();
            beattxt.Txt = " ";

            Thread.Sleep(1000);
            Hook.WUInt32(Hook.RIntPtr(Hook.RIntPtr(Hook.RIntPtr(Hook.RIntPtr(Hook.RIntPtr(0x141c04e28) + 8) + 0x20) + 0x58) + 0x20) + 0x10, 0xe);

            while (MenuMan.LoadingState == 0)
                Thread.Sleep(33);


            titlebox.Size = FrpgWindow.DisplaySize;
            titlebox.Pos = new Vector2(0, 0);
            titlebox.Color1 = Color.Black;
            titlebox.Color2 = Color.Black;

            titletxt.Txt = "L O A D I N G";
            titletxt.TextColor = Color.BlanchedAlmond;
            titletxt.Pos = new Vector2(FrpgWindow.WindowSize.X / 2 - 100, 500) * txtRatio;

            while (MenuMan.LoadingState > 0)
                Thread.Sleep(33);





            Thread.Sleep(1000);

            ChrDbg.PlayerHide = true;
            ChrDbg.AllNoUpdateAI = true;



            WorldChrMan.LocalPlayer.WarpToCoords(23.728f, 15.817f, -118.945f, 90.0f);

            Thread.Sleep(500);
            WorldChrMan.LocalPlayer.WarpToCoords(32.0f, 15.817f, -118.945f, 270.0f);

            FreeCam.Enabled = true;
            FreeCam.PosX = 28.5f;
            FreeCam.PosY = 18.5f;
            FreeCam.PosZ = -128.0f;
            FreeCam.RotX = 0.0f;
            FreeCam.RotY = 0.0f;
            FreeCam.RotZ = 1.0f;

            Thread.Sleep(500);

            MenuMan.ActionMsgState = 0;
            MenuMan.TextEffect = 0;

            

            GameDataMan.Options.HUD = false;

            //1010700 = taurus
            Enemy taurus = WorldChrMan.GetEnemyByName("c2250_0000");

            Thread.Sleep(1000);
            IngameFuncs.ForcePlayAnimation(1010700, -1);
            taurus.WarpToCoords(25.728f, 15.8f, -119.945f, 90.0f);

            titlebox.Cleanup();
            titletxt.Cleanup();

            IngameFuncs.ChangeTarget(1010700, 10000);
            IngameFuncs.ChangeTarget(10000, 1010700);

            Thread.Sleep(1000);

            IngameFuncs.PlayAnimation(10000, 6500);
            Thread.Sleep(1000);
            IngameFuncs.ForceDead(1010700, 1);
            GameMan.IsDisableAllAreaEvent = true;
            GameMan.IsDisableAllAreaEne = true;



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

            Vector2 scrRatio = FrpgWindow.DisplaySize / new Vector2(1280, 720);
            
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
                                Enemy nme = Enemy.FromPtr(IngameFuncs.GetPlayerInsFromHandle(MenuMan.HpBars[x].Handle));
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
    
        void DbgNodeRestore()
        {
            SafeRemoteHandle DbgMenuManFix_ = new SafeRemoteHandle(0x1000);
            SafeRemoteHandle DbgMenuManGetNode_ = new SafeRemoteHandle(0x10);
            IntPtr DbgMenuManFix = DbgMenuManFix_.GetHandle();
            IntPtr DbgMenuManGetNode = DbgMenuManGetNode_.GetHandle();

            var c = new Assembler(64);
            c.jmp(0x140152b00);
            var stream = new MemoryStream();
            c.Assemble(new StreamCodeWriter(stream), (ulong)0x140152af0);
            Hook.WBytes(0x140152af0, stream.ToArray());

            c = new Assembler(64);
            c.jmp((ulong)DbgMenuManGetNode);
            stream = new MemoryStream();
            c.Assemble(new StreamCodeWriter(stream), (ulong)0x140152b00);
            Hook.WBytes(0x140152b00, stream.ToArray());

            c = new Assembler(64);
            Label SkipDbgMenuManGetNode = c.CreateLabel();
            c.mov(rax, 0x141c04cc8);
            c.mov(rax, __[rax]);
            c.cmp(rax, 0);
            c.je(SkipDbgMenuManGetNode);
            c.mov(rax, __[rax + 8]);
            c.Label(ref SkipDbgMenuManGetNode);
            c.ret();
            stream = new MemoryStream();
            c.Assemble(new StreamCodeWriter(stream), (ulong)DbgMenuManGetNode);
            Hook.WBytes(DbgMenuManGetNode, stream.ToArray());

            c = new Assembler(64);
            c.jmp((ulong)DbgMenuManFix);
            stream = new MemoryStream();
            c.Assemble(new StreamCodeWriter(stream), (ulong)0x140152a38);
            Hook.WBytes(0x140152a38, stream.ToArray());

            c = new Assembler(64);
            c.mov(rcx, 0x1414acd70);
            c.call(0x140466aa0);
            c.mov(rcx, 0x141c04cc8);
            c.mov(rcx, __[rcx]);
            c.mov(__[rcx + 8], rax);
            c.add(rsp, 0x28);
            c.ret();
            stream = new MemoryStream();
            c.Assemble(new StreamCodeWriter(stream), (ulong)DbgMenuManFix);
            Hook.WBytes(DbgMenuManFix, stream.ToArray());


        }

        private void BtnDbgRestore_Click(object sender, RoutedEventArgs e)
        {
            //Restore debug nodes, not full menu
            Launch();

            Hook.WUnicodeStr(0x1412d1f20, "./CAPTURE");

            WaitForBoot();
            DbgNodeRestore();




            Hook.WByte(0x14015CF6A, 0x60);

        }

        private void btnTst_Click(object sender, RoutedEventArgs e)
        {

            EzDrawHook.Hook3();
            EzDrawHook.Box titlebox = new EzDrawHook.Box();

            titlebox.Size = new Vector2(250, 250);
            titlebox.Pos = new Vector2(1050, 300);

            titlebox.Flags = 7;
            titlebox.TexHandle = TexMan.GetHandleTexHdlResCap("Icon00");


            //for (int i = 0; i < TexMan.NumTexHdlResCaps; i++)
            //Console.WriteLine(TexMan.TexHdlResCaps[i].ResName);

        }

        private void btnDeadCnt_Click(object sender, RoutedEventArgs e)
        {
            
            new Thread(DeadCount).Start();
        }

        private void DeadCount()
        {
            if (FrpgSystem.Address.Equals(IntPtr.Zero))
            {
                Launch();
                WaitForTitle();
                while (MenuMan.LoadingState == 0)
                    Thread.Sleep(33);
                while (MenuMan.LoadingState > 0)
                    Thread.Sleep(33);
            }

            EzDrawHook.Hook3();
            EzDrawHook.Text deadtxt = new EzDrawHook.Text();

            deadtxt.Pos = FrpgWindow.DisplaySize / new Vector2(2.5f, 1.25f);
            deadtxt.Size = 70;
            deadtxt.TextColor = Color.Red;
            deadtxt.Txt = " ";

            while (Hook.RInt32(0x140000000) > 0)
            {
                if ((WorldChrMan.LocalPlayer.HP <= 0) && (MenuMan.LoadingState == 0))
                    deadtxt.Txt = $@"Painful Owwy Count:  {GameDataMan.DeathNum}";
                else
                    deadtxt.Txt = " ";
                Thread.Sleep(33);
            }
        }

    }

}

