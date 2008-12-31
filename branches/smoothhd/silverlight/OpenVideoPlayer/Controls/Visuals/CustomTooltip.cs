using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace org.OpenVideoPlayer.Controls.Visuals {
	/// <summary>
	/// A very basic control for a custom tooltip.  Just set the text and position it as needed.
	/// </summary>
	public class CustomToolTip : ControlBase {
		internal TextBlock textBox;

		public override void OnApplyTemplate() {
			base.OnApplyTemplate();
			textBox = GetTemplateChild("textBox") as TextBlock;
		}

		/// <summary>
		/// The text of this tooltip.  
		/// </summary>
		public string Text {
			get { return textBox.Text; }
			set { textBox.Text = value; }
		}
	}
}
