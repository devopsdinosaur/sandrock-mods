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
using Pathea.GuildRanking;

[BepInPlugin("devopsdinosaur.sandrock.testing", "Testing", "0.0.1")]
public class TestingPlugin : BaseUnityPlugin {

	private const int MAX_STACK = 999999;

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

	[HarmonyPatch(typeof(Player), "SetJetPackActive")]
	class HarmonyPatch_Player_SetJetPackActive {

		private static bool Prefix(ref bool cheatMod) {
			cheatMod = true;
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

	private static Dictionary<int, int> m_loved_gifts = new Dictionary<int, int>();

	[HarmonyPatch(typeof(SendGiftUtils), "DoSendGiftResult")]
	class HarmonyPatch_SendGiftUtils_DoGiftResult {

		private static bool Prefix(int npcId, ref int itemId, ref GradeType gradeType) {
			WishItemData wish_data = Module<DynamicWishModule>.Self.GetWishItemData(npcId);
			if (wish_data != null) {
				itemId = wish_data.itemId;
			} else {
				if (!m_loved_gifts.ContainsKey(npcId)) {
					SendGiftData send_data = SendGiftData.Get(npcId);
					foreach (GiftItemData item_data in GiftItemData.GetAll()) {
						if (SendGiftUtils.AnyTagID(send_data.TagIdExcellent, item_data.tagA)) {
							m_loved_gifts[npcId] = item_data.id;
							break;
						}
					}
				}
				if (m_loved_gifts.ContainsKey(npcId)) {
					itemId = m_loved_gifts[npcId];
				}
			}
			gradeType = GradeType.None;
			return true;
		}
	}

	[HarmonyPatch(typeof(InteractiveOption), "CanInteractive")]
	class HarmonyPatch_InteractiveOption_CanInteractive {

		private static bool Prefix(ref bool __result) {
			__result = true;
			return false;
		}
	}

	[HarmonyPatch(typeof(InteractiveAction_MotionAnim), "OnUpdate")]
	class HarmonyPatch_InteractiveAction_MotionAnimLoop_OnUpdate {

		private static bool Prefix(ref InteractiveResult __result) {
			__result = InteractiveResult.Success;
			return false;
		}
	}

	[HarmonyPatch(typeof(InteractiveAction_MotionAnimLoop), "OnInputEsc")]
	class HarmonyPatch_InteractiveAction_MotionAnimLoop_OnInputEsc {

		private static bool Prefix(ref bool ___isEsc) {
			___isEsc = true;
			return false;
		}
	}

    [HarmonyPatch(typeof(WorldLauncher), "Awake")]
    class HarmonyPatch_WorldLauncher_Awake {

        private static bool Prefix(WorldLauncher __instance) {
            PluginUpdater.create(__instance.gameObject);
            PluginUpdater.Instance.register("bulldozer_update", 1, testing_update);
            return true;
        }
    }

	private static void testing_update() {

	}

	public static Dictionary<int, ItemPrototype> m_item_prototypes = null;
	public static Dictionary<int, ItemInstance> m_item_instances = null;
	public static Dictionary<int, SellProductBaseData> m_sell_items = null;
	public static Dictionary<int, Store> m_stores = null;
    public static Dictionary<int, Npc> m_npcs = null;

    [HarmonyPatch(typeof(ItemPrototype), "Init")]
    class HarmonyPatch_ItemPrototype_Init {

        private static bool Prefix(ItemPrototype __instance) {
			try {
				if (m_item_prototypes == null) {
					m_item_prototypes = new Dictionary<int, ItemPrototype>();
				}
				if (m_item_instances == null) {
					m_item_instances = new Dictionary<int, ItemInstance>();
				}
				m_item_prototypes[__instance.id] = __instance;
				__instance.stackNumber = MAX_STACK;
				logger.LogInfo($"ItemPrototype.Init - name: {TextMgr.GetStr(__instance.nameId)}, id: {__instance.id}");
			} catch (Exception e) {
                logger.LogError("** HarmonyPatch_ItemPrototype_Init.Prefix ERROR - " + e);
            }
            return true;
        }
    }

	[HarmonyPatch(typeof(SellProductBaseData), "LoadData")]
    class HarmonyPatch_SellProductBaseData_LoadData {

        private static void Postfix() {
            try {
                if (m_sell_items == null) {
                    m_sell_items = new Dictionary<int, SellProductBaseData>();
                }
                foreach (SellProductBaseData data in SellProductBaseData.refData) {
					m_sell_items[data.id] = data;
					//logger.LogInfo($"id: {data.itemId}, price: {data.price}");
				}
            } catch (Exception e) {
                logger.LogError("** HarmonyPatch_ItemPrototype_Init.Prefix ERROR - " + e);
            }
        }
    }

    [HarmonyPatch(typeof(Store), "InitRecordData")]
    class HarmonyPatch_Store_InitRecordData {

        private static void Postfix(Store __instance) {
			try {
				if (m_stores == null) {
					m_stores = new Dictionary<int, Store>();
				}
				m_stores[__instance.id] = __instance;
				//logger.LogInfo($"Store - id: {__instance.id}, name: {__instance.Name}");
			} catch (Exception e) {
				logger.LogError("** HarmonyPatch_Store_GenProduct.Prefix ERROR - " + e);
			}
        }
    }

	[HarmonyPatch(typeof(Store), "FetchSlot")]
    class HarmonyPatch_Store_FetchSlot {

        private static bool Prefix(Store __instance, List<ItemSlot> ___fetchSlots) {
            try {
                /*
				__instance.ClearSlot();
				foreach (SellProductBaseData data in m_sell_items.Values) {
					try {
						SellProductItem product = new SellProductItem(
							Module<ItemInstance.Module>.Self.CreateAsDefault(data.itemId),
							data.price,
							data.currency
						);
						logger.LogInfo(product);
						___fetchSlots.Add(product);
					} catch (Exception e) {
						logger.LogError(e);
					}
					//fetchSlots.Add(product);
				}
                return false;
				*/
				if (!m_enabled.Value || __instance.id != 2) {
					return true;
				}
				__instance.groupProducts = new List<GroupProductItem>();
				__instance.singleProducts = new List<SellProduct>();
				foreach (int id in m_item_prototypes.Keys) {
					if (!m_sell_items.ContainsKey(id)) {
						m_sell_items
					}
				}
				
				foreach (SellProductBaseData data in m_sell_items.Values) {
					SellProduct product = new SellProduct(data, MAX_STACK, 0, __instance);
					product.sellProductItem.Add(new SellProductItem(
						Module<ItemInstance.Module>.Self.Create(data.itemId, MAX_STACK, GradeType.Max, true),
						data.price,
						data.currency
					));
					__instance.singleProducts.Add(product);
				}
				return true;
            } catch (Exception e) {
                logger.LogError("** HarmonyPatch_Store_FetchSlot.Prefix ERROR - " + e);
            }
			return true;
        }
    }

	[HarmonyPatch(typeof(ItemPrototypeModule), "Get")]
	class HarmonyPatch_ItemPrototypeModule_Get {

		private static bool Prefix(int id, ref ItemPrototype __result) {
			try {
				__result = m_item_prototypes[id];
				__result.stackNumber = MAX_STACK;
				return false;
			} catch (Exception e) {
				logger.LogError("** HarmonyPatch_ItemPrototypeModule_Get.Prefix ERROR - " + e);
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Npc), "CreatActor")]
    class HarmonyPatch_Npc_CreateActor {

        private static bool Prefix(Npc __instance) {
			try {
				if (m_npcs == null) {
					m_npcs = new Dictionary<int, Npc>();
				}
				m_npcs[__instance.id] = __instance;
			} catch (Exception e) {
                logger.LogError("** HarmonyPatch_Npc_CreateActor.Prefix ERROR - " + e);
            }
            return true;
        }
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
}