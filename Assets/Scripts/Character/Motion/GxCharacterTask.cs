using Kit2.Tasks;

namespace Gaia
{
	public abstract class GxCharacterTask : MyTask
	{
		public readonly GxCharacter Character;
		public GxCharacterTask(GxCharacter character)
		{
			Character = character;
		}
	}
}