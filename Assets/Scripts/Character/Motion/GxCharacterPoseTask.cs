using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DateTime = System.DateTime;
using TimeSpan = System.TimeSpan;
namespace Gaia
{
	// Base class for character pose control task.
	// This should be unique to each character,
	public abstract class GxCharacterPoseTask : GxCharacterTask
	{
		public DateTime start { get; private set; } = DateTime.MinValue;
		public GxCharacterPoseTask(GxCharacter character) : base(character) { }

		public enum eState
		{
			None,
			Entering,
			Updating,
			Exiting,
			Completed
		}
		public eState state { get; private set; } = eState.None;

		protected sealed override bool InternalExecute()
		{
			if (isDisposed || isCompleted)
				return false; // end task
			switch (state)
			{
				case eState.None:
				{
					start = DateTime.UtcNow;
					state = eState.Entering;
					OnEnterPose();
					state = eState.Updating; // set to updating after entering
				}
				break;
				case eState.Entering: throw new System.InvalidOperationException("Should not be in entering state, should be in updating state.");
				case eState.Updating:
				{
					OnPoseUpdate();
				}
				break;
				case eState.Exiting:
				{
					OnExitPose();
					state = eState.Completed;
				}
				break;
				case eState.Completed:
				return false;
				default: throw new System.NotImplementedException($"State {state} is not implemented.");
			}
			return state < eState.Completed;
		}

		public TimeSpan duration
		{
			get
			{
				if (start == DateTime.MinValue)
					return TimeSpan.Zero;
				return DateTime.UtcNow - start;
			}
		}

		protected abstract void OnEnterPose();
		protected abstract void OnPoseUpdate();
		protected abstract void OnExitPose();
	}
}