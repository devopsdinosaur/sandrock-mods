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


[BepInPlugin("devopsdinosaur.sunhaven.testing", "Testing", "0.0.1")]
public class ActionSpeedPlugin : BaseUnityPlugin {

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
}