using BehaviorDesigner.Runtime.Tasks;
namespace Gaia
{
	[TaskCategory("Gaia")]
	[TaskName("Clean Character Pose")]
	[TaskDescription("Clean Character Pose")]
	public class CleanCharacterPose : CharacterAction
	{
		protected override eState OnModelViewUpdate()
		{
			Character.CleanPose();
			return eState.Success;
		}
	}
}