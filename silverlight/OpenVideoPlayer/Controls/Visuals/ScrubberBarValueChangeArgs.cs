using System;
using System.Windows.Input;

namespace org.OpenVideoPlayer.Controls.Visuals {
	public class ScrubberBarValueChangeArgs : EventArgs {
		public ScrubberBarValueChangeArgs(Double dblValue, MouseEventArgs args)// Point mousePosition)
			: base() {
			Value = dblValue;
			MouseArgs = args;
		}

		public ScrubberBarValueChangeArgs(Double dblValue, MouseEventArgs args, bool pressed)// Point mousePosition)
			: base() {
			Value = dblValue;
			MouseArgs = args;
			MousePressed = pressed;
		}

		public Double Value { get; private set; }
		public MouseEventArgs MouseArgs { get; private set; }
		public bool MousePressed { get; private set; }
	}
}
