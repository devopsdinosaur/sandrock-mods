using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using Pathea;
using Pathea.TreasureRevealerNs;
using Pathea.AnimalCardFight;
using Pathea.UISystemV2.UIControl;
using Pathea.StoryScriptExt;
using Pathea.MachineNs;
using Pathea.StoreNs;
using Pathea.ItemNs;
using Pathea.MissionNs;
using Pathea.UISystemV2.UI;
using UnityExtensions;
using Pathea.SendGiftNs;
using Pathea.RandomDungeonNs;
using Pathea.DanceNs;
using Pathea.UseItemNs;
using Pathea.ActorNs;
using Pathea.HoldableNs;
using Pathea.VoxelAimNs;
using Pathea.ResourcePointNs;
using Pathea.Mtas;
using Pathea.TerrainTree;
/*
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using Pathea.ActionNs;
using Pathea.ActorDramaNs;
using Pathea.AffixNs;
using Pathea.AimSystemNs;
using Pathea.Attr;
using Pathea.Audios;
using Pathea.BattleFieldNs;
using Pathea.BehaviorNs;
using Pathea.CamCaptureNs;
using Pathea.CameraNs;
using Pathea.CatchNs;
using Pathea.ConversationNs;
using Pathea.CustomPlayer;
using Pathea.DramaNs;
using Pathea.EnvironmentNs;
using Pathea.EquipmentNs;
using Pathea.FactionNs;
using Pathea.FestivalNs;
using Pathea.FestivalNs.HidenSeek;
using Pathea.FootstepNs;
using Pathea.FovNs;
using Pathea.FrameworkNs;
using Pathea.FxNs;
using Pathea.GeneratorNs;
using Pathea.Gun;
using Pathea.HoldableNs;
using Pathea.HomeNs;
using Pathea.HomeViewerNs;
using Pathea.HotAreaNs;
using Pathea.InfoTip;
using Pathea.ItemAttrNs;
using Pathea.ItemContainers;
using Pathea.ItemFuncNs;
using Pathea.ItemNs;
using Pathea.MachineNs;
using Pathea.MapNs;
using Pathea.MountNs;
using Pathea.NearCameraFadeNs;
using Pathea.NpcAI;
using Pathea.OptionNs;
using Pathea.PatheaGDCNs;
using Pathea.Plants;
using Pathea.RepairNs;
using Pathea.RideNs;
using Pathea.ScenarioNs;
using Pathea.SceneInfoNs;
using Pathea.SkillNs;
using Pathea.SkillNs.SR;
using Pathea.StatisticsNs;
using Pathea.TakeThingNs;
using Pathea.TerrainTree;
using Pathea.TimeNs;
using Pathea.UseItemNs;
using UnityEngine;
using UtilNs;
*/
using Pathea.FrameworkNs;
using Pathea.EquipmentNs;

[BepInPlugin("devopsdinosaur.sunhaven.testing", "Testing", "0.0.1")]
public class TestingPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sunhaven.testing");
	public static ManualLogSource logger;
	private static ConfigEntry<bool> m_enabled;
	
	private void Awake() {
		logger = this.Logger;
		try {
			m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
			if (m_enabled.Value) {
				this.m_harmony.PatchAll();
			}
			logger.LogInfo("devopsdinosaur.sunhaven.testing v0.0.1" + (m_enabled.Value ? "" : " [inactive; disabled in config]") + " loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}
	}

	public static bool list_descendants(Transform parent, Func<Transform, bool> callback, int indent) {
		Transform child;
		string indent_string = "";
		for (int counter = 0; counter < indent; counter++) {
			indent_string += " => ";
		}
		for (int index = 0; index < parent.childCount; index++) {
			child = parent.GetChild(index);
			logger.LogInfo(indent_string + child.gameObject.name);
			if (callback != null) {
				if (callback(child) == false) {
					return false;
				}
			}
			list_descendants(child, callback, indent + 1);
		}
		return true;
	}

	public static bool enum_descendants(Transform parent, Func<Transform, bool> callback) {
		Transform child;
		for (int index = 0; index < parent.childCount; index++) {
			child = parent.GetChild(index);
			if (callback != null) {
				if (callback(child) == false) {
					return false;
				}
			}
			enum_descendants(child, callback);
		}
		return true;
	}

	public static void list_component_types(Transform obj) {
		foreach (Component component in obj.GetComponents<Component>()) {
			logger.LogInfo(component.GetType().ToString());
		}
	}

	public static void print_stack() {
		for (int index = 0; ; index++) {
			try {
				StackFrame frame = new StackFrame(index);
				logger.LogInfo($"StackFrame[{index}] - file: {frame.GetFileName()}, line: {frame.GetFileLineNumber()}, method: {frame.GetMethod().Name}");
			} catch {
				break;	
			}
		}
	}

	[HarmonyPatch(typeof(AnimalCardFightModule), "BeginGame")]
	class HarmonyPatch_AnimalCardFightModule_BeginGame {

		private static void Postfix(AnimalCardFightModule __instance) {
			try {
				__instance.GetType().GetField("fadorTotal", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(__instance, 13);
				Dictionary<int, AnimalCardFightData> animalCardFightDatas = (Dictionary<int, AnimalCardFightData>) __instance.GetType().GetField("animalCardFightDatas", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
				animalCardFightDatas[(int) __instance.GetType().GetField("npcid", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance)].timesOfDay = 3;
				__instance.GetType().GetField("animalCardFightDatas", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(__instance, animalCardFightDatas);
				__instance.GetType().GetProperty("IsPlayAgain", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).SetValue(__instance, false);
				__instance.EndGame();
			} catch (Exception e) {
				logger.LogError("** HarmonyPatch_AnimalCardFightModule_BeginGame.Postfix ERROR - " + e);
			}
		}
	}

	[HarmonyPatch(typeof(AnimalCardFightModule), "Deserialize")]
	class HarmonyPatch_AnimalCardFightModule_Deserialize {

		private static void Postfix(AnimalCardFightModule __instance) {
			__instance.GetType().GetField("lockNpcPlay", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(__instance, new List<int>());
		}
	}

	[HarmonyPatch(typeof(MapPartControl), "EnoughFavor")]
	class HarmonyPatch_MapPartControl_EnoughFavor {

		private static bool Prefix(MapPartControl __instance, ref bool __result) {
			__result = true;
			return false;
		}
	}

	[HarmonyPatch(typeof(TreasureRevealerManager), "DetectorUpdate")]
	class HarmonyPatch_TreasureRevealerManager_DetectorUpdate {

		static Type m_data_type = null;

		private static bool Prefix(TreasureRevealerManager __instance) {
			try {
				if (!__instance.IsActive) {
					return false;
				}
				if (m_data_type == null) {
					foreach (Type type in __instance.GetType().GetNestedTypes(BindingFlags.Instance | BindingFlags.NonPublic)) {
						if (type.Name == "Data") {
							m_data_type = type;
						}
					}
				}
				float activeTime = (float) __instance.GetType().GetField("activeTime", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
				object data = __instance.GetType().GetField("detectorData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
				m_data_type.GetField("durationSec").SetValue(data, 99999);
				__instance.GetType().GetField("detectorData", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(__instance, data);
				return true;
			} catch (Exception e) {
				logger.LogError("** HarmonyPatch_TreasureRevealerManager_DetectorUpdate ERROR - " + e);
			}
			return true;
		}
	}

    [HarmonyPatch(typeof(JetPack), "Start")]
    class HarmonyPatch_JetPack_Start {

        private static bool Prefix(JetPack __instance) {
            __instance.SetCostSpeed(0);
            return true;
        }
    }

    [HarmonyPatch(typeof(OnPlayerChopTreeFall), "Filter")]
	class HarmonyPatch_OnPlayerChopTreeFall_Filter {

		private static bool Prefix(ref bool __result) {
			__result = true;
			return false;
		}
	}

	[HarmonyPatch(typeof(MachineSupport), "UpdatePerMinuteCost")]
	class HarmonyPatch_MachineSupport_UpdatePerMinuteCost {

		private static bool Prefix() {
			GlobalModule.Self.GlobalMisc.seasonWaterRate = new float[GlobalModule.Self.GlobalMisc.seasonWaterRate.Length];
			for (int index = 0; index < GlobalModule.Self.GlobalMisc.seasonWaterRate.Length; index++) {
				GlobalModule.Self.GlobalMisc.seasonWaterRate[index] = 0;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Store), "IsOpen")]
	class HarmonyPatch_Store_IsOpen {

		private static bool Prefix(ref bool __result) {
			__result = true;
			return false;
		}
	}

	/*
	[HarmonyPatch(typeof(Store), "CanRecycle")]
	class HarmonyPatch_Store_CanRecycle {

		private static void Postfix(Store __instance, ItemInstance item, ref CannotRecycleReason cannotRecycleReason, ref bool __result) {
			logger.LogInfo($"HarmonyPatch_Store_CanRecycle(item: {item.GetName()})");
			if (cannotRecycleReason == CannotRecycleReason.StoreRefuse) {
				foreach (int tag in item.ItemTag) {
					__instance.data.recycle.AddItem(new IdFloat(tag, 99999));
				}
				cannotRecycleReason = CannotRecycleReason.None;
				__result = true;
			}
		}
	}
	*/

	[HarmonyPatch(typeof(LoadingMaskUI), "SetDisplay")]
	[HarmonyPatch(new Type[] {typeof(string), typeof(string), typeof(Sprite), typeof(string), typeof(bool)})]
	class HarmonyPatch_LoadingMaskUI_SetDisplay {

		private static bool Prefix(ref string hint, string sceneName, Sprite sprite, string path, bool useBg) {
			//logger.LogInfo($"hint: {hint}, sceneName: {sceneName}, sprite: {sprite}, path: {path}, useBg: {useBg}");
			hint = "Your mother was a hamster and your father smelled of elderberries!";
			return true;
		}
	}

	[HarmonyPatch(typeof(LoadingMaskForStart), "Awake")]
	class HarmonyPatch_LoadingMaskForStart_Awake {

		private static bool Prefix(Tween[] ___needResetTweens) {
			foreach (Tween tween in ___needResetTweens) {
				tween.duration = 0;
				logger.LogInfo($"tween - name: {tween.name}, duration: {tween.duration}");
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(AutoDoorCondition_Favor), "IsOk")]
	class HarmonyPatch_AutoDoorCondition_Favor_IsOk {

		private static bool Prefix(ref bool __result) {
			__result = true;
			return false;
		}
	}

    [HarmonyPatch(typeof(SendGiftModule), "GetRepeatGiftCount")]
    class HarmonyPatch_SendGiftModule_GetRepeatGiftCount {

        private static bool Prefix(ref int __result) {
            __result = 0;
            return false;
        }
    }

    [HarmonyPatch(typeof(MachineStatus))]
	[HarmonyPatch("IsSandBlock", MethodType.Getter)]
    class HarmonyPatch_IsSandBlock_Getter {

        private static bool Prefix(ref bool __result) {
            __result = false;
            return false;
        }
    }

	[HarmonyPatch(typeof(TrialDungeonModule), "InitDungeonData")]
	class HarmonyPatch_TrialDungeonModule_InitDungeonData {

		private static bool Prefix(ref bool jumpTime) {
			jumpTime = false;
			return true;
		}
	}

	[HarmonyPatch(typeof(Dancer), "AddScore")]
	class HarmonyPatch_Dancer_AddScore {

		private static bool Prefix(Dancer __instance, DanceRhythmData danceRhythmData) {
			if (__instance.actor.InstanceId != 8000) {
				return true;
			}
			danceRhythmData.danceRhythmLevelType = DanceRhythmLevelType.Perfect;
			return true;
		}
	}

    [HarmonyPatch(typeof(Player), "CheckTired")]
    class HarmonyPatch_Player_CheckTired {

        private static bool Prefix() {
            return false;
        }
    }

    [HarmonyPatch(typeof(VoxelTarget), "DoDig")]
	class HarmonyPatch_XXX_XXX {

		private static bool Prefix(ref float radius) {
			radius *= 2.0f;
			//foreach (CatchableResourcePoint catchable in Resources.FindObjectsOfTypeAll<CatchableResourcePoint>()) {
			//	catchable.GetType().GetMethod("DoGetItem", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(catchable, new object[] {true});
			//}
			return true;
		}
	}

	private class PluginUpdater : MonoBehaviour {
		
		private static PluginUpdater m_instance = null;
		public static PluginUpdater Instance {
			get { 
				return m_instance; 
			}
		}
		private class UpdateInfo {
			public string name;
			public float frequency;
			public float elapsed;
			public Action action;
		}
		private List<UpdateInfo> m_actions = new List<UpdateInfo>();

		public static PluginUpdater create(GameObject parent) {
			if (m_instance != null) {
				return m_instance;
			}
			return (m_instance = parent.AddComponent<PluginUpdater>());
        }

		public void register(string name, float frequency, Action action) {
			m_actions.Add(new UpdateInfo {
				name = name,
				frequency = frequency, 
				elapsed = 0f, 
				action = action
			});
		}
		
		public void Update() {
			foreach (UpdateInfo info in m_actions) {
				if ((info.elapsed += Time.deltaTime) >= info.frequency) {
                    info.elapsed = 0f;
					try {
						info.action();
					} catch(Exception e) {
						logger.LogError($"PluginUpdater.Update.{info.name} Exception - {e.ToString()}");
					}
				}
			}
		}
	}

    [HarmonyPatch(typeof(WorldLauncher), "Awake")]
    class HarmonyPatch_WorldLauncher_Awake {

        private static bool Prefix(WorldLauncher __instance) {
            PluginUpdater.create(__instance.gameObject);
			PluginUpdater.Instance.register("bulldozer_update", 0.5f, bulldozer_update);
            return true;
        }
    }

	public static void bulldozer_update() {
        ActorViewModel model = null;
		BaseData data = null;
        foreach (ActorViewModel _model in Resources.FindObjectsOfTypeAll<ActorViewModel>()) {
			if (_model == null || (data = (BaseData) _model.GetType().GetField("baseData", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(_model)) == null) {
				continue;
			}
			model = _model;
			break;
		}
		if (data == null) {
			return;
		}
		//logger.LogInfo(Module<Player>.Self.GamePos);
		return;
        foreach (CatchableResourcePoint catchable in Resources.FindObjectsOfTypeAll<CatchableResourcePoint>()) {
            float distance = Vector3.Distance(model.transform.position, catchable.transform.position);
			//logger.LogInfo($"distance to {catchable.name} == {distance}");
			if (distance > 5) {
				continue;
			}
            catchable.GetType().GetMethod("DoGetItem", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(catchable, new object[] { true });
        }
		foreach (TerrainTreeObject tree in Resources.FindObjectsOfTypeAll<TerrainTreeObject>()) {
            float distance = Vector3.Distance(model.transform.position, tree.transform.position);
            if (distance > 5) {
                continue;
            }
			logger.LogInfo(tree.name);
        }
    }

    [HarmonyPatch(typeof(Player), "Update")]
    class HarmonyPatch_Player_Update {

        private static bool Prefix(Player __instance) {
            if (__instance.actor == null) {
				return true;
			}

            return true;
        }
    }

    /*
    [HarmonyPatch(typeof(ActorViewModel), "Update")]
	class HarmonyPatch_XXX_XXXx {

		private static bool Prefix(ActorViewModel __instance) {
			if (__instance.actorID != 8000) {
				return true;
			}
			logger.LogInfo(__instance.transform.position);
			return true;
		}
    }
	*/
}