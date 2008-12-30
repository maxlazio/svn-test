using System;
using System.Collections;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using org.OpenVideoPlayer.Util;

namespace org.OpenVideoPlayer.Controls.Visuals {
	public partial class LogViewer : ControlBase, IElementList {
		IEnumerable source;
		protected internal TextScrollBox text;
		StringBuilder logText = new StringBuilder(4096);
		DateTime start = DateTime.Now;
		int lines = 0;
		private CheckBox autoScroll;

		#region IElementList Members

		public int Count { get { return lines; } }

		public int SelectedIndex {
			get { return text.SelectionStart; }
			set { text.SelectionStart = value; }
		}

		public new FrameworkElement Parent { get { return base.Parent as FrameworkElement; } }

		public void ScrollIntoView(object o) { text.ScrollToBottom(); }

		public event SelectionChangedEventHandler SelectionChanged;

		#endregion

		public override void OnApplyTemplate() {
			BindFields = false;
			base.OnApplyTemplate();
			text = GetTemplateChild("text") as TextScrollBox;
			text.GotFocus += OnTextGotFocus;

			try {
				Border b = new Border() { VerticalAlignment = VerticalAlignment.Top, HorizontalAlignment = HorizontalAlignment.Left };
				b.Child = autoScroll = new CheckBox() { IsChecked = true, Content = "AutoScroll", Width = 100, Foreground = new SolidColorBrush(Colors.White), FontSize = 9 };
				autoScroll.Checked += new RoutedEventHandler(autoScroll_Checked);
				autoScroll.Unchecked += new RoutedEventHandler(autoScroll_Unchecked);
				Box p = Parent as Box;
				p.ApplyTemplate();
				Grid grid = p.LayoutRoot as Grid;
				grid.Children.Add(b);
				p.UpdateLayout();
			} catch { }
		}

		void autoScroll_Unchecked(object sender, RoutedEventArgs e) {
			if (text != null) text.AutoSelect = (bool)autoScroll.IsChecked;
		}

		void autoScroll_Checked(object sender, RoutedEventArgs e) {
			if (text != null) text.AutoSelect = (bool)autoScroll.IsChecked;
		}

		protected internal void OnTextGotFocus(object sender, RoutedEventArgs e) {
			text.SelectAll();
		}

		public IEnumerable Source {
			get { return source; }
			set {
				if (source != value) {
					source = value;
					if (value is LogCollection) {
						((LogCollection)value).CollectionChanged += LogViewer_CollectionChanged;
					}
				}
			}
		}

		void LogViewer_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
			if (text == null) return;
			foreach (object o in e.NewItems) {
				OutputEntry oe = o as OutputEntry;
				if (oe != null) {
					lines++;
					TimeSpan interval = (oe.Timestamp - start);

					logText.AppendFormat("\r\n{0}:{1}.{2}  {3} {4}: {5} {6}",
						((int)interval.TotalMinutes).ToString().PadLeft(2, '0'),
						interval.Seconds.ToString().PadLeft(2, '0'),
						interval.Milliseconds.ToString().PadLeft(3, '0'),
						oe.Source, oe.OutputType, oe.Description, oe.ExtendedDesc ?? "");
				}
			}

			if (Visibility == Visibility.Visible && Parent.Visibility == Visibility.Visible) {
				text.AutoSelect = (bool)autoScroll.IsChecked;
				text.Text = logText.ToString();

				if ((bool)autoScroll.IsChecked) {
					text.ScrollToBottom();
				}
				if (SelectionChanged != null) SelectionChanged(this, new SelectionChangedEventArgs(e.OldItems, e.NewItems));
			}
		}

		public void Refresh() {
			text.Text = logText.ToString();

			if ((bool)autoScroll.IsChecked) {
				text.ScrollToBottom();
			}
		}
	}

	public class TextScrollBox : TextBox {
		public TextScrollBox() { AutoSelect = true; }

		public override void OnApplyTemplate() {
			base.OnApplyTemplate();
			this.TextChanged += new TextChangedEventHandler(TextScrollBox_TextChanged);
			this.LayoutUpdated += new EventHandler(TextScrollBox_LayoutUpdated);
			((Border)GetTemplateChild("ReadOnlyVisualElement")).Opacity = 0;
		}

		void TextScrollBox_LayoutUpdated(object sender, EventArgs e) {
			if (AutoSelect) {
				ScrollToLeft();
			}
		}

		public bool AutoSelect { get; set; }

		protected override void OnGotFocus(RoutedEventArgs e) {
			base.OnGotFocus(e);
			if (AutoSelect) {
				SelectAll();
			}
		}

		protected override void OnMouseMove(System.Windows.Input.MouseEventArgs e) {
			base.OnMouseMove(e);
			if (AutoSelect) {
				SelectAll();
			}
		}

		void TextScrollBox_TextChanged(object sender, TextChangedEventArgs e) {
			//ScrollToBottom();
			if (AutoSelect) {
				SelectAll();
			}
		}

		public void ScrollToBottom() {
			ScrollViewer scp = GetTemplateChild("ContentElement") as ScrollViewer;
			if (scp != null) {
				scp.ScrollToVerticalOffset(scp.ExtentHeight);
				scp.ScrollToHorizontalOffset(0);
			}
		}

		public void ScrollToLeft() {
			ScrollViewer scp = GetTemplateChild("ContentElement") as ScrollViewer;
			if (scp != null) {
				scp.ScrollToHorizontalOffset(0);
			}
		}
	}
}
