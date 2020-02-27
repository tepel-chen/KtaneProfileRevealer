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
	}
}
