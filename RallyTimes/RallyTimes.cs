using HutongGames.PlayMaker;
using MSCLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
// -mscloader-devmode
/*
 * 
 * 
 * 
      echo .
      echo "== Running post build actions for configuration $(ConfigurationName)! ==="

      IF "$(ConfigurationName)" == "Release" (
      IF NOT "$(MSCMODSFOLDER)" == "NONE" (
      copy "$(TargetPath)" "$(MSCMODSFOLDER)" /y
      echo  "Copied dll into MSC mods folder!"        
      )
      IF NOT "$(MWCMODSFOLDER)" == "NONE" (
      copy "$(TargetPath)" "$(MWCMODSFOLDER)" /y
      echo  "Copied dll into MWC mods folder!"        
      )
      )
      IF "$(ConfigurationName)" == "Debug" (
      IF NOT "$(MSCMODSFOLDER)" == "NONE" (        
      copy "$(TargetPath)" "$(MSCMODSFOLDER)" /y
      copy "$(TargetDir)$(TargetName).pdb" "$(MSCMODSFOLDER)" /y
      echo  "Copied pdb file into mods folder!"
      cd "$(MSCMODSFOLDER)"
      IF exist "$(MSCMODSFOLDER)\debug.bat" (
      call "$(MSCMODSFOLDER)\debug.bat"
      ) ELSE (
      echo "debug.bat not found in "$(MSCMODSFOLDER)". You probably dont have MSCLoader debugging enabled!"
      )
      )
      IF NOT "$(MWCMODSFOLDER)" == "NONE" (        
      copy "$(TargetPath)" "$(MWCMODSFOLDER)" /y
      copy "$(TargetDir)$(TargetName).pdb" "$(MWCMODSFOLDER)" /y
      echo  "Copied pdb file into mods folder!"
      cd "$(MWCMODSFOLDER)"
      IF exist "$(MWCMODSFOLDER)\debug.bat" (
      call "$(MWCMODSFOLDER)\debug.bat"
      ) ELSE (
      echo "debug.bat not found in "$(MWCMODSFOLDER)". You probably dont have MSCLoader debugging enabled!"
      )
      )      
      )

      echo "=== Done! ==="
    */

namespace RallyTimes
{
    // GUI Component to display rally times on screen


    class RallyDay
    {
        readonly string identifier;
        readonly string timingObjPath;
        public GameObject timingObj;
        public PlayMakerFSM timingFsm;
        public PlayMakerFSM clockFsm;
        public Dictionary<string, FsmFloat> sectorTimeVariables = [];

        public FsmFloat totalTimeFsmFloat;
        public FsmFloat penaltyFsmFloat;
        public int numOfSectors;

        public bool isSetup = false;

        public RallyDay(string newTimingObjPath, string identifier, int numOfSectors = 4)
        {
            this.identifier = identifier;
            this.numOfSectors = numOfSectors;
            timingObjPath = newTimingObjPath;
            Init();
        }

        public bool Init()
        {
            try
            {
                timingObj = GameObject.Find(timingObjPath);
                if (timingObj == null)
                {
                    isSetup = false;
                    return false;
                }
                timingFsm = timingObj.GetPlayMaker("Timing");
                if (!timingFsm)
                {
                    isSetup = false;
                    return false;
                }
                clockFsm = timingObj.GetPlayMaker("Clock");
                if (!clockFsm)
                {
                    isSetup = false;
                    return false;
                }
                totalTimeFsmFloat = clockFsm.GetVariable<FsmFloat>("TimeTotal");
                penaltyFsmFloat = clockFsm.GetVariable<FsmFloat>("Penalty");
                for (int i = 1; i <= numOfSectors; i++)
                {
                    string sector = $"Sector{i:00}";
                    var sectorVariable = timingFsm.GetVariable<FsmFloat>(sector);


                    if (sectorVariable != null)
                    {
                        sectorTimeVariables[sector] = sectorVariable;
                    }
                }
                isSetup = true;
                return true;
            }
            catch (Exception e)
            {
                ModConsole.Error(e.Message);
                ModConsole.Error(e.StackTrace);
                return false;
            }
        }

        public float TotalTime
        {
            get
            {
                var value = totalTimeFsmFloat.Value;
                if (value > 0 && value != 4000)
                {
                    return value;
                }
                return 0;
            }
        }

        public Dictionary<string, float> SectorTimes
        {
            get
            {
                Dictionary<string, float> sectorTimes = [];
                foreach (var sector in sectorTimeVariables)
                {
                    var sectorTime = sector.Value.Value;
                    if (sectorTime != 0)
                    {
                        sectorTimes[sector.Key] = sectorTime;
                    }
                }
                return sectorTimes;
            }
        }
        public float Penalty
        {
            get
            {
                return penaltyFsmFloat.Value;
            }
        }
        public string ActiveStateName
        {
            get
            {
                if (!this.IsActive())
                {
                    return null;
                }
                return timingFsm.ActiveStateName;
            }
           
        }

        public bool IsActive()
        {
            return timingObj.activeSelf;
        }

    }

    class RallyDayBestTimeSettings
    {
        public Dictionary<string, SettingsTextBox> bestSectorTimesSettings = [];
        public SettingsTextBox bestTotalTimeSettings;

        public string identifier;
        public int numOfSectors = 4;

        public RallyDayBestTimeSettings(string identifier, int numOfSectors = 4)
        {
            this.identifier = identifier;
            this.numOfSectors = numOfSectors;
            Settings.AddHeader($"{identifier} Best Times");
            Settings.CreateGroup();
            bestTotalTimeSettings = Settings.AddTextBox(GetSettingIdentifier("totalTime"), "Total", "0", string.Empty, InputField.ContentType.DecimalNumber);
            for (int i = 1; i <= numOfSectors; i++)
            {
                string sector = $"Sector{i:00}";
                string settingId = GetSettingIdentifier(sector);
                bestSectorTimesSettings[settingId] = Settings.AddTextBox(settingId, sector, "0", string.Empty, InputField.ContentType.DecimalNumber);
            }
            Settings.EndGroup();
        }
        private string GetSettingIdentifier(string settingName) => $"{identifier}.{settingName}";
        public void SaveBestTime(Dictionary<string, float> sectorTimes, float totalTime)
        {
            if (totalTime <= 0)
            {
                return;
            }

            bestTotalTimeSettings.SetValue(totalTime.ToString("0.000"));

            foreach (var sector in sectorTimes)
            {

                if (sector.Value > 0)
                {
                    var sectorSetting = bestSectorTimesSettings[GetSettingIdentifier(sector.Key)];
                    sectorSetting.SetValue(sector.Value.ToString("0.000"));
                }
            }
        }

        public Dictionary<string, float> BestSectorTimes
        {
            get
            {
                Dictionary<string, float> bestSectorTimes = [];
                for (int i = 1; i <= numOfSectors; i++)
                {
                    string sector = $"Sector{i:00}";
                    string settingId = GetSettingIdentifier(sector);
                    var settingValue = bestSectorTimesSettings[settingId].GetValue();
                    bestSectorTimes[sector] = (float)Convert.ToDouble(settingValue);
                }
                return bestSectorTimes;
            }
        }

        public float BestTotalTime
        {
            get
            {
                return (float)Convert.ToDouble(bestTotalTimeSettings.GetValue());
            }
        }

    }

    public class RallyTimesGUI : MonoBehaviour
    {
        public Dictionary<string, float> ss1SectorTimes = [];
        public Dictionary<string, float> ss2SectorTimes = [];
        public Dictionary<string, float> ss3SectorTimes = [];
        public float ss1PenaltyTime = 0;
        public float ss2PenaltyTime = 0;
        public float ss3PenaltyTime = 0;
        public float ss1TotalTime = 0;
        public float ss2TotalTime = 0;
        public float ss3TotalTime = 0;
        private GUIStyle labelStyle;
        private GUIStyle positiveLabelStyle;
        private GUIStyle negativeLabelStyle;
        private GUIStyle boxStyle;
        private bool stylesInitialized = false;
        public bool isVisible = false;

        public Dictionary<string, float> ss1BestSectorTimes = [];
        public Dictionary<string, float> ss2BestSectorTimes = [];
        public Dictionary<string, float> ss3BestSectorTimes = [];
 
        public float ss1TotalTimeBest;
        public float ss2TotalTimeBest;
        public float ss3TotalTimeBest;


        public string ss1ActiveState = null;
        public string ss2ActiveState = null;
        public string ss3ActiveState = null;

        private readonly Dictionary<string, string> MappedCheckpointNames = new()
        {
            {"Sector01", "Checkpoint 1" },
            {"Sector02", "Checkpoint 2" },
            {"Sector03", "Checkpoint 3" },
            {"Sector04", "Checkpoint 4" },
            {"Sector05", "Checkpoint 5" },
            {"Sector06", "Checkpoint 6" }
        };


        public class LineData
        {
            public string text;
            public GUIStyle style;

        }

        public LineData InitLineData(string text, GUIStyle style)
        {
            return new LineData { text = text, style = style };
        }

        void Awake()
        {
            InitializeStyles();
        }

        void Start()
        {
            if (!stylesInitialized)
            {
                InitializeStyles();
            }
        }

        private void InitializeStyles()
        {
            try
            {
                labelStyle = new GUIStyle(GUI.skin.label);
                labelStyle.fontSize = 20;
                labelStyle.fontStyle = FontStyle.Bold;
                labelStyle.normal.textColor = Color.white;
                labelStyle.alignment = TextAnchor.UpperLeft;
                labelStyle.padding = new RectOffset(0, 0, 5, 5);
                labelStyle.margin = new RectOffset(0, 0, 0, 0);

                positiveLabelStyle = new GUIStyle(labelStyle);
                positiveLabelStyle.normal.textColor = Color.green;
                negativeLabelStyle = new GUIStyle(labelStyle);
                negativeLabelStyle.normal.textColor = Color.red;


                boxStyle = new GUIStyle(GUI.skin.box);
                boxStyle.normal.background = MakeTex(2, 2, new Color(0, 0, 0, 0.7f));
                boxStyle.stretchHeight = true;
                stylesInitialized = true;
            }
            catch (System.Exception)
            {
                //
            }
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();

            return result;
        }
        //GetPlayMaker 
        // "RACES/RALLY/Saturday/TimingSaturday"  Timing States: Checkpoint 1, Checkpoint 2, Checkpoint 3, Finish
        private string FormatTime(float time)
        {
            if (time <= 0) return "0:00.000";

            int minutes = (int)(time / 60);
            float seconds = time % 60;
            return $"{minutes}:{seconds:00.000}";
        }
        private const float padding = 10f;
        private const float boxWidth = 300f;
        private const float lineHeight = 30f;
        private const float textPadding = 5f;

        private LineData CreateComparisonLineData(float newTime, float bestTime)
        {
            float sectorTimeDiff = newTime - bestTime;
            string normalisedDiff = sectorTimeDiff.ToString("0.000");
            string text = sectorTimeDiff > 0 ? $"+{normalisedDiff}" : $"{normalisedDiff}";
            var style = sectorTimeDiff > 0 ? negativeLabelStyle : positiveLabelStyle;
            if (normalisedDiff == "0.000")
            {
                return null;
            }
            return InitLineData(text, style);
        }

        List<List<LineData>> CreateDayLineData(Dictionary<string, float> sectorData, Dictionary<string, float> bestSectorData, float totalTime, float bestTotalTime, float penalty, string header, string currentState)
        {
            List<List<LineData>> lines = [];

            var sortedSectors = new List<string>(sectorData.Keys);
            sortedSectors.Sort();
            lines.Add([InitLineData(header.ToUpper(), labelStyle)]);

            foreach (var sectorName in sortedSectors)
            {
                float sectorTime = sectorData[sectorName];
                string formattedSectorTime = FormatTime(sectorTime);
                float bestSectorTime = bestSectorData[sectorName];
                List<LineData> rows = [];
                rows.Add(InitLineData($"{sectorName}: {formattedSectorTime}", labelStyle));
                bool isCurrentSectorFinished = currentState == null || currentState != MappedCheckpointNames[sectorName];
                if (bestSectorTime > 0 && isCurrentSectorFinished)
                {
                    var comparisonData = CreateComparisonLineData(sectorTime, bestSectorTime);
                    if (comparisonData != null)
                    {
                        rows.Add(comparisonData);
                    }
                }
                lines.Add(rows);

            }
            if (penalty > 0)
            {
                string penaltyFormatted = FormatTime(penalty);
                lines.Add([InitLineData($"Penalty: {penaltyFormatted}", labelStyle)]);
            }
            if (totalTime > 0)
            {

                string totalFormatted = FormatTime(totalTime);
                List<LineData> rows = [];
                rows.Add(InitLineData($"Total: {totalFormatted}", labelStyle));
                bool shouldDisplayBestTotalTime = currentState == null || currentState == "Finish";
                if (bestTotalTime > 0 && shouldDisplayBestTotalTime)
                {
                    var comparisonData = CreateComparisonLineData(totalTime, bestTotalTime);
                    if (comparisonData != null)
                    {
                        rows.Add(comparisonData);
                    }
                }
                lines.Add(rows);
            }

            return lines;

        }

        public void Render()
        {
            if (!stylesInitialized)
            {
                InitializeStyles();
                if (!stylesInitialized)
                {
                    return;
                }
            }

            bool isValidSS1 = ss1SectorTimes.Count > 0 && ss1TotalTime > 0;
            bool isValidSS2 = ss2SectorTimes.Count > 0 && ss2TotalTime > 0;
            bool isValidSS3 = ss3SectorTimes.Count > 0 && ss3TotalTime > 0;

            if (!isValidSS1 && !isValidSS2 && !isValidSS3)
            {
                return;
            }




            float xPos = Screen.width - boxWidth - padding;
            float yPos = padding;



            List<List<LineData>> day1Lines = [];
            List<List<LineData>> day2Lines = [];
            if (ss1SectorTimes.Count > 0 || ss1TotalTime > 0)
            {
                day1Lines = CreateDayLineData(ss1SectorTimes, ss1BestSectorTimes, ss1TotalTime, ss1TotalTimeBest, ss1PenaltyTime, "SS1:", ss1ActiveState);
            }
            if (ss2SectorTimes.Count > 0 || ss2TotalTime > 0)
            {
                day1Lines = CreateDayLineData(ss2SectorTimes, ss2BestSectorTimes, ss2TotalTime, ss2TotalTimeBest, ss2PenaltyTime, "SS2:", ss2ActiveState);
            }
            if (ss3SectorTimes.Count > 0 || ss3TotalTime > 0)
            {
                day1Lines = CreateDayLineData(ss3SectorTimes, ss3BestSectorTimes, ss3TotalTime, ss3TotalTimeBest, ss3PenaltyTime, "ss3:", ss3ActiveState);
            }
            //if (ss2SectorTimes.Count > 0 || ss > 0)
            //{
            //    day2Lines = CreateDayLineData(sectorTimesDay2, bestSectorTimesDay2, day2TotalTime, day2TotalTimeBest, day2PenaltyTime, "Day 2:", sundayActiveState);
            //}
            List<List<LineData>> lines = [.. day1Lines, .. day2Lines];
            if (lines.Count() > 0)
            {
                float boxHeight = (labelStyle.lineHeight + textPadding * 2) * lines.Count();
                GUILayout.BeginArea(new Rect(xPos, yPos, boxWidth, boxHeight), boxStyle);
                GUILayout.BeginVertical(GUILayout.ExpandHeight(true));
                foreach (var lineData in lines)
                {
                    if(lineData != null && lineData.Count() > 0) { 
                    GUILayout.BeginHorizontal(GUILayout.Width(boxWidth), GUILayout.Height(lineHeight));
                    
                    foreach (var line in lineData)
                    {
                        GUILayout.Label(line.text, line.style, GUILayout.ExpandWidth(true));
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.Space(-2);
                    }

                }
                GUILayout.EndVertical();
                GUILayout.EndArea();
            }
        }
    }

    public class RallyTimes : Mod
    {

        public override string ID => "RallyTimes"; // Your (unique) mod ID 
        public override string Name => "RallyTimes"; // Your mod name
        public override string Author => "BORYSSEY"; // Name of the Author (your name)
        public override string Version => "1.2"; // Version
        public override string Description => "Display your rally total, section time and penalty"; // Short description of your mod 
        public override Game SupportedGames => Game.MyWinterCar;

        public static SettingsKeybind toggleVisibilityKeybind;

        
        readonly string ss1TimingPath = "RACES/RALLY/SS1/TimingSS1";
        readonly string ss2TimingPath = "RACES/RALLY/SS2/TimingSS2";
        readonly string ss3TimingPath = "RACES/RALLY/SS3/TimingSS3";

        private RallyTimesGUI guiComponent;
        private GameObject guiObject;

        private bool isModVisible = true;

        public static SettingsKeybind saveBestTimesKeybind;

        public bool isLeaderboardShown = false;


        //"RACES/RALLY/Saturday/TimingSaturday"
        // FsmBool
        // 
        // Timing.JumpStart
        // FsmFloat
        // Timing.Sector01
        // Timing.Sector02
        // Timing.Sector03
        // Timing.Sector04

        // FsmString
        // Timing.SectorTime01
        // Timing.SectorTime02
        // Timing.SectorTime03
        // Timing.SectorTime04
        // "RACES/RALLY/ResultsWeekend"

        //RallyTimePlayer
        //    RallyTimePlayer


        public override void ModSetup()
        {
            SetupFunction(Setup.OnLoad, Mod_OnLoad);
            SetupFunction(Setup.PostLoad, Mod_PostLoad);
            SetupFunction(Setup.Update, Mod_Update);
            SetupFunction(Setup.OnGUI, Mod_OnGui);
            SetupFunction(Setup.ModSettings, Mod_Settings);
        }


        SettingsKeybind showLeaderboard;

        RallyDay ss1Race, ss2Race, ss3Race;
        RallyDayBestTimeSettings ss1Settings, ss2Settings, ss3Settings;
        private void Mod_Settings()
        {
            Keybind.AddHeader("Rally Times Settings");

            toggleVisibilityKeybind = Keybind.Add("toggleRallyTimesVisibility", "Toggle Visibility", KeyCode.F8);
            saveBestTimesKeybind = Keybind.Add("saveRallyBestTimes", "Save current time as best", KeyCode.X);
            showLeaderboard = Keybind.Add("showLeaderboard", "Show leaderboard", KeyCode.F4);
            ss1Settings = new RallyDayBestTimeSettings("SS1");
            ss2Settings = new RallyDayBestTimeSettings("SS2", 6);
            ss3Settings = new RallyDayBestTimeSettings("SS3");
        }
        private bool isModSetup = false;

        private FsmInt day;

        private void Mod_OnLoad()
        {
            day = PlayMakerGlobals.Instance.Variables.GetFsmInt("GlobalDay");
            ss1Race = new RallyDay(ss1TimingPath, "SS1");
            ss2Race = new RallyDay(ss2TimingPath, "SS2", 6);
            ss3Race = new RallyDay(ss3TimingPath, "SS3");

            isModSetup = true;
        }
        private bool isRaceDay()
        {
            if (day == null) return false;
            if(day.Value == 6 ||  day.Value == 7) return true;
            return false;
            
        }


        private bool IsRaceActive() => ss1Race.IsActive() || ss3Race.IsActive();

        private void Mod_PostLoad()
        {
            // Create GameObject for GUI component
            guiObject = new GameObject("RallyTimesGUI");
            UnityEngine.Object.DontDestroyOnLoad(guiObject);
            guiComponent = guiObject.AddComponent<RallyTimesGUI>();
        }

        void Mod_OnGui()
        {
            if (!guiComponent || guiComponent == null || !isModVisible || ss1Race == null || ss3Race == null || ss2Race == null || !ss3Race.isSetup || !ss1Race.isSetup || !ss2Race.isSetup)
            {
                return;
            }
            guiComponent.ss1BestSectorTimes = ss1Settings.BestSectorTimes;
            guiComponent.ss1TotalTimeBest = ss1Settings.BestTotalTime;
            guiComponent.ss1ActiveState = ss1Race.ActiveStateName;


            guiComponent.ss2BestSectorTimes = ss2Settings.BestSectorTimes;
            guiComponent.ss2TotalTimeBest = ss2Settings.BestTotalTime;
            guiComponent.ss2ActiveState = ss2Race.ActiveStateName;

            
            guiComponent.ss3BestSectorTimes = ss3Settings.BestSectorTimes;
            guiComponent.ss3TotalTimeBest = ss3Settings.BestTotalTime;
            guiComponent.ss3ActiveState = ss3Race.ActiveStateName;


            guiComponent.Render();

        }

        ////GetPlayMaker 
        // "RACES/RALLY/Saturday/TimingSaturday"  Timing States: Checkpoint 1, Checkpoint 2, Checkpoint 3, Finish

        private void OpenResultsSheet()
        {
            var rallyResultsSheet = GameObject.Find("Sheets").transform.FindChild("RallyResults");
            if (rallyResultsSheet == null || rallyResultsSheet.gameObject.activeSelf)
            {
                return;
            }

            PlayMakerGlobals.Instance.Variables.GetFsmString("GUIinteraction").Value = "";
            PlayMakerGlobals.Instance.Variables.GetFsmBool("PlayerInMenu").Value = true;

            rallyResultsSheet?.gameObject.SetActive(true);
        }



        private void Mod_Update()
        {
            if (!isModSetup)
            {
                return;
            }
            if(!isRaceDay())
            {
                return;
            }
            if (toggleVisibilityKeybind.GetKeybindDown())
            {
                isModVisible = !isModVisible;
            }
            if (guiComponent == null)
            {
                return;
            }
            if (!isModVisible)
            {
                return;
            }

            if (showLeaderboard.GetKeybindDown())
            {
                OpenResultsSheet();
            }
            if(!ss1Race.isSetup)
            {
                var isSetup = ss1Race.Init();
                if(!isSetup)
                {
                    return;
                }
            }
            if (!ss2Race.isSetup)
            {
                var isSetup = ss2Race.Init();
                if (!isSetup)
                {
                    return;
                }
            }
            if (!ss3Race.isSetup)
            {
                var isSetup = ss3Race.Init();
                if (!isSetup)
                {
                    return;
                }
            }


           
            var shouldSaveCurrentTime = saveBestTimesKeybind.GetKeybindDown();
            if (shouldSaveCurrentTime)
            {
                if (ss1Race.TotalTime > 0)
                {
                    ss1Settings.SaveBestTime(ss1Race.SectorTimes, ss1Race.TotalTime);
                }
                if(ss2Race.TotalTime > 0)
                {
                    ss2Settings.SaveBestTime(ss2Race.SectorTimes, ss2Race.TotalTime);
                }
                if (ss3Race.TotalTime> 0)
                {
                    ss2Settings.SaveBestTime(ss3Race.SectorTimes, ss3Race.TotalTime);
                }
            }

            //if(!IsRaceActive())
            //{
            //    return;
            //}

            guiComponent.ss1SectorTimes = ss1Race.SectorTimes;
            guiComponent.ss1TotalTime = ss1Race.TotalTime;
            guiComponent.ss1PenaltyTime = ss1Race.Penalty;

            guiComponent.ss2SectorTimes = ss2Race.SectorTimes;
            guiComponent.ss2TotalTime = ss2Race.TotalTime;
            guiComponent.ss2PenaltyTime = ss2Race.Penalty;

            guiComponent.ss3SectorTimes = ss3Race.SectorTimes;
            guiComponent.ss3TotalTime = ss3Race.TotalTime;
            guiComponent.ss3PenaltyTime = ss3Race.Penalty;
        }
    }
}
