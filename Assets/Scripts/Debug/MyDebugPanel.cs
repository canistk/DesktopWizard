using DesktopWizard;
using Kit2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Gaia
{
    public class MyDebugPanel : MonoBehaviour
    {
		[Header("Required")]
		[SerializeField] DwCamera m_DwCamera;
		
		[SerializeField] GxModelView m_ModelView;

		[SerializeField] UIButton m_DebugModel;

		[SerializeField] UIButton m_ToggleSitBtn;
		[SerializeField] UIButton m_TriggerRandomPoseBtn;
		[SerializeField] UIButton m_WelcomePoseBtn;
		[SerializeField] UIButton m_SitFloorBtn;

		[SerializeField] UIButton m_CloseAppBtn;

		[SerializeField] Slider m_ScalerSlider;
		[SerializeField] Vector2Int m_FormSize = new Vector2Int(250, 400);
		[SerializeField] Vector2 m_FormScaleMinMax = new Vector2Int(1, 5);


		private void Awake()
		{
			if (m_CloseAppBtn)
			{
				m_CloseAppBtn.EVENT_OnClick += OnExitApp;
				m_CloseAppBtn.Label = "Exit App";
			}
			if (m_DebugModel)
			{
				m_DebugModel.EVENT_OnClick += ToggleDebugMode;
				m_DebugModel.Label = "Debug Model";
			}

			if (m_ToggleSitBtn)
			{
				m_ToggleSitBtn.EVENT_OnClickButton += OnPoseBtnClicked;
				m_ToggleSitBtn.Label = "Sit";
			}
			if (m_TriggerRandomPoseBtn)
			{
				m_TriggerRandomPoseBtn.EVENT_OnClickButton += OnPoseBtnClicked;
				m_TriggerRandomPoseBtn.Label = "Random Pose";
			}
			if (m_WelcomePoseBtn)
			{
				m_WelcomePoseBtn.EVENT_OnClickButton += OnPoseBtnClicked;
				m_WelcomePoseBtn.Label = "Welcome Pose";
			}
			if (m_SitFloorBtn)
			{
				m_SitFloorBtn.EVENT_OnClickButton += OnPoseBtnClicked;
				m_SitFloorBtn.Label = "Sit Floor";
			}
			if (m_ScalerSlider)
			{
				m_ScalerSlider.onValueChanged.AddListener(OnWindowSizeScaled);
			}
		}

		private void OnExitApp()
		{
			if (m_DwCamera)
				m_DwCamera.gameObject.SetActive(false);
			m_ModelView.TryDisappearAni(() =>
			{
#if UNITY_EDITOR
				UnityEditor.EditorApplication.ExitPlaymode();
#else
				Application.Quit();
#endif
			});
		}

		private void OnDestroy()
		{
			if (m_DebugModel) m_DebugModel.EVENT_OnClick -= ToggleDebugMode;
			if (m_ToggleSitBtn) m_ToggleSitBtn.EVENT_OnClickButton -= OnPoseBtnClicked;
			if (m_TriggerRandomPoseBtn) m_TriggerRandomPoseBtn.EVENT_OnClickButton -= OnPoseBtnClicked;
			if (m_WelcomePoseBtn) m_WelcomePoseBtn.EVENT_OnClickButton -= OnPoseBtnClicked;
			if (m_SitFloorBtn) m_SitFloorBtn.EVENT_OnClickButton -= OnPoseBtnClicked;
		}

		private void OnWindowSizeScaled(float value)
		{
			var f = Mathf.Clamp(value, m_FormScaleMinMax.x, m_FormScaleMinMax.y);
			var w = Mathf.FloorToInt(m_FormSize.x * f);
			var h = Mathf.FloorToInt(m_FormSize.y * f);
			m_ModelView.dwCamera.setting.Size = new Vector2Int(w, h);
		}

		private void OnPoseBtnClicked(UIButton obj)
		{
			var _animator = m_ModelView?.bodyLayout?.animator;
			if (_animator == null)
			{
				Debug.LogError("Animator not found");
				return;
			}

			if (obj == m_ToggleSitBtn)
			{
				var val = _animator.GetBool("Sit");
				obj.Label = val ? "Stand" : "Sit";
				_animator.SetBool("Sit", !val);
			}
			else if (obj == m_TriggerRandomPoseBtn)
			{
				_animator.SetTrigger("TriggerRandom");
			}
			else if (obj == m_WelcomePoseBtn)
			{
				_animator.SetTrigger("Welcome");
			}
			else if (obj == m_SitFloorBtn)
			{
				var val = _animator.GetBool("SitFloor");
				obj.Label = val ? "Stand" : "Sit Floor";
				_animator.SetBool("SitFloor", !val);
			}
		}

		private void ToggleDebugMode()
		{
			if (m_ModelView == null)
				return;
			Debug.Log("ToggleDebugMode");
			var _animator = m_ModelView.bodyLayout.animator;
			_animator.enabled = !_animator.enabled;

			var handler = _animator.GetOrAddComponent<HumanoidHandler>();
		}
	}
}