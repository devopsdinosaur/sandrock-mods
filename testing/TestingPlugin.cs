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
using Pathea.TreasureRevealerNs;
using Pathea.AnimalCardFight;


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

	[HarmonyPatch(typeof(TreasureRevealerManager), "Deactive")]
	class HarmonyPatch_TreasureRevealerManager_Deactive {

		private static bool Prefix(TreasureRevealerManager __instance) {
			StackTrace stack_trace = new StackTrace();
			int index = 0;
			logger.LogInfo(Time.time);
			foreach (StackFrame frame in stack_trace.GetFrames()) {
				logger.LogInfo($"{index++}: {frame.GetMethod().Name}");
			}
			return true;
		}
	}


	[HarmonyPatch(typeof(TreasureRevealerManager), "DetectorUpdate")]
	class HarmonyPatch_TreasureRevealerManager_DetectorUpdate {

		static Type m_data_type = null;

		private static bool Prefix(
			Vector3 position,
			TreasureRevealerManager __instance,
			TreasureRevealerDetector ___detector,
			ref float ___currentScanRadius,
			List<ITreasureRevealerTarget> ___targets
		) {
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
					logger.LogInfo((m_data_type == null ? "null" : "not null"));
				}
				float activeTime = (float) __instance.GetType().GetField("activeTime", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
				object data = __instance.GetType().GetField("detectorData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
				m_data_type.GetField("durationSec").SetValue(data, 99999);
				__instance.GetType().GetField("detectorData", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(__instance, data);
				return true;
				/*
				float durationSec = (float) m_data_type.GetField("durationSec").GetValue(data);
				float radius = (float) m_data_type.GetField("radius").GetValue(data);
				float scanAddRadius = (float) m_data_type.GetField("scanAddRadius").GetValue(data);
				float num = activeTime + durationSec;
				
				logger.LogInfo($"durationSec: {durationSec}, num: {num}, Time.time: {Time.time}");

				return true;
				
				//if (Time.time > num)
				//{
				//	Deactive();
				//	return;
				//}
				float num2 = Time.time - activeTime;
				float num3 = radius / scanAddRadius;
				float num4 = durationSec - __instance.Setting.detectorLightDisappearSec;
				float num5;
				if (num2 > num4) {
					float fadeOut = Mathf.Clamp01((num2 - num4) / __instance.Setting.detectorLightDisappearSec);
					___detector.SetFadeOut(fadeOut);
					num5 = 1f;
				}
				else if (num2 > num3) {
					num5 = 1f;
				} else {
					num5 = Mathf.Clamp01(num2 / num3);
					___detector.Set(num5, radius);
				}
				___currentScanRadius = radius * num5;
				for (int i = 0; i < ___targets.Count; i++) {
					ITreasureRevealerTarget treasureRevealerTarget = ___targets[i];
					if (treasureRevealerTarget.IsValid && !treasureRevealerTarget.Detected && __instance.IsTargetInCurrentRange(treasureRevealerTarget)) {
						treasureRevealerTarget.Detected = true;
						
						//__instance.OnTargetDetected?.Invoke(treasureRevealerTarget);
					}
				}
				___detector.transform.position = position;
				return false;
				*/
			} catch (Exception e) {
				logger.LogError("** ERROR - " + e);
			}
			return true;
		}
	}

}