using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Kit2;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace Gaia
{
	[RequireComponent(typeof(Button))]
	public class UIButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
		IPointerEnterHandler, IPointerExitHandler
	{
		[SerializeField] Button m_Button;

		[Header("Speed Up")]
		[SerializeField] float m_LongClickPeriod = 0.5f;
		[SerializeField] AnimationCurve m_RepeatDurationCurve01 = AnimationCurve.Linear(1f, 0.35f, 2f, 0.1f);
		[SerializeField] UIText m_Label;
		
		public Button button
		{
			get
			{
				if (!m_Fetched) Fetch();
				return m_Button;
			}
		}

		public bool interactable
		{
			get
			{
				if (!m_Fetched) Fetch();
				return m_Button.interactable;
			}
			set => m_Button.interactable = value;
		}
		public string Label
		{
			get
			{
				if (!m_Fetched)
					Fetch();
				if (m_Label)
					return m_Label.Text;
				else
					return "";
			}
			set
			{
				if (m_Label)
					m_Label.Text = value;
			}
		}
		private bool m_Fetched = false;

		private void OnValidate()
		{
			if (!this.gameObject.scene.IsValid())
				return;
			m_RepeatDurationCurve01.postWrapMode = WrapMode.ClampForever;
		}

		private void Reset()
		{
			Fetch();
		}

		private void Fetch()
		{
			if (m_Fetched)
				return;
			m_Fetched = true;
			if (m_Label == null)
				m_Label = gameObject.GetComponentInChildren<UIText>();

			if (m_Button == null)
				m_Button = GetComponentInChildren<Button>(true);
		}

		public event System.Action EVENT_OnClick;
		public event System.Action<UIButton> EVENT_OnClickButton;
		[SerializeField] bool m_SpamBlocker = true;
		private const float s_BlockPeriod = 0.3f;
		private float m_LastClick = -s_BlockPeriod;
		private void InternalOnClick()
		{

			if (m_SpamBlocker && Time.realtimeSinceStartup - m_LastClick < s_BlockPeriod)
			{
				// Debug.LogWarning($"Blocked : {name}");
				return;
			}
			// Debug.Log($"Clicked {name}");
			m_LastClick = Time.realtimeSinceStartup;
			EVENT_OnClick?.Invoke();
			EVENT_OnClickButton?.Invoke(this);
			OnClick();
		}

		protected virtual void OnClick() { }

		private void Awake()
		{
			button.onClick.AddListener(InternalOnClick);
			m_RepeatDurationCurve01.postWrapMode = WrapMode.ClampForever;
			UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
		}

		private void OnSceneLoaded(UnityEngine.SceneManagement.Scene _scene, UnityEngine.SceneManagement.LoadSceneMode mode)
		{
			m_LastClick = -s_BlockPeriod;
		}

		private void OnDestroy()
		{
			button.onClick.RemoveListener(InternalOnClick);
		}

		private void Update()
		{
			HandleLongClickEvent();
		}

		private void OnEnable()
		{
			m_LastClick = 0;
		}

		private void OnDisable()
		{
			CleanEvent();
		}

		#region Long Click
		private bool m_IsPointerDown = false;
		private bool m_IsLongClickFired = false;
		private float m_LastPointerDownTime = -1f;
		private float m_LastTriggerLongClickRepeat = 0f;
		public event System.Action EVENT_LongClick;
		public event System.Action EVENT_LongClickRepeat;
		public void OnPointerDown(PointerEventData eventData)
		{
			//Debug.LogWarning("Pointer Down");
			m_IsPointerDown = true;
			m_IsLongClickFired = false;
			m_LastPointerDownTime = Time.timeSinceLevelLoad;
			m_LastTriggerLongClickRepeat = 0f;
		}

		public void OnPointerUp(PointerEventData eventData)
		{
			// Known issue. U3D UGUI's pointer drag will trigger PointerUp.
			// and Pointer Up after drag will be ignore.
			// Unity's team, you had one job to do.
			// TODO: find a solution to solve the "pointer up" event missing
			// between finger touch input & mouse input.
			if (!m_IsPointerDown)
				return;
			CleanEvent();
			//Debug.LogWarning("Pointer Up");
		}
		private void CleanEvent()
		{

			m_IsPointerDown = false;
			m_IsLongClickFired = false;
			m_LastPointerDownTime = 0f;
			m_LastTriggerLongClickRepeat = 0f;
		}

		private void HandleLongClickEvent()
		{
			if (!m_IsPointerDown)
				return;
			if (Time.timeSinceLevelLoad < m_LastPointerDownTime + m_LongClickPeriod)
				return;
			if (!m_IsLongClickFired)
			{
				m_IsLongClickFired = true;
				EVENT_LongClick?.Invoke();
				//Debug.LogWarning("Long Clicked");
			}

			float biasTimer = Time.timeSinceLevelLoad - m_LastPointerDownTime;
			float period = m_RepeatDurationCurve01.Evaluate(biasTimer);
			if (Time.timeSinceLevelLoad < m_LastTriggerLongClickRepeat + period)
				return;
			m_LastTriggerLongClickRepeat = Time.timeSinceLevelLoad;
			EVENT_LongClickRepeat?.Invoke();
			//Debug.LogWarning("Long Clicked - repeating");
		}
		#endregion Long Click

		#region Hover
		public event System.Action<UIButton> EVENT_Enter;
		public event System.Action<UIButton> EVENT_Exit;
		public void OnPointerEnter(PointerEventData eventData)
		{
			EVENT_Enter?.TryCatchDispatchEventError(o => o?.Invoke(this));
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			EVENT_Exit?.TryCatchDispatchEventError(o => o?.Invoke(this));
		}
		#endregion Hover
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(UIButton))]
	public class UIButtonInspector : Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			UIButton button = (UIButton)target;
			EditorGUI.BeginChangeCheck();
			string str = EditorGUILayout.TextArea(button.Label, GUILayout.MinHeight(120), GUILayout.ExpandHeight(true));
			if (EditorGUI.EndChangeCheck())
			{
				button.Label = str;
				// Debug.Log(str);
			}
		}
	}
#endif
}