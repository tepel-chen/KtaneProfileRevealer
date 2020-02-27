using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace ProfileRevealerLib {
	public class ModulePopup : MonoBehaviour {
		public GameObject Canvas;
		public Text Text;
		public float Delay { get; set; }
		internal string moduleName;
		internal IEnumerable<string> enabledProfiles;
		internal IEnumerable<string> disabledProfiles;
		internal IEnumerable<string> inactiveProfiles;
		private Coroutine coroutine;

		public void Start() {
			var enabledProfilesStr = Join(", ", this.enabledProfiles);
			var disabledProfilesStr = Join(", ", this.disabledProfiles);
			var inactiveProfilesStr = Join(", ", this.inactiveProfiles);
			this.Canvas.SetActive(false);

			var builder = new StringBuilder();
			if (this.moduleName != null) builder.Append($"<b>{this.moduleName}</b>\n");
			builder.Append($"Enabled by: <color=lime>{enabledProfilesStr}</color>");
			if (disabledProfilesStr.Length > 0)
				builder.Append($"\nDisabled by: <color=red>{disabledProfilesStr}</color>");
			if (inactiveProfilesStr.Length > 0)
				builder.Append($"\nInactive vetos: <color=silver>{inactiveProfilesStr}</color>");
			this.Text.text = builder.ToString();
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

		public void StartAnimation() {
			if (this.coroutine != null) this.StopCoroutine(this.coroutine);
			this.coroutine = this.StartCoroutine(this.AnimateCoroutine());
		}

		public void StopAnimation() {
			if (this.coroutine != null) this.StopCoroutine(this.coroutine);
			this.coroutine = null;
			this.Canvas.SetActive(false);
		}

		private IEnumerator AnimateCoroutine() {
			if (this.Delay < 0 || float.IsInfinity(this.Delay)) yield break;
			yield return new WaitForSeconds(this.Delay);
			this.Canvas.SetActive(true);
		}

		public void Show() {
			if (this.coroutine != null) this.StopCoroutine(this.coroutine);
			this.coroutine = null;
			this.Canvas.SetActive(true);
		}
	}
}
