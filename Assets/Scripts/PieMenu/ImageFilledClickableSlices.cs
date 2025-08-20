using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Kit2.PieMenu
{
	/// <summary>
	/// This code snippet is a slightly edited version of the original code, which comes from:
	/// https://forum.unity.com/threads/button-with-radial-fill-sprite-only-clickable-on-visible-area.546614/
	/// and was posted by Michael Grönert.
	/// His profile: https://forum.unity.com/members/badtoxic.813294/
	/// His discord: https://discord.gg/8QMCm2d
	/// It calculates the area of the image that is not visible due to fillAmount settings.
	/// </summary>
    public class ImageFilledClickableSlices : Image
    {
		protected override void Start()
		{
			base.Start();
			// The alphaHitTestMinimumThreshold property is used to determine the minimum alpha value required for
			// the image to be considered for hit testing. In other words, it controls how transparent or opaque a
			// part of the image needs to be in order for it to respond to input events(e.g., mouse clicks or touch events).
			this.GetComponent<Image>().alphaHitTestMinimumThreshold = 0.1f;
		}
		public override bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
		{
			bool result = base.IsRaycastLocationValid(screenPoint, eventCamera);
			if (!result)
			{
				return false;
			}

			RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera,
				out Vector2 localPoint);

			
			float clickAngle = fillClockwise ?
				Vector2.SignedAngle(localPoint, Vector2.up) :
				Vector2.SignedAngle(localPoint, Vector2.left);

			return (clickAngle >= 0) && (clickAngle < (360f * fillAmount));
		}
	}
}