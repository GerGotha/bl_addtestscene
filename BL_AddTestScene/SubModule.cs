using System.Collections.Generic;
using System.Collections.ObjectModel;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Engine.Screens;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;

namespace BL_AddTestScene;

public class SubModule : MBSubModuleBase
{

    static SubModule()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, args) => Debug.Print("[BL_AddTestScene]: "+args.ExceptionObject.ToString(), color: Debug.DebugColor.Red);
    }

    protected override void OnSubModuleLoad()
    {
        base.OnSubModuleLoad();
        Debug.Print("Loading \"BL_AddTestScene_Client\".");
    }

    protected override void OnApplicationTick(float dt)
    {

        double currentTime = Convert.ToDouble(new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds());
        Scene scene = MBEditor._editorScene;
        
        if (scene != null && MBEditor.IsEditModeOn && Mission.Current == null)
        {
            List<GameEntity> allEntities = new();
            Utilities.GetSelectedEntities(ref allEntities);
            if (allEntities.Count > 0)
            {
                if (Input.IsKeyDown(InputKey.LeftControl) && Input.IsKeyPressed(InputKey.M))
                {
                    foreach (GameEntity entity in allEntities)
                    {
                        // Anzahl der MetaMeshes holen
                        string prefabName = !string.IsNullOrEmpty(entity.GetPrefabName()) ? entity.GetPrefabName() : !string.IsNullOrEmpty(entity.GetOldPrefabName()) ? entity.GetOldPrefabName() : entity.Name;
                        GameEntity? copyFrom = GameEntity.Instantiate(scene, prefabName, false);
                        if(copyFrom == null)
                        {
                            InformationManager.DisplayMessage(new InformationMessage($"Could not find prefab with name {prefabName}."));
                            continue;
                        }

                        if (entity.HasScriptComponent("mesh_bender"))
                        {
                            InformationManager.DisplayMessage(new InformationMessage($"You need to remove the mesh bender and reload the scene before changing the LODs ({prefabName})."));
                            continue;
                        }

                        List<MetaMesh> copyFromMetaMeshList = new();
                        int amountOfMetaMeshes = copyFrom.GetComponentCount(GameEntity.ComponentType.MetaMesh);
                        for (int i = 0; i < amountOfMetaMeshes; i++)
                        {
                            MetaMesh metaMesh = copyFrom.GetMetaMesh(i);
                            copyFromMetaMeshList.Add(metaMesh.CreateCopy());
                        }

                        InformationManager.DisplayMessage(new InformationMessage($"Found {amountOfMetaMeshes} MetaMeshes."));

                        for (int i = 0; i < amountOfMetaMeshes; i++)
                        {
                            if (i >= copyFromMetaMeshList.Count)
                            {
                                continue;
                            }

                            MetaMesh originalMetaMesh = copyFromMetaMeshList[i];
                            MetaMesh currentMetaMesh = entity.GetMetaMesh(i);

                            if (originalMetaMesh == null || currentMetaMesh == null)
                            {
                                InformationManager.DisplayMessage(new InformationMessage($"MetaMesh {i} is null, skipping."));
                                continue;
                            }

                            InformationManager.DisplayMessage(new InformationMessage($"Processing MetaMesh {i} with {originalMetaMesh.MeshCount} meshes."));

                            // Sammle die LOD-Masken in aufsteigender Reihenfolge
                            SortedDictionary<int, List<(int, Mesh)>> originalLods = new();
                            SortedDictionary<int, List<(int, Mesh)>> currentLods = new();

                            for (int j = 0; j < originalMetaMesh.MeshCount; j++)
                            {
                                int lodMask = originalMetaMesh.GetLodMaskForMeshAtIndex(j);
                                Mesh mesh = originalMetaMesh.GetMeshAtIndex(j);

                                if (!originalLods.ContainsKey(lodMask))
                                {
                                    originalLods[lodMask] = new List<(int, Mesh)>();
                                }

                                originalLods[lodMask].Add((j, mesh));
                            }

                            for (int j = 0; j < currentMetaMesh.MeshCount; j++)
                            {
                                int lodMask = currentMetaMesh.GetLodMaskForMeshAtIndex(j);
                                Mesh mesh = currentMetaMesh.GetMeshAtIndex(j);

                                if (!currentLods.ContainsKey(lodMask))
                                {
                                    currentLods[lodMask] = new List<(int, Mesh)>();
                                }

                                currentLods[lodMask].Add((j, mesh));
                            }

                            // Iteriere über die LODs in aufsteigender Reihenfolge
                            foreach (var lod in originalLods.Keys)
                            {
                                if (!currentLods.ContainsKey(lod))
                                {
                                    continue;
                                }

                                List<(int, Mesh)> originalMeshes = originalLods[lod];
                                List<(int, Mesh)> currentMeshes = currentLods[lod];

                                for (int k = 0; k < originalMeshes.Count && k < currentMeshes.Count; k++)
                                {
                                    int index = originalMeshes[k].Item1;
                                    Mesh originalMesh = originalMeshes[k].Item2;
                                    Mesh currentMesh = currentMeshes[k].Item2;

                                    Material originalMaterial = originalMesh.GetMaterial();
                                    Material currentMaterial = currentMesh.GetMaterial();

                                    if (currentMaterial.Name != originalMaterial.Name || (currentMesh.Color != 0xFFFFFFFF || currentMesh.Color2 != 0xFFFFFFFF))
                                    {
                                        // Setze das Material in höheren LODs entsprechend
                                        foreach (var higherLod in currentLods.Keys.Where(h => h > lod))
                                        {
                                            foreach (var higherLodMeshData in currentLods[higherLod])
                                            {
                                                Mesh higherLodMesh = higherLodMeshData.Item2;

                                                int indexOfLod = currentLods[higherLod].IndexOf(higherLodMeshData);
                                                //if (currentMaterial.Name != originalMaterial.Name || higherLodMesh.GetMaterial().Name == originalMaterial.Name)
                                                InformationManager.DisplayMessage(new InformationMessage($"{originalLods[higherLod][indexOfLod].Item2.GetMaterial().Name} {originalMaterial.Name} {originalLods[higherLod][indexOfLod].Item2.GetMaterial().Name == originalMaterial.Name}."));

                                                if (higherLodMesh.GetMaterial().Name == originalMaterial.Name || (higherLodMesh.GetMaterial().Name == currentMaterial.Name && originalLods[higherLod][indexOfLod].Item2.GetMaterial().Name == originalMaterial.Name))
                                                {
                                                    higherLodMesh.SetMaterial(currentMaterial);
                                                    higherLodMesh.Color = currentMesh.Color;
                                                    higherLodMesh.Color2 = currentMesh.Color2;

                                                    InformationManager.DisplayMessage(new InformationMessage($"Updated material {currentMaterial.Name} in LOD Mask {higherLod} for MetaMesh {i}."));
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            InformationManager.DisplayMessage(new InformationMessage($"Finished processing MetaMesh {i}."));
                        }

                        InformationManager.DisplayMessage(new InformationMessage($"Replaced materials in all MetaMeshes!"));
                        copyFrom?.RemoveAllChildren();
                        copyFrom?.Remove(75);
                    }
                    // copyFrom?.Remove(0);

                }
            }
        }
    }
}


