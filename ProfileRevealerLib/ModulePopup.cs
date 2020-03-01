using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace ProfileRevealerLib {
	public class ModulePopup : MonoBehaviour {
		public RectTransform Canvas;
		public Text Text;
		public RectTransform BoxCollider;

		public bool Visible => this.Canvas.gameObject.activeSelf;

		public float Delay { get; set; }
		public Transform Module { get; set; }
		internal string moduleName;
		internal IEnumerable<string> enabledProfiles;
		internal IEnumerable<string> disabledProfiles;
		internal IEnumerable<string> inactiveProfiles;
		private Coroutine coroutine;

		public void Start() {
			var enabledProfilesStr = Join(", ", this.enabledProfiles);
			var disabledProfilesStr = Join(", ", this.disabledProfiles);
			var inactiveProfilesStr = Join(", ", this.inactiveProfiles);
			this.Canvas.gameObject.SetActive(false);

			var builder = new StringBuilder();
			if (this.moduleName != null) builder.Append($"<b>{this.moduleName}</b>\n");
			builder.Append($"Enabled by: <color=lime>{enabledProfilesStr}</color>");
			if (disabledProfilesStr.Length > 0)
				builder.Append($"\nDisabled by: <color=red>{disabledProfilesStr}</color>");
			if (inactiveProfilesStr.Length > 0)
				builder.Append($"\nInactive vetos: <color=silver>{inactiveProfilesStr}</color>");
			this.Text.text = builder.ToString();

			var colliders = new List<Collider>();
			foreach (var collider in this.Module.GetComponentsInChildren<Collider>(true)) {
				if (!collider.enabled) {
					colliders.Add(collider);
					collider.enabled = true;
				}
			}

			var halfExtents = this.BoxCollider.transform.lossyScale;
			halfExtents.Scale(new Vector3(300, 150, 100));
			Debug.Log($"{this.BoxCollider.position} {halfExtents} {this.BoxCollider.eulerAngles}");
			if (Physics.CheckBox(this.BoxCollider.position, halfExtents, this.BoxCollider.rotation)) {
				var transform = (RectTransform) this.Canvas.GetChild(0);
				transform.pivot = new Vector2(0, 1);
			}

			foreach (var collider in colliders) collider.enabled = false;
		}

		private static string Join<T>(string separator, IEnumerable<T> enumerable) {
			if (enumerable == null) return "";
			var enumerator = enumerable.GetEnumerator();
			if (!enumerator.MoveNext()) return "";
			var builder = new StringBuilder();
			builder.Append(enumerator.Current);
			while (enumerator.MoveNext()) {
				builder.Append(separator);
				builder.Append(enumerator.Current);
			}
			return builder.ToString();
		}

		public void ShowDelayed() {
			if (this.coroutine != null) this.StopCoroutine(this.coroutine);
			this.coroutine = this.StartCoroutine(this.DelayCoroutine());
		}

		public void Show() {
			if (this.coroutine != null) this.StopCoroutine(this.coroutine);
			this.coroutine = null;
			this.Canvas.gameObject.SetActive(true);
		}

		public void Hide() {
			if (this.coroutine != null) this.StopCoroutine(this.coroutine);
			this.coroutine = null;
			this.Canvas.gameObject.SetActive(false);
		}

		private IEnumerator DelayCoroutine() {
			if (this.Delay < 0 || float.IsInfinity(this.Delay) || float.IsNaN(this.Delay)) yield break;
			yield return new WaitForSeconds(this.Delay);
			this.Canvas.gameObject.SetActive(true);
		}
	}
}
