using Kit2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
	public class GxFadeInOutHandler : GxAppearHandler
	{
		[System.Serializable]
		private struct FadeInfo
		{
			public float duration;
			public float start { get; private set; }
			public void Start()
			{
				start = Time.realtimeSinceStartup;
			}
			public void Reset()
			{
				start = 0f;
			}

			public float progress
			{
				get
				{
					var p = (Time.realtimeSinceStartup - start) / duration;
					return p;
				}
			}
		}

		[Header("Fading")]
		[SerializeField] private FadeInfo m_FadeIn = new FadeInfo();
		[SerializeField] private FadeInfo m_FadeOut = new FadeInfo();
		[SerializeField] AnimationCurve m_FadeInCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
		[SerializeField] AnimationCurve m_FadeOutCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

		private float m_Opacity = 1f;
		private class OpacityModifier : IPriorityObj
		{
			private GxFadeInOutHandler Owner;
			public OpacityModifier(GxFadeInOutHandler owner)
			{
				this.Owner = owner;
			}
			public float Priority => 1f;

			public object Value => Owner.m_Opacity;

			public int CompareTo(IPriorityObj other) => Priority.CompareTo(other.Priority);
			public bool Equals(IPriorityObj other) => this == other;
		}

		[Header("LOD")]
		[SerializeField] private float m_AppearLOD = 0f;
		[SerializeField] AnimationCurve m_AppearingCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
		[SerializeField] private float m_DisappearLOD = 6f;
		[SerializeField] AnimationCurve m_DisappearCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
		private float LOD
		{
			get => modelView.dwCamera.m_Lod;
			set => modelView.dwCamera.m_Lod = value;
		}
		protected override void StartAppearing()
		{
			m_FadeIn.Start();
			m_Opacity = 0f;
			LOD = m_DisappearLOD;
			modelView.dwCamera.AddOpacityModifier(new OpacityModifier(this));
		}
		protected override bool InternalAppearing()
		{
			if (m_FadeIn.progress < 1f)
			{
				m_Opacity = m_FadeInCurve.Evaluate(Mathf.Clamp01(m_FadeIn.progress));
				LOD = Mathf.Lerp(m_DisappearLOD, m_AppearLOD, m_FadeIn.progress);
				return true;
			}
			return false;
		}
		protected override void EndAppeared()
		{
			m_Opacity = 1f;
			LOD = m_AppearLOD;
			modelView.dwCamera.RemoveOpacityModifier(new OpacityModifier(this));
			m_FadeIn.Reset();
		}


		protected override void StartDisappearing()
		{
			m_FadeOut.Start();
			m_Opacity = 1f;
			LOD = m_AppearLOD;
			modelView.dwCamera.AddOpacityModifier(new OpacityModifier(this));
		}

		protected override bool InternalDisappearing()
		{
			if (m_FadeOut.progress < 1f)
			{
				m_Opacity = m_FadeOutCurve.Evaluate(Mathf.Clamp01(1f - m_FadeOut.progress));
				LOD = Mathf.Lerp(m_AppearLOD, m_DisappearLOD, m_FadeOut.progress);
				return true;
			}
			return false;
		}
		protected override void EndDisappeared()
		{
			m_Opacity = 0f;
			LOD = m_DisappearLOD;
			modelView.dwCamera.RemoveOpacityModifier(new OpacityModifier(this));
			m_FadeOut.Reset();
		}
	}
}