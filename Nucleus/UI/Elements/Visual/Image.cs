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
	ImageOrientation ImageOrientation = ImageOrientation.None;

	Vector2F ImageOffset = new(0);
	Vector2F ImagePadding = new(0);
	float ImageRotation = 0;
	bool ImageFlipX = false;
	bool ImageFlipY = false;

	SchemeableSetting<Color> ImageColor = SchemeableSetting<Color>.Default(Color.White);

	public Image(Element? parent) : base(parent) {
		SetPaintBackgroundEnabled(false);
		SetPaintBorderEnabled(false);
	}

	public ImageOrientation GetImageOrientation() => ImageOrientation;
	public void SetImageOrientation(ImageOrientation orientation) {
		ImageOrientation = orientation;
		InvalidateLayout();
		GetParent()?.InvalidateLayout();
	}

	public Vector2F GetImageOffset() => ImageOffset;
	public void SetImageOffset(Vector2F offset) {
		ImageOffset = offset;
		InvalidateLayout();
		GetParent()?.InvalidateLayout();
	}

	public Vector2F GetImagePadding() => ImagePadding;
	public void SetImagePadding(Vector2F padding) {
		ImagePadding = padding;
		InvalidateLayout();
		GetParent()?.InvalidateLayout();
	}

	public float GetImageRotation() => ImageRotation;
	public void SetImageRotation(float rotation) {
		ImageRotation = rotation;
		InvalidateLayout();
		GetParent()?.InvalidateLayout();
	}

	public ITexture? GetTexture() => image;
	public void SetTexture(ITexture? tex) {
		if (image == tex) return;
		image = tex;
		InvalidateLayout();
		GetParent()?.InvalidateLayout();
	}

	public bool GetImageFlipX() => ImageFlipX;
	public void SetImageFlipX(bool state) {
		if (ImageFlipX == state) return;
		ImageFlipX = state;
		InvalidateLayout();
		GetParent()?.InvalidateLayout();
	}

	public bool GetImageFlipY() => ImageFlipY;
	public void SetImageFlipY(bool state) {
		if (ImageFlipY == state) return;
		ImageFlipY = state;
		InvalidateLayout();
		GetParent()?.InvalidateLayout();
	}

	public Color GetImageColor() => ImageColor.Get();
	public void SetImageColor(Color color) {
		ImageColor.SetUserValue(color);
	}

	public override void Paint(float width, float height) {
		ImageDrawing(size: new(width, height));
	}
	public void ImageDrawing(Vector2F? pos = null, Vector2F? size = null) {
		if (image == null)
			return;

		var offset = Graphics2D.Offset + (pos ?? new Vector2F(0));
		var bounds = RenderBounds;
		if (size != null) {
			bounds.W = size.Value.X;
			bounds.H = size.Value.Y;
		}

		Rectangle sourceRect = new(0, 0, image.Width, image.Height);
		Rectangle destRect = new(offset.X, offset.Y, image.Width, image.Height);
		var scldiv2 = RenderBounds.Size / 2;

		var width = RenderBounds.Size.W;
		var height = RenderBounds.Size.H;

		switch (ImageOrientation) {
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

		destRect.X += ImagePadding.X + ImageOffset.X;
		destRect.Y += ImagePadding.Y + ImageOffset.Y;
		destRect.Width -= ImagePadding.X * 2;
		destRect.Height -= ImagePadding.Y * 2;

		Color thisC = ImageColor.Get();

		if (!IsMouseInputEnabled())
			thisC = thisC.Adjust(0, 0, -.5f);

		if (image.HasPublicFlags(PublicTextureFlags.RequiresFlippedV))
			sourceRect.Height *= -1;

		if (ImageRotation != 0 || ImageFlipX || ImageFlipY) {
			destRect.X += destRect.Width / 2;
			destRect.Y += destRect.Height / 2;

			if (ImageFlipX) {
				sourceRect.X = sourceRect.Width;
				sourceRect.Width *= -1;
			}
			if (ImageFlipY) {
				sourceRect.Y = sourceRect.Height;
				sourceRect.Height *= -1;
			}

			Raylib.DrawTexturePro((Texture)image, sourceRect, destRect, new(destRect.Width / 2, destRect.Height / 2), ImageRotation, thisC);
		}
		else
			Raylib.DrawTexturePro((Texture)image, sourceRect, destRect, new(0, 0), ImageRotation, thisC);
	}
}
