using DesktopWizard;
using Kit2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
    public class GxDragCharacterHandler : GxDraggableForm
	{
		public bool
			m_Hip,
			m_Back,
			m_Hand;

		[SerializeField, LayerField] private string m_CharacterLayerName = "Character";
		[SerializeField] GxModelView m_ModelView;
		[SerializeField] Animator m_Animator;
		private BodyLayout bodyLayout => m_ModelView?.bodyLayout;
		private const float s_Distance = 1000f;
		private int m_CharacterLayerMask;

		[Header("Cross Fade")]
		[SerializeField] int LayerNo = 0;
		[SerializeField] AnimatorCrossFadeFixedSetting m_CrossFadeFixed;

		[SerializeField][RefAnimator(nameof(m_Animator))] AnimatorStateSelector m_Idle;
		[SerializeField][RefAnimator(nameof(m_Animator))] AnimatorStateSelector m_DragHandState;
		[SerializeField][RefAnimator(nameof(m_Animator))] AnimatorStateSelector m_DragHipState;
		[SerializeField][RefAnimator(nameof(m_Animator))] AnimatorStateSelector m_DragBackState;

		private Vector2 m_FixVector = Vector2.zero;

		private void Awake()
		{
			m_CharacterLayerMask = LayerMask.GetMask(m_CharacterLayerName);
			if (m_ModelView == null)
			{
				m_ModelView = gameObject.GetComponentInParent<GxModelView>();
			}
		}

		protected override void OnStartDrag(GxModelView win, GxPointerEventData evt)
		{	
			var ray = evt.ray;
			if (!Physics.Raycast(ray, out var hitInfo, s_Distance, m_CharacterLayerMask, QueryTriggerInteraction.Ignore))
			{
				Debug.LogWarning($"Nothing hit {evt.monitorPosition}");
				DebugExtend.DrawRay(ray.origin, ray.direction * s_Distance, Color.green, 5f, true);
				return;
			}

			if (bodyLayout == null)
				return;
			var shoulder0 = bodyLayout.animator.GetBoneTransform(HumanBodyBones.LeftShoulder);
			var shoulder1 = bodyLayout.animator.GetBoneTransform(HumanBodyBones.RightShoulder);
			var hips = new[]
			{
				bodyLayout.animator.GetBoneTransform(HumanBodyBones.Hips),
				bodyLayout.animator.GetBoneTransform(HumanBodyBones.Spine)
			};

			var isDragHand = 
				shoulder0 != null &&
				shoulder1 != null &&
				(hitInfo.collider.transform.IsChildOf(shoulder0) || hitInfo.collider.transform.IsChildOf(shoulder1));

			var isDragHip = false;
			for (var i = 0; i < hips.Length && !isDragHip; i++)
			{
				if (hips[i] == null)
					continue;
				if (hitInfo.collider == hips[i] || hitInfo.collider.transform.parent == hips[i])
					isDragHip = true;
			}

			var isDragBack = !isDragHand && !isDragHip;

			m_Hip = isDragHip;
			m_Hand = isDragHand;
			m_Back = isDragBack;

			// Debug.Log($"Hitted : {hitInfo.collider}");
			DebugExtend.DrawRay(ray.origin, ray.direction * hitInfo.distance, Color.red, 5f, true);

			if (isDragBack)
				bodyLayout.animator.CrossFadeInFixedTime(m_DragBackState.m_SelectedHash, LayerNo, m_CrossFadeFixed);
			if (isDragHip)
				bodyLayout.animator.CrossFadeInFixedTime(m_DragHipState.m_SelectedHash, LayerNo, m_CrossFadeFixed);
			if (isDragHand)
				bodyLayout.animator.CrossFadeInFixedTime(m_DragHandState.m_SelectedHash, LayerNo, m_CrossFadeFixed);
			m_FixVector = Vector2.zero;
		}

		protected override void OnDragging(GxModelView win, GxPointerEventData evt)
		{
			if (!TryGetDragInfo(out var dragInfo))
				return;
			if (!dragInfo.IsActive)
				throw new System.Exception();
			if (!dragInfo.IsDragging)
				return;

			// the offset from start drag position vs FORM's os position;
			var _camera = win?.dwCamera?.linkCamera;
			Debug.Assert(_camera != null, "Camera is null");

			
			Transform _tran = null;
			if (m_Hip || m_Back)
			{
				_tran = m_Animator.GetBoneTransform(HumanBodyBones.Hips);
			}
			else if (m_Hand)
			{
				_tran = m_Animator.GetBoneTransform(HumanBodyBones.RightHand);
			}

			if (_tran != null)
			{
				// world 2 form space
				var formPos = (Vector2)_camera.WorldToScreenPoint(_tran.position);
				var formDiff = dragInfo.formStartPos - formPos;

				m_FixVector = (Vector2) win.dwCamera.MatrixFormToMonitor().MultiplyVector(formDiff);
				/***
				var debug_wantedPos = (Vector2) win.dwCamera.MatrixFormToMonitor().MultiplyPoint3x4(formPos);
				DebugExtend.DrawLine(evt.monitorPosition, debug_wantedPos, Color.blue, 5f, true);
				DebugExtend.DrawCircle(debug_wantedPos, Vector3.forward, Color.green, 10f, 5f, false);
				DebugExtend.DrawCircle(evt.monitorPosition + m_FixVector, Vector3.forward, Color.yellow, 20f, 5f, false);
				//**/

				// Calculate the FORM's position in OS space.
				var monitorPos = evt.monitorPosition + dragInfo.monitorOffset + m_FixVector;
				win.dwWindow.MoveTo_Monitor(monitorPos);
			}
			else
			{
				var monitorPos = evt.monitorPosition + dragInfo.monitorOffset;
				win.dwWindow.MoveTo_Monitor(monitorPos);
				m_FixVector = Vector2.zero;
			}
		}

		protected override void OnEndDrag(GxModelView win, GxPointerEventData evt)
		{
			if (bodyLayout)
				bodyLayout.animator.CrossFade(m_Idle.m_SelectedHash, 0.15f);

			m_FixVector = Vector2.zero;


			// TODO: find landing area, let characher sit/dock on it.
			if (!win.dwCamera.RaycastBorderWin(evt.monitorPosition, Vector2.down, s_Distance, out var rst))
				return;

			DebugExtend.DrawRay(evt.monitorPosition, Vector3.down * s_Distance, Color.white.CloneAlpha(0.2f), 10f, false);
			DebugExtend.DrawRay(evt.monitorPosition, Vector3.down * rst.distance, Color.magenta, 10f, false);


			var area = rst.collider.GetComponent<DwArea>();
			Debug.Log($"Found Area {area}");
			if (area is not DwWindow window)
				return;

			var p0 = rst.endPoint;

			// window
			var c1 = window.collider;
			var pivot1 = DwArea.CalcInnerPointNormalize(c1, p0);
			
			// character
			var c2 = win.dwWindow.collider;
			var pivot2 = DwArea.CalcInnerPointNormalize(c2, p0);
		}
	}
}