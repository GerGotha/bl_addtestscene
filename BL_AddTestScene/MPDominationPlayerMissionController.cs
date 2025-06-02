using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects;
using System.Runtime.InteropServices;
using System.Text;

namespace BL_AddTestScene;

public class MPDominationPlayerMissionController : MissionLogic
{
    private const float FlagArea = 4f;
    private const float ContestArea = 1.5f;

    private string _troop = "mp_light_cavalry_vlandia_hero";
    private List<GameEntity> _spawnpoints;
    private List<GameEntity> _spawnpointsTeam0; // Defender
    private List<GameEntity> _spawnpointsTeam1; // Attacker
    private List<GameEntity> _spawnzonesTeam0; // Defender
    private List<GameEntity> _spawnzonesTeam1; // Attacker

    private long? _timerstartTimestamp = null;
    private bool _renderFlagContestAreas = false;

    private readonly Game _game;
    private readonly List<GameEntity> _flags;
    private bool _endMission = false;

    // Import the messagebox function of user32.dll
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBox(IntPtr hWnd, string lpText, string lpCaption, uint uType);
    private const uint MB_OKCANCEL = 0x00000001;
    private const uint MB_ICONWARNING = 0x00000030;


    public MPDominationPlayerMissionController(string troop, BattleSideEnum side)
    {
        _game = Game.Current;
        _troop = troop;
        _spawnpoints = new List<GameEntity>();
        _spawnpointsTeam0 = new List<GameEntity>();
        _spawnpointsTeam1 = new List<GameEntity>();
        _spawnzonesTeam0 = new List<GameEntity>();
        _spawnzonesTeam1 = new List<GameEntity>();
        _flags = new List<GameEntity>();
        Debug.Print("MPDominationPlayerMissionController \n", 0, Debug.DebugColor.White, 17592186044416UL);
    }

    public override void AfterStart()
    {
        Debug.Print("MPDominationPlayerMissionController AfterStart\n", 0, Debug.DebugColor.White, 17592186044416UL);
        _spawnpoints = Mission.Current.Scene.FindEntitiesWithTag("spawnpoint").ToList();
        _spawnpointsTeam0 = _spawnpoints.Where((GameEntity sp) => sp.HasTag("defender")).ToList();
        _spawnpointsTeam1 = _spawnpoints.Where((GameEntity sp) => sp.HasTag("attacker")).ToList();

        _spawnzonesTeam0 = (from sz in _spawnpointsTeam0.Select((GameEntity sp) => sp.Parent).Distinct()
                            where sz != null
                            select sz).ToList();
        _spawnzonesTeam1 = (from sz in _spawnpointsTeam1.Select((GameEntity sp) => sp.Parent).Distinct()
                            where sz != null
                            select sz).ToList();


        // _flags = _spawnpoints.Where((GameEntity sp) => sp.Name.Contains("flag_pole_big_sergeant_")).ToList();

        var flagCapturePoints = new List<GameEntity>();
        Mission.Scene.GetAllEntitiesWithScriptComponent<FlagCapturePoint>(ref flagCapturePoints);

        foreach (var flagCapturePoint in flagCapturePoints)
        {
            GameEntity? gameEntity = flagCapturePoint.GetFirstChildEntityWithTag("score_stand");
            if (gameEntity != null)
            {
                _flags.Add(gameEntity);
            }
        }

        BasicCharacterObject troopObject = _game.ObjectManager.GetObject<BasicCharacterObject>(_troop);

        GameEntity overrideTroopClass = Mission.Current.Scene.FindEntityWithTag("enforce_troop_spawn");
        if (overrideTroopClass != null)
        {
            troopObject = _game.ObjectManager.GetObject<BasicCharacterObject>(overrideTroopClass.Name);
            if (troopObject != null)
            {
                _troop = overrideTroopClass.Name;
            }
        }

        var rnd = new Random();
        Color yellow = new(0.65f, 0.65f, 0f);
        InformationManager.DisplayMessage(new InformationMessage("\n"));
        InformationManager.DisplayMessage(new InformationMessage("\n"));
        InformationManager.DisplayMessage(new InformationMessage("\n"));
        InformationManager.DisplayMessage(new InformationMessage("\n"));
        InformationManager.DisplayMessage(new InformationMessage("\n"));
        InformationManager.DisplayMessage(new InformationMessage("-------------\n"));
        InformationManager.DisplayMessage(new InformationMessage("Press U to respawn in a respawn zone as Team 1 and Ctrl Left + U to respawn as Team 2. Press alt aswell to respawn in your initial spawn instead.\n", yellow));
        InformationManager.DisplayMessage(new InformationMessage("You can place an entity with the tag 'enforce_troop_spawn' and change the name to a class that is defined in the mpcharacters.xml to spawn with that troop instead (only in the editor of course..).\n"));

        GameEntity? overrideSpawnEntity = Mission.Current.Scene.FindEntityWithTag("sp_play") ?? Mission.Current.Scene.FindEntityWithTag("spawnpoint_player");
        SpawnAgent(overrideSpawnEntity ?? FindSpawnPoint((BattleSideEnum)rnd.Next((int)BattleSideEnum.Defender, (int)BattleSideEnum.NumSides), true));

        CheckAndShowSkirmishWarnings();

    }

    /// <summary>
    /// Adjusted SpawnAgent method to spawn the player agent at a specific spawn point.
    /// </summary>
    /// <param name="spawn"></param>
    private void SpawnAgent(GameEntity? spawn)
    {
        Debug.Print("MPDominationPlayerMissionController SpawnAgent\n", 0, Debug.DebugColor.White, 17592186044416UL);
        Agent myAgent = Mission.Current.MainAgent;
        if (myAgent != null)
        {
            if (myAgent.IsActive())
            {
                myAgent.FadeOut(true, true);
            }
        }

        MatrixFrame matrixFrame = spawn?.GetGlobalFrame() ?? MatrixFrame.Identity;
        BasicCharacterObject troopObject = _game.ObjectManager.GetObject<BasicCharacterObject>(_troop);
        AgentBuildData agentBuildData = new(new BasicBattleAgentOrigin(troopObject));
        AgentBuildData agentBuildData2 = agentBuildData.InitialPosition(in matrixFrame.origin);
        Vec2 direction = matrixFrame.rotation.f.AsVec2.Normalized();
        agentBuildData2.InitialDirection(in direction).Controller(Agent.ControllerType.Player);
        Mission.SpawnAgent(agentBuildData).WieldInitialWeapons();

    }

    /// <summary>
    /// Check scene for common errors which might lead to Skirmish not working.
    /// </summary>
    private void CheckAndShowSkirmishWarnings()
    {
        bool errorFound = false;
        Color yellow = new(0.95f, 0.9f, 0f);
        Color red = new(1.0f, 0.1f, 0.1f);
        Color green = new(0.1f, 1f, 0.1f);

        InformationManager.DisplayMessage(new InformationMessage(string.Empty));

        List<GameEntity> stonePileEntities = new();
        Mission.Scene.GetAllEntitiesWithScriptComponent<StonePile>(ref stonePileEntities);
        if (stonePileEntities.Count > 0)
        {
            StringBuilder sb = new();

            foreach (GameEntity sp in stonePileEntities)
            {
                sb.AppendLine(sp.Name);
            }

            string lpText = $"Object(s) with script component 'StonePile' found!\nThis might lead to an editor crash and ingame crash!\n\nName:\n{sb}\nPress 'Cancel' to return to the editor. Pressing 'OK' will most likely lead to a crash.";
            string lpCaption = "WARNING";

            int result = MessageBox(IntPtr.Zero, lpText, lpCaption, MB_OKCANCEL | MB_ICONWARNING);

            if (result != 1)
            {
                _endMission = true;

                string message = "> "+ lpText;
                InformationManager.DisplayMessage(new InformationMessage(message, yellow));
                MBEditor.AddEditorWarning(message);
                errorFound = true;
            }

        }

        if (_spawnzonesTeam0.Count() == 0)
        {
            string message = "> Team 0 (Defender) has no spawn areas at all!";
            InformationManager.DisplayMessage(new InformationMessage(message, yellow));
            MBEditor.AddEditorWarning(message);
            errorFound = true;
        }
        else
        {
            if (_spawnzonesTeam0.Where(sp => sp.HasTag("starting")).ToList().Count() == 0)
            {
                string message = "> Team 0 (Defender) has no initial spawn area!";
                InformationManager.DisplayMessage(new InformationMessage(message, yellow));
                MBEditor.AddEditorWarning(message);
                errorFound = true;
            }

            if (_spawnzonesTeam0.Where(sp => !sp.HasTag("starting")).ToList().Count() == 0)
            {
                string message = "> Team 0 (Defender) has no respawn area!";
                InformationManager.DisplayMessage(new InformationMessage(message, yellow));
                MBEditor.AddEditorWarning(message);
                errorFound = true;
            }
        }

        if (_spawnzonesTeam1.Count() == 0)
        {
            string message = "> Team 1 (Attacker) has no spawn areas at all!";
            InformationManager.DisplayMessage(new InformationMessage(message, yellow));
            MBEditor.AddEditorWarning(message);
            errorFound = true;
        }
        else
        {
            if (_spawnzonesTeam1.Where(sp => sp.HasTag("starting")).ToList().Count() == 0)
            {
                string message = "> Team 1 (Attacker) has no initial spawn area!";
                InformationManager.DisplayMessage(new InformationMessage(message, yellow));
                MBEditor.AddEditorWarning(message);
                errorFound = true;
            }

            if (_spawnzonesTeam1.Where(sp => !sp.HasTag("starting")).ToList().Count() == 0)
            {
                string message = "> Team 1 (Attacker) has no respawn area!";
                InformationManager.DisplayMessage(new InformationMessage(message, yellow));
                MBEditor.AddEditorWarning(message);
                errorFound = true;
            }
        }

        List<GameEntity> geList = new();
        Mission.Current.Scene.GetEntities(ref geList);
        List<GameEntity> aFlags = geList.Where(sp => sp.Name == "flag_pole_big_sergeant_A").ToList();
        List<GameEntity> bFlags = geList.Where(sp => sp.Name == "flag_pole_big_sergeant_B").ToList();
        List<GameEntity> cFlags = geList.Where(sp => sp.Name == "flag_pole_big_sergeant_C").ToList();
        List<GameEntity> borderSoft = geList.Where(sp => sp.Name == "border_soft").ToList();
        List<GameEntity> spawnVisual = geList.Where(sp => sp.Name == "spawn_visual").ToList();

        int counterTeam1 = 0;
        int counterTeam2 = 0;
        foreach (GameEntity startSpawns in _spawnzonesTeam0)
        {
            if (!startSpawns.HasTag("starting"))
            {
                continue;
            }

            if (startSpawns.ChildCount == 0)
            {
                string message = $"> {startSpawns.Name} does not have any child objects (spawn points with tags)";
                InformationManager.DisplayMessage(new InformationMessage(message, yellow));
                MBEditor.AddEditorWarning(message);
                continue;
            }

            if (startSpawns.GetChild(0).HasTag("defender"))
            {
                counterTeam1++;
            }
        }

        foreach (GameEntity startSpawns in _spawnzonesTeam1)
        {
            if (!startSpawns.HasTag("starting"))
            {
                continue;
            }

            if (startSpawns.ChildCount == 0)
            {
                string message = $"> {startSpawns.Name} does not have any child objects (spawn points with tags)";
                InformationManager.DisplayMessage(new InformationMessage(message, yellow));
                MBEditor.AddEditorWarning(message);
                continue;
            }

            if (startSpawns.GetChild(0).HasTag("attacker"))
            {
                counterTeam2++;
            }
        }

        if (counterTeam1 > 1)
        {
            string message = "> Team Defender has more than one start spawn!";
            InformationManager.DisplayMessage(new InformationMessage(message, yellow));
            MBEditor.AddEditorWarning(message);
            errorFound = true;
        }

        if (counterTeam2 > 1)
        {
            string message = "> Team Attacker has more than one start spawn!";
            InformationManager.DisplayMessage(new InformationMessage(message, yellow));
            MBEditor.AddEditorWarning(message);
            errorFound = true;
        }

        if (aFlags.Count == 0)
        {
            string message = "> Flag A is missing or misconfigured!";
            InformationManager.DisplayMessage(new InformationMessage(message, yellow));
            MBEditor.AddEditorWarning(message);
            errorFound = true;
        }

        if (bFlags.Count == 0)
        {
            string message = "> Flag B is missing or misconfigured!";
            InformationManager.DisplayMessage(new InformationMessage(message, yellow));
            MBEditor.AddEditorWarning(message);
            errorFound = true;
        }

        if (cFlags.Count == 0)
        {
            string message = "> Flag C is missing or misconfigured!";
            InformationManager.DisplayMessage(new InformationMessage(message, yellow));
            MBEditor.AddEditorWarning(message);
            errorFound = true;
        }

        if (aFlags.Count > 1)
        {
            string message = "> There is more than one A Flag!";
            InformationManager.DisplayMessage(new InformationMessage(message, yellow));
            MBEditor.AddEditorWarning(message);
            errorFound = true;
        }

        if (bFlags.Count > 1)
        {
            string message = "> There is more than one B Flag!";
            InformationManager.DisplayMessage(new InformationMessage(message, yellow));
            MBEditor.AddEditorWarning(message);
            errorFound = true;
        }

        if (cFlags.Count > 1)
        {
            string message = "> There is more than one C Flag!";
            InformationManager.DisplayMessage(new InformationMessage(message, yellow));
            MBEditor.AddEditorWarning(message);
            errorFound = true;
        }

        if (borderSoft.Count == 0)
        {
            string message = "> There are no border_soft placed on your scene!";
            InformationManager.DisplayMessage(new InformationMessage(message, yellow));
            MBEditor.AddEditorWarning(message);
            errorFound = true;
        }
        else if (borderSoft.Count < 3)
        {
            string message = "> You need at least three border_soft for your scene!";
            InformationManager.DisplayMessage(new InformationMessage(message, yellow));
            MBEditor.AddEditorWarning(message);
            errorFound = true;
        }

        if (spawnVisual.Count == 0)
        {
            string message = "> spawn_visual is missing!";
            InformationManager.DisplayMessage(new InformationMessage(message, yellow));
            MBEditor.AddEditorWarning(message);
            errorFound = true;
        }

        if (errorFound)
        {
            string finalMessage = "Fix all these errors, otherwise your map might crash in Skirmish!";
            InformationManager.DisplayMessage(new InformationMessage(finalMessage, red));
            MBEditor.AddEditorWarning(finalMessage);
        }
        else
        {
            InformationManager.DisplayMessage(new InformationMessage("No issues for Skirmish detected :-)", green));
        }

    }

    private GameEntity? FindSpawnPoint(BattleSideEnum side, bool isInitial = false)
    {
        // Debug.Print("MPVlandiaLightCavPlayerMissionController FindSpawnPoint\n", 0, Debug.DebugColor.White, 17592186044416UL);
        Random random = new();

        IEnumerable<GameEntity> targetSpawnzones = BattleSideEnum.Defender == side ? _spawnzonesTeam0 : _spawnzonesTeam1;
        if (targetSpawnzones.Count() == 0)
        {
            return null;
        }

        List<GameEntity> eligibleSpawnzones = targetSpawnzones.ToList();
        eligibleSpawnzones = targetSpawnzones.Where(sp => sp.HasTag("starting") == isInitial).ToList();
        int spawnZone = random.Next(eligibleSpawnzones.Count);
        int childNo = random.Next(eligibleSpawnzones[spawnZone].ChildCount);
        InformationManager.DisplayMessage(new InformationMessage($"Spawned in Spawnzone {spawnZone} {(isInitial ? "(Start spawn)" : string.Empty)} | Child: {childNo} | Team: {side} (Team {((int)side) + 1})"));

        return eligibleSpawnzones[spawnZone].GetChild(childNo);
    }

    public override void OnMissionTick(float dt)
    {
        base.OnMissionTick(dt);

        if (_endMission && (Mission.Scene?.IsLoadingFinished() ?? false))
        {
            _endMission = false;
            MBEditor.LeaveEditMissionMode();
            return;
        }

        if (_renderFlagContestAreas)
        {
            foreach(GameEntity flag in _flags)
            {
                DebugExtensions.RenderDebugCircleOnTerrain(Mission.Scene, flag.GetGlobalFrame(), FlagArea, 0xFFFF0000, false, false);
                DebugExtensions.RenderDebugCircleOnTerrain(Mission.Scene, flag.GetGlobalFrame(), FlagArea * ContestArea, 0xFF00FF00, false, false);
            }
        }

        OnKeyPressed();
    }

    private void OnKeyPressed()
    {
        try
        {
            BattleSideEnum side = Input.IsKeyDown(InputKey.LeftControl) ? BattleSideEnum.Defender : BattleSideEnum.Attacker;
            if (Input.IsKeyDown(InputKey.LeftAlt) && Input.IsKeyPressed(InputKey.U))
            {
                SpawnAgent(FindSpawnPoint(side, true));
            }
            else if (Input.IsKeyPressed(InputKey.U))
            {
                SpawnAgent(FindSpawnPoint(side));
            }
            else if (Input.IsKeyPressed(InputKey.P))
            {
                _renderFlagContestAreas = !_renderFlagContestAreas;
                Color yellow = new(0.95f, 0.9f, 0f);
                InformationManager.DisplayMessage(new InformationMessage($"Debug flag conquest areas were {(_renderFlagContestAreas ? "enabled" : "disabled")}!", yellow));
            }
            else if (Input.IsKeyPressed(InputKey.O))
            {
                long nowTimestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
                Color yellow = new(0.95f, 0.9f, 0f);
                if (_timerstartTimestamp == null)
                {
                    _timerstartTimestamp = nowTimestamp;
                    InformationManager.DisplayMessage(new InformationMessage($"Timer started! Press 'O' to stop the timer.", yellow));
                    return;
                }

                double timerDuration = Math.Max(1, Convert.ToDouble(nowTimestamp) - Convert.ToDouble(_timerstartTimestamp ?? 1d)) / 1000d;
                string outputDuration = string.Format("{0:0.00}", timerDuration);
                InformationManager.DisplayMessage(new InformationMessage($"Timer duration: {outputDuration} seconds.", yellow));
                _timerstartTimestamp = null;
            }

        }
        catch
        {
            InformationManager.DisplayMessage(new InformationMessage($"An error occured! Please end the test mode now!"));
        }
    }

    public override bool MissionEnded(ref MissionResult missionResult)
    {
        if (Mission.InputManager.IsGameKeyPressed((int)GameKeyDefinition.Leave))
        {
            return true;
        }

        return _endMission;
    }

}