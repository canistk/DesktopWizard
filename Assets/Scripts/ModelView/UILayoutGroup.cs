using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Gaia
{
	public abstract class UILayoutGroup : LayoutGroup
	{
		public struct Cell
		{
			public int index;
			public RectTransform rect;
		}

		protected int m_dataCount = 0;
		protected List<Cell> m_cells = new List<Cell>();


		public List<Cell> Cells() { return m_cells; }

		public void SetDataCount(int dataCount)
		{
			m_dataCount = dataCount;
			OnSetDataCount(dataCount);
		}

		public Matrix4x4 GetRectMatrix()
		{
			var rectWS = rectTransform.rect;
			return transform.localToWorldMatrix * Matrix4x4.Translate(new Vector3(rectWS.xMin, rectWS.yMax));
		}

		public Rect GetMaskRectLS(RectTransform maskTran)
		{
			var localM = GetRectMatrix();
			var maskRect = maskTran.rect;

			var maskMat = localM.inverse * maskTran.localToWorldMatrix;
			var minLS = maskMat.MultiplyPoint3x4(maskRect.min);
			var sizeLS = maskMat.MultiplyVector(maskRect.size);
			var maskLS = new Rect(minLS, sizeLS);

			return maskLS;
		}

		public void GetCellRect(out Rect outRect, int idx)
		{
			OnGetCellRect(out outRect, idx);
		}

		public void GetCellRectWS(out Rect outRect, int idx)
		{
			GetCellRect(out var rectLS, idx);
			var m = GetRectMatrix();
			var p = m.MultiplyPoint(new Vector3(rectLS.xMin, -rectLS.yMax));
			var s = m.MultiplyVector(rectLS.size);
			outRect = new Rect(p, s);
		}

		public void CalculateViewRange(out RangeInt outRange, RectTransform viewMask)
		{
			if (viewMask == null)
			{
				outRange = new RangeInt(0, 0);
				return;
			}
			OnCalculateViewRange(out outRange, viewMask);
		}


		protected virtual void OnCalculateViewRange(out RangeInt outRange, RectTransform viewMask) { outRange = default; }
		protected virtual void OnGetCellRect(out Rect outRect, int idx) { outRect = default; }
		protected virtual void OnSetDataCount(int dataCount) { }
	}
}