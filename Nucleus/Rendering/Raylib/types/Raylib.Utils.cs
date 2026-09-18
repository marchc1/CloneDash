using Nucleus.Common.Graphics;
using Nucleus.Common.Images;
using Nucleus.Common.Types;
using System;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Raylib_cs;

public static unsafe partial class Raylib
{
	/// <summary>Initialize window and OpenGL context</summary>
	//public static void InitWindow(int width, int height, string title)
	//{
	//    using var str1 = title.ToUtf8Buffer();
	//    InitWindow(width, height, str1.AsPointer());
	//}

	/// <summary>Set title for window (only PLATFORM_DESKTOP)</summary>
	//public static void SetWindowTitle(string title)
	//{
	//    using var str1 = title.ToUtf8Buffer();
	//    SetWindowTitle(str1.AsPointer());
	//}

	/// <summary>Get the human-readable, UTF-8 encoded name of the specified monitor</summary>
	//public static string GetMonitorName_(int monitor)
	//{
	//    return Utf8StringUtils.GetUTF8String(GetMonitorName(monitor));
	//}

	/// <summary>Get clipboard text content</summary>
	//public static string GetClipboardText_()
	//{
	//    return Utf8StringUtils.GetUTF8String(GetClipboardText());
	//}

	/// <summary>Set clipboard text content</summary>
	//public static void SetClipboardText(string text)
	//{
	//    using var str1 = text.ToUtf8Buffer();
	//    SetClipboardText(str1.AsPointer());
	//}

	/// <summary>Open URL with default system browser (if available)</summary>
	public static void OpenURL(string url) {
		using var str1 = url.ToUtf8Buffer();
		OpenURL(str1.AsPointer());
	}

	/// <summary>Set internal gamepad mappings (SDL_GameControllerDB)</summary>
	//public static int SetGamepadMappings(string mappings)
	//{
	//    using var str1 = mappings.ToUtf8Buffer();
	//    return SetGamepadMappings(str1.AsPointer());
	//}

	/// <summary>Load shader from files and bind default locations</summary>
	public static Shader LoadShader(string vsFileName, string fsFileName) {
		using var str1 = vsFileName.ToAnsiBuffer();
		using var str2 = fsFileName.ToAnsiBuffer();
		return LoadShader(str1.AsPointer(), str2.AsPointer());
	}

	/// <summary>Load shader from code string and bind default locations</summary>
	public static Shader LoadShaderFromMemory(string vsCode, string fsCode) {
		using var str1 = vsCode.ToUtf8Buffer();
		using var str2 = fsCode.ToUtf8Buffer();
		return LoadShaderFromMemory(str1.AsPointer(), str2.AsPointer());
	}

	/// <summary>Get shader uniform location</summary>
	public static int GetShaderLocation(Shader shader, ReadOnlySpan<char> uniformName) {
		using var str1 = uniformName.ToUtf8Buffer();
		return GetShaderLocation(shader, str1.AsPointer());
	}

	/// <summary>Get shader attribute location</summary>
	public static int GetShaderLocationAttrib(Shader shader, ReadOnlySpan<char> attribName) {
		using var str1 = attribName.ToUtf8Buffer();
		return GetShaderLocationAttrib(shader, str1.AsPointer());
	}

	/// <summary>Takes a screenshot of current screen (saved a .png)</summary>
	public static void TakeScreenshot(string fileName) {
		using var str1 = fileName.ToAnsiBuffer();
		TakeScreenshot(str1.AsPointer());
	}


	/// <summary>
	/// A helper class for image manipulation and disposal
	/// </summary>
	public class ImageRef : IDisposable
	{
		private Image img;
		private bool disposedValue;

		public static implicit operator Image(ImageRef self) => self.img;

		public int Width => img.Width;
		public int Height => img.Height;

		public ImageRef(Image img) {
			this.img = img;
		}
		
		public ImageRef(byte* source, int length) {
			img = Image.LoadImageFromMemory(new(source, length));
		}
		public ImageRef(Stream stream) {
			img = Image.LoadImageFromStream(stream);
		}
		public ImageRef(byte[] source) {
			img = Image.LoadImageFromMemory(source);
		}

		protected virtual void Dispose(bool disposing) {
			if (!disposedValue) {
				if (disposing) {
					// TODO: dispose managed state (managed objects)
				}

				Image.UnloadImage(img);
				disposedValue = true;
			}
		}

		// // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
		// ~TemporaryImage()
		// {
		//     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		//     Dispose(disposing: false);
		// }

		public void Dispose() {
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}

	/// <summary>Export image data to file</summary>
	public static CBool ExportImage(Image image, string fileName) {
		using var str1 = fileName.ToAnsiBuffer();
		return ExportImage(image, str1.AsPointer());
	}

	/// <summary>Export image as code file defining an array of bytes</summary>
	public static CBool ExportImageAsCode(Image image, string fileName) {
		using var str1 = fileName.ToAnsiBuffer();
		return ExportImageAsCode(image, str1.AsPointer());
	}

	/// <summary>Show trace log messages (LOG_DEBUG, LOG_INFO, LOG_WARNING, LOG_ERROR)</summary>
	public static void TraceLog(TraceLogLevel logLevel, string text) {
		using var str1 = text.ToUtf8Buffer();
		TraceLog(logLevel, str1.AsPointer());
	}

	/// <summary>Set shader uniform value vector</summary>
	public static void SetShaderValueV<T>(
		Shader shader,
		int locIndex,
		T[] values,
		ShaderUniformDataType uniformType,
		int count
	) where T : unmanaged {
		SetShaderValueV(shader, locIndex, (Span<T>)values, uniformType, count);
	}

	/// <summary>Set shader uniform value vector</summary>
	public static void SetShaderValueV<T>(
		Shader shader,
		int locIndex,
		Span<T> values,
		ShaderUniformDataType uniformType,
		int count
	) where T : unmanaged {
		fixed (T* valuePtr = values) {
			SetShaderValueV(shader, locIndex, valuePtr, uniformType, count);
		}
	}

	/// <summary>Set shader uniform value</summary>
	public static void SetShaderValue<T>(Shader shader, int locIndex, T value, ShaderUniformDataType uniformType)
	where T : unmanaged {
		SetShaderValue(shader, locIndex, &value, uniformType);
	}

	/// <summary>Set shader uniform value</summary>
	public static void SetShaderValue<T>(
		Shader shader,
		int locIndex,
		T[] values,
		ShaderUniformDataType uniformType
	) where T : unmanaged {
		SetShaderValue(shader, locIndex, (Span<T>)values, uniformType);
	}

	/// <summary>Set shader uniform value</summary>
	public static void SetShaderValue<T>(
		Shader shader,
		int locIndex,
		Span<T> values,
		ShaderUniformDataType uniformType
	) where T : unmanaged {
		fixed (T* valuePtr = values) {
			SetShaderValue(shader, locIndex, valuePtr, uniformType);
		}
	}

	/// <summary>C++ style memory allocator</summary>
	public static T* New<T>(int count) where T : unmanaged {
		return (T*)MemAlloc(count * sizeof(T));
	}

	/// <summary>Get gamepad internal name id</summary>
	//public static string GetGamepadName_(int gamepad)
	//{
	//    return Utf8StringUtils.GetUTF8String(GetGamepadName(gamepad));
	//}

	/// <summary>Update camera position for selected mode</summary>
	public static void UpdateCamera(ref Camera3D camera, CameraMode mode) {
		fixed (Camera3D* c = &camera) {
			UpdateCamera(c, mode);
		}
	}

	/// <summary>Update camera movement/rotation</summary>
	public static void UpdateCameraPro(ref Camera3D camera, Vector3 movement, Vector3 rotation, float zoom) {
		fixed (Camera3D* c = &camera) {
			UpdateCameraPro(c, movement, rotation, zoom);
		}
	}

	/// <summary>Returns the cameras forward vector (normalized)</summary>
	public static Vector3 GetCameraForward(ref Camera3D camera) {
		fixed (Camera3D* c = &camera) {
			return GetCameraForward(c);
		}
	}

	/// <summary>
	/// Returns the cameras up vector (normalized)<br/>
	/// NOTE: The up vector might not be perpendicular to the forward vector
	/// </summary>
	public static Vector3 GetCameraUp(ref Camera3D camera) {
		fixed (Camera3D* c = &camera) {
			return GetCameraUp(c);
		}
	}

	/// <summary>Returns the cameras right vector (normalized)</summary>
	public static Vector3 GetCameraRight(ref Camera3D camera) {
		fixed (Camera3D* c = &camera) {
			return GetCameraRight(c);
		}
	}

	/// <summary>Moves the camera in its forward direction</summary>
	public static void CameraMoveForward(ref Camera3D camera, float distance, CBool moveInWorldPlane) {
		fixed (Camera3D* c = &camera) {
			CameraMoveForward(c, distance, moveInWorldPlane);
		}
	}

	/// <summary>Moves the camera in its up direction</summary>
	public static void CameraMoveUp(ref Camera3D camera, float distance) {
		fixed (Camera3D* c = &camera) {
			CameraMoveUp(c, distance);
		}
	}

	/// <summary>Moves the camera target in its current right direction</summary>
	public static void CameraMoveRight(ref Camera3D camera, float distance, CBool moveInWorldPlane) {
		fixed (Camera3D* c = &camera) {
			CameraMoveRight(c, distance, moveInWorldPlane);
		}
	}

	/// <summary>Moves the camera position closer/farther to/from the camera target</summary>
	public static void CameraMoveToTarget(ref Camera3D camera, float delta) {
		fixed (Camera3D* c = &camera) {
			CameraMoveToTarget(c, delta);
		}
	}

	/// <summary>
	/// Rotates the camera around its up vector<br/>
	/// If rotateAroundTarget is false, the camera rotates around its position
	/// </summary>
	public static void CameraYaw(ref Camera3D camera, float angle, CBool rotateAroundTarget) {
		fixed (Camera3D* c = &camera) {
			CameraYaw(c, angle, rotateAroundTarget);
		}
	}

	/// <summary>
	/// Rotates the camera around its right vector
	/// </summary>
	public static void CameraPitch(ref Camera3D camera,
		float angle,
		CBool lockView,
		CBool rotateAroundTarget,
		CBool rotateUp
		) {
		fixed (Camera3D* c = &camera) {
			CameraPitch(c, angle, lockView, rotateAroundTarget, rotateUp);
		}
	}

	/// <summary>Rotates the camera around its forward vector</summary>
	public static void CameraRoll(ref Camera3D camera, float angle) {
		fixed (Camera3D* c = &camera) {
			CameraRoll(c, angle);
		}
	}

	/// <summary>Returns the camera view matrix</summary>
	public static Matrix4x4 GetCameraViewMatrix(ref Camera3D camera) {
		fixed (Camera3D* c = &camera) {
			return GetCameraViewMatrix(c);
		}
	}

	/// <summary>Returns the camera projection matrix</summary>
	public static Matrix4x4 GetCameraProjectionMatrix(ref Camera3D camera, float aspect) {
		fixed (Camera3D* c = &camera) {
			return GetCameraProjectionMatrix(c, aspect);
		}
	}

	/// <summary>
	/// Check the collision between two lines defined by two points each, returns collision point by reference
	/// </summary>
	public static CBool CheckCollisionLines(
		Vector2 startPos1,
		Vector2 endPos1,
		Vector2 startPos2,
		Vector2 endPos2,
		ref Vector2 collisionPoint
	) {
		fixed (Vector2* p = &collisionPoint) {
			return CheckCollisionLines(startPos1, endPos1, startPos2, endPos2, p);
		}
	}

	/// <summary>Load texture from file into GPU memory (VRAM)</summary>
	public static Texture2D LoadTexture(string fileName) {
		using var str1 = fileName.ToAnsiBuffer();
		var tex = LoadTexture(str1.AsPointer());
		loadedTextures[tex.Id] = tex;
		return tex;
	}

	/// <summary>Update GPU texture with new data</summary>
	public static void UpdateTexture<T>(Texture2D texture, T[] pixels) where T : unmanaged {
		UpdateTexture(texture, (ReadOnlySpan<T>)pixels);
	}

	/// <summary>Update GPU texture with new data</summary>
	public static void UpdateTexture<T>(Texture2D texture, ReadOnlySpan<T> pixels) where T : unmanaged {
		fixed (void* pixelPtr = pixels) {
			UpdateTexture(texture, pixelPtr);
		}
	}

	/// <summary>Update GPU texture rectangle with new data</summary>
	public static void UpdateTextureRec<T>(Texture2D texture, Rectangle rec, T[] pixels) where T : unmanaged {
		UpdateTextureRec(texture, rec, (ReadOnlySpan<T>)pixels);
	}

	/// <summary>Update GPU texture rectangle with new data</summary>
	public static void UpdateTextureRec<T>(Texture2D texture, Rectangle rec, ReadOnlySpan<T> pixels) where T : unmanaged {
		fixed (void* pixelPtr = pixels) {
			UpdateTextureRec(texture, rec, pixelPtr);
		}
	}

	/// <summary>Generate GPU mipmaps for a texture</summary>
	public static void GenTextureMipmaps(ref Texture2D texture) {
		fixed (Texture2D* p = &texture) {
			GenTextureMipmaps(p);
		}
	}

	/// <summary>Load font from file into GPU memory (VRAM)</summary>
	public static Font LoadFont(string fileName) {
		using var str1 = fileName.ToAnsiBuffer();
		return LoadFont(str1.AsPointer());
	}

	/// <summary>Load font from file with extended parameters</summary>
	public static Font LoadFontEx(string fileName, int fontSize, int[] codepoints, int codepointCount) {
		using var str1 = fileName.ToAnsiBuffer();
		fixed (int* p = codepoints) {
			return LoadFontEx(str1.AsPointer(), fontSize, p, codepointCount);
		}
	}

	/// <summary>
	/// Load font from managed memory, fileType refers to extension: i.e. "ttf"
	/// </summary>
	public static Font LoadFontFromMemory(
		string fileType,
		byte[] fileData,
		int fontSize,
		int[] codepoints,
		int codepointCount
	) {
		using var fileTypeNative = fileType.ToAnsiBuffer();
		fixed (byte* fileDataNative = fileData) {
			fixed (int* fontCharsNative = codepoints) {
				Font font = LoadFontFromMemory(
					fileTypeNative.AsPointer(),
					fileDataNative,
					fileData.Length,
					fontSize,
					fontCharsNative,
					codepointCount
				);

				return font;
			}
		}
	}

	/// <summary>Upload vertex data into GPU and provided VAO/VBO ids</summary>
	public static void UploadMesh(ref Mesh mesh, CBool dynamic) {
		fixed (Mesh* p = &mesh) {
			UploadMesh(p, dynamic);
		}
	}

	/// <summary>Unload mesh from memory (RAM and/or VRAM)</summary>
	public static void UnloadMesh(ref Mesh mesh) {
		fixed (Mesh* p = &mesh) {
			UnloadMesh(p);
		}
	}

	/// <summary>Set texture for a material map type (MAP_DIFFUSE, MAP_SPECULAR...)</summary>
	public static void SetMaterialTexture(ref Material material, MaterialMapIndex mapType, Texture2D texture) {
		fixed (Material* p = &material) {
			SetMaterialTexture(p, mapType, texture);
		}
	}

	/// <summary>Set material for a mesh</summary>
	public static void SetModelMeshMaterial(ref Model model, int meshId, int materialId) {
		fixed (Model* p = &model) {
			SetModelMeshMaterial(p, meshId, materialId);
		}
	}

	/// <summary>Load model animations from file</summary>
	public static ModelAnimation* LoadModelAnimations(string fileName, ref uint animCount) {
		using var str1 = fileName.ToAnsiBuffer();
		fixed (uint* p = &animCount) {
			return LoadModelAnimations(str1.AsPointer(), p);
		}
	}

	/// <summary>Compute mesh tangents</summary>
	public static void GenMeshTangents(ref Mesh mesh) {
		fixed (Mesh* p = &mesh) {
			GenMeshTangents(p);
		}
	}

	/// <summary>Draw lines sequence</summary>
	public static void DrawLineStrip(Vector2[] points, int pointCount, Color color) {
		fixed (Vector2* p = points) {
			DrawLineStrip(p, pointCount, color);
		}
	}

	/// <summary>Draw a triangle fan defined by points (first vertex is the center)</summary>
	public static void DrawTriangleFan(Vector2[] points, int pointCount, Color color) {
		fixed (Vector2* p = points) {
			DrawTriangleFan(p, pointCount, color);
		}
	}

	/// <summary>Draw a triangle strip defined by points</summary>
	public static void DrawTriangleStrip(Vector2[] points, int pointCount, Color color) {
		fixed (Vector2* p = points) {
			DrawTriangleStrip(p, pointCount, color);
		}
	}

	/// <summary>Draw spline: Linear, minimum 2 points</summary>
	public static void DrawSplineLinear(Vector2[] points, int pointCount, float thick, Color color) {
		fixed (Vector2* p = points) {
			DrawSplineLinear(p, pointCount, thick, color);
		}
	}

	/// <summary>Draw spline: B-Spline, minimum 4 points</summary>
	public static void DrawSplineBasis(Vector2[] points, int pointCount, float thick, Color color) {
		fixed (Vector2* p = points) {
			DrawSplineBasis(p, pointCount, thick, color);
		}
	}

	/// <summary>Draw spline: Catmull-Rom, minimum 4 points</summary>
	public static void DrawSplineCatmullRom(Vector2[] points, int pointCount, float thick, Color color) {
		fixed (Vector2* p = points) {
			DrawSplineCatmullRom(p, pointCount, thick, color);
		}
	}

	/// <summary>Draw spline: Quadratic Bezier, minimum 3 points (1 control point): [p1, c2, p3, c4...]</summary>
	public static void DrawSplineBezierQuadratic(Vector2[] points, int pointCount, float thick, Color color) {
		fixed (Vector2* p = points) {
			DrawSplineBezierQuadratic(p, pointCount, thick, color);
		}
	}

	/// <summary>Draw spline: Cubic Bezier, minimum 4 points (2 control points): [p1, c2, c3, p4, c5, c6...]</summary>
	public static void DrawSplineBezierCubic(Vector2[] points, int pointCount, float thick, Color color) {
		fixed (Vector2* p = points) {
			DrawSplineBezierCubic(p, pointCount, thick, color);
		}
	}

	/// <summary>Draw text (using default font)</summary>
	public static void DrawText(ReadOnlySpan<char> text, int posX, int posY, int fontSize, Color color) {
		using var str1 = text.ToUtf8Buffer();
		DrawText(str1.AsPointer(), posX, posY, fontSize, color);
	}

	/// <summary>Draw text using font and additional parameters</summary>
	public static void DrawTextEx(
		Font font,
		ReadOnlySpan<char> text,
		Vector2 position,
		float fontSize,
		float spacing,
		Color tint
	) {
		using var str1 = text.ToUtf8Buffer();
		DrawTextEx(font, str1.AsPointer(), position, fontSize, spacing, tint);
	}

	/// <summary>Draw text using Font and pro parameters (rotation)</summary>
	public static void DrawTextPro(
		Font font,
		ReadOnlySpan<char> text,
		Vector2 position,
		Vector2 origin,
		float rotation,
		float fontSize,
		float spacing,
		Color tint
	) {
		using var str1 = text.ToUtf8Buffer();
		DrawTextPro(font, str1.AsPointer(), position, origin, rotation, fontSize, spacing, tint);
	}

	/// <summary>Measure string width for default font</summary>
	public static int MeasureText(ReadOnlySpan<char> text, int fontSize) {
		using var str1 = text.ToUtf8Buffer();
		return MeasureText(str1.AsPointer(), fontSize);
	}

	public static Vector2 MeasureTextEx(Font font, ReadOnlySpan<char> text, float fontSize, float spacing) {
		using var str1 = text.ToUtf8Buffer();
		return MeasureTextEx(font, str1.AsPointer(), fontSize, spacing);
	}

	/// <summary>Get all codepoints in a string, codepoints count returned by parameters</summary>
	public static int[] LoadCodepoints(ReadOnlySpan<char> text, ref int count) {
		using var str1 = text.ToUtf8Buffer();
		fixed (int* c = &count) {
			var pointsPtr = LoadCodepoints(str1.AsPointer(), c);
			var codepoints = new ReadOnlySpan<int>(pointsPtr, count).ToArray();
			UnloadCodepoints(pointsPtr);
			return codepoints;
		}
	}

	/// <summary>Get total number of codepoints in a UTF8 encoded string</summary>
	public static int GetCodepointCount(ReadOnlySpan<char> text) {
		using var str1 = text.ToUtf8Buffer();
		return GetCodepointCount(str1.AsPointer());
	}

	/// <summary>Get next codepoint in a UTF-8 encoded string, 0x3f('?') is returned on failure</summary>
	public static int GetCodepoint(ReadOnlySpan<char> text, ref int codepointSize) {
		using var str1 = text.ToUtf8Buffer();
		fixed (int* p = &codepointSize) {
			return GetCodepoint(str1.AsPointer(), p);
		}
	}

	/// <summary>Encode one codepoint into UTF-8 byte array (array length returned as parameter)</summary>
	public static string CodepointToUTF8(int codepoint, ref int utf8Size) {
		fixed (int* l1 = &utf8Size) {
			var ptr = CodepointToUTF8(codepoint, l1);
			return Utf8StringUtils.GetUTF8String(ptr);
		}
	}

	/// <summary>Load UTF-8 text encoded from codepoints array</summary>
	public static string LoadUTF8(int[] codepoints, int length) {
		fixed (int* c1 = codepoints) {
			var ptr = LoadUTF8(c1, length);
			var text = Utf8StringUtils.GetUTF8String(ptr);
			MemFree(ptr);
			return text;
		}
	}

	/// <summary>Draw a model (with texture if set)</summary>
	public static Model LoadModel(string fileName) {
		using var str1 = fileName.ToAnsiBuffer();
		return LoadModel(str1.AsPointer());
	}

	/// <summary>Export mesh data to file, returns true on success</summary>
	public static CBool ExportMesh(Mesh mesh, string fileName) {
		using var str1 = fileName.ToAnsiBuffer();
		return ExportMesh(mesh, str1.AsPointer());
	}

	/// <summary>Draw a triangle strip defined by points</summary>
	public static void DrawTriangleStrip3D(Vector3[] points, int pointCount, Color color) {
		fixed (Vector3* p = points) {
			DrawTriangleStrip3D(p, pointCount, color);
		}
	}

	/// <summary>Draw multiple mesh instances with material and different transforms</summary>
	public static void DrawMeshInstanced(Mesh mesh, Material material, Matrix4x4[] transforms, int instances) {
		fixed (Matrix4x4* p = transforms) {
			DrawMeshInstanced(mesh, material, p, instances);
		}
	}

	public static string SubText(this string input, int position, int length) {
		return input.Substring(position, Math.Min(length, input.Length));
	}

	public static Material GetMaterial(ref Model model, int materialIndex) {
		return model.Materials[materialIndex];
	}

	public static Texture2D GetMaterialTexture(ref Model model, int materialIndex, MaterialMapIndex mapIndex) {
		return model.Materials[materialIndex].Maps[(int)mapIndex].Texture;
	}

	public static void SetMaterialTexture(
		ref Model model,
		int materialIndex,
		MaterialMapIndex mapIndex,
		ref Texture2D texture
	) {
		SetMaterialTexture(&model.Materials[materialIndex], mapIndex, texture);
	}

	public static void SetMaterialShader(ref Model model, int materialIndex, ref Shader shader) {
		model.Materials[materialIndex].Shader = shader;
	}
}
