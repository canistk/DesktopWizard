using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
	[RequireComponent(typeof(Animator))]
    public class BodyLayout : MonoBehaviour
    {
		[SerializeField] Animator m_Animator;
		public Animator animator
		{
			get
			{
				if (m_Animator == null)
					m_Animator = GetComponent<Animator>();
				return m_Animator;
			}
		}
		[SerializeField] FaceRig m_FaceRig;
		public FaceRig faceRig
		{
			get
			{
				if (m_FaceRig == null)
					m_FaceRig = GetComponentInChildren<FaceRig>();
				return m_FaceRig;
			}
		}

		[SerializeField] EmotionWheel m_EmotionWheel;
		public EmotionWheel EmotionWheel => m_EmotionWheel;

		[SerializeField] HeroineEyeCtrl m_HeroineEyeCtrl;
		public HeroineEyeCtrl EyeCtrl => m_HeroineEyeCtrl;

		[Header("Extra")]
		[SerializeField] List<NamedTransform> m_NamedTransforms;
        private Dictionary<string, Transform> m_NamedTransformsDict = null;
		private Dictionary<string, Transform> namedTransforms
        {
            get
            {
				if (!Application.isPlaying)
					throw new System.Exception("This property is only available in play mode.");
                if (m_NamedTransformsDict == null)
				{
					m_NamedTransformsDict = new Dictionary<string, Transform>();
					foreach (var nt in m_NamedTransforms)
					{
						m_NamedTransformsDict[nt.name] = nt.transform;
					}
				}
				return m_NamedTransformsDict;
			}
        }

		public void Editor_Fetch()
		{
			if (Application.isPlaying)
				return;
			m_FaceRig			= GetComponentInChildren<FaceRig>();
			m_Animator			= GetComponent<Animator>();
			m_EmotionWheel		= GetComponentInChildren<EmotionWheel>();
			m_HeroineEyeCtrl	= GetComponentInChildren<HeroineEyeCtrl>();
		}

		private void Awake()
		{
			if (m_FaceRig == null)			m_FaceRig			= GetComponentInChildren<FaceRig>();
			if (m_Animator == null)			m_Animator			= GetComponent<Animator>();
			if (m_EmotionWheel == null)		m_EmotionWheel		= GetComponentInChildren<EmotionWheel>();
            if (m_HeroineEyeCtrl == null)	m_HeroineEyeCtrl	= GetComponentInChildren<HeroineEyeCtrl>();


            if (m_FaceRig == null)			Debug.LogError("FaceRig is missing.");
			if (m_Animator == null)			Debug.LogError("Animator is missing.");
			if (m_EmotionWheel == null)		Debug.LogWarning("EmotionWheel is missing");
			if (m_HeroineEyeCtrl == null)	Debug.LogWarning("Eye control is missing");
		}

		public Transform GetNamedTransform(string name)
		{
			return namedTransforms[name];
		}
		public void AddNamedTransfrom(Transform t)
		{
			AddNamedTransfrom(t.name, t);
		}
		public void AddNamedTransfrom(string name, Transform t)
		{
            if (!t.IsChildOf(animator.transform))
				throw new System.Exception("The transform must be a child of the animator.");
			if (m_NamedTransforms == null)
				m_NamedTransforms = new List<NamedTransform>();
            m_NamedTransforms.Add(new NamedTransform(name, t));
		}
		public bool TryGetNamedTransform(string name, out Transform transform)
			=> namedTransforms.TryGetValue(name, out transform);

		
		public void FOO()
		{
		}
    }

	public enum eAnimation
	{
		Idle,
		Sit,
		SitFloor,
	}

	public enum eAniAction
	{
		None = 0,
		RandomIdle,
		Welcome,
	}

	[System.Serializable]
    public struct NamedTransform
    {
		public string name;
		public Transform transform;
        public NamedTransform(string name, Transform transform)
		{
			this.name = name;
			this.transform = transform;
		}
        public NamedTransform(Transform transform)
            : this(transform.name, transform) { }
	}
}