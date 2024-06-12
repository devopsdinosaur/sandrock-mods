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
using Pathea.FrameworkNs;
using Pathea.EquipmentNs;
using Pathea.ActionNs;
using UnityEngine.SceneManagement;
using Pathea.MonsterNs;
using Pathea.HatredNs;
using Pathea.DynamicWishNs;
using Pathea.ItemContainers;
using Pathea.NpcNs;
using Pathea.InteractiveNs;
using Pathea.UISystemV2.Grid;
using Pathea.CutsceneNs;
using Pathea.DesignerConfig;

[BepInPlugin("devopsdinosaur.sandrock.testing", "Testing", "0.0.1")]
public class TestingPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sandrock.testing");
	public static ManualLogSource logger;
	private static ConfigEntry<bool> m_enabled;

	private void Awake() {
		logger = this.Logger;
		try {
			m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
			if (m_enabled.Value) {
				this.m_harmony.PatchAll();
			}
			logger.LogInfo("devopsdinosaur.sandrock.testing v0.0.1" + (m_enabled.Value ? "" : " [inactive; disabled in config]") + " loaded.");
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

    private class PluginUpdater:MonoBehaviour {

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
                elapsed = frequency,
                action = action
            });
        }

        public void Update() {
            foreach (UpdateInfo info in m_actions) {
                if ((info.elapsed += Time.deltaTime) >= info.frequency) {
                    info.elapsed = 0f;
                    try {
                        info.action();
                    } catch (Exception e) {
                        logger.LogError($"PluginUpdater.Update.{info.name} Exception - {e.ToString()}");
                    }
                }
            }
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

	[HarmonyPatch(typeof(Player), "SetJetPackActive")]
	class HarmonyPatch_Player_SetJetPackActive {

		private static bool Prefix(ref bool cheatMod) {
			cheatMod = true;
			return true;
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

	[HarmonyPatch(typeof(Actor), "ApplyFallGroundedDamage")]
	class HarmonyPatch_Actor_ApplyFallGroundedDamage {

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

    [HarmonyPatch(typeof(WorldLauncher), "Awake")]
    class HarmonyPatch_WorldLauncher_Awake {

        private static bool Prefix(WorldLauncher __instance) {
            PluginUpdater.create(__instance.gameObject);
            PluginUpdater.Instance.register("testing_update", 1, testing_update);
            return true;
        }
    }

	private static void testing_update() {
		
	}

    [HarmonyPatch(typeof(StartMenuUI), "Init")]
    class HarmonyPatch_StartMenuUI_Init {

        private static void Postfix(StartMenuUI __instance, bool ___inited) {
            try {
				if (!___inited) {
					return;
				}
                __instance.Invoke("ResumeGame", 0.5f);
            } catch (Exception e) {
                logger.LogError("** HarmonyPatch_StartMenuUI_Init.Postfix ERROR - " + e);
            }
        }
    }

    [HarmonyPatch(typeof(Cutscene), "Start")]
    class HarmonyPatch_Cutscene_Start {

        private static void Postfix(Cutscene __instance) {
            try {
				if (__instance.name.StartsWith("CG_Start_")) {
					GameObject.Destroy(__instance.gameObject);
				}
            } catch (Exception e) {
                logger.LogError("** HarmonyPatch_Cutscene_Start.Prefix ERROR - " + e);
            }
        }
    }

    [HarmonyPatch(typeof(EscMenuUI), "InitMenu")]
    class HarmonyPatch_EscMenuUI_InitMenu {

        private static bool Prefix(EscMenuUI __instance, ref string[] menuName) {
            try {
				string[] originalMenuName = menuName;
				menuName = new string[originalMenuName.Length + 1];
				for (int index = 0; index < originalMenuName.Length; index++) {
					menuName[index] = originalMenuName[index];
				}
				menuName[originalMenuName.Length] = "Quit Application";
            } catch (Exception e) {
                logger.LogError("** HarmonyPatch_EscMenuUI_InitMenu.Prefix ERROR - " + e);
            }
			return true;
        }
    }

    [HarmonyPatch(typeof(EscMenuUI), "Button_onClick")]
    class HarmonyPatch_EscMenuUI_Button_onClick {

        private static bool Prefix(EscMenuUI __instance, GridElement element) {
            try {
                if (element.index < __instance.buttons.Count - 1) {
					return true;
				}
				Application.Quit();
				return false;
            } catch (Exception e) {
                logger.LogError("** HarmonyPatch_EscMenuUI_InitMenu.Prefix ERROR - " + e);
            }
            return true;
        }
    }

	private static Dictionary<int, Npc> m_npcs = new Dictionary<int, Npc>();
	private static Dictionary<int, NpcSuitInfo> m_suits = new Dictionary<int, NpcSuitInfo>();
	private static Dictionary<string, List<int>> m_suit_map = new Dictionary<string, List<int>>();

	[HarmonyPatch(typeof(Npc), "CreatActor")]
	class HarmonyPatch_Npc_CreatActor {

		private static void Postfix(Npc __instance) {
			try {
				m_npcs[__instance.id] = __instance;
				logger.LogInfo($"Npc - id: {__instance.id}, name: {__instance.NpcName}");
			} catch (Exception e) {
				logger.LogError("** HarmonyPatch_Npc_CreatActor.Postfix ERROR - " + e);
			}
		}
	}

	[HarmonyPatch(typeof(NpcReplaceSuitModule), "OnLoad")]
	class HarmonyPatch_NpcReplaceSuitModule_OnLoad {

		private static void Postfix(ConfigReaderId<NpcSuitInfo> ___npcSuitInfoProtos) {
			try {
				foreach (NpcSuitInfo info in ___npcSuitInfoProtos) {
					m_suits[info.id] = info;
					logger.LogInfo($"NpcSuit - suitId: {info.Id}, npcId: {info.npcID}");
				}
			} catch (Exception e) {
				logger.LogError("** HarmonyPatch_NpcReplaceSuitModule_OnLoad.Postfix ERROR - " + e);
			}
		}
	}

	[HarmonyPatch(typeof(Store), "FetchSlot")]
	class HarmonyPatch_Store_FetchSlot {

		private static void Postfix() {
			try {
				foreach (Npc npc in m_npcs.Values) {
					List<int> suits = new List<int>();
					foreach (NpcSuitInfo info in m_suits.Values) {
						if (info.npcID == npc.id) {
							suits.Add(info.id);
						}
					}
					if (suits.Count > 0) {
						m_suit_map[npc.NpcName] = suits;
						logger.LogInfo($"{npc.NpcName} - {string.Join(",", suits)}");
					}
				}
				Module<NpcReplaceSuitModule>.Self.NpcRelpaceSuit(33);
			} catch (Exception e) {
				logger.LogError("** HarmonyPatch_Store_FetchSlot.Postfix ERROR - " + e);
			}
		}
	}
}