using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
    public class GxGarage : MonoBehaviour
    {
		private void Start()
		{
			// TODO: tutorial flow, on first 2 runs.
			UIPopup.Explorer(Application.streamingAssetsPath, new[] { ".vrm" },
			(path) =>
				{
					GxModelView.LoadVRM(path);
				}
			);
		}
	}
}