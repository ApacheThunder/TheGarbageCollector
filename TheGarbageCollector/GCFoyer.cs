using UnityEngine;

namespace TheGarbageCollector {

    public class GCFoyer : BraveBehaviour {

        public GCFoyer() { m_State = State.PreFoyerCheck; }

        private enum State { PreFoyerCheck, EnableMod, Exit };
        private State m_State;

        public void Awake() { }
        public void Start() { }

        public void Update() {
            switch (m_State) {
                case State.PreFoyerCheck:
                    if (Foyer.DoIntroSequence && Foyer.DoMainMenu) { return; }
                    m_State = State.EnableMod;
                    return;
                case State.EnableMod:
                    int ManualMemoryCap = PlayerPrefs.GetInt(TheGarbageCollector.GarbageCollectorMemoryCap);

                    if (ManualMemoryCap >= 1024) {
                        GC_Manager.MemoryGrowthAllowence = ManualMemoryCap;
                        if (ManualMemoryCap >= 8196) { TheGarbageCollector.disableMonitor = true; }
                    } else {
                        GC_Manager.MemoryGrowthAllowence = TheGarbageCollector.GetBestMemoryCap;
                        // If user has obcene amount of ram, there is no need to do colletions during gameplay unless player is AFK/has game paused and when a floor is loading.
                        if (SystemInfo.systemMemorySize > 24576) { TheGarbageCollector.disableMonitor = true; }
                    }

                    if (PlayerPrefs.GetInt(TheGarbageCollector.GarbageCollectorToggleName) == 1) { TheGarbageCollector.DisableGC = false; return; }
                    if (TheGarbageCollector.DisableGC) {
                        ETGModConsole.Log(TheGarbageCollector.ModNameInRed + "Unity's Garbage Collector is now disabled. Use command garbagecolletor toggle to run it back on.");
                        if (GC_Manager.load_mono_gc()) {
                            TheGarbageCollector.ToggleHooksAndGC(true);
                            if (SystemInfo.systemMemorySize < 8196) { ETGModConsole.Log(TheGarbageCollector.ModNameInRed + "Warning: Your computer was detected as having 8GB or less ram. It is recommended only to disable GarbageColletor on machines with more then 8GB of ram!", true); }
                        }
                    } else {
                        ETGModConsole.Log(TheGarbageCollector.ModNameInRed + "Unity's Garbage Collector currently active. Use command garbagecolletor toggle to enable TheGarbageCollector and disable Unity's GarbageCollector.");
                    }
                    m_State = State.Exit;
                    return;
                case State.Exit:
                    if (gameObject) { Destroy(gameObject); }
                    return;
            }
        }

        protected override void OnDestroy() { base.OnDestroy(); }
    }
}

