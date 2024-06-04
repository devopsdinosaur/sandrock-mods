using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using Pathea.ActorNs;
using Pathea.ResourcePointNs;
using Pathea.Mtas;
using Pathea.TerrainTree;
using Pathea.FrameworkNs;
using UnityEngine.SceneManagement;
using Pathea.MonsterNs;
using Pathea.HatredNs;
using Pathea.ProficiencyNs;
using Pathea.Attr;
using Pathea.ScenarioNs;
using Pathea.ItemNs;
using Pathea.DestroyableNs;
using Pathea.ProjectileNs;
using Pathea.SkillNs;

[BepInPlugin("devopsdinosaur.sandrock.bulldozer", "Bulldozer", "0.0.1")]
public class BulldozerPlugin : BaseUnityPlugin {

	private Harmony m_harmony = new Harmony("devopsdinosaur.sandrock.bulldozer");
	public static ManualLogSource logger;
	private static ConfigEntry<bool> m_enabled;
	private static ConfigEntry<float> m_bulldoze_radius;
	private static ConfigEntry<float> m_bulldoze_update_frequency;
	private static ConfigEntry<bool> m_bulldoze_terrain;
	private static ConfigEntry<bool> m_bulldoze_nodes;
	private static ConfigEntry<bool> m_bulldoze_monsters;
	private static ConfigEntry<int> m_gather_exp;
	private static ConfigEntry<int> m_combat_exp;

	private void Awake() {
		logger = this.Logger;
		try {
			m_enabled = this.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
			m_bulldoze_radius = this.Config.Bind<float>("General", "Radius", 10f, "Radius within which objects will be automatically destroyed/killed/harvested [depending on other settings] (float).");
			m_bulldoze_update_frequency = this.Config.Bind<float>("General", "Update Frequency", 0.25f, "Delay (in seconds) between scans for objects to bulldoze (float, note: setting this to a number closer to zero will increase computation drag on the game [addtionally, changes to this value in-game will have no effect; requires restart]).");
			m_bulldoze_terrain = this.Config.Bind<bool>("General", "Bulldoze Terrain", true, "Set to false to disable bulldozing of terrain objects, such as rocks and trees.");
			m_bulldoze_nodes = this.Config.Bind<bool>("General", "Bulldoze Nodes", true, "Set to false to disable bulldozing of mine nodes, such as copper, tin, power crystals, etc.");
			m_bulldoze_monsters = this.Config.Bind<bool>("General", "Bulldoze Monsters", false, "Set to true to enable auto-killing of monsters.");
			m_gather_exp = this.Config.Bind<int>("General", "Gather Exp", 0, "Amount of gather exp gained with each bulldozed rock/tree/etc (int, default 0)");
			m_combat_exp = this.Config.Bind<int>("General", "Combat Exp", 0, "Amount of combat exp gained with each bulldozed monster (int, default 0)");
			if (m_enabled.Value) {
				this.m_harmony.PatchAll();
			}
			logger.LogInfo("devopsdinosaur.sandrock.bulldozer v0.0.1" + (m_enabled.Value ? "" : " [inactive; disabled in config]") + " loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
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
			PluginUpdater.Instance.register("bulldozer_update", m_bulldoze_update_frequency.Value, bulldozer_update);
            return true;
        }
    }

	public static void bulldozer_update() {
		
		void bulldoze_resource<T>(Player _player, AttrData _data) where T : ResourcePointBase {
			foreach (T obj in Resources.FindObjectsOfTypeAll<T>()) {
				if (Vector3.Distance(_player.GamePos, obj.transform.position) > m_bulldoze_radius.Value) {
					continue;
				}
				try {
					float current_sp = _data.runtimeAttr[ActorRunTimeAttrType.Sp];
					if ((bool) obj.GetType().GetField("useAutoGeneratorGroup", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetValue(obj)) {
						int autoGeneratorGroupCount = (int) obj.GetType().GetField("autoGeneratorGroupCount", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetValue(obj);
						GeneratorGroup autoGeneratorGroup = (GeneratorGroup) obj.GetType().GetField("autoGeneratorGroup", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetValue(obj);
						List<int> id_list = new List<int>();
						for (int counter = 1; counter < autoGeneratorGroupCount; counter++) {
							obj.itemDrop.CreateDropItem(
								autoGeneratorGroup.generatorGroupId, 
								0f, 
								obj.ResourcePointData.GetScale(), 
								Module<ScenarioModule>.Self.CurScene,
								(int) obj.GetType().GetProperty("itemLevel", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(obj),
								canDetected: false, 
								delegate (List<ItemInstance> itemInstances) {
								}, 
								id_list
							);
							Module<ResourceAreaModule>.Self.GatherDropItemAction(id_list);
							Module<ResourceAreaModule>.Self.PlayerGetResourcePointAction(obj.ResourcePointData);
						}
					}
					obj.GetType().GetMethod("DoGetItem", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(obj, new object[] {true});
					Module<ProficiencyModule>.Self.AddAttrModifierByType(ProficiencyType.Gather, new AttrModifier(ModifyAttrType.FinalPlus, m_gather_exp.Value));
					_data.runtimeAttr.SetCurrenValue(ActorRunTimeAttrType.Sp, current_sp);
				} catch (Exception e) {
					logger.LogError(e);
				}
			}
		}
		
		if (!m_enabled.Value) {
			return;
		}
		Player player = null;
		AttrCmpt cmpt = null;
		AttrData data = null;
		try {
			player = Module<Player>.Self;
			if (player == null) {
				return;
			}
			cmpt = (AttrCmpt) player?.actor.GetType().GetField("attrCmpt", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(player?.actor);
			data = (AttrData) (cmpt != null ? cmpt.GetType().GetField("attrData", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(cmpt) : null);
		} catch {
			return;
		}
		if (m_bulldoze_nodes.Value) {
			bulldoze_resource<CatchableResourcePoint>(player, data);
		}
		if (m_bulldoze_terrain.Value) {
			foreach (GameObject top_obj in SceneManager.GetActiveScene().GetRootGameObjects()) {
				if (top_obj != null && top_obj.name.StartsWith("Terrain") && top_obj.transform.childCount > 0) {
					Transform parent = top_obj.transform.GetChild(0);
					if (parent == null) {
						break;
					}
					for (int index = 1; index < parent.childCount; index++) {
						TerrainTreeObject tree = parent.GetChild(index).GetComponent<TerrainTreeObject>();
						if (tree == null) {
							continue;
						}
						try {
							tree.GetType().GetMethod("ChopTree", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(tree, new object[] { player.GamePos, 999999 });
							Module<ProficiencyModule>.Self.AddAttrModifierByType(ProficiencyType.Gather, new AttrModifier(ModifyAttrType.FinalPlus, m_gather_exp.Value));
						} catch {}
					}
				}
			}
			bulldoze_resource<ResourcePoint>(player, data);
			foreach (DestroyableSceneItemPoint obj in Resources.FindObjectsOfTypeAll<DestroyableSceneItemPoint>()) {
				try {
                    DestroyableSceneItemData item_data = (DestroyableSceneItemData) obj.GetType().GetField("data", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(obj);
                    if (!obj.gameObject.activeSelf || item_data == null || !item_data.isDisplay || item_data.hpPercent == 0 || Vector3.Distance(player.GamePos, item_data.pos) > m_bulldoze_radius.Value) {
						continue;
					}
					logger.LogInfo(obj.name);
					Module<ProjectileModule>.Self.Create(
						"Bullet_Rifle", 
						ShootInfo.Create(player.HeadPos, item_data.pos - player.HeadPos),
						player.actor.GetCasterHandle()
					);
					/*
                    DestroyableItem item = (DestroyableItem) obj.GetType().GetField("dItem", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(obj);
                    item.hp = 0;
                    HitResult hit_result = new HitResult();
                    hit_result.hitTrans = obj.transform;
                    hit_result.hitPos = obj.transform.position;
                    item.GetType().GetMethod("OnChangeHp", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(item, new object[] {999999, hit_result});
                    item.OnDeath();
					item_data.isDisplay = false;
					*/
                } catch (Exception e) {
					logger.LogError(e);
				}
			}
        }
		if (m_bulldoze_monsters.Value) {
			Scene scene = SceneManager.GetSceneByName("Game");
			if (scene == null) {
				return;
			}
			GameObject actors_obj = null;
			foreach (GameObject obj in scene.GetRootGameObjects()) {
				if (obj != null && obj.name == "Actors") {
					actors_obj = obj;
					break;
				}
			}
			if (actors_obj == null) {
				return;
			}
			for (int index = 0; index < actors_obj.transform.childCount; index++) {
				Transform child = actors_obj.transform.GetChild(index);
				ActorViewModel model = null;
				Monster monster = null;
				if (!child.name.StartsWith("Monster_") || Vector3.Distance(player.GamePos, child.position) > m_bulldoze_radius.Value || (model = child.GetComponent<ActorViewModel>()) == null || (monster = Module<MonsterMgr>.Self.GetMonster(model.actor.InstanceId)) == null) {
					continue;
				}
				Module<HatredModule>.Self.AddHatredDamage(monster.actor.InstanceId, 8000, 999999);
				monster.MonsterDeadBefore(-2);
				monster.Kill();
				Module<ProficiencyModule>.Self.AddAttrModifierByType(ProficiencyType.Fight, new AttrModifier(ModifyAttrType.FinalPlus, m_combat_exp.Value));
			}
		}
	}

}