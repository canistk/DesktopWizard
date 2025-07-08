using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Gaia
{
	public class UIGridLayoutGroup : UILayoutGroup, ILayoutSelfController
	{
		/// <summary>
		/// Which corner is the starting corner for the grid.
		/// </summary>
		public enum Corner
		{
			/// <summary>
			/// Upper Left corner.
			/// </summary>
			UpperLeft = 0,
			/// <summary>
			/// Upper Right corner.
			/// </summary>
			UpperRight = 1,
			/// <summary>
			/// Lower Left corner.
			/// </summary>
			LowerLeft = 2,
			/// <summary>
			/// Lower Right corner.
			/// </summary>
			LowerRight = 3
		}

		/// <summary>
		/// The grid axis we are looking at.
		/// </summary>
		/// <remarks>
		/// As the storage is a [][] we make access easier by passing a axis.
		/// </remarks>
		public enum Axis
		{
			/// <summary>
			/// Horizontal axis
			/// </summary>
			Horizontal = 0,
			/// <summary>
			/// Vertical axis.
			/// </summary>
			Vertical = 1
		}

		/// <summary>
		/// Constraint type on either the number of columns or rows.
		/// </summary>
		public enum Constraint
		{
			/// <summary>
			/// Don't constrain the number of rows or columns.
			/// </summary>
			Flexible = 0,
			/// <summary>
			/// Constrain the number of columns to a specified number.
			/// </summary>
			FixedColumnCount = 1,
			/// <summary>
			/// Constraint the number of rows to a specified number.
			/// </summary>
			FixedRowCount = 2
		}

		[SerializeField] protected Corner m_StartCorner = Corner.UpperLeft;

		/// <summary>
		/// Which corner should the first cell be placed in?
		/// </summary>
		public Corner startCorner { get { return m_StartCorner; } set { SetProperty(ref m_StartCorner, value); } }

		[SerializeField] protected Axis m_StartAxis = Axis.Horizontal;

		/// <summary>
		/// Which axis should cells be placed along first
		/// </summary>
		/// <remarks>
		/// When startAxis is set to horizontal, an entire row will be filled out before proceeding to the next row. When set to vertical, an entire column will be filled out before proceeding to the next column.
		/// </remarks>
		public Axis startAxis { get { return m_StartAxis; } set { SetProperty(ref m_StartAxis, value); } }

		[SerializeField] protected Vector2 m_CellSize = new Vector2(100, 100);

		/// <summary>
		/// The size to use for each cell in the grid.
		/// </summary>
		public Vector2 cellSize { get { return m_CellSize; } set { SetProperty(ref m_CellSize, value); } }

		[SerializeField] protected Vector2 m_Spacing = Vector2.zero;

		/// <summary>
		/// The spacing to use between layout elements in the grid on both axises.
		/// </summary>
		public Vector2 spacing { get { return m_Spacing; } set { SetProperty(ref m_Spacing, value); } }

		[SerializeField] protected Constraint m_Constraint = Constraint.Flexible;

		/// <summary>
		/// Which constraint to use for the GridLayoutGroup.
		/// </summary>
		/// <remarks>
		/// Specifying a constraint can make the GridLayoutGroup work better in conjunction with a [[ContentSizeFitter]] component. When GridLayoutGroup is used on a RectTransform with a manually specified size, there's no need to specify a constraint.
		/// </remarks>
		public Constraint constraint { get { return m_Constraint; } set { SetProperty(ref m_Constraint, value); } }

		[SerializeField] protected int m_ConstraintCount = 2;

		/// <summary>
		/// How many cells there should be along the constrained axis.
		/// </summary>
		public int constraintCount { get { return m_ConstraintCount; } set { SetProperty(ref m_ConstraintCount, Mathf.Max(1, value)); } }

		protected UIGridLayoutGroup()
		{ }

#if UNITY_EDITOR
		protected override void OnValidate()
		{
			if (gameObject == null ||
				gameObject.scene.buildIndex == -1 ||
				gameObject.scene.rootCount == 0 ||
				!gameObject.scene.IsValid())
				return;
			base.OnValidate();
			constraintCount = constraintCount;
		}

#endif

		[System.Serializable]
		public enum FitMode
		{
			Unconstrained,
			MinSize,
			PreferredSize
		}

		public struct GridCell
		{
			public int index;
			public RectTransform rect;
		}

		public struct GridInfo
		{
			public Vector2Int corner;
			public int cellsPerMainAxis;
			public Vector2 startOffset;
			public Vector2Int actualCellCount;
		}


		[SerializeField] FitMode m_HorizontalFit = FitMode.Unconstrained;
		[SerializeField] FitMode m_VerticalFit = FitMode.Unconstrained;

		public FitMode horizontalFit { get { return m_HorizontalFit; } set { if (m_HorizontalFit != value) { m_HorizontalFit = value; SetDirty(); } } }
		public FitMode verticalFit { get { return m_VerticalFit; } set { if (m_VerticalFit != value) { m_VerticalFit = value; SetDirty(); } } }


		protected GridInfo m_gridInfo;

		void CalcGridInfo()
		{
			float width = rectTransform.rect.size.x;
			float height = rectTransform.rect.size.y;

			int cellCountX = 1;
			int cellCountY = 1;
			if (m_Constraint == Constraint.FixedColumnCount)
			{
				cellCountX = m_ConstraintCount;

				if (m_dataCount > cellCountX)
					cellCountY = m_dataCount / cellCountX + (m_dataCount % cellCountX > 0 ? 1 : 0);
			}
			else if (m_Constraint == Constraint.FixedRowCount)
			{
				cellCountY = m_ConstraintCount;

				if (m_dataCount > cellCountY)
					cellCountX = m_dataCount / cellCountY + (m_dataCount % cellCountY > 0 ? 1 : 0);
			}
			else
			{
				if (cellSize.x + spacing.x <= 0)
					cellCountX = int.MaxValue;
				else
					cellCountX = Mathf.Max(1, Mathf.FloorToInt((width - padding.horizontal + spacing.x + 0.001f) / (cellSize.x + spacing.x)));

				if (cellSize.y + spacing.y <= 0)
					cellCountY = int.MaxValue;
				else
					cellCountY = Mathf.Max(1, Mathf.FloorToInt((height - padding.vertical + spacing.y + 0.001f) / (cellSize.y + spacing.y)));
			}

			int cornerX = (int)startCorner % 2;
			int cornerY = (int)startCorner / 2;

			int cellsPerMainAxis, actualCellCountX, actualCellCountY;
			if (startAxis == Axis.Horizontal)
			{
				cellsPerMainAxis = cellCountX;
				actualCellCountX = Mathf.Clamp(cellCountX, 1, m_dataCount);
				actualCellCountY = Mathf.Clamp(cellCountY, 1, Mathf.CeilToInt(m_dataCount / (float)cellsPerMainAxis));
			}
			else
			{
				cellsPerMainAxis = cellCountY;
				actualCellCountY = Mathf.Clamp(cellCountY, 1, m_dataCount);
				actualCellCountX = Mathf.Clamp(cellCountX, 1, Mathf.CeilToInt(m_dataCount / (float)cellsPerMainAxis));
			}

			Vector2 requiredSpace = new Vector2(actualCellCountX * cellSize.x + (actualCellCountX - 1) * spacing.x,
												actualCellCountY * cellSize.y + (actualCellCountY - 1) * spacing.y
			);

			Vector2 startOffset = new Vector2(GetStartOffset(0, requiredSpace.x),
											   GetStartOffset(1, requiredSpace.y));

			m_gridInfo = new GridInfo()
			{
				corner = new Vector2Int(cornerX, cornerY),
				cellsPerMainAxis = cellsPerMainAxis,
				startOffset = startOffset,
				actualCellCount = new Vector2Int(actualCellCountX, actualCellCountY),
			};
		}

		public Vector2 GetGridCellPos(in GridInfo gridInfo, int index)
		{
			if (gridInfo.cellsPerMainAxis == 0)
				return Vector2.zero;

			int positionX;
			int positionY;

			if (startAxis == Axis.Horizontal)
			{
				positionX = index % gridInfo.cellsPerMainAxis;
				positionY = index / gridInfo.cellsPerMainAxis;
			}
			else
			{
				positionX = index / gridInfo.cellsPerMainAxis;
				positionY = index % gridInfo.cellsPerMainAxis;
			}

			if (gridInfo.corner.x == 1) positionX = gridInfo.actualCellCount.x - 1 - positionX;
			if (gridInfo.corner.y == 1) positionY = gridInfo.actualCellCount.y - 1 - positionY;

			var px = gridInfo.startOffset.x + (cellSize[0] + spacing[0]) * positionX;
			var py = gridInfo.startOffset.y + (cellSize[1] + spacing[1]) * positionY;

			return new Vector2(px, py);
		}

		protected override void OnSetDataCount(int dataCount)
		{
			base.OnSetDataCount(dataCount);
			CalcGridInfo();
		}


		protected override void OnGetCellRect(out Rect outRect, int idx)
		{
			outRect = new Rect(GetGridCellPos(m_gridInfo, idx), cellSize);
		}

		protected override void OnCalculateViewRange(out RangeInt outRange, RectTransform viewMask)
		{
			bool _RectOverlap(Rect rectL, Rect rectR)
			{
				return rectL.xMin < rectR.xMax && rectL.xMax > rectR.xMin &&
					   rectL.yMax > rectR.yMin && rectL.yMin < rectR.yMax;
			}

			if (viewMask == null)
			{
				outRange = new RangeInt(0, 0);
				return;
			}

			var maskRect = GetMaskRectLS(viewMask);

			int? start = null;
			int len = 0;

			for (int i = 0; i < m_dataCount; i++)
			{
				var p = GetGridCellPos(m_gridInfo, i);

				var rmin = new Vector2(p.x, -p.y - cellSize[1]);
				var rect = new Rect(rmin, cellSize);
				var show = _RectOverlap(maskRect, rect);

				if (start == null && show)
				{
					start = i;
				}

				if (start != null)
				{
					if (!show)
						break;

					len++;
				}
			}

			outRange = new RangeInt(start == null ? 0 : start.Value, len);
		}

		public override void SetLayoutHorizontal()
		{
			SetCellAlongAxis(0);
			SetSizeFitter(0);
		}

		public override void SetLayoutVertical()
		{
			SetCellAlongAxis(1);
			SetSizeFitter(1);
		}

		/// <summary><see cref="GridLayoutGroup.CalculateLayoutInputHorizontal"/></summary>
		public override void CalculateLayoutInputHorizontal()
		{
			base.CalculateLayoutInputHorizontal();
			CalcGridInfo();

			var axis = 0;
			GetLayoutInputForAxis(m_dataCount, axis, out var totalMin, out var totalPreferred, out var totalFlexible);
			SetLayoutInputForAxis(totalMin, totalPreferred, totalFlexible, axis);
		}

		/// <summary><see cref="GridLayoutGroup.CalculateLayoutInputVertical"/></summary>
		public override void CalculateLayoutInputVertical()
		{
			CalcGridInfo();

			var axis = 1;
			GetLayoutInputForAxis(m_dataCount, axis, out var totalMin, out var totalPreferred, out var totalFlexible);
			SetLayoutInputForAxis(totalMin, totalPreferred, totalFlexible, axis);
		}

		protected void GetLayoutInputForAxis(int dataCount, int axis, out float totalMin, out float totalPreferred, out float totalFlexible)
		{
			if (axis == 0)
				GetLayoutInputForHorizontalAxis(dataCount, out totalMin, out totalPreferred, out totalFlexible);
			else
				GetLayoutInputForVerticalAxis(dataCount, out totalMin, out totalPreferred, out totalFlexible);
		}

		/// <summary><see cref="GridLayoutGroup.CalculateLayoutInputHorizontal"/></summary>
		/// <param name="dataCount"></param>
		/// <param name="totalMin"></param>
		/// <param name="totalPreferred"></param>
		/// <param name="totalFlexible"></param>
		protected void GetLayoutInputForHorizontalAxis(int dataCount, out float totalMin, out float totalPreferred, out float totalFlexible)
		{
			int minColumns = 0;
			int preferredColumns = 0;
			if (m_Constraint == Constraint.FixedColumnCount)
			{
				minColumns = preferredColumns = m_ConstraintCount;
			}
			else if (m_Constraint == Constraint.FixedRowCount)
			{
				minColumns = preferredColumns = Mathf.CeilToInt(dataCount / (float)m_ConstraintCount - 0.001f);
			}
			else
			{
				minColumns = 1;
				preferredColumns = Mathf.CeilToInt(Mathf.Sqrt(dataCount));
			}

			totalMin = padding.horizontal + (cellSize.x + spacing.x) * minColumns - spacing.x;
			totalPreferred = padding.horizontal + (cellSize.x + spacing.x) * preferredColumns - spacing.x;
			totalFlexible = -1;
		}

		/// <summary><see cref="GridLayoutGroup.CalculateLayoutInputVertical"/></summary>
		/// <param name="dataCount"></param>
		/// <param name="totalMin"></param>
		/// <param name="totalPreferred"></param>
		/// <param name="totalFlexible"></param>
		protected void GetLayoutInputForVerticalAxis(int dataCount, out float totalMin, out float totalPreferred, out float totalFlexible)
		{
			int minRows = 0;
			if (m_Constraint == Constraint.FixedColumnCount)
			{
				minRows = Mathf.CeilToInt(dataCount / (float)m_ConstraintCount - 0.001f);
			}
			else if (m_Constraint == Constraint.FixedRowCount)
			{
				minRows = m_ConstraintCount;
			}
			else
			{
				float width = rectTransform.rect.width;
				int cellCountX = Mathf.Max(1, Mathf.FloorToInt((width - padding.horizontal + spacing.x + 0.001f) / (cellSize.x + spacing.x)));
				minRows = Mathf.CeilToInt(dataCount / (float)cellCountX);
			}

			float minSpace = padding.vertical + (cellSize.y + spacing.y) * minRows - spacing.y;

			totalMin = minSpace;
			totalPreferred = minSpace;
			totalFlexible = -1;
		}

		void SetSizeFitter(int axis)
		{
			FitMode fitting = (axis == 0 ? horizontalFit : verticalFit);
			if (fitting == FitMode.Unconstrained)
			{
				m_Tracker.Add(this, rectTransform, DrivenTransformProperties.None);
				return;
			}

			m_Tracker.Add(this, rectTransform, (axis == 0) ? DrivenTransformProperties.SizeDeltaX : DrivenTransformProperties.SizeDeltaY);

			if (fitting == FitMode.MinSize)
				rectTransform.SetSizeWithCurrentAnchors((RectTransform.Axis)axis, axis == 0 ? minWidth : minHeight);
			else
				rectTransform.SetSizeWithCurrentAnchors((RectTransform.Axis)axis, axis == 0 ? preferredWidth : preferredHeight);
		}

		void SetCellAlongAxis(int axis)
		{
			if (axis == 0)
			{

				foreach (var cell in m_cells)
				{
					if (cell.rect == null)
						continue;

					m_Tracker.Add(this, cell.rect, DrivenTransformProperties.Anchors |
												   DrivenTransformProperties.AnchoredPosition |
												   DrivenTransformProperties.SizeDelta);
				}
			}
			else
			{
				foreach (var grid in m_cells)
				{
					if (grid.rect == null)
						continue;

					var pos = GetGridCellPos(m_gridInfo, grid.index);

					SetChildAlongAxis(grid.rect, 0, pos.x, cellSize[0]);
					SetChildAlongAxis(grid.rect, 1, pos.y, cellSize[1]);
				}
			}
		}
	}
}









