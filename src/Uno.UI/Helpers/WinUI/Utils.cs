using System;
using System.Collections.Generic;
using System.Text;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace Uno.UI.Helpers.WinUI
{
	internal class LayoutUtils
	{
		public static double MeasureAndGetDesiredWidthFor(UIElement element, Size availableSize)
		{
			double desiredWidth = 0;
			if (element != null)
			{
				element.Measure(availableSize);
				desiredWidth = element.DesiredSize.Width;
			}
			return desiredWidth;
		}

		public static double GetActualWidthFor(UIElement element)
		{
			return (element != null ? (element as FrameworkElement).ActualWidth : 0);
		}
	}
}
