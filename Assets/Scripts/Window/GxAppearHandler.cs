using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kit2;
namespace Gaia
{

	public abstract class GxAppearHandler : GxWinPart
	{
		public enum eState
		{
			Invalid = 0,
			Appearing = 1,
			Appeared = 2,
			Disappearing = 10,
			Disappeared = 11,
		}

		public event System.Action<eState> EVENT_StateChanged;
		private eState m_State = eState.Invalid;
		public eState state
		{
			get => m_State;
			set
			{
				if (m_State == value)
					return;
				switch (value)
				{
					case eState.Invalid:
					break;
					case eState.Appearing:
					if (m_State == eState.Appeared) throw new System.Exception();
					if (m_State == eState.Disappearing) _EndDisappeared();
					_StartAppearing();
					break;
					case eState.Appeared:
					if (m_State == eState.Disappearing) _EndDisappeared();
					if (m_State == eState.Disappeared) _StartAppearing();
					EndAppeared();
					break;
					case eState.Disappearing:
					if (m_State == eState.Disappeared) throw new System.Exception();
					if (m_State == eState.Appearing) EndAppeared();
					StartDisappearing();
					break;
					case eState.Disappeared:
					if (m_State == eState.Appearing) EndAppeared();
					if (m_State == eState.Appeared) StartDisappearing();
					_EndDisappeared();
					break;
					default:
					throw new System.NotImplementedException();
				}
				m_State = value;
				EVENT_StateChanged.TryCatchDispatchEventError(o => o.Invoke(m_State));
			}
		}

		private void Update()
		{
			if (state == eState.Appearing)
			{
				var alive = InternalAppearing();
				if (!alive)
					state = eState.Appeared;
			}
			if (state == eState.Disappearing)
			{
				var alive = InternalDisappearing();
				if (!alive)
					state = eState.Disappeared;
			}
		}

		[ContextMenu("Appear")]
		public void Appear()
		{
			if (state == eState.Disappeared ||
				state == eState.Invalid)
			{
				state = eState.Appearing;
			}
		}

		[ContextMenu("Disappear")]
		public void Disappear()
		{
			if (state != eState.Appeared)
				return;
			state = eState.Disappearing;
		}

		private void _StartAppearing()
		{
			StartAppearing();
		}
		protected virtual void StartAppearing() { }
		protected abstract bool InternalAppearing();
		protected virtual void EndAppeared() { }

		protected virtual void StartDisappearing() { }
		protected abstract bool InternalDisappearing();
		private void _EndDisappeared()
		{
			EndDisappeared();
		}
		protected virtual void EndDisappeared() { }
	}
}