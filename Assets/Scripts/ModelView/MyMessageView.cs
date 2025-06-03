using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Deepseek;
namespace Gaia
{
	public abstract class MyMessageView<Msg> : ViewBase<MyMessageCtrl, Message>
		where Msg : Message
	{
		protected sealed override void OnViewUpdate(Message data)
		{
            if (data is Msg _data)
            {
                OnViewUpdate(_data);
            }
			else
			{
				OnViewUpdateInvalid(data);
			}
		}

		protected abstract void OnViewUpdate(Msg data);
	}
}