using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Controls
{
	internal enum NavigationViewVisualStateDisplayMode
	{
		Compact,
		Expanded,
		Minimal,
		MinimalWithBackButton
	}

	internal enum NavigationViewListPosition
	{
		LeftNav,
		TopPrimary,
		TopOverflow
	}

	internal enum NavigationViewPropagateTarget
	{
		LeftListView,
		TopListView,
		OverflowListView,
		All
	}

	public class NavigationViewHelper
	{
		static string c_OnLeftNavigationReveal = "OnLeftNavigationReveal";
		static string c_OnLeftNavigation = "OnLeftNavigation";
		static string c_OnTopNavigationPrimary = "OnTopNavigationPrimary";
		static string c_OnTopNavigationPrimaryReveal = "OnTopNavigationPrimaryReveal";
		static string c_OnTopNavigationOverflow = "OnTopNavigationOverflow";

	}
}
