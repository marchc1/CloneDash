using Nucleus.Common.Graphics;
using Nucleus.Common.Types;
using Nucleus.Core;
using Nucleus.Extensions;
using Nucleus.ManagedMemory;
using Nucleus.Types;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nucleus.UI.Elements;

public class Image : Element
{
	ITexture? image;
	ImageOrientation __ImageOrientation = ImageOrientation.None;

	Vector2F __ImageOffset = new(0);
	Vector2F __ImagePadding = new(0);
	float __ImageRotation = 0;
	bool __ImageFlipX = false;
	bool __ImageFlipY = false;

	SchemeableSetting<Color> __ImageColor = SchemeableSetting<Color>.Default(Color.White);

	public Image(Element? parent) : base(parent) {
		SetPaintBackgroundEnabled(false);
		SetPaintBorderEnabled(false);
		// by default, images are just rendered
		// (usually would be used on buttons etc in the past)
		SetPassthru(true);
	}

	public ImageOrientation ImageOrientation {
		get => __ImageOrientation;
		set {
			__ImageOrientation = value;
			InvalidateLayout();
			GetParent()?.InvalidateLayout();
		}
	}

	public Vector2F ImageOffset {
		get => __ImageOffset;
		set {
			__ImageOffset = value;
			InvalidateLayout();
			GetParent()?.InvalidateLayout();
		}
	}

	public Vector2F ImagePadding {
		get => __ImagePadding;
		set {
			__ImagePadding = value;
			InvalidateLayout();
			GetParent()?.InvalidateLayout();
		}
	}

	public float ImageRotation {
		get => __ImageRotation;
		set {
			__ImageRotation = value;
			InvalidateLayout();
			GetParent()?.InvalidateLayout();
		}
	}

	public ITexture? Texture {
		get => image;
		set {
			if (image == value) return;
			image = value;
			InvalidateLayout();
			GetParent()?.InvalidateLayout();
		}
	}

	public bool ImageFlipX {
		get => __ImageFlipX;
		set {
			if (__ImageFlipX == value) return;
			__ImageFlipX = value;
			InvalidateLayout();
			GetParent()?.InvalidateLayout();
		}
	}

	public bool ImageFlipY {
		get => __ImageFlipY;
		set {
			if (__ImageFlipY == value) return;
			__ImageFlipY = value;
			InvalidateLayout();
			GetParent()?.InvalidateLayout();
		}
	}

	public Color ImageColor {
		get => __ImageColor.Get();
		set => __ImageColor.SetUserValue(value);
	}
	public override void Paint(float width, float height) {
		ImageDrawing(size: new(width, height));
	}
	public void ImageDrawing(Vector2F? pos = null, Vector2F? size = null) {
		if (image == null)
			return;

		var offset = (pos ?? new Vector2F(0));
		var bounds = GetRenderBounds();
		if (size != null) {
			bounds.W = size.Value.X;
			bounds.H = size.Value.Y;
		}

		RectangleF sourceRect = new(0, 0, image.Width, image.Height);
		RectangleF destRect = new(offset.X, offset.Y, image.Width, image.Height);

		var width = bounds.W;
		var height = bounds.H;

		switch (__ImageOrientation) {
			case ImageOrientation.None:
				destRect.X += pos?.X ?? 0;
				destRect.Y += pos?.Y ?? 0;
				destRect.Width = size?.X ?? destRect.Width;
				destRect.Height = size?.Y ?? destRect.Height;
				break;
			case ImageOrientation.Centered:
				var x = (bounds.Width / 2) - (image.Width / 2);
				var y = (bounds.Height / 2) - (image.Height / 2);
				destRect.X += x;
				destRect.Y += y;
				break;
			case ImageOrientation.Stretch:
				destRect.Width = width;
				destRect.Height = height;
				break;
			case ImageOrientation.Zoom:
				if (width <= height) { // Width is the bottleneck
					var ratio = (float)image.Height / image.Width;
					destRect.Width = width;
					destRect.Height = width * ratio;
					destRect.Y += (height / 2) - (width / 2);
				}
				else {
					var ratio = (float)image.Width / image.Height;
					destRect.Height = height;
					destRect.Width = height * ratio;
					destRect.X += (width / 2) - (height / 2);
				}

				break;
			case ImageOrientation.Fit:
				var clampWidth = Math.Clamp(width, 0, image.Width);
				var clampHeight = Math.Clamp(height, 0, image.Height);
				if (clampWidth <= clampHeight) { // Width is the bottleneck
					var ratio = (float)image.Height / image.Width;
					destRect.Width = clampWidth;
					destRect.Height = clampWidth * ratio;
					destRect.Y += (height / 2) - (width / 2);
				}
				else {
					var ratio = (float)image.Width / image.Height;
					destRect.Height = clampHeight;
					destRect.Width = clampHeight * ratio;
					destRect.X += (width / 2) - (height / 2);
				}

				break;
		}

		destRect.X += __ImagePadding.X;
		destRect.Y += __ImagePadding.Y;
		destRect.Width -= __ImagePadding.X * 2;
		destRect.Height -= __ImagePadding.Y * 2;

		Color thisC = __ImageColor.Get();

		if (!IsMouseInputEnabled())
			thisC = thisC.Adjust(0, 0, -.5f);

		Graphics2D.SetTexture(image);
		Graphics2D.SetDrawColor(thisC);

		if (__ImageRotation != 0 || __ImageFlipX || __ImageFlipY) {
			destRect.X += destRect.Width / 2;
			destRect.Y += destRect.Height / 2;

			if (__ImageFlipX) {
				sourceRect.X = sourceRect.Width;
				sourceRect.Width *= -1;
			}
			if (__ImageFlipY) {
				sourceRect.Y = sourceRect.Height;
				sourceRect.Height *= -1;
			}
			Vector2F imageOffset = __ImageOffset * new Vector2F(destRect.Width, destRect.Height);
			Graphics2D.CalculateUVCoordinatesFromRects(image, sourceRect, destRect, out float sU, out float sV, out float eU, out float eV);
			Graphics2D.DrawTexturedRectangle(destRect, __ImageRotation, imageOffset, sU, sV, eU, eV);
		}
		else{
			Vector2F imageOffset = __ImageOffset * new Vector2F(destRect.Width, destRect.Height);
			Graphics2D.CalculateUVCoordinatesFromRects(image, sourceRect, destRect, out float sU, out float sV, out float eU, out float eV);
			Graphics2D.DrawTexturedRectangle(destRect, __ImageRotation, imageOffset, sU, sV, eU, eV);
		}
	}
}
