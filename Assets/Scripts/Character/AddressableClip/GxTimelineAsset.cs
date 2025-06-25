using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using Kit2;
using Kit2.Pooling;
using Kit2.ObjectPool;

namespace Gaia
{
    /// <summary>
    /// This class is used to define a Gaia animation.
    /// </summary>
    [RequireComponent(typeof(PlayableDirector))]
	public class GxTimelineAsset : TimeLineHelper, ISpawnToken
    {
        // TODO: bind actor to track, so that the timeline can control the actor's animation.
		[SerializeField] private GxRetargeting m_Retargeting;


		public GxRetargeting GetRetargeting() => m_Retargeting;
		public void AssignRetargeting(GxRetargeting value) => m_Retargeting = value;

		/// <summary>
		/// Remark, it's copy value from AnimationClip.isLooping, not from PlayableDirector.extrapolationMode.
		/// <see cref="UpdateInfo(AnimationClip)"/>
		/// </summary>
		[SerializeField] bool m_IsLoop;
		[SerializeField] float m_Duration;
		public bool IsLoop => m_IsLoop;
		public float Duration => m_Duration;
		public void UpdateInfo(AnimationClip clip)
		{
			this.m_Duration = clip.length;
			this.m_IsLoop = clip.isLooping;
			playableDirector.extrapolationMode = m_IsLoop ?
				DirectorWrapMode.Loop :
				DirectorWrapMode.Hold;
		}

		private ISpawner m_Spawner;
		private Dictionary<Renderer, bool> m_Renderers = null;
		protected override void Awake()
		{
			if (m_Renderers == null)
				m_Renderers = new Dictionary<Renderer, bool>();
			foreach (var renderer in GetComponentsInChildren<Renderer>())
			{
				m_Renderers.Add(renderer, renderer.enabled);
			}
		}

		private float m_SpawnTime = 0f;
		public float playedTime => Time.timeSinceLevelLoad - m_SpawnTime;
		public double InitialTime => playableDirector.initialTime;

		public void OnSpawn(ISpawner pool)
		{
			this.m_Spawner = pool;
			m_SpawnTime = Time.timeSinceLevelLoad;
			foreach (var renderer in GetComponentsInChildren<Renderer>())
			{
				renderer.enabled = false; // Disable renderers by default
			}
			// Always animate to ensure the character is animated even when not visible
			m_Retargeting.animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
		}

		public void OnDespawn()
		{
		}

		public void Despawn()
		{
			if (m_Spawner != null)
			{
				m_Spawner.Despawn(gameObject);
			}
			else
			{
				Debug.LogWarning("GxTimelineAsset is not spawned by a pool, cannot despawn.");
			}
		}
	}

	#region Character Playable Asset
	[TrackClipType(typeof(GxCharacterTrack))]
	[TrackBindingType(typeof(GxCharacter))]
	public class GxCharacterTrack : TrackAsset
	{
		// public ExposedReference<ParticleSystem> m_TestRef;
		public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
		{
			var playableDirector	= go.GetComponent<PlayableDirector>();
			var binding				= playableDirector.GetGenericBinding(this);
			var character			= binding as GxCharacter;
			foreach (var clip in GetClips())
			{
				if (clip.asset is not GxCharacterPlayableAsset charAsset)
					continue;
				charAsset.SetCharacter(this, character);
			}

			// var vfx = m_TestRef.Resolve(graph.GetResolver());
			return base.CreateTrackMixer(graph, go, inputCount);
		}
	}

	/// <summary><see cref="GxCharacterPlayableAsset{BEHAVIOUR}"/></summary>
	public abstract class GxCharacterPlayableAsset : GxPlayableAsset
	{
		public GxCharacter Character { get; private set; }
		internal void SetCharacter(TrackAsset track, GxCharacter character)
		{
			if (track is not GxCharacterTrack gxTrack)
				throw new System.InvalidCastException("Track is not a GxCharacterTrack.");
			Character = character;
		}
	}
	public abstract class GxCharacterPlayableAsset<BEHAVIOUR> : GxCharacterPlayableAsset
		where BEHAVIOUR : GxCharacterBehaviour, new()
	{
		public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
		{
			var playable = ScriptPlayable<BEHAVIOUR>.Create(graph);
			var behaviour = playable.GetBehaviour();
			return playable;
		}
	}

    public abstract class GxCharacterBehaviour : GxPlayableBehaviour
	{
		public GxCharacter Character { get; private set; }
	}
	#endregion Character Playable Asset

	#region Base timline asset wrapper
	public abstract class GxPlayableAsset : PlayableAsset
    {
		/***
        // Example of a Gaia playable asset for the timeline.
        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            // This method is used to create a playable for the Gaia timeline.
            // <example>
            var playable = ScriptPlayable<GxPlayableBehaviour>.Create(graph);
            var behaviour = playable.GetBehaviour();
            return playable;
            // </example>
        }
        //**/
	}

    /// <summary><see cref="TrackAsset"/></summary>
	public abstract class GxTrackAsset : TrackAsset { }
	/// <summary><see cref="PlayableBehaviour"/></summary>
	public abstract class GxPlayableBehaviour : PlayableBehaviour { }
	#endregion Base timline asset wrapper
}