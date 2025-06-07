using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using Kit2;

namespace Gaia
{
    /// <summary>
    /// This class is used to define a Gaia animation.
    /// </summary>
    [RequireComponent(typeof(PlayableDirector))]
	public class GxTimelineAsset : MonoBehaviour
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

        
		// TODO: bind actor to track, so that the timeline can control the actor's animation.

		/// <summary>Bind target actor to related track</summary>
		/// <param name="actor"></param>
		public void Bind(Animator actor)
        {
            var playableAsset = Director.playableAsset as GxPlayableAsset;

            //Director.GetGenericBinding(TrackAsset);
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