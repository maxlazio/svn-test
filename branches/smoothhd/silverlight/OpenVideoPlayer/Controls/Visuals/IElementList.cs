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

	/// <summary>
	/// An interface used for a listitem, like playlist, logviewer etc.  
	/// This way the actual implementation (whether a listbox, textbox, or totally custom control) can be independent.
	/// </summary>
	public interface IElementList {
		/// <summary>
		/// The count of items
		/// </summary>
		int Count { get; }
		/// <summary>
		/// Selected index of current items
		/// </summary>
		int SelectedIndex { get; set; }
		/// <summary>
		/// The source of the items, allows you to set a collection that will provide the items
		/// </summary>
		IEnumerable Source { get; set; }
		/// <summary>
		/// The parent control
		/// </summary>
		FrameworkElement Parent { get; }

		double ActualWidth { get; }
		double ActualHeight { get; }
		Thickness Margin { get; set; }
		/// <summary>
		/// Scrolls a particular item into view
		/// </summary>
		/// <param name="o"></param>
		void ScrollIntoView(object o);

		/// <summary>
		/// event fires when the selected item changes
		/// </summary>
		event SelectionChangedEventHandler SelectionChanged;

		event SizeChangedEventHandler SizeChanged;

		/// <summary>
		/// Implemented in some cases to refresh the view, if required
		/// </summary>
		void Refresh();
	}
}
