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
using System.Collections;

namespace org.OpenVideoPlayer.Controls.Visuals {
	public interface IElementList {

		int Count { get; }
		int SelectedIndex { get; set; }
		IEnumerable Source { get; set; }
		FrameworkElement Parent { get; }
		double ActualWidth { get; }
		double ActualHeight { get; }
		Thickness Margin { get; set; }
		void ScrollIntoView(object o);
		event SelectionChangedEventHandler SelectionChanged;

		void Refresh();
	}
}
