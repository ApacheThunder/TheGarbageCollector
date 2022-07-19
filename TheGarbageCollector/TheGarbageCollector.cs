using UnityEngine;
using System.Collections.Generic;
using System;
using MonoMod.RuntimeDetour;
using System.Reflection;
using BepInEx;

namespace TheGarbageCollector {

    [BepInDependency("etgmodding.etg.mtgapi")]
    [BepInPlugin(GUID, ModName, VERSION)]
    public class TheGarbageCollector : BaseUnityPlugin {

        public static bool DisableGC = true;
        public static bool disableMonitor = false;

        public const string GUID = "ApacheThunder.etg.TheGarbageCollector";
        public const string ModName = "TheGarbageCollector";
        public const string VERSION = "1.4.2";

        public static readonly string ConsoleCommandName = "garbagecollector";
        public static readonly string GarbageCollectorToggleName = "TheGarbageCollectorDisabled";
        public static readonly string GarbageCollectorMemoryCap = "TheGarbageCollectorMemCap";
        public static readonly string ModNameInRed = "<color=#00FF00>[TheGarbageCollector]</color> ";
                
        public static string FolderPath;

        public static Hook BraveMemoryCollectHook;
        public static Hook clearLevelDataHook;
        public static Hook gameManagerHook;

        public static GameObject GCManagerObject;

        private static int GetBestMemoryCap {
            get {
                if (SystemInfo.systemMemorySize > 24576) {
                    return 8196;
                } else if (SystemInfo.systemMemorySize > 12288) {
                    return 5120;
                } else if (SystemInfo.systemMemorySize > 8196) {
                    return 3072;
                } else {
                    return 2048;
                }
            }
        }
        
        public void Start() { ETGModMainBehaviour.WaitForGameManagerStart(GMStart); }
        
        public void GMStart(GameManager gameManager) {
            FolderPath = this.FolderPath();

            ETGModConsole.Commands.AddGroup(ConsoleCommandName, ConsoleInfo);
            ETGModConsole.Commands.GetGroup(ConsoleCommandName).AddUnit("toggle", ToggleGCSetting);
            ETGModConsole.Commands.GetGroup(ConsoleCommandName).AddUnit("collect", DoACollect);
            ETGModConsole.Commands.GetGroup(ConsoleCommandName).AddUnit("stats", ToggleGCStats);
            ETGModConsole.Commands.GetGroup(ConsoleCommandName).AddUnit("setmemcap", SetMemoryCap);
            
            int ManualMemoryCap = PlayerPrefs.GetInt(GarbageCollectorMemoryCap);

            if (ManualMemoryCap >= 1024) {
                GC_Manager.MemoryGrowthAllowence = ManualMemoryCap;
                if (ManualMemoryCap >= 8196) { disableMonitor = true; }
            } else {
                GC_Manager.MemoryGrowthAllowence = GetBestMemoryCap;
                // If user has obcene amount of ram, there is no need to do colletions during gameplay unless player is AFK/has game paused and when a floor is loading.
                if (SystemInfo.systemMemorySize > 24576) { disableMonitor = true; }
            }

            if (PlayerPrefs.GetInt(GarbageCollectorToggleName) == 1) { DisableGC = false; return; }
            if (DisableGC) {
                ETGModConsole.Log(ModNameInRed + "Unity's Garbage Collector is now disabled. Use command garbagecolletor toggle to run it back on.");
                if (GC_Manager.load_mono_gc()) {
                    ToggleHooksAndGC(true);
                    if (SystemInfo.systemMemorySize < 8196) { ETGModConsole.Log(ModNameInRed + "Warning: Your computer was detected as having 8GB or less ram. It is recommended only to disable GarbageColletor on machines with more then 8GB of ram!", true); }
                }
            } else {
                ETGModConsole.Log(ModNameInRed + "Unity's Garbage Collector currently active. Use command garbagecolletor toggle to enable TheGarbageCollector and disable Unity's GarbageCollector.");
            }
        }


        private void GameManager_Awake(Action<GameManager> orig, GameManager self) {
            orig(self);
            self.OnNewLevelFullyLoaded += OnLevelFullyLoaded;
        }

        public static void OnLevelFullyLoaded() {
            if (DisableGC && GC_Manager.Instance && GC_Manager.d_gc_disabled) {
                if (gameManagerHook == null) {
                    gameManagerHook = new Hook(
                        typeof(GameManager).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance),
                        typeof(TheGarbageCollector).GetMethod("GameManager_Awake", BindingFlags.NonPublic | BindingFlags.Instance),
                        typeof(GameManager)
                    );
                }
                GC_Manager.Instance.DoCollect();
            }            
        }
        
        private void ConsoleInfo(string[] consoleText) {
            if (ETGModConsole.Commands.GetGroup(ConsoleCommandName) != null && ETGModConsole.Commands.GetGroup(ConsoleCommandName).GetAllUnitNames() != null) {
                List<string> m_CommandList = new List<string>();

                foreach (string Command in ETGModConsole.Commands.GetGroup(ConsoleCommandName).GetAllUnitNames()) { m_CommandList.Add(Command); }

                if (m_CommandList.Count <=0) { return; }

                if (!m_IsCommandValid(consoleText, string.Empty, string.Empty)) {
                    ETGModConsole.Log("[TheGarbageCollector] No sub command specified! The following console commands are available for TheGarbageCollector:\n", false);
                    foreach (string Command in m_CommandList) { ETGModConsole.Log("    " + Command + "\n", false); }
                    return;
                } else if (!m_CommandList.Contains(consoleText[0].ToLower())) {
                    ETGModConsole.Log("[TheGarbageCollector] Invalid sub-command! The following console commands are available for TheGarbageCollector:\n", false);
                    foreach (string Command in m_CommandList) { ETGModConsole.Log("    " + Command + "\n", false); }
                    return;
                }
            } else {
                return;
            }
        }

        private bool m_IsCommandValid(string[] CommandText, string validCommands, string sourceSubCommand) {
            if (CommandText == null) {
                if (!string.IsNullOrEmpty(validCommands) && !string.IsNullOrEmpty(sourceSubCommand)) { ETGModConsole.Log("[TheGarbageCollector] [" + sourceSubCommand + "] ERROR: Invalid console command specified! Valid Sub-Commands: \n" + validCommands); }
                return false;
            } else if (CommandText.Length <= 0) {
                if (!string.IsNullOrEmpty(validCommands) && !string.IsNullOrEmpty(sourceSubCommand)) { ETGModConsole.Log("[TheGarbageCollector] [" + sourceSubCommand + "] No sub-command specified. Valid Sub-Commands: \n" + validCommands); }
                return false;
            } else if (string.IsNullOrEmpty(CommandText[0])) {
                if (!string.IsNullOrEmpty(validCommands) && !string.IsNullOrEmpty(sourceSubCommand)) { ETGModConsole.Log("[TheGarbageCollector] [" + sourceSubCommand + "] No sub-command specified. Valid Sub-Commands: \n" + validCommands); }
                return false;
            } else if (CommandText.Length > 1) {
                if (!string.IsNullOrEmpty(validCommands) && !string.IsNullOrEmpty(sourceSubCommand)) { ETGModConsole.Log("[TheGarbageCollector] [" + sourceSubCommand + "] ERROR: Only one sub-command is accepted!. Valid Commands: \n" + validCommands); }
                return false;
            }
            return true;
        }

        private void ToggleGCSetting(string[] consoleText) {
            if (!DisableGC) {
                DisableGC = true;
                if (GC_Manager.load_mono_gc()) {
                    ToggleHooksAndGC(DisableGC);
                    if (SystemInfo.systemMemorySize < 8196) { ETGModConsole.Log("[TheGarbageCollector] Warning: Your computer was detected as having 8GB or less ram. It is recommended only to use this feature on machines with more then 8GB of ram!"); }
                    ETGModConsole.Log("[TheGarbageCollector] Automatic GC disabled.\nNow will only do collections during floor loads and if player is AFK or been in pause menu for more then 30 seconds!");
                }
                PlayerPrefs.SetInt(GarbageCollectorToggleName, 0);
            } else {
                DisableGC = false;
                ToggleHooksAndGC(DisableGC);
                ETGModConsole.Log("[TheGarbageCollector] Automatic GC enabled.\nUnity's GC will now run normally!");
                PlayerPrefs.SetInt(GarbageCollectorToggleName, 1);
            }
            PlayerPrefs.Save();
        }

        private void ToggleGCStats(string[] consoleText) {
            if (GCStats.Instance.ShowGcData) {
                GCStats.Instance.ShowGcData = false;
                ETGModConsole.Log(ModNameInRed + "GC Stats disabled!");
            } else {
                GCStats.Instance.ShowGcData = true;
                ETGModConsole.Log(ModNameInRed + "GC Stats enabled!");
            }
        }

        private void DoACollect(string[] consoleText) {
            ETGModConsole.Log("[TheGarbageCollector] Doing a Collection");
            if (GameManager.Instance && GameManager.Instance.PrimaryPlayer) {
                AkSoundEngine.PostEvent("Play_TrashMan_01", GameManager.Instance.PrimaryPlayer.gameObject);
            } else {
                AkSoundEngine.PostEvent("Play_TrashMan_01", ETGModMainBehaviour.Instance.gameObject);
            }
            if (DisableGC) { GC_Manager.Instance.DoCollect(); } else { BraveMemory.DoCollect(); }
        }
        
        private void SetMemoryCap(string[] consoleText) {
            if (consoleText != null && consoleText.Length == 1) {
                int MemorySize = int.Parse(consoleText[0]);
                bool MemoryCapWasReset = false;
                if (MemorySize < 1024) {
                    MemorySize = GetBestMemoryCap;
                    MemoryCapWasReset = true;
                }
                if (MemorySize >= 8196) { disableMonitor = true; } else { disableMonitor = false; }
                GC_Manager.MemoryGrowthAllowence = MemorySize;
                PlayerPrefs.SetInt(GarbageCollectorMemoryCap, MemorySize);
                PlayerPrefs.Save();
                if (MemoryCapWasReset) {
                    ETGModConsole.Log(ModNameInRed + "Memory allocation limit manually set. To allow this to be auto set again, set it to a value below 1024!");
                } else {
                    ETGModConsole.Log(ModNameInRed + "Requested memory cap is below minimum allowed value.\nMemory cap is now set to Auto based on your current system memory spec.");
                }
            } else {
                ETGModConsole.Log(ModNameInRed + "No memory value specified. Please specify a memory limit you want to use!");
            }
        }

        public static void ToggleHooksAndGC(bool state) {
            if (state) {
                GC_Manager.Instance.Enable();
                if (BraveMemoryCollectHook == null) {
                    BraveMemoryCollectHook = new Hook(
                        typeof(BraveMemory).GetMethod("DoCollect", BindingFlags.Public | BindingFlags.Static),
                        typeof(TheGarbageCollector).GetMethod("DoCollect", BindingFlags.Public | BindingFlags.Static)
                    );
                }
                if (clearLevelDataHook == null) {
                    clearLevelDataHook = new Hook(
                        typeof(GameManager).GetMethod("ClearPerLevelData", BindingFlags.Public | BindingFlags.Instance),
                        typeof(TheGarbageCollector).GetMethod("ClearPerLevelDataHook", BindingFlags.Public | BindingFlags.Instance),
                        typeof(GameManager)
                    );
                }
                GameManager.Instance.OnNewLevelFullyLoaded += OnLevelFullyLoaded;
            } else {
                GC_Manager.Instance.Disable();
                if (BraveMemoryCollectHook != null) { BraveMemoryCollectHook.Dispose(); BraveMemoryCollectHook = null; }
                if (clearLevelDataHook != null) { clearLevelDataHook.Dispose(); clearLevelDataHook = null; }
                if (gameManagerHook != null) { gameManagerHook.Dispose(); gameManagerHook = null; }
                GameManager.Instance.OnNewLevelFullyLoaded -= OnLevelFullyLoaded;
            }
        }

        public void ClearPerLevelDataHook(Action<GameManager> orig, GameManager self) {
            orig(self);
            if (DisableGC && GC_Manager.d_gc_disabled && GC_Manager.load_mono_gc()) { GC_Manager.Instance.DoCollect(); }
        }

        public static void DoCollect() {
            if (GC_Manager.d_gc_disabled && !GameManager.Instance.IsLoadingLevel) { return; }
            if (GC_Manager.d_gc_disabled) {
                GC_Manager.Instance.DoCollect();
            } else {
                GC_Manager.LastGcTime.SetValue(typeof(BraveMemory), Time.realtimeSinceStartup);
                GC.Collect();
            }
        }
    }
}

