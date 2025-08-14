using BehaviorDesigner.Runtime.Tasks;
namespace Gaia
{
	[TaskCategory("Gaia")]
	[TaskName("Clean Character Motions")]
	[TaskDescription("Clean Character Motions")]
	public class CleanCharacterMotions : CharacterAction
	{
		protected override eState OnModelViewUpdate()
		{
			Character.CleanMotions();

			return eState.Success;
		}
	}
}