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
using org.OpenVideoPlayer.Util;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Windows.Controls.Theming;

namespace org.OpenVideoPlayer.Controls {
	public class ControlBase : ContentControl {

		public ControlBase() {
			DefaultStyleKey = this.GetType();
		}

		protected OutputLog log = new OutputLog("Control");

		public event RoutedEventHandler TemplateBound;

		protected bool BindFields = false;
		protected bool BindEvents = false;
		protected bool BindRecursive = false;

		public override void OnApplyTemplate() {
			try {
				base.OnApplyTemplate();

				BindTemplate();//, GetTemplateChild);
				if (TemplateBound != null) TemplateBound(this, new RoutedEventArgs());

			} catch (Exception ex) {
				log.Output(OutputType.Error, "Error in Apply Template", ex);
			}
		}

		public void ApplyTheme(Uri uri, bool recursive) {
			ApplyThemeToElement(this, uri, recursive);
		}

		public static void ApplyThemeToElement(FrameworkElement element, Uri uri, bool recursive) {
			if (element == null) return;

			if (recursive) {
				if (element is Panel) {
					foreach (FrameworkElement e in ((Panel)element).Children) {
						ApplyThemeToElement(e, uri, recursive);
					}
				}
				if (element is ContentControl) {
					ApplyThemeToElement(((ContentControl)element).Content as FrameworkElement, uri, recursive);
				}
				if (element is Border) {
					ApplyThemeToElement(((Border)element).Child as FrameworkElement, uri, recursive);
				}
			}
			ImplicitStyleManager.SetResourceDictionaryUri(element, uri);
			ImplicitStyleManager.SetApplyMode(element, ImplicitStylesApplyMode.Auto);
			ImplicitStyleManager.Apply(element);
		}

		/// <summary>
		/// Binds all the internal fields of the object into the template, field name must match xaml name.  
		/// Events will also be bound if they are internal, start with 'On' and contain both the name of the field, and the name of the event 
		/// </summary>
		protected void BindTemplate() {
			if (!BindFields) return;

			DateTime start = DateTime.Now;
			//use reflection to eliminate all that biolerplate binding code.
			//NOTE - field names must match element names in the xaml for binding to work!
			BindingFlags flags = (BindRecursive) ? BindingFlags.Instance | BindingFlags.NonPublic : BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
			FieldInfo[] fields = GetType().GetFields(flags);
			MethodInfo[] mi = GetType().GetMethods(flags);
			List<string> handlers = new List<string>(); //prefilter this list
			foreach (MethodInfo m in mi) if (m.IsAssembly && m.Name.StartsWith("On") && (!BindRecursive||m.DeclaringType.IsSubclassOf(typeof(ControlBase)))) handlers.Add(m.Name.ToLower());

			int fc = 0, ec = 0;
			foreach (FieldInfo fi in fields) {
				if ((fi.FieldType.Equals(typeof(FrameworkElement)) || fi.FieldType.IsSubclassOf(typeof(FrameworkElement))) || fi.FieldType.IsInterface) {
					//this points to the protected method to get a child from template.
					object o = GetTemplateChild(fi.Name);//GetChildDlg(fi.Name);//  dlg(fi.Name);
					//preload all child templates
					if (o is ControlBase) ((ControlBase)o).ApplyTemplate();

					if (!fi.IsAssembly && !fi.IsFamilyOrAssembly) continue;//only works with internal

					if (o != null && (o.GetType().Equals(fi.FieldType) || o.GetType().IsSubclassOf(fi.FieldType) || (fi.FieldType.IsInterface && ReflectionHelper.TypeHasInterface(o.GetType(),fi.FieldType))) && fi.GetValue(this) == null) {
						fi.SetValue(this, o);
						fc++;
						if (BindEvents) {
							EventInfo[] ei = fi.FieldType.GetEvents();

							//dynamically bind events - the geeky way
							foreach (EventInfo e in ei) {
								string meth = null;
								try {
									foreach (string method in handlers) {
										meth = method;
										if (method.Contains(fi.Name.ToLower()) && method.Contains(e.Name.ToLower())) {
											Delegate eh = Delegate.CreateDelegate(e.EventHandlerType, this, method, true, true);
											e.AddEventHandler(o, eh);
											ec++;
											//Debug.WriteLine("Bound Event: " + e.Name + " to " + method);
										}
									}
								} catch (Exception ex) {
									Debug.WriteLine("Error binding event " + this.Name + "." + fi.Name + "." + e.Name + ", " + e.EventHandlerType + " to " + meth ?? "NULL" + ex);
								}
							}
						}

					} else {
						Debug.WriteLine(string.Format("No template match for: {0}, {1}", fi.Name, fi.FieldType));
					}
				}
			}

			Debug.WriteLine(Name + ", Type " + GetType() + " has bound to " + fc + " fields, " + ec + " events in " + (DateTime.Now - start).TotalMilliseconds + "ms");
		}


	}
}
