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
using System.Reflection;
using System.Collections.Generic;

namespace org.OpenVideoPlayer.Util {
	public static class ReflectionHelper {

		public static object GetValue(object target, string name) {
			if (target == null || string.IsNullOrEmpty(name)) return null;

			int i = name.IndexOf('.');
			string next = (i > 0 && name.Length > i) ? name.Substring(i + 1) : null;
			if (i > 0) name = name.Substring(0, i);
			object value = GetValueInternal(target, name);
			return (next == null) ? value : GetValue(value, next);
		}

		private static object GetValueInternal(object target, string name) {
			object value = null;
			MemberInfo[] mi = null;

			if (target is Type) {
				mi = ((Type)target).GetMember(name, BindingFlags.Public | BindingFlags.Default | BindingFlags.Static | BindingFlags.IgnoreCase);
				target = null;
			} else {
				mi = target.GetType().GetMember(name, BindingFlags.Public | BindingFlags.Default | BindingFlags.IgnoreCase | BindingFlags.Instance);
			}

			if (mi != null && mi.Length > 0) {
				if (mi[0].MemberType == MemberTypes.Property) {
					value = ((PropertyInfo)mi[0]).GetValue(target, null);

				} else if (mi[0].MemberType == MemberTypes.Field) {
					value = ((FieldInfo)mi[0]).GetValue(target);

				} else if (mi[0].MemberType == MemberTypes.Method) {
					value = ((MethodInfo)mi[0]).Invoke(target, null);
				}
			}
			return value;
		}

		public static bool SetValue(object target, string name, object value) {
			int i = name.LastIndexOf('.');
			if (i > 0 && name.Length > i) {
				target = GetValue(target, name.Substring(0, i));
				name = name.Substring(i + 1);
			}

			return SetValueInternal(target, target as Type, name, value);
		}

		public static bool SetValue(Type target, string name, object value) {
			int i = name.LastIndexOf('.');
			object val = target;
			if (i > 0 && name.Length > i) {
				val = GetValue(val, name.Substring(0, i));
				name = name.Substring(i + 1);
			}

			return SetValueInternal(val, val as Type, name, value);
		}

		private static bool SetValueInternal(object target, Type type, string name, object value) {
			MemberInfo[] mi = null;
			if (type != null) {
				mi = type.GetMember(name, 
					MemberTypes.Property | MemberTypes.Field | MemberTypes.Method, 
					BindingFlags.Public | BindingFlags.Default | BindingFlags.Static | BindingFlags.IgnoreCase);
				target = null;

			} else if (target != null) {
				mi = target.GetType().GetMember(name, BindingFlags.Public | BindingFlags.Default | BindingFlags.IgnoreCase | BindingFlags.Instance);
			}

			if (mi != null && mi.Length > 0) {
				if (mi[0].MemberType == MemberTypes.Property) {
					PropertyInfo pi = (PropertyInfo)mi[0];
					Type pt = pi.PropertyType;
					try {
						if(!pt.IsInstanceOfType(value)) {
							pi.SetValue(target, Conversion.Change(value, pt), null);
						}else {
							pi.SetValue(target, value, null);
						}
					} catch {
						pi.SetValue(target, value, null);
					}
					return true;

				} else if (mi[0].MemberType == MemberTypes.Field) {
					FieldInfo fi = (FieldInfo)mi[0];
					Type ft = fi.FieldType;
					try {
						if(!fi.FieldType.IsInstanceOfType(value)) {
							fi.SetValue(target, Conversion.Change(value, ft));
						}else {
							fi.SetValue(target, value);
						}
					} catch {
						fi.SetValue(target, value);
					}
					return true;


				} else if (mi[0].MemberType == MemberTypes.Method) {
					if (value is object[]) {
						//todo - adapt types
						((MethodInfo)mi[0]).Invoke(target, value as object[]);
						return true;
					} else {
						((MethodInfo) mi[0]).Invoke(target, new object[] {value});
						return true;
					}
				}
			}
			return false;
		}

		public static void AttachEvent(object target, string eventName, object handler, string method) {
			Type targetType = (target is Type) ? (Type)target : target.GetType();
			EventInfo e = targetType.GetEvent(eventName);
			Delegate eh = Delegate.CreateDelegate(e.EventHandlerType, handler, method, true, true);
			e.AddEventHandler(target, eh);
		}

		public static Version GetAssemblyVersion() {
			string fn = Assembly.GetCallingAssembly().FullName;
			if (fn != null) {
				int iv = fn.IndexOf("Version=");
				return new Version(fn.Substring(iv + 8, fn.IndexOf(" ", iv + 1) - iv - 9));
			}
			return new Version(1, 0);
		}

		public static bool TypeHasInterface(Type type, Type iface) {
			return (new List<Type>(type.GetInterfaces())).Contains(iface);
		}
	}
}
