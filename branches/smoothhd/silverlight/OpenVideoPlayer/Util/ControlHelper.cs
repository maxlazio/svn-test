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
using Microsoft.Windows.Controls.Theming;
using System.Reflection;
using System.Diagnostics;

namespace org.OpenVideoPlayer.Util {
	public static class ControlHelper {
		public static void ApplyTheme(FrameworkElement element, Uri uri, bool recursive) {
			if (element == null) return;

			if (recursive) {
				if (element is Panel) {
					foreach (FrameworkElement e in ((Panel)element).Children) {
						ApplyTheme(e, uri, recursive);
					}
				}
				if (element is ContentControl) {
					ApplyTheme(((ContentControl)element).Content as FrameworkElement, uri, recursive);
				}
				if (element is Border) {
					ApplyTheme(((Border)element).Child as FrameworkElement, uri, recursive);
				}
			}
			ImplicitStyleManager.SetResourceDictionaryUri(element, uri);
			ImplicitStyleManager.SetApplyMode(element, ImplicitStylesApplyMode.Auto);
			ImplicitStyleManager.Apply(element);
		}


		public delegate DependencyObject GetChildDlg(string name);

		/// <summary>
		/// Binds all the protected properties of the object into the template
		/// </summary>
		public static void BindTemplate(Control sender, GetChildDlg dlg) {
			//use reflection to eliminate all that biolerplate binding code.
			//NOTE - field names must match element names in the xaml for binding to work!
			FieldInfo[] fields = sender.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic);

			foreach (FieldInfo fi in fields) {
				if ((fi.FieldType.Equals(typeof(FrameworkElement)) || fi.FieldType.IsSubclassOf(typeof(FrameworkElement))) && fi.GetValue(sender) == null) {
					//object o = sender.GetTemplateChild(fi.Name);
					object o = dlg(fi.Name);
					if (o != null && (o.GetType().Equals(fi.FieldType) || o.GetType().IsSubclassOf(fi.FieldType))) {
						fi.SetValue(sender, o);
					} else {
						Debug.WriteLine(string.Format("No template match for: {0}, {1}", fi.Name, fi.FieldType));
					}
				}
			}
		}
	}
}
