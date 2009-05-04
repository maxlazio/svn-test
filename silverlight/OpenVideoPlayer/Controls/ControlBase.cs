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
using System.IO;
using System.Windows.Browser;

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

		private int fieldCount = 0, eventCount = 0, nullCount = 0;
		private List<ControlBase> boundItems = new List<ControlBase>();

		private List<FrameworkElement> children = new List<FrameworkElement>();
		public List<FrameworkElement> Children {
			get { return children; }
			set { children = value; }
		}

		public override void OnApplyTemplate() {

			try {
				base.OnApplyTemplate();

				BindTemplate();
				if (TemplateBound != null) TemplateBound(this, new RoutedEventArgs());

			} catch (Exception ex) {
				log.Output(OutputType.Error, "Error in Apply Template", ex);
			}
		}

		public new DependencyObject GetTemplateChild(string name) {
			return base.GetTemplateChild(name);
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
		/// Applyes the theme (found in a xaml resource) to this control, and optionally to any child controls that supoprt it.
		/// </summary>
		/// <param name="uri"></param>
		/// <param name="recursive"></param>
		public void ApplyTheme(Stream uri, bool recursive) {
			ApplyThemeToElement(this, uri, recursive);
		}

		/// <summary>
		/// Applies a theme to a specific control
		/// </summary>
		/// <param name="element"></param>
		/// <param name="uri"></param>
		/// <param name="recursive"></param>
		public static void ApplyThemeToElement(FrameworkElement element, Uri uri, bool recursive) {
			ApplyThemeToElement(element, uri, null, recursive, false);
		}

		/// <summary>
		/// Applies a theme to a specific control
		/// </summary>
		/// <param name="element"></param>
		/// <param name="uri"></param>
		/// <param name="recursive"></param>
		public static void ApplyThemeToElement(FrameworkElement element, Stream s, bool recursive) {
			ApplyThemeToElement(element, null, s, recursive, false);
		}

		protected static void ApplyThemeToElement(FrameworkElement element, Uri uri, Stream s, bool recursive, bool atdepth) {
			if (element == null) return;
			DateTime start = DateTime.Now;
			try {
				if (element is Control) {
					((Control)element).ApplyTemplate();
				}
				if (recursive) {
					if (element is Panel) {
						foreach (FrameworkElement e in ((Panel) element).Children) {
							ApplyThemeToElement(e, uri, s, recursive, true);
						}
					}
					if (element is ContentControl) {
						if (((ContentControl)element).Content is FrameworkElement) {
							ApplyThemeToElement(((ContentControl) element).Content as FrameworkElement, uri, s, recursive, true);
						}
					}
					if (element is ControlBase) {
						//((ControlBase)element)
						foreach (FrameworkElement e in ((ControlBase)element).Children) {
							ApplyThemeToElement(e, uri, s, recursive, true);
						}
					}
					if (element is Border) {
						ApplyThemeToElement(((Border) element).Child as FrameworkElement, uri, s, recursive, true);
					}
				}

				if (uri != null) {
					ImplicitStyleManager.SetResourceDictionaryUri(element, uri);
				} else if (s != null) {
					ImplicitStyleManager.SetResourceDictionaryStream(element, s);
				}

				ImplicitStyleManager.SetApplyMode(element, ImplicitStylesApplyMode.None);
				ImplicitStyleManager.Apply(element);

				if (!atdepth) {
					OutputLog.StaticOutput("ControlBase", OutputType.Info, string.Format("Applyed theme to {0}, {1}ms", element.Name, (DateTime.Now - start).TotalMilliseconds));
				} else {
					//Debug.WriteLine(string.Format("Applyed theme to {0}, {1}ms", element.Name, (DateTime.Now - start).TotalMilliseconds));
				}
			} catch (Exception ex) {
				OutputLog.StaticOutput("ControlBase", OutputType.Error, "Error applying theme: ", ex);
			}
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
			BindingFlags flags = ((ControlBinding & ControlBindingFlags.SearchBaseClasses) != 0) ? BindingFlags.Instance | BindingFlags.NonPublic : BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

			List<MemberInfo> members = new List<MemberInfo>();

			//get fields for the member list
			members.AddRange(GetFields(GetType(), flags));

			//get properties for list
			members.AddRange(GetProperties(GetType(), flags));

			//get methods for use as event handlers			
			List<string> handlers = new List<string>(); //prefilter this list
			if ((ControlBinding & ControlBindingFlags.BindEvents) != 0) {
				MethodInfo[] mi = GetType().GetMethods(flags);
				foreach (MethodInfo m in mi) {
					if (m.IsAssembly && m.Name.StartsWith("On") && (!((ControlBinding & ControlBindingFlags.SearchBaseClasses) != 0) || m.DeclaringType.IsSubclassOf(typeof (ControlBase)))) {
						handlers.Add(m.Name.ToLower());
					}
				}
			}

			List<MemberInfo> unmatched = BindMembers(members, handlers);
			//recuse just once at this point - TODO make truly recursive?
			if (unmatched.Count > 0) {
				BindMembers(unmatched, handlers);
			}

			if (fieldCount + eventCount > 0) {
				Debug.WriteLine(string.Format("{0}, Type {1} has bound to {2} fields, {3} events in {4}ms", Name, GetType(), fieldCount, eventCount, (DateTime.Now - start).TotalMilliseconds));
			}
		}

		private List<MemberInfo> BindMembers(List<MemberInfo> members, List<string> handlers) {
			List<MemberInfo> unmatched = new List<MemberInfo>();

			//foreach (MemberInfo mi in members){
			for(int x = 0; x < members.Count; x++){
				MemberInfo mi = members[x];
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

					//is null? 
					if (o == null) {
						//if recursive, search our already bound items
						if ((ControlBinding & ControlBindingFlags.Recursive) != 0) {
							foreach (ControlBase c in boundItems) {
								if ((o = c.GetTemplateChild(mi.Name)) != null) {
									break;
								}
							}
						}
						//if not recursive or still no match, add to unmatched list and re-loop
						if (o == null) {
							unmatched.Add((fi != null) ? (MemberInfo)fi : (MemberInfo)pi);
							continue;
						}
					}

					if(o is FrameworkElement) children.Add(o as FrameworkElement);

					//preload all child templates
					if (o is ControlBase) {
						((ControlBase) o).ApplyTemplate();
						boundItems.Add(o as ControlBase);
					}

					//make sure the types match
					if (!o.GetType().Equals(itemType) && !o.GetType().IsSubclassOf(itemType) && !(itemType.IsInterface && ReflectionHelper.TypeHasInterface(o.GetType(), itemType))) continue;

					//set the value finally
					if (fi != null) {
						fi.SetValue(this, o);
					} else {
						pi.SetValue(this, o, null);
					}
					fieldCount++;

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
										eventCount++;
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

			return unmatched;
		}

		private List<MemberInfo> GetFields(Type t, BindingFlags flags) {
			List<MemberInfo> members = new List<MemberInfo>();
			if ((ControlBinding & ControlBindingFlags.BindFields) != 0) {
				FieldInfo[] f = t.GetFields(flags);
				foreach (FieldInfo fi in f) {
					//only works with internal, and we only set field that start out null
					if (fi == null || (!fi.IsAssembly && !fi.IsFamilyOrAssembly) || fi.GetValue(this)!=null) continue;
					//add to list
					members.Add(fi);
				}
			}
			return members;
		}

		private List<MemberInfo> GetProperties(Type t, BindingFlags flags) {
			List<MemberInfo> members = new List<MemberInfo>();
			if ((ControlBinding & ControlBindingFlags.BindProperties) != 0) {
				PropertyInfo[] p = t.GetProperties(flags);
				foreach (PropertyInfo pi in p) {
					//todo - check access of property accessors, and we only set field that start out null
					if (pi == null || pi.GetValue(this, null)!=null) continue;
					//add to list
					members.Add(pi);
				}
			}
			return members;
		}
	}

	/// <summary>
	/// Used to control how a ControlBase derived control automatically binds it's xaml template
	/// </summary>
	[Flags]
	public enum ControlBindingFlags{
		/// <summary>
		/// Default - no binding at all - xaml template must be manually bound with GetTemplateChild()
		/// </summary>
		Disabled = 0,
		/// <summary>
		/// Bind xaml template items to like-named internal fields
		/// </summary>
		BindFields = 1,
		/// <summary>
		/// Bind xaml template items to like-named internal properties
		/// </summary>
		BindProperties = 2,
		/// <summary>
		/// Look for matching handlers for events on our members and bind them
		/// </summary>
		BindEvents = 4,
		/// <summary>
		/// Look at members defined on base classes
		/// </summary>
		SearchBaseClasses = 8,
		/// <summary>
		/// Recursively search members of our sucessfully bound members for matches
		/// </summary>
		Recursive = 16
	}
}
