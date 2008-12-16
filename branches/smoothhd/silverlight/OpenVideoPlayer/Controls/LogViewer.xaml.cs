using System.Collections;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using org.OpenVideoPlayer.Util;

namespace org.OpenVideoPlayer.Controls {
	public partial class LogViewer : UserControl {
		//private LogCollection logs = new LogCollection();

		public LogViewer() {
			InitializeComponent();

			//LogList.ItemsSource = logs;
			//logs.Add(new OutputEntry(DateTime.Now, "Test Source", OutputType.Error, "Something happened!", "And it was really really bad!"));
			//logs.Add(new OutputEntry(DateTime.Now, "Player", OutputType.Info, "Something else happened!", "And it was really really bad!"));
			//logs.Add(new OutputEntry(DateTime.Now, "PArser", OutputType.Debug, "Something happened!", "And it was really really bad!"));
			//logs.Add(new OutputEntry(DateTime.Now, "Your mom?", OutputType.Critical, "asddfhfg hfg h happened!", "And it was really really bad!"));
		}

		public IEnumerable ItemsSource {
			get { return LogList.ItemsSource; }
			set {
				LogList.ItemsSource = value;
				if(value is LogCollection) {
					((LogCollection)value).CollectionChanged += LogViewer_CollectionChanged;
				}
			}
		}

		void LogViewer_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
			if(LogList.SelectedIndex >= ((LogCollection)LogList.ItemsSource).Count -2) {
				//object o = ((LogCollection) LogList.ItemsSource)[((LogCollection) LogList.ItemsSource).Count - 1];
				LogList.SelectedIndex = ((LogCollection)LogList.ItemsSource).Count - 1;
				LogList.ScrollIntoView(LogList.SelectedItem);
			}
		}
	}

	public class LogCollection : ObservableCollection<OutputEntry> {

		protected override void InsertItem(int index, OutputEntry item) {
			item.OwnerCollection = this;
			base.InsertItem(index, item);
		}

		protected override void RemoveItem(int index) {
			this[index].OwnerCollection = null;
			base.RemoveItem(index);
		}

		protected Visibility extended = Visibility.Collapsed;
		public Visibility Extended{
			get{return extended;}
			set { extended = value; }
		}
	}
}
