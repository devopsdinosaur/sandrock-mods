using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using Pathea;
using Pathea.StoreNs;
using Pathea.ItemNs;
using Pathea.ItemContainers;
using Pathea.FrameworkNs;
using Pathea.UISystemV2.UI;

[BepInPlugin("devopsdinosaur.sandrock.walmart", "Walmart", "0.0.1")]
public class WalmartPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sandrock.walmart");
	public static ManualLogSource logger;
	private static ConfigEntry<bool> m_enabled;
	private static ConfigEntry<int> m_max_stack;
	private static ConfigEntry<bool> m_always_open;
	private static ConfigEntry<int> m_store_id;
	private static ConfigEntry<int> m_default_price;

	public static Dictionary<int, ItemPrototype> m_item_prototypes = null;
	public static Dictionary<int, SellProductBaseData> m_sell_items = null;
	public static Dictionary<int, Store> m_stores = null;

	private void Awake() {
		logger = this.Logger;
		try {
			m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
			m_max_stack = this.Config.Bind<int>("General", "Max Stack", 99999, "Maximum stack size of all items (int, default 99999).");
			m_always_open = this.Config.Bind<bool>("General", "Always Open", true, "Set to false to have stores close at their normal hours.");
			m_store_id = this.Config.Bind<int>("General", "Store ID", 2, "ID of store which will be used for plugin (int, default 2 [Hammer Time]; check BepInEx/LogOutput.log for other IDs).");
			m_default_price = this.Config.Bind<int>("General", "Default Price", 100, "Cost of items that have no cost listed in the game database (int, default 100 [note that setting this to zero will cause the shopkeeper to refuse to sell]).");
			if (m_enabled.Value) {
				this.m_harmony.PatchAll();
			}
			logger.LogInfo("devopsdinosaur.sandrock.walmart v0.0.1" + (m_enabled.Value ? "" : " [inactive; disabled in config]") + " loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}
	}

	[HarmonyPatch(typeof(Store), "IsOpen")]
	class HarmonyPatch_Store_IsOpen {

		private static bool Prefix(ref bool __result) {
			if (!m_enabled.Value || !m_always_open.Value) {
				return true;
			}
			__result = true;
			return false;
		}
	}
	
	[HarmonyPatch(typeof(ItemPrototype), "Init")]
	class HarmonyPatch_ItemPrototype_Init {

		private static bool Prefix(ItemPrototype __instance) {
			try {
				if (m_item_prototypes == null) {
					m_item_prototypes = new Dictionary<int, ItemPrototype>();
				}
				m_item_prototypes[__instance.id] = __instance;
				__instance.stackNumber = m_max_stack.Value;
			} catch (Exception e) {
				logger.LogError("** HarmonyPatch_ItemPrototype_Init.Prefix ERROR - " + e);
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(ItemPrototypeModule), "Get")]
	class HarmonyPatch_ItemPrototypeModule_Get {

		private static bool Prefix(int id, ref ItemPrototype __result) {
			try {
				__result = m_item_prototypes[id];
				__result.stackNumber = m_max_stack.Value;
				return false;
			} catch (Exception e) {
				logger.LogError("** HarmonyPatch_ItemPrototypeModule_Get.Prefix ERROR - " + e);
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
				}
			} catch (Exception e) {
				logger.LogError("** HarmonyPatch_SellProductBaseData_LoadData.Prefix ERROR - " + e);
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
				if (m_enabled.Value) {
					logger.LogInfo($"Store - id: {__instance.id}, name: {__instance.Name})");
					if (__instance.id == m_store_id.Value) {
						__instance.data.FilterType = (FilterType[]) Enum.GetValues(typeof(FilterType));
					}
				}
			} catch (Exception e) {
				logger.LogError("** HarmonyPatch_Store_InitRecordData.Prefix ERROR - " + e);
			}
		}
	}

	[HarmonyPatch(typeof(Store), "FetchSlot")]
	class HarmonyPatch_Store_FetchSlot {

		private static bool Prefix(Store __instance, List<ItemSlot> ___fetchSlots) {
			try {
				if (!m_enabled.Value || __instance.id != m_store_id.Value) {
					return true;
				}
				__instance.Money = 9999999;
				__instance.groupProducts = new List<GroupProductItem>();
				__instance.singleProducts = new List<SellProduct>();
				GradeRandomData grade_random_data = new GradeRandomData(1, 0, 0, 0);
				Season[] seasons = new Season[] {Season.Max};
				foreach (int id in m_item_prototypes.Keys) {
					ItemPrototype proto = m_item_prototypes[id];
					if (!m_sell_items.ContainsKey(id)) {
						if (string.IsNullOrEmpty(TextMgr.GetStr(proto.nameId))) {
							continue;
						}
						m_sell_items[id] = new SellProductBaseData() {
							id = proto.infoId,
							itemId = id,
							price = new DoubleInt((proto.buyPrice > 0 ? proto.buyPrice : m_default_price.Value), 1),
							currency = -1,
							grade = grade_random_data,
							sellSeason = seasons,
							unlockDlc = new int[] {}
						};
					}
					SellProductBaseData data = m_sell_items[id];
					SellProduct product = new SellProduct(data, m_max_stack.Value, 0, __instance);
					product.sellProductItem.Add(new SellProductItem(
						Module<ItemInstance.Module>.Self.Create(data.itemId, m_max_stack.Value, GradeType.Max, true),
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
}