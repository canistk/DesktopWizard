using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kit2;
namespace Gaia
{
    public class TShirtCtrlV2 : MonoBehaviour
	{
		[SerializeField] public GxCtrlBone shirtFL, shirtBL, shirtFR, shirtBR;
		[SerializeField] public float speed = 1f;

		private void Update()
		{
			if (shirtFL != null) shirtFL.ApplyCoordinate(speed);
			if (shirtBL != null) shirtBL.ApplyCoordinate(speed);
			if (shirtFR != null) shirtFR.ApplyCoordinate(speed);
			if (shirtBR != null) shirtBR.ApplyCoordinate(speed);
		}
	}
}