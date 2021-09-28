using System;
using System.Collections;
using System.Reflection;
using MonoMod.RuntimeDetour;
using UnityEngine;
using System.Collections.Generic;

namespace TheGarbageCollector { 

    public class TheGarbageCollector : ETGModule {
                
        public static bool debugMode = false;
        public static bool DisableGC = false;

        public static readonly bool enableSoundFX = false;

        public static readonly string ConsoleCommandName = "garbagecollector";
        public static readonly string GarbageCollectorToggleName = "GarbageCollectorToggle";
        public static readonly string GarbageCollectorDebugModeName = "GarbageCollectorDebugModeName";


        public override void Init() { }
        
        public override void Start() {
            ETGModConsole.Commands.AddGroup(ConsoleCommandName, ConsoleInfo);
            ETGModConsole.Commands.GetGroup(ConsoleCommandName).AddUnit("toggle", ToggleGCSetting);
            ETGModConsole.Commands.GetGroup(ConsoleCommandName).AddUnit("debugmode", ToggleGCStats);

            if (PlayerPrefs.GetInt(GarbageCollectorToggleName) == 1) { DisableGC = true; }
            if (PlayerPrefs.GetInt(GarbageCollectorDebugModeName) == 1) { debugMode = true; }

            if (DisableGC) {
                GC_Manager.Instance.turn_off_mono_gc = true;
                GC_Manager.Instance.Init();
                if (GC_Manager.load_mono_gc()) {
                    GC_Manager.Instance.ToggleHookAndGC();
                    if (debugMode) { HUDGC.ShowGcData = true; }
                    if (SystemInfo.systemMemorySize < 8196) { ETGModConsole.Log("[TheGarbageCollector] Warning: Your computer was detected as having 8GB or less ram. It is recommended only to disable GarbageColletor on machines with more then 8GB of ram!", true); }
                }
            }
            if (GameManager.Instance) { GameManager.Instance.OnNewLevelFullyLoaded += OnLevelFullyLoaded; }
        }
        
        public void OnLevelFullyLoaded() { if (DisableGC && GC_Manager.d_gc_disabled) { GC_Manager.DoCollect(); } }

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
                    GC_Manager.Instance.ToggleHookAndGC(true);
                    if (SystemInfo.systemMemorySize < 8196) { ETGModConsole.Log("[TheGarbageCollector] Warning: Your computer was detected as having 8GB or less ram. It is recommended only to use this feature on machines with more then 8GB of ram!"); }
                    ETGModConsole.Log("[ExpandTheGungeon] Automatic GC disabled.\nNow will only do collections during floor loads and if player is AFK or been in pause menu for more then 30 seconds!");
                }
                PlayerPrefs.SetInt(GarbageCollectorToggleName, 1);
                PlayerPrefs.Save();
            } else {
                DisableGC = false;
                GC_Manager.Instance.ToggleHookAndGC(false);
                ETGModConsole.Log("[TheGarbageCollector] Automatic GC enabled.\nUnity's GC will now run normally!");
                PlayerPrefs.SetInt(GarbageCollectorToggleName, 0);
                PlayerPrefs.Save();
            }
        }

        private void ToggleGCStats(string[] consoleText) {
            if (!debugMode) {
                HUDGC.ShowGcData = true;
                debugMode = true;
                ETGModConsole.Log("[TheGarbageCollector] GC Stats enabled!");
                PlayerPrefs.SetInt(GarbageCollectorDebugModeName, 1);
                PlayerPrefs.Save();
            } else {
                HUDGC.ShowGcData = false;
                ETGModConsole.Log("[TheGarbageCollector] GC Stats disabled!");
                PlayerPrefs.SetInt(GarbageCollectorDebugModeName, 0);
                PlayerPrefs.Save();
            }
        }
    
        public override void Exit() { }
    }
}

