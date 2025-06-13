using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Kit2.Tasks;
using Kit2;
using System.IO;
namespace Gaia
{
	public class WaitForFBXImportCompleted : MyTaskWithState
	{
		public GameObject model;
		private readonly string assetPath;
		private readonly float timeout;
		private readonly System.Action<GameObject> success;
		private readonly System.Action<System.Exception> fail;
		public WaitForFBXImportCompleted(string assetPath, System.Action<GameObject> Success, System.Action<System.Exception> fail, float timeout = 0.3f)
		{
			this.assetPath	= assetPath;
			this.timeout	= Time.realtimeSinceStartup + timeout;
			this.success	= Success;
			this.fail		= fail;
		}
		protected override void OnEnter()
		{
		}
		protected override bool ContinueOnNextCycle()
		{
			this.model = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
			if (this.model != null)
				return false; // got it early exit

			// wait for next cycle
			return this.model != null || Time.realtimeSinceStartup < timeout;
		}

		protected override void OnComplete()
		{
			if (model == null)
			{
				fail?.Invoke(new System.Exception($"WaitForFBXImportCompleted failed, assetPath={assetPath}"));
				return;
			}

			Debug.Log($"WaitForFBXImportCompleted success, assetPath={assetPath}");
			success?.Invoke(this.model);
		}
	}
}