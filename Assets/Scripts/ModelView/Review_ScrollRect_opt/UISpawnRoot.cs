using Kit2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Gaia
{
	public class UISpawnRoot : MonoBehaviour
	{
#if UNITY_EDITOR
		public bool drawGizmos = false;
#endif

		public UIScrollRect scrollRect;
		public UILayoutGroup layoutGroup;

		SpawnCommandBase m_spawnCommand;
		bool m_dirty = true;

		private void Reset()
		{
			if (!Application.isPlaying)
				Editor_ReplaceAll();
		}

		void Awake()
		{
			if (scrollRect)
				scrollRect.onValueChanged.AddListener(OnViewChanged);
		}

		void OnDestroy()
		{
			if (scrollRect)
				scrollRect.onValueChanged.RemoveListener(OnViewChanged);
		}

#if UNITY_EDITOR
		void OnDrawGizmos()
		{
			if (scrollRect == null || layoutGroup == null || m_spawnCommand == null)
				return;

			if (!drawGizmos)
				return;

			using (new ColorScope(Color.cyan))
			using (new GizmosMatrix(layoutGroup.GetRectMatrix()))
			{
				var viewMask = scrollRect.GetViewRect();
				var Col0 = new Color32(164, 24, 46, 255);
				var Col1 = new Color32(53, 53, 53, 255);
				var maskRect = layoutGroup.GetMaskRectLS(viewMask);
				Gizmos.DrawWireCube(maskRect.center, maskRect.size);

				layoutGroup.CalculateViewRange(out var viewRange, viewMask);

				for (int i = 0; i < m_spawnCommand.dataCount; i++)
				{
					Gizmos.color = (i >= viewRange.start && i < viewRange.end) ? Col0 : Col1;
					layoutGroup.GetCellRect(out var cellRect, i);
					var cx = cellRect.position.x + cellRect.size.x * 0.5f;
					var cy = -cellRect.position.y - cellRect.size.y * 0.5f;
					Gizmos.DrawWireCube(new Vector3(cx, cy), cellRect.size);
				}
			}
		}

#endif

		#region Editor Setup
		[ContextMenu("Editor replace scrollRect")]
		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		protected virtual void Editor_ReplaceScrollRect()
		{
#if UNITY_EDITOR
			// replace scrollrect -> AxScrollRect
			var org = GetComponentInChildren<ScrollRect>(true);
			if (org == null)
			{
				var exist = GetComponentInChildren<UIScrollRect>();
				if (exist)
				{
					this.scrollRect = exist;
					return;
				}
				throw new System.Exception($"Fail to locate {nameof(ScrollRect)}");
			}

			var tran = org.transform;

			var content = org.content;
			var horizontal = org.horizontal;
			var vertical = org.vertical;
			var movementType = org.movementType;
			var elasticity = org.elasticity;
			var inertia = org.inertia;
			var decelerationRate = org.decelerationRate;
			var scrollSensitivity = org.scrollSensitivity;
			var viewport = org.viewport;

			var hScrollbar = org.horizontalScrollbar;
			var hSBVisibility = org.horizontalScrollbarVisibility;
			var hSBSpacing = org.horizontalScrollbarSpacing;

			var vScrollbar = org.verticalScrollbar;
			var vSBVisibility = org.verticalScrollbarVisibility;
			var vSBSpacing = org.verticalScrollbarSpacing;

			DestroyImmediate(org, true);

			var s = tran.gameObject.AddComponent<UIScrollRect>();
			s.content = content;
			s.horizontal = horizontal;
			s.vertical = vertical;
			s.movementType = movementType;
			s.elasticity = elasticity;
			s.inertia = inertia;
			s.decelerationRate = decelerationRate;
			s.scrollSensitivity = scrollSensitivity;
			s.viewport = viewport;
			s.horizontalScrollbar = hScrollbar;
			s.horizontalScrollbarVisibility = hSBVisibility;
			s.horizontalScrollbarSpacing = hSBSpacing;
			s.verticalScrollbar = vScrollbar;
			s.verticalScrollbarVisibility = vSBVisibility;
			s.verticalScrollbarSpacing = vSBSpacing;
			this.scrollRect = s;
#endif
		}

		[ContextMenu("Editor replace layout")]
		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		protected virtual void Editor_ReplaceLayout()
		{
#if UNITY_EDITOR
			var org = GetComponentInChildren<GridLayoutGroup>(true);
			if (org == null)
			{
				var exist = GetComponentInChildren<UIGridLayoutGroup>();
				if (exist)
				{
					this.layoutGroup = exist;
					return;
				}
				throw new System.Exception($"Fail to locate {nameof(GridLayoutGroup)}");
			}

			var tran = org.transform;

			var padding = org.padding;
			var cellSize = org.cellSize;
			var spacing = org.spacing;
			var startCorner = org.startCorner;
			var startAxis = org.startAxis;
			var childAlignment = org.childAlignment;
			var constraint = org.constraint;

			DestroyImmediate(org, true);

			var g = tran.gameObject.AddComponent<UIGridLayoutGroup>();
			g.padding = padding;
			g.cellSize = cellSize;
			g.spacing = spacing;
			g.startCorner = (UIGridLayoutGroup.Corner)(int)startCorner;
			g.startAxis = (UIGridLayoutGroup.Axis)(int)startAxis;
			g.childAlignment = childAlignment;
			g.constraint = (UIGridLayoutGroup.Constraint)(int)constraint;
			this.layoutGroup = g;
#endif
		}

		[ContextMenu("Editor replace all")]
		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		private void Editor_ReplaceAll()
		{
			Editor_ReplaceScrollRect();
			Editor_ReplaceLayout();
		}
		#endregion Editor Setup

		public void Spawn(SpawnCommandBase command, bool resetScrollPos = false)
		{
			m_spawnCommand = command;
			UpdateUIs();

			if (resetScrollPos && scrollRect)
			{
				LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
				if (scrollRect.horizontal)
					scrollRect.SetHorizontalNormalizedPosition(0); // to leftmost

				if (scrollRect.vertical)
					scrollRect.SetVerticalNormalizedPosition(1); // to topmost
			}

			UpdateUIs();
		}

		[System.Obsolete("Reimplement", true)]
		public void ScrollTo(int index, int totalCount, float duration = 0.5f)
		{
			float normalizeIntervalSize = totalCount > 1 ? 1f / (totalCount - 1) : 1;
			float targetPosition = normalizeIntervalSize * index;

			if (scrollRect.horizontal)
			{
				//DOTween.To(
				//	() => scrollRect.horizontalNormalizedPosition,
				//	x => scrollRect.horizontalNormalizedPosition = x,
				//	targetPosition,
				//	duration
				//);
			}
			else if (scrollRect.vertical)
			{
				//DOTween.To(
				//	() => scrollRect.verticalNormalizedPosition,
				//	y => scrollRect.verticalNormalizedPosition = y,
				//	 1 - targetPosition,
				//	duration
				//);
			}
		}

		public void Focus(int index, Vector2 viewportOffset, Vector2 rectOffset)
		{
			if (scrollRect == null || layoutGroup == null)
				return;
			layoutGroup.GetCellRectWS(out var rectWS, index);
			scrollRect.Focus(rectWS, viewportOffset, rectOffset);
		}


		public void Rebuild()
		{
			if (m_spawnCommand == null)
				return;

			m_spawnCommand.ClearAllSpawnedUI();
			UpdateUIs();
		}

		void Update()
		{
			if (!m_dirty)
				return;

			UpdateUIs();

			m_dirty = false;
		}

		void OnViewChanged(Vector2 _)
		{
			m_dirty = true;

			if (m_spawnCommand == null)
				return;

			var performCulling = PerformCulling();
			if (performCulling)
			{
				m_spawnCommand.AppendLayoutCells(layoutGroup.Cells());
			}
		}

		bool PerformCulling()
		{
			return scrollRect != null && layoutGroup != null;
		}

		//[Sirenix.OdinInspector.Button]
		[ContextMenu("Refresh Layout")]
		void UpdateUIs()
		{
			if (m_spawnCommand == null)
				return;

			if (!PerformCulling())
			{
				m_spawnCommand.SpawnUIs(new RangeInt(0, m_spawnCommand.dataCount));
			}
			else
			{
				var count = m_spawnCommand.dataCount;

				layoutGroup.SetDataCount(count);
				layoutGroup.CalculateViewRange(out var viewRange, scrollRect.GetViewRect());

				m_spawnCommand.DespawnCulledUIs(viewRange);
				m_spawnCommand.SpawnUIs(viewRange);
				m_spawnCommand.AppendLayoutCells(layoutGroup.Cells());

				LayoutRebuilder.MarkLayoutForRebuild(layoutGroup.transform as RectTransform);
			}
		}
	}
}

