using System;
using System.Collections;
using System.Reflection;
using System.Collections.Generic;

namespace CommonEventHandler {
	/// <summary>
	/// This delegate describes the signature of the event, which is emited by the generated helper classes.
	/// </summary>
	public delegate void CommonEventHandlerDlgt(Type EventType, object[] args);

	/// <summary>
	/// Summary description for EventHandlerFactory.
	/// </summary>
	public class EventHandlerFactory {
		private static Dictionary<string, object> eventHandlers = new Dictionary<string, object>();
		private EventHandlerTypeEmitter emitter;
		private string helperName;

		/// <summary>
		/// Creates the EventHandlerFactory.
		/// </summary>
		/// <param name="Name">Name of the Factory. Is used as naming component of the event handlers to create.</param>
		public EventHandlerFactory(string Name) {
			helperName = Name;
			emitter = new EventHandlerTypeEmitter(Name);
		}

		/// <summary>
		/// Creates an event handler for the specified event
		/// </summary>
		/// <param name="Info">The event info class of the event</param>
		/// <returns>The created event handler help class.</returns>
		public object GetEventHandler(EventInfo Info) {
			string handlerName = helperName + Info.Name;
			object eventHandler = eventHandlers.ContainsKey(handlerName) ? eventHandlers[handlerName] : null;
			if (eventHandler == null) {
				Type eventHandlerType = emitter.GetEventHandlerType(Info);

				// Call constructor of event handler type to create event handler
				ConstructorInfo myCtor = eventHandlerType.GetConstructor(new Type[] { typeof(EventInfo) });
				object[] ctorArgs = new object[] { Info };
				eventHandler = myCtor.Invoke(ctorArgs);

				eventHandlers.Add(handlerName, eventHandler);
			}
			return eventHandler;
		}
	}
}
