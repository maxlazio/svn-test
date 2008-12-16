using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace org.OpenVideoPlayer.Controls {
	public class MenuItem : ListBoxItem {
		public MenuItem() {
			DefaultStyleKey = GetType();
		}

		public event RoutedEventHandler Click;
		public event RoutedEventHandler CheckedChanged;

		public override void OnApplyTemplate() {
			base.OnApplyTemplate();

			layoutRoot = GetTemplateChild("layoutRoot") as Panel;
			if (text == null) text = GetTemplateChild("text") as TextBlock;
			if (checkedIcon == null) checkedIcon = GetTemplateChild("checkedIcon") as FrameworkElement;
			if (subMenuIcon == null) subMenuIcon = GetTemplateChild("subMenuIcon") as FrameworkElement;
			if (button == null) button = GetTemplateChild("button") as ContentControl;

			if (layoutRoot != null) {
				if (!layoutRoot.Children.Contains(button)) {
					if (button.Parent != null) ((Panel)button.Parent).Children.Remove(button);
					layoutRoot.Children.Add(button);
				}
			}

			foreach (FrameworkElement fe in layoutRoot.Children) {
				if (fe is Menu) {
					subMenu = fe as Menu;
					break;
				}
			}

			//BindTemplate(this, GetTemplateChild);
			text.Text = t;
			subMenu.Target = this;
			subMenu.Name = text.Text;
			subMenu.ItemCheckedChanged += (subMenu_ItemCheckedChanged);
			subMenu.ItemClick += (subMenu_ItemClick);
			subMenu.Items = items;

			subMenu.ApplyTemplate();
		}

		protected TextBlock text;
		protected Menu subMenu;
		protected FrameworkElement checkedIcon;
		protected FrameworkElement subMenuIcon;
		protected Panel layoutRoot;
		protected ContentControl button;

		public bool CheckOnClick { get; set; }
		private bool check;
		public bool Checked {
			get { return check; }
			set {
				if (check != value) {
					check = value;
					if (CheckedChanged != null) CheckedChanged(this, new RoutedEventArgs());
				}
				checkedIcon.Visibility = (Checked && SubMenu.Items.Count == 0) ? Visibility.Visible : Visibility.Collapsed;
			}
		}

		void subMenu_ItemClick(object sender, RoutedEventArgs e) {
			if (Click != null) Click(sender, new RoutedEventArgs());
		}

		void subMenu_ItemCheckedChanged(object sender, RoutedEventArgs e) {
			if (CheckedChanged != null) CheckedChanged(sender, new RoutedEventArgs());
		}

		public Menu SubMenu { get { return subMenu; } }
		public static readonly DependencyProperty SubMenuProperty = DependencyProperty.Register("SubMenu", typeof(Menu), typeof(MenuItem), new PropertyMetadata(null));

		public bool RadioMode { get; set; }
		public static readonly DependencyProperty RadioModeProperty = DependencyProperty.Register("RadioMode", typeof(bool), typeof(Menu), new PropertyMetadata(null));

		public Panel LayoutRoot {
			get { return layoutRoot; }
		}

		public ContentControl Button {
			get { return button; }
			set { button = value; }
		}
		public static readonly DependencyProperty ButtonProperty = DependencyProperty.Register("Button", typeof(ContentControl), typeof(MenuItem), new PropertyMetadata(null));

		private string t;
		public string Text {
			get { return (text != null) ? text.Text : null; }
			set {
				if (text != null) text.Text = value;
				if (subMenu != null) subMenu.Name = value;
				t = value;
			}
		}

		public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(MenuItem), new PropertyMetadata(null));

		private ObservableCollection<MenuItem> items = new ObservableCollection<MenuItem>();
		public ObservableCollection<MenuItem> Items {
			get { return items; }
		}
		public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register("Items", typeof(ObservableCollection<MenuItem>), typeof(Menu), new PropertyMetadata(null));

		public Menu Menu { get; set; }

		public void DoAction() {
			//invoke delegate..
			if (Click != null) Click(this, new RoutedEventArgs());
		}

		internal void Showing() {
			if (SubMenu == null) return;
			SubMenu.RadioMode = RadioMode;
			if (subMenuIcon != null) subMenuIcon.Visibility = (SubMenu.Items.Count > 0) ? Visibility.Visible : Visibility.Collapsed;
		}


	}
}
