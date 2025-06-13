using Kit2.Tasks;

namespace Gaia
{
	public abstract class GxCharacterTask : MyTaskWithState
	{
		protected GxCharacter Character;
		public GxCharacterTask(GxCharacter character)
		{
			Character = character;
		}
	}
}