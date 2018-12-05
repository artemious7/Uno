using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Controls
{
	partial class RatingControl
    {
		TextBlock m_captionTextBlock;

		CompositionPropertySet m_sharedPointerPropertySet = null ;

		StackPanel m_backgroundStackPanel;
		StackPanel m_foregroundStackPanel;

		bool m_isPointerOver = false ;
		bool m_isPointerDown = false ;
		double m_mousePercentage = 0.0 ;

		RatingInfoType m_infoType = RatingInfoType.Font ;

		// Holds the value of the Rating control at the moment of engagement,
		// used to handle cancel-disengagements where we reset the value.
		double m_preEngagementValue = 0.0 ;
		bool m_disengagedWithA = false ;
		bool m_shouldDiscardValue = true ;

		// event_token m_pointerCancelledToken;
		// event_token m_pointerCaptureLostToken;
		// event_token m_pointerMovedToken;
		// event_token m_pointerEnteredToken;
		// event_token m_pointerExitedToken;
		// event_token m_pointerPressedToken;
		// event_token m_pointerReleasedToken;
		// event_token m_captionSizeChangedToken;
		// event_token m_fontFamilyChangedToken;

		// UISettings::TextScaleFactorChanged_revoker m_textScaleChangedRevoker = ;
		// static UISettings GetUISettings();

		// DispatcherHelper m_dispatcherHelper = *this ;
	}
}
