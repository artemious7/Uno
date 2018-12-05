using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Controls
{
	public partial class RatingControl
	{
		public double Value
		{
			get
			{
				return (double)this.GetValue(ValueProperty);
			}
			set
			{
				this.SetValue(ValueProperty, value);
			}
		}

		public double PlaceholderValue
		{
			get
			{
				return (double)this.GetValue(PlaceholderValueProperty);
			}
			set
			{
				this.SetValue(PlaceholderValueProperty, value);
			}
		}

		public int MaxRating
			{
				get
				{
					return (int)this.GetValue(MaxRatingProperty);
				}
				set
				{
					this.SetValue(MaxRatingProperty, value);
				}
			}

		public RatingItemInfo ItemInfo
			{
				get
				{
					return (RatingItemInfo)this.GetValue(ItemInfoProperty);
				}
				set
				{
					this.SetValue(ItemInfoProperty, value);
				}
			}

		public bool IsReadOnly
			{
				get
				{
					return (bool)this.GetValue(IsReadOnlyProperty);
				}
				set
				{
					this.SetValue(IsReadOnlyProperty, value);
				}
			}

		public bool IsClearEnabled
			{
				get
				{
					return (bool)this.GetValue(IsClearEnabledProperty);
				}
				set
				{
					this.SetValue(IsClearEnabledProperty, value);
				}
			}

		public int InitialSetValue
			{
				get
				{
					return (int)this.GetValue(InitialSetValueProperty);
				}
				set
				{
					this.SetValue(InitialSetValueProperty, value);
				}
			}

		public string Caption
			{
				get
				{
					return (string)this.GetValue(CaptionProperty);
				}
				set
				{
					this.SetValue(CaptionProperty, value);
				}
			}

		public static DependencyProperty CaptionProperty { get; } =
			Windows.UI.Xaml.DependencyProperty.Register(
				"Caption", typeof(string),
				typeof(RatingControl),
				new FrameworkPropertyMetadata(null, OnStaticPropertyChanged));

		public static DependencyProperty InitialSetValueProperty { get; } =
			Windows.UI.Xaml.DependencyProperty.Register(
				"InitialSetValue", typeof(int),
				typeof(RatingControl),
				new FrameworkPropertyMetadata(1, OnStaticPropertyChanged));

		public static DependencyProperty IsClearEnabledProperty { get; } =
			Windows.UI.Xaml.DependencyProperty.Register(
				"IsClearEnabled", typeof(bool),
				typeof(RatingControl),
				new FrameworkPropertyMetadata(true, OnStaticPropertyChanged));

		public static DependencyProperty IsReadOnlyProperty { get; } =
			Windows.UI.Xaml.DependencyProperty.Register(
				"IsReadOnly", typeof(bool),
				typeof(RatingControl),
				new FrameworkPropertyMetadata(false, OnStaticPropertyChanged));

		public static DependencyProperty ItemInfoProperty { get; } =
			Windows.UI.Xaml.DependencyProperty.Register(
				"ItemInfo", typeof(RatingItemInfo),
				typeof(RatingControl),
				new FrameworkPropertyMetadata(null, OnStaticPropertyChanged));

		public static DependencyProperty MaxRatingProperty { get; } =
			Windows.UI.Xaml.DependencyProperty.Register(
				"MaxRating", typeof(int),
				typeof(RatingControl),
				new FrameworkPropertyMetadata(5, OnStaticPropertyChanged));

		public static DependencyProperty PlaceholderValueProperty { get; } =
			Windows.UI.Xaml.DependencyProperty.Register(
				"PlaceholderValue", typeof(double),
				typeof(RatingControl),
				new FrameworkPropertyMetadata(-1.0, OnStaticPropertyChanged));

		public static DependencyProperty ValueProperty { get; } =
			Windows.UI.Xaml.DependencyProperty.Register(
				"Value", typeof(double),
				typeof(RatingControl),
				new FrameworkPropertyMetadata(-1.0, OnStaticPropertyChanged));
	}
}
