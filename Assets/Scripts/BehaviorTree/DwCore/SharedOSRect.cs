using DesktopWizard;
namespace BehaviorDesigner.Runtime
{
	[System.Serializable]
	public class SharedOSRect : SharedVariable<OSRect>
	{
		public static implicit operator SharedOSRect(OSRect value) { return new SharedOSRect { mValue = value }; }
	}
}