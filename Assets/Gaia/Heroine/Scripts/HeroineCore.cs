using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DesktopWizard;
using BehaviorDesigner.Runtime;
using System.Text;
using Kit2.Tasks;
using System;
using UnityEngine.UIElements;
namespace Gaia
{
	using MouseBtn = PointerEventData.InputButton;
	public enum eMode
	{
		/// <summary>Reset avoid to interrupt player</summary>
        Rest,

        /// <summary>Focus on player</summary>
        Focus,
    }

    /// <summary>
    /// A helper class to layout the body parts of the heroine.
    /// AI mindset <see cref="m_BehaviorTree"/>
    /// Window camera <see cref="m_Camera"/>
    /// </summary>
    [RequireComponent(typeof(BehaviorTree))]
	[System.Obsolete("change to puzzle programming style", false)]
	public class HeroineCore : MonoBehaviour
    {
        private static HeroineCore m_Instance;
        public static HeroineCore instance => m_Instance;

		[Header("Required")]
		[SerializeField] GxModelView m_ModelView;
		[SerializeField] BehaviorTree m_BehaviorTree;
        [SerializeField] Animator m_Animator;
        [SerializeField] DwCamera m_Camera;

		[Header("Character's")]
		[SerializeField] BodyLayout m_Bodylayout;
		[SerializeField] HeroineEyeCtrl m_EyeCtrl;
        [SerializeField] Transform m_LookAtTarget;

        [Header("Mouse Ray")]
        [SerializeField] float m_RayLength = 10f;
		[SerializeField] LayerMask m_LayerMask;
        [SerializeField] QueryTriggerInteraction m_QueryTriggerInteraction = QueryTriggerInteraction.UseGlobal;

        [Header("ContextMenu")]
		[SerializeField] DwCamera m_ContextMenu = null;

		public GxModelView modelView => m_ModelView;

		/// <summary>The behavior tree for the heroine.</summary>
		public BehaviorTree behaviorTree => m_BehaviorTree;

        /// <summary>An animator for the heroine.</summary>
        public Animator animator => m_Animator;

        /// <summary>render texture for window widget</summary>
        public new DwCamera camera => m_Camera;

        /// <summary>A widget to display as "Window" based on
        /// <see cref="DwCamera"/> render texture.</summary>
        public DwForm form => m_Camera?.dwForm;

        [System.Flags]
        private enum eDebug
        {
            DrawMouseRay = 1 << 0,
            LogMouseEvent = 1 << 1,
            LogKeyEvent = 1 << 2,
			ShowAnimatorInfo = 1 << 3,
		}
        [System.Serializable]
        private class DebugConfig
        {
			public eDebug config = (eDebug)0;
		}
        [SerializeField] DebugConfig m_Debug;

		private eMode m_Mode = eMode.Rest;
        public eMode mode
		{
			get => m_Mode;
			private set => m_Mode = value;
		}

        [System.Serializable]
        public struct CamMode
        {
            public string name;
            public Vector3 cameraPos;
            public Vector3 cameraRot;
            public Vector3 avatarPos;
            public Vector3 avatarRot;
            public Vector2Int formSize;
            public float orthographicSize;
            public float nearClipPlane;
            public float farClipPlane;
            

            public CamMode(string name, DwCamera camera, Transform avatar)
				: this(name,
                    camera.transform.position, camera.transform.eulerAngles,
                    avatar.position, avatar.eulerAngles,
                    camera.setting.Size,
                    camera.linkCamera.orthographicSize,
                    camera.linkCamera.nearClipPlane,
                    camera.linkCamera.farClipPlane)
			{
			}

            public CamMode(string name,
                Vector3 cameraPos, Vector3 cameraRot,
                Vector3 avatarPos, Vector3 avatarRot,
                Vector2Int size,
                float orthographicSize,
                float nearClipPlane, float farClipPlane)
            {
                this.name = name;
                this.cameraPos = cameraPos;
                this.cameraRot = cameraRot;
                this.avatarPos = avatarPos;
                this.avatarRot = avatarRot;
                this.formSize = size;
                this.orthographicSize = orthographicSize;
                this.nearClipPlane = nearClipPlane;
                this.farClipPlane = farClipPlane;
            }
        }
        [Header("Mode")]
        [SerializeField] bool m_FetchCurrentSetupAsNewMode = false;
        [SerializeField] List<CamMode> m_Modes = new List<CamMode>();

        [SerializeField] bool m_NextCameraMode = false;
        [SerializeField] int m_CameraModeIndex = 0;

		private void OnValidate()
		{
			if (m_FetchCurrentSetupAsNewMode)
			{
				m_FetchCurrentSetupAsNewMode = false;
				var mode = new CamMode("New Mode", m_Camera, m_Animator.transform);
				m_Modes.Add(mode);
			}

            if (m_NextCameraMode)
			{
				m_NextCameraMode = false;
				SwitchCameraMode(m_CameraModeIndex);
			}
		}

        public void SwitchCameraMode(string modeName)
        {
            for (int i = 0; i < m_Modes.Count; ++i)
			{
				var mode = m_Modes[i];
				if (mode.name != modeName)
                    continue;
				InternalSwitchCameraMode(mode);  
			}
        }

        public void SwitchCameraMode(int index)
        {
        	if (index < 0 || index >= m_Modes.Count)
				return;
            InternalSwitchCameraMode(m_Modes[index]);
		}

        private void InternalSwitchCameraMode(CamMode mode)
        {
            if (m_Camera)
            {
                m_Camera.transform.SetPositionAndRotation(mode.cameraPos, Quaternion.Euler(mode.cameraRot));
				m_Camera.setting.Size = mode.formSize;
				m_Camera.linkCamera.orthographicSize = mode.orthographicSize;
				m_Camera.linkCamera.nearClipPlane = mode.nearClipPlane;
				m_Camera.linkCamera.farClipPlane = mode.farClipPlane;
			}

            if (m_Animator)
                m_Animator.transform.SetPositionAndRotation(mode.avatarPos, Quaternion.Euler(mode.avatarRot));
        }

		#region Mono
		private void Reset()
		{
			m_BehaviorTree = GetComponent<BehaviorTree>();
		}

		private void Awake()
		{
			MyTaskHandler.Add(new MyTaskDelay(.3f, Welcome));
            m_Instance = this;
            HandleAwakeCmdProc();
		}

		private void Start()
		{
		}

		private void OnDestroy()
		{
            if (m_Instance == this)
                m_Instance = null;
            RemoveFormEvent();
		}

		private void Update()
		{
            if (!m_HookedFormEvent)
                DelayListenFormEvent();
		}

		private void OnDrawGizmos()
		{
            GizmosDrawMouseRay();
			GizmosDisplayAnimatorInfo();
		}
		#endregion Mono

		#region Redirect Form Event
		private bool m_HookedFormEvent = false;
        private float m_DelayInit = 0f;
		private void DelayListenFormEvent()
		{
            if (m_HookedFormEvent)
                throw new System.Exception("Hooked Form Event, handle this error on higher leve.");

			m_DelayInit += Time.unscaledDeltaTime;
            if (m_DelayInit >= 3f)
			{
                Debug.Log("[FormEvent] wait for init.");
                m_DelayInit = 0f;
			}
            if (form == null)
                return;
			AddFormEvent();
			m_HookedFormEvent = true;
		}
        private void AddFormEvent()
        {
			Debug.Log("[FormEvent] hooking event.");
            camera.EVENT_keyUp      += OnFormKeyUp;
            camera.EVENT_keyDown    += OnFormKeyDown;
            camera.EVENT_MouseDown  += OnFormMouseDown;
            camera.EVENT_MouseUp    += OnFormMouseUp;
			Debug.Log("[FormEvent] hook event completed.");
		}
        private void RemoveFormEvent()
        {
            if (!m_HookedFormEvent)
                return;
            m_HookedFormEvent       = false;
            camera.EVENT_keyUp      -= OnFormKeyUp;
            camera.EVENT_keyDown    -= OnFormKeyDown;
            camera.EVENT_MouseDown  -= OnFormMouseDown;
            camera.EVENT_MouseUp    -= OnFormMouseUp;
		}

		private void OnFormMouseUp(PointerEventData evt)
		{
            if (m_Debug.config.HasFlag(eDebug.LogMouseEvent))
                Debug.Log($"Mouse Up: {evt}");
		}

		private void OnFormMouseDown(PointerEventData evt)
		{
			if (m_Debug.config.HasFlag(eDebug.LogMouseEvent))
				Debug.Log($"Mouse Down: {evt}, btn:{evt.button}");

            switch(evt.button)
            {
                case MouseBtn.Left:     _MouseLeftBtnDown(evt); break;
                case MouseBtn.Middle:   _MouseMiddleBtnDown(evt); break;
				case MouseBtn.Right:    _MouseRightBtnDown(evt); break;
			}

            void _MouseLeftBtnDown(in PointerEventData evt)
            {
			    var ray = camera.GetMouseRayInModelSpace();
				// var hit = Physics.SphereCast(ray, 0.02f, out var hitInfo, m_RayLength, m_LayerMask, m_QueryTriggerInteraction);
				var hit = Physics.Raycast(ray, out var hitInfo, m_RayLength, m_LayerMask, m_QueryTriggerInteraction);
				if (hit)
                {
                    Debug.Log($"Mouse Left Click: {hitInfo.collider}", hitInfo.collider);
                    // TODO: player touching heroine, define reaction.
                }
            }
            void _MouseMiddleBtnDown(in PointerEventData evt)
			{
			}
            void _MouseRightBtnDown(in PointerEventData evt)
            {
                if (m_ShowContextMenu)
                    return;
                InternalShowContextMenu();
			}
		}

		private void OnFormKeyDown(KeyDownEvent evt)
		{
			if (m_Debug.config.HasFlag(eDebug.LogKeyEvent))
				Debug.Log($"KeyDn: {evt}");
		}

		private void OnFormKeyUp(KeyUpEvent evt)
		{
			if (m_Debug.config.HasFlag(eDebug.LogKeyEvent))
				Debug.Log($"KeyUp: {evt}");
			if (evt.keyCode == KeyCode.Space)
			{
				m_Animator.SetTrigger("TriggerRandom");
			}
			if (evt.keyCode == KeyCode.D)
			{
				m_Animator.SetBool("Sit", true);
			}
			if (evt.keyCode == KeyCode.W)
			{
				m_Animator.SetBool("Sit", false);
			}
			if (evt.keyCode == KeyCode.Tab)
			{
				m_Animator.SetTrigger("RndLookAt");
			}
		}
		#endregion Redirect Form Event

		#region Context Menu
        private bool m_ShowContextMenu = false;
        private void InternalShowContextMenu()
		{
			//if (!m_ShowContextMenu)
			//    return;
			if (m_ContextMenu == null)
			{
				Debug.LogError("ContextMenu is not set.");
				return;
            }

            m_ShowContextMenu = true;
			var osPos = camera.GetMousePosInOSSpace();
            //Debug.Log($"ContextMenu pos = {osPos}");
			m_ContextMenu.gameObject.SetActive(true);
            m_ContextMenu.Left  = osPos.x;
            m_ContextMenu.Top   = osPos.y;
            m_ContextMenu.EVENT_LostFocus += InternalHideContextMenu;
			m_ContextMenu.EVENT_Closed += InternalHideContextMenu;
		}
        private void InternalHideContextMenu()
        {
			//if (!m_ShowContextMenu)
			//	return;
			if (m_ContextMenu == null)
			{
				Debug.LogError("ContextMenu is not set.");
				return;
			}
			m_ContextMenu.EVENT_LostFocus -= InternalHideContextMenu;
			m_ContextMenu.EVENT_Closed -= InternalHideContextMenu;
			m_ShowContextMenu = false;
			m_ContextMenu.gameObject.SetActive(false);
		}
		#endregion Context Menu

		/// <summary></summary>
		/// <param name="modelSpace_pos">position in model space</param>
		public void SetLookAtTargetPos(Vector3 modelSpace_pos)
		{
			if (m_LookAtTarget == null)
				return;
			m_LookAtTarget.position = modelSpace_pos;
		}

		private void Welcome()
		{
			if (m_Animator == null)
				return;
			m_Animator.SetTrigger("Welcome");
		}

		StringBuilder m_Sb = null;
        private void GizmosDisplayAnimatorInfo()
		{
            if (!m_Debug.config.HasFlag(eDebug.ShowAnimatorInfo))
                return;
			if (m_Sb == null)
                m_Sb = new StringBuilder(1024);
            m_Sb.Clear();
            m_Sb.AppendLine("Animator Info:");
            m_Sb.AppendLine($"Current State:");
			FetchStateInfo(m_Sb, GetCurrentAnimatorStateInfo(0));

			m_Sb.AppendLine($"Next State:");
			FetchStateInfo(m_Sb, GetNextAnimatorStateInfo(0));

			var msg = m_Sb.ToString();
            Kit2.GizmosExtend.DrawLabel(m_Animator.transform.position, msg, 2f, 2f);

            void FetchStateInfo(StringBuilder sb, AnimatorStateInfo info)
			{
				sb.AppendLine($"AnimatorStateInfo:");
				sb.AppendLine($"  Time(n): {info.normalizedTime:F2}");
				sb.AppendLine($"  Loop: {info.loop}");
				sb.AppendLine($"  Length: {info.length}");
				sb.AppendLine($"  ShortNamePash: {info.shortNameHash}");
				sb.AppendLine($"  FullPathHash: {info.fullPathHash}");
			}
        }

        private void GizmosDrawMouseRay()
        {
            if (!m_Debug.config.HasFlag(eDebug.DrawMouseRay))
                return;
			if (camera == null)
				return;
			var monPos = (Vector3)camera.GetMousePosInMonitorSpace();
			Kit2.GizmosExtend.DrawPoint(monPos, Color.red, 20f);

			var m2r = camera.GetMouseRayInModelSpace();
			Kit2.GizmosExtend.DrawRay(m2r.origin, m2r.direction * 10f, Color.green);
		}

		#region Animator Helper
		public AnimatorStateInfo GetCurrentAnimatorStateInfo(int layerIndex)
        {
            if (m_Animator == null)
                return default;
            return m_Animator.GetCurrentAnimatorStateInfo(layerIndex);
        }
        public AnimatorStateInfo GetNextAnimatorStateInfo(int layerIndex)
        {
            if (m_Animator == null)
                return default;
            return m_Animator.GetNextAnimatorStateInfo(layerIndex);
        }

        public void SetTrigger(string trigger)
        {
            var hash = Animator.StringToHash(trigger);
			this.SetTrigger(hash);
		}
        public void SetTrigger(int hash)
        {
			if (m_Animator == null)
				return;
            m_Animator.SetTrigger(hash);
        }

        public void ResetTrigger(string trigger)
        {
            var hash = Animator.StringToHash(trigger);
            this.SetTrigger(hash);
        }
        public void ResetTrigger(int hash)
        {
            if (m_Animator == null)
                return;
            m_Animator.ResetTrigger(hash);
        }

        public void SetBool(string key, bool value)
        {
            var hash = Animator.StringToHash(key);
            this.SetBool(hash, value);
        }
        public void SetBool(int hash, bool value)
        {
            if (m_Animator == null)
                return;
            m_Animator.SetBool(hash, value);
        }

        public bool GetBool(string key)
        {
			var hash = Animator.StringToHash(key);
			return this.GetBool(hash);
		}
        public bool GetBool(int hash)
		{
            if (m_Animator == null)
                return false;
			return m_Animator.GetBool(hash);
		}

		public void SetFloat(string key, float value)
        {
            var hash = Animator.StringToHash(key);
            this.SetFloat(hash, value);
        }
        public void SetFloat(int hash, float value)
        {
            if (m_Animator == null)
                return;
            m_Animator.SetFloat(hash, value);
        }

        public float GetFloat(string key)
		{
			var hash = Animator.StringToHash(key);
			return this.GetFloat(hash);
		}

		public float GetFloat(int hash)
		{
			if (m_Animator == null)
				return 0f;
			return m_Animator.GetFloat(hash);
		}

		public void SetInteger(string key, int value)
        {
            var hash = Animator.StringToHash(key);
            this.SetInteger(hash, value);
        }
        public void SetInteger(int hash, int value)
        {
            if (m_Animator == null)
                return;
            m_Animator.SetInteger(hash, value);
        }

        public int GetInteger(string key)
        {
            var hash = Animator.StringToHash(key);
			return this.GetInteger(hash);
		}

		public int GetInteger(int hash)
		{
			if (m_Animator == null)
				return 0;
			return m_Animator.GetInteger(hash);
		}
		#endregion Animator Helper

		#region Wait for Awake
		public delegate void Cmd(HeroineCore core);
		private static Queue<Cmd> s_Cmdb4Awake = null;
		public static void WaitForAwake(Cmd cmd)
		{
			if (m_Instance != null)
			{
				// already awake, execute cmd directly.
				cmd.TryCatchDispatchEventError(o => o?.Invoke(instance));
				return;
			}

			// Not yet ready, cache command and wait.
			if (s_Cmdb4Awake == null)
				s_Cmdb4Awake = new Queue<Cmd>(8);
			s_Cmdb4Awake.Enqueue(cmd);
		}
		private void HandleAwakeCmdProc()
		{
			if (s_Cmdb4Awake == null)
				return;
			if (instance == null)
				throw new System.Exception("Unexpected error.");
			if (instance != this)
				return;
			while (s_Cmdb4Awake.Count > 0)
			{
				var cmd = s_Cmdb4Awake.Dequeue();
				cmd?.TryCatchDispatchEventError(o => o?.Invoke(this));
			}
		}
		#endregion Wait for Awake

	}
}