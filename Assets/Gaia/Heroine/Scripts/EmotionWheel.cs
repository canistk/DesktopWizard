using Kit2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;
namespace Gaia
{
    public class EmotionWheel : MonoBehaviour, IBlendShapeRequest
    {
		public FaceRig m_FaceRig = null;
        [Header("Wheel of Emotions")]
        [Range(0f,1f)]public float m_Anger = 0f;
        [Range(0f,1f)]public float m_Frightened = 0f;
        [Range(0f,1f)]public float m_Speechless = 0f;
        [Range(0f,1f)]public float m_Happy = 0f;
        [Range(0f,1f)]public float m_Sadness = 0f;
        [Range(0f,1f)]public float m_Sleep = 0f;
        

		[SerializeField] float m_BlendSpeed = 5f;
        private enum eEmotion
		{
            Neutral = 0,
			Anger,
			Frightened,
			Speechless,
			Smile,
			Sadness,
			Sleep,
		}

		private Dictionary<eEmotion, eHeroineFaceRig[]> mapping = new Dictionary<eEmotion, eHeroineFaceRig[]>
		{
			{ eEmotion.Neutral, new eHeroineFaceRig[0] },

			{ eEmotion.Anger, new eHeroineFaceRig[]{
				eHeroineFaceRig.anger01,
				eHeroineFaceRig.anger02,
			}},
			{ eEmotion.Frightened, new eHeroineFaceRig[]{
				eHeroineFaceRig.frightened01,
				eHeroineFaceRig.frightened02,
			}},
			{ eEmotion.Speechless, new eHeroineFaceRig[]{
				eHeroineFaceRig.speechless,
			}},
			{ eEmotion.Smile, new eHeroineFaceRig[]{
                eHeroineFaceRig.smile01,
                eHeroineFaceRig.smile02,
				eHeroineFaceRig.smile03,
				eHeroineFaceRig.smile04,
				eHeroineFaceRig.smile05,
			}},
			{ eEmotion.Sadness, new eHeroineFaceRig[]{
				eHeroineFaceRig.unhappy01,
				eHeroineFaceRig.unhappy02,
				eHeroineFaceRig.sad01,
				eHeroineFaceRig.sad02,
			}},
			{ eEmotion.Sleep, new eHeroineFaceRig[]{
				eHeroineFaceRig.sleepy,
				eHeroineFaceRig.sleep01,
				eHeroineFaceRig.sleep02,
			}},
		};
		
		[SerializeField]
		private eEmotion m_Emotion = eEmotion.Neutral;
		private eEmotion m_LastEmotion = eEmotion.Neutral;
		private int m_BlendShapeIdx = -1, m_LastBlendShapeIdx = -1;
		private List<int> m_ResetIdxs = new List<int>();
        private float[] rawData => new float[] { m_Anger, m_Frightened, m_Speechless, m_Happy, m_Sadness, m_Sleep };
		private void OnEnable()
		{
			if (m_FaceRig)
				m_FaceRig.Register(this);
		}
		private void OnDisable()
		{
			if (m_FaceRig)
				m_FaceRig.Unregister(this);
		}

		private void FixedUpdate()
		{
            DefineCurrentEmotion();
		}

		private void DefineCurrentEmotion() 
		{
            // find the most dominant emotion
            int dominantIdx = -1;
            float maxValue = 0;
            for (int i = 0; i < rawData.Length; ++i)
            {
                if (rawData[i] <= maxValue)
                    continue;
				
				maxValue = rawData[i];
				dominantIdx = i;
            }

            // since the neutral is 0, we need to add 1 to the index
            m_Emotion = (eEmotion)dominantIdx + 1;

			if (m_LastEmotion == m_Emotion)
				return;

			// select a random blend shape index from the emotion group.
			m_LastEmotion = m_Emotion;
			if (m_LastBlendShapeIdx != -1)
			{
				// reset the last blend shape index
				m_ResetIdxs.Add(m_LastBlendShapeIdx);
			}
			m_LastBlendShapeIdx = m_BlendShapeIdx;

			if (mapping.TryGetValue(m_Emotion, out var idxArr) && idxArr.Length > 0)
			{
				var idx = Random.Range(0, idxArr.Length);
				m_BlendShapeIdx = (int)idxArr[idx];
			}
			else
			{
				// it's not necessary to set the rig id
				m_BlendShapeIdx = -1;
			}
		}

		float IBlendShapeRequest.GetBlendWeight()
		{
			return enabled ? 1f : 0f;
		}

		IEnumerable<BlendShapeRequest> IBlendShapeRequest.GetBlendShapeRequests()
		{
			var t = Time.deltaTime * Mathf.Max(1f, m_BlendSpeed);
			if (m_ResetIdxs.Count > 0)
			{
				// reset the last last blend shape index, no blending
				for (int i = 0; i < m_ResetIdxs.Count; ++i)
				{
					var idx = m_ResetIdxs[i];
					if (idx == m_LastBlendShapeIdx ||
						idx == m_BlendShapeIdx)
						continue; // skip when it's the blending index
					yield return new BlendShapeRequest(idx, 0f);
				}
				m_ResetIdxs.Clear();
			}
			if (m_LastBlendShapeIdx != -1)
			{
				Debug.Assert(m_LastBlendShapeIdx != m_BlendShapeIdx, $"Unexpected situation, last index ({m_LastBlendShapeIdx}) & index({m_BlendShapeIdx}) shouldn't be same.");
				// reset the last blend shape index
				var current = m_FaceRig.GetBlendShapeWeight01(m_LastBlendShapeIdx);
				var value = Mathf.Lerp(current, 0f, 0.5f);
				yield return new BlendShapeRequest(m_LastBlendShapeIdx, value);
				if (value < 0.1f)
				{
					m_ResetIdxs.Add(m_LastBlendShapeIdx);
					m_LastBlendShapeIdx = -1;
				}
			}
			if (m_BlendShapeIdx != -1)
			{
				// set the current blend shape index
				var current = m_FaceRig.GetBlendShapeWeight01(m_BlendShapeIdx);
				var value = Mathf.Lerp(current, 1f, t);
				if (value > 0.9f)
					value = 1f; // force to one
				yield return new BlendShapeRequest(m_BlendShapeIdx, 1f);
			}
		}
	}
}