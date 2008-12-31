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
	public class Playlist : ControlBase, IElementList {
		public override void OnApplyTemplate() {
			base.OnApplyTemplate();
			listBox = GetTemplateChild("listBox") as ListBox;
			listBox.SelectionChanged += OnListBoxSelectionChanged;
		}

		internal ListBox listBox;

		public int Count { get { return listBox.Items.Count; } }
		public int SelectedIndex { get { return listBox.SelectedIndex; } set { listBox.SelectedIndex = value; } }
		public IEnumerable Source { get { return listBox.ItemsSource; } set { listBox.ItemsSource = value; } }
		public new FrameworkElement Parent { get { return base.Parent as FrameworkElement; } }
		public void ScrollIntoView(object o) { listBox.ScrollIntoView(o); }
		public event SelectionChangedEventHandler SelectionChanged;

		internal void OnListBoxSelectionChanged(object sender, SelectionChangedEventArgs args) {
			if (SelectionChanged != null) SelectionChanged(sender, args);
		}
		public void Refresh() { }
	}

	public class Chapters : Playlist {
		//new type so it can be styled differently
	}
}
