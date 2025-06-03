using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
    [CreateAssetMenu(fileName = "FaceRigDatabase", menuName = "Gaia/FaceRigDatabase", order = 1)]
	public class FaceRigDatabase : ScriptableObject
    {
		[System.Flags]
        public enum eShapeType
		{
			LeftEye = 1 << 1,
			RightEye = 1 << 2,
			Eyes = LeftEye | RightEye,

			BrowLeft = 1 << 3,
			BrowRight = 1 << 4,
			Brow = BrowLeft | BrowRight,

			Cheek = 1 << 5,
			Mouth = 1 << 6,
			Nose = 1 << 7,
			Tongue = 1 << 8,
			Jaw = 1 << 9,

			Face = Eyes | Brow | Cheek | Mouth | Nose | Tongue | Jaw,
		}

		[System.Serializable]
		public struct ShapeInfo
        {
            public string name;
            public int index;
			public eShapeType shapeType;
			/// <summary>
			/// provide a range of 0-1 for the blendshape weight
			/// the **blink cap** will use this value as reference
			/// to caluclate the final blendshape weight max cap value when the eye blink.
			/// </summary>
			[Range(0f, 1f)] public float leftBlinkCap, rightBlinkCap;
		}

		public ShapeInfo[] data = new ShapeInfo[0];

		private string[] m_Names = null;
		private int[] m_Indices = null;
		private void OnValidate()
		{
			m_Names = null;
			m_Indices = null;
			Editor_Fetch(out m_Names, out m_Indices);
		}

		public void Editor_Fetch(out string[] names, out int[] indices)
		{
			if (m_Names != null && m_Indices != null)
			{
				names = m_Names;
				indices = m_Indices;
				return;
			}

			var cnt = data.Length;
  			names = new string[cnt + 1];
			indices = new int[cnt + 1];

			names[0] = "None";
			indices[0] = -1;

			for (int i = 0; i < data.Length; ++i)
			{
				names[i+1] = data[i].name;
				indices[i+1] = data[i].index;
			}
		}

	}
}