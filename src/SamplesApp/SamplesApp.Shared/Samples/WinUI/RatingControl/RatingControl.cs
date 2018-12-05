using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media;

namespace SamplesApp.Samples.WinUI.RatingControl
{
	enum RatingControlStates
	{
		Disabled = 0,
		Set = 1,
		PointerOverSet = 2,
		PointerOverPlaceholder = 3, // Also functions as the pointer over unset state at the moment
		Placeholder = 4,
		Unset = 5,
		Null = 6
	};

	enum RatingInfoType
	{
		None,
		Font,
		Image
	};

	partial class RatingControl : Control
    {
		const float c_horizontalScaleAnimationCenterPoint = 0.5f;
		const float c_verticalScaleAnimationCenterPoint = 0.8f;
		const int c_defaultRatingFontSizeForRendering = 32; // (32 = 2 * [default fontsize] -- because of double size rendering), remove when MSFT #10030063 is done
		const int c_itemSpacing = 8;

		const float c_mouseOverScale = 0.8f;
		const float c_touchOverScale = 1.0f;
		const float c_noPointerOverMagicNumber = -100;

		const int c_noValueSetSentinel = -1;

		Thickness c_focusVisualMargin = new Thickness( -8, -7, -8, 0 );


		public RatingControl()
		{
			DefaultStyleKey = GetType();
		}

		//~RatingControl()
		//{
		//	// We only need to use safe_get in the deconstruction loop
		//	RecycleEvents(true /* useSafeGet */);
		//}

		float RenderingRatingFontSize()
		{
			// MSFT #10030063 Replacing with Rating size DPs
			return (float) (c_defaultRatingFontSizeForRendering * GetUISettings().TextScaleFactor());
		}

		float ActualRatingFontSize()
		{
			return RenderingRatingFontSize() / 2;
		}

		void OnApplyTemplate()
		{
			RecycleEvents();

			if (GetTemplateChild("Caption") is TextBlock captionTextBlock)
			{
				m_captionTextBlock = captionTextBlock;
				m_captionSizeChangedToken = captionTextBlock.SizeChanged({ this, &OnCaptionSizeChanged });
			}

			if (GetTemplateChild("RatingBackgroundStackPane") is StackPanel backgroundStackPanel)
			{
				m_backgroundStackPanel = backgroundStackPanel;
				m_pointerCancelledToken = backgroundStackPanel.PointerCanceled({ this, &OnPointerCancelledBackgroundStackPanel });
				m_pointerCaptureLostToken = backgroundStackPanel.PointerCaptureLost({ this, &OnPointerCaptureLostBackgroundStackPanel });
				m_pointerMovedToken = backgroundStackPanel.PointerMoved({ this, &OnPointerMovedOverBackgroundStackPanel });
				m_pointerEnteredToken = backgroundStackPanel.PointerEntered({ this, &OnPointerEnteredBackgroundStackPanel });
				m_pointerExitedToken = backgroundStackPanel.PointerExited({ this, &OnPointerExitedBackgroundStackPanel });
				m_pointerPressedToken = backgroundStackPanel.PointerPressed({ this, &OnPointerPressedBackgroundStackPanel });
				m_pointerReleasedToken = backgroundStackPanel.PointerReleased({ this, &OnPointerReleasedBackgroundStackPanel });
			}

			m_foregroundStackPanel = GetTemplateChild("RatingForegroundStackPane") as StackPanel;

			//if (SharedHelpers.IsRS1OrHigher())
			//{
			//	// FUTURE: Ideally these would be in template overrides:

			//	// IsFocusEngagementEnabled means the control has to be "engaged" with 
			//	// using the A button before it actually receives key input from gamepad.
			//	FocusEngaged({ this, &OnFocusEngaged });
			//	FocusDisengaged({ this, &OnFocusDisengaged });
			//	IsFocusEngagementEnabled(true);

			//	// I've picked values so that these LOOK like the redlines, but these
			//	// values are not actually from the redlines because the redlines don't
			//	// consistently pick "distance from glyph"/"distance from edge of textbox"
			//	// so it's not possible to actually just have a consistent sizing model
			//	// here based on the redlines.
			//	FocusVisualMargin(c_focusVisualMargin);
			//}

			IsEnabledChanged += OnIsEnabledChanged;
			m_fontFamilyChangedToken = RegisterPropertyChangedCallback(
				Control.FontFamilyProperty, OnFontFamilyChanged);

			Visual visual = ElementCompositionPreview.GetElementVisual(this);
			Compositor comp = visual.Compositor;

			m_sharedPointerPropertySet = comp.CreatePropertySet();

			m_sharedPointerPropertySet.InsertScalar("starsScaleFocalPoint", c_noPointerOverMagicNumber);
			m_sharedPointerPropertySet.InsertScalar("pointerScalar", c_mouseOverScale);
 
			StampOutRatingItems();
			m_textScaleChangedRevoker = GetUISettings().TextScaleFactorChanged(var_revoke, { this, &OnTextScaleFactorChanged });
		}


		double CoerceValueBetweenMinAndMax(double value)
		{
			if (value < 0.0) // Force all negative values to the sentinel "unset" value.
			{
				value = c_noValueSetSentinel;
			}
			else if (value <= 1.0)
			{
				value = 1.0;
			}
			else if (value > MaxRating)
			{
				value = MaxRating;
			}

			return value;
		}

		// IUIElement / IUIElementOverridesHelper
		varmationPeer OnCreatevarmationPeer()
		{
			return new RatingControlvarmationPeer(this);
		}

		// private methods 
		bool IsItemInfoPresentAndFontInfo()
		{
			return m_infoType == RatingInfoType.Font;
		}

		bool IsItemInfoPresentAndImageInfo()
		{
			return m_infoType == RatingInfoType.Image;
		}

		// TODO: call me when font size changes, and stuff like that, glyph, etc
		void StampOutRatingItems()
		{
			if (m_backgroundStackPanel == null || m_foregroundStackPanel == null)
			{
				// OnApplyTemplate() hasn't executed yet, this is being called 
				// from a property value changed handler for markup set values.

				return;
			}

			// Background initialization:

			m_backgroundStackPanel.Children().Clear();

			if (IsItemInfoPresentAndFontInfo())
			{
				PopulateStackPanelWithItems("BackgroundGlyphDefaultTemplate", m_backgroundStackPanel, RatingControlStates.Unset);
			}
			else if (IsItemInfoPresentAndImageInfo())
			{
				PopulateStackPanelWithItems("BackgroundImageDefaultTemplate", m_backgroundStackPanel, RatingControlStates.Unset);
			}

			// Foreground initialization:
			m_foregroundStackPanel.Children().Clear();
			if (IsItemInfoPresentAndFontInfo())
			{
				PopulateStackPanelWithItems("ForegroundGlyphDefaultTemplate", m_foregroundStackPanel, RatingControlStates.Set);
			}
			else if (IsItemInfoPresentAndImageInfo())
			{
				PopulateStackPanelWithItems("ForegroundImageDefaultTemplate", m_foregroundStackPanel, RatingControlStates.Set);
			}
    
			UpdateRatingItemsAppearance();
		}

		void ReRenderCaption()
		{
			if (m_captionTextBlock != null)
			{
				ResetControlWidth();
			}
		}

		void UpdateRatingItemsAppearance()
		{
			if (m_foregroundStackPanel != null)
			{
				// TODO: MSFT 11521414 - complete disabled state functionality

				double placeholderValue = PlaceholderValue;
				double ratingValue = Value;
				double value = 0.0;
       
				if (m_isPointerOver)
				{
					value = Math.Ceil(m_mousePercentage * MaxRating);
					if (ratingValue == c_noValueSetSentinel)
					{
						if (placeholderValue == -1)
						{
							VisualStateManager.GoToState(this, "PointerOverPlaceholder", false);
							CustomizeStackPanel(m_foregroundStackPanel, RatingControlStates.PointerOverPlaceholder);
						}
						else
						{
							VisualStateManager.GoToState(this, "PointerOverUnselected", false);
							// The API is locked, so we can't change this part to be consistent any more:
							CustomizeStackPanel(m_foregroundStackPanel, RatingControlStates.PointerOverPlaceholder);
						}
					}
					else
					{
						VisualStateManager.GoToState(this, "PointerOverSet", false);
						CustomizeStackPanel(m_foregroundStackPanel, RatingControlStates.PointerOverSet);
					}
				}
				else if (ratingValue > c_noValueSetSentinel)
				{
					value = ratingValue;
					VisualStateManager.GoToState(this, "Set", false);
					CustomizeStackPanel(m_foregroundStackPanel, RatingControlStates.Set);
				}
				else if (placeholderValue > c_noValueSetSentinel)
				{
					value = placeholderValue;
					VisualStateManager.GoToState(this, "Placeholder", false);
					CustomizeStackPanel(m_foregroundStackPanel, RatingControlStates.Placeholder);
				} // there's no "unset" state because the foreground items are simply cropped out

				if (!IsEnabled)
				{
					// TODO: MSFT 11521414 - complete disabled state functionality [merge this code block with ifs above]
					VisualStateManager.GoToState(this, "Disabled", false);
					CustomizeStackPanel(m_foregroundStackPanel, RatingControlStates.Disabled);
				}

				int i = 0;
				foreach(var uiElement in m_foregroundStackPanel.Children)
				{
					// Handle clips on stars
					float width = RenderingRatingFontSize();
					if (i + 1 > value)
					{
						if (i < value)
						{
							// partial stars
							width *= (float)(value - math.Floor(value));
						}
						else
						{
							// empty stars
							width = 0.0f;
						}
					}

					Rect rect;
					rect.X = 0;
					rect.Y = 0;
					rect.Height = RenderingRatingFontSize();
					rect.Width = width;

					RectangleGeometry rg = new RectangleGeometry();
					rg.Rect = rect;
					uiElement.Clip = rg;

					i++;
				}

				ResetControlWidth();
			}
		}

		void ApplyScaleExpressionAnimation(UIElement uiElement, int starIndex)
		{ 
			Visual uiElementVisual = ElementCompositionPreview.GetElementVisual(uiElement);
			Compositor comp = uiElementVisual.Compositor;

			// starsScaleFocalPoint is updated in OnPointerMovedOverBackgroundStackPanel.
			// This expression uses the horizontal delta between pointer position and star center to calculate the star scale.
			// Star gets larger when pointer is closer to its center, and gets smaller when pointer moves further away.
			ExpressionAnimation ea = comp.CreateExpressionAnimation(
				"max( (-0.0005 * sharedPropertySet.pointerScalar * ((starCenterX - sharedPropertySet.starsScaleFocalPoint)*(starCenterX - sharedPropertySet.starsScaleFocalPoint))) + 1.0*sharedPropertySet.pointerScalar, 0.5)"
			);
			float starCenter = (float)(CalculateStarCenter(starIndex));
			ea.SetScalarParameter("starCenterX", starCenter);
			ea.SetReferenceParameter("sharedPropertySet", m_sharedPointerPropertySet);

			uiElementVisual.StartAnimation("Scale.X", ea);
			uiElementVisual.StartAnimation("Scale.Y", ea);

			// Star size = 16. 0.5 and 0.8 are just arbitrary center point chosen in design spec
			// 32 = star size * 2 because of the rendering at double size we do
			uiElementVisual.CenterPoint = new Vector3(c_defaultRatingFontSizeForRendering * c_horizontalScaleAnimationCenterPoint, c_defaultRatingFontSizeForRendering * c_verticalScaleAnimationCenterPoint, 0.0f));
		}

		void PopulateStackPanelWithItems(string templateName, StackPanel stackPanel, RatingControlStates state)
		{
			if (Application.Current.Resources.Lookup(templateName) is DataTemplate dt) {

				for (int i = 0; i < MaxRating; i++)
				{
					if (dt.LoadContent() is UIElement ui)
					{
						CustomizeRatingItem(ui, state);
						stackPanel.Children.Append(ui);
						ApplyScaleExpressionAnimation(ui, i);
					}
				}
			}
		}

		void CustomizeRatingItem(UIElement ui, RatingControlStates type)
		{
			if (IsItemInfoPresentAndFontInfo())
			{
				if (ui is TextBlock textBlock)
				{
					textBlock.FontFamily = FontFamily;
					textBlock.Text = GetAppropriateGlyph(type);
				}
			}
			else if (IsItemInfoPresentAndImageInfo())
			{
				if (ui is Image image)
				{
					image.Source =GetAppropriateImageSource(type));
					image.Width = RenderingRatingFontSize(); // 
					image.Height = RenderingRatingFontSize(); // MSFT #10030063 Replacing with Rating size DPs
				}
			}
			else
			{
				throw new InvalidOperationException("Runtime error, ItemInfo property is nul");
			}

		}

		void CustomizeStackPanel(StackPanel stackPanel, RatingControlStates state)
		{
			foreach(var child in stackPanel.Children)
			{
				CustomizeRatingItem(child, state);
			}
		}

		string GetAppropriateGlyph(RatingControlStates type)
		{
			if (!IsItemInfoPresentAndFontInfo())
			{
				throw new InvalidOperationException("Runtime error, tried to retrieve a glyph when the ItemInfo is not a RatingItemGlyphInfo");
			}

			RatingItemFontInfo rifi = ItemInfo as RatingItemFontInfo;

			switch (type)
			{
			case RatingControlStates.Disabled:
				return GetNextGlyphIfNull(rifi.DisabledGlyph, RatingControlStates.Set);

			case RatingControlStates.PointerOverSet:
				return GetNextGlyphIfNull(rifi.PointerOverGlyph, RatingControlStates.Set);

			case RatingControlStates.PointerOverPlaceholder:
				return GetNextGlyphIfNull(rifi.PointerOverPlaceholderGlyph, RatingControlStates.Placeholder);

			case RatingControlStates.Placeholder:
				return GetNextGlyphIfNull(rifi.PlaceholderGlyph, RatingControlStates.Set);

			case RatingControlStates.Unset:
				return GetNextGlyphIfNull(rifi.UnsetGlyph, RatingControlStates.Set);

			case RatingControlStates.Null:
				return {};

			default:
				return rifi.Glyph; // "Set" state

			}
		}

		string GetNextGlyphIfNull(string glyph, RatingControlStates fallbackType)
		{
			if (glyph.Length == 0)
			{
				if (fallbackType == RatingControlStates.Null)
				{
					return null;
				}
				return GetAppropriateGlyph(fallbackType);
			}
			return glyph;
		}

		ImageSource GetAppropriateImageSource(RatingControlStates type)
		{
			if (!IsItemInfoPresentAndImageInfo())
			{
				throw new InvalidOperationException("Runtime error, tried to retrieve an image when the ItemInfo is not a RatingItemImageInfo");
			}

			RatingItemImageInfo imageInfo = ItemInfo as RatingItemImageInfo;

			switch (type)
			{
			case RatingControlStates.Disabled:
				return GetNextImageIfNull(imageInfo.DisabledImage, RatingControlStates.Set);

			case RatingControlStates.PointerOverSet:
				return GetNextImageIfNull(imageInfo.PointerOverImage, RatingControlStates.Set);

			case RatingControlStates.PointerOverPlaceholder:
				return GetNextImageIfNull(imageInfo.PointerOverPlaceholderImage, RatingControlStates.Placeholder);

			case RatingControlStates.Placeholder:
				return GetNextImageIfNull(imageInfo.PlaceholderImage, RatingControlStates.Set);

			case RatingControlStates.Unset:
				return GetNextImageIfNull(imageInfo.UnsetImage, RatingControlStates.Set);

			case RatingControlStates.Null:
				return null;

			default:
				return imageInfo.Image; // "Set" state

			}
		}

		ImageSource GetNextImageIfNull(ImageSource image, RatingControlStates fallbackType)
		{
			if (image == null)
			{
				if (fallbackType == RatingControlStates.Null)
				{
					return null;
				}
				return GetAppropriateImageSource(fallbackType);
			}
			return image;
		}

		void ResetControlWidth()
		{
			double newWidth = CalculateTotalRatingControlWidth();
			Control thisAsControl = this;
			thisAsControl.Width = newWidth;
		}

		void ChangeRatingBy(double change, bool originatedFromMouse)
		{
			if (change != 0.0)
			{
				double ratingValue = 0.0;
				double oldRatingValue = Value();
				if (oldRatingValue != c_noValueSetSentinel)
				{
					// If the Value was programmatically set to a fraction, drop that fraction before we modify it
					if ((int)Value != Value)
					{
						if (change == -1)
						{
							ratingValue = (int)Value;
						}
						else
						{
							ratingValue = (int)Value + change;
						}
					}
					else
					{
						oldRatingValue = ratingValue = oldRatingValue;
						ratingValue += change;
					}
				}
				else
				{
					ratingValue = InitialSetValue();
				}

				SetRatingTo(ratingValue, originatedFromMouse);
			}
		}

		void SetRatingTo(double newRating, bool originatedFromMouse)
		{
			double ratingValue = 0.0;
			double oldRatingValue = Value;

			ratingValue = Math.Min(newRating, (double)(MaxRating()));
			ratingValue = Math.Max(ratingValue, 0.0);

			// The base case, and the you have no rating, and you pressed left case [wherein nothing should happen]
			if (oldRatingValue > c_noValueSetSentinel || ratingValue != 0.0)
			{
				if (!IsClearEnabled() && ratingValue <= 0.0)
				{
					Value = 1.0;
				}
				else if (ratingValue == oldRatingValue && IsClearEnabled && (ratingValue != MaxRating || originatedFromMouse))
				{
					// If you increase the Rating via the keyboard/gamepad when it's maxed, the value should stay stable.
					// But if you click a star that represents the current Rating value, it should clear the rating.

					Value = c_noValueSetSentinel;
				}
				else if (ratingValue > 0.0)
				{
					Value = ratingValue;
				}
				else
				{
					Value = c_noValueSetSentinel;
				}

				//if (SharedHelpers.IsRS1OrHigher() && IsFocusEngaged() && SharedHelpers.IsAnimationsEnabled())
				//{
				//	double focalPoint = CalculateStarCenter((int)(ratingValue - 1.0));
				//	m_sharedPointerPropertySet.InsertScalar("starsScaleFocalPoint", (float)(focalPoint));
				//}

				// Notify that the Value has changed
				m_valueChangedEventSource(this, null);
			}
		}

		void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
		{
			var property = args.Property;

			// Do coercion first.
			if (property == MaxRatingProperty)
			{
				// Enforce minimum MaxRating
				var value = (int)(args.NewValue);
				var coercedValue = Math.Max(1, value);

				if (Value > coercedValue)
				{
					Value = coercedValue;
				}

				if (PlaceholderValue > coercedValue)
				{
					PlaceholderValue = coercedValue;
				}

				if (coercedValue != value)
				{
					SetValue(property, coercedValue);
					return;
				}
			}
			else if (property == PlaceholderValueProperty || property == ValueProperty)
			{
				var value = (double)(args.NewValue);
				var coercedValue = CoerceValueBetweenMinAndMax(value);
				if (value != coercedValue)
				{
					SetValue(property, coercedValue);
					// early return, we'll come back to handle the change to the corced value.
					return;
				}
			}

			// Property value changed handling.
			if (property == CaptionProperty)
			{
				OnCaptionChanged(args);
			}
			else if (property == InitialSetValueProperty)
			{
				OnInitialSetValueChanged(args);
			}
			else if (property == IsClearEnabledProperty)
			{
				OnIsClearEnabledChanged(args);
			}
			else if (property == IsReadOnlyProperty)
			{
				OnIsReadOnlyChanged(args);
			}
			else if (property == ItemInfoProperty)
			{
				OnItemInfoChanged(args);
			}
			else if (property == MaxRatingProperty)
			{
				OnMaxRatingChanged(args);
			}
			else if (property == PlaceholderValueProperty)
			{
				OnPlaceholderValueChanged(args);
			}
			else if (property == ValueProperty)
			{
				OnValueChanged(args);
			}
		}

		void OnCaptionChanged(DependencyPropertyChangedEventArgs /*args*/)
		{
			ReRenderCaption();
		}

		void OnFontFamilyChanged(DependencyObject sender, DependencyProperty args)
		{
			if (m_backgroundStackPanel != null) // We don't want to do this for the initial property set
			{
				for (int i = 0; i < MaxRating; i++)
				{
					// FUTURE: handle image rating items
					if (var backgroundTB = safe_cast<TextBlock>(m_backgroundStackPanel.Children().GetAt(i)))
					{
						CustomizeRatingItem(backgroundTB, RatingControlStates.Unset);
					}

					if (var foregroundTB = safe_cast<TextBlock>(m_foregroundStackPanel.Children().GetAt(i)))
					{
						CustomizeRatingItem(foregroundTB, RatingControlStates.Set);
					}
				}
			}

			UpdateRatingItemsAppearance();
		}

		void OnInitialSetValueChanged(DependencyPropertyChangedEventArgs /*args*/)
		{

		}

		void OnIsClearEnabledChanged(DependencyPropertyChangedEventArgs /*args*/)
		{

		}

		void OnIsReadOnlyChanged(DependencyPropertyChangedEventArgs /*args*/)
		{
			// TODO: Colour changes - see spec
		}

		void OnItemInfoChanged(DependencyPropertyChangedEventArgs /*args*/)
		{
			bool changedType = false;

			if (ItemInfo == null)
			{
				m_infoType = RatingInfoType.None;
			}
			else if (ItemInfo.try_as<RatingItemFontInfo>())
			{
				if (m_infoType != RatingInfoType.Font && m_backgroundStackPanel /* prevent calling StampOutRatingItems() twice at initialisation */)
				{
					m_infoType = RatingInfoType.Font;
					StampOutRatingItems();
					changedType = true;
				}
			}
			else
			{
				if (m_infoType != RatingInfoType.Image)
				{
					m_infoType = RatingInfoType.Image;
					StampOutRatingItems();
					changedType = true;
				}
			}

			// We don't want to do this for the initial property set
			// Or if we just stamped them out
			if (m_backgroundStackPanel && !changedType)
			{
				for (int i = 0; i < MaxRating(); i++)
				{
					CustomizeRatingItem(m_backgroundStackPanel.Children().GetAt(i), RatingControlStates.Unset);
					CustomizeRatingItem(m_foregroundStackPanel.Children().GetAt(i), RatingControlStates.Set);
				}
			}

			UpdateRatingItemsAppearance();
		}

		void OnMaxRatingChanged(const DependencyPropertyChangedEventArgs& /*args*/)
		{
			StampOutRatingItems();
		}

		void OnPlaceholderValueChanged(const DependencyPropertyChangedEventArgs& /*args*/)
		{
			UpdateRatingItemsAppearance();
		}

		void OnValueChanged(const DependencyPropertyChangedEventArgs& /*args*/)
		{
			// Fire property change for UIA
			if (varmationPeer peer = FrameworkElementvarmationPeer.FromElement(this))
			{
				var ratingPeer = peer.as<RatingControlvarmationPeer>();
				get_self<RatingControlvarmationPeer>(ratingPeer)->RaisePropertyChangedEvent(Value());
			}

			UpdateRatingItemsAppearance();
		}

		void OnIsEnabledChanged(const IInspectable& /*sender*/, const DependencyPropertyChangedEventArgs& /*args*/)
		{
			// MSFT 11521414 TODO: change states (add a state)
			UpdateRatingItemsAppearance();
		}

		void OnCaptionSizeChanged(const IInspectable& /*sender*/, const SizeChangedEventArgs& /*args*/)
		{
			ResetControlWidth();
		}

		void OnPointerCancelledBackgroundStackPanel(const IInspectable& /*sender*/, const PointerRoutedEventArgs& args)
		{
			PointerExitedImpl(args);
		}

		void OnPointerCaptureLostBackgroundStackPanel(const IInspectable& /*sender*/, const PointerRoutedEventArgs& args)
		{
			// We capture the pointer because we want to support the drag off the
			// left side to clear the rating scenario. However, this means that
			// when we simply click to set values - we get here, but we don't want
			// to reset the scaling on the stars underneath the pointer.
			PointerExitedImpl(args, false /* resetScaleAnimation */);
		}

		void OnPointerMovedOverBackgroundStackPanel(const IInspectable& /*sender*/, const PointerRoutedEventArgs& args)
		{
			if (!IsReadOnly())
			{
				var point = args.GetCurrentPoint(m_backgroundStackPanel);
				float xPosition = point.Position().X;
				if (SharedHelpers.IsAnimationsEnabled())
				{
					m_sharedPointerPropertySet.InsertScalar("starsScaleFocalPoint", xPosition);
					var deviceType = args.Pointer().PointerDeviceType();

					switch (deviceType)
					{
					case PointerDeviceType.Touch:
						m_sharedPointerPropertySet.InsertScalar("pointerScalar", c_touchOverScale);
						break;
					default: // mouse, TODO: distinguish pen later
						m_sharedPointerPropertySet.InsertScalar("pointerScalar", c_mouseOverScale);
						break;
					}
				}

				m_mousePercentage = (double)(xPosition) / CalculateActualRatingWidth();

				UpdateRatingItemsAppearance();
				args.Handled(true);
			}
		}

		void OnPointerEnteredBackgroundStackPanel(const IInspectable& /*sender*/, const PointerRoutedEventArgs& args)
		{
			if (!IsReadOnly())
			{
				m_isPointerOver = true;
				args.Handled(true);
			}
		}

		void OnPointerExitedBackgroundStackPanel(const IInspectable& /*sender*/, const PointerRoutedEventArgs& args)
		{
			PointerExitedImpl(args);
		}

		void PointerExitedImpl(const PointerRoutedEventArgs& args, bool resetScaleAnimation)
		{
			var point = args.GetCurrentPoint(m_backgroundStackPanel);

			if (resetScaleAnimation)
			{
				m_isPointerOver = false;
			}

			if (!m_isPointerDown)
			{
				if (resetScaleAnimation)
				{
					m_sharedPointerPropertySet.InsertScalar("starsScaleFocalPoint", c_noPointerOverMagicNumber);
				}
				UpdateRatingItemsAppearance();
			}

			args.Handled(true);
		}

		void OnPointerPressedBackgroundStackPanel(const IInspectable& /*sender*/, const PointerRoutedEventArgs& args)
		{
			if (!IsReadOnly())
			{
				m_isPointerDown = true;

				// We capture the pointer on pointer down because we want to support
				// the drag off the left side to clear the rating scenario.
				m_backgroundStackPanel.CapturePointer(args.Pointer());
			}
		}

		void OnPointerReleasedBackgroundStackPanel(const IInspectable& /*sender*/, const PointerRoutedEventArgs& args)
		{
			if (!IsReadOnly())
			{
				var point = args.GetCurrentPoint(m_backgroundStackPanel);
				var xPosition = point.Position().X;

				double mousePercentage = xPosition / CalculateActualRatingWidth();
				SetRatingTo(ceil(mousePercentage * MaxRating()), true);

				if (SharedHelpers.IsRS1OrHigher())
				{
					ElementSoundPlayer.Play(ElementSoundKind.Invoke);
				}
			}

			if (m_isPointerDown)
			{
				m_isPointerDown = false;
				UpdateRatingItemsAppearance();
			}
		}

		double CalculateTotalRatingControlWidth()
		{
			double ratingStarsWidth = CalculateActualRatingWidth();
			var captionAsWinRT = unbox_value<string>(GetValue(s_CaptionProperty));
			double textSpacing = 0.0;

			if (captionAsWinRT.size() > 0)
			{
				// TODO MSFT #10030063: Convert to itemspacing DP
				textSpacing = c_itemSpacing;
			}

			double captionWidth = 0.0;
    
			if (m_captionTextBlock)
			{
				captionWidth = m_captionTextBlock.ActualWidth();
			}

			return ratingStarsWidth + textSpacing + captionWidth;
		}

		double CalculateStarCenter(int starIndex)
		{
			// TODO: sub in real API DP values
			// MSFT #10030063
			// [real Rating Size * (starIndex + 0.5)] + (starIndex * itemSpacing)
			return (ActualRatingFontSize() * (starIndex + 0.5)) + (starIndex * c_itemSpacing);
		}

		double CalculateActualRatingWidth()
		{
			// TODO: replace hardcoding
			// MSFT #10030063
			// (max rating * rating size) + ((max rating - 1) * item spacing)
			return (MaxRating() * ActualRatingFontSize()) + ((MaxRating() - 1) * c_itemSpacing);
		}

		// IControlOverrides
		void OnKeyDown(KeyRoutedEventArgs const& eventArgs)
		{
			if (eventArgs.Handled())
			{
				return;
			}

			if (!IsReadOnly())
			{
				bool handled = false;
				VirtualKey key = eventArgs.as<KeyRoutedEventArgs>().Key();
      
				double flowDirectionReverser = 1.0;

				if (FlowDirection() == FlowDirection.RightToLeft)
				{
					flowDirectionReverser *= -1.0;
				}

				var originalKey = eventArgs.as<KeyRoutedEventArgs>().OriginalKey();

				// Up down are right/left in keyboard only
				if (originalKey == VirtualKey.Up)
				{
					key = VirtualKey.Right;
					flowDirectionReverser = 1.0;
				}
				else if (originalKey == VirtualKey.Down)
				{
					key = VirtualKey.Left;
					flowDirectionReverser = 1.0;
				}

				if (originalKey == VirtualKey.GamepadDPadLeft || originalKey == VirtualKey.GamepadDPadRight ||
					originalKey == VirtualKey.GamepadLeftThumbstickLeft || originalKey == VirtualKey.GamepadLeftThumbstickRight)
				{
					if (SharedHelpers.IsRS1OrHigher())
					{
						ElementSoundPlayer.Play(ElementSoundKind.Focus);
					}
				}

				switch (key)
				{
				case VirtualKey.Left:
					ChangeRatingBy(-1.0 * flowDirectionReverser, false);
					handled = true;
					break;
				case VirtualKey.Right:
					ChangeRatingBy(1.0 * flowDirectionReverser, false);
					handled = true;
					break;
				case VirtualKey.Home:
					SetRatingTo(0.0, false);
					handled = true;
					break;
				case VirtualKey.End:
					SetRatingTo((double)(MaxRating()), false);
					handled = true;
					break;
				default:
					break;
				}

				eventArgs.Handled(handled);
			}

			__super.OnKeyDown(eventArgs);
		}

		// We use the same engagement model as Slider/sorta like ComboBox
		// Pressing GamepadA engages, pressing either GamepadA or GamepadB disengages,
		// where GamepadA commits the new value, and GamepadB discards and restores the old value.

		// The reason we do this in the OnPreviewKey* virtuals is we need
		// to beat the framework to handling this event. Because disengagement
		// happens on key down, and engagement happens on key up...
		// if we disengage on GamepadA, the framework would otherwise
		// varmatically reengage us.

		// Order:
		// OnPreviewKey* virtuals
		// PreviewKey subscribed events
		// [regular key events]

		void OnPreviewKeyDown(KeyRoutedEventArgs const& eventArgs)
		{
			if (eventArgs.Handled())
			{
				return ;
			}

			if (!IsReadOnly() && IsFocusEngaged() && IsFocusEngagementEnabled())
			{
				var originalKey = eventArgs.as<KeyRoutedEventArgs>().OriginalKey();
				if (originalKey == VirtualKey.GamepadA)
				{
					m_shouldDiscardValue = false;
					m_preEngagementValue = -1.0;
					RemoveFocusEngagement();
					m_disengagedWithA = true;
					eventArgs.Handled(true);
				}
				else if (originalKey == VirtualKey.GamepadB)
				{
					bool valueChanged = false;
					m_shouldDiscardValue = false;

					if (Value() != m_preEngagementValue)
					{
						valueChanged = true;
					}

					Value(m_preEngagementValue);

					if (valueChanged)
					{
						m_valueChangedEventSource(this, null);
					}

					m_preEngagementValue = -1.0;
					RemoveFocusEngagement();
					eventArgs.Handled(true);
				}
			}
		}

		void OnPreviewKeyUp(KeyRoutedEventArgs const& eventArgs)
		{
			var originalKey = eventArgs.as<KeyRoutedEventArgs>().OriginalKey();

			if (IsFocusEngagementEnabled() && originalKey == VirtualKey.GamepadA && m_disengagedWithA)
			{
				// Block the re-engagement
				m_disengagedWithA = false; // We want to do this regardless of handled
				eventArgs.Handled(true);
			}
		}

		void OnFocusEngaged(const Control& /*sender*/, const FocusEngagedEventArgs& /*args*/)
		{
			if (!IsReadOnly())
			{
				EnterGamepadEngagementMode();
			}
		}

		void OnFocusDisengaged(const Control& /*sender*/, const FocusDisengagedEventArgs& /*args*/)
		{
			// Revert value:
			// for catching programmatic disengagements, gamepad ones are handled in OnPreviewKeyDown
			if (m_shouldDiscardValue)
			{
				bool valueChanged = false;

				if (Value() != m_preEngagementValue)
				{
					valueChanged = true;
				}

				Value(m_preEngagementValue);
				m_preEngagementValue = -1.0f;

				if (valueChanged)
				{
					m_valueChangedEventSource(this, null);
				}
			}

			ExitGamepadEngagementMode();
		}

		void EnterGamepadEngagementMode()
		{
			double currentValue = Value();
			m_shouldDiscardValue = true;

			if (currentValue == c_noValueSetSentinel)
			{
				Value(InitialSetValue());
				// Notify that the Value has changed
				m_valueChangedEventSource(this, null);
				currentValue = InitialSetValue();
				m_preEngagementValue = -1;
			}
			else
			{
				currentValue = Value();
				m_preEngagementValue = currentValue;
			}

			if (SharedHelpers.IsRS1OrHigher())
			{
				ElementSoundPlayer.Play(ElementSoundKind.Invoke);
			}
    
			if (SharedHelpers.IsAnimationsEnabled())
			{
				double focalPoint = CalculateStarCenter((int)(currentValue - 1.0));
				m_sharedPointerPropertySet.InsertScalar("starsScaleFocalPoint", (float)(focalPoint));
			}
		}

		void ExitGamepadEngagementMode()
		{
			if (SharedHelpers.IsRS1OrHigher())
			{
				ElementSoundPlayer.Play(ElementSoundKind.GoBack);
			}

			m_sharedPointerPropertySet.InsertScalar("starsScaleFocalPoint", c_noPointerOverMagicNumber);
			m_disengagedWithA = false;
		}

		void RecycleEvents(bool useSafeGet)
		{
			if (var backgroundStackPanel = m_backgroundStackPanel.safe_get(useSafeGet))
			{
				if (m_pointerCancelledToken.value)
				{
					backgroundStackPanel.PointerCanceled(m_pointerCancelledToken);
					m_pointerCancelledToken.value = 0;
				}

				if (m_pointerCaptureLostToken.value)
				{
					backgroundStackPanel.PointerCaptureLost(m_pointerCaptureLostToken);
					m_pointerCaptureLostToken.value = 0;
				}

				if (m_pointerMovedToken.value)
				{
					backgroundStackPanel.PointerMoved(m_pointerMovedToken);
					m_pointerMovedToken.value = 0;
				}

				if (m_pointerEnteredToken.value)
				{
					backgroundStackPanel.PointerEntered(m_pointerEnteredToken);
					m_pointerEnteredToken.value = 0;
				}

				if (m_pointerExitedToken.value)
				{
					backgroundStackPanel.PointerExited(m_pointerExitedToken);
					m_pointerExitedToken.value = 0;
				}

				if (m_pointerPressedToken.value)
				{
					backgroundStackPanel.PointerPressed(m_pointerPressedToken);
					m_pointerPressedToken.value = 0;
				}

				if (m_pointerReleasedToken.value)
				{
					backgroundStackPanel.PointerReleased(m_pointerReleasedToken);
					m_pointerReleasedToken.value = 0;
				}
			}

			if (var captionTextBlock = m_captionTextBlock.safe_get(useSafeGet))
			{
				if (m_captionSizeChangedToken.value)
				{
					captionTextBlock.SizeChanged(m_captionSizeChangedToken);
					m_captionSizeChangedToken.value = 0;
				}
			}
		}

		void OnTextScaleFactorChanged(const UISettings& setting, const IInspectable& args)
		{
			// OnTextScaleFactorChanged happens in non-UI thread, use dispatcher to call StampOutRatingItems in UI thread.
			var strongThis = get_strong();
			m_dispatcherHelper.RunAsync([strongThis]()
			{
				strongThis->StampOutRatingItems();
			});
    
		}

		UISettings GetUISettings()
		{
			static UISettings uiSettings = UISettings();
			return uiSettings;
		}
    }
}
