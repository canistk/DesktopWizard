using Kit2.Tasks;

namespace Gaia
{
	public abstract class GxCharacterTask : MyTask
	{
		public GxCharacter Character { get; private set; }
		public GxCharacterTask(GxCharacter character)
		{
			this.Character = character;
		}

		protected override void OnDisposing()
		{
			base.OnDisposing();
			this.Character = null;
		}
	}
}