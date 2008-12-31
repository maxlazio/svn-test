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
	/// <summary>
	/// A base class designed for derived CustomControls.  Inhereits from ContentControl to allow for easily adding content in xaml.  
	/// Fully supports dynamic theming
	/// Also has automatic binding of xaml template to internal fields and events (if enabled) 
	/// </summary>
	public class ControlBase : ContentControl {

		public ControlBase() {DefaultStyleKey = this.GetType();}

		protected OutputLog log = new OutputLog("Control");
		protected ControlBindingFlags ControlBinding { get; set; }
		public event RoutedEventHandler TemplateBound;

		public override void OnApplyTemplate() {
			try {
				base.OnApplyTemplate();

				BindTemplate();
				if (TemplateBound != null) TemplateBound(this, new RoutedEventArgs());

			} catch (Exception ex) {
				log.Output(OutputType.Error, "Error in Apply Template", ex);
			}
		}

		/// <summary>
		/// Applyes the theme (found in a xaml resource) to this control, and optionally to any child controls that supoprt it.
		/// </summary>
		/// <param name="uri"></param>
		/// <param name="recursive"></param>
		public void ApplyTheme(Uri uri, bool recursive) {
			ApplyThemeToElement(this, uri, recursive);
		}

		/// <summary>
		/// Applies a theme to a specific control
		/// </summary>
		/// <param name="element"></param>
		/// <param name="uri"></param>
		/// <param name="recursive"></param>
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
		/// Events can also be bound if they are internal, start with 'On' and contain both the name of the field, and the name of the event 
		/// This method is fairly elaborate in order to increase performance - reflection calls are costly
		/// </summary>
		protected void BindTemplate() {
			DateTime start = DateTime.Now;
			//use reflection to eliminate all that boilerplate binding code.
			//NOTE - field names must match element names in the xaml for binding to work!
			BindingFlags flags = ((ControlBinding & ControlBindingFlags.Recursive) != 0) ? BindingFlags.Instance | BindingFlags.NonPublic : BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

			List<MemberInfo> members = new List<MemberInfo>();
			//get fields for the member list
			if ((ControlBinding & ControlBindingFlags.BindFields) != 0) {
				FieldInfo[] f = GetType().GetFields(flags);
				foreach (FieldInfo fi in f) {
					//only works with internal, and we only set field that start out null
					if (fi == null || (!fi.IsAssembly && !fi.IsFamilyOrAssembly) || fi.GetValue(this)!=null) continue;
					//add to list
					members.Add(fi);
				}
			}
			//get properties for list
			if ((ControlBinding & ControlBindingFlags.BindProperties) != 0) {
				PropertyInfo[] p = GetType().GetProperties(flags);
				foreach (PropertyInfo pi in p) {
					//todo - check access of property accessors, and we only set field that start out null
					if (pi == null || pi.GetValue(this, null)!=null) continue;
					//add to list
					members.Add(pi);
				}
			}
			//get methods for use as event handlers			
			List<string> handlers = new List<string>(); //prefilter this list
			if ((ControlBinding & ControlBindingFlags.BindEvents) != 0) {
				MethodInfo[] mi = GetType().GetMethods(flags);
				foreach (MethodInfo m in mi) {
					if (m.IsAssembly && m.Name.StartsWith("On") && (!((ControlBinding & ControlBindingFlags.Recursive) != 0) || m.DeclaringType.IsSubclassOf(typeof (ControlBase)))) {
						handlers.Add(m.Name.ToLower());
					}
				}
			}

			int fc = 0, ec = 0;
			foreach (MemberInfo mi in members) {
				try {
					FieldInfo fi = mi as FieldInfo;
					PropertyInfo pi = mi as PropertyInfo;
					if (pi == null && fi == null) continue;
					//get the items type, regardless of field or property
					Type itemType = (fi != null) ? fi.FieldType : pi.PropertyType;
					//only support items that derive from frameworkelement
					if (!itemType.Equals(typeof (FrameworkElement)) && !itemType.IsSubclassOf(typeof (FrameworkElement)) && !itemType.IsInterface) continue;
					//this points to the protected method to get a child from template.
					object o = GetTemplateChild(mi.Name);
					//preload all child templates
					if (o is ControlBase) ((ControlBase) o).ApplyTemplate();

					//make sure the types match
					if (o == null || (!o.GetType().Equals(itemType) && !o.GetType().IsSubclassOf(itemType) && !(itemType.IsInterface && ReflectionHelper.TypeHasInterface(o.GetType(), itemType)))) continue;

					//set the value finally
					if (fi != null) {
						fi.SetValue(this, o);
					} else {
						pi.SetValue(this, o, null);
					}
					fc++;

					if ((ControlBinding & ControlBindingFlags.BindEvents) != 0) {
						EventInfo[] ei = itemType.GetEvents();

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
								Debug.WriteLine("Error binding event " + this.Name + "." + mi.Name + "." + e.Name + ", " + e.EventHandlerType + " to " + meth ?? "NULL" + ex);
							}
						}
					}

				} catch (Exception ex) {
					Debug.WriteLine("Error binding member " + mi.Name + ", " + ex);
				}
			}
			Debug.WriteLine(Name + ", Type " + GetType() + " has bound to " + fc + " fields, " + ec + " events in " + (DateTime.Now - start).TotalMilliseconds + "ms");
		}
	}

	/// <summary>
	/// Used to control how a ControlBase derived control automatically binds it's xaml template
	/// </summary>
	[Flags]
	public enum ControlBindingFlags{
		Disabled = 0,
		BindFields = 1,
		BindProperties = 2,
		BindEvents = 4,
		Recursive = 8,
	}
}
