using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// Known issue: Border detection is not accurate after changing the screen arrangement.
/// if player re-arrangement display setup in the middle of the game, ScreenHandler cannot update the screen border status properly.
/// leading to the wrong screen border detection.

namespace DesktopWizard
{
	[RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
	public class DwScreen : DwArea
	{
		public uint id => m_ScreenInfo.id;

		public ScreenInfo m_ScreenInfo;
		public void Init(ScreenInfo data)
		{
			this.m_ScreenInfo = data;
			this.gameObject.name = $"[Screen {data.id}]";
			if (DwCore.layer.ScreenLayer >= 0)
				this.gameObject.layer = DwCore.layer.ScreenLayer;
			base.Init(m_ScreenInfo.rect);
			UpdateRawBorder();
		}

		#region Border area
		private const float margin = 10f;
		private CullInfo[] m_RawBorders = null;
		private void UpdateRawBorder()
		{
			UpdateRawBorder(collider, margin, ref m_RawBorders);
			if (DwCore.layer.RawBorderLayer >= 0)
			{
				foreach (var b in m_RawBorders)
				{
					b.gameObject.layer = DwCore.layer.RawBorderLayer;
				}
			}
		}
		private static void UpdateRawBorder(BoxCollider area, float margin, ref CullInfo[] arr)
		{
			var size = (Vector2)area.size;
			var offset = (Vector2)area.center;
			var pos = area.transform.position;

			if (arr == null)
				arr = new CullInfo[4];

			const float depth = 10f;
			var m2 = margin * 0.5f;

			// Left border
			UpdateCollider(ref arr, 0, pos + new Vector3(offset.x - size.x / 2 - m2, offset.y, pos.z), margin, size.y, depth);

			// Top border
			UpdateCollider(ref arr, 1, pos + new Vector3(offset.x, offset.y + size.y / 2 + m2, pos.z), size.x, margin, depth);

			// Right border
			UpdateCollider(ref arr, 2, pos + new Vector3(offset.x + size.x / 2 + m2, offset.y, pos.z), margin, size.y, depth);

			// Bottom border
			UpdateCollider(ref arr, 3, pos + new Vector3(offset.x, offset.y - size.y / 2 - m2, pos.z), size.x, margin, depth);

			void UpdateCollider(ref CullInfo[] arr, int index, Vector3 position, float sizeX, float sizeY, float sizeZ)
			{
				if (arr[index] == null)
				{
					var name = index switch
					{
						0 => "Left",
						1 => "Top",
						2 => "Right",
						3 => "Bottom",
						_ => throw new System.Exception(),
					};
					var go = new GameObject(name, typeof(BoxCollider));
					var boxCollider = go.GetComponent<BoxCollider>();
					arr[index] = new CullInfo(boxCollider);
					arr[index].collider.transform.SetParent(area.transform);
				}

				arr[index].transform.position = position;
				arr[index].collider.size = new Vector3(sizeX, sizeY, sizeZ);
			}
		}
		#endregion Border area

		#region Cull Overlap
		private class CullInfo
		{
			public readonly BoxCollider collider;
			public QuickHash check;
			public Transform transform => collider.transform;
			public GameObject gameObject => collider.gameObject;
			public CullInfo(BoxCollider target)
			{
				this.collider = target;
				this.check = new QuickHash(target);
			}
			private Dictionary<BoxCollider, QuickHash> cache = new Dictionary<BoxCollider, QuickHash>();
			public List<BoxCollider> culled = new List<BoxCollider>();

			public void ClearSubBox()
			{
				foreach (var c in culled)
				{
					Destroy(c.gameObject);
				}
				culled.Clear();
				
			}

			public bool NoChanged()
			{
				var tmp = new QuickHash(collider);
				if (check.check == tmp.check)
					return true;
				check = tmp;
				return false;
			}

			public bool IsSameSources(Collider[] screens)
			{
				var matched = 0;
				foreach (var screen in screens)
				{
					if (screen is not BoxCollider box)
						continue;
					if (collider.transform.IsChildOf(screen.transform))
						continue;
					if (!cache.TryGetValue(box, out var rec))
						return false;

					var tmp = new QuickHash(box);
					if (rec.check != tmp.check)
						return false;
					++matched;
				}
				if (matched == 0 && culled.Count == 1)
					return true; // normal case, by nothing to compare.
				if (culled.Count == 0)
					return false; // force calculate(clone itself)
				return matched == culled.Count;
			}

			public void KeepRecords(IList<BoxCollider> rayRst)
			{
				cache.Clear();
				if (rayRst == null)
					return;
				foreach (var box in rayRst)
				{
					if (box == null)
						continue;
					cache.Add(box, new QuickHash(box));
				}
			}
		}

		private class QuickHash
		{
			public readonly BoxCollider box;
			public string check;
			public QuickHash(BoxCollider box)
			{
				this.box = box;
				this.check = $"{box.transform.position:F1}{box.size:F0}{box.transform.eulerAngles:F0}{box.center:F0}{box.size}";
			}
		}

		private Collider[] m_TmpSensor = new Collider[10];
		private List<BoxCollider> m_TmpList = new List<BoxCollider>(10);

		private void CleanCache()
		{
			if (m_TmpList.Count > 0)
				m_TmpList.Clear();
			for (int i = 0; i < m_TmpSensor.Length; ++i)
			{
				m_TmpSensor[i] = null;
			}
		}

		public void CullOverlap()
		{
			if (m_RawBorders == null)
				throw new System.Exception("Raw borders not inited.");
			for (int i = 0; i < m_RawBorders.Length; ++i)
			{
				// Left, top, right, bottom
				var isVertical = i % 2 == 0;
				CullOverlap(m_RawBorders[i], isVertical);
			}
		}

		private void CullOverlap(CullInfo data, bool isVertical)
		{
			var border = data.collider;
			CleanCache();
			var pivot = data.transform.position + data.collider.center;
			var hitted = Physics.OverlapBoxNonAlloc(pivot, border.size * 0.5f, m_TmpSensor, Quaternion.identity, DwCore.layer.ScreenLayerMask);
			if (hitted == 0 &&
				data.culled.Count == 1 &&
				data.culled[0].name == CLONE_COLLIDER)
			{
				// Special cases, early return.
				// no overlap, and already cloned it before.
				return;
			}

			// Quick check is same culled or not.
			if (data.NoChanged() && data.IsSameSources(m_TmpSensor))
				return;

			// Now start the full checking.
			data.ClearSubBox();
			m_TmpList.Clear();
			foreach (Collider collider in m_TmpSensor)
			{
				if (collider == null)
					continue;
				if (collider == border)
					throw new System.Exception("Self collider should not be here,\nwhen layer setting error this may happen.");
				if (border.transform.IsChildOf(collider.transform))
					continue; // ignore current screen collision
				if (collider is not BoxCollider screen)
					continue; // only deal with box collider (AABB) checking.
				m_TmpList.Add(screen);

				var min0 = isVertical ? border.bounds.min.y : border.bounds.min.x;
				var max0 = isVertical ? border.bounds.max.y : border.bounds.max.x;
				var min1 = isVertical ? screen.bounds.min.y : screen.bounds.min.x;
				var max1 = isVertical ? screen.bounds.max.y : screen.bounds.max.x;
				if (min1 > min0)
				{
					data.culled.Add(CreateSubBox(border, min0, min1, isVertical));
				}
				if (max1 < max0)
				{
					data.culled.Add(CreateSubBox(border, max1, max0, isVertical));
				}
			}

			if (m_TmpList.Count > 0)
			{
				data.KeepRecords(m_TmpList);
				return;
			}

			// else, none of above matching. this border collision nothing.
			data.ClearSubBox();
			data.culled.Add(DumpBox(border));
			data.KeepRecords(null);
		}

		private const string CLONE_COLLIDER = "CloneCollider";
		private const string CULLED_COLLIDER = "CulledCollider";
		private BoxCollider DumpBox(BoxCollider original)
		{
			var subBox = new GameObject(CLONE_COLLIDER, typeof(BoxCollider));
			subBox.transform.SetParent(original.transform, false);
			subBox.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
			if (DwCore.layer.BorderLayer >= 0)
				subBox.gameObject.layer = DwCore.layer.BorderLayer;
			var boxCollider = subBox.GetComponent<BoxCollider>();
			boxCollider.size = original.size;
			boxCollider.center = original.center;
			return boxCollider;
		}

		private BoxCollider CreateSubBox(BoxCollider original, float min, float max, bool isVertical)
		{
			var axisDir = isVertical ? Vector3.up : Vector3.right;
			
			var orgSize = original.size;
			var diff = Mathf.Abs(max - min);
			var vector = axisDir * diff;
			var halfSize = isVertical ? orgSize.y * 0.5f : orgSize.x * 0.5f;
			var resetVector = axisDir * -halfSize;

			//var center = resetVector + (vector * 0.5f);
			var p = original.transform.position;
			var anchor = (min + max) * 0.5f;
			var pivot = isVertical ? new Vector3(p.x, anchor, p.z) : new Vector3(anchor, p.y, p.z);
			var center = original.transform.InverseTransformPoint(pivot);

			var size = isVertical ? new Vector3(orgSize.x, 0f, orgSize.z) : new Vector3(0f, orgSize.y, orgSize.z);
			size += axisDir * diff;

			var subBox = new GameObject(CULLED_COLLIDER, typeof(BoxCollider));
			if (DwCore.layer.BorderLayer >= 0)
				subBox.gameObject.layer = DwCore.layer.BorderLayer;
			subBox.transform.SetParent(original.transform, false);
			subBox.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
			var boxCollider = subBox.GetComponent<BoxCollider>();
			boxCollider.size = size;
			boxCollider.center = center;
			return boxCollider;
		}
		#endregion Cull Overlap
	}
}