using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gaia
{
	public abstract class GxUIBoolean : GxUIBooleanBase<bool>
	{
		protected string[] m_OptionNames = new[]
		{
			"UI/DLG_OPTIONS_TXT_OptionDisable",
			"UI/DLG_OPTIONS_TXT_OptionEnable"
		};
		protected bool[] m_OptionValus = new[]
		{
			false,
			true,
		};
		public override string[] GetOptionNames()
		{
			return m_OptionNames;
		}
		protected override bool[] GetOptionValues()
		{
			return m_OptionValus;
		}
	}
}