using System.Text;
using UnityEngine;

namespace TheGarbageCollector {

    public class GCStats : MonoBehaviour {

        public static GCStats Instance {
            get {
                if(!GCStatsObject) {
                    GCStatsObject = new GameObject("GCStats") { layer = 14 };
                    DontDestroyOnLoad(GCStatsObject);
                }
                return GCStatsObject.GetOrAddComponent<GCStats>();
            }
        }
        
        public GCStats() {
            ShowGcData = false;
            UIPosition = new Vector3(-710f, 130, 0);

            RedColor = new Color32(190, 160, 0, 255);
            WhiteColor = new Color32(255, 255, 255, 255);
            IsRedText = false;
            WaitingForCollection = false;
        }
        
        public static GameObject GCStatsObject;
        
        public bool ShowGcData;
        public Vector3 UIPosition;
        public StringBuilder stringBuilder;


        private dfLabel m_label;
        private bool IsRedText;
        private bool WaitingForCollection;
        private Color32 WhiteColor;
        private Color32 RedColor;
        private float AllocationLimit;

        private void SetupLabel() {
            if (!GameUIRoot.Instance?.Manager) { return; }
            AssetBundle shared1 = ResourceManager.LoadAssetBundle("shared_auto_001");
            dfTiledSprite referenceLabel = shared1.LoadAsset<GameObject>("Weapon Skull Ammo FG").GetComponent<dfTiledSprite>();
            dfFont referenceFont = shared1.LoadAsset<GameObject>("04b03_df40").GetComponent<dfFont>();
            
            dfLabel m_NewLabel = GameUIRoot.Instance.Manager.AddControl<dfLabel>();
            m_NewLabel.Atlas = referenceLabel.Atlas;
            m_NewLabel.Font = referenceFont;
            m_NewLabel.Anchor = (dfAnchorStyle)6;
            m_NewLabel.IsEnabled = true;
            m_NewLabel.IsVisible = false;
            m_NewLabel.IsInteractive = true;
            m_NewLabel.Tooltip = string.Empty;
            m_NewLabel.Pivot = dfPivotPoint.BottomLeft;
            m_NewLabel.zindex = 29;
            m_NewLabel.Opacity = 0.5f;
            m_NewLabel.Color = Color.white;
            m_NewLabel.DisabledColor = new Color32(128, 128, 128, 255);
            m_NewLabel.Size = new Vector2(350, 120);
            m_NewLabel.MinimumSize = m_NewLabel.Size;
            m_NewLabel.MaximumSize = new Vector2(400, 240);
            m_NewLabel.ClipChildren = false;
            m_NewLabel.InverseClipChildren = false;
            m_NewLabel.TabIndex = -1;
            m_NewLabel.CanFocus = false;
            m_NewLabel.AutoFocus = false;
            m_NewLabel.IsLocalized = false;
            m_NewLabel.HotZoneScale = Vector2.one;
            m_NewLabel.AllowSignalEvents = true;
            m_NewLabel.PrecludeUpdateCycle = false;
            m_NewLabel.PerCharacterOffset = Vector2.zero;
            m_NewLabel.PreventFontChanges = true;
            m_NewLabel.BackgroundSprite = string.Empty;
            m_NewLabel.BackgroundColor = Color.white;
            m_NewLabel.AutoSize = true;
            m_NewLabel.AutoHeight = false;
            m_NewLabel.WordWrap = false;
            m_NewLabel.Text = "PLACEHOLDER";
            m_NewLabel.BottomColor = Color.white;
            m_NewLabel.TextAlignment = TextAlignment.Left;
            m_NewLabel.VerticalAlignment = dfVerticalAlignment.Top;
            m_NewLabel.TextScale = 0.5f;
            m_NewLabel.TextScaleMode = dfTextScaleMode.None;
            m_NewLabel.CharacterSpacing = 0;
            m_NewLabel.ColorizeSymbols = false;
            m_NewLabel.ProcessMarkup = false;
            m_NewLabel.Outline = false;
            m_NewLabel.OutlineSize = 0;
            m_NewLabel.ShowGradient = false;
            m_NewLabel.OutlineColor = Color.black;
            m_NewLabel.Shadow = false;
            m_NewLabel.ShadowColor = Color.black;
            m_NewLabel.ShadowOffset = new Vector2(1, -1);
            m_NewLabel.Padding = new RectOffset() { left = 0, right = 0, top = 0, bottom = 0 };
            m_NewLabel.TabSize = 48;
            m_NewLabel.MaintainJapaneseFont = false;
            m_NewLabel.MaintainKoreanFont = false;
            m_NewLabel.MaintainRussianFont = false;
            m_NewLabel.Position = UIPosition;

            m_label = m_NewLabel;
            
            referenceFont = null;
            referenceLabel = null;
            shared1 = null;
        }


        private void Start() {
            stringBuilder = new StringBuilder(500);
            AllocationLimit = GC_Manager.MemoryGrowthAllowence;            
        }
        
        protected void Update() {
            if (ShowGcData && GC_Manager.d_gc_disabled) {
            
                if (!m_label) { SetupLabel(); }

                if (!m_label) { return; }
                
                if (!m_label.IsVisible) { m_label.IsVisible = true; }

                if (GC_Manager.Instance.AllocatedMemorySinceLastCollection != -1) { AllocationLimit = Mathf.Max(AllocationLimit, GC_Manager.Instance.AllocatedMemorySinceLastCollection); }

                if (GC_Manager.GetTotalMemoryAllocatedInMB >= AllocationLimit) {
                    if (!WaitingForCollection) { WaitingForCollection = true; }
                    if (!IsRedText) {
                        m_label.Color = RedColor;
                        IsRedText = true;
                    }
                } else {
                    if (WaitingForCollection) { WaitingForCollection = false; }
                    if (IsRedText) {
                        m_label.Color = WhiteColor;
                        IsRedText = false;
                    }
                }
                
                stringBuilder.Length = 0;
                stringBuilder.AppendFormat("Current Allocation: {0:0} MB / {1:0} MB\n", GC_Manager.GetTotalMemoryAllocatedInMB, AllocationLimit);
                stringBuilder.AppendFormat("Current Heap Size: {0:0} MB\n", (ProfileUtils.GetMonoHeapSize() / 1024 / 1024));
                stringBuilder.AppendFormat("Time Since Last In Combat: {0: 00} sec\n", (Time.realtimeSinceStartup - GC_Manager.Instance.LastInCombatTime));
                if ((Time.realtimeSinceStartup - GC_Manager.Instance.LastUnpausedTime) > 0.1f) {
                    stringBuilder.AppendFormat("Time Spent In Pause Screen: {0: 00} sec\n", (Time.realtimeSinceStartup - GC_Manager.Instance.LastUnpausedTime));
                }
                stringBuilder.AppendFormat("Time Since Last Collection: {0: 00} sec\n", (Time.realtimeSinceStartup - GC_Manager.Instance.LastCollectionTime));
                stringBuilder.AppendFormat("Total Collections: {0}\n", ProfileUtils.GetMonoCollectionCount());
                if (WaitingForCollection) {
                    stringBuilder.Append("Waiting to do a Collection...\n");
                } else if (TheGarbageCollector.disableMonitor) {
                    stringBuilder.Append("GC Monitor is Disabled.\n");
                }

                m_label.Text = stringBuilder.ToString();
            } else if (!ShowGcData && GC_Manager.d_gc_disabled) {
                if (m_label && m_label.IsVisible) { m_label.IsVisible = false; }
            }
        }
    }
}


