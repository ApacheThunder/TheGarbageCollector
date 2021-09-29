using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace TheGarbageCollector { 

    public class TheGarbageCollector : ETGModule {
                
        public static bool debugMode = false;
        public static bool DisableGC = true;
        public static bool disableMonitor = false;
        
        public static readonly string ConsoleCommandName = "garbagecollector";
        public static readonly string GarbageCollectorToggleName = "TheGarbageCollectorDisabled";
        
        public static string ZipFilePath;
        public static string FilePath;
        
        public override void Init() {
            ZipFilePath = Metadata.Archive;
            FilePath = Metadata.Directory;
        }
        
        public override void Start() {
            ETGModConsole.Commands.AddGroup(ConsoleCommandName, ConsoleInfo);
            ETGModConsole.Commands.GetGroup(ConsoleCommandName).AddUnit("toggle", ToggleGCSetting);
            ETGModConsole.Commands.GetGroup(ConsoleCommandName).AddUnit("collect", DoACollect);
            ETGModConsole.Commands.GetGroup(ConsoleCommandName).AddUnit("stats", ToggleGCStats);
            ETGModConsole.Commands.GetGroup(ConsoleCommandName).AddUnit("debug", ToggleDebugMode);
            AudioLoader.InitAudio();
            
            if (SystemInfo.systemMemorySize > 24576) {
                GC_Manager.manual_gc_bytes_threshold_mb = 8196;
                disableMonitor = true; // If user has obcene amount of ram, there is no need to do colletions during gameplay unless player is AFK/has game paused and when a floor is loading.
            } else if (SystemInfo.systemMemorySize > 12288) {
                GC_Manager.manual_gc_bytes_threshold_mb = 4096;
            } else if (SystemInfo.systemMemorySize > 8196) {
                GC_Manager.manual_gc_bytes_threshold_mb = 3072;
            }

            GameManager.Instance.StartCoroutine(WaitForFoyerLoad());
        }
        
        public static void OnLevelFullyLoaded() { if (DisableGC && GC_Manager.Instance && GC_Manager.d_gc_disabled) { GC_Manager.Instance.ForceCollect(); } }

        private static IEnumerator WaitForFoyerLoad() {
            while (Foyer.DoIntroSequence && Foyer.DoMainMenu) { yield return null; }
            if (PlayerPrefs.GetInt(GarbageCollectorToggleName) == 1) { DisableGC = false; }
            yield return null;
            if (DisableGC) {
                ETGModConsole.Log("[TheGarbageCollector] Unity's Garbage Collector is now disabled. Use command garbagecolletor toggle to run it back on.");
                GC_Manager.Instance.Init();
                yield return null;
                if (GC_Manager.load_mono_gc()) {
                    GC_Manager.Instance.ToggleHooksAndGC(true);
                    if (SystemInfo.systemMemorySize < 8196) { ETGModConsole.Log("[TheGarbageCollector] Warning: Your computer was detected as having 8GB or less ram. It is recommended only to disable GarbageColletor on machines with more then 8GB of ram!", true); }
                }
            } else {
                ETGModConsole.Log("[TheGarbageCollector] Unity's Garbage Collector currently active. Use command garbagecolletor toggle to enable TheGarbageCollector and disable Unity's GarbageCollector.");
            }
            
            if (GameManager.Instance) { GameManager.Instance.OnNewLevelFullyLoaded += OnLevelFullyLoaded; }

            yield break;
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
                    GC_Manager.Instance.ToggleHooksAndGC(DisableGC);
                    if (SystemInfo.systemMemorySize < 8196) { ETGModConsole.Log("[TheGarbageCollector] Warning: Your computer was detected as having 8GB or less ram. It is recommended only to use this feature on machines with more then 8GB of ram!"); }
                    ETGModConsole.Log("[TheGarbageCollector] Automatic GC disabled.\nNow will only do collections during floor loads and if player is AFK or been in pause menu for more then 30 seconds!");
                    AkSoundEngine.PostEvent("Play_TrashMan_01", ETGModMainBehaviour.Instance.gameObject);
                }
                PlayerPrefs.SetInt(GarbageCollectorToggleName, 0);
            } else {
                DisableGC = false;
                GC_Manager.Instance.ToggleHooksAndGC(DisableGC);
                ETGModConsole.Log("[TheGarbageCollector] Automatic GC enabled.\nUnity's GC will now run normally!");
                PlayerPrefs.SetInt(GarbageCollectorToggleName, 1);
            }
            PlayerPrefs.Save();
        }

        private void DoACollect(string[] consoleText) {
            ETGModConsole.Log("[TheGarbageCollector] Doing a Collection");
            if (GameManager.Instance && GameManager.Instance.PrimaryPlayer) {
                AkSoundEngine.PostEvent("Play_TrashMan_01", GameManager.Instance.PrimaryPlayer.gameObject);
            } else {
                AkSoundEngine.PostEvent("Play_TrashMan_01", ETGModMainBehaviour.Instance.gameObject);
            }
            if (DisableGC) { GC_Manager.Instance.ForceCollect(); } else { BraveMemory.DoCollect(); }
        }

        private void ToggleGCStats(string[] consoleText) {
            if (HUDGC.ShowGcData) {
                HUDGC.ShowGcData = false;
                ETGModConsole.Log("[TheGarbageCollector] GC Stats disabled!");
            } else {
                HUDGC.ShowGcData = true;
                ETGModConsole.Log("[TheGarbageCollector] GC Stats enabled!");
            }
        }

        private void ToggleDebugMode(string[] consoleText) {
            if (debugMode) {
                debugMode = false;
                ETGModConsole.Log("[TheGarbageCollector] GC DebugMode Stats disabled!");
            } else {
                debugMode = true;
                ETGModConsole.Log("[TheGarbageCollector] GC DebugMode Stats enabled!");
            }
        }
    
        public override void Exit() { }
    }
}

