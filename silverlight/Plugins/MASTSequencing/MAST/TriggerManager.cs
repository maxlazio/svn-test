using System;
using System.Collections.Generic;
using org.OpenVideoPlayer.Util;

namespace org.OpenVideoPlayer.Advertising.MAST {
	/// <summary>
	/// Handles actions for a trigger.  The trigger calls itself is auto generated in the parser from XSD, 
	/// so the functionality was pulled into this class - to allow for schema changes wihtout interfereing with business logic
	/// </summary>
	public class TriggerManager : IDisposable {

		#region members and constructor
		/// <summary>
		/// The trigger we are representing
		/// </summary>
		public Trigger Trigger { get; protected set; }

		/// <summary>
		/// The MAST Interface to the player/system - gives us the properties and events we need
		/// </summary>
		public IMastAdapter MastInterface { get; protected set; }

		/// <summary>
		/// Fires when conditions are right and we are activating trigger
		/// </summary>
		public event EventHandler Activate;
		/// <summary>
		/// Fires when an active trigger is shutting down
		/// So far this can only happen because of external action, or our duration
		/// </summary>
		public event EventHandler Deactivate;

		/// <summary>
		/// Our conditions - only top level, so multiples are evaluated as boolean 'OR'
		/// Child conditions are handled by their parent
		/// </summary>
		public List<ConditionManager> StartConditions = new List<ConditionManager>();

		public List<ConditionManager> EndConditions = new List<ConditionManager>();

		OutputLog Log = new OutputLog("Trigger");

		public bool IsActive { get; set; }

		//private DateTime nextReset = DateTime.MinValue;
		//bool resetNextClip = false;

		public TriggerManager(Trigger trigger, IMastAdapter mastInterface) { 
			if(trigger == null) {
				throw new NullReferenceException("Trigger must not be null.");
			}
			if(mastInterface == null) {
				throw new NullReferenceException("IMastAdapter must not be null.");
			}

			Trigger = trigger;
			MastInterface = mastInterface;

			foreach (Condition c in trigger.startConditions) {
				ConditionManager cm = new ConditionManager(c, MastInterface);
				//We need to wire up events, for event type conditions, and this is also used for some property calculations around time
				//if (c.type == ConditionType.@event) {
				cm.EventFired += new EventHandler(OnConditionEventFired);
				//}
				StartConditions.Add(cm);
			}

			foreach (Condition c in trigger.endConditions) {
				ConditionManager cm = new ConditionManager(c, MastInterface) { IsEndCondition = true };
				//if (c.type == ConditionType.@event) {
				cm.EventFired += new EventHandler(OnConditionEventFired);
				//}
				EndConditions.Add(cm);
			}

			//we just need this for 'per clip' events, to reset the flag
			//ReflectionHelper.AttachEvent(MastInterface, "OnItemEnd", this, "OnItemEnd");
		}
		#endregion

		#region Evaluations

		void OnConditionEventFired(object sender, EventArgs e) {
			ConditionManager cm = sender as ConditionManager;
			if (cm == null) return;

			//make sure this is a top-level condition
			while (cm.ParentCondition != null) cm = cm.ParentCondition;

			//check value, in case there are property-based children
			if (cm.Evaluate()) {
				if (!cm.IsEndCondition) {
					Log.Output(OutputType.Debug, "Fired from event: " + cm.Condition.name);
					ActivateTrigger();
				} else {
					Log.Output(OutputType.Debug, "Ending trigger from event: " + cm.Condition.name);
					DeactivateTrigger();
				}
			}
		}

		/// <summary>
		/// This is called by the engine's timer
		/// </summary>
		public void Evaluate() {
			if (!IsActive) {
				ConditionManager cm = EvaluateConditionList(StartConditions);
					if (cm!=null && ActivateTrigger()) {
						Log.Output(OutputType.Debug, "Fired from Property: " + cm.Condition.name);
					}
			} else {
				ConditionManager cm = EvaluateConditionList(EndConditions);
				if(cm!=null){
					DeactivateTrigger();
					Log.Output(OutputType.Debug, "Ending trigger from Property: " + cm.Condition.name);
					return;
				}
			}
		}

		/// <summary>
		/// Evaluates a list of conditions.  Returns the first condition that evaluates true, or null 
		/// </summary>
		/// <param name="conditions"></param>
		/// <returns></returns>
		private ConditionManager EvaluateConditionList(List<ConditionManager> conditions) {
			foreach (ConditionManager cm in conditions) {
				//only evaluate property based conditions directly, skip event types
				if (cm.Condition.type == ConditionType.@event) continue;

				//this is an OR, so all we need is one of them.
				if (cm.Evaluate()) {
					return cm;
				}
			}

			return null;
		}

		#endregion

		#region Activation and Deactivation
		/// <summary>
		/// TriggerNow fires when our conditions are met based on properties and/or events
		/// </summary>
		protected bool ActivateTrigger() {
			if (/*resetNextClip || */ IsActive) {
				Log.Output(OutputType.Debug, "Trigger firing skipped - reset period has not elapsed or trigger is already active");
				return false;
			}

			if (EvaluateConditionList(EndConditions) != null) {
				Log.Output(OutputType.Debug, "Trigger firing skipped - End conditions were true.");
				return false;
			}

			IsActive = true;
			//resetNextClip = true;

			//we are ready to do our action
			if (Activate != null) {
				Activate(this, new EventArgs());
			}
			return true;
		}

		protected void DeactivateTrigger() {
			IsActive = false;
			if (Deactivate != null) {
				Deactivate(this, new EventArgs());
			}
		}
		#endregion

		#region Other methods

		//public void OnItemEnd(object sender, EventArgs args) {
			//resetNextClip = false;
		//}

		public void Dispose() {
			foreach (ConditionManager cm in StartConditions) {
				cm.Dispose();
			}
			foreach (ConditionManager cm in EndConditions) {
				cm.Dispose();
			}
		}
		#endregion
	}
}