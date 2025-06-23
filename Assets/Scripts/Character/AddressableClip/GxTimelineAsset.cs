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
        [SerializeField] private PlayableDirector m_director = null;
        public PlayableDirector Director
        {
            get
            {
                if (m_director == null)
                {
                    m_director = GetComponent<PlayableDirector>();
                }
                return m_director;
            }
		}

		private void Reset()
		{
			m_director = GetComponent<PlayableDirector>();
		}

		// TODO: bind actor to track, so that the timeline can control the actor's animation.
		[SerializeField] private GxRetargeting m_Retargeting;


		public GxRetargeting GetRetargeting() => m_Retargeting;
		public void AssignRetargeting(GxRetargeting value) => m_Retargeting = value;

		public bool isLoop;
		public float duration;
		public void UpdateInfo(AnimationClip clip)
		{
			this.duration = clip.length;
			this.isLoop = clip.isLooping;
			Director.extrapolationMode = isLoop ?
				DirectorWrapMode.Loop :
				DirectorWrapMode.Hold;
		}

		private ISpawner m_Spawner;
		private Dictionary<Renderer, bool> m_Renderers = null;
		private void Awake()
		{
			if (m_Renderers == null)
				m_Renderers = new Dictionary<Renderer, bool>();
			foreach (var renderer in GetComponentsInChildren<Renderer>())
			{
				m_Renderers.Add(renderer, renderer.enabled);
			}
		}

		public void OnSpawn(ISpawner pool)
		{
			this.m_Spawner = pool;
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