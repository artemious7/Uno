using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SamplesApp.Samples.WinUI.Common;
using Windows.Foundation;
using Windows.System.Threading;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace SamplesApp.Samples.ColorPicker
{
	enum IncrementDirection
	{
		Lower,
		Higher,
	};

	enum IncrementAmount
	{
		Small,
		Large,
	};

	class ColorHelpers
    {
		const int CheckerSize = 4;

		Hsv IncrementColorChannel(
			Hsv originalHsv,
			ColorPickerHsvChannel channel,
			IncrementDirection direction,
			IncrementAmount amount,
			bool shouldWrap,
			double minBound,
			double maxBound)
		{
			Hsv newHsv = originalHsv;

			if (amount == IncrementAmount.Small || !DownlevelHelper::ToDisplayNameExists())
			{
				// In order to avoid working with small values that can incur rounding issues,
				// we'll multiple saturation and value by 100 to put them in the range of 0-100 instead of 0-1.
				newHsv.s *= 100;
				newHsv.v *= 100;

				double valueToIncrement;
				double incrementAmount = 0;

				// If we're adding a small increment, then we'll just add or subtract 1.
				// If we're adding a large increment, then we want to snap to the next
				// or previous major value - for hue, this is every increment of 30;
				// for saturation and value, this is every increment of 10.
				switch (channel)
				{
				case ColorPickerHsvChannel.Hue:
					valueToIncrement = newHsv.h;
					incrementAmount = amount == IncrementAmount.Small ? 1 : 30;
					break;

				case ColorPickerHsvChannel.Saturation:
					valueToIncrement = newHsv.s;
					incrementAmount = amount == IncrementAmount.Small ? 1 : 10;
					break;

				case ColorPickerHsvChannel.Value:
					valueToIncrement = newHsv.v;
					incrementAmount = amount == IncrementAmount.Small ? 1 : 10;
					break;

				default:
					throw new InvalidOperationException($"Unknown ColorPickerHsvChannel {channel}");
				}

				double previousValue = valueToIncrement;

				valueToIncrement += (direction == IncrementDirection.Lower ? -incrementAmount : incrementAmount);

				// If the value has reached outside the bounds, we were previous at the boundary, and we should wrap,
				// then we'll place the selection on the other side of the spectrum.
				// Otherwise, we'll place it on the boundary that was exceeded.
				if (valueToIncrement < minBound)
				{
					valueToIncrement = (shouldWrap && previousValue == minBound) ? maxBound : minBound;
				}

				if (valueToIncrement > maxBound)
				{
					valueToIncrement = (shouldWrap && previousValue == maxBound) ? minBound : maxBound;
				}

				switch (channel)
				{
					case ColorPickerHsvChannel.Hue:
						newHsv.h = valueToIncrement;
						break;

					case ColorPickerHsvChannel.Saturation:
						newHsv.s = valueToIncrement;
						break;

					case ColorPickerHsvChannel.Value:
						newHsv.v = valueToIncrement;
						break;

					default:
						throw new InvalidOperationException($"Unknown ColorPickerHsvChannel {channel}");
				}

				// We multiplied saturation and value by 100 previously, so now we want to put them back in the 0-1 range.
				newHsv.s /= 100;
				newHsv.v /= 100;
			}
			else
			{
				// While working with named colors, we're going to need to be working in actual HSV units,
				// so we'll divide the min bound and max bound by 100 in the case of saturation or value,
				// since we'll have received units between 0-100 and we need them within 0-1.
				if (channel == ColorPickerHsvChannel.Saturation ||
					channel == ColorPickerHsvChannel.Value)
				{
					minBound /= 100;
					maxBound /= 100;
				}

				newHsv = FindNextNamedColor(originalHsv, channel, direction, shouldWrap, minBound, maxBound);
			}

			return newHsv;
		}

		// Implementation of the signum function, which returns the sign of a number while discarding its value.
		int sgn<T>(T val)
		{
			int first = ((T)(0) < val) ? 1 : 0;
			int second = ((T)(0) > val) ? 1 : 0;

			return first - second;
		}

		private class HsvPointer
		{
			private ColorPickerHsvChannel _channel;
			private Hsv _source;

			public HsvPointer(Hsv source, ColorPickerHsvChannel channel)
			{
				_channel = channel;
				_source = source;
			}

			public double Value
			{
				get
				{
					switch (_channel)
					{
						case ColorPickerHsvChannel.Hue:
							return _source.h;

						case ColorPickerHsvChannel.Saturation:
							return _source.s;

						case ColorPickerHsvChannel.Value:
							return _source.v;

						default:
							throw new InvalidOperationException($"Invalid channel {_channel}");
					}
				}
				set
				{
					switch (_channel)
					{
						case ColorPickerHsvChannel.Hue:
							_source.h = value;

						case ColorPickerHsvChannel.Saturation:
							_source.s = value;

						case ColorPickerHsvChannel.Value:
							_source.v = value;

						default:
							throw new InvalidOperationException($"Invalid channel {_channel}");
					}
				}
			}
		}

		Hsv FindNextNamedColor(
			Hsv originalHsv,
			ColorPickerHsvChannel channel,
			IncrementDirection direction,
			bool shouldWrap,
			double minBound,
			double maxBound)
		{
			// There's no easy way to directly get the next named color, so what we'll do
			// is just iterate in the direction that we want to find it until we find a color
			// in that direction that has a color name different than our current color name.
			// Once we find a new color name, then we'll iterate across that color name until
			// we find its bounds on the other side, and then select the color that is exactly
			// in the middle of that color's bounds.
			Hsv newHsv = originalHsv;

			string originalColorName = ColorHelper.ToDisplayName(ColorFromRgba(HsvToRgb(originalHsv)));
			string newColorName = originalColorName;

			double originalValue = 0;
			var newValue = new HsvPointer(originalHsv, channel);
			double incrementAmount = 0;

			switch (channel)
			{
			case ColorPickerHsvChannel.Hue:
				originalValue = originalHsv.h;
				incrementAmount = 1;
				break;

			case ColorPickerHsvChannel.Saturation:
				originalValue = originalHsv.s;
				incrementAmount = 0.01;
				break;

			case ColorPickerHsvChannel.Value:
				originalValue = originalHsv.v;
				incrementAmount = 0.01;
				break;

			default:
				throw new InvalidOperationException($"Invalid channel {channel}");
			}

			bool shouldFindMidPoint = true;

			while (newColorName == originalColorName)
			{
				double previousValue = newValue.Value;
				newValue.Value += (direction == IncrementDirection.Lower ? -1 : 1) * incrementAmount;

				bool justWrapped = false;

				// If we've hit a boundary, then either we should wrap or we shouldn't.
				// If we should, then we'll perform that wrapping if we were previously up against
				// the boundary that we've now hit.  Otherwise, we'll stop at that boundary.
				if (newValue.Value > maxBound)
				{
					if (shouldWrap)
					{
						newValue.Value = minBound;
						justWrapped = true;
					}
					else
					{
						newValue.Value = maxBound;
						shouldFindMidPoint = false;
						newColorName = ColorHelper.ToDisplayName(ColorFromRgba(HsvToRgb(newHsv)));
						break;
					}
				}
				else if (newValue.Value < minBound)
				{
					if (shouldWrap)
					{
						newValue.Value = maxBound;
						justWrapped = true;
					}
					else
					{
						newValue.Value = minBound;
						shouldFindMidPoint = false;
						newColorName = ColorHelper.ToDisplayName(ColorFromRgba(HsvToRgb(newHsv)));
						break;
					}
				}

				if (!justWrapped &&
					previousValue != originalValue &&
					sgn(newValue.Value - originalValue) != sgn(previousValue - originalValue))
				{
					// If we've wrapped all the way back to the start and have failed to find a new color name,
					// then we'll just quit - there isn't a new color name that we're going to find.
					shouldFindMidPoint = false;
					break;
				}

				newColorName = ColorHelper.ToDisplayName(ColorFromRgba(HsvToRgb(newHsv)));
			}

			if (shouldFindMidPoint)
			{
				Hsv startHsv = newHsv;
				Hsv currentHsv = startHsv;
				double startEndOffset = 0;
				string currentColorName = newColorName;

				var startValue = new HsvPointer(startHsv, channel);
				var currentValue = new HsvPointer(currentHsv, channel);
				double wrapIncrement = 0;

				switch (channel)
				{
				case ColorPickerHsvChannel.Hue:
					wrapIncrement = 360.0;
					break;

				case ColorPickerHsvChannel.Saturation:
					wrapIncrement = 1.0;
					break;

				case ColorPickerHsvChannel.Value:
					wrapIncrement = 1.0;
					break;

				default:
					throw new InvalidOperationException($"Invalid channel {channel}");
				}

				while (newColorName == currentColorName)
				{
					currentValue.Value += (direction == IncrementDirection.Lower ? -1 : 1) * incrementAmount;

					// If we've hit a boundary, then either we should wrap or we shouldn't.
					// If we should, then we'll perform that wrapping if we were previously up against
					// the boundary that we've now hit.  Otherwise, we'll stop at that boundary.
					if (currentValue.Value > maxBound)
					{
						if (shouldWrap)
						{
							currentValue.Value = minBound;
							startEndOffset = maxBound - minBound;
						}
						else
						{
							currentValue.Value = maxBound;
							break;
						}
					}
					else if (currentValue.Value < minBound)
					{
						if (shouldWrap)
						{
							currentValue.Value = maxBound;
							startEndOffset = minBound - maxBound;
						}
						else
						{
							currentValue.Value = minBound;
							break;
						}
					}

					currentColorName = ColorHelper.ToDisplayName(ColorFromRgba(HsvToRgb(currentHsv)));
				}

				newValue.Value = (startValue.Value + currentValue.Value + startEndOffset) / 2;

				// Dividing by 2 may have gotten us halfway through a single step, so we'll
				// remove that half-step if it exists.
				double leftoverValue = Math.Abs(newValue.Value);

				while (leftoverValue > incrementAmount)
				{
					leftoverValue -= incrementAmount;
				}

				newValue.Value -= leftoverValue;

				while (newValue.Value < minBound)
				{
					newValue.Value += wrapIncrement;
				}

				while (newValue.Value > maxBound)
				{
					newValue.Value -= wrapIncrement;
				}
			}

			return newHsv;
		}

		double IncrementAlphaChannel(
			double originalAlpha,
			IncrementDirection direction,
			IncrementAmount amount,
			bool shouldWrap,
			double minBound,
			double maxBound)
		{
			// In order to avoid working with small values that can incur rounding issues,
			// we'll multiple alpha by 100 to put it in the range of 0-100 instead of 0-1.
			originalAlpha *= 100;

			double smallIncrementAmount = 1;
			double largeIncrementAmount = 10;

			if (amount == IncrementAmount.Small)
			{
				originalAlpha += (direction == IncrementDirection.Lower ? -1 : 1) * smallIncrementAmount;
			}
			else
			{
				if (direction == IncrementDirection.Lower)
				{
					originalAlpha = Math.Ceiling((originalAlpha - largeIncrementAmount) / largeIncrementAmount) * largeIncrementAmount;
				}
				else
				{
					originalAlpha = Math.Floor((originalAlpha + largeIncrementAmount) / largeIncrementAmount) * largeIncrementAmount;
				}
			}

			// If the value has reached outside the bounds and we should wrap, then we'll place the selection
			// on the other side of the spectrum.  Otherwise, we'll place it on the boundary that was exceeded.
			if (originalAlpha < minBound)
			{
				originalAlpha = shouldWrap ? maxBound : minBound;
			}

			if (originalAlpha > maxBound)
			{
				originalAlpha = shouldWrap ? minBound : maxBound;
			}

			// We multiplied alpha by 100 previously, so now we want to put it back in the 0-1 range.
			return originalAlpha / 100;
		}

		void CreateCheckeredBackgroundAsync(
			int width,
			int height,
			Color checkerColor,
			List<byte> bgraCheckeredPixelData,
			IAsyncAction asyncActionToAssign,
			DispatcherHelper dispatcherHelper,
			Func<WriteableBitmap> completedFunction)
		{
			if (width == 0 || height == 0)
			{
				return;
			}

			bgraCheckeredPixelData.Capacity = width * height * 4;

			WorkItemHandler workItemHandler = 
			(IAsyncAction workItem) =>
			{
				for (int y = 0; y < height; y++)
				{
					for (int x = 0; x < width; x++)
					{
						if (workItem.Status == AsyncStatus.Canceled)
						{
							break;
						}

						// We want the checkered pattern to alternate both vertically and horizontally.
						// In order to achieve that, we'll toggle visibility of the current pixel on or off
						// depending on both its x- and its y-position.  If x == CheckerSize, we'll turn visibility off,
						// but then if y == CheckerSize, we'll turn it back on.
						// The below is a shorthand for the above intent.
						bool pixelShouldBeBlank = (x / CheckerSize + y / CheckerSize) % 2 == 0 ? true : false;

						if (pixelShouldBeBlank)
						{
							bgraCheckeredPixelData.Add(0);
							bgraCheckeredPixelData.Add(0);
							bgraCheckeredPixelData.Add(0);
							bgraCheckeredPixelData.Add(0);
						}
						else
						{
							bgraCheckeredPixelData.Add((byte)(checkerColor.B * checkerColor.A / 255));
							bgraCheckeredPixelData.Add((byte)(checkerColor.G * checkerColor.A / 255));
							bgraCheckeredPixelData.Add((byte)(checkerColor.R * checkerColor.A / 255));
							bgraCheckeredPixelData.Add(checkerColor.A);
						}
					}
				}
			};

			if (asyncActionToAssign != null)
			{
				asyncActionToAssign.Cancel();
			}

			asyncActionToAssign = ThreadPool.RunAsync(workItemHandler);
			asyncActionToAssign.Completed = 
			(IAsyncAction asyncInfo, AsyncStatus asyncStatus) =>
			{
				if (asyncStatus != AsyncStatus.Completed)
				{
					return;
				}

				asyncActionToAssign = null;

				dispatcherHelper.RunAsync(() =>
				{
					var checkeredBackgroundBitmap = CreateBitmapFromPixelData(width, height, bgraCheckeredPixelData);
					completedFunction(checkeredBackgroundBitmap);
				});
			}));
		}

		WriteableBitmap CreateBitmapFromPixelData(
			int pixelWidth,
			int pixelHeight,
			List<byte> bgraPixelData)
		{
			// IBufferByteAccess isn't included in any WinMD file, because its sole method - Buffer() -
			// allows direct pointer access, which isn't applicable to C#.  In C#, there's a separate ToStream()
			// method that similarly allows read access.  As a result, we have no C++/WinMD projection for IBufferByteAccess;
			// we need to flip to ABI in this circumstance to do what this method requires.
			var bitmap = new WriteableBitmap(pixelWidth, pixelHeight);

			byte *pixelBuffer = nullptr;
			check_hresult(bitmap.PixelBuffer().as<Windows::Storage::Streams::IBufferByteAccess>()->Buffer(&pixelBuffer));

			std::memcpy(pixelBuffer, (*bgraPixelData).data(), (*bgraPixelData).size());
			bitmap.Invalidate();

			return bitmap;
		}

		LoadedImageSurface CreateSurfaceFromPixelData(
			int pixelWidth,
			int pixelHeight,
			std::shared_ptr<std::vector<byte>> const& bgraPixelData)
		{
			MUX_ASSERT(SharedHelpers::IsRS2OrHigher());

			// LoadedImageSurface uses WIC to load images, so we need to put the pixel data into an image format.
			// We'll use the BMP format, since it stores uncompressed pixel data.
			std::vector<byte> bmpData;

			// Size is header (14 bytes) + DIB header (40 bytes) + Pixel array (size of bgraPixelData).
			size_t dibHeaderSize = 40;
			size_t headerSize = 14 + dibHeaderSize;
			size_t fileSize = headerSize + (*bgraPixelData).size();

			// Header field to identify as BMP.
			bmpData.push_back('B');
			bmpData.push_back('M');

			// File size.  Note that the BMP format is always little-endian.
			bmpData.push_back(static_cast<byte>(fileSize & 0x000000FF));
			bmpData.push_back(static_cast<byte>((fileSize & 0x0000FF00) >> 8));
			bmpData.push_back(static_cast<byte>((fileSize & 0x00FF0000) >> 16));
			bmpData.push_back(static_cast<byte>((fileSize & 0xFF000000) >> 24));

			// Reserved for application-specific usage.  We don't care about these bytes.
			bmpData.push_back(0);
			bmpData.push_back(0);
			bmpData.push_back(0);
			bmpData.push_back(0);

			// Offset of the pixel data from the start.  Since our header is 14 + 12 bytes long, it starts after that.
			uint32_t pixelDataOffset = static_cast<uint32_t>(headerSize);
			bmpData.push_back(static_cast<byte>(pixelDataOffset & 0x000000FF));
			bmpData.push_back(static_cast<byte>((pixelDataOffset & 0x0000FF00) >> 8));
			bmpData.push_back(static_cast<byte>((pixelDataOffset & 0x00FF0000) >> 16));
			bmpData.push_back(static_cast<byte>((pixelDataOffset & 0xFF000000) >> 24));

			// Beginning of DIB header.  First 4 bytes are the size of the header (12 bytes in our case).
			bmpData.push_back(static_cast<byte>(dibHeaderSize & 0x000000FF));
			bmpData.push_back(static_cast<byte>((dibHeaderSize & 0x0000FF00) >> 8));
			bmpData.push_back(static_cast<byte>((dibHeaderSize & 0x00FF0000) >> 16));
			bmpData.push_back(static_cast<byte>((dibHeaderSize & 0xFF000000) >> 24));

			// Bitmap width in pixels (32-bit).
			uint32_t bitmapWidth = static_cast<uint32_t>(pixelWidth);
			bmpData.push_back(static_cast<byte>(bitmapWidth & 0x000000FF));
			bmpData.push_back(static_cast<byte>((bitmapWidth & 0x0000FF00) >> 8));
			bmpData.push_back(static_cast<byte>((bitmapWidth & 0x00FF0000) >> 16));
			bmpData.push_back(static_cast<byte>((bitmapWidth & 0xFF000000) >> 24));

			// Bitmap height in pixels (32-bit).
			uint32_t bitmapHeight = static_cast<uint32_t>(pixelHeight);
			bmpData.push_back(static_cast<byte>(bitmapHeight & 0x000000FF));
			bmpData.push_back(static_cast<byte>((bitmapHeight & 0x0000FF00) >> 8));
			bmpData.push_back(static_cast<byte>((bitmapHeight & 0x00FF0000) >> 16));
			bmpData.push_back(static_cast<byte>((bitmapHeight & 0xFF000000) >> 24));

			// Color plane count.
			uint16_t colorPlaneCount = 1;
			bmpData.push_back(static_cast<byte>(colorPlaneCount & 0x00FF));
			bmpData.push_back(static_cast<byte>((colorPlaneCount & 0xFF00) >> 8));

			// Bits per pixel.
			uint16_t bitsPerPixel = 32;
			bmpData.push_back(static_cast<byte>(bitsPerPixel & 0x00FF));
			bmpData.push_back(static_cast<byte>((bitsPerPixel & 0xFF00) >> 8));

			// Compression method.  0 means uncompressed.
			uint32_t compressionMethod = 0;
			bmpData.push_back(static_cast<byte>(compressionMethod & 0x000000FF));
			bmpData.push_back(static_cast<byte>((compressionMethod & 0x0000FF00) >> 8));
			bmpData.push_back(static_cast<byte>((compressionMethod & 0x00FF0000) >> 16));
			bmpData.push_back(static_cast<byte>((compressionMethod & 0xFF000000) >> 24));

			// Image size.  Not needed for uncompressed images.
			uint32_t imageSize = 0;
			bmpData.push_back(static_cast<byte>(imageSize & 0x000000FF));
			bmpData.push_back(static_cast<byte>((imageSize & 0x0000FF00) >> 8));
			bmpData.push_back(static_cast<byte>((imageSize & 0x00FF0000) >> 16));
			bmpData.push_back(static_cast<byte>((imageSize & 0xFF000000) >> 24));

			// Horizontal resolution.  We don't care about this value.
			uint32_t horizontalResolution = 1;
			bmpData.push_back(static_cast<byte>(horizontalResolution & 0x000000FF));
			bmpData.push_back(static_cast<byte>((horizontalResolution & 0x0000FF00) >> 8));
			bmpData.push_back(static_cast<byte>((horizontalResolution & 0x00FF0000) >> 16));
			bmpData.push_back(static_cast<byte>((horizontalResolution & 0xFF000000) >> 24));

			// Vertical resolution.  We don't care about this value.
			uint32_t verticalResolution = 1;
			bmpData.push_back(static_cast<byte>(verticalResolution & 0x000000FF));
			bmpData.push_back(static_cast<byte>((verticalResolution & 0x0000FF00) >> 8));
			bmpData.push_back(static_cast<byte>((verticalResolution & 0x00FF0000) >> 16));
			bmpData.push_back(static_cast<byte>((verticalResolution & 0xFF000000) >> 24));

			// Number of colors in the palette.  0 means use the default.
			uint32_t colorsInPalette = 0;
			bmpData.push_back(static_cast<byte>(colorsInPalette & 0x000000FF));
			bmpData.push_back(static_cast<byte>((colorsInPalette & 0x0000FF00) >> 8));
			bmpData.push_back(static_cast<byte>((colorsInPalette & 0x00FF0000) >> 16));
			bmpData.push_back(static_cast<byte>((colorsInPalette & 0xFF000000) >> 24));

			// Important colors.  0 means all colors are important.
			uint32_t importantColors = 0;
			bmpData.push_back(static_cast<byte>(importantColors & 0x000000FF));
			bmpData.push_back(static_cast<byte>((importantColors & 0x0000FF00) >> 8));
			bmpData.push_back(static_cast<byte>((importantColors & 0x00FF0000) >> 16));
			bmpData.push_back(static_cast<byte>((importantColors & 0xFF000000) >> 24));

			// Pixel data.  BMP images are stored upside-down, so we need to copy the image data backwards.
			bmpData.resize(fileSize);
			uint32_t stride = pixelWidth * 4;

			for (int y = pixelHeight - 1; y >= 0; y--)
			{
				memcpy(bmpData.data() + (headerSize + (pixelHeight - 1 - y) * stride), (*bgraPixelData).data() + (y * stride), stride);
			}

			InMemoryRandomAccessStream stream = SharedHelpers::CreateStreamFromBytes(array_view<const byte>(bmpData));

			return LoadedImageSurface::StartLoadFromStream(stream);
		}

		void CancelAsyncAction(IAsyncAction const& action)
		{
			if (action && action.Status() == AsyncStatus::Started)
			{
				action.Cancel();
			}
		}
    }
}
