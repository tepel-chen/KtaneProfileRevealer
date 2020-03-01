using System;

namespace ProfileRevealerLib {
	[Flags]
	public enum ModifierKeys {
		Shift = 1,
		Ctrl = 2,
		Alt = 4,
		Command = 8,
		Super = 16
	}
}
