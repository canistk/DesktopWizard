using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
	[System.Serializable]
	public class GxPoseData
	{
		public string key;
		[SerializeField] GxMotionKey[] m_EnterTracks;
		[SerializeField] GxMotionKey[] m_LoopTracks;
		[SerializeField] GxMotionKey[] m_ExitTracks;

		public GxPoseData(string key, GxMotionKey[] enter, GxMotionKey[] loop, GxMotionKey[] exit)
		{
			this.key = key;
			this.m_EnterTracks = enter;
			this.m_LoopTracks = loop;
			this.m_ExitTracks = exit;
		}

		public GxPoseData(string key, GxMotionKey enter, GxMotionKey loop, GxMotionKey exit)
		{
			this.key = key;
			this.m_EnterTracks = new[] { enter };
			this.m_LoopTracks = new[] { loop };
			this.m_ExitTracks = new[] { exit };
		}

		public GxMotionKey[] GetEnterTracks() => m_EnterTracks;

		public GxMotionKey[] GetLoopTracks() => m_LoopTracks;

		public GxMotionKey[] GetExitTracks() => m_ExitTracks;
	}
}
