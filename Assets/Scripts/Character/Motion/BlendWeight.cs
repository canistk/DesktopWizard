using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kit2.Tasks;
namespace Gaia
{
	/// <summary>
	/// Handling the blending of weights over a specified duration.
	/// </summary>
	public class BlendWeight : MyTaskBase
	{
		private readonly float start, end;
		public bool realTime;
		public float weight { get; private set; } = 0f;
		public float duration { get; private set; } = 0f;
		public BlendWeight(float startWeight01, float targetWeight01, float duration, bool realTime = false)
		{
			this.start = Mathf.Clamp01(startWeight01);
			this.end = Mathf.Clamp01(targetWeight01);
			this.duration = Mathf.Max(0f, duration);
			this.realTime = realTime;
			this.weight = start; // Initialize weight to start value
		}

		protected KeyValuePair<bool, float> m_StartTime = default;

		public bool TrySetRealtime(bool realTime)
		{
			if (this.realTime == realTime)
				return false; // No change
			if (m_StartTime.Key)
				return false; // Cannot change to real-time if already started

			this.realTime = realTime;
			m_StartTime = default; // Reset start time
			return true; // Changed to real-time or game-time
		}

		private float GetTime()
		{
			return realTime ? Time.realtimeSinceStartup : Time.time;
		}

		public bool IsComplete()
		{
			if (duration <= float.Epsilon)
				return true;
			if (!m_StartTime.Key)
				return false; // Not started yet
			var time = GetTime();
			return time - m_StartTime.Value >= duration;
		}

		public override bool Execute()
		{
			if (duration <= float.Epsilon)
			{
				weight = end; // Instant transition
				return false; // Task is complete
			}

			var time = GetTime();

			if (!m_StartTime.Key)
			{
				m_StartTime = new KeyValuePair<bool, float>(true, time);
				weight = start;
			}

			float elapsed = time - m_StartTime.Value;
			if (elapsed >= duration)
			{
				weight = end;
				return false; // Task is complete
			}

			// Interpolate the weight based on elapsed time
			float pt = elapsed / duration;
			weight = Mathf.Lerp(start, end, pt);
			//Debug.Log($"BlendWeight: {weight:F2}, PT={pt:F2}");
			return true;
		}

		public override string ToString()
		{
			return $"Blend duration={duration:F2}";
		}

		public override void Reset()
		{
			base.Reset();
			m_StartTime = default;
			weight = start;
		}
	}
}