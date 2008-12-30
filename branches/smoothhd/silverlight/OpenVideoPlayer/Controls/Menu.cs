using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using org.OpenVideoPlayer.Util;
using System.Windows.Shapes;
using System.Reflection;

namespace org.OpenVideoPlayer.Controls {

	public class Menu : ControlBase {

		public Menu() {
			//DefaultStyleKey = GetType();
			//InitializeComponent();
		}

		public event RoutedEventHandler ItemCheckedChanged;
		public event RoutedEventHandler ItemClick;

		internal ListBox listBox;
		internal Panel layoutRoot;

		public override void OnApplyTemplate() {
			BindFields = false;
			base.OnApplyTemplate();
			listBox = GetTemplateChild("listBox") as ListBox;
			layoutRoot = GetTemplateChild("layoutRoot") as Panel;

			//BindTemplate(this, GetTemplateChild);

			menuItems.CollectionChanged += OnMenuItemsCollectionChanged;
			listBox.SelectionChanged += OnListBoxSelectionChanged;
			listBox.SizeChanged += OnListBoxSizeChanged;

			foreach(MenuItem mi in menuItems) {
				mi.ApplyTemplate();
				mi.Menu = this;
				mi.Click += (mi_Click);
				mi.CheckedChanged += (mi_CheckedChanged);
				listBox.Items.Add(mi);
			}
		}

		#region Properties
		public MenuExpandModes MenuExpandMode { get; set; }
		public static readonly DependencyProperty ExpandModeProperty = DependencyProperty.Register("ExpandMode", typeof(MenuExpandModes), typeof(Menu), new PropertyMetadata(null));

		public bool RadioMode { get; set; }
		public static readonly DependencyProperty RadioModeProperty = DependencyProperty.Register("RadioMode", typeof(bool), typeof(Menu), new PropertyMetadata(null));

		private ObservableCollection<MenuItem> menuItems = new ObservableCollection<MenuItem>();
		public ObservableCollection<MenuItem> Items {
			get { return menuItems; }
			set { menuItems = value; }
		}
		public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register("Items", typeof(ObservableCollection<MenuItem>), typeof(Menu), new PropertyMetadata(null));

		private Control target;
		public Control Target {
			get { return target; }
			set {
				target = value;
			}
		}
		public static readonly DependencyProperty TargetProperty = DependencyProperty.Register("Target", typeof(Control), typeof(Menu), new PropertyMetadata(null));

		private Menu Root {
			get {
				Menu root = this;
				while ((root.Target as MenuItem) != null) {
					root = ((MenuItem)root.Target).Menu;
				}
				return root;
			}
		}

		//public Panel LayoutRoot { get { return layoutRoot; } }
		public ListBox ListBox { get { return listBox; } }
		#endregion

		#region Menu Methods
		public void Show() {
			if (Visibility != Visibility.Collapsed) return;

			//hide any of our parent's sibling menus
			if (Target is MenuItem) {
				foreach (MenuItem mi in ((MenuItem)Target).Menu.Items) {
					if (mi != Target) mi.SubMenu.Hide();
				}
			}

			//show ourselves
			Visibility = Visibility.Visible;

			bool check = false;
			//trickle down the current expandmode
			foreach (MenuItem mi in Items) {
				if(mi.SubMenu!=null) mi.SubMenu.MenuExpandMode = MenuExpandMode;
				if (mi.Checked) check = true;
				mi.Showing();
			}
			//Make sure at least something is checked in radiomode
			if (RadioMode && !check && Items.Count > 0) Items[0].Checked = true;

			//position our submenu (if available)
			PositionListbox();
		}

		public void Hide() {
			if (Visibility != Visibility.Visible) return;

			Visibility = Visibility.Collapsed;
			//also hide anything higher on the tree
			foreach (MenuItem m in Items) if (m.SubMenu != null) m.SubMenu.Hide();

			if (listBox == null) return;
			//makes sure selection highlighting doesn't stick..
			MenuItem mi = listBox.SelectedItem as MenuItem;
			if (mi != null) {
				try {
					mi.IsSelected = false;
				} catch {
					Console.WriteLine("");
				} //expected exception.. odd index issue in framework
				if (listBox.SelectedIndex > -1) listBox.SelectedIndex = -1;
			}
		}

		public void Toggle() {
			if (Visibility == Visibility.Collapsed) {
				Show();
			} else {
				Hide();
			}
		}

		public bool SetCheckState(string text, bool check) { return SetState(null, text, check, MenuItemStateType.Checked); }

		public bool SetCheckState(object tag, bool check) { return SetState(tag, null, check, MenuItemStateType.Checked); }

		public bool SetEnabled(string text, bool enabled) { return SetState(null, text, enabled, MenuItemStateType.Enabled); }

		public bool SetEnabled(object tag, bool enabled) { return SetState(tag, null, enabled, MenuItemStateType.Enabled); }

		protected bool SetState(object tag, string text, bool state, MenuItemStateType type) {
			if (menuItems == null) return false;

			foreach (MenuItem mi in menuItems) {
				if ((tag != null && mi.Tag == tag) || (text != null && mi.Text == text)) {
					if (type == MenuItemStateType.Checked) mi.Checked = state;
					if (type == MenuItemStateType.Enabled) mi.IsEnabled = state;
					return true;
				}

				if (mi.SubMenu!=null && mi.SubMenu.Items.Count > 0) {
					if (mi.SubMenu.SetState(tag, text, state, type)) return true;
				}
			}
			return false;
		}

		protected enum MenuItemStateType {
			Enabled,
			Checked
		}

		#endregion

		#region Positioning

		private Double LeftLevel {
			get {
				Double d = listBox.ActualWidth;
				Menu root = this;
				while ((root.Target as MenuItem) != null) {
					root = ((MenuItem)root.Target).Menu;
					d += root.listBox.ActualWidth;
				}
				d -= root.listBox.ActualWidth;
				return d;
			}
		}

		private double TopLevel {
			get {
				//get height of items higher than our parent item
				double top = 0;
				foreach (MenuItem mi in ((MenuItem)Target).Menu.Items) {
					if (mi == Target) break;
					top += mi.ActualHeight;
				}
				return top;
			}
		}

		private void PositionListbox() {
			try {
				//Get the coordinates of the Root target (button, etc)
				Point p = Root.Target.TransformToVisual(Root).Transform(new Point(0, 0));

				//this means we are a submenu
				if (Root.Target != Target) {
					if ((Parent == null || ((Panel)Parent).Children.Contains(this)) && !((Panel)Root.Parent).Children.Contains(this)) {
						if (Parent != null) {
							Debug.WriteLine("Moving " + Name + "from " + ((Panel)Parent).Name + " to " + ((Panel)Root.Parent).Name);
							((Panel)Parent).Children.Remove(this);
						}
						((Panel)Root.Parent).Children.Add(this);
					}

					if (listBox == null) return;

					if ((MenuExpandMode & MenuExpandModes.Left) != 0) {
						listBox.SetValue(Canvas.LeftProperty, (Double)Root.listBox.GetValue(Canvas.LeftProperty) - LeftLevel + listBox.BorderThickness.Right);
					} else {
						listBox.SetValue(Canvas.LeftProperty, (Double)Root.listBox.GetValue(Canvas.LeftProperty) + LeftLevel + listBox.BorderThickness.Right);
					}
					if ((MenuExpandMode & MenuExpandModes.Up) != 0) {
						listBox.SetValue(Canvas.TopProperty, (Double)Root.listBox.GetValue(Canvas.TopProperty) + TopLevel + Target.ActualHeight - listBox.ActualHeight);
					} else {
						listBox.SetValue(Canvas.TopProperty, (Double)Root.listBox.GetValue(Canvas.TopProperty) + TopLevel);
					}

					//in this case we are the root
				} else {
					if (listBox == null) return;
					if ((MenuExpandMode & MenuExpandModes.Left) != 0) {
						listBox.SetValue(Canvas.LeftProperty, p.X - (listBox.ActualWidth - Target.ActualWidth));
					} else {
						listBox.SetValue(Canvas.LeftProperty, p.X);
					}

					if ((MenuExpandMode & MenuExpandModes.Up) != 0) {
						listBox.SetValue(Canvas.TopProperty, p.Y - listBox.ActualHeight);
					} else {
						listBox.SetValue(Canvas.TopProperty, p.Y + Target.ActualHeight);
					}
				}
			} catch (Exception ex) {
				Debug.WriteLine("Error positioning: " + ex);
			}
		}
		#endregion

		#region Event Handlers

		void OnMenuItemsCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
			//sync our colelction with our listbox's items
			if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add || e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Replace) {
				foreach (MenuItem mi in e.NewItems) {
					listBox.Items.Add(mi);
					mi.Menu = this;
					mi.Click += (mi_Click);
					mi.CheckedChanged += (mi_CheckedChanged);
				}
			}

			if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove || e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Replace || e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset) {
				foreach (MenuItem mi in e.OldItems) {
					listBox.Items.Remove(mi);
					mi.Click -= (mi_Click);
					mi.CheckedChanged -= (mi_CheckedChanged);
				}
			}
		}

		void mi_CheckedChanged(object sender, RoutedEventArgs e) {
			if (ItemCheckedChanged != null) ItemCheckedChanged(sender, e);
		}

		void mi_Click(object sender, RoutedEventArgs e) {
			if (ItemClick != null) ItemClick(sender, e);
		}

		//this means an item was clicked
		internal void OnListBoxSelectionChanged(object sender, SelectionChangedEventArgs e) {
			MenuItem mi = e.AddedItems[0] as MenuItem;

			//shouldn't happen
			if (mi == null) {
				Hide();
				return;
			}

			if (mi.SubMenu != null && mi.SubMenu.Items.Count > 0) {
				mi.SubMenu.Show();

			} else {
				Hide();
				Root.Hide();
				if (mi.CheckOnClick) {
					mi.Checked = !mi.Checked;
				}
				if (RadioMode) {
					mi.Checked = true;
					foreach (MenuItem m in Items) if (m != mi) m.Checked = false;
				}

				mi.DoAction();
			}
		}

		internal void OnListBoxSizeChanged(object sender, SizeChangedEventArgs e) {
			PositionListbox();
		}
		#endregion

		/// <summary>
		/// Binds all the protected properties of the object into the template
		/// </summary>
		//public static void BindTemplate(Control sender, org.OpenVideoPlayer.Util.ControlHelper.GetChildDlg dlg) {
		//    //use reflection to eliminate all that biolerplate binding code.
		//    //NOTE - field names must match element names in the xaml for binding to work!
		//    FieldInfo[] fields = sender.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic);

		//    foreach (FieldInfo fi in fields) {
		//        if ((fi.FieldType.Equals(typeof(FrameworkElement)) || fi.FieldType.IsSubclassOf(typeof(FrameworkElement))) && fi.GetValue(sender) == null) {
		//            //object o = sender.GetTemplateChild(fi.Name);
		//            object o = dlg(fi.Name);
		//            if (o != null && (o.GetType().Equals(fi.FieldType) || o.GetType().IsSubclassOf(fi.FieldType))) {
		//                fi.SetValue(sender, o);
		//            } else {
		//                Debug.WriteLine(string.Format("No template match for: {0}, {1}", fi.Name, fi.FieldType));
		//            }
		//        }
		//    }
		//}

	}


	[Flags]
	public enum MenuExpandModes {
		Unknown,
		Left = 1,
		Right = 0,
		Up = 2,
		Down = 0,
		LeftUp = Left | Up,
		LeftDown = Left | Down,
		RightUp = Right | Up,
		RightDown = Right | Down
	}
}
