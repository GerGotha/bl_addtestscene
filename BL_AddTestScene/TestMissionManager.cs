using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.Source.Missions;

namespace BL_AddTestScene;

[MissionManager]
public static class TestMissions
{

    [MissionMethod(UsableByEditor = true)]
    public static Mission MPSkirmishVlandiaLightCav(string scene, string sceneLevels)
    {
        return MissionState.OpenNew("MPSkirmishVlandiaLightCav", new MissionInitializerRecord(scene)
        {
            PlayingInCampaignMode = Campaign.Current.GameMode == CampaignGameMode.Campaign,
            AtmosphereOnCampaign = (Campaign.Current.GameMode == CampaignGameMode.Campaign) ? Campaign.Current.Models.MapWeatherModel.GetAtmosphereModel(MobileParty.MainParty.GetLogicalPosition()) : default,
            SceneLevels = sceneLevels
        }, (Mission missionController) => new MissionBehavior[]
        {
                new MissionOptionsComponent(),
                new MPDominationPlayerMissionController("mp_light_cavalry_vlandia_hero", BattleSideEnum.Defender),
                new EquipmentControllerLeaveLogic(),
                new AgentCommonAILogic(),
                new MissionAgentPanicHandler(),
        }, true, true);
    }

    [MissionMethod(UsableByEditor = true)]
    public static Mission MPSkirmishKhuzaitHeavyArcher(string scene, string sceneLevels)
    {
        return MissionState.OpenNew("MPSkirmishKhuzaitHeavyArcher", new MissionInitializerRecord(scene)
        {
            PlayingInCampaignMode = Campaign.Current.GameMode == CampaignGameMode.Campaign,
            AtmosphereOnCampaign = (Campaign.Current.GameMode == CampaignGameMode.Campaign) ? Campaign.Current.Models.MapWeatherModel.GetAtmosphereModel(MobileParty.MainParty.GetLogicalPosition()) : default,
            SceneLevels = sceneLevels
        }, (Mission missionController) => new MissionBehavior[]
        {
                new MissionOptionsComponent(),
                new MPDominationPlayerMissionController("mp_heavy_ranged_khuzait_hero", BattleSideEnum.Defender),
                new EquipmentControllerLeaveLogic(),
                new AgentCommonAILogic(),
                new MissionAgentPanicHandler(),
        }, true, true);
    }

    [MissionMethod(UsableByEditor = true)]
    public static Mission SimpleMountedPlayer(string scene, string sceneLevels)
    {
        return MissionState.OpenNew("SimpleMountedPlayerMissionController", new MissionInitializerRecord(scene)
        {
            PlayingInCampaignMode = Campaign.Current.GameMode == CampaignGameMode.Campaign,
            AtmosphereOnCampaign = (Campaign.Current.GameMode == CampaignGameMode.Campaign) ? Campaign.Current.Models.MapWeatherModel.GetAtmosphereModel(MobileParty.MainParty.GetLogicalPosition()) : default,
            SceneLevels = sceneLevels
        }, (Mission missionController) => new MissionBehavior[]
        {
                new MissionOptionsComponent(),
                new SimpleMountedPlayerMissionController(),
                new EquipmentControllerLeaveLogic(),
                new AgentCommonAILogic(),
                new MissionAgentPanicHandler(),
        }, true, true);
    }
}
