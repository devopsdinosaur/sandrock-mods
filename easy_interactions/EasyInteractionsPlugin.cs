using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Reflection;
using System.Collections.Generic;
using Pathea;
using Pathea.AnimalCardFight;
using Pathea.UISystemV2.UIControl;
using Pathea.StoryScriptExt;
using Pathea.ItemNs;
using Pathea.SendGiftNs;
using Pathea.FrameworkNs;
using Pathea.DynamicWishNs;
using Pathea.InteractiveNs;
using Pathea.SocialNs;
using Pathea.ActorNs;
using Pathea.InteractionNs;
using Pathea.ConversationNs;

[BepInPlugin("devopsdinosaur.sandrock.easy_interactions", "Easy Interactions", "0.0.1")]
public class EasyInteractionsPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sandrock.easy_interactions");
	public static ManualLogSource logger;
	private static ConfigEntry<bool> m_enabled;
	private static ConfigEntry<bool> m_see_all_on_map;
	private static ConfigEntry<bool> m_quick_card_game;
	private static ConfigEntry<bool> m_no_gift_counting;
	private static ConfigEntry<bool> m_love_all_gifts;
	private static ConfigEntry<int> m_card_favor_value;
	private static ConfigEntry<bool> m_enable_all_interactives;
	private static ConfigEntry<bool> m_no_esc_warning;
	private static ConfigEntry<bool> m_skip_interaction_cutscenes;
	private static ConfigEntry<bool> m_allow_tree_chopping;
	private static ConfigEntry<bool> m_always_open_doors;

	private static Dictionary<int, int> m_loved_gifts = new Dictionary<int, int>();

	private void Awake() {
		logger = this.Logger;
		try {
			m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
			m_see_all_on_map = this.Config.Bind<bool>("General", "See All NPCs on Map", false, "Set to true to see all NPC icons on the map, regardless of friendship level.");
			m_always_open_doors = this.Config.Bind<bool>("General", "Always Open Doors", false, "Set to true to open all private room doors, regardless of friendship level.");
			m_no_gift_counting = this.Config.Bind<bool>("General", "Disable Repeat Gift Counting", false, "Set to true to prevent NPCs from caring if you give them the same thing over and over.");
			m_love_all_gifts = this.Config.Bind<bool>("General", "Love All Gifts", false, "Set to true to have NPCs magically believe everything you give them is something else they really love (makes for interesting dialogue screenshots when gifting manure...)");
			m_quick_card_game = this.Config.Bind<bool>("General", "Instant Card Win", false, "Set to true to instantly win the animal card game.");
			m_card_favor_value = this.Config.Bind<int>("General", "Instant Card Favor Value", 13, "Amount of favor gained from instant card game win (int, default 13 [game average from 3 wins]).");
			m_enable_all_interactives = this.Config.Bind<bool>("General", "Enable All Interactive Actions", false, "Set to true to enable ALL interactions in the 'Interactive' submenu, regardless of friendship or legality of interspecies relations (note that obviously a lot of the animations will be wonky)");
			m_no_esc_warning = this.Config.Bind<bool>("General", "Disable Esc Confirmation for Interactive Cutscenes", true, "Set to false to reenable the annoying 'Are you sure?' window that pops up when attempting to escape the Kiss/etc interaction cutscene.");
			m_skip_interaction_cutscenes = this.Config.Bind<bool>("General", "Skip Interaction Cutscenes", false, "Set to true to make interaction (kiss/hug/etc) cutscenes die immediately (cuz they do REALLY get old).");
			m_allow_tree_chopping = this.Config.Bind<bool>("General", "Allow Tree Chopping", false, "Set to true to keep Burgess's prying eyes (edited for language [I hate Burgess!]) somewhere else when you chop down cactuses and trees (note: this will not work if he's right next to you =P).");
			if (m_enabled.Value) {
				this.m_harmony.PatchAll();
			}
			logger.LogInfo("devopsdinosaur.sandrock.easy_interactions v0.0.1" + (m_enabled.Value ? "" : " [inactive; disabled in config]") + " loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}
	}

	[HarmonyPatch(typeof(MapPartControl), "EnoughFavor")]
	class HarmonyPatch_MapPartControl_EnoughFavor {

		private static bool Prefix(MapPartControl __instance, ref bool __result) {
			if (!m_enabled.Value || !m_see_all_on_map.Value) {
				return true;
			}
			__result = true;
			return false;
		}
	}

	[HarmonyPatch(typeof(AnimalCardFightModule), "BeginGame")]
	class HarmonyPatch_AnimalCardFightModule_BeginGame {

		private static void Postfix(
			AnimalCardFightModule __instance, 
			ref int ___fadorTotal,
			Dictionary<int, AnimalCardFightData> ___animalCardFightDatas,
			int ___npcid
		) {
			try {
				if (!m_enabled.Value || !m_quick_card_game.Value) {
					return;
				}
				___fadorTotal = m_card_favor_value.Value;
				___animalCardFightDatas[___npcid].timesOfDay = 3;
				__instance.GetType().GetProperty("IsPlayAgain", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).SetValue(__instance, false);
				__instance.EndGame();
			} catch (Exception e) {
				logger.LogError("** HarmonyPatch_AnimalCardFightModule_BeginGame.Postfix ERROR - " + e);
			}
		}
	}

	[HarmonyPatch(typeof(AnimalCardFightModule), "Deserialize")]
	class HarmonyPatch_AnimalCardFightModule_Deserialize {

		private static void Postfix(ref List<int> ___lockNpcPlay) {
			try {
				if (m_enabled.Value && m_quick_card_game.Value) {
					___lockNpcPlay = new List<int>();
				}
			} catch (Exception e) {
				logger.LogError("** HarmonyPatch_AnimalCardFightModule_Deserialize.Postfix ERROR - " + e);
			}
		}
	}

	[HarmonyPatch(typeof(OnPlayerChopTreeFall), "Filter")]
	class HarmonyPatch_OnPlayerChopTreeFall_Filter {

		private static bool Prefix(ref bool __result) {
			if (!m_enabled.Value || !m_allow_tree_chopping.Value) {
				return true;
			}
			__result = true;
			return false;
		}
	}

	[HarmonyPatch(typeof(AutoDoorCondition_Favor), "IsOk")]
	class HarmonyPatch_AutoDoorCondition_Favor_IsOk {

		private static bool Prefix(ref bool __result) {
			if (!m_enabled.Value || !m_always_open_doors.Value) {
				return true;
			}
			__result = true;
			return false;
		}
	}

    [HarmonyPatch(typeof(SendGiftModule), "GetRepeatGiftCount")]
    class HarmonyPatch_SendGiftModule_GetRepeatGiftCount {

        private static bool Prefix(ref int __result) {
			if (!m_enabled.Value || !m_no_gift_counting.Value) {
				return true;
			}
            __result = 0;
            return false;
        }
    }

	[HarmonyPatch(typeof(SendGiftUtils), "DoSendGiftResult")]
	class HarmonyPatch_SendGiftUtils_DoGiftResult {

		private static bool Prefix(int npcId, ref int itemId, ref GradeType gradeType) {
			try {
				if (!m_enabled.Value || !m_love_all_gifts.Value) {
					return true;
				}
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
			} catch (Exception e) {
				logger.LogError("** HarmonyPatch_SendGiftUtils_DoGiftResult.Prefix ERROR - " + e);
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(InteractiveOption), "CanInteractive")]
	class HarmonyPatch_InteractiveOption_CanInteractive {

		private static bool Prefix(ref bool __result) {
			if (!m_enabled.Value || !m_enable_all_interactives.Value) {
				return true;
			}
			__result = true;
			return false;
		}
	}

	[HarmonyPatch(typeof(InteractiveMgr), "OnInteractNpc")]
	class HarmonyPatch_InteractiveMgr_OnInteractNpc {

		private static bool Prefix(
			InteractiveMgr __instance,
            Actor actor, 
			ChoiceType type,
            List<InteractiveData> ___datas
        ) {
			try {
				if (!m_enabled.Value || !m_skip_interaction_cutscenes.Value) {
					return true;
				}
                if (type != ChoiceType.Interactive) {
                    return false;
                }
                int instId = actor.InstanceId;
                int protoId = actor.InstanceId;
                List<InteractiveContent> contents = (List<InteractiveContent>) __instance.GetType().GetMethod("GetOptionals", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[] {InteractiveActorType.Npc, protoId});
                if (contents.Count == 0) {
                    Module<NpcInteractionManager>.Self.OnCancel();
                    return false;
                }
                string str = TextMgr.GetStr(3196);
                List<string> list = new List<string>();
                for (int i = 0; i < contents.Count; i++) {
                    list.Add(TextMgr.GetStr(contents[i].nameId));
                }
                list.Add(TextMgr.GetStr(20263));
                ConvSegmentBase convSegmentBase = new ConvSegmentBase(str, list);
                convSegmentBase.IgnoreBehavour = true;
                convSegmentBase.OnClick = (Action<int>) Delegate.Combine(convSegmentBase.OnClick, (Action<int>) delegate (int idx) {
                    if (idx >= 0 && idx < contents.Count) {
                        int data_index = (int) __instance.GetType().GetMethod("GetDataIdx", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[] {InteractiveActorType.Npc, protoId});
                        ___datas[data_index].count++;
                        ___datas[data_index].totalCount++;
                        Module<SocialModule>.Self.AddSocialFavor(instId, 2);
                    }
					Module<NpcInteractionManager>.Self.OnCancel();
                });
                Module<ConversationManager>.Self.AddSegmentBaseToCache(convSegmentBase);
                return false;
			} catch (Exception e) {
				logger.LogError("** HarmonyPatch_InteractiveMgr_OnInteractNpc.Prefix ERROR - " + e);
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(InteractiveAction_MotionAnimLoop), "OnInputEsc")]
	class HarmonyPatch_InteractiveAction_MotionAnimLoop_OnInputEsc {

		private static bool Prefix(ref bool ___isEsc) {
			if (!m_enabled.Value || !m_no_esc_warning.Value) {
				return true;
			}
			___isEsc = true;
			return false;
		}
	}

}