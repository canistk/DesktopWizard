using DesktopWizard;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using BehaviorDesigner.Runtime;
namespace Gaia
{
	/// <summary>
	/// Load VRM character and display it in a window.
	/// handle System.Windows.Forms creation and destruction.
	/// define the model view position in world space & OS space.
	/// and relocate the character pivot to the model view position.
	/// <see cref="GxVRMLoader.LoadModel(string, System.Action{GxCharacter, UniVRM10.Vrm10Instance}, System.Action{System.Exception})"/>
	/// </summary>
	public class GxWinCharacter : GxWin
    {
		[SerializeField] Transform m_CharacterPivot;

		private KeyValuePair<bool, GxCharacter> m_Character;
		/// <summary>Try get character component in this model view. (nullable)</summary>
		/// </summary>
		public GxCharacter Character
		{
			get
			{
				if (!m_Character.Key)
				{
					m_Character = new KeyValuePair<bool, GxCharacter>(true, GetComponentInChildren<GxCharacter>(true));
				}
				return m_Character.Value;
			}
		}

		private KeyValuePair<bool, GxCameraCtrl> m_CameraCtrl;
		public GxCameraCtrl CameraCtrl
		{
			get
			{
				if (!m_CameraCtrl.Key)
				{
					m_CameraCtrl = new KeyValuePair<bool, GxCameraCtrl>(true, GetComponentInChildren<GxCameraCtrl>(true));
				}
				return m_CameraCtrl.Value;
			}
		}

		[SerializeField] BehaviorTree m_BehaviorTree;
		public BehaviorTree BehaviorTree
		{
  			get
			{
				if (m_BehaviorTree == null)
				{
					m_BehaviorTree = gameObject.GetComponent<BehaviorTree>();
					if (m_BehaviorTree == null)
					{
						m_BehaviorTree = gameObject.AddComponent<BehaviorTree>();
					}
					// TODO: load resource's BehaviorTree if not found in this gameObject.
				}
				return m_BehaviorTree;
			}
		}

		protected override void Awake()
		{
			ReferenceEquals(Character, null); // Force initialization of Character
			base.Awake();
		}

		public void Assign(GxCharacter character)
		{
			if (m_Character.Value != null)
			{
				Debug.LogError($"Already assigned character {m_Character.Value.name}", m_Character.Value);
				return;
			}

			// Align  ModelView, but parent pivot (due to camera orbit control)
			character.transform.SetParent(m_CharacterPivot, false);
			character.transform.SetPositionAndRotation(transform.position, transform.rotation);

			m_Character = new KeyValuePair<bool, GxCharacter>(true, character);
		}


		#region Runtime Creation
		private static Dictionary<GxCharacter, GxWin> s_CharacterDict = new Dictionary<GxCharacter, GxWin>();
		private static int s_SpawnedVRMCount = 0;
		public static async Task LoadVRM(string vrmFilePath)
		{
			if (string.IsNullOrEmpty(vrmFilePath))
			{
				Debug.LogError("VRM file path is null or empty.");
				return;
			}

			Debug.Log($"Loading VRM : {vrmFilePath}");
			await GxVRMLoader.LoadModel(vrmFilePath, (ch, vrm) =>
			{
				var _name = Kit2.KxPath.GetFileNameWithoutExtension(vrmFilePath);
				Debug.Log($"Loaded {_name}", ch);
				try
				{
					Debug.Log($"Regist desktop wizard {_name}");
					const string P_Name = "UIs/DWTemplate";
					var prefab = Resources.Load(P_Name);
					if (prefab == null)
					{
						Debug.LogError($"Prefab '{P_Name}' not found in Resources.");
						return;
					}
					var pos = new Vector3(0f, 3f * s_SpawnedVRMCount, 0f);
					var token = (GameObject)GameObject.Instantiate(prefab, pos, Quaternion.identity);
					if (token == null)
					{
						Debug.LogError($"Failed to instantiate prefab '{P_Name}'.");
						return;
					}
					token.name = $"[WIN]-{_name}";
					var modelView = token.GetComponentInChildren<GxWinCharacter>();
					if (modelView == null)
					{
						Debug.LogError($"{nameof(GxWinCharacter)} component not found in instantiated prefab.");
						return;
					}
					modelView.Assign(ch);
					++s_SpawnedVRMCount;
					s_CharacterDict.Add(ch, modelView);
					modelView.dwForm.FormClosed += (sender, e) =>
					{
						UnloadVRM(ch);
					};
					modelView.dwCamera.EVENT_MouseDown += DwCamera_EVENT_MouseRightClicked;
				}
				catch (System.Exception ex)
				{
					throw ex;
				}
			}, Debug.LogException);
		}

		private static void DwCamera_EVENT_MouseRightClicked(PointerEventData evt)
		{
			// check is right click
			if (evt.pointerId != 2)
			{
				return;
			}
			Debug.LogWarning("Right clicked.");
		}

		private static bool UnloadVRM(GxCharacter character)
		{
			if (!s_CharacterDict.TryGetValue(character, out var modelView))
			{
				Debug.LogWarning($"Character {character.name} not found in model view dictionary.");
				return false;
			}

			try
			{
				s_CharacterDict.Remove(character);
				GxVRMLoader.UnloadModel(character);
				GameObject.Destroy(modelView.gameObject);
			}
			catch (System.Exception ex)
			{
				Debug.LogError($"Failed to unload VRM for character {character.name}: {ex.Message}");
				return false;
			}
			return true;
		}
		#endregion Runtime Creation
	}
}