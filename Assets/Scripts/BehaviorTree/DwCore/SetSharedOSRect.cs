using DesktopWizard;

namespace BehaviorDesigner.Runtime.Tasks.Unity.SharedVariables
{
	[TaskCategory("Unity/SharedVariable")]
	[TaskDescription("Sets the SharedOSRect variable to the specified object. Returns Success.")]
	public class SetSharedOSRect : BehaviorDesigner.Runtime.Tasks.Action
	{
		[Tooltip("The value to set the SharedRect to")]
		public SharedOSRect targetValue;
		[RequiredField]
		[Tooltip("The SharedRect to set")]
		public SharedOSRect targetVariable;

		public override TaskStatus OnUpdate()
		{
			targetVariable.Value = targetValue.Value;

			return TaskStatus.Success;
		}

		public override void OnReset()
		{
			targetValue = new OSRect();
			targetVariable = new OSRect();
		}
	}
}