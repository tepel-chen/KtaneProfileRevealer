#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Assets.Scripts.Missions;
using Assets.Scripts.Services;
using Newtonsoft.Json;
using UnityEngine;

namespace ProfileRevealerLib {
	public class ProfileRevealerService : MonoBehaviour {
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
		public ModulePopup PopupPrefab;
		public GameObject AdvantageousWarningCanvas;

		private KMModSettings KMModSettings;
		private KMGameInfo KMGameInfo;
		private Config config;
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
		private KMGameInfo.State gameState;
		private ModulePopup? currentPopup;
		private Component? tweaksService;

		public void Start() {
			this.AdvantageousWarningCanvas.SetActive(false);

			this.KMGameInfo = this.GetComponent<KMGameInfo>();
			this.KMModSettings = this.GetComponent<KMModSettings>();

			if (Application.isEditor) {
				this.config = new Config { ShowModuleNames = true };
				this.StartCoroutine(this.CheckForBombsTest());
			} else {
				this.RefreshConfig();
				this.KMGameInfo.OnStateChange = this.KMGameInfo_OnStateChange;
			}
		}

		public void Update() {
			if (this.gameState == KMGameInfo.State.Gameplay && this.config != null && this.currentPopup != null && Input.GetKeyDown(this.config.PopupKey))
				this.currentPopup.Show();
		}

		private void KMGameInfo_OnStateChange(KMGameInfo.State state) {
			if (state == KMGameInfo.State.Gameplay) {
				// Enabling Show Module Names is considered an advantageous feature, so disable records in that case.
				// This code is based on the Tweaks mod.
				if (this.config.ShowModuleNames) {
					if (this.tweaksService != null) AbstractServices.Instance.GetType().GetField("TargetMissionID").SetValue(null, GameplayState.MissionToLoad);
					else SteamFilterService.TargetMissionID = GameplayState.MissionToLoad;
				}
				this.StartCoroutine(this.CheckForBombs());
			} else if (state == KMGameInfo.State.Transitioning && this.gameState == KMGameInfo.State.Setup) {
				this.KMModSettings.RefreshSettings();
				this.RefreshConfig();

				if (this.config.ShowModuleNames && GameplayState.MissionToLoad != "freeplay" && GameplayState.MissionToLoad != "custom")
					this.StartCoroutine(this.ShowAdvantageousWarning());
			} else if (state == KMGameInfo.State.Setup) {
				if (this.tweaksService == null) {
					Debug.Log("[Profile Revealer] Looking for Tweaks service...");
					var obj = GameObject.Find("Tweaks(Clone)");
					if (obj != null) this.tweaksService = obj.GetComponent("Tweaks");
					if (this.tweaksService != null) Debug.Log("[Profile Revealer] Found Tweaks service.");
					else Debug.Log("[Profile Revealer] Did not find Tweaks service.");

					if (this.tweaksService == null)
						typeof(AbstractServices).GetField("instance", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, new SteamFilterService());
				}
			}
			this.gameState = state;
		}

		private void RefreshConfig() {
			try {
				this.config = JsonConvert.DeserializeObject<Config>(this.KMModSettings.Settings);
				if (this.config != null) return;
			} catch (JsonSerializationException ex) {
				Debug.LogError("[Profile Revealer] The mod settings file is invalid.");
				Debug.LogException(ex, this);
			}
			this.config = new Config();
			using var writer = new StreamWriter(this.KMModSettings.SettingsPath);
			new JsonSerializer() { Formatting = Formatting.Indented }.Serialize(writer, this.config);
		}

		private IEnumerator ShowAdvantageousWarning() {
			yield return new WaitForSeconds(2.5f);
			// If Tweaks is present, we'll use its warning.
			if (this.tweaksService != null) {
				var warning = this.tweaksService.transform.Find("UI/AdvantageousWarning");
				if (warning == null || !warning.gameObject.activeSelf) {
					this.StartCoroutine((IEnumerator) this.tweaksService.GetType().GetMethod("ShowAdvantageousWarning", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
						.Invoke(this.tweaksService, null));
				}
			} else {
				// Otherwise use our own.
				this.AdvantageousWarningCanvas.SetActive(true);
				yield return new WaitForSeconds(5);
				this.AdvantageousWarningCanvas.SetActive(false);
			}
		}

		private IEnumerator CheckForBombs() {
			Debug.Log("[Profile Revealer] Waiting for bombs...");
			var oldBombs = new List<Bomb>();
			var bombs = SceneManager.Instance.GameplayState.Bombs;
			var count = 0;
			while (bombs.Count == 0) yield return null;
			// Wait to see if any more bombs are being generated.
			while (true) {
				count = bombs.Count;
				yield return new WaitForSeconds(0.5f);
				if (bombs.Count == count) break;
			}
			Debug.Log($"[Profile Revealer] Found {bombs.Count} bomb(s).");

			var isFactoryRoom = bombs[0].GetComponent<Selectable>().Parent.name.StartsWith("FactoryRoom");

			// Disable leaderboards (needs to be done now to override Tweaks).
			if (this.config.ShowModuleNames) {
				Assets.Scripts.Stats.StatsManager.Instance.DisableStatChanges = true;
				Assets.Scripts.Records.RecordManager.Instance.DisableBestRecords = true;
			}

			// Load profiles.
			var profiles = new List<KeyValuePair<string, HashSet<string>>>();
			var inactiveVetos = new List<KeyValuePair<string, HashSet<string>>>();
			var enabledProfiles = new HashSet<string>();
			var path = Path.Combine(Application.persistentDataPath, "modSelectorConfig.json");
			if (File.Exists(path)) {
				using var reader = new StreamReader(path);
				enabledProfiles = new JsonSerializer().Deserialize<HashSet<string>>(new JsonTextReader(reader));
			}
			path = Path.Combine(Application.persistentDataPath, "ModProfiles");
			if (Directory.Exists(path)) {
				foreach (var file in Directory.GetFiles(path)) {
					var profileName = Path.GetFileNameWithoutExtension(file);
					try {
						using var reader = new StreamReader(file);
						var profile = new JsonSerializer().Deserialize<Profile>(new JsonTextReader(reader));
						if (profile.DisabledList == null) {
							Debug.LogWarning($"[Profile Revealer] Could not load profile {Path.GetFileName(file)}");
							continue;
						}
						if (enabledProfiles.Contains(profileName)) {
							if (profile.Operation == ProfileType.Expert)
								profiles.Add(new KeyValuePair<string, HashSet<string>>(profileName, profile.DisabledList));
						} else {
							if (profile.Operation != ProfileType.Expert)
								inactiveVetos.Add(new KeyValuePair<string, HashSet<string>>(profileName, profile.DisabledList));
						}
					} catch (Exception ex) {
						Debug.LogWarning($"[Profile Revealer] Could not load profile {Path.GetFileName(file)}");
						Debug.LogException(ex, this);
					}
				}
			} else {
				Debug.LogWarning($"[Profile Revealer] The Mod Selector profile directory does not exist. Abort.");
				yield break;
			}

			while (true) {
				foreach (var bomb in bombs.Except(oldBombs)) {
					foreach (var component in bomb.BombComponents) {
						if (component.ComponentType == ComponentTypeEnum.Empty || component.ComponentType == ComponentTypeEnum.Timer) continue;
						Debug.Log($"[Profile Revealer] Attaching to '{component.name}'.");

						var kmBombModule = component.GetComponent<KMBombModule>();
						var kmNeedyModule = component.GetComponent<KMNeedyModule>();

						var popup = Instantiate(this.PopupPrefab, component.transform, false);
						if (this.config.ShowModuleNames) popup.moduleName = component.GetModuleDisplayName();
						popup.Delay = this.config.Delay;
						if (kmBombModule == null && kmNeedyModule == null) {
							// Vanilla modules will be shown as enabled by all profiles.
							// Otherwise it could be used to easily find the two vanilla modules on the Centurion, for instance.
							popup.enabledProfiles = profiles.Select(p => p.Key);
						} else {
							var moduleID = kmBombModule != null ? kmBombModule.ModuleType : kmNeedyModule.ModuleType;
							popup.enabledProfiles = profiles.Where(p => !p.Value.Contains(moduleID)).Select(p => p.Key);
							popup.disabledProfiles = profiles.Where(p => p.Value.Contains(moduleID)).Select(p => p.Key);
							popup.inactiveProfiles = inactiveVetos.Where(p => p.Value.Contains(moduleID)).Select(p => p.Key);
						}
						popup.transform.SetParent(component.transform.parent, true);
						var selectable = component.GetComponent<Selectable>();
						selectable.OnHighlight += () => { this.currentPopup = popup; popup.StartAnimation(); };
						selectable.OnHighlightEnded += () => { if (this.currentPopup == popup) this.currentPopup = null; popup.StopAnimation(); };
					}
					oldBombs.Add(bomb);
				}

				if (!isFactoryRoom) yield break;
				Debug.Log("[Profile Revealer] Factory room is active. Waiting for new bombs.");
				while (!bombs.Except(oldBombs).Any()) {
					if (this.gameState != KMGameInfo.State.Gameplay) yield break;
					yield return new WaitForSeconds(1);
				}
			}
		}

		private IEnumerator CheckForBombsTest() {
			Debug.Log("[Profile Revealer] Looking for bombs.");
			KMBomb[] bombs;
			while (true) {
				bombs = FindObjectsOfType<KMBomb>();
				if (bombs.Length > 0) break;
				yield return null;
			}
			foreach (var bomb in bombs) {
				foreach (var transform in bomb.transform.Find("Modules").Cast<Transform>()) {
					string name;
					var module = transform.GetComponent<KMBombModule>();
					if (module != null) name = module.ModuleDisplayName;
					else {
						var needyModule = transform.GetComponent<KMNeedyModule>();
						if (needyModule != null) name = needyModule.ModuleDisplayName;
						else continue;
					}
					Debug.Log($"[Profile Revealer] Attaching to '{name}'.");

					var popup = Instantiate(this.PopupPrefab, transform, false);
					if (this.config.ShowModuleNames) popup.moduleName = transform.name;
					popup.Delay = 2;
					popup.enabledProfiles = new[] { "Alice", "Bob" };
					popup.disabledProfiles = new[] { "Carol", "Dan" };
					popup.inactiveProfiles = new[] { "Veto A", "Veto B" };
					popup.transform.SetParent(transform.parent, true);
					var selectable = transform.GetComponent<KMSelectable>();
					selectable.OnHighlight += () => { this.currentPopup = popup; popup.StartAnimation(); };
					selectable.OnHighlightEnded += () => { if (this.currentPopup == popup) this.currentPopup = null; popup.StopAnimation(); };
				}
			}
			yield break;
		}

#pragma warning disable CS0649  // Field is never assigned to
		private struct Profile {
			public HashSet<string> DisabledList;
			public ProfileType Operation;
		}

		private enum ProfileType {
			Expert,
			Defuser
		}
	}
}
