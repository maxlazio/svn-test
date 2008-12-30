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
	public class ScrollableListBox : ListBox {

		public ScrollViewer ScrollViewer { get { return GetTemplateChild("ScrollViewer") as ScrollViewer; } }

		public Double VerticalOffset {
			get {
				if (ScrollViewer != null) return ScrollViewer.VerticalOffset;
				return 0;
			}
			set {
				if (ScrollViewer != null) ScrollViewer.ScrollToVerticalOffset(value);
			}
		}

		public Double MaxVerticalOffset {
			get {
				if (ScrollViewer != null) return ScrollViewer.ExtentHeight;
				return 0;
			}
		}

		public Double HorizontalOffset {
			get {
				if (ScrollViewer != null) return ScrollViewer.HorizontalOffset;
				return 0;
			}
			set {
				if (ScrollViewer != null) ScrollViewer.ScrollToHorizontalOffset(value);
			}
		}

		public Double MaxHorizontalOffset {
			get {
				if (ScrollViewer != null) return ScrollViewer.ExtentWidth;
				return 0;
			}
		}
	}
}
