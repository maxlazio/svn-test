using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using org.OpenVideoPlayer.Util;
using System.Windows.Shapes;
using System.Reflection;

namespace org.OpenVideoPlayer.Controls {

	/// <summary>
	/// The menu is an extensible menu that hold menuitems, each of which can have their own menu as well, 
	/// or be 'checkable' or radio button style.  The menu can be created 100% through xaml.
	/// </summary>
	public class Menu : ControlBase {

		public Menu() {}

		/// <summary>
		/// Fired when any of this menu's menu items (recursive through submenus) is checked or unchecked.
		/// </summary>
		public event RoutedEventHandler ItemCheckedChanged;
		/// <summary>
		/// Fired when any menuitems in the tree are clicked
		/// </summary>
		public event RoutedEventHandler ItemClick;

		internal ListBox listBox;
		internal Panel layoutRoot;

		public override void OnApplyTemplate() {
			base.OnApplyTemplate();
			//maunally bind - it's faster ad we only have a couple items
			listBox = GetTemplateChild("listBox") as ListBox;
			layoutRoot = GetTemplateChild("layoutRoot") as Panel;

			menuItems.CollectionChanged += OnMenuItemsCollectionChanged;
			listBox.SelectionChanged += OnListBoxSelectionChanged;
			listBox.SizeChanged += OnListBoxSizeChanged;

			//attach to all menuitems
			foreach(MenuItem mi in menuItems) {
				mi.ApplyTemplate();
				mi.Menu = this;
				mi.Click += (mi_Click);
				mi.CheckedChanged += (mi_CheckedChanged);
				listBox.Items.Add(mi);
			}
		}

		#region Properties
		/// <summary>
		/// The direction that menuitems and submenus expand from the target
		/// </summary>
		public MenuExpandModes MenuExpandMode { get; set; }
		public static readonly DependencyProperty ExpandModeProperty = DependencyProperty.Register("ExpandMode", typeof(MenuExpandModes), typeof(Menu), new PropertyMetadata(null));

		/// <summary>
		/// Use radiobutton mode for the checkable menuitems on this node
		/// </summary>
		public bool RadioMode { get; set; }
		public static readonly DependencyProperty RadioModeProperty = DependencyProperty.Register("RadioMode", typeof(bool), typeof(Menu), new PropertyMetadata(null));

		/// <summary>
		/// The menuitems of the next level on the menu tree
		/// </summary>
		public ObservableCollection<MenuItem> Items {
			get { return menuItems; }
			set { menuItems = value; }
		}
		private ObservableCollection<MenuItem> menuItems = new ObservableCollection<MenuItem>();
		public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register("Items", typeof(ObservableCollection<MenuItem>), typeof(Menu), new PropertyMetadata(null));

		/// <summary>
		/// The control we are targetted toward for positioning
		/// </summary>
		public Control Target {
			get { return target; }
			set {target = value;}
		}
		private Control target;
		public static readonly DependencyProperty TargetProperty = DependencyProperty.Register("Target", typeof(Control), typeof(Menu), new PropertyMetadata(null));

		/// <summary>
		/// The Menu at the base of our tree
		/// </summary>
		private Menu Root {
			get {
				Menu root = this;
				while ((root.Target as MenuItem) != null) {
					root = ((MenuItem)root.Target).Menu;
				}
				return root;
			}
		}

		public ListBox ListBox { get { return listBox; } }
		#endregion

		#region Menu Methods
		/// <summary>
		/// Shows this menu
		/// </summary>
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

		/// <summary>
		/// Hide this portion of the menu
		/// </summary>
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

		/// <summary>
		/// Toggles hide/show of the portion of the menu
		/// </summary>
		public void Toggle() {
			if (Visibility == Visibility.Collapsed) {
				Show();
			} else {
				Hide();
			}
		}

		/// <summary>
		/// Sets the check state of a menu item by name
		/// </summary>
		/// <param name="text"></param>
		/// <param name="check"></param>
		/// <returns></returns>
		public bool SetCheckState(string text, bool check) { return SetState(null, text, check, MenuItemStateType.Checked); }

		/// <summary>
		/// Sets checkstate of a menuitem by it's tag
		/// </summary>
		/// <param name="tag"></param>
		/// <param name="check"></param>
		/// <returns></returns>
		public bool SetCheckState(object tag, bool check) { return SetState(tag, null, check, MenuItemStateType.Checked); }

		/// <summary>
		/// Sets the enabled value of a menuitem by name
		/// </summary>
		/// <param name="text"></param>
		/// <param name="enabled"></param>
		/// <returns></returns>
		public bool SetEnabled(string text, bool enabled) { return SetState(null, text, enabled, MenuItemStateType.Enabled); }

		/// <summary>
		/// Sets the enabled value of a menuitem by tag
		/// </summary>
		/// <param name="tag"></param>
		/// <param name="enabled"></param>
		/// <returns></returns>
		public bool SetEnabled(object tag, bool enabled) { return SetState(tag, null, enabled, MenuItemStateType.Enabled); }

		/// <summary>
		/// A base method for use by all of the setcheckstate and setenabled methods
		/// </summary>
		/// <param name="tag"></param>
		/// <param name="text"></param>
		/// <param name="state"></param>
		/// <param name="type"></param>
		/// <returns></returns>
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

		/// <summary>
		/// only needed internally for the setstate method
		/// </summary>
		protected enum MenuItemStateType {
			Enabled,
			Checked
		}

		#endregion

		#region Positioning

		/// <summary>
		/// Gets height of items to left of our parent item for positioning
		/// </summary>
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

		/// <summary>
		/// Gets height of items higher than our parent item for positioning
		/// </summary>
		protected double TopLevel {
			get {
				double top = 0;
				foreach (MenuItem mi in ((MenuItem)Target).Menu.Items) {
					if (mi == Target) break;
					top += mi.ActualHeight;
				}
				return top;
			}
		}

		/// <summary>
		/// The trickiest part of the menu, figuring how to position ourselves, in relation to our parent
		/// </summary>
		protected void PositionListbox() {
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

		/// <summary>
		/// Make sure we keep in sync if items are changed
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
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

		/// <summary>
		/// Route the menuitem events back through, so consumers don't need to attach to individual items
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void mi_CheckedChanged(object sender, RoutedEventArgs e) {
			if (ItemCheckedChanged != null) ItemCheckedChanged(sender, e);
		}

		/// <summary>
		/// Route the menuitem events back through, so consumers don't need to attach to individual items
		/// </summary>
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
	}

	/// <summary>
	/// The direction to expand new nodes of the menu
	/// </summary>
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
