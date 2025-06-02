using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace BL_AddTestScene;

public class SimpleMountedPlayerMissionController : MissionLogic
{
    private readonly Game _game;
    private long? _timerstartTimestamp = null;
    private string _troop = "aserai_tribal_horseman";

    public SimpleMountedPlayerMissionController()
    {
        _game = Game.Current;
    }

    public override void AfterStart()
    {
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

        BasicCharacterObject troopObject2 = _game.ObjectManager.GetObject<BasicCharacterObject>(_troop);
        
        MatrixFrame matrixFrame = (Mission.Current.Scene.FindEntityWithTag("sp_play")?.GetGlobalFrame() ?? Mission.Current.Scene.FindEntityWithTag("spawnpoint_player")?.GetGlobalFrame()) ?? MatrixFrame.Identity;

        AgentBuildData agentBuildData = new(new BasicBattleAgentOrigin(troopObject2));
        AgentBuildData agentBuildData2 = agentBuildData.InitialPosition(in matrixFrame.origin);
        Vec2 direction = matrixFrame.rotation.f.AsVec2.Normalized();
        agentBuildData2.InitialDirection(in direction).Controller(Agent.ControllerType.Player);
        Mission.SpawnAgent(agentBuildData).WieldInitialWeapons();
    }

    public override bool MissionEnded(ref MissionResult missionResult)
    {
        if (Mission.InputManager.IsGameKeyPressed((int)GameKeyDefinition.Leave))
        {
            return true;
        }

        return false;
    }

    public override void OnMissionTick(float dt)
    {
        base.OnMissionTick(dt);

        OnKeyPressed();
    }

    private void OnKeyPressed()
    {
        try
        {
            if (Input.IsKeyPressed(InputKey.O))
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
}
