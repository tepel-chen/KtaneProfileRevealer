using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

namespace ProfileRevealerLib {
	public class Config {
		public bool ShowModuleNames;
		public float Delay = 2;
		[JsonConverter(typeof(StringEnumConverter))]
		public KeyCode PopupKey = KeyCode.F1;

		internal static readonly Dictionary<string, object>[] TweaksEditorSettings = new[] {
			new Dictionary<string, object> {
				{ "Filename", "ProfileRevealer-settings.txt" },
				{ "Name", "Profile Revealer" },
				{ "Listings", new List<Dictionary<string, object>> {
						new Dictionary<string, object> { { "Key", nameof(ShowModuleNames) }, { "Text", "Show Module Names" }, { "Description", "Shows the name of the module in the popup. Disables leaderboards." } },
						new Dictionary<string, object> { { "Key", nameof(Delay) }, { "Text", "Popup Delay" }, { "Description", "Time in seconds a module should be higlighted before showing the popup." } },
						new Dictionary<string, object> { { "Key", nameof(PopupKey) }, { "Text", "Popup Key" }, { "Description", "The button which can be used to force the popup to appear." } }
					}
				}
			}
		};
	}
}
