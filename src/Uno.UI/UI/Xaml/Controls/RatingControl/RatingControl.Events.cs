using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;

namespace Windows.UI.Xaml.Controls
{
	public partial class RatingControl
	{
		public event TypedEventHandler<RatingControl, object> ValueChanged;

		private void RaiseValueChanged()
		{
			ValueChanged?.Invoke(this, null);
		}
	}
}
