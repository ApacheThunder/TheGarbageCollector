using Dungeonator;
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;

namespace TheGarbageCollector {

    // Do not use this in mods other then ExpandTheGungeon to prevent conflicting GC setups!
    // If you do you must find a way to detect if GC is disabled by another mod!
    
    public class GC_Manager : MonoBehaviour {

        public static GC_Manager Instance {
            get {
                if (!TheGarbageCollector.GCManagerObject?.GetComponent<GC_Manager>()) {
                    if (!TheGarbageCollector.GCManagerObject) {
                        TheGarbageCollector.GCManagerObject = new GameObject("TheGarbageCollector");
                        DontDestroyOnLoad(TheGarbageCollector.GCManagerObject);
                    }
                }
                return TheGarbageCollector.GCManagerObject.GetOrAddComponent<GC_Manager>();
            }
        }

        

        public GC_Manager() {
            turn_off_mono_gc = false;

            LastInCombatTime = 0;
            LastUnpausedTime = 0;
            DoManualCollection = false;
            CanDoCollectionNow = false;

            PauseTimeTillManualCollection = 120; // 2 minutes
            OutOfCombatTimeTillManualCollection = 300; // 5 minutes

            AllocatedMemorySinceLastCollection = -1;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);
        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        public static FieldInfo LastGcTime = typeof(BraveMemory).GetField("LastGcTime", BindingFlags.Static | BindingFlags.NonPublic);
        private static FieldInfo m_stringBuilder = typeof(HUDGC).GetField("stringBuilder", BindingFlags.Instance | BindingFlags.NonPublic);
        
        // Extracted from mono.pdb using dbh.exe (using the "enum *!*mono_gc_*" command)
        // Note: for the 64 bit editor, there is only a 64 bit version of mono.pdb, so you need to also download the 32 bit editor to update this for 32 bit standalone builds
        // (you also need to decide which version of the dll to use; this can be done by comparing the mono_gc_collect offset with the two offsets for the 32 bit and 64 bit dlls)
        // Apache - Updated offsets for the version of Mono Enter The Gungeon uses
        public static int offset_mono_gc_disable = 0x1b310;
        public static int offset_mono_gc_enable = 0x1b318;
        public static int offset_mono_gc_collect = 0x1b2c4;

        public static int MemoryGrowthAllowence = 2048;

        public long AllocatedMemorySinceLastCollection;

        public static long GetTotalMemoryAllocatedInMB {
            get { return (GC.GetTotalMemory(false) / 1024 / 1024); }
        }

        public static Action mono_gc_disable;
        public static Action mono_gc_enable;
        public static Action mono_gc_collect;

        public static bool load_mono_gc() {

            if (mono_gc_loaded) { return true; }
            
            IntPtr mono_module = GetModuleHandle("mono.dll");
            IntPtr func_ptr_mono_gc_collect = new IntPtr(mono_module.ToInt64() + offset_mono_gc_collect);
            IntPtr expected_func_ptr_mono_gc_collect = GetProcAddress(mono_module, "mono_gc_collect");
            if (func_ptr_mono_gc_collect != expected_func_ptr_mono_gc_collect) {
                //if you see this error, you need to update the "offset_mono_gc_" variables defined near the top of this class.
                ETGModConsole.Log("[TheGarbageCollector] Cannot load GarbageCollector functions. Expected mono's collect at " + func_ptr_mono_gc_collect.ToInt64() + " Actual at " + func_ptr_mono_gc_collect.ToInt64() + " Module root " + mono_module.ToInt64(), true);
                return false;
            }

            mono_gc_enable = (Action)Marshal.GetDelegateForFunctionPointer(new IntPtr(mono_module.ToInt64() + offset_mono_gc_enable), typeof(Action));
            mono_gc_disable = (Action)Marshal.GetDelegateForFunctionPointer(new IntPtr(mono_module.ToInt64() + offset_mono_gc_disable), typeof(Action));
            mono_gc_collect = (Action)Marshal.GetDelegateForFunctionPointer(new IntPtr(mono_module.ToInt64() + offset_mono_gc_collect), typeof(Action));

            mono_gc_loaded = true;
            return true;
        }

        public static bool d_gc_disabled = false;
        private static bool mono_gc_loaded = false;

        //set this to true to have the GC be manually invoked by this script when certain thresholds are reached
        public bool turn_off_mono_gc;
        public bool CanDoCollectionNow;
        private bool DoManualCollection;
        
        

        public float PauseTimeTillManualCollection;
        public float OutOfCombatTimeTillManualCollection;
        public float LastUnpausedTime;
        public float LastInCombatTime;
        public float LastCollectionTime;

        private float AllocationLimit;

        private int[] dummy_object;
        
        public void Disable() {
            mono_gc_enable();
            d_gc_disabled = false;
            turn_off_mono_gc = false;
        }

        public void Enable() {
            mono_gc_disable();
            d_gc_disabled = true;
            turn_off_mono_gc = true;
        }
        
        public void DoCollect() {
            if (d_gc_disabled) {
                Collect();
            } else {
                LastGcTime.SetValue(typeof(BraveMemory), Time.realtimeSinceStartup);
                GC.Collect();
            }
        }
        
        private void Collect() {
            if (GCStats.Instance.ShowGcData && !Dungeon.IsGenerating && (GameManager.Instance && !GameManager.Instance.IsLoadingLevel)) {
                AkSoundEngine.PostEvent("Play_TrashMan_01", ETGModMainBehaviour.Instance.gameObject);
            }
                        
            int collection_count = ProfileUtils.GetMonoCollectionCount();

            mono_gc_enable();

            // see if gc will run on its own after being enabled
            for (int x = 0; x < 100; ++x) { dummy_object = new int[1]; dummy_object[0] = 0; }

            if (ProfileUtils.GetMonoCollectionCount() == collection_count) {
                LastGcTime.SetValue(null, Time.realtimeSinceStartup); // Currently only HUDGC checks this but will include it to ensure HUDGC has accurate stats.
                LastCollectionTime = Time.realtimeSinceStartup;
                GC.Collect(); // if not, run it manually
            }
            mono_gc_disable();

            AllocatedMemorySinceLastCollection = GetTotalMemoryAllocatedInMB;

            if (GCStats.Instance.ShowGcData && !Dungeon.IsGenerating && (GameManager.Instance && !GameManager.Instance.IsLoadingLevel)) {
                if (GCStats.Instance.stringBuilder != null) {
                    Debug.Log("[TheGarbageCollector] Collection ran. Last GC Stats:");
                    Debug.Log(GCStats.Instance.stringBuilder);
                }
            }
        }

        private void Start() { }
                
        protected void Update() {
            if (!d_gc_disabled | Dungeon.IsGenerating | (GameManager.Instance && GameManager.Instance.IsLoadingLevel)) { return; }

            // Do manual if game is left paused for more then a specific time set via PauseTimeTillManualCollection
            // Doing a collection during pause screen has minimal impact on gameplay and is also to help keep memory usage from growing too much if player is AFK.
            if (GameManager.Instance.IsPaused) {
                if (LastUnpausedTime > 0.1f && (Time.realtimeSinceStartup - LastUnpausedTime) > PauseTimeTillManualCollection) {
                    DoManualCollection = true;
                    LastUnpausedTime = Time.realtimeSinceStartup;
                }
            } else {
                DoManualCollection = false;
                LastUnpausedTime = Time.realtimeSinceStartup;
            }

            // If player has been out of combat for awhile (specific time set on OutOfCombatTimeTillManualCollection), then run manual collection.
            // This is to detect if player is AFK and didn't pause the game
            // It is reletively unlikely a player would spend more then 5 minutes out of combat on any specific floor)
            // (and even if they were this collection wouldn't occur while they are in combat so it has less of a negative effect on gameplay)
            if (GameManager.Instance.PrimaryPlayer && !GameManager.Instance.IsPaused) {
                if (GameManager.Instance.PrimaryPlayer.IsInCombat) {
                    LastInCombatTime = Time.realtimeSinceStartup;
                    DoManualCollection = false;
                } else {
                    if (LastInCombatTime > 0 && ((Time.realtimeSinceStartup - LastInCombatTime) > OutOfCombatTimeTillManualCollection)) {
                        DoManualCollection = true;
                        LastInCombatTime = Time.realtimeSinceStartup;
                    } else {
                        DoManualCollection = false;
                    }
                }
            }
            if (GameManager.Instance?.PrimaryPlayer && GameManager.Instance.PrimaryPlayer.IsInCombat) {
                CanDoCollectionNow = false;
            } else {
                CanDoCollectionNow = true;
            }

            AllocationLimit = MemoryGrowthAllowence;
                
            if (AllocatedMemorySinceLastCollection != -1) { AllocationLimit = Mathf.Max(AllocationLimit, AllocatedMemorySinceLastCollection); }

            if (DoManualCollection | (!TheGarbageCollector.disableMonitor && CanDoCollectionNow && (GetTotalMemoryAllocatedInMB >= AllocationLimit))) {
                Collect();
                DoManualCollection = false;
                CanDoCollectionNow = false;
            }
        }
    }
}

