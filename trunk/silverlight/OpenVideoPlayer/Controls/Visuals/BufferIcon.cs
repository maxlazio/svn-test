using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace org.OpenVideoPlayer.Controls.Visuals {
	public class BufferIcon : ControlBase {
		public BufferIcon() {
			//InitializeComponent();
		}

		protected Storyboard roll;

		public override void OnApplyTemplate() {
			base.OnApplyTemplate();
			roll = GetTemplateChild("roll") as Storyboard;
		}

		private bool active = false;
		public bool Active {
			get {
				return active;
			}
			set {
				if (active == value) return;
				active = value;
				if (value) {
					roll.Begin();
					Visibility = Visibility.Visible;
				} else {
					roll.Stop();
					Visibility = Visibility.Collapsed;
				}
			}
		}
	}
}
