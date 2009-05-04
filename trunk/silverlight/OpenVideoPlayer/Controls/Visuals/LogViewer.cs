using System;
using System.Collections;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using org.OpenVideoPlayer.Util;
using System.Windows.Browser;
using System.Windows.Shapes;

namespace org.OpenVideoPlayer.Controls.Visuals {

	[ScriptableType]
	/// <summary>
	/// Our logviewer, currently implemented with a custom textbox, and uses the IElementList interface
	/// </summary>
	public partial class LogViewer : ControlBase, IElementList {
		IEnumerable source;
		protected internal TextScrollBox text;
		StringBuilder logText = new StringBuilder(4096);
		DateTime start = DateTime.Now;
		int lines = 0;
		private CheckBox autoScroll, translucent;
		private TextBlock stats, title;
		private Grid grid, subgrid;
		private Button copy, showLogs;
		private Path path;

		#region IElementList Members

		public int Count { get { return lines; } }

		public int SelectedIndex {
			get { return text.SelectionStart; }
			set { text.SelectionStart = value; }
		}

		public new FrameworkElement Parent { get { return base.Parent as FrameworkElement; } }

		public void ScrollIntoView(object o) { text.ScrollToBottom(); }

		public event SelectionChangedEventHandler SelectionChanged;

		[ScriptableMember]
		public event ElementListEventHandler StatsChanged;

		#endregion

		public override void OnApplyTemplate() {
			base.OnApplyTemplate();
			text = GetTemplateChild("text") as TextScrollBox;
			text.GotFocus += OnTextGotFocus;

			autoScroll = GetTemplateChild("autoScroll") as CheckBox;
			autoScroll.Checked+=new RoutedEventHandler(autoScroll_Checked);
			autoScroll.Unchecked+=new RoutedEventHandler(autoScroll_Unchecked);

			translucent = GetTemplateChild("translucent") as CheckBox;
			translucent.Checked += new RoutedEventHandler(translucent_Checked);
			translucent.Unchecked += new RoutedEventHandler(translucent_Unchecked);

			stats = GetTemplateChild("stats") as TextBlock;
			stats.SizeChanged += new SizeChangedEventHandler(stats_SizeChanged);

			grid = GetTemplateChild("grid") as Grid;
			subgrid = GetTemplateChild("subGrid") as Grid;

			showLogs = GetTemplateChild("showLogs") as Button;
			showLogs.Click += new RoutedEventHandler(showLogs_Click);

			path = GetTemplateChild("path") as Path;

			Children.Add(grid);

			//ExpandLogs = false;

			//try {
			//    Border b = new Border() { VerticalAlignment = VerticalAlignment.Top, HorizontalAlignment = HorizontalAlignment.Left };
			//    b.Child = autoScroll = new CheckBox() { IsChecked = true, Content = "AutoScroll", Width = 100, Foreground = new SolidColorBrush(Colors.White), FontSize = 9 };
			//    autoScroll.Checked += new RoutedEventHandler(autoScroll_Checked);
			//    autoScroll.Unchecked += new RoutedEventHandler(autoScroll_Unchecked);
			//    Box p = Parent as Box;
			//    p.ApplyTemplate();
			//    Grid grid = p.LayoutRoot as Grid;
			//    grid.Children.Add(b);
			//    p.UpdateLayout();
			//} catch { }
		}

		void translucent_Unchecked(object sender, RoutedEventArgs e) {
			if (trans == 0 && Parent.Opacity < 1) trans = Parent.Opacity;
			Parent.Opacity = 1.0;
		}
		double trans = 0.0;

		void translucent_Checked(object sender, RoutedEventArgs e) {
			Parent.Opacity = (trans > 0) ? trans : .6;
		}

		public bool Transparent {
			get { return translucent.IsChecked.Value; }
			set { translucent.IsChecked = value; }
		}

		public bool ShowExpandLogsButton {
			get { return grid.ColumnDefinitions[1].Width.Value > 0; }
			set { grid.ColumnDefinitions[1].Width = new GridLength((value) ? 16 : 0); }
		}


		void stats_SizeChanged(object sender, SizeChangedEventArgs e) {
			//need to try OnApplyTemplate text changed...
			double h = Math.Max(stats.DesiredSize.Height, stats.ActualHeight) + stats.Margin.Top + stats.Margin.Bottom;
			double w = Math.Max(stats.DesiredSize.Width, stats.ActualWidth) + stats.Margin.Left + stats.Margin.Right;
			grid.RowDefinitions[0].MinHeight = (h > 54) ? h : 54;
			grid.ColumnDefinitions[2].MinWidth = (w > 150) ? w : 150;
		}

		void showLogs_Click(object sender, RoutedEventArgs e) {
			ExpandLogs = !ExpandLogs;
		}

		void autoScroll_Unchecked(object sender, RoutedEventArgs e) {
			if (text != null) text.AutoSelect = (bool)autoScroll.IsChecked;
		}

		void autoScroll_Checked(object sender, RoutedEventArgs e) {
			if (text != null) text.AutoSelect = (bool)autoScroll.IsChecked;
		}

		/// <summary>
		/// Autoselect all text to make it easier for someone to copy the text - siolverlight doesn't give access to the clipboard
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected internal void OnTextGotFocus(object sender, RoutedEventArgs e) {
			text.SelectAll();
		}

		/// <summary>
		/// the source for our items.  It is expected to be a LogCollection in this case.
		/// </summary>
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

		[ScriptableMember]
		public event ElementListEventHandler NewEntry;

		void LogViewer_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
			if (text == null) return;
			foreach (object o in e.NewItems) {
				OutputEntry oe = o as OutputEntry;
				if (oe != null) {
					lines++;
					TimeSpan interval = (oe.Timestamp - start);

					string s = string.Format("\r\n{0}:{1}.{2}  {3} {4}: {5} {6}",
						((int)interval.TotalMinutes).ToString().PadLeft(2, '0'),
						interval.Seconds.ToString().PadLeft(2, '0'),
						interval.Milliseconds.ToString().PadLeft(3, '0'),
						oe.Source, oe.OutputType, oe.Description, oe.ExtendedDesc ?? "");

					logText.Append(s);

					if(NewEntry!=null) {
							NewEntry(this, new ElementListEventArgs() { Text = s, Entry = oe });
					}
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

		public string Stats {
			get { return stats.Text; }
			set { 
				stats.Text = value;
				if (StatsChanged != null) {
					StatsChanged(this, new ElementListEventArgs() {Text = value});
				}
				stats_SizeChanged(this, null);
			}
		}

		public bool ExpandLogs {
			get { return text.Visibility == Visibility.Visible; }
			set {
				if (value) {
					grid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
					grid.RowDefinitions[1].Height = new GridLength(9, GridUnitType.Star);
					subgrid.Visibility = text.Visibility = Visibility.Visible;
					Parent.VerticalAlignment = VerticalAlignment.Stretch;
					Parent.HorizontalAlignment = HorizontalAlignment.Stretch;
					((RotateTransform) path.RenderTransform).Angle = 180;
					log.Output(OutputType.Debug, "Opening logs.");

				} else {

					grid.ColumnDefinitions[0].Width = new GridLength(0);
					grid.RowDefinitions[1].Height = new GridLength(0); 

					Parent.VerticalAlignment = VerticalAlignment.Top;
					Parent.HorizontalAlignment = HorizontalAlignment.Right;
					subgrid.Visibility = text.Visibility = Visibility.Collapsed;
					((RotateTransform) path.RenderTransform).Angle = 0;
				}
			}
		}
	}

	public class TextScrollBox : TextBox {
		public TextScrollBox() { AutoSelect = true; }

		//public event EventHandler TextChanged;

		public override void OnApplyTemplate() {
			base.OnApplyTemplate();
			this.TextChanged += new TextChangedEventHandler(TextScrollBox_TextChanged);
			this.LayoutUpdated += new EventHandler(TextScrollBox_LayoutUpdated);
			((Border)GetTemplateChild("ReadOnlyVisualElement")).Opacity = 0;
		}

		void TextScrollBox_LayoutUpdated(object sender, EventArgs e) {
		//	if (AutoSelect) {
				//ScrollToLeft();
		//	}
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
			//if (AutoSelect) {
			//	SelectAll();
			//}
		}

		void TextScrollBox_TextChanged(object sender, TextChangedEventArgs e) {
			//ScrollToBottom();
			if (AutoSelect) {
				SelectAll();
			}

			//if (TextChanged != null) TextChanged(this, null);
		}

		public void ScrollToBottom() {
			ScrollViewer scp = GetTemplateChild("ContentElement") as ScrollViewer;
			if (scp != null) {
				scp.ScrollToVerticalOffset(scp.ExtentHeight);
				//scp.ScrollToHorizontalOffset(0);
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
