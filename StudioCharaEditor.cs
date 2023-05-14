using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;
using KKAPI;
using UnityEngine;

namespace StudioCharaEditor
{
    [BepInPlugin(GUID, Name, Version)]
    [BepInDependency(KoikatuAPI.GUID, "1.4")]
    [BepInDependency("mikke.pushUpAI", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.fairbair.hs2_boobsettings", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInProcess("StudioNEOV2.exe")]
    public class StudioCharaEditor : BaseUnityPlugin
    {
        public const string GUID = "Countd360.StudioCharaEditor.HS2";
        public const string Name = "Studio Chara Editor";
        public const string Version = "2.1.0";
        public const string DefaultPathMacro = "$DEFAULT_CHAR_PATH$";
        public const string DefaultCoordMacro = "$DEFAULT_COORD_PATH$";

        public static StudioCharaEditor Instance { get; private set; }
        internal static new ManualLogSource Logger;

        // configs
        public static ConfigEntry<KeyboardShortcut> KeyShowUI { get; private set; }
        public static ConfigEntry<string> CharaExportPath { get; private set; }
        public static ConfigEntry<string> CoordExportPath { get; private set; }
        public static ConfigEntry<bool> DoubleThumbnailSize { get; private set; }
        public static ConfigEntry<bool> PreciseInputMode { get; private set; }
        public static ConfigEntry<bool> UnlimitedSlider { get; private set; }
        public static ConfigEntry<bool> ShowSelectedThumb { get; private set; }
        public static ConfigEntry<bool> CloseListAfterSelect { get; private set; }

        public static ConfigEntry<bool> VerboseMessage { get; private set; }

        public static ConfigEntry<int> UIXPosition { get; private set; }
        public static ConfigEntry<int> UIYPosition { get; private set; }
        public static ConfigEntry<int> UIWidth { get; private set; }
        public static ConfigEntry<int> UIHeight { get; private set; }
        public static ConfigEntry<string> UILanguage { get; private set; }

        //private ConfigEntry<string> configGreeting;
        //private ConfigEntry<bool> configDisplayGreeting;

        private void Awake()
        {
            Instance = this;
            Logger = base.Logger;

            // config
            KeyShowUI = Config.Bind("General", "StudioCharaEditor UI shortcut key", new KeyboardShortcut(KeyCode.D, KeyCode.LeftShift), "Toggles the main UI on and off.");
            CharaExportPath = Config.Bind("General", "Default charactor export path", DefaultPathMacro, "Set default charactor export path. $DEFAULT_CHAR_PATH$ stands for UserData\\chara\\male or UserData\\chara\\female");
            CoordExportPath = Config.Bind("General", "Default coordinate export path", DefaultCoordMacro, "Set default coordinate export path. $DEFAULT_COORD_PATH$ stands for UserData\\coordinate\\male or UserData\\coordinate\\female");
            DoubleThumbnailSize = Config.Bind("General", "Double export PNG size", false, "Use double sized thumbnail photo when export char to PNG");
            PreciseInputMode = Config.Bind("General", "Precise input mode", false, "Allows the user to enter decimal for fine adjustment");
            UnlimitedSlider = Config.Bind("General", "Unlimited slider bar", false, "Slider input without limit check. AT YOUR OWN RISK!");
            ShowSelectedThumb = Config.Bind("General", "Thumbnail of current item", true, "Show the thumbnail of current selected item (hair, clothes, etc)");
            CloseListAfterSelect = Config.Bind("General", "Folder list after select", true, "Auto folder up the list after click on a item");

            VerboseMessage = Config.Bind("Debug", "Print verbose info", false, "Print more debug info to console.");

            UIXPosition = Config.Bind("GUI", "Main GUI X position", 50, "X offset from left in pixel");
            UIYPosition = Config.Bind("GUI", "Main GUI Y position", 300, "Y offset from top in pixel");
            UIWidth = Config.Bind("GUI", "Main GUI window width", 600, "Main window width, minimum 600, set it when UI is hided.");
            UIHeight = Config.Bind("GUI", "Main GUI window height", 400, "Main window height, minimum 400, set it when UI is hided.");
            UILanguage = Config.Bind("GUI", "GUI Language", "default", "Language setting, valid setting can be found in HS2StudioCharaEditor.xml. Need reload.");


            /*
            configGreeting = Config.Bind("General",   // The section under which the option is shown
                                        "GreetingText",  // The key of the configuration option in the configuration file
                                        "Hello, world!", // The default value
                                        "A greeting text to show when the game is launched"); // Description of the option to show in the config file
            configDisplayGreeting = Config.Bind("General.Toggles",
                                            "DisplayGreeting",
                                            true,
                                            "Whether or not to show the greeting text");
            */

            // init accessories plugin
            PluginMoreAccessories.Initialize();

            // start
            GameObject gameObject = new GameObject(Name);
            UnityEngine.Object.DontDestroyOnLoad(gameObject);
            CharaEditorMgr.Install(gameObject);

            // Patch
            //Harmony harmony = new Harmony(GUID);
            //harmony.PatchAll(Assembly.GetExecutingAssembly());
            
        }

    }
}
