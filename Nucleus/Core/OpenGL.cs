// I forgot where I got this from but thank you random person who wrote C# wrapping over the entire OpenGL library

using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Reflection;
using System.Linq.Expressions;

namespace Nucleus
{
    /// <summary>
    ///     Returns a function pointer for the OpenGL function with the specified name. 
    /// </summary>
    /// <param name="funcName">The name of the function to lookup.</param>
    public delegate IntPtr GetProcAddressHandler(string funcName);

    /// <summary>
    ///     Provides bindings for OpenGL 3.3 Core Profile.
    /// </summary>
    [SuppressUnmanagedCodeSecurity]
    public static unsafe partial class OpenGL
    {
        private static string PtrToStringUtf8(IntPtr ptr) {
            var length = 0;
            while (Marshal.ReadByte(ptr, length) != 0)
                length++;
            var buffer = new byte[length];
            Marshal.Copy(ptr, buffer, 0, length);
            return Encoding.UTF8.GetString(buffer);
        }

        private static string PtrToStringUtf8(IntPtr ptr, int length) {
            var buffer = new byte[length];
            Marshal.Copy(ptr, buffer, 0, length);
            return Encoding.UTF8.GetString(buffer);
        }

        /// <summary>
        ///     The unsafe NULL pointer.
        ///     <para>Analog of IntPtr.Zero.</para>
        /// </summary>
        public static readonly void* NULL = (void*)0;

        /// <summary>
        ///     Specify whether front- or back-facing facets can be culled.
        /// </summary>
        /// <param name="mode">
        ///     Specifies whether front- or back-facing facets are candidates for culling.
        ///     <para>GL_FRONT, BACK, and FRONT_AND_BACK are accepted. The initial value is BACK.</para>
        /// </param>
        /// <remarks>If mode is FRONT_AND_BACK, no facets are drawn, but other primitives such as points and lines are drawn.</remarks>
        public static void CullFace(int mode) => _glCullFace(mode);

        /// <summary>
        ///     Define front- and back-facing polygons.
        /// </summary>
        /// <param name="mode">
        ///     Specifies the orientation of front-facing polygons.
        ///     <para>GL_CW and CCW are accepted. The initial value is CCW.</para>
        /// </param>
        public static void FrontFace(int mode) => _glFrontFace(mode);

        /// <summary>
        ///     Specify implementation-specific hints.
        /// </summary>
        /// <param name="target">
        ///     Specifies a symbolic constant indicating the behavior to be controlled.
        ///     <para>
        ///         LINE_SMOOTH_HINT, POLYGON_SMOOTH_HINT, TEXTURE_COMPRESSION_HINT, and
        ///         FRAGMENT_SHADER_DERIVATIVE_HINT are accepted.
        ///     </para>
        /// </param>
        /// <param name="mode">Specifies a symbolic constant indicating the desired behavior.</param>
        public static void Hint(int target, int mode) => _glHint(target, mode);

        /// <summary>
        ///     Specify the width of rasterized lines.
        /// </summary>
        /// <param name="width">Specifies the width of rasterized lines.<para>The initial value is <c>1.0f</c></para>.</param>
        public static void LineWidth(float width) => _glLineWidth(width);

        /// <summary>
        ///     Specify the diameter of rasterized points.
        /// </summary>
        /// <param name="size">Specifies the diameter of rasterized points.<para>The initial value is <c>1.0f</c>.</para></param>
        public static void PointSize(float size) => _glPointSize(size);

        /// <summary>
        ///     Select a polygon rasterization mode
        /// </summary>
        /// <param name="face">
        ///     Specifies the polygons that mode applies to. Must be FRONT_AND_BACK for front- and back-facing
        ///     polygons
        /// </param>
        /// <param name="mode">
        ///     Specifies how polygons will be rasterized.
        ///     <para>Accepted values are POINT, LINE, and FILL.</para>
        ///     The initial value is FILL for both front- and back-facing polygons.
        /// </param>
        public static void PolygonMode(int face, int mode) => _glPolygonMode(face, mode);

        /// <summary>
        ///     Define the scissor box.
        /// </summary>
        /// <param name="x">
        ///     Specify the lower left corner of the scissor box on the x-axis
        ///     <para>Initially <c>0</c>.</para>
        /// </param>
        /// <param name="y">
        ///     Specify the lower left corner of the scissor box on the y-axis
        ///     <para>Initially <c>0</c>.</para>
        /// </param>
        /// <param name="width">Specify the width of the scissor box.</param>
        /// <param name="height">Specify the height of the scissor box.</param>
        /// <remarks>When a GL context is first attached to a window, width and height are set to the dimensions of that window.</remarks>
        public static void Scissor(int x, int y, int width, int height) => _glScissor(x, y, width, height);

        /// <summary>
        ///     Specify clear values for the color buffers.
        /// </summary>
        /// <param name="red">The red component value, a value between <c>0.0f</c> and <c>1.0f</c>.</param>
        /// <param name="green">The green component value, a value between <c>0.0f</c> and <c>1.0f</c>.</param>
        /// <param name="blue">The blue component value, a value between <c>0.0f</c> and <c>1.0f</c>.</param>
        /// <param name="alpha">The alpha component value, a value between <c>0.0f</c> and <c>1.0f</c>.</param>
        /// <remarks>Initial values are (0, 0, 0, 0)</remarks>
        public static void ClearColor(float red, float green, float blue, float alpha) => _glClearColor(red, green, blue, alpha);

        /// <summary>
        ///     Clear buffers to preset values.
        ///     <para>The value to which each buffer is cleared depends on the setting of the clear value for that buffer.</para>
        /// </summary>
        /// <param name="mask">
        ///     Bitwise OR of masks that indicate the buffers to be cleared.
        ///     <para>The three masks are COLOR_BUFFER_BIT, DEPTH_BUFFER_BIT,, and STENCIL_BUFFER_BIT.</para>
        /// </param>
        public static void Clear(uint mask) => _glClear(mask);

        /// <summary>
        ///     Block until all GL execution is complete.
        ///     <para>
        ///         Does not return until the effects of all previously called GL commands are complete. Such effects include all
        ///         changes to GL state, all changes to connection state, and all changes to the frame buffer contents.
        ///     </para>
        /// </summary>
        public static void Finish() => _glFinish();

        /// <summary>
        ///     Force execution of GL commands in finite time.
        /// </summary>
        public static void Flush() => _glFlush();

        /// <summary>
        ///     Enable server-side GL capabilities.
        /// </summary>
        /// <param name="cap">Specifies a symbolic constant indicating a GL capability.</param>
        public static void Enable(int cap) => _glEnable(cap);
        public static void AlphaFunc(int cond, float threshold) => _glAlphaFunc(cond, threshold);

        /// <summary>
        ///     Disable server-side GL capabilities.
        /// </summary>
        /// <param name="cap">Specifies a symbolic constant indicating a GL capability.</param>
        public static void Disable(int cap) => _glDisable(cap);

        /// <summary>
        ///     Specify the clear value for the stencil buffer.
        /// </summary>
        /// <param name="index">
        ///     Specifies the index used when the stencil buffer is cleared.
        ///     <para>The initial value is 0.</para>
        /// </param>
        public static void ClearStencil(int index) => _glClearStencil(index);

        /// <summary>
        ///     Specify the clear value for the depth buffer.
        /// </summary>
        /// <param name="depth">
        ///     Specifies the depth value used when the depth buffer is cleared.
        ///     <para>he initial value is <c>1.0</c>.</para>
        /// </param>
        public static void ClearDepth(double depth) => _glClearDepth(depth);

        /// <summary>
        ///     Control the front and back writing of individual bits in the stencil planes.
        /// </summary>
        /// <param name="mask">
        ///     Specifies a bit mask to enable and disable writing of individual bits in the stencil planes.
        ///     <para>Initially, the mask is all 1's</para>
        ///     .
        /// </param>
        public static void StencilMask(uint mask) => _glStencilMask(mask);

        /// <summary>
        ///     Enable and disable writing of frame buffer color components
        /// </summary>
        /// <param name="red">Specify whether red will be written into the frame buffer.</param>
        /// <param name="green">Specify whether green will be written into the frame buffer.</param>
        /// <param name="blue">Specify whether blue will be written into the frame buffer.</param>
        /// <param name="alpha">Specify whether alpha will be written into the frame buffer.</param>
        public static void ColorMask(bool red, bool green, bool blue, bool alpha) => _glColorMask(red, green, blue, alpha);

        /// <summary>
        ///     Enable and disable writing of frame buffer color components
        /// </summary>
        /// <param name="index">Specifies the index of the draw buffer whose color mask to set.</param>
        /// <param name="red">Specify whether red will be written into the frame buffer.</param>
        /// <param name="green">Specify whether green will be written into the frame buffer.</param>
        /// <param name="blue">Specify whether blue will be written into the frame buffer.</param>
        /// <param name="alpha">Specify whether alpha will be written into the frame buffer.</param>
        public static void ColorMaski(uint index, bool red, bool green, bool blue, bool alpha) => _glColorMaski(index, red, green, blue, alpha);

        /// <summary>
        ///     Enable or disable writing into the depth buffer.
        /// </summary>
        /// <param name="enabled">Specifies whether the depth buffer is enabled for writing.</param>
        public static void DepthMask(bool enabled) => _glDepthMask(enabled);

        /// <summary>
        ///     Set the blend color.
        /// </summary>
        /// <param name="red">Specify the red component of the color to blend.</param>
        /// <param name="green">Specify the green component of the color to blend.</param>
        /// <param name="blue">Specify the blue component of the color to blend.</param>
        /// <param name="alpha">Specify the alpha component of the color to blend.</param>
        public static void BlendColor(float red, float green, float blue, float alpha) => _glBlendColor(red, green, blue, alpha);

        /// <summary>
        ///     Specify pixel arithmetic.
        /// </summary>
        /// <param name="srcFactor">
        ///     Specifies how the red, green, blue, and alpha source blending factors are computed.
        ///     <para>The initial value is ONE.</para>
        /// </param>
        /// <param name="dstFactor">
        ///     Specifies how the red, green, blue, and alpha destination blending factors are computed.
        ///     <para>The initial value is ZERO.</para>
        /// </param>
        public static void BlendFunc(int srcFactor, int dstFactor) => _glBlendFunc(srcFactor, dstFactor);

        /// <summary>
        ///     Specify the equation used for both the RGB blend equation and the Alpha blend equation.
        /// </summary>
        /// <param name="mode">Specifies how source and destination colors are combined.</param>
        public static void BlendEquation(int mode) => _glBlendEquation(mode);

        /// <summary>
        ///     Set the viewport.
        /// </summary>
        /// <param name="x">The lower left corner of the viewport rectangle on the x-axis, in pixels.</param>
        /// <param name="y">The lower left corner of the viewport rectangle on the y-axis, in pixels.</param>
        /// <param name="width">The width of the viewport, in pixels.</param>
        /// <param name="height">The height of the viewport.</param>
        public static void Viewport(int x, int y, int width, int height) => _glViewport(x, y, width, height);

        /// <summary>
        ///     Test whether a capability is enabled.
        /// </summary>
        /// <param name="cap">Specifies a symbolic constant indicating a GL capability.</param>
        /// <returns><c>true</c> if capability is enabled, otherwise <c>false</c>.</returns>
        public static bool IsEnabled(int cap) => _glIsEnabled(cap);

        /// <summary>
        ///     Render primitives from array data.
        /// </summary>
        /// <param name="mode">Specifies what kind of primitives to render.</param>
        /// <param name="first">Specifies the starting index in the enabled arrays.</param>
        /// <param name="count">Specifies the number of indices to be rendered.</param>
        public static void DrawArrays(int mode, int first, int count) => _glDrawArrays(mode, first, count);

        /// <summary>
        ///     Specify which color buffers are to be drawn into.
        /// </summary>
        /// <param name="buffer">Specifies the color buffer to be drawn into.</param>
        public static void DrawBuffer(int buffer) => _glDrawBuffer(buffer);

        /// <summary>
        ///     Select a color buffer source for pixels.
        /// </summary>
        /// <param name="buffer">Specifies a color buffer.</param>
        public static void ReadBuffer(int buffer) => _glReadBuffer(buffer);

        /// <summary>
        ///     Specify a logical pixel operation for rendering.
        /// </summary>
        /// <param name="opcode"></param>
        public static void LogicOp(int opcode) => _glLogicOp(opcode);

        /// <summary>
        ///     Set front and back function and reference value for stencil testing.
        /// </summary>
        /// <param name="func">Specifies the test function.</param>
        /// <param name="reference">Specifies the reference value for the stencil test.</param>
        /// <param name="mask">
        ///     Specifies a mask that is ANDed with both the reference value and the stored stencil value when the
        ///     test is done.
        ///     <para>The initial value is all 1's.</para>
        /// </param>
        public static void StencilFunc(int func, int reference, uint mask) => _glStencilFunc(func, reference, mask);

        /// <summary>
        ///     Set front and back stencil test actions.
        /// </summary>
        /// <param name="fail">Specifies the action to take when the stencil test fail.</param>
        /// <param name="zfail">Specifies the stencil action when the stencil test passes, but the depth test fails.</param>
        /// <param name="zpass">
        ///     Specifies the stencil action when both the stencil test and the depth test pass, or when the
        ///     stencil test passes and either there is no depth buffer or depth testing is not enabled
        /// </param>
        public static void StencilOp(int fail, int zfail, int zpass) => _glStencilOp(fail, zfail, zpass);

        /// <summary>
        ///     Specify the value used for depth buffer comparisons.
        /// </summary>
        /// <param name="func">Specifies the depth comparison function.</param>
        public static void DepthFunc(int func) => _glDepthFunc(func);

        /// <summary>
        ///     Start conditional rendering.
        /// </summary>
        /// <param name="id">
        ///     Specifies the name of an occlusion query object whose results are used to determine if the rendering
        ///     commands are discarded.
        /// </param>
        /// <param name="mode">Specifies how the results of the occlusion query is interpreted.</param>
        public static void BeginConditionalRender(uint id, int mode) => _glBeginConditionalRender(id, mode);

        /// <summary>
        ///     Ends conditional rendering.
        /// </summary>
        public static void EndConditionalRender() => _glEndConditionalRender();

        /// <summary>
        ///     Specify whether data read via ReadPixels should be clamped.
        /// </summary>
        /// <param name="clamp">Specifies whether to apply color clamping.</param>
        public static void ClampColor(bool clamp) => _glClampColor(CLAMP_READ_COLOR, clamp ? TRUE : FALSE);

        /// <summary>
        ///     Return a string describing the current GL connection.
        /// </summary>
        /// <param name="name">
        ///     Specifies a symbolic constant, one of VENDOR, RENDERER, VERSION, or
        ///     SHADING_LANGUAGE_VERSION
        /// </param>
        /// <returns>The requested value.</returns>


        public static string GetString(int name) {
            var buffer = new IntPtr(_glGetString(name));
            return PtrToStringUtf8(buffer);
        }

        /// <summary>
        ///     Return a string describing the current GL connection.
        /// </summary>
        /// <param name="name">
        ///     Specifies a symbolic constant, one of VENDOR, RENDERER, VERSION, SHADING_LANGUAGE_VERSION, or EXTENSIONS.
        /// </param>
        /// <param name="index">The index of the string to return.</param>
        /// <returns>The requested value.</returns>


        public static string GetStringi(int name, uint index) {
            var buffer = new IntPtr(_glGetStringi(name, index));
            return PtrToStringUtf8(buffer);
        }

        /// <summary>
        ///     Set pixel storage modes.
        /// </summary>
        /// <param name="paramName">
        ///     Specifies the symbolic name of the parameter to be set. One value affects the packing of pixel data
        ///     into memory: PACK_ALIGNMENT. The other affects the unpacking of pixel data from memory: UNPACK_ALIGNMENT.
        /// </param>
        /// <param name="param">Specifies the value that <paramref name="paramName" /> is set to. Valid values are 1, 2, 4, or 8.</param>
        public static void PixelStorei(int paramName, int param) => _glPixelStorei(paramName, param);

        /// <summary>
        ///     Set pixel storage modes.
        /// </summary>
        /// <param name="paramName">Specifies the symbolic name of the parameter to be set.</param>
        /// <param name="param">Specifies the value that <paramref name="paramName" /> is set to.</param>
        public static void PixelStoref(int paramName, float param) => _glPixelStoref(paramName, param);

        /// <summary>
        ///     Creates and initializes a buffer object's data store.
        /// </summary>
        /// <param name="target">Specifies the target to which the buffer object is bound.</param>
        /// <param name="size">Specifies the size in bytes of the buffer object's new data store.</param>
        /// <param name="data">
        ///     Specifies a pointer to data that will be copied into the data store for initialization, or NULL if
        ///     no data is to be copied.
        /// </param>
        /// <param name="usage">
        ///     Specifies the expected usage pattern of the data store.
        ///     <para>
        ///         Must be STREAM_DRAW, STREAM_READ, STREAM_COPY, STATIC_DRAW, STATIC_READ, STATIC_COPY,
        ///         DYNAMIC_DRAW, DYNAMIC_READ, or DYNAMIC_COPY.
        ///     </para>
        ///     .
        /// </param>
        public static void BufferData(int target, int size, IntPtr data, int usage) => _glBufferData(target, new IntPtr(size), data.ToPointer(), usage);

        /// <summary>
        ///     Creates and initializes a buffer object's data store.
        /// </summary>
        /// <param name="target">Specifies the target to which the buffer object is bound.</param>
        /// <param name="size">Specifies the size in bytes of the buffer object's new data store.</param>
        /// <param name="data">
        ///     Specifies a pointer to data that will be copied into the data store for initialization, or NULL if
        ///     no data is to be copied.
        /// </param>
        /// <param name="usage">
        ///     Specifies the expected usage pattern of the data store.
        ///     <para>
        ///         Must be STREAM_DRAW, STREAM_READ, STREAM_COPY, STATIC_DRAW, STATIC_READ, STATIC_COPY,
        ///         DYNAMIC_DRAW, DYNAMIC_READ, or DYNAMIC_COPY.
        ///     </para>
        ///     .
        /// </param>
        public static void BufferData(int target, int size, /*const*/ void* data, int usage) => _glBufferData(target, new IntPtr(size), data, usage);

        /// <summary>
        ///     Gets the stored error code information.
        /// </summary>
        /// <returns>An OpenGL error code.</returns>
        public static int GetError() => _glGetError();

        /// <summary>
        ///     Set texture parameters.
        /// </summary>
        /// <param name="target">
        ///     Specifies the target texture of the active texture unit, which must be either TEXTURE_2D or
        ///     TEXTURE_CUBE_MAP.
        /// </param>
        /// <param name="paramName">
        ///     Specifies the symbolic name of a single-valued texture parameter. <paramref name="paramName" /> can be
        ///     one of the following: TEXTURE_MIN_FILTER, TEXTURE_MAG_FILTER, TEXTURE_WRAP_S, or TEXTURE_WRAP_T.
        /// </param>
        /// <param name="param">Specifies the value of <paramref name="paramName" />.</param>
        public static void TexParameterf(int target, int paramName, float param) => _glTexParameterf(target, paramName, param);

        /// <summary>
        ///     Set texture parameters.
        /// </summary>
        /// <param name="target">
        ///     Specifies the target texture of the active texture unit, which must be either TEXTURE_2D or
        ///     TEXTURE_CUBE_MAP.
        /// </param>
        /// <param name="paramName">
        ///     Specifies the symbolic name of a single-valued texture parameter. <paramref name="paramName" /> can be
        ///     one of the following: TEXTURE_MIN_FILTER, TEXTURE_MAG_FILTER, TEXTURE_WRAP_S, or TEXTURE_WRAP_T.
        /// </param>
        /// <param name="param">Specifies the value of <paramref name="paramName" />.</param>
        public static void TexParameteri(int target, int paramName, int param) => _glTexParameteri(target, paramName, param);

        /// <summary>
        ///     Set texture parameters.
        /// </summary>
        /// <param name="target">
        ///     Specifies the target texture of the active texture unit, which must be either TEXTURE_2D or
        ///     TEXTURE_CUBE_MAP.
        /// </param>
        /// <param name="paramName">
        ///     Specifies the symbolic name of a single-valued texture parameter. <paramref name="paramName" /> can be
        ///     one of the following: TEXTURE_MIN_FILTER, TEXTURE_MAG_FILTER, TEXTURE_WRAP_S, or TEXTURE_WRAP_T.
        /// </param>
        /// <param name="param">Specifies the value of <paramref name="paramName" />.</param>
        public static void TexParameterfv(int target, int paramName, float* param) => _glTexParameterfv(target, paramName, param);

        /// <summary>
        ///     Set texture parameters.
        /// </summary>
        /// <param name="target">
        ///     Specifies the target texture of the active texture unit, which must be either TEXTURE_2D or
        ///     TEXTURE_CUBE_MAP.
        /// </param>
        /// <param name="paramName">
        ///     Specifies the symbolic name of a single-valued texture parameter. <paramref name="paramName" /> can be
        ///     one of the following: TEXTURE_MIN_FILTER, TEXTURE_MAG_FILTER, TEXTURE_WRAP_S, or TEXTURE_WRAP_T.
        /// </param>
        /// <param name="param">Specifies the value of <paramref name="paramName" />.</param>
        public static void TexParameteriv(int target, int paramName, int* param) => _glTexParameteriv(target, paramName, param);

        /// <summary>
        ///     Set texture parameters.
        /// </summary>
        /// <param name="target">
        ///     Specifies the target texture of the active texture unit, which must be either TEXTURE_2D or
        ///     TEXTURE_CUBE_MAP.
        /// </param>
        /// <param name="paramName">
        ///     Specifies the symbolic name of a single-valued texture parameter. <paramref name="paramName" /> can be
        ///     one of the following: TEXTURE_MIN_FILTER, TEXTURE_MAG_FILTER, TEXTURE_WRAP_S, or TEXTURE_WRAP_T.
        /// </param>
        /// <param name="param">Specifies the value of <paramref name="paramName" />.</param>
        public static void TexParameterfv(int target, int paramName, float[] param) {
            fixed (float* p = &param[0]) {
                _glTexParameterfv(target, paramName, p);
            }
        }

        /// <summary>
        ///     Set texture parameters.
        /// </summary>
        /// <param name="target">
        ///     Specifies the target texture of the active texture unit, which must be either TEXTURE_2D or
        ///     TEXTURE_CUBE_MAP.
        /// </param>
        /// <param name="paramName">
        ///     Specifies the symbolic name of a single-valued texture parameter. <paramref name="paramName" /> can be
        ///     one of the following: TEXTURE_MIN_FILTER, TEXTURE_MAG_FILTER, TEXTURE_WRAP_S, or TEXTURE_WRAP_T.
        /// </param>
        /// <param name="param">Specifies the value of <paramref name="paramName" />.</param>
        public static void TexParameteriv(int target, int paramName, int[] param) {
            fixed (int* p = &param[0]) {
                _glTexParameteriv(target, paramName, p);
            }
        }

        /// <summary>
        ///     Specify mapping of depth values from normalized device coordinates to window coordinates.
        /// </summary>
        /// <param name="near">
        ///     Specifies the mapping of the near clipping plane to window coordinates.
        ///     <c>The initial value is 0.</c>
        /// </param>
        /// <param name="far">
        ///     Specifies the mapping of the far clipping plane to window coordinates.<c>The initial value is 1.</c>
        /// </param>
        public static void DepthRange(double near, double far) => _glDepthRange(near, far);

        /// <summary>
        ///     Render primitives from array data.
        /// </summary>
        /// <param name="mode">Specifies what kind of primitives to render.</param>
        /// <param name="count">Specifies the number of elements to be rendered.</param>
        /// <param name="type">
        ///     Specifies the type of the values in indices.
        ///     <para>Must be one of UNSIGNED_BYTE, UNSIGNED_SHORT, or UNSIGNED_INT.</para>
        /// </param>
        /// <param name="indices">Specifies a pointer to the location where the indices are stored.</param>
        public static void DrawElements(int mode, int count, int type, /*const*/ void* indices) => _glDrawElements(mode, count, type, indices);

        /// <summary>
        ///     Render primitives from array data.
        /// </summary>
        /// <param name="mode">Specifies what kind of primitives to render.</param>
        /// <param name="indices">An array containing the indices.</param>
        public static void DrawElements(int mode, byte[] indices) {
            fixed (void* pointer = &indices[0]) {
                _glDrawElements(mode, indices.Length, UNSIGNED_BYTE, pointer);
            }
        }

        /// <summary>
        ///     Render primitives from array data.
        /// </summary>
        /// <param name="mode">Specifies what kind of primitives to render.</param>
        /// <param name="indices">An array containing the indices.</param>
        public static void DrawElements(int mode, ushort[] indices) {
            fixed (void* pointer = &indices[0]) {
                _glDrawElements(mode, indices.Length, UNSIGNED_SHORT, pointer);
            }
        }

        /// <summary>
        ///     Render primitives from array data.
        /// </summary>
        /// <param name="mode">Specifies what kind of primitives to render.</param>
        /// <param name="indices">An array containing the indices.</param>
        public static void DrawElements(int mode, uint[] indices) {
            fixed (void* pointer = &indices[0]) {
                _glDrawElements(mode, indices.Length, UNSIGNED_INT, pointer);
            }
        }

        /// <summary>
        ///     Return a texture image.
        /// </summary>
        /// <param name="target">Specifies the target to which the texture is bound.</param>
        /// <param name="level">Specifies the level-of-detail number of the desired image. Level 0 is the base image level. Level n is the nth mipmap reduction image.</param>
        /// <param name="format">Specifies a pixel format for the returned data. </param>
        /// <param name="type">Specifies a pixel type for the returned data.</param>
        /// <param name="pixels">Returns the texture image. Should be a pointer to an array of the type specified by type.</param>
        public static void GetTexImage(int target, int level, int format, int type, void* pixels) => _glGetTexImage(target, level, format, type, pixels);

        /// <summary>
        ///     Return a texture image.
        /// </summary>
        /// <param name="target">Specifies the target to which the texture is bound.</param>
        /// <param name="level">Specifies the level-of-detail number of the desired image. Level 0 is the base image level. Level n is the nth mipmap reduction image.</param>
        /// <param name="format">Specifies a pixel format for the returned data. </param>
        /// <param name="type">Specifies a pixel type for the returned data.</param>
        /// <param name="pixels">Returns the texture image. Should be a pointer to an array of the type specified by type.</param>
        public static void GetTexImage(int target, int level, int format, int type, IntPtr pixels) => _glGetTexImage(target, level, format, type, pixels.ToPointer());

        /// <summary>
        ///     Read a block of pixels from the frame buffer.
        /// </summary>
        /// <param name="x">Specify the window coordinates of the first pixel that is read from the frame buffer on the x-axis.<para>This location is the lower left corner of a rectangular block of pixels.</para></param>
        /// <param name="y">Specify the window coordinates of the first pixel that is read from the frame buffer on the y-axis.<para>This location is the lower left corner of a rectangular block of pixels.</para></param>
        /// <param name="width">Specify the width of the pixel rectangle, in pixels.</param>
        /// <param name="height">Specify the height of the pixel rectangle, in pixels.</param>
        /// <param name="format">Specifies the format of the pixel data.<para>The following symbolic values are accepted: STENCIL_INDEX, DEPTH_COMPONENT, DEPTH_STENCIL, RED, GREEN, BLUE, RGB, BGR, RGBA, and BGRA.</para></param>
        /// <param name="type">Specifies the data type of the pixel data.</param>
        /// <param name="pixels">A pointer where the pixel data will be written.<para>Must have enough memory allocated for the desired dimensions and pixel format.</para></param>
        public static void ReadPixels(int x, int y, int width, int height, int format, int type, void* pixels) => _glReadPixels(x, y, width, height, format, type, pixels);

        /// <summary>
        ///     Read a block of pixels from the frame buffer.
        /// </summary>
        /// <param name="x">Specify the window coordinates of the first pixel that is read from the frame buffer on the x-axis.<para>This location is the lower left corner of a rectangular block of pixels.</para></param>
        /// <param name="y">Specify the window coordinates of the first pixel that is read from the frame buffer on the y-axis.<para>This location is the lower left corner of a rectangular block of pixels.</para></param>
        /// <param name="width">Specify the width of the pixel rectangle, in pixels.</param>
        /// <param name="height">Specify the height of the pixel rectangle, in pixels.</param>
        /// <param name="format">Specifies the format of the pixel data.</param>
        /// <param name="type">Specifies the data type of the pixel data.</param>
        /// <param name="pixels">A pointer where the pixel data will be written.<para>Must have enough memory allocated for the desired dimensions and pixel format.</para></param>
        public static void ReadPixels(int x, int y, int width, int height, int format, int type, IntPtr pixels) => _glReadPixels(x, y, width, height, format, type, pixels.ToPointer());

        /// <summary>
        ///     Read a block of pixels from the frame buffer.
        /// </summary>
        /// <param name="x">Specify the window coordinates of the first pixel that is read from the frame buffer on the x-axis.<para>This location is the lower left corner of a rectangular block of pixels.</para></param>
        /// <param name="y">Specify the window coordinates of the first pixel that is read from the frame buffer on the y-axis.<para>This location is the lower left corner of a rectangular block of pixels.</para></param>
        /// <param name="width">Specify the width of the pixel rectangle, in pixels.</param>
        /// <param name="height">Specify the height of the pixel rectangle, in pixels.</param>
        /// <param name="format">Specifies the format of the pixel data.</param>
        /// <param name="type">Specifies the data type of the pixel data.</param>
        /// <param name="pixels">A buffer where the pixel data will be written.<para>Must have enough memory allocated for the desired dimensions and pixel format.</para></param>
        public static void ReadPixels(int x, int y, int width, int height, int format, int type, byte[] pixels) {
            var handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
            var ptr = handle.AddrOfPinnedObject();
            _glReadPixels(x, y, width, height, format, type, ptr.ToPointer());
            handle.Free();
        }

        /// <summary>
        ///     Specifies a list of color buffers to be drawn into.
        /// </summary>
        /// <param name="n">Specifies the number of buffers.</param>
        /// <param name="buffers">Points to an array of symbolic constants specifying the buffers into which fragment colors or data values will be written.</param>
        public static void DrawBuffers(int n, /*const*/ int* buffers) => _glDrawBuffers(n, buffers);

        /// <summary>
        ///     Specifies a list of color buffers to be drawn into.
        /// </summary>
        /// <param name="buffers">PAn array of symbolic constants specifying the buffers into which fragment colors or data values will be written.</param>
        public static void DrawBuffers(int[] buffers) {
            fixed (int* buf = &buffers[0]) {
                _glDrawBuffers(buffers.Length, buf);
            }
        }

        /// <summary>
        ///     Specify a one-dimensional texture image.
        /// </summary>
        /// <param name="target">Specifies the target texture.</param>
        /// <param name="level">
        ///     Specifies the level-of-detail number. Level 0 is the base image level. Level n is the nth mipmap
        ///     reduction image.
        /// </param>
        /// <param name="internalFormat">Specifies the number of color components in the texture. </param>
        /// <param name="width">
        ///     Specifies the width of the texture image.
        ///     <para>All implementations support texture images that are at least 1024 texels wide.</para>
        /// </param>
        /// <param name="border">This value must be 0.</param>
        /// <param name="format">
        ///     Specifies the format of the pixel data.
        /// </param>
        /// <param name="type">
        ///     Specifies the data type of the pixel data.
        /// </param>
        /// <param name="pixels">Specifies a pointer to the image data in memory.</param>
        public static void TexImage1D(int target, int level, int internalFormat, int width, int border, int format, int type, IntPtr pixels) => _glTexImage1D(target, level, internalFormat, width, border, format, type, pixels.ToPointer());

        /// <summary>
        ///     Specify a one-dimensional texture image.
        /// </summary>
        /// <param name="target">Specifies the target texture.</param>
        /// <param name="level">
        ///     Specifies the level-of-detail number. Level 0 is the base image level. Level n is the nth mipmap
        ///     reduction image.
        /// </param>
        /// <param name="internalFormat">Specifies the number of color components in the texture. </param>
        /// <param name="width">
        ///     Specifies the width of the texture image.
        ///     <para>All implementations support texture images that are at least 1024 texels wide.</para>
        /// </param>
        /// <param name="border">This value must be 0.</param>
        /// <param name="format">
        ///     Specifies the format of the pixel data.
        /// </param>
        /// <param name="type">
        ///     Specifies the data type of the pixel data.
        /// </param>
        /// <param name="pixels">Specifies a pointer to the image data in memory.</param>
        public static void TexImage1D(int target, int level, int internalFormat, int width, int border, int format, int type, /*const*/ void* pixels) => _glTexImage1D(target, level, internalFormat, width, border, format, type, pixels);

        /// <summary>
        ///     Specify a two-dimensional texture image.
        /// </summary>
        /// <param name="target">Specifies the target texture.</param>
        /// <param name="level">
        ///     Specifies the level-of-detail number. Level 0 is the base image level. Level n is the nth mipmap
        ///     reduction image.
        /// </param>
        /// <param name="internalFormat">Specifies the number of color components in the texture. </param>
        /// <param name="width">
        ///     Specifies the width of the texture image.
        ///     <para>All implementations support texture images that are at least 1024 texels wide.</para>
        /// </param>
        /// <param name="height">
        ///     Specifies the height of the texture image, or the number of layers in a texture array.
        ///     <para>
        ///         All implementations support 2D texture images that are at least 1024 texels high, and texture arrays that are
        ///         at least 256 layers deep.
        ///     </para>
        /// </param>
        /// <param name="border">This value must be 0.</param>
        /// <param name="format">
        ///     Specifies the format of the pixel data.
        /// </param>
        /// <param name="type">
        ///     Specifies the data type of the pixel data.
        /// </param>
        /// <param name="pixels">Specifies a pointer to the image data in memory.</param>
        public static void TexImage2D(int target, int level, int internalFormat, int width, int height, int border, int format, int type, IntPtr pixels) => _glTexImage2D(target, level, internalFormat, width, height, border, format, type, pixels.ToPointer());

        /// <summary>
        ///     Specify a two-dimensional texture image.
        /// </summary>
        /// <param name="target">Specifies the target texture.</param>
        /// <param name="level">
        ///     Specifies the level-of-detail number. Level 0 is the base image level. Level n is the nth mipmap
        ///     reduction image.
        /// </param>
        /// <param name="internalFormat">Specifies the number of color components in the texture. </param>
        /// <param name="width">
        ///     Specifies the width of the texture image.
        ///     <para>All implementations support texture images that are at least 1024 texels wide.</para>
        /// </param>
        /// <param name="height">
        ///     Specifies the height of the texture image, or the number of layers in a texture array.
        ///     <para>
        ///         All implementations support 2D texture images that are at least 1024 texels high, and texture arrays that are
        ///         at least 256 layers deep.
        ///     </para>
        /// </param>
        /// <param name="border">This value must be 0.</param>
        /// <param name="format">
        ///     Specifies the format of the pixel data.
        /// </param>
        /// <param name="type">
        ///     Specifies the data type of the pixel data.
        /// </param>
        /// <param name="pixels">Specifies a pointer to the image data in memory.</param>
        public static void TexImage2D(int target, int level, int internalFormat, int width, int height, int border, int format, int type, /*const*/ void* pixels) => _glTexImage2D(target, level, internalFormat, width, height, border, format, type, pixels);

        /// <summary>
        ///     Specify a three-dimensional texture image.
        /// </summary>
        /// <param name="target">Specifies the target texture.</param>
        /// <param name="level">
        ///     Specifies the level-of-detail number. Level 0 is the base image level. Level n is the nth mipmap
        ///     reduction image.
        /// </param>
        /// <param name="internalFormat">Specifies the number of color components in the texture. </param>
        /// <param name="width">
        ///     Specifies the width of the texture image.
        ///     <para>All implementations support texture images that are at least 1024 texels wide.</para>
        /// </param>
        /// <param name="height">
        ///     Specifies the height of the texture image, or the number of layers in a texture array.
        ///     <para>
        ///         All implementations support 2D texture images that are at least 1024 texels high, and texture arrays that are
        ///         at least 256 layers deep.
        ///     </para>
        /// </param>
        /// <param name="depth">Specifies the depth of the texture image, or the number of layers in a texture array.<para>All implementations support 3D texture images that are at least 256 texels deep, and texture arrays that are at least 256 layers deep.</para></param>
        /// <param name="border">This value must be 0.</param>
        /// <param name="format">
        ///     Specifies the format of the pixel data.
        /// </param>
        /// <param name="type">
        ///     Specifies the data type of the pixel data.
        /// </param>
        /// <param name="pixels">Specifies a pointer to the image data in memory.</param>
        public static void TexImage3D(int target, int level, int internalFormat, int width, int height, int depth, int border, int format, int type, IntPtr pixels) => _glTexImage3D(target, level, internalFormat, width, height, depth, border, format, type, pixels.ToPointer());

        /// <summary>
        ///     Specify a three-dimensional texture image.
        /// </summary>
        /// <param name="target">Specifies the target texture.</param>
        /// <param name="level">
        ///     Specifies the level-of-detail number. Level 0 is the base image level. Level n is the nth mipmap
        ///     reduction image.
        /// </param>
        /// <param name="internalFormat">Specifies the number of color components in the texture. </param>
        /// <param name="width">
        ///     Specifies the width of the texture image.
        ///     <para>All implementations support texture images that are at least 1024 texels wide.</para>
        /// </param>
        /// <param name="height">
        ///     Specifies the height of the texture image, or the number of layers in a texture array.
        ///     <para>
        ///         All implementations support 2D texture images that are at least 1024 texels high, and texture arrays that are
        ///         at least 256 layers deep.
        ///     </para>
        /// </param>
        /// <param name="depth">Specifies the depth of the texture image, or the number of layers in a texture array.<para>All implementations support 3D texture images that are at least 256 texels deep, and texture arrays that are at least 256 layers deep.</para></param>
        /// <param name="border">This value must be 0.</param>
        /// <param name="format">
        ///     Specifies the format of the pixel data.
        /// </param>
        /// <param name="type">
        ///     Specifies the data type of the pixel data.
        /// </param>
        /// <param name="pixels">Specifies a pointer to the image data in memory.</param>
        public static void TexImage3D(int target, int level, int internalFormat, int width, int height, int depth, int border, int format, int type, /*const*/ void* pixels) => _glTexImage3D(target, level, internalFormat, width, height, depth, border, format, type, pixels);

        /// <summary>
        ///     Bind a named texture to a texturing target.
        /// </summary>
        /// <param name="target">Specifies the target to which the texture is bound.</param>
        /// <param name="texture">Specifies the name of a texture.</param>
        public static void BindTexture(int target, uint texture) => _glBindTexture(target, texture);

        /// <summary>
        ///     Select active texture unit.
        /// </summary>
        /// <param name="texture">Specifies which texture unit to make active.</param>
        public static void ActiveTexture(int texture) => _glActiveTexture(texture);

        /// <summary>
        ///     Delete named textures.
        /// </summary>
        /// <param name="n">Specifies the number of textures to be deleted.</param>
        /// <param name="textures">Specifies an array of textures to be deleted.</param>
        public static void DeleteTextures(int n, /*const*/ uint* textures) => _glDeleteTextures(n, textures);

        /// <summary>
        ///     Delete named textures.
        /// </summary>
        /// <param name="textures">Specifies an array of textures to be deleted.</param>
        public static void DeleteTextures(uint[] textures) {
            if (textures is null)
                return;
            fixed (uint* ids = &textures[0]) {
                _glDeleteTextures(textures.Length, ids);
            }
        }

        /// <summary>
        ///     Deletes a single texture object.
        /// </summary>
        /// <param name="texture">A texture to delete.</param>
        public static void DeleteTexture(uint texture) => _glDeleteTextures(1, &texture);

        /// <summary>
        ///     Determine if a name corresponds to a texture
        /// </summary>
        /// <param name="texture">Specifies a value that may be the name of a texture.</param>
        /// <returns><c>true</c> if object is a texture, otherwise false.</returns>
        public static bool IsTexture(uint texture) => _glIsTexture(texture);

        /// <summary>
        ///     Generate texture names.
        /// </summary>
        /// <param name="n">Specifies the number of texture names to be generated.</param>
        /// <param name="textures">Specifies an array in which the generated texture names are stored.</param>
        public static void GenTextures(int n, uint* textures) => _glGenTextures(n, textures);

        /// <summary>
        ///     Generate texture names.
        /// </summary>
        /// <param name="n">Specifies the number of texture names to be generated.</param>
        /// <returns>Generated texture names.</returns>


        public static uint[] GenTextures(int n) {
            var textures = new uint[n];
            fixed (uint* ids = &textures[0]) {
                _glGenTextures(n, ids);
            }

            return textures;
        }

        /// <summary>
        ///     Generates a single texture name.
        /// </summary>
        /// <returns>The generated texture name.</returns>
        public static uint GenTexture() {
            uint texture;
            _glGenTextures(1, &texture);
            return texture;
        }


        /// <summary>
        ///     Generate a single query object name.
        /// </summary>
        /// <returns>The query object name.</returns>

        public static uint GenQuery() {
            uint id;
            _glGenQueries(1, &id);
            return id;
        }

        /// <summary>
        ///     Generate query object names.
        /// </summary>
        /// <param name="n">Specifies the number of query object names to be generated.</param>
        /// <param name="ids">Specifies an array in which the generated query object names are stored.</param>
        public static void GenQueries(int n, uint* ids) => _glGenQueries(n, ids);

        /// <summary>
        ///     Generate query object names.
        /// </summary>
        /// <param name="n">Specifies the number of query object names to be generated.</param>
        /// <returns>An array of generated query object names.</returns>


        public static uint[] GenQueries(int n) {
            var queries = new uint[n];
            fixed (uint* ids = &queries[0]) {
                _glGenQueries(n, ids);
            }

            return queries;
        }

        /// <summary>
        ///     Set the scale and units used to calculate depth values.
        /// </summary>
        /// <param name="factor">
        ///     Specifies a scale factor that is used to create a variable depth offset for each polygon.
        ///     <para>The initial value is 0.</para>
        /// </param>
        /// <param name="units">
        ///     Is multiplied by an implementation-specific value to create a constant depth offset.
        ///     <para>The initial value is 0.</para>
        /// </param>
        public static void PolygonOffset(float factor, float units) => _glPolygonOffset(factor, units);

        /// <summary>
        /// Specify the vertex to be used as the source of data for flat shaded varyings.
        /// </summary>
        /// <param name="mode">Specifies the vertex to be used as the source of data for flat shaded varyings.</param>
        public static void ProvokingVertex(int mode) => _glProvokingVertex(mode);

        /// <summary>
        ///     Returns a compressed texture image.
        /// </summary>
        /// <param name="target">Specifies the target to which the texture is bound.</param>
        /// <param name="level">
        ///     Specifies the level-of-detail number of the desired image.
        ///     <para>Level 0 is the base image level. Level n is the n-th mipmap reduction image.</para>
        /// </param>
        /// <param name="pixels">
        ///     A pointer where the pixel data will be written.
        ///     <para>Enough memory must be allocated at this location for the data to written.</para>
        /// </param>
        public static void GetCompressedTexImage(int target, int level, IntPtr pixels) => _glGetCompressedTexImage(target, level, pixels.ToPointer());

        /// <summary>
        ///     Returns a compressed texture image.
        /// </summary>
        /// <param name="target">Specifies the target to which the texture is bound.</param>
        /// <param name="level">
        ///     Specifies the level-of-detail number of the desired image.
        ///     <para>Level 0 is the base image level. Level n is the n-th mipmap reduction image.</para>
        /// </param>
        /// <param name="pixels">
        ///     A pointer where the pixel data will be written.
        ///     <para>Enough memory must be allocated at this location for the data to written.</para>
        /// </param>
        public static void GetCompressedTexImage(int target, int level, void* pixels) => _glGetCompressedTexImage(target, level, pixels);

        /// <summary>
        ///     Specify multisample coverage parameters.
        /// </summary>
        /// <param name="value">
        ///     Specify a single floating-point sample coverage value.
        ///     <para>The value is clamped to the range 0 and 1. The initial value is 1.0.</para>
        /// </param>
        /// <param name="invert">
        ///     Specify a single boolean value representing if the coverage masks should be inverted.
        ///     <para>The initial value is <c>false</c>.</para>
        /// </param>
        public static void SampleCoverage(float value, bool invert) => _glSampleCoverage(value, invert);

        /// <summary>
        ///     Delimit the boundaries of a query object.
        /// </summary>
        /// <param name="target">
        ///     Specifies the target type of query object established between <see cref="glBeginQuery" /> and the subsequent
        ///     <see cref="glEndQuery" />.
        ///     <para>
        ///         Must be one of SAMPLES_PASSED, ANY_SAMPLES_PASSED, ANY_SAMPLES_PASSED_CONSERVATIVE,
        ///         PRIMITIVES_GENERATED, TRANSFORM_FEEDBACK_PRIMITIVES_WRITTEN, or TIME_ELAPSED.
        ///     </para>
        /// </param>
        /// <param name="id">Specifies the name of a query object.</param>
        public static void BeginQuery(int target, uint id) => _glBeginQuery(target, id);

        /// <summary>
        ///     Delimit the boundaries of a query object.
        /// </summary>
        /// <param name="target">
        ///     Specifies the target type of query object to be concluded.
        ///     <para>
        ///         Must be one of SAMPLES_PASSED, ANY_SAMPLES_PASSED, ANY_SAMPLES_PASSED_CONSERVATIVE,
        ///         PRIMITIVES_GENERATED, TRANSFORM_FEEDBACK_PRIMITIVES_WRITTEN, or TIME_ELAPSED.
        ///     </para>
        /// </param>
        public static void EndQuery(int target) => _glEndQuery(target);

        /// <summary>
        ///     Determine if a name corresponds to a query object.
        /// </summary>
        /// <param name="id">Specifies a value that may be the name of a query object.</param>
        /// <returns><c>true</c> if object is a query object object, otherwise <c>false</c>.</returns>
        public static bool IsQuery(uint id) => _glIsQuery(id);

        /// <summary>
        ///     Delete named query objects.
        /// </summary>
        /// <param name="n">Specifies the number of query objects to be deleted.</param>
        /// <param name="ids">Specifies an array of query objects to be deleted.</param>
        public static void DeleteQueries(int n, /*const*/ uint* ids) => _glDeleteQueries(n, ids);

        /// <summary>
        ///     Delete named query objects.
        /// </summary>
        /// <param name="ids">An array of query objects to be deleted.</param>
        public static void DeleteQueries(uint[] ids) {
            fixed (uint* names = &ids[0]) {
                _glDeleteQueries(ids.Length, names);
            }
        }

        /// <summary>
        ///     Deletes a single query object.
        /// </summary>
        /// <param name="id">The query to delete.</param>
        public static void DeleteQuery(uint id) => _glDeleteQueries(1, &id);

        /// <summary>
        ///     Set the RGB blend equation and the alpha blend equation separately.
        /// </summary>
        /// <param name="modeRGB">
        ///     Specifies the RGB blend equation, how the red, green, and blue components of the source and
        ///     destination colors are combined.
        ///     <para>Must be FUNC_ADD, FUNC_SUBTRACT, FUNC_REVERSE_SUBTRACT, MIN, MAX.</para>
        /// </param>
        /// <param name="modeAlpha">
        ///     Specifies the alpha blend equation, how the alpha component of the source and destination
        ///     colors are combined.
        ///     <para>Must be FUNC_ADD, FUNC_SUBTRACT, FUNC_REVERSE_SUBTRACT, MIN, MAX.</para>
        /// </param>
        public static void BlendEquationSeparate(int modeRGB, int modeAlpha) => _glBlendEquationSeparate(modeRGB, modeAlpha);

        /// <summary>
        ///     Set front and/or back function and reference value for stencil testing
        /// </summary>
        /// <param name="face">
        ///     Specifies whether front and/or back stencil state is updated.
        ///     <para>Three symbolic constants are valid: FRONT, BACK, and FRONT_AND_BACK.</para>
        /// </param>
        /// <param name="func">
        ///     Specifies the test function.
        ///     <para>
        ///         Eight symbolic constants are valid: NEVER, LESS, LEQUAL, GREATER, GEQUAL, EQUAL,
        ///         NOTEQUAL, and ALWAYS. The initial value is ALWAYS
        ///     </para>
        ///     .
        /// </param>
        /// <param name="reference">
        ///     Specifies the reference value for the stencil test.
        ///     <para>
        ///         Clamped to the range [0, 2n - 1], where n is the number of bitplanes in the stencil buffer. The initial value
        ///         is 0.
        ///     </para>
        /// </param>
        /// <param name="mask">
        ///     Specifies a mask that is ANDed with both the reference value and the stored stencil value when the
        ///     test is done.
        ///     <para>The initial value is all 1's.</para>
        /// </param>
        public static void StencilFuncSeparate(int face, int func, int reference, uint mask) => _glStencilFuncSeparate(face, func, reference, mask);

        /// <summary>
        ///     Set front and/or back stencil test actions.
        /// </summary>
        /// <param name="face">
        ///     Specifies whether front and/or back stencil state is updated.
        ///     <para>Three symbolic constants are valid: FRONT, BACK, and FRONT_AND_BACK.</para>
        /// </param>
        /// <param name="sfail">
        ///     Specifies the action to take when the stencil test fails.
        ///     <para>
        ///         Eight symbolic constants are accepted: KEEP, ZERO, REPLACE, INCR, INCR_WRAP, DECR,
        ///         DECR_WRAP, and INVERT. The initial value is KEEP.
        ///     </para>
        /// </param>
        /// <param name="dpfail">
        ///     Specifies the stencil action when the stencil test passes, but the depth test fails.
        ///     <paramref name="dpfail" /> accepts the same symbolic constants as <paramref name="sfail" />.
        ///     <para>The initial value is KEEP.</para>
        /// </param>
        /// <param name="dppass">
        ///     Specifies the stencil action when both the stencil test and the depth test pass, or when the
        ///     stencil test passes and either there is no depth buffer or depth testing is not enabled. dppass​ accepts the same
        ///     symbolic constants as <paramref name="sfail"/>​.
        ///     <para>The initial value is KEEP.</para>
        /// </param>
        public static void StencilOpSeparate(int face, int sfail, int dpfail, int dppass) => _glStencilOpSeparate(face, sfail, dpfail, dppass);

        /// <summary>
        ///     Control the front and/or back writing of individual bits in the stencil planes.
        /// </summary>
        /// <param name="face">
        ///     Specifies whether the front and/or back stencil writemask is updated.
        ///     <para>Three symbolic constants are valid: FRONT, BACK, and FRONT_AND_BACK.</para>
        /// </param>
        /// <param name="mask">
        ///     Specifies a bit mask to enable and disable writing of individual bits in the stencil planes.
        ///     Initially, the mask is all 1's.
        /// </param>
        public static void StencilMaskSeparate(int face, uint mask) => _glStencilMaskSeparate(face, mask);

        /// <summary>
        ///     Instruct the GL server to block until the specified sync object becomes signaled.
        /// </summary>
        /// <param name="sync">Specifies the sync object whose status to wait on.</param>
        /// <param name="flags">A bitfield controlling the command flushing behavior.
        ///     <para>May be zero.</para>
        /// </param>
        /// <param name="timeout">
        ///     Specifies the timeout that the server should wait before continuing.
        ///     <para>Must be TIMEOUT_IGNORED.</para>
        /// </param>
        public static void WaitSync(IntPtr sync, uint flags, ulong timeout) => _glWaitSync(sync, flags, timeout);

        /// <summary>
        ///     Create a new sync object and insert it into the GL command stream.
        /// </summary>
        /// <param name="condition">Specifies the condition that must be met to set the sync object's state to signaled.
        ///     <para>Must be SYNC_GPU_COMMANDS_COMPLETE.</para>
        /// </param>
        /// <param name="flags">Specifies a bitwise combination of flags controlling the behavior of the sync object.
        ///     <para>No flags are presently defined for this operation and flags must be zero.</para>
        /// </param>
        /// <returns>The sync object reference.</returns>
        public static IntPtr FenceSync(int condition, uint flags = 0) => _glFenceSync(condition, flags);

        /// <summary>
        ///     Delete a sync object
        /// </summary>
        /// <param name="sync">The sync object to be deleted.</param>
        public static void DeleteSync(IntPtr sync) => _glDeleteSync(sync);

        /// <summary>
        ///     Determines if a name corresponds to a sync object.
        /// </summary>
        /// <param name="sync">Specifies a value that may be the name of a sync object.</param>
        /// <returns><c>true</c> if sync is currently the name of a sync object, otherwise <c>false</c>.</returns>
        public static bool IsSync(IntPtr sync) => _glIsSync(sync);

        /// <summary>
        ///     Block and wait for a sync object to become signaled.
        /// </summary>
        /// <param name="sync">The sync object whose status to wait on.</param>
        /// <param name="flags">A bitfield controlling the command flushing behavior. flags may be SYNC_FLUSH_COMMANDS_BIT.</param>
        /// <param name="timeout">
        ///     The timeout, specified in nanoseconds, for which the implementation should wait for sync to
        ///     become signaled.
        /// </param>
        /// <returns>The status, which will be ALREADY_SIGNALED, TIMEOUT_EXPIRED, CONDITION_SATISFIED, or WAIT_FAILED.</returns>
        public static int ClientWaitSync(IntPtr sync, uint flags, ulong timeout) => _glClientWaitSync(sync, flags, timeout);

        /// <summary>
        /// Return the value or values of a selected parameter.
        /// </summary>
        /// <param name="paramName">Specifies the parameter value to be returned.</param>
        /// <param name="data">Returns the value or values of the specified parameter.</param>
        public static void GetBooleanv(int paramName, bool* data) => _glGetBooleanv(paramName, data);

        /// <summary>
        /// Return the value or values of a selected parameter.
        /// </summary>
        /// <param name="paramName">Specifies the parameter value to be returned.</param>
        /// <returns>The request parameter value.</returns>

        public static bool GetBoolean(int paramName) {
            bool value;
            _glGetBooleanv(paramName, &value);
            return value;
        }

        /// <summary>
        /// Return the value or values of a selected parameter.
        /// </summary>
        /// <param name="paramName">Specifies the parameter value to be returned.</param>
        /// <param name="count">The number of values to get.</param>
        /// <returns>The request parameter value.</returns>


        public static bool[] GetBooleanv(int paramName, int count) {
            var value = new bool[count];
            fixed (bool* v = &value[0]) {
                _glGetBooleanv(paramName, v);
            }
            return value;
        }

        /// <summary>
        /// Return the value or values of a selected parameter.
        /// </summary>
        /// <param name="paramName">Specifies the parameter value to be returned.</param>
        /// <param name="data">Returns the value or values of the specified parameter.</param>
        public static void GetDoublev(int paramName, double* data) => _glGetDoublev(paramName, data);

        /// <summary>
        /// Return the value or values of a selected parameter.
        /// </summary>
        /// <param name="paramName">Specifies the parameter value to be returned.</param>
        /// <returns>The request parameter value.</returns>

        public static double GetDouble(int paramName) {
            double value;
            _glGetDoublev(paramName, &value);
            return value;
        }

        /// <summary>
        /// Return the value or values of a selected parameter.
        /// </summary>
        /// <param name="paramName">Specifies the parameter value to be returned.</param>
        /// <param name="count">The number of values to get.</param>
        /// <returns>The request parameter value.</returns>


        public static double[] GetDoublev(int paramName, int count) {
            var value = new double[count];
            fixed (double* v = &value[0]) {
                _glGetDoublev(paramName, v);
            }
            return value;
        }


        /// <summary>
        /// Return the value or values of a selected parameter.
        /// </summary>
        /// <param name="paramName">Specifies the parameter value to be returned.</param>
        /// <param name="data">Returns the value or values of the specified parameter.</param>
        public static void GetFloatv(int paramName, float* data) => _glGetFloatv(paramName, data);

        /// <summary>
        /// Return the value or values of a selected parameter.
        /// </summary>
        /// <param name="paramName">Specifies the parameter value to be returned.</param>
        /// <returns>The request parameter value.</returns>

        public static float GetFloat(int paramName) {
            float value;
            _glGetFloatv(paramName, &value);
            return value;
        }

        /// <summary>
        /// Return the value or values of a selected parameter.
        /// </summary>
        /// <param name="paramName">Specifies the parameter value to be returned.</param>
        /// <param name="count">The number of values to get.</param>
        /// <returns>The request parameter value.</returns>


        public static float[] GetFloatv(int paramName, int count) {
            var value = new float[count];
            fixed (float* v = &value[0]) {
                _glGetFloatv(paramName, v);
            }
            return value;
        }

        /// <summary>
        /// Return the value or values of a selected parameter.
        /// </summary>
        /// <param name="paramName">Specifies the parameter value to be returned.</param>
        /// <param name="data">Returns the value or values of the specified parameter.</param>
        public static void GetIntegerv(int paramName, int* data) => _glGetIntegerv(paramName, data);

        /// <summary>
        /// Return the value or values of a selected parameter.
        /// </summary>
        /// <param name="paramName">Specifies the parameter value to be returned.</param>
        /// <returns>The request parameter value.</returns>

        public static int GetInteger(int paramName) {
            int value;
            _glGetIntegerv(paramName, &value);
            return value;
        }

        /// <summary>
        /// Return the value or values of a selected parameter.
        /// </summary>
        /// <param name="paramName">Specifies the parameter value to be returned.</param>
        /// <param name="count">The number of values to get.</param>
        /// <returns>The request parameter value.</returns>


        public static int[] GetIntegerv(int paramName, int count) {
            var value = new int[count];
            fixed (int* v = &value[0]) {
                _glGetIntegerv(paramName, v);
            }
            return value;
        }

        /// <summary>
        /// Return the value or values of a selected parameter.
        /// </summary>
        /// <param name="paramName">Specifies the parameter value to be returned.</param>
        /// <param name="data">Returns the value or values of the specified parameter.</param>
        public static void GetInteger64v(int paramName, long* data) => _glGetInteger64v(paramName, data);

        /// <summary>
        /// Return the value or values of a selected parameter.
        /// </summary>
        /// <param name="paramName">Specifies the parameter value to be returned.</param>
        /// <returns>The request parameter value.</returns>

        public static long GetInteger64(int paramName) {
            long value;
            _glGetInteger64v(paramName, &value);
            return value;
        }

        /// <summary>
        /// Return the value or values of a selected parameter.
        /// </summary>
        /// <param name="paramName">Specifies the parameter value to be returned.</param>
        /// <param name="count">The number of values to get.</param>
        /// <returns>The request parameter value.</returns>


        public static long[] GetInteger64v(int paramName, int count) {
            var value = new long[count];
            fixed (long* v = &value[0]) {
                _glGetInteger64v(paramName, v);
            }
            return value;
        }

        /// <summary>
        ///     Return texture parameter values.
        /// </summary>
        /// <param name="target">Specifies the target to which the texture is bound.</param>
        /// <param name="paramName">Specifies the symbolic name of a texture parameter.</param>
        /// <param name="count">The number of values to get.</param>
        /// <remarks>Array must have enough space allocated to contain the requested value(s).</remarks>


        public static float[] GetTexParameterfv(int target, int paramName, int count) {
            var args = new float[count];
            fixed (float* a = &args[0]) {
                _glGetTexParameterfv(target, paramName, a);
            }
            return args;
        }

        /// <summary>
        ///     Return texture parameter values.
        /// </summary>
        /// <param name="target">Specifies the target to which the texture is bound.</param>
        /// <param name="paramName">Specifies the symbolic name of a texture parameter.</param>
        /// <param name="count">The number of values to get.</param>
        /// <remarks>Array must have enough space allocated to contain the requested value(s).</remarks>


        public static int[] GetTexParameteriv(int target, int paramName, int count) {
            var args = new int[count];
            fixed (int* a = &args[0]) {
                _glGetTexParameteriv(target, paramName, a);
            }
            return args;
        }

        /// <summary>
        ///     Return texture parameter values.
        /// </summary>
        /// <param name="target">Specifies the target to which the texture is bound.</param>
        /// <param name="paramName">Specifies the symbolic name of a texture parameter.</param>
        /// <param name="args">An pointer to an array where the texture parameters will be stored.</param>
        public static void GetTexParameterfv(int target, int paramName, float* args) => _glGetTexParameterfv(target, paramName, args);

        /// <summary>
        ///     Return texture parameter values.
        /// </summary>
        /// <param name="target">Specifies the target to which the texture is bound.</param>
        /// <param name="paramName">Specifies the symbolic name of a texture parameter.</param>
        /// <param name="args">An pointer to an array where the texture parameters will be stored.</param>
        public static void GetTexParameteriv(int target, int paramName, int* args) => _glGetTexParameteriv(target, paramName, args);

        /// <summary>
        ///     Return a single texture parameter value.
        /// </summary>
        /// <param name="target">Specifies the target to which the texture is bound.</param>
        /// <param name="paramName">Specifies the symbolic name of a texture parameter.</param>
        /// <returns>The value of the parameter.</returns>
        public static float GetTexParameterf(int target, int paramName) {
            float args;
            _glGetTexParameterfv(target, paramName, &args);
            return args;
        }

        /// <summary>
        ///     Return a single texture parameter value.
        /// </summary>
        /// <param name="target">Specifies the target to which the texture is bound.</param>
        /// <param name="paramName">Specifies the symbolic name of a texture parameter.</param>
        /// <returns>The value of the parameter.</returns>
        public static int GetTexParameteri(int target, int paramName) {
            int args;
            _glGetTexParameteriv(target, paramName, &args);
            return args;
        }

        /// <summary>
        ///     Return texture parameter values for a specific level of detail.
        /// </summary>
        /// <param name="target">Specifies the target to which the texture is bound.</param>
        /// <param name="level">
        ///     Specifies the level-of-detail number of the desired image.
        ///     <para>Level 0 is the base image level. Level n is the nth mipmap reduction image.</para>
        /// </param>
        /// <param name="paramName">Specifies the symbolic name of a texture parameter.</param>
        /// <param name="count">The number of values to get.</param>
        /// <remarks>Array must have enough space allocated to contain the requested value(s).</remarks>


        public static float[] GetTexLevelParameterfv(int target, int level, int paramName, int count) {
            var args = new float[count];
            fixed (float* a = &args[0]) {
                _glGetTexLevelParameterfv(target, level, paramName, a);
            }
            return args;
        }

        /// <summary>
        ///     Return texture parameter values for a specific level of detail.
        /// </summary>
        /// <param name="target">Specifies the target to which the texture is bound.</param>
        /// <param name="level">
        ///     Specifies the level-of-detail number of the desired image.
        ///     <para>Level 0 is the base image level. Level n is the nth mipmap reduction image.</para>
        /// </param>
        /// <param name="paramName">Specifies the symbolic name of a texture parameter.</param>
        /// <param name="count">The number of values to get.</param>
        /// <remarks>Array must have enough space allocated to contain the requested value(s).</remarks>


        public static int[] GetTexLevelParameteriv(int target, int level, int paramName, int count) {
            var args = new int[count];
            fixed (int* a = &args[0]) {
                _glGetTexLevelParameteriv(target, level, paramName, a);
            }
            return args;
        }

        /// <summary>
        ///     Return texture parameter values for a specific level of detail.
        /// </summary>
        /// <param name="target">Specifies the target to which the texture is bound.</param>
        /// <param name="level">
        ///     Specifies the level-of-detail number of the desired image.
        ///     <para>Level 0 is the base image level. Level n is the nth mipmap reduction image.</para>
        /// </param>
        /// <param name="paramName">Specifies the symbolic name of a texture parameter.</param>
        /// <param name="args">An pointer to an array where the texture parameters will be stored.</param>
        public static void GetTexLevelParameterfv(int target, int level, int paramName, float* args) => _glGetTexLevelParameterfv(target, level, paramName, args);

        /// <summary>
        ///     Return texture parameter values for a specific level of detail.
        /// </summary>
        /// <param name="target">Specifies the target to which the texture is bound.</param>
        /// <param name="level">
        ///     Specifies the level-of-detail number of the desired image.
        ///     <para>Level 0 is the base image level. Level n is the nth mipmap reduction image.</para>
        /// </param>
        /// <param name="paramName">Specifies the symbolic name of a texture parameter.</param>
        /// <param name="args">An pointer to an array where the texture parameters will be stored.</param>
        public static void GetTexLevelParameteriv(int target, int level, int paramName, int* args) => _glGetTexLevelParameteriv(target, level, paramName, args);

        /// <summary>
        ///     Return a single texture parameter value for a specific level of detail.
        /// </summary>
        /// <param name="target">Specifies the target to which the texture is bound.</param>
        /// <param name="level">
        ///     Specifies the level-of-detail number of the desired image.
        ///     <para>Level 0 is the base image level. Level n is the nth mipmap reduction image.</para>
        /// </param>
        /// <param name="paramName">Specifies the symbolic name of a texture parameter.</param>
        /// <returns>The value of the parameter.</returns>
        public static float GetTexLevelParameterf(int target, int level, int paramName) {
            float args;
            _glGetTexLevelParameterfv(target, level, paramName, &args);
            return args;
        }

        /// <summary>
        ///     Return a single texture parameter value for a specific level of detail.
        /// </summary>
        /// <param name="target">Specifies the target to which the texture is bound.</param>
        /// <param name="level">
        ///     Specifies the level-of-detail number of the desired image.
        ///     <para>Level 0 is the base image level. Level n is the nth mipmap reduction image.</para>
        /// </param>
        /// <param name="paramName">Specifies the symbolic name of a texture parameter.</param>
        /// <returns>The value of the parameter.</returns>
        public static int GetTexLevelParameteri(int target, int level, int paramName) {
            int args;
            _glGetTexLevelParameteriv(target, level, paramName, &args);
            return args;
        }

        /// <summary>
        /// Copy pixels into a 1D texture image.
        /// </summary>
        /// <param name="target">Specifies the target texture.<para>Must be TEXTURE_1D.</para></param>
        /// <param name="level">Specifies the level-of-detail number. Level 0 is the base image level. Level n is the nth mipmap reduction image.</param>
        /// <param name="internalFormat">Specifies the internal format of the texture.</param>
        /// <param name="x">Specify the window coordinates of the left corner of the row of pixels to be copied.</param>
        /// <param name="y">Specify the window coordinates of the left corner of the row of pixels to be copied.</param>
        /// <param name="width">Specifies the width of the texture image. Must be 0 or 2 n + 2 ⁡ border for some integer n. The height of the texture image is 1.</param>
        /// <param name="border">Specifies the width of the border. Must be either 0 or 1.</param>
        public static void CopyTexImage1D(int target, int level, int internalFormat, int x, int y, int width, int border) => _glCopyTexImage1D(target, level, internalFormat, x, y, width, border);

        /// <summary>
        /// Copy pixels into a 2D texture image.
        /// </summary>
        /// <param name="target">Specifies the target texture.<para>Must be TEXTURE_2D, TEXTURE_CUBE_MAP_POSITIVE_X, TEXTURE_CUBE_MAP_NEGATIVE_X, TEXTURE_CUBE_MAP_POSITIVE_Y, TEXTURE_CUBE_MAP_NEGATIVE_Y, TEXTURE_CUBE_MAP_POSITIVE_Z, or TEXTURE_CUBE_MAP_NEGATIVE_Z.</para></param>
        /// <param name="level">Specifies the level-of-detail number. Level 0 is the base image level. Level n is the nth mipmap reduction image.</param>
        /// <param name="internalFormat">Specifies the internal format of the texture.</param>
        /// <param name="x">Specify the window coordinates of the lower left corner of the rectangular region of pixels to be copied.</param>
        /// <param name="y">Specify the window coordinates of the lower left corner of the rectangular region of pixels to be copied.</param>
        /// <param name="width">Specifies the width of the texture image. Must be 0 or 2 n + 2 ⁡ border for some integer n.</param>
        /// <param name="height">Specifies the height of the texture image. Must be 0 or 2 n + 2 ⁡ border for some integer n.</param>
        /// <param name="border">Specifies the width of the border. Must be either 0 or 1.</param>
        public static void CopyTexImage2D(int target, int level, int internalFormat, int x, int y, int width, int height, int border) => _glCopyTexImage2D(target, level, internalFormat, x, y, width, height, border);

        /// <summary>
        ///     Copy a one-dimensional texture sub-image
        /// </summary>
        /// <param name="target">Specifies the target texture.<para>Must be TEXTURE_1D.</para></param>
        /// <param name="level">Specifies the level-of-detail number.
        ///     <para>Level 0 is the base image level. Level n is the nth mipmap reduction image.</para>
        /// </param>
        /// <param name="xOffset">Specifies the texel offset within the texture array.</param>
        /// <param name="x">Specify the window coordinates of the left corner of the row of pixels to be copied.</param>
        /// <param name="y">Specify the window coordinates of the left corner of the row of pixels to be copied.</param>
        /// <param name="width">Specifies the width of the texture sub-image.</param>
        public static void CopyTexSubImage1D(int target, int level, int xOffset, int x, int y, int width) => _glCopyTexSubImage1D(target, level, xOffset, x, y, width);

        /// <summary>
        ///     Copy a two-dimensional texture sub-image
        /// </summary>
        /// <param name="target">
        ///     Specifies the target to which the texture object is bound.
        ///     <para>
        ///         Must be TEXTURE_1D_ARRAY, TEXTURE_2D, TEXTURE_CUBE_MAP_POSITIVE_X, TEXTURE_CUBE_MAP_NEGATIVE_X,
        ///         TEXTURE_CUBE_MAP_POSITIVE_Y, TEXTURE_CUBE_MAP_NEGATIVE_Y, TEXTURE_CUBE_MAP_POSITIVE_Z,
        ///         TEXTURE_CUBE_MAP_NEGATIVE_Z, or TEXTURE_RECTANGLE.
        ///     </para>
        /// </param>
        /// <param name="level">
        ///     Specifies the level-of-detail number.
        ///     <para>Level 0 is the base image level. Level n is the nth mipmap reduction image.</para>
        /// </param>
        /// <param name="xOffset">Specifies a texel offset in the x direction within the texture array.</param>
        /// <param name="yOffset">Specifies a texel offset in the y direction within the texture array.</param>
        /// <param name="x">
        ///     Specify the window coordinates of the lower left corner on the x-axis of the rectangular region of
        ///     pixels to be copied.
        /// </param>
        /// <param name="y">
        ///     Specify the window coordinates of the lower left corner on the y-axis of the rectangular region of
        ///     pixels to be copied.
        /// </param>
        /// <param name="width">Specifies the width of the texture sub-image.</param>
        /// <param name="height">Specifies the height of the texture sub-image.</param>
        public static void CopyTexSubImage2D(int target, int level, int xOffset, int yOffset, int x, int y, int width, int height) => _glCopyTexSubImage2D(target, level, xOffset, yOffset, x, y, width, height);

        /// <summary>
        ///     Specify a one-dimensional texture sub-image.
        /// </summary>
        /// <param name="target">Specifies the target texture.<para>Must be TEXTURE_1D.</para></param>
        /// <param name="level">Specifies the level-of-detail number.
        ///     <para>Level 0 is the base image level. Level n is the nth mipmap reduction image.</para>
        /// </param>
        /// <param name="xOffset">Specifies a texel offset in the x direction within the texture array.</param>
        /// <param name="width">Specifies the width of the texture sub-image.</param>
        /// <param name="format">Specifies the format of the pixel data.
        ///     <para>Must be RED, RG, RGB, BGR, RGBA, DEPTH_COMPONENT, or STENCIL_INDEX.</para>
        /// </param>
        /// <param name="type">Specifies the data type of the pixel data.</param>
        /// <param name="pixels">Specifies a pointer to the image data in memory.</param>
        public static void TexSubImage1D(int target, int level, int xOffset, int width, int format, int type, IntPtr pixels) => _glTexSubImage1D(target, level, xOffset, width, format, type, pixels.ToPointer());

        /// <summary>
        ///     Specify a two-dimensional texture sub-image.
        /// </summary>
        /// <param name="target">Specifies the target to which the texture is bound.</param>
        /// <param name="level">Specifies the level-of-detail number.
        ///     <para>Level 0 is the base image level. Level n is the nth mipmap reduction image.</para>
        /// </param>
        /// <param name="xOffset">Specifies a texel offset in the x direction within the texture array.</param>
        /// <param name="yOffset">Specifies a texel offset in the y direction within the texture array.</param>
        /// <param name="width">Specifies the width of the texture sub-image.</param>
        /// <param name="height">Specifies the height of the texture sub-image.</param>
        /// <param name="format">Specifies the format of the pixel data.
        ///     <para>Must be RED, RG, RGB, BGR, RGBA, DEPTH_COMPONENT, or STENCIL_INDEX.</para>
        /// </param>
        /// <param name="type">Specifies the data type of the pixel data.</param>
        /// <param name="pixels">Specifies a pointer to the image data in memory.</param>
        public static void TexSubImage2D(int target, int level, int xOffset, int yOffset, int width, int height, int format, int type, IntPtr pixels) => _glTexSubImage2D(target, level, xOffset, yOffset, width, height, format, type, pixels.ToPointer());

        /// <summary>
        ///     Specify a three-dimensional texture sub-image.
        /// </summary>
        /// <param name="target">Specifies the target to which the texture is bound.
        ///     <para>Must be TEXTURE_3D, TEXTURE_2D_ARRAY or TEXTURE_CUBE_MAP_ARRAY.</para>
        /// </param>
        /// <param name="level">Specifies the level-of-detail number.
        ///     <para>Level 0 is the base image level. Level n is the nth mipmap reduction image.</para>
        /// </param>
        /// <param name="xOffset">Specifies a texel offset in the x direction within the texture array.</param>
        /// <param name="yOffset">Specifies a texel offset in the y direction within the texture array.</param>
        /// <param name="zOffset">Specifies a texel offset in the z direction within the texture array.</param>
        /// <param name="width">Specifies the width of the texture sub-image.</param>
        /// <param name="height">Specifies the height of the texture sub-image.</param>
        /// <param name="depth">Specifies the depth of the texture sub-image.</param>
        /// <param name="format">Specifies the format of the pixel data.
        ///     <para>Must be RED, RG, RGB, BGR, RGBA, DEPTH_COMPONENT, or STENCIL_INDEX.</para>
        /// </param>
        /// <param name="type">Specifies the data type of the pixel data.</param>
        /// <param name="pixels">Specifies a pointer to the image data in memory.</param>
        public static void TexSubImage3D(int target, int level, int xOffset, int yOffset, int zOffset, int width,
            int height, int depth, int format, int type, IntPtr pixels) => _glTexSubImage3D(target, level,
            xOffset, yOffset, zOffset, width, height, depth, format, type, pixels.ToPointer());

        /// <summary>
        ///     Specify a one-dimensional texture sub-image.
        /// </summary>
        /// <param name="target">Specifies the target texture.<para>Must be TEXTURE_1D.</para></param>
        /// <param name="level">Specifies the level-of-detail number.
        ///     <para>Level 0 is the base image level. Level n is the nth mipmap reduction image.</para>
        /// </param>
        /// <param name="xOffset">Specifies a texel offset in the x direction within the texture array.</param>
        /// <param name="width">Specifies the width of the texture sub-image.</param>
        /// <param name="format">Specifies the format of the pixel data.
        ///     <para>Must be RED, RG, RGB, BGR, RGBA, DEPTH_COMPONENT, or STENCIL_INDEX.</para>
        /// </param>
        /// <param name="type">Specifies the data type of the pixel data.</param>
        /// <param name="pixels">Specifies a pointer to the image data in memory.</param>
        public static void TexSubImage1D(int target, int level, int xOffset, int width, int format, int type, /*const*/ void* pixels) => _glTexSubImage1D(target, level, xOffset, width, format, type, pixels);

        /// <summary>
        ///     Specify a two-dimensional texture sub-image.
        /// </summary>
        /// <param name="target">Specifies the target to which the texture is bound.</param>
        /// <param name="level">Specifies the level-of-detail number.
        ///     <para>Level 0 is the base image level. Level n is the nth mipmap reduction image.</para>
        /// </param>
        /// <param name="xOffset">Specifies a texel offset in the x direction within the texture array.</param>
        /// <param name="yOffset">Specifies a texel offset in the y direction within the texture array.</param>
        /// <param name="width">Specifies the width of the texture sub-image.</param>
        /// <param name="height">Specifies the height of the texture sub-image.</param>
        /// <param name="format">Specifies the format of the pixel data.
        ///     <para>Must be RED, RG, RGB, BGR, RGBA, DEPTH_COMPONENT, or STENCIL_INDEX.</para>
        /// </param>
        /// <param name="type">Specifies the data type of the pixel data.</param>
        /// <param name="pixels">Specifies a pointer to the image data in memory.</param>
        public static void TexSubImage2D(int target, int level, int xOffset, int yOffset, int width, int height, int format, int type, /*const*/ void* pixels) => _glTexSubImage2D(target, level, xOffset, yOffset, width, height, format, type, pixels);

        /// <summary>
        ///     Specify a three-dimensional texture sub-image.
        /// </summary>
        /// <param name="target">Specifies the target to which the texture is bound.
        ///     <para>Must be TEXTURE_3D, TEXTURE_2D_ARRAY or TEXTURE_CUBE_MAP_ARRAY.</para>
        /// </param>
        /// <param name="level">Specifies the level-of-detail number.
        ///     <para>Level 0 is the base image level. Level n is the nth mipmap reduction image.</para>
        /// </param>
        /// <param name="xOffset">Specifies a texel offset in the x direction within the texture array.</param>
        /// <param name="yOffset">Specifies a texel offset in the y direction within the texture array.</param>
        /// <param name="zOffset">Specifies a texel offset in the z direction within the texture array.</param>
        /// <param name="width">Specifies the width of the texture sub-image.</param>
        /// <param name="height">Specifies the height of the texture sub-image.</param>
        /// <param name="depth">Specifies the depth of the texture sub-image.</param>
        /// <param name="format">Specifies the format of the pixel data.
        ///     <para>Must be RED, RG, RGB, BGR, RGBA, DEPTH_COMPONENT, or STENCIL_INDEX.</para>
        /// </param>
        /// <param name="type">Specifies the data type of the pixel data.</param>
        /// <param name="pixels">Specifies a pointer to the image data in memory.</param>
        public static void TexSubImage3D(int target, int level, int xOffset, int yOffset, int zOffset, int width, int height, int depth, int format, int type, /*const*/ void* pixels) => _glTexSubImage3D(target, level, xOffset, yOffset, zOffset, width, height, depth, format, type, pixels);

        /// <summary>
        ///     Render primitives from array data.
        /// </summary>
        /// <param name="mode">
        ///     Specifies what kind of primitives to render.
        ///     <para>
        ///         POINTS, LINE_STRIP, LINE_LOOP, LINES, LINE_STRIP_ADJACENCY, LINES_ADJACENCY,
        ///         TRIANGLE_STRIP, TRIANGLE_FAN, TRIANGLES, TRIANGLE_STRIP_ADJACENCY, TRIANGLES_ADJACENCY and
        ///         PATCHES are accepted.
        ///     </para>
        /// </param>
        /// <param name="start">Specifies the minimum array index contained in <paramref name="indices" />.</param>
        /// <param name="end">Specifies the maximum array index contained in <paramref name="indices" />.</param>
        /// <param name="count">Specifies the number of elements to be rendered.</param>
        /// <param name="indices">The source indices.</param>
        public static void DrawRangeElements(int mode, uint start, uint end, int count, byte[] indices) {
            fixed (void* i = &indices[0]) {
                _glDrawRangeElements(mode, start, end, count, UNSIGNED_BYTE, i);
            }
        }

        /// <summary>
        ///     Render primitives from array data.
        /// </summary>
        /// <param name="mode">
        ///     Specifies what kind of primitives to render.
        ///     <para>
        ///         POINTS, LINE_STRIP, LINE_LOOP, LINES, LINE_STRIP_ADJACENCY, LINES_ADJACENCY,
        ///         TRIANGLE_STRIP, TRIANGLE_FAN, TRIANGLES, TRIANGLE_STRIP_ADJACENCY, TRIANGLES_ADJACENCY and
        ///         PATCHES are accepted.
        ///     </para>
        /// </param>
        /// <param name="start">Specifies the minimum array index contained in <paramref name="indices" />.</param>
        /// <param name="end">Specifies the maximum array index contained in <paramref name="indices" />.</param>
        /// <param name="count">Specifies the number of elements to be rendered.</param>
        /// <param name="indices">The source indices.</param>
        public static void DrawRangeElements(int mode, uint start, uint end, int count, ushort[] indices) {
            fixed (void* i = &indices[0]) {
                _glDrawRangeElements(mode, start, end, count, UNSIGNED_SHORT, i);
            }
        }

        /// <summary>
        ///     Render primitives from array data.
        /// </summary>
        /// <param name="mode">
        ///     Specifies what kind of primitives to render.
        ///     <para>
        ///         POINTS, LINE_STRIP, LINE_LOOP, LINES, LINE_STRIP_ADJACENCY, LINES_ADJACENCY,
        ///         TRIANGLE_STRIP, TRIANGLE_FAN, TRIANGLES, TRIANGLE_STRIP_ADJACENCY, TRIANGLES_ADJACENCY and
        ///         PATCHES are accepted.
        ///     </para>
        /// </param>
        /// <param name="start">Specifies the minimum array index contained in <paramref name="indices" />.</param>
        /// <param name="end">Specifies the maximum array index contained in <paramref name="indices" />.</param>
        /// <param name="count">Specifies the number of elements to be rendered.</param>
        /// <param name="indices">The source indices.</param>
        public static void DrawRangeElements(int mode, uint start, uint end, int count, uint[] indices) {
            fixed (void* i = &indices[0]) {
                _glDrawRangeElements(mode, start, end, count, UNSIGNED_INT, i);
            }
        }

        /// <summary>
        ///     Render primitives from array data.
        /// </summary>
        /// <param name="mode">
        ///     Specifies what kind of primitives to render.
        ///     <para>
        ///         POINTS, LINE_STRIP, LINE_LOOP, LINES, LINE_STRIP_ADJACENCY, LINES_ADJACENCY,
        ///         TRIANGLE_STRIP, TRIANGLE_FAN, TRIANGLES, TRIANGLE_STRIP_ADJACENCY, TRIANGLES_ADJACENCY and
        ///         PATCHES are accepted.
        ///     </para>
        /// </param>
        /// <param name="start">Specifies the minimum array index contained in <paramref name="indices" />.</param>
        /// <param name="end">Specifies the maximum array index contained in <paramref name="indices" />.</param>
        /// <param name="count">Specifies the number of elements to be rendered.</param>
        /// <param name="type">
        ///     Specifies the type of the values in indices.
        ///     <para>Must be one of UNSIGNED_BYTE, UNSIGNED_SHORT, or UNSIGNED_INT.</para>
        /// </param>
        /// <param name="indices">Specifies a pointer to the location where the indices are stored.</param>
        public static void DrawRangeElements(int mode, uint start, uint end, int count, int type, /*const*/void* indices) => _glDrawRangeElements(mode, start, end, count, type, indices);

        /// <summary>
        ///     Map all of a buffer object's data store into the client's address space.
        /// </summary>
        /// <param name="target">Specifies the target to which the buffer object is bound.</param>
        /// <param name="access">
        ///     Specifies the access policy, indicating whether it will be possible to read from, write to, or
        ///     both read from and write to the buffer object's mapped data store.
        ///     <para>The symbolic constant must be READ_ONLY, WRITE_ONLY, or READ_WRITE.</para>
        /// </param>
        /// <returns>A pointer to the beginning of the mapped range.</returns>
        public static IntPtr MapBuffer(int target, int access) => new(_glMapBuffer(target, access));

        /// <summary>
        ///     Release the mapping of a buffer object's data store into the client's address space.
        /// </summary>
        /// <param name="target">Specifies the target to which the buffer object is bound.</param>
        /// <returns><c>true</c> unless the data store contents have become corrupt during the time the data store was mapped.</returns>
        public static bool UnmapBuffer(int target) => _glUnmapBuffer(target);

        /// <summary>
        ///     Copy a three-dimensional texture sub-image.
        /// </summary>
        /// <param name="target">
        ///     Specifies the target to which the texture object is bound.
        ///     <para>Must be TEXTURE_3D, TEXTURE_2D_ARRAY or TEXTURE_CUBE_MAP_ARRAY.</para>
        /// </param>
        /// <param name="level">
        ///     Specifies the level-of-detail number. Level 0 is the base image level. Level n is the nth mipmap
        ///     reduction image.
        /// </param>
        /// <param name="xOffset">Specifies a texel offset in the x direction within the texture array.</param>
        /// <param name="yOffset">Specifies a texel offset in the y direction within the texture array.</param>
        /// <param name="zOffset">Specifies a texel offset in the z direction within the texture array.</param>
        /// <param name="x">
        ///     Specify the window coordinates of the lower left corner of the rectangular region of pixels to be
        ///     copied.
        /// </param>
        /// <param name="y">
        ///     Specify the window coordinates of the lower left corner of the rectangular region of pixels to be
        ///     copied.
        /// </param>
        /// <param name="width">Specifies the width of the texture sub-image.</param>
        /// <param name="height">Specifies the height of the texture sub-image.</param>
        public static void CopyTexSubImage3D(int target, int level, int xOffset, int yOffset, int zOffset, int x, int y, int width, int height) => _glCopyTexSubImage3D(target, level, xOffset, yOffset, zOffset, x, y, width, height);

        /// <summary>
        /// Specify a three-dimensional texture image in a compressed format.
        /// </summary>
        /// <param name="target">Specifies the target texture.<para>Must be TEXTURE_3D, PROXY_TEXTURE_3D, TEXTURE_2D_ARRAY or PROXY_TEXTURE_2D_ARRAY.</para></param>
        /// <param name="level">Specifies the level-of-detail number. Level 0 is the base image level. Level n is the nth mipmap reduction image.</param>
        /// <param name="internalFormat">Specifies the format of the compressed image data stored at address data.</param>
        /// <param name="width">Specifies the width of the texture image.<para>All implementations support 3D texture images that are at least 16 texels wide.</para></param>
        /// <param name="height">Specifies the height of the texture image.<para>All implementations support 3D texture images that are at least 16 texels high.</para></param>
        /// <param name="depth">Specifies the depth of the texture image.<para>All implementations support 3D texture images that are at least 16 texels deep.</para></param>
        /// <param name="border">This value must be 0.</param>
        /// <param name="imageSize">Specifies the number of unsigned bytes of image data starting at the address specified by <paramref name="data"/>.</param>
        /// <param name="data">Specifies a pointer to the compressed image data in memory.</param>
        public static void CompressedTexImage3D(int target, int level, int internalFormat, int width, int height, int depth, int border, int imageSize, IntPtr data) => _glCompressedTexImage3D(target, level, internalFormat, width, height, depth, border, imageSize, data.ToPointer());

        /// <summary>
        /// Specify a two-dimensional texture image in a compressed format
        /// </summary>
        /// <param name="target">Specifies the target texture.</param>
        /// <param name="level">Specifies the level-of-detail number. Level 0 is the base image level. Level n is the nth mipmap reduction image.</param>
        /// <param name="internalFormat">Specifies the format of the compressed image data stored at address data.</param>
        /// <param name="width">Specifies the width of the texture image.<para>All implementations support 2D texture and cube map texture images that are at least 16384 texels wide.</para></param>
        /// <param name="height">Specifies the height of the texture image.<para>All implementations support 2D texture and cube map texture images that are at least 16384 texels high.</para></param>
        /// <param name="border">This value must be 0.</param>
        /// <param name="imageSize">Specifies the number of unsigned bytes of image data starting at the address specified by <paramref name="data"/>.</param>
        /// <param name="data">Specifies a pointer to the compressed image data in memory.</param>
        public static void CompressedTexImage2D(int target, int level, int internalFormat, int width, int height, int border, int imageSize, IntPtr data) => _glCompressedTexImage2D(target, level, internalFormat, width, height, border, imageSize, data.ToPointer());

        /// <summary>
        /// Specify a one-dimensional texture image in a compressed format.
        /// </summary>
        /// <param name="target">Specifies the target texture.<para>Must be TEXTURE_1D or PROXY_TEXTURE_1D.</para></param>
        /// <param name="level">Specifies the level-of-detail number. Level 0 is the base image level. Level n is the nth mipmap reduction image.</param>
        /// <param name="internalFormat">Specifies the format of the compressed image data stored at address data.</param>
        /// <param name="width">Specifies the width of the texture image.<para>All implementations support texture images that are at least 64 texels wide. The height of the 1D texture image is 1.</para></param>
        /// <param name="border">This value must be 0.</param>
        /// <param name="imageSize">Specifies the number of unsigned bytes of image data starting at the address specified by <paramref name="data"/>.</param>
        /// <param name="data">Specifies a pointer to the compressed image data in memory.</param>
        public static void CompressedTexImage1D(int target, int level, int internalFormat, int width, int border, int imageSize, IntPtr data) => _glCompressedTexImage1D(target, level, internalFormat, width, border, imageSize, data.ToPointer());

        /// <summary>
        /// Specify a three-dimensional texture image in a compressed format.
        /// </summary>
        /// <param name="target">Specifies the target texture.<para>Must be TEXTURE_3D, PROXY_TEXTURE_3D, TEXTURE_2D_ARRAY or PROXY_TEXTURE_2D_ARRAY.</para></param>
        /// <param name="level">Specifies the level-of-detail number. Level 0 is the base image level. Level n is the nth mipmap reduction image.</param>
        /// <param name="internalFormat">Specifies the format of the compressed image data stored at address data.</param>
        /// <param name="width">Specifies the width of the texture image.<para>All implementations support 3D texture images that are at least 16 texels wide.</para></param>
        /// <param name="height">Specifies the height of the texture image.<para>All implementations support 3D texture images that are at least 16 texels high.</para></param>
        /// <param name="depth">Specifies the depth of the texture image.<para>All implementations support 3D texture images that are at least 16 texels deep.</para></param>
        /// <param name="border">This value must be 0.</param>
        /// <param name="imageSize">Specifies the number of unsigned bytes of image data starting at the address specified by <paramref name="data"/>.</param>
        /// <param name="data">Specifies a pointer to the compressed image data in memory.</param>
        public static void CompressedTexImage3D(int target, int level, int internalFormat, int width, int height, int depth, int border, int imageSize, /*const*/ void* data) => _glCompressedTexImage3D(target, level, internalFormat, width, height, depth, border, imageSize, data);

        /// <summary>
        /// Specify a two-dimensional texture image in a compressed format
        /// </summary>
        /// <param name="target">Specifies the target texture.</param>
        /// <param name="level">Specifies the level-of-detail number. Level 0 is the base image level. Level n is the nth mipmap reduction image.</param>
        /// <param name="internalFormat">Specifies the format of the compressed image data stored at address data.</param>
        /// <param name="width">Specifies the width of the texture image.<para>All implementations support 2D texture and cube map texture images that are at least 16384 texels wide.</para></param>
        /// <param name="height">Specifies the height of the texture image.<para>All implementations support 2D texture and cube map texture images that are at least 16384 texels high.</para></param>
        /// <param name="border">This value must be 0.</param>
        /// <param name="imageSize">Specifies the number of unsigned bytes of image data starting at the address specified by <paramref name="data"/>.</param>
        /// <param name="data">Specifies a pointer to the compressed image data in memory.</param>
        public static void CompressedTexImage2D(int target, int level, int internalFormat, int width, int height, int border, int imageSize, /*const*/ void* data) => _glCompressedTexImage2D(target, level, internalFormat, width, height, border, imageSize, data);

        /// <summary>
        /// Specify a one-dimensional texture image in a compressed format.
        /// </summary>
        /// <param name="target">Specifies the target texture.<para>Must be TEXTURE_1D or PROXY_TEXTURE_1D.</para></param>
        /// <param name="level">Specifies the level-of-detail number. Level 0 is the base image level. Level n is the nth mipmap reduction image.</param>
        /// <param name="internalFormat">Specifies the format of the compressed image data stored at address data.</param>
        /// <param name="width">Specifies the width of the texture image.<para>All implementations support texture images that are at least 64 texels wide. The height of the 1D texture image is 1.</para></param>
        /// <param name="border">This value must be 0.</param>
        /// <param name="imageSize">Specifies the number of unsigned bytes of image data starting at the address specified by <paramref name="data"/>.</param>
        /// <param name="data">Specifies a pointer to the compressed image data in memory.</param>
        public static void CompressedTexImage1D(int target, int level, int internalFormat, int width, int border, int imageSize, /*const*/ void* data) => _glCompressedTexImage1D(target, level, internalFormat, width, border, imageSize, data);

        /// <summary>
        /// Specify a three-dimensional texture sub-image in a compressed format.
        /// </summary>
        /// <param name="target">Specifies the target to which the texture is bound.<para>Must be TEXTURE_2D_ARRAY, TEXTURE_3D, or TEXTURE_CUBE_MAP_ARRAY.</para></param>
        /// <param name="level">Specifies the level-of-detail number. Level 0 is the base image level. Level n is the nth mipmap reduction image.</param>
        /// <param name="xOffset">Specifies a texel offset in the x direction within the texture array.</param>
        /// <param name="yOffset">Specifies a texel offset in the y direction within the texture array.</param>
        /// <param name="zOffset">Specifies a texel offset in the y direction within the texture array.</param>
        /// <param name="width">Specifies the width of the texture sub-image.</param>
        /// <param name="height">Specifies the height of the texture sub-image.</param>
        /// <param name="depth">Specifies the depth of the texture sub-image.</param>
        /// <param name="format">Specifies the format of the compressed image data stored at address <paramref name="data"/>.</param>
        /// <param name="imageSize">Specifies the number of unsigned bytes of image data starting at the address specified by <paramref name="data"/>.</param>
        /// <param name="data">Specifies a pointer to the compressed image data in memory.</param>
        public static void CompressedTexSubImage3D(int target, int level, int xOffset, int yOffset, int zOffset, int width, int height, int depth, int format, int imageSize, IntPtr data) => _glCompressedTexSubImage3D(target, level, xOffset, yOffset, zOffset, width, height, depth, format, imageSize, data.ToPointer());

        /// <summary>
        /// Specify a two-dimensional texture sub-image in a compressed format.
        /// </summary>
        /// <param name="target">Specifies the target to which the texture is bound.</param>
        /// <param name="level">Specifies the level-of-detail number. Level 0 is the base image level. Level n is the nth mipmap reduction image.</param>
        /// <param name="xOffset">Specifies a texel offset in the x direction within the texture array.</param>
        /// <param name="yOffset">Specifies a texel offset in the y direction within the texture array.</param>
        /// <param name="width">Specifies the width of the texture sub-image.</param>
        /// <param name="height">Specifies the height of the texture sub-image.</param>
        /// <param name="format">Specifies the format of the compressed image data stored at address <paramref name="data"/>.</param>
        /// <param name="imageSize">Specifies the number of unsigned bytes of image data starting at the address specified by <paramref name="data"/>.</param>
        /// <param name="data">Specifies a pointer to the compressed image data in memory.</param>
        public static void CompressedTexSubImage2D(int target, int level, int xOffset, int yOffset, int width, int height, int format, int imageSize, IntPtr data) => _glCompressedTexSubImage2D(target, level, xOffset, yOffset, width, height, format, imageSize, data.ToPointer());

        /// <summary>
        /// Specify a one-dimensional texture sub-image in a compressed format.
        /// </summary>
        /// <param name="target">Specifies the target texture.<para>Must be TEXTURE_1D or PROXY_TEXTURE_1D.</para></param>
        /// <param name="level">Specifies the level-of-detail number. Level 0 is the base image level. Level n is the nth mipmap reduction image.</param>
        /// <param name="xOffset">Specifies a texel offset in the x direction within the texture array.</param>
        /// <param name="width">Specifies the width of the texture sub-image.</param>
        /// <param name="format">Specifies the format of the compressed image data stored at address <paramref name="data"/>.</param>
        /// <param name="imageSize">Specifies the number of unsigned bytes of image data starting at the address specified by <paramref name="data"/>.</param>
        /// <param name="data">Specifies a pointer to the compressed image data in memory.</param>
        public static void CompressedTexSubImage1D(int target, int level, int xOffset, int width, int format, int imageSize, IntPtr data) => _glCompressedTexSubImage1D(target, level, xOffset, width, format, imageSize, data.ToPointer());

        /// <summary>
        /// Specify a three-dimensional texture sub-image in a compressed format.
        /// </summary>
        /// <param name="target">Specifies the target to which the texture is bound.<para>Must be TEXTURE_2D_ARRAY, TEXTURE_3D, or TEXTURE_CUBE_MAP_ARRAY.</para></param>
        /// <param name="level">Specifies the level-of-detail number. Level 0 is the base image level. Level n is the nth mipmap reduction image.</param>
        /// <param name="xOffset">Specifies a texel offset in the x direction within the texture array.</param>
        /// <param name="yOffset">Specifies a texel offset in the y direction within the texture array.</param>
        /// <param name="zOffset">Specifies a texel offset in the y direction within the texture array.</param>
        /// <param name="width">Specifies the width of the texture sub-image.</param>
        /// <param name="height">Specifies the height of the texture sub-image.</param>
        /// <param name="depth">Specifies the depth of the texture sub-image.</param>
        /// <param name="format">Specifies the format of the compressed image data stored at address <paramref name="data"/>.</param>
        /// <param name="imageSize">Specifies the number of unsigned bytes of image data starting at the address specified by <paramref name="data"/>.</param>
        /// <param name="data">Specifies a pointer to the compressed image data in memory.</param>
        public static void CompressedTexSubImage3D(int target, int level, int xOffset, int yOffset, int zOffset, int width, int height, int depth, int format, int imageSize, /*const*/ void* data) => _glCompressedTexSubImage3D(target, level, xOffset, yOffset, zOffset, width, height, depth, format, imageSize, data);

        /// <summary>
        /// Specify a two-dimensional texture sub-image in a compressed format.
        /// </summary>
        /// <param name="target">Specifies the target to which the texture is bound.</param>
        /// <param name="level">Specifies the level-of-detail number. Level 0 is the base image level. Level n is the nth mipmap reduction image.</param>
        /// <param name="xOffset">Specifies a texel offset in the x direction within the texture array.</param>
        /// <param name="yOffset">Specifies a texel offset in the y direction within the texture array.</param>
        /// <param name="width">Specifies the width of the texture sub-image.</param>
        /// <param name="height">Specifies the height of the texture sub-image.</param>
        /// <param name="format">Specifies the format of the compressed image data stored at address <paramref name="data"/>.</param>
        /// <param name="imageSize">Specifies the number of unsigned bytes of image data starting at the address specified by <paramref name="data"/>.</param>
        /// <param name="data">Specifies a pointer to the compressed image data in memory.</param>
        public static void CompressedTexSubImage2D(int target, int level, int xOffset, int yOffset, int width, int height, int format, int imageSize, /*const*/ void* data) => _glCompressedTexSubImage2D(target, level, xOffset, yOffset, width, height, format, imageSize, data);

        /// <summary>
        /// Specify a one-dimensional texture sub-image in a compressed format.
        /// </summary>
        /// <param name="target">Specifies the target texture.<para>Must be TEXTURE_1D or PROXY_TEXTURE_1D.</para></param>
        /// <param name="level">Specifies the level-of-detail number. Level 0 is the base image level. Level n is the nth mipmap reduction image.</param>
        /// <param name="xOffset">Specifies a texel offset in the x direction within the texture array.</param>
        /// <param name="width">Specifies the width of the texture sub-image.</param>
        /// <param name="format">Specifies the format of the compressed image data stored at address <paramref name="data"/>.</param>
        /// <param name="imageSize">Specifies the number of unsigned bytes of image data starting at the address specified by <paramref name="data"/>.</param>
        /// <param name="data">Specifies a pointer to the compressed image data in memory.</param>
        public static void CompressedTexSubImage1D(int target, int level, int xOffset, int width, int format, int imageSize, /*const*/ void* data) => _glCompressedTexSubImage1D(target, level, xOffset, width, format, imageSize, data);

        /// <summary>
        ///     Specify pixel arithmetic for RGB and alpha components separately.
        /// </summary>
        /// <param name="sFactorRgb">
        ///     Specifies how the red, green, and blue blending factors are computed.
        ///     <para>The initial value is ONE.</para>
        /// </param>
        /// <param name="dFactorRgb">
        ///     Specifies how the red, green, and blue destination blending factors are computed.
        ///     <para>The initial value is ZERO.</para>
        /// </param>
        /// <param name="sFactorAlpha">
        ///     Specified how the alpha source blending factor is computed.
        ///     <para>The initial value is ONE.</para>
        /// </param>
        /// <param name="dFactorAlpha">
        ///     Specified how the alpha destination blending factor is computed.
        ///     <para>The initial value is ZERO.</para>
        /// </param>
        public static void BlendFuncSeparate(int sFactorRgb, int dFactorRgb, int sFactorAlpha, int dFactorAlpha) => _glBlendFuncSeparate(sFactorRgb, dFactorRgb, sFactorAlpha, dFactorAlpha);

        /// <summary>
        ///     Delete named framebuffer objects.
        /// </summary>
        /// <param name="n">Specifies the number of framebuffer objects to be deleted.</param>
        /// <param name="buffers">Specifies an array of framebuffer objects to be deleted.</param>
        public static void DeleteFramebuffers(int n, /*const*/ uint* buffers) => _glDeleteFramebuffers(n, buffers);

        /// <summary>
        ///     Delete named framebuffer objects.
        /// </summary>
        /// <param name="buffers">Specifies an array of framebuffer objects to be deleted.</param>
        public static void DeleteFramebuffers(uint[] buffers) {
            if (buffers is null)
                return;
            fixed (uint* ids = &buffers[0]) {
                _glDeleteFramebuffers(buffers.Length, ids);
            }
        }

        /// <summary>
        ///     Deletes a single framebuffer object.
        /// </summary>
        /// <param name="buffer">A framebuffer to be deleted.</param>
        public static void DeleteFramebuffer(uint buffer) => _glDeleteFramebuffers(1, &buffer);

        /// <summary>
        ///     Bind a named buffer object.
        /// </summary>
        /// <param name="target">Specifies the target to which the buffer object is bound.</param>
        /// <param name="buffer">Specifies the name of a buffer object.</param>
        public static void BindBuffer(int target, uint buffer) => _glBindBuffer(target, buffer);

        /// <summary>
        /// Bind a named framebuffer object.
        /// </summary>
        /// <param name="framebuffer">Specifies the name of a framebuffer object.</param>
        public static void BindFramebuffer(uint framebuffer) => _glBindFramebuffer(FRAMEBUFFER, framebuffer);

        /// <summary>
        /// Bind a named renderbuffer object.
        /// </summary>
        /// <param name="renderbuffer">Specifies the name of a renderbuffer object.</param>
        public static void BindRenderbuffer(uint renderbuffer) => _glBindRenderbuffer(RENDERBUFFER, renderbuffer);

        /// <summary>
        ///     Deletes a single buffer object.
        /// </summary>
        /// <param name="buffer">A buffer to be deleted.</param>
        public static void DeleteBuffer(uint buffer) => _glDeleteBuffers(1, &buffer);

        /// <summary>
        ///     Deletes a single renderbuffer object.
        /// </summary>
        /// <param name="renderbuffer">A renderbuffer to be deleted.</param>
        public static void DeleteRenderbuffer(uint renderbuffer) => _glDeleteRenderbuffers(1, &renderbuffer);

        /// <summary>
        ///     Delete named renderbuffer objects.
        /// </summary>
        /// <param name="n">Specifies the number of renderbuffer objects to be deleted.</param>
        /// <param name="buffers">Specifies an array of renderbuffer objects to be deleted.</param>
        public static void DeleteRenderbuffers(int n, /*const*/ uint* buffers) => _glDeleteRenderbuffers(n, buffers);

        /// <summary>
        ///     Delete named renderbuffer objects.
        /// </summary>
        /// <param name="buffers">Specifies an array of renderbuffer objects to be deleted.</param>
        public static void DeleteRenderbuffers(uint[] buffers) {
            if (buffers is null)
                return;
            fixed (uint* ids = &buffers[0]) {
                _glDeleteRenderbuffers(buffers.Length, ids);
            }
        }

        /// <summary>
        ///     Delete named buffer objects.
        /// </summary>
        /// <param name="n">Specifies the number of buffer objects to be deleted.</param>
        /// <param name="buffers">Specifies an array of buffer objects to be deleted.</param>
        public static void DeleteBuffers(int n, /*const*/ uint* buffers) => _glDeleteBuffers(n, buffers);

        /// <summary>
        ///     Delete named buffer objects.
        /// </summary>
        /// <param name="buffers">Specifies an array of buffer objects to be deleted.</param>
        public static void DeleteBuffers(uint[] buffers) {
            if (buffers is null)
                return;
            fixed (uint* ids = &buffers[0]) {
                _glDeleteBuffers(buffers.Length, ids);
            }
        }

        /// <summary>
        ///     Generate framebuffer object names.
        /// </summary>
        /// <param name="n">Specifies the number of framebuffer object names to be generated.</param>
        /// <param name="buffers">Specifies an array in which the generated framebuffer object names are stored.</param>
        public static void GenFramebuffers(int n, uint* buffers) => _glGenFramebuffers(n, buffers);

        /// <summary>
        ///     Generate a single framebuffer object name.
        /// </summary>
        /// <returns>The framebuffer object name.</returns>

        public static uint GenFramebuffer() {
            uint id;
            _glGenFramebuffers(1, &id);
            return id;
        }

        /// <summary>
        ///     Generate framebuffer object names.
        /// </summary>
        /// <param name="n">Specifies the number of framebuffer object names to be generated.</param>
        /// <returns>An array of generated framebuffer object names.</returns>


        public static uint[] GenFramebuffers(int n) {
            var buffers = new uint[n];
            fixed (uint* ids = &buffers[0]) {
                _glGenFramebuffers(n, ids);
            }
            return buffers;
        }

        /// <summary>
        ///     Generate renderbuffer object names.
        /// </summary>
        /// <param name="n">Specifies the number of renderbuffer object names to be generated.</param>
        /// <param name="buffers">Specifies an array in which the generated renderbuffer object names are stored.</param>
        public static void GenRenderbuffers(int n, uint* buffers) => _glGenRenderbuffers(n, buffers);

        /// <summary>
        ///     Generate a single renderbuffer object name.
        /// </summary>
        /// <returns>The renderbuffer object name.</returns>

        public static uint GenRenderbuffer() {
            uint id;
            _glGenRenderbuffers(1, &id);
            return id;
        }

        /// <summary>
        ///     Generate renderbuffer object names.
        /// </summary>
        /// <param name="n">Specifies the number of renderbuffer object names to be generated.</param>
        /// <returns>An array of generated renderbuffer object names.</returns>


        public static uint[] GenRenderbuffers(int n) {
            var buffers = new uint[n];
            fixed (uint* ids = &buffers[0]) {
                _glGenRenderbuffers(n, ids);
            }
            return buffers;
        }

        /// <summary>
        ///     Generate buffer object names.
        /// </summary>
        /// <param name="n">Specifies the number of buffer object names to be generated.</param>
        /// <param name="buffers">Specifies an array in which the generated buffer object names are stored.</param>
        public static void GenBuffers(int n, uint* buffers) => _glGenBuffers(n, buffers);

        /// <summary>
        ///     Generate a single buffer object name.
        /// </summary>
        /// <returns>The buffer object name.</returns>

        public static uint GenBuffer() {
            uint id;
            _glGenBuffers(1, &id);
            return id;
        }

        /// <summary>
        ///     Generate buffer object names.
        /// </summary>
        /// <param name="n">Specifies the number of buffer object names to be generated.</param>
        /// <returns>An array of generated buffer object names.</returns>


        public static uint[] GenBuffers(int n) {
            var buffers = new uint[n];
            fixed (uint* ids = &buffers[0]) {
                _glGenBuffers(n, ids);
            }
            return buffers;
        }

        /// <summary>
        /// Determine if a name corresponds to a buffer object.
        /// </summary>
        /// <param name="buffer">Specifies a value that may be the name of a buffer object.</param>
        /// <returns><c>true</c> if object is a buffer, otherwise <c>false</c>.</returns>
        public static bool IsBuffer(uint buffer) => _glIsBuffer(buffer);

        /// <summary>
        /// Determine if a name corresponds to a framebuffer object.
        /// </summary>
        /// <param name="framebuffer">Specifies a value that may be the name of a framebuffer object.</param>
        /// <returns><c>true</c> if value is a framebuffer object, otherwise <c>false</c>.</returns>
        public static bool IsFramebuffer(uint framebuffer) => _glIsFramebuffer(framebuffer);

        /// <summary>
        /// Determine if a name corresponds to a renderbuffer object.
        /// </summary>
        /// <param name="renderbuffer">Specifies a value that may be the name of a renderbuffer object.</param>
        /// <returns><c>true</c> if object is a renderbuffer, otherwise <c>false</c>.</returns>
        public static bool IsRenderbuffer(uint renderbuffer) => _glIsRenderbuffer(renderbuffer);

        /// <summary>
        ///     Generate sampler object names.
        /// </summary>
        /// <param name="count">Specifies the number of sampler object names to generate.</param>
        /// <param name="samplers">Specifies an array in which the generated sampler object names are stored.</param>
        public static void GenSamplers(int count, uint* samplers) => _glGenSamplers(count, samplers);

        /// <summary>
        ///     Generate sampler object names.
        /// </summary>
        /// <param name="count">Specifies the number of sampler object names to generate.</param>
        /// <returns>An array containing the generated sampler object names.</returns>

        public static uint[] GenSamplers(int count) {
            var samplers = new uint[count];
            fixed (uint* s = &samplers[0]) {
                _glGenSamplers(count, s);
            }

            return samplers;
        }

        /// <summary>
        ///     Generate a single sampler object name.
        /// </summary>
        /// <returns>The generated sampler object name.</returns>
        public static uint GenSampler() {
            uint sampler;
            _glGenSamplers(1, &sampler);
            return sampler;
        }

        /// <summary>
        ///     Determine if a name corresponds to a sampler object.
        /// </summary>
        /// <param name="sampler">Specifies a value that may be the name of a sampler object.</param>
        /// <returns><c>true</c> if object is a sampler object, otherwise <c>false</c>.</returns>
        public static bool IsSampler(uint sampler) => _glIsSampler(sampler);

        /// <summary>
        ///     Delete named sampler objects.
        /// </summary>
        /// <param name="samplers">Specifies an array of sampler objects to be deleted.</param>
        public static void DeleteSamplers(uint[] samplers) {
            fixed (uint* s = &samplers[0]) {
                _glDeleteSamplers(samplers.Length, s);
            }
        }

        /// <summary>
        ///     Delete named sampler objects.
        /// </summary>
        /// <param name="count">Specifies the number of sampler objects to be deleted.</param>
        /// <param name="samplers">Specifies an array of sampler objects to be deleted.</param>
        public static void DeleteSamplers(int count, /*const*/ uint* samplers) => _glDeleteSamplers(count, samplers);

        /// <summary>
        ///     Delete a single named sampler object.
        /// </summary>
        /// <param name="sampler">Sampler object to delete.</param>
        public static void DeleteSampler(uint sampler) => _glDeleteSamplers(1, &sampler);

        /// <summary>
        ///     Bind a named sampler to a texturing target.
        /// </summary>
        /// <param name="unit">Specifies the index of the texture unit to which the sampler is bound.</param>
        /// <param name="sampler">Specifies the name of a sampler.</param>
        public static void BindSampler(uint unit, uint sampler) => _glBindSampler(unit, sampler);

        /// <summary>
        /// Attach a level of a texture object as a logical buffer of a framebuffer object.
        /// </summary>
        /// <param name="target">Specifies the target to which the framebuffer is bound.</param>
        /// <param name="attachment">Specifies the attachment point of the framebuffer.</param>
        /// <param name="texTarget">Specifies what type of texture is expected in the texture parameter, or for cube map textures, which face is to be attached.</param>
        /// <param name="texture">Specifies the name of an existing texture object to attach.</param>
        /// <param name="level">Specifies the mipmap level of the texture object to attach.</param>
        public static void FramebufferTexture1D(int target, int attachment, int texTarget, uint texture, int level) => _glFramebufferTexture1D(target, attachment, texTarget, texture, level);

        /// <summary>
        /// Attach a level of a texture object as a logical buffer of a framebuffer object.
        /// </summary>
        /// <param name="target">Specifies the target to which the framebuffer is bound.</param>
        /// <param name="attachment">Specifies the attachment point of the framebuffer.</param>
        /// <param name="texTarget">Specifies what type of texture is expected in the texture parameter, or for cube map textures, which face is to be attached.</param>
        /// <param name="texture">Specifies the name of an existing texture object to attach.</param>
        /// <param name="level">Specifies the mipmap level of the texture object to attach.</param>
        public static void FramebufferTexture2D(int target, int attachment, int texTarget, uint texture, int level) => _glFramebufferTexture2D(target, attachment, texTarget, texture, level);

        /// <summary>
        /// Attach a level of a texture object as a logical buffer of a framebuffer object.
        /// </summary>
        /// <param name="target">Specifies the target to which the framebuffer is bound.</param>
        /// <param name="attachment">Specifies the attachment point of the framebuffer.</param>
        /// <param name="texTarget">Specifies what type of texture is expected in the texture parameter, or for cube map textures, which face is to be attached.</param>
        /// <param name="texture">Specifies the name of an existing texture object to attach.</param>
        /// <param name="level">Specifies the mipmap level of the texture object to attach.</param>
        /// <param name="zOffset">The offset on the z-axis.</param>
        public static void FramebufferTexture3D(int target, int attachment, int texTarget, uint texture, int level, int zOffset) => _glFramebufferTexture3D(target, attachment, texTarget, texture, level, zOffset);

        /// <summary>
        ///     Check the completeness status of a framebuffer.
        /// </summary>
        /// <param name="target">Specify the target to which the framebuffer is bound to check.</param>
        /// <returns></returns> 
        public static int CheckFramebufferStatus(int target) => _glCheckFramebufferStatus(target);

        /// <summary>
        /// Clear individual buffers of a framebuffer.
        /// </summary>
        /// <param name="buffer">Specify the buffer to clear.</param>
        /// <param name="drawbuffer">Specify a particular draw buffer to clear.</param>
        /// <param name="value">The value or values to clear the buffer to.</param>
        public static void ClearBufferiv(int buffer, int drawbuffer, int[] value) {
            fixed (int* v = &value[0]) {
                _glClearBufferiv(buffer, drawbuffer, v);
            }
        }

        /// <summary>
        /// Clear individual buffers of a framebuffer.
        /// </summary>
        /// <param name="buffer">Specify the buffer to clear.</param>
        /// <param name="drawbuffer">Specify a particular draw buffer to clear.</param>
        /// <param name="value">The value or values to clear the buffer to.</param>
        public static void ClearBufferuiv(int buffer, int drawbuffer, uint[] value) {
            fixed (uint* v = &value[0]) {
                _glClearBufferuiv(buffer, drawbuffer, v);
            }
        }

        /// <summary>
        /// Clear individual buffers of a framebuffer.
        /// </summary>
        /// <param name="buffer">Specify the buffer to clear.</param>
        /// <param name="drawbuffer">Specify a particular draw buffer to clear.</param>
        /// <param name="value">The value or values to clear the buffer to.</param>
        public static void ClearBufferfv(int buffer, int drawbuffer, float[] value) {
            fixed (float* v = &value[0]) {
                _glClearBufferfv(buffer, drawbuffer, v);
            }
        }

        /// <summary>
        /// Clear individual buffers of a framebuffer.
        /// </summary>
        /// <param name="buffer">Specify the buffer to clear.</param>
        /// <param name="drawbuffer">Specify a particular draw buffer to clear.</param>
        /// <param name="value">A pointer to the value or values to clear the buffer to.</param>
        public static void ClearBufferiv(int buffer, int drawbuffer, /*const*/ int* value) => _glClearBufferiv(buffer, drawbuffer, value);

        /// <summary>
        /// Clear individual buffers of a framebuffer.
        /// </summary>
        /// <param name="buffer">Specify the buffer to clear.</param>
        /// <param name="drawbuffer">Specify a particular draw buffer to clear.</param>
        /// <param name="value">A pointer to the value or values to clear the buffer to.</param>
        public static void ClearBufferuiv(int buffer, int drawbuffer, /*const*/ uint* value) => _glClearBufferuiv(buffer, drawbuffer, value);

        /// <summary>
        /// Clear individual buffers of a framebuffer.
        /// </summary>
        /// <param name="buffer">Specify the buffer to clear.</param>
        /// <param name="drawbuffer">Specify a particular draw buffer to clear.</param>
        /// <param name="value">A pointer to the value or values to clear the buffer to.</param>
        public static void ClearBufferfv(int buffer, int drawbuffer, /*const*/ float* value) => _glClearBufferfv(buffer, drawbuffer, value);

        /// <summary>
        /// Clear individual buffers of a framebuffer.
        /// </summary>
        /// <param name="buffer">Specify the buffer to clear.</param>
        /// <param name="drawbuffer">Specify a particular draw buffer to clear.</param>
        /// <param name="depth">The value to clear the depth buffer to.</param>
        /// <param name="stencil">The value to clear the stencil buffer to.</param>
        public static void ClearBufferfi(int buffer, int drawbuffer, float depth, int stencil) => _glClearBufferfi(buffer, drawbuffer, depth, stencil);

        /// <summary>
        /// Attaches a shader object to a program object.
        /// </summary>
        /// <param name="program">Specifies the program object to which a shader object will be attached.</param>
        /// <param name="shader">Specifies the shader object that is to be attached.</param>
        public static void AttachShader(uint program, uint shader) => _glAttachShader(program, shader);

        /// <summary>
        ///     Bind a buffer object to an indexed buffer target.
        /// </summary>
        /// <param name="target">
        ///     Specify the target of the bind operation
        ///     <para>
        ///         Must be one of ATOMIC_COUNTER_BUFFER, TRANSFORM_FEEDBACK_BUFFER, UNIFORM_BUFFER, or
        ///         SHADER_STORAGE_BUFFER.
        ///     </para>
        /// </param>
        /// <param name="index">Specify the index of the binding point within the array specified by target.</param>
        /// <param name="buffer">The name of a buffer object to bind to the specified binding point.</param>
        public static void BindBufferBase(int target, uint index, uint buffer) => _glBindBufferBase(target, index, buffer);

        /// <summary>
        ///     Set the value of a sub-word of the sample mask.
        /// </summary>
        /// <param name="maskNumber">Specifies which 32-bit sub-word of the sample mask to update.</param>
        /// <param name="mask">Specifies the new value of the mask sub-word.</param>
        public static void SampleMaski(uint maskNumber, uint mask) => _glSampleMaski(maskNumber, mask);

        /// <summary>
        ///     Query the bindings of color indices to user-defined varying out variables.
        /// </summary>
        /// <param name="program">The name of the program containing varying out variable whose binding to query.</param>
        /// <param name="name">The name of the user-defined varying out variable whose index to query.</param>
        /// <returns>
        ///     The index of the fragment color to which the variable name was bound when the program object program was last
        ///     linked, ot <c>-1</c> if an error occured.
        /// </returns>
        public static int GetFragDataIndex(uint program, string name) {
            var buffer = Encoding.UTF8.GetBytes(name);
            fixed (byte* b = &buffer[0]) {
                return _glGetFragDataIndex(program, b);
            }
        }

        /// <summary>
        /// Start transform feedback operation.
        /// </summary>
        /// <param name="primitiveMode">Specify the output type of the primitives that will be recorded into the buffer objects that are bound for transform feedback.</param>
        public static void BeginTransformFeedback(int primitiveMode) => _glBeginTransformFeedback(primitiveMode);

        /// <summary>
        /// End transform feedback operation.
        /// </summary>
        public static void EndTransformFeedback() => _glEndTransformFeedback();

        /// <summary>
        /// Enable or disable server-side GL capabilities.
        /// </summary>
        /// <param name="target">Specifies a symbolic constant indicating a GL capability.</param>
        /// <param name="index">Specifies the index of the switch to enable.</param>
        public static void Enablei(int target, uint index) => _glEnablei(target, index);

        /// <summary>
        /// Disable server-side GL capabilities
        /// </summary>
        /// <param name="target">Specifies a symbolic constant indicating a GL capability.</param>
        /// <param name="index">Specifies the index of the switch to disable</param>
        public static void Disablei(int target, uint index) => _glDisablei(target, index);

        /// <summary>
        /// Test whether a capability is enabled.
        /// </summary>
        /// <param name="target">Specifies a symbolic constant indicating a GL capability.</param>
        /// <param name="index">Specifies the index of the capability.</param>
        /// <returns><c>true</c> if capability is enabled, otherwise <c>false</c>.</returns>
        public static bool IsEnabledi(int target, uint index) => _glIsEnabledi(target, index);

        /// <summary>
        ///     Compiles a shader object.
        /// </summary>
        /// <param name="shader">Specifies the shader object to be compiled.</param>
        public static void CompileShader(uint shader) => _glCompileShader(shader);

        /// <summary>
        ///     Creates a shader program object.
        /// </summary>
        /// <returns>An empty program object, a non-zero value by which it can be referenced.</returns>
        public static uint CreateProgram() => _glCreateProgram();

        /// <summary>
        ///     Creates a shader object.
        /// </summary>
        /// <param name="type">Specifies the type of shader to be created.<para>Must be one of VERTEX_SHADER, GEOMETRY_SHADER, or FRAGMENT_SHADER.</para></param>
        /// <returns>An empty shader object, a non-zero value by which it can be referenced.</returns>
        public static uint CreateShader(int type) => _glCreateShader(type);

        /// <summary>
        ///     Determines if a name corresponds to a program object.
        /// </summary>
        /// <param name="program">The potential program object to check.</param>
        /// <returns><c>true</c> if object is a program, otherwise <c>false</c>.</returns>
        public static bool IsProgram(uint program) => _glIsProgram(program);

        /// <summary>
        ///     Determines if a name corresponds to a shader object.
        /// </summary>
        /// <param name="shader">The potential program object to check.</param>
        /// <returns><c>true</c> if object is a shader, otherwise <c>false</c>.</returns>
        public static bool IsShader(uint shader) => _glIsShader(shader);

        /// <summary>
        ///     Deletes a program object.
        /// </summary>
        /// <param name="program">Specifies the program object to be deleted.</param>
        public static void DeleteProgram(uint program) => _glDeleteProgram(program);

        /// <summary>
        ///     Deletes a shader object.
        /// </summary>
        /// <param name="shader">Specifies the shader object to be deleted.</param>
        public static void DeleteShader(uint shader) => _glDeleteShader(shader);

        /// <summary>
        ///     Detaches a shader object from a program object to which it is attached.
        /// </summary>
        /// <param name="program">Specifies the program object from which to detach the shader object.</param>
        /// <param name="shader">Specifies the shader object to be detached.</param>
        public static void DetachShader(uint program, uint shader) => _glDetachShader(program, shader);

        /// <summary>
        ///     Installs a program object as part of current rendering state.
        /// </summary>
        /// <param name="program">Specifies the handle of the program object whose executables are to be used as part of current rendering state.</param>
        public static void UseProgram(uint program) => _glUseProgram(program);

        /// <summary>
        ///     Links a program object.
        /// </summary>
        /// <param name="program">Specifies the handle of the program object to be linked.</param>
        public static void LinkProgram(uint program) => _glLinkProgram(program);

        /// <summary>
        ///      Replaces the source code in a shader object.
        /// </summary>
        /// <param name="shader">Specifies the handle of the shader object whose source code is to be replaced.</param>
        /// <param name="count">Specifies the number of elements in the string and length arrays.</param>
        /// <param name="str">Specifies an array of pointers to strings containing the source code to be loaded into the shader.</param>
        /// <param name="length">Specifies an array of string lengths.</param>
        public static void ShaderSource(uint shader, int count, /*const*/ byte** str, /*const*/ int* length) => _glShaderSource(shader, count, str, length);

        /// <summary>
        ///      Replaces the source code in a shader object.
        /// </summary>
        /// <param name="shader">Specifies the handle of the shader object whose source code is to be replaced.</param>
        /// <param name="source">The source code to be loaded into the shader.</param>
        public static void ShaderSource(uint shader, string source) {
            var buffer = Encoding.UTF8.GetBytes(source);
            fixed (byte* p = &buffer[0]) {
                var sources = new[] { p };
                fixed (byte** s = &sources[0]) {
                    var length = buffer.Length;
                    _glShaderSource(shader, 1, s, &length);
                }
            }
        }

        /// <summary>
        ///      Returns the location of a uniform variable.
        /// </summary>
        /// <param name="program">Specifies the program object to be queried.</param>
        /// <param name="name">Points to a null terminated string containing the name of the uniform variable whose location is to be queried.</param>
        /// <returns>An integer that represents the location of a specific uniform variable within a program object.</returns>
        public static int GetUniformLocation(uint program, /*const*/ byte* name) => _glGetUniformLocation(program, name);

        /// <summary>
        ///      Returns the location of a uniform variable.
        /// </summary>
        /// <param name="program">Specifies the program object to be queried.</param>
        /// <param name="name">A array of bytes containing the name of the uniform variable whose location is to be queried.</param>
        /// <returns>An integer that represents the location of a specific uniform variable within a program object.</returns>
        public static int GetUniformLocation(uint program, byte[] name) {
            fixed (byte* b = &name[0]) {
                return _glGetUniformLocation(program, b);
            }
        }

        /// <summary>
        ///      Returns the location of a uniform variable.
        /// </summary>
        /// <param name="program">Specifies the program object to be queried.</param>
        /// <param name="name">A string containing the name of the uniform variable whose location is to be queried.</param>
        /// <returns>An integer that represents the location of a specific uniform variable within a program object.</returns>
        public static int GetUniformLocation(uint program, string name) {
            var bytes = Encoding.UTF8.GetBytes(name);
            fixed (byte* b = &bytes[0]) {
                return _glGetUniformLocation(program, b);
            }
        }

        /// <summary>
        ///     Returns the source code string from a shader object.
        /// </summary>
        /// <param name="shader">Specifies the shader object to be queried.</param>
        /// <param name="bufSize">Specifies the size of the character buffer for storing the returned source code string.</param>
        /// <returns>The shader source, or <c>null</c> if an error occured.</returns>


        public static string GetShaderSource(uint shader, int bufSize = 4096) {
            var buffer = Marshal.AllocHGlobal(bufSize);
            try {
                int length;
                var source = (byte*)buffer.ToPointer();
                _glGetShaderSource(shader, bufSize, &length, source);
                return PtrToStringUtf8(buffer, length);
            }
            catch (Exception) {
                return null;
            }
            finally {
                Marshal.FreeHGlobal(buffer);
            }
        }

        /// <summary>
        ///     Returns the information log for a program object.
        /// </summary>
        /// <param name="program">Specifies the program object whose information log is to be queried.</param>
        /// <param name="bufSize">Specifies the size of the character buffer for storing the returned information log.</param>
        /// <returns>The info log, or <c>null</c> if an error occured.</returns>


        public static string GetProgramInfoLog(uint program, int bufSize = 1024) {
            var buffer = Marshal.AllocHGlobal(bufSize);
            try {
                int length;
                var source = (byte*)buffer.ToPointer();
                _glGetProgramInfoLog(program, bufSize, &length, source);
                return PtrToStringUtf8(buffer, length);
            }
            catch (Exception) {
                return null;
            }
            finally {
                Marshal.FreeHGlobal(buffer);
            }
        }

        /// <summary>
        ///     Returns the information log for a shader object.
        /// </summary>
        /// <param name="shader">Specifies the shader object whose information log is to be queried.</param>
        /// <param name="bufSize">Specifies the size of the character buffer for storing the returned information log.</param>
        /// <returns>The info log, or <c>null</c> if an error occured.</returns>


        public static string GetShaderInfoLog(uint shader, int bufSize = 1024) {
            var buffer = Marshal.AllocHGlobal(bufSize);
            try {
                int length;
                var source = (byte*)buffer.ToPointer();
                _glGetShaderInfoLog(shader, bufSize, &length, source);
                return PtrToStringUtf8(buffer, length);
            }
            catch (Exception) {
                return null;
            }
            finally {
                Marshal.FreeHGlobal(buffer);
            }
        }

        /// <summary>
        ///     Validates a program object.
        /// </summary>
        /// <param name="program">Specifies the handle of the program object to be validated.</param>
        /// <seealso cref="glGetProgramInfoLog"/>
        public static void ValidateProgram(uint program) => _glValidateProgram(program);

        /// <summary>
        ///     Render multiple sets of primitives by specifying indices of array data elements.
        /// </summary>
        /// <param name="mode">Specifies what kind of primitives to render.</param>
        /// <param name="count">Points to an array of the elements counts.</param>
        /// <param name="type">
        ///     Specifies the type of the values in indices.
        ///     <para>Must be one of UNSIGNED_BYTE, UNSIGNED_SHORT, or UNSIGNED_INT.</para>
        /// </param>
        /// <param name="indices">Specifies a pointer to the location where the indices are stored.</param>
        /// <param name="drawCount">Specifies the size of the count and indices arrays.</param>
        public static void MultiDrawElements(int mode, /*const*/ int* count, int type, /*const*/void* /*const*/* indices, int drawCount) => _glMultiDrawElements(mode, count, type, indices, drawCount);

        /// <summary>
        ///     Render multiple sets of primitives by specifying indices of array data elements.
        /// </summary>
        /// <param name="mode">Specifies what kind of primitives to render.</param>
        /// <param name="count">Points to an array of the elements counts.</param>
        /// <param name="type">
        ///     Specifies the type of the values in indices.
        ///     <para>Must be one of UNSIGNED_BYTE, UNSIGNED_SHORT, or UNSIGNED_INT.</para>
        /// </param>
        /// <param name="indices">Specifies a pointer to the location where the indices are stored.</param>
        /// <param name="drawCount">Specifies the size of the count and indices arrays.</param>
        public static void MultiDrawElements(int mode, int[] count, int type, IntPtr indices, int drawCount) {
            // Test this actually works
            var ptr = (void**)indices.ToPointer();
            fixed (int* c = &count[0]) {
                _glMultiDrawElements(mode, c, type, ptr, drawCount);
            }
        }

        /// <summary>
        ///     Render multiple sets of primitives from array data.
        /// </summary>
        /// <param name="mode">Specifies what kind of primitives to render.</param>
        /// <param name="first">Points to an array of starting indices in the enabled arrays.</param>
        /// <param name="count">Points to an array of the number of indices to be rendered.</param>
        /// <param name="drawCount">Specifies the size of the first and count.</param>
        public static void MultiDrawArrays(int mode, /*const*/ int* first, /*const*/ int* count, int drawCount) => _glMultiDrawArrays(mode, first, count, drawCount);

        /// <summary>
        ///     Render multiple sets of primitives from array data.
        /// </summary>
        /// <param name="mode">Specifies what kind of primitives to render.</param>
        /// <param name="first">An array of starting indices in the enabled arrays.</param>
        /// <param name="count">An array of the number of indices to be rendered.</param>
        /// <param name="drawCount">Specifies the size of the first and count.</param>
        public static void MultiDrawArrays(int mode, int[] first, int[] count, int drawCount) {
            fixed (int* f = &first[0]) {
                fixed (int* c = &count[0]) {
                    _glMultiDrawArrays(mode, f, c, drawCount);
                }
            }
        }

        /// <summary>
        ///     Attach a level of a texture object as a logical buffer of a framebuffer object.
        /// </summary>
        /// <param name="target">Specifies the target to which the framebuffer is bound.</param>
        /// <param name="attachment">Specifies the attachment point of the framebuffer.</param>
        /// <param name="texture">Specifies the name of an existing texture object to attach.</param>
        /// <param name="level">Specifies the mipmap level of the texture object to attach.</param>
        public static void FramebufferTexture(int target, int attachment, uint texture, int level) => _glFramebufferTexture(target, attachment, texture, level);

        /// <summary>
        ///     Attach a renderbuffer object to a framebuffer object.
        /// </summary>
        /// <param name="target">Specifies the framebuffer target. The symbolic constant must be FRAMEBUFFER.</param>
        /// <param name="attachment">
        ///     Specifies the attachment point to which renderbuffer should be attached.
        ///     <para>
        ///         Must be one of the following symbolic constants: COLOR_ATTACHMENT0, DEPTH_ATTACHMENT, or
        ///         STENCIL_ATTACHMENT.
        ///     </para>
        /// </param>
        /// <param name="renderbufferTarget">Specifies the renderbuffer target. The symbolic constant must be RENDERBUFFER.</param>
        /// <param name="renderbuffer">Specifies the renderbuffer object that is to be attached.</param>
        public static void FramebufferRenderbuffer(int target, int attachment, int renderbufferTarget, uint renderbuffer) => _glFramebufferRenderbuffer(target, attachment, renderbufferTarget, renderbuffer);

        /// <summary>
        ///     Attach a renderbuffer object to a framebuffer object.
        /// </summary>
        /// <param name="attachment">
        ///     Specifies the attachment point to which renderbuffer should be attached.
        ///     <para>
        ///         Must be one of the following symbolic constants: COLOR_ATTACHMENT0, DEPTH_ATTACHMENT, or
        ///         STENCIL_ATTACHMENT.
        ///     </para>
        /// </param>
        /// <param name="renderbuffer">Specifies the renderbuffer object that is to be attached.</param>
        public static void FramebufferRenderbuffer(int attachment, uint renderbuffer) => _glFramebufferRenderbuffer(FRAMEBUFFER, attachment, RENDERBUFFER, renderbuffer);

        /// <summary>
        ///     Returns a subset of a buffer object's data store.
        /// </summary>
        /// <param name="target">SSpecifies the target to which the buffer object is bound.</param>
        /// <param name="offset">
        ///     Specifies the offset into the buffer object's data store from which data will be returned,
        ///     measured in bytes.
        /// </param>
        /// <param name="size">Specifies the size in bytes of the data store region being returned.</param>
        /// <param name="data">Specifies a pointer to the location where buffer object data is returned.</param>
        public static void GetBufferSubData(int target, int offset, int size, IntPtr data) => _glGetBufferSubData(target, new IntPtr(offset), new IntPtr(size), data.ToPointer());

        /// <summary>
        ///     Returns a subset of a buffer object's data store.
        /// </summary>
        /// <param name="target">SSpecifies the target to which the buffer object is bound.</param>
        /// <param name="offset">
        ///     Specifies the offset into the buffer object's data store from which data will be returned,
        ///     measured in bytes.
        /// </param>
        /// <param name="size">Specifies the size in bytes of the data store region being returned.</param>
        /// <param name="data">Specifies a pointer to the location where buffer object data is returned.</param>
        public static void GetBufferSubData(int target, long offset, long size, IntPtr data) => _glGetBufferSubData(target, new IntPtr(offset), new IntPtr(size), data.ToPointer());

        /// <summary>
        ///     Returns a subset of a buffer object's data store.
        /// </summary>
        /// <param name="target">SSpecifies the target to which the buffer object is bound.</param>
        /// <param name="offset">
        ///     Specifies the offset into the buffer object's data store from which data will be returned,
        ///     measured in bytes.
        /// </param>
        /// <param name="size">Specifies the size in bytes of the data store region being returned.</param>
        /// <param name="data">Specifies a pointer to the location where buffer object data is returned.</param>
        public static void GetBufferSubData(int target, int offset, int size, void* data) => _glGetBufferSubData(target, new IntPtr(offset), new IntPtr(size), data);

        /// <summary>
        ///     Returns a subset of a buffer object's data store.
        /// </summary>
        /// <param name="target">SSpecifies the target to which the buffer object is bound.</param>
        /// <param name="offset">
        ///     Specifies the offset into the buffer object's data store from which data will be returned,
        ///     measured in bytes.
        /// </param>
        /// <param name="size">Specifies the size in bytes of the data store region being returned.</param>
        /// <param name="data">Specifies a pointer to the location where buffer object data is returned.</param>
        public static void GetBufferSubData(int target, long offset, long size, void* data) => _glGetBufferSubData(target, new IntPtr(offset), new IntPtr(size), data);

        /// <summary>
        ///     Map all or part of a buffer object's data store into the client's address space.
        /// </summary>
        /// <param name="target">Specifies the target to which the buffer object is bound.</param>
        /// <param name="offset">Specifies the starting offset within the buffer of the range to be mapped.</param>
        /// <param name="length">Specifies the length of the range to be mapped.</param>
        /// <param name="access">Specifies a combination of access flags indicating the desired access to the mapped range.</param>
        /// <returns>A pointer to the beginning of the mapped range.</returns>
        public static IntPtr MapBufferRange(int target, int offset, int length, uint access) => new(_glMapBufferRange(target, new IntPtr(offset), new IntPtr(length), access));

        /// <summary>
        ///     Map all or part of a buffer object's data store into the client's address space.
        /// </summary>
        /// <param name="target">Specifies the target to which the buffer object is bound.</param>
        /// <param name="offset">Specifies the starting offset within the buffer of the range to be mapped.</param>
        /// <param name="length">Specifies the length of the range to be mapped.</param>
        /// <param name="access">Specifies a combination of access flags indicating the desired access to the mapped range.</param>
        /// <returns>A pointer to the beginning of the mapped range.</returns>
        public static IntPtr MapBufferRange(int target, long offset, long length, uint access) => new(_glMapBufferRange(target, new IntPtr(offset), new IntPtr(length), access));

        /// <summary>
        ///     Indicate modifications to a range of a mapped buffer.
        /// </summary>
        /// <param name="target">Specifies the target to which the buffer object is bound.</param>
        /// <param name="offset">Specifies the start of the buffer subrange, in basic machine units.</param>
        /// <param name="length">Specifies the length of the buffer subrange, in basic machine units.</param>
        public static void FlushMappedBufferRange(int target, int offset, int length) => _glFlushMappedBufferRange(target, new IntPtr(offset), new IntPtr(length));

        /// <summary>
        ///     Indicate modifications to a range of a mapped buffer.
        /// </summary>
        /// <param name="target">Specifies the target to which the buffer object is bound.</param>
        /// <param name="offset">Specifies the start of the buffer subrange, in basic machine units.</param>
        /// <param name="length">Specifies the length of the buffer subrange, in basic machine units.</param>
        public static void FlushMappedBufferRange(int target, long offset, long length) => _glFlushMappedBufferRange(target, new IntPtr(offset), new IntPtr(length));

        /// <summary>
        ///     Attach a single layer of a texture to a framebuffer.
        /// </summary>
        /// <param name="target">
        ///     Specifies the framebuffer target.
        ///     <para>
        ///         Must be DRAW_FRAMEBUFFER, READ_FRAMEBUFFER, or FRAMEBUFFER. FRAMEBUFFER is equivalent to
        ///         DRAW_FRAMEBUFFER.
        ///     </para>
        /// </param>
        /// <param name="attachment">
        ///     Specifies the attachment point of the framebuffer.
        ///     <para>Must be COLOR_ATTACHMENTi, DEPTH_ATTACHMENT, STENCIL_ATTACHMENT or DEPTH_STENCIL_ATTACHMENT.</para>
        /// </param>
        /// <param name="texture">
        ///     Specifies the texture object to attach to the framebuffer attachment point named by
        ///     <paramref name="attachment" />.
        /// </param>
        /// <param name="level">Specifies the mipmap level of texture to attach.</param>
        /// <param name="layer">Specifies the layer of texture to attach.</param>
        public static void FramebufferTextureLayer(int target, int attachment, uint texture, int level, int layer) => _glFramebufferTextureLayer(target, attachment, texture, level, layer);

        /// <summary>
        ///     Bind a range within a buffer object to an indexed buffer target.
        /// </summary>
        /// <param name="target">
        ///     Specify the target of the bind operation
        ///     <para>
        ///         Must be one of ATOMIC_COUNTER_BUFFER, TRANSFORM_FEEDBACK_BUFFER, UNIFORM_BUFFER, or
        ///         SHADER_STORAGE_BUFFER.
        ///     </para>
        /// </param>
        /// <param name="index">Specify the index of the binding point within the array specified by target.</param>
        /// <param name="buffer">The name of a buffer object to bind to the specified binding point.</param>
        /// <param name="offset">The starting offset in basic machine units into the buffer object buffer.</param>
        /// <param name="size">
        ///     The amount of data in machine units that can be read from the buffer object while used as an indexed
        ///     target.
        /// </param>
        public static void BindBufferRange(int target, uint index, uint buffer, int offset, int size) => _glBindBufferRange(target, index, buffer, new IntPtr(offset), new IntPtr(size));

        /// <summary>
        ///     Bind a range within a buffer object to an indexed buffer target.
        /// </summary>
        /// <param name="target">
        ///     Specify the target of the bind operation
        ///     <para>
        ///         Must be one of ATOMIC_COUNTER_BUFFER, TRANSFORM_FEEDBACK_BUFFER, UNIFORM_BUFFER, or
        ///         SHADER_STORAGE_BUFFER.
        ///     </para>
        /// </param>
        /// <param name="index">Specify the index of the binding point within the array specified by target.</param>
        /// <param name="buffer">The name of a buffer object to bind to the specified binding point.</param>
        /// <param name="offset">The starting offset in basic machine units into the buffer object buffer.</param>
        /// <param name="size">
        ///     The amount of data in machine units that can be read from the buffer object while used as an indexed
        ///     target.
        /// </param>
        public static void BindBufferRange(int target, uint index, uint buffer, long offset, long size) => _glBindBufferRange(target, index, buffer, new IntPtr(offset), new IntPtr(size));

        /// <summary>
        ///     Copy a block of pixels from one framebuffer object to another.
        /// </summary>
        /// <param name="srcX0">The lower left corner of the read buffer on the x-axis.</param>
        /// <param name="srcY0">The lower left corner of the read buffer on the y-axis.</param>
        /// <param name="srcX1">The upper right corner of the read buffer on the x-axis.</param>
        /// <param name="srcY1">The upper right corner of the read buffer on the y-axis.</param>
        /// <param name="dstX0">The lower left corner of the write buffer on the x-axis.</param>
        /// <param name="dstY0">The lower left corner of the write buffer on the y-axis.</param>
        /// <param name="dstX1">The upper right corner of the write buffer on the x-axis.</param>
        /// <param name="dstY1">The upper right corner of the write buffer on the y-axis.</param>
        /// <param name="mask">The bitwise OR of the flags indicating which buffers are to be copied.<para>The allowed flags are COLOR_BUFFER_BIT, DEPTH_BUFFER_BIT and STENCIL_BUFFER_BIT.</para></param>
        /// <param name="filter">Specifies the interpolation to be applied if the image is stretched.<para>Must be NEAREST or LINEAR.</para></param>
        public static void BlitFramebuffer(int srcX0, int srcY0, int srcX1, int srcY1, int dstX0, int dstY0, int dstX1, int dstY1, uint mask, int filter) => _glBlitFramebuffer(srcX0, srcY0, srcX1, srcY1, dstX0, dstY0, dstX1, dstY1, mask, filter);

        /// <summary>
        /// Establish data storage, format and dimensions of a renderbuffer object's image.
        /// </summary>
        /// <param name="target">Specify the target of the bind operation.<para>Must be RENDERBUFFER.</para></param>
        /// <param name="internalFormat">Specifies the internal format to use for the renderbuffer object's image.</param>
        /// <param name="width">Specifies the width of the renderbuffer, in pixels.</param>
        /// <param name="height">Specifies the height of the renderbuffer, in pixels.</param>
        public static void RenderbufferStorage(int target, int internalFormat, int width, int height) => _glRenderbufferStorage(target, internalFormat, width, height);

        /// <summary>
        ///     Set the current color as a packed value.
        /// </summary>
        /// <param name="type">Specifies the data type of each color components.</param>
        /// <param name="color">The packed color value.</param>
        public static void ColorP3ui(int type, uint color) => _glColorP3ui(type, color);

        /// <summary>
        ///     Set the current color as a packed value.
        /// </summary>
        /// <param name="type">Specifies the data type of each color components.</param>
        /// <param name="color">The packed color value.</param>
        public static void ColorP3uiv(int type, /*const*/ uint* color) => _glColorP3uiv(type, color);

        /// <summary>
        ///     Set the current color as a packed value.
        /// </summary>
        /// <param name="type">Specifies the data type of each color components.</param>
        /// <param name="color">The packed color value.</param>
        public static void ColorP4ui(int type, uint color) => _glColorP4ui(type, color);

        /// <summary>
        ///     Set the current color as a packed value.
        /// </summary>
        /// <param name="type">Specifies the data type of each color components.</param>
        /// <param name="color">The packed color value.</param>
        public static void ColorP4uiv(int type, /*const*/ uint* color) => _glColorP4uiv(type, color);

        /// <summary>
        ///     Set the current color as a packed value.
        /// </summary>
        /// <param name="type">Specifies the data type of each color components.</param>
        /// <param name="color">The packed color value.</param>
        public static void ColorP3uiv(int type, uint[] color) {
            fixed (uint* c = &color[0]) {
                _glColorP3uiv(type, c);
            }
        }

        /// <summary>
        ///     Set the current color as a packed value.
        /// </summary>
        /// <param name="type">Specifies the data type of each color components.</param>
        /// <param name="color">The packed color value.</param>
        public static void ColorP4uiv(int type, uint[] color) {
            fixed (uint* c = &color[0]) {
                _glColorP4uiv(type, c);
            }
        }

        /// <summary>
        ///     Set the current color as a packed value.
        /// </summary>
        /// <param name="type">Specifies the data type of each color components.</param>
        /// <param name="color">The packed color value.</param>
        public static void SecondaryColorP3ui(int type, uint color) => _glSecondaryColorP3ui(type, color);

        /// <summary>
        ///     Set the current color as a packed value.
        /// </summary>
        /// <param name="type">Specifies the data type of each color components.</param>
        /// <param name="color">The packed color value.</param>
        public static void SecondaryColorP3uiv(int type, uint[] color) {
            fixed (uint* c = &color[0]) {
                _glSecondaryColorP3uiv(type, c);
            }
        }

        /// <summary>
        ///     Set the current color as a packed value.
        /// </summary>
        /// <param name="type">Specifies the data type of each color components.</param>
        /// <param name="color">The packed color value.</param>
        public static void SecondaryColorP3uiv(int type, /*const*/ uint* color) => _glSecondaryColorP3uiv(type, color);

        /// <summary>
        ///     Updates a subset of a buffer object's data store.
        /// </summary>
        /// <param name="target">Specifies the target to which the buffer object is bound.</param>
        /// <param name="offset">
        ///     Specifies the offset into the buffer object's data store where data replacement will begin,
        ///     measured in bytes.
        /// </param>
        /// <param name="size">Specifies the size in bytes of the data store region being replaced.</param>
        /// <param name="data">Specifies a pointer to the new data that will be copied into the data store.</param>
        public static void BufferSubData(int target, int offset, int size, IntPtr data) => _glBufferSubData(target, new IntPtr(offset), new IntPtr(size), data.ToPointer());

        /// <summary>
        ///     Updates a subset of a buffer object's data store.
        /// </summary>
        /// <param name="target">Specifies the target to which the buffer object is bound.</param>
        /// <param name="offset">
        ///     Specifies the offset into the buffer object's data store where data replacement will begin,
        ///     measured in bytes.
        /// </param>
        /// <param name="size">Specifies the size in bytes of the data store region being replaced.</param>
        /// <param name="data">Specifies a pointer to the new data that will be copied into the data store.</param>
        public static void BufferSubData(int target, long offset, long size, IntPtr data) => _glBufferSubData(target, new IntPtr(offset), new IntPtr(size), data.ToPointer());


        /// <summary>
        ///     Updates a subset of a buffer object's data store.
        /// </summary>
        /// <param name="target">Specifies the target to which the buffer object is bound.</param>
        /// <param name="offset">
        ///     Specifies the offset into the buffer object's data store where data replacement will begin,
        ///     measured in bytes.
        /// </param>
        /// <param name="size">Specifies the size in bytes of the data store region being replaced.</param>
        /// <param name="data">Specifies a pointer to the new data that will be copied into the data store.</param>
        public static void BufferSubData(int target, int offset, int size, /*const*/ void* data) => _glBufferSubData(target, new IntPtr(offset), new IntPtr(size), data);

        /// <summary>
        ///     Updates a subset of a buffer object's data store.
        /// </summary>
        /// <param name="target">Specifies the target to which the buffer object is bound.</param>
        /// <param name="offset">
        ///     Specifies the offset into the buffer object's data store where data replacement will begin,
        ///     measured in bytes.
        /// </param>
        /// <param name="size">Specifies the size in bytes of the data store region being replaced.</param>
        /// <param name="data">Specifies a pointer to the new data that will be copied into the data store.</param>
        public static void BufferSubData(int target, long offset, long size, /*const*/ void* data) => _glBufferSubData(target, new IntPtr(offset), new IntPtr(size), data);

        /// <summary>
        ///     Set the current normal vector.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="coords">The coords.</param>
        public static void NormalP3ui(int type, uint coords) => _glNormalP3ui(type, coords);

        /// <summary>
        ///     Set the current normal vector.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="coords">The coords.</param>
        public static void NormalP3uiv(int type, /*const*/ uint* coords) => _glNormalP3uiv(type, coords);

        /// <summary>
        ///     Set the current normal vector.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="coords">The coords.</param>
        public static void NormalP3uiv(int type, uint[] coords) {
            fixed (uint* c = &coords[0]) {
                _glNormalP3uiv(type, c);
            }
        }

        /// <summary>
        /// Bind a user-defined varying out variable to a fragment shader color number.
        /// </summary>
        /// <param name="program">The name of the program containing varying out variable whose binding to modify.</param>
        /// <param name="color">The color number to bind the user-defined varying out variable to.</param>
        /// <param name="name">The name of the user-defined varying out variable whose binding to modify.</param>
        public static void BindFragDataLocation(uint program, uint color, string name) {
            var utf8 = Encoding.UTF8.GetBytes(name);
            fixed (byte* b = &utf8[0]) {
                _glBindFragDataLocation(program, color, b);
            }
        }

        /// <summary>
        /// Query the bindings of color numbers to user-defined varying out variables.
        /// </summary>
        /// <param name="program">The name of the program containing varying out variable whose binding to query.</param>
        /// <param name="name">The name of the user-defined varying out variable whose binding to query.</param>
        /// <returns>The requested location, or <c>-1</c> if error occured.</returns>

        public static int GetFragDataLocation(uint program, string name) {
            var utf8 = Encoding.UTF8.GetBytes(name);
            fixed (byte* b = &utf8[0]) {
                return _glGetFragDataLocation(program, b);
            }
        }

        /// <summary>
        /// Returns the location of an attribute variable.
        /// </summary>
        /// <param name="program">Specifies the program object to be queried.</param>
        /// <param name="name">A string containing the name of the attribute variable whose location is to be queried.</param>
        /// <returns>The location of the attribute, or <c>-1</c> if an error occured.</returns>
        public static int GetAttribLocation(uint program, string name) {
            var utf8 = Encoding.UTF8.GetBytes(name);
            fixed (byte* b = &utf8[0]) {
                return _glGetAttribLocation(program, b);
            }
        }

        /// <summary>
        /// Returns the handles of the shader objects attached to a program object.
        /// </summary>
        /// <param name="program">Specifies the program object to be queried.</param>
        /// <param name="maxCount">Specifies the size of the array for storing the returned object names.</param>
        /// <param name="count">Returns the number of names actually returned in shaders.</param>
        /// <param name="shaders">Specifies an array that is used to return the names of attached shader objects.</param>
        public static void GetAttachedShaders(uint program, int maxCount, int* count, uint* shaders) => _glGetAttachedShaders(program, maxCount, count, shaders);

        /// <summary>
        /// Returns the handles of the shader objects attached to a program object.
        /// </summary>
        /// <param name="program">Specifies the program object to be queried.</param>
        /// <param name="maxCount">Specifies the size of the array for storing the returned object names.</param>
        /// <returns>An array containing the attached shaders of the specified program.</returns>


        public static uint[] GetAttachedShaders(uint program, int maxCount) {
            int count;
            var shaders = new uint[maxCount];
            fixed (uint* shader = &shaders[0]) {
                _glGetAttachedShaders(program, maxCount, &count, shader);
            }
            return count < maxCount ? shaders.Take(count).ToArray() : shaders;
        }

        /// <summary>
        /// Associates a generic vertex attribute index with a named attribute variable.
        /// </summary>
        /// <param name="program">Specifies the handle of the program object in which the association is to be made.</param>
        /// <param name="index">Specifies the index of the generic vertex attribute to be bound.</param>
        /// <param name="name">Specifies a string containing the name of the vertex shader attribute variable to which index is to be bound.</param>
        public static void BindAttribLocation(uint program, uint index, string name) {
            var utf8 = Encoding.UTF8.GetBytes(name);
            fixed (byte* b = &utf8[0]) {
                _glBindAttribLocation(program, index, b);
            }
        }

        /// <summary>
        /// Returns information about an active attribute variable for the specified program object.
        /// </summary>
        /// <param name="program">Specifies the program object to be queried.</param>
        /// <param name="index">Specifies the index of the attribute variable to be queried.</param>
        /// <param name="bufSize">Specifies the maximum number of characters OpenGL is allowed to write in the character buffer indicated by name.</param>
        /// <param name="length">Returns the number of characters actually written by OpenGL in the string.</param>
        /// <param name="size">Returns the size of the attribute variable.</param>
        /// <param name="type">Returns the data type of the attribute variable.</param>
        /// <param name="name">The name of the attribute variable.</param>
        public static void GetActiveAttrib(uint program, uint index, int bufSize, out int length, out int size,
            out int type, out string name) {
            var buffer = Marshal.AllocHGlobal(bufSize);
            try {
                _glGetActiveAttrib(program, index, bufSize, out length, out size, out type, buffer);
                name = PtrToStringUtf8(buffer, length);
            }
            finally {
                Marshal.FreeHGlobal(buffer);
            }
        }

        /// <summary>
        /// Returns information about an active uniform variable for the specified program object.
        /// </summary>
        /// <param name="program">Specifies the program object to be queried.</param>
        /// <param name="index">Specifies the index of the uniform variable to be queried.</param>
        /// <param name="bufSize">Specifies the maximum number of characters OpenGL is allowed to write in the character buffer indicated by name.</param>
        /// <param name="length">Returns the number of characters actually written by OpenGL in the string.</param>
        /// <param name="size">Returns the size of the uniform variable.</param>
        /// <param name="type">Returns the data type of the uniform variable.</param>
        /// <param name="name">Returns a string containing the name of the uniform variable.</param>
        public static void GetActiveUniform(uint program, uint index, int bufSize, out int length, out int size,
            out int type, out string name) {
            var buffer = Marshal.AllocHGlobal(bufSize);
            try {
                _glGetActiveUniform(program, index, bufSize, out length, out size, out type, buffer);
                name = PtrToStringUtf8(buffer, length);
            }
            finally {
                Marshal.FreeHGlobal(buffer);
            }
        }

        /// <summary>
        /// Generate mipmaps for a specified texture object.
        /// </summary>
        /// <param name="target">Specifies the target to which the texture object is bound.<para>Must be one of TEXTURE_1D, TEXTURE_2D, TEXTURE_3D, TEXTURE_1D_ARRAY, TEXTURE_2D_ARRAY, TEXTURE_CUBE_MAP, or TEXTURE_CUBE_MAP_ARRAY.</para></param>
        public static void GenerateMipmap(int target) => _glGenerateMipmap(target);

        /// <summary>
        /// Return the value or values of a selected parameter.
        /// </summary>
        /// <param name="target">Specifies the parameter value to be returned.</param>
        /// <param name="index">Specifies the index of the particular element being queried.</param>
        /// <param name="data">Returns the value or values of the specified parameter.</param>
        public static void GetBooleani_v(int target, uint index, bool* data) => _glGetBooleani_v(target, index, data);

        /// <summary>
        /// Return the value or values of a selected parameter.
        /// </summary>
        /// <param name="target">Specifies the parameter value to be returned.</param>
        /// <param name="index">Specifies the index of the particular element being queried.</param>
        /// <param name="data">Returns the value or values of the specified parameter.</param>
        public static void GetIntegeri_v(int target, uint index, int* data) => _glGetIntegeri_v(target, index, data);

        /// <summary>
        /// Return the value or values of a selected parameter.
        /// </summary>
        /// <param name="target">Specifies the parameter value to be returned.</param>
        /// <param name="index">Specifies the index of the particular element being queried.</param>
        /// <param name="data">Returns the value or values of the specified parameter.</param>
        public static void GetInteger64i_v(int target, uint index, long* data) => _glGetInteger64i_v(target, index, data);

        /// <summary>
        /// Return the value or values of a selected parameter.
        /// </summary>
        /// <param name="target">Specifies the parameter value to be returned.</param>
        /// <param name="index">Specifies the index of the particular element being queried.</param>
        /// <returns>The request parameter value.</returns>
        public static bool GetBooleani(int target, uint index) {
            bool value;
            _glGetBooleani_v(target, index, &value);
            return value;
        }

        /// <summary>
        /// Return the value or values of a selected parameter.
        /// </summary>
        /// <param name="target">Specifies the parameter value to be returned.</param>
        /// <param name="index">Specifies the index of the particular element being queried.</param>
        /// <returns>The request parameter value.</returns>
        public static int GetIntegeri(int target, uint index) {
            int value;
            _glGetIntegeri_v(target, index, &value);
            return value;
        }

        /// <summary>
        /// Return the value or values of a selected parameter.
        /// </summary>
        /// <param name="target">Specifies the parameter value to be returned.</param>
        /// <param name="index">Specifies the index of the particular element being queried.</param>
        /// <returns>The request parameter value.</returns>
        public static long GetInteger64i(int target, uint index) {
            long value;
            _glGetInteger64i_v(target, index, &value);
            return value;
        }

        /// <summary>
        /// Return the value or values of a selected parameter.
        /// </summary>
        /// <param name="target">Specifies the parameter value to be returned.</param>
        /// <param name="index">Specifies the index of the particular element being queried.</param>
        /// <param name="count">The number of values to get.</param>
        /// <returns>The request parameter value.</returns>

        public static bool[] GetBooleani_v(int target, uint index, int count) {
            var value = new bool[count];
            fixed (bool* v = &value[0]) {
                _glGetBooleani_v(target, index, v);
            }
            return value;
        }

        /// <summary>
        /// Return the value or values of a selected parameter.
        /// </summary>
        /// <param name="target">Specifies the parameter value to be returned.</param>
        /// <param name="index">Specifies the index of the particular element being queried.</param>
        /// <param name="count">The number of values to get.</param>
        /// <returns>The request parameter value.</returns>

        public static int[] GetIntegeri_v(int target, uint index, int count) {
            var value = new int[count];
            fixed (int* v = &value[0]) {
                _glGetIntegeri_v(target, index, v);
            }
            return value;
        }

        /// <summary>
        /// Return the value or values of a selected parameter.
        /// </summary>
        /// <param name="target">Specifies the parameter value to be returned.</param>
        /// <param name="index">Specifies the index of the particular element being queried.</param>
        /// <param name="count">The number of values to get.</param>
        /// <returns>The request parameter value.</returns>

        public static long[] GetInteger64i_v(int target, uint index, int count) {
            var value = new long[count];
            fixed (long* v = &value[0]) {
                _glGetInteger64i_v(target, index, v);
            }
            return value;
        }

        /// <summary>
        ///     Determine if a name corresponds to a vertex array object.
        /// </summary>
        /// <param name="array">Specifies a value that may be the name of a vertex array object.</param>
        /// <returns><c>true</c> if value is a vertex array, otherwise <c>false</c>.</returns>
        public static bool IsVertexArray(uint array) => _glIsVertexArray(array);

        /// <summary>
        ///     Generate vertex array object names.
        /// </summary>
        /// <param name="n">Specifies the number of vertex array object names to generate.</param>
        /// <param name="arrays">Specifies an array in which the generated vertex array object names are stored.</param>
        public static void GenVertexArrays(int n, uint* arrays) => _glGenVertexArrays(n, arrays);

        /// <summary>
        ///     Generate vertex array object names.
        /// </summary>
        /// <param name="n">Specifies the number of vertex array object names to generate.</param>
        /// <returns>An array of generated vertex array object names.</returns>


        public static uint[] GenVertexArrays(int n) {
            var arrays = new uint[n];
            fixed (uint* names = &arrays[0]) {
                _glGenVertexArrays(n, names);
            }

            return arrays;
        }

        /// <summary>
        ///     Generates a single vertex array object name.
        /// </summary>
        /// <returns>A generated vertex array name.</returns>

        public static uint GenVertexArray() {
            uint array;
            _glGenVertexArrays(1, &array);
            return array;
        }

        /// <summary>
        ///     Bind a vertex array object.
        /// </summary>
        /// <param name="array">Specifies the name of the vertex array to bind.</param>
        public static void BindVertexArray(uint array) => _glBindVertexArray(array);

        /// <summary>
        ///     Delete vertex array objects.
        /// </summary>
        /// <param name="n">Specifies the number of vertex array objects to be deleted.</param>
        /// <param name="arrays">Specifies the address of an array containing the n names of the objects to be deleted.</param>
        public static void DeleteVertexArrays(int n, /*const*/ uint* arrays) => _glDeleteVertexArrays(n, arrays);

        /// <summary>
        ///     Delete vertex array objects.
        /// </summary>
        /// <param name="arrays">An array of vertex array objects to delete.</param>
        public static void DeleteVertexArrays(uint[] arrays) {
            if (arrays is null)
                return;
            fixed (uint* names = &arrays[0]) {
                _glDeleteVertexArrays(arrays.Length, names);
            }
        }

        /// <summary>
        ///     Deletes a single vertex array object.
        /// </summary>
        /// <param name="array">The array to delete.</param>
        public static void DeleteVertexArray(uint array) => _glDeleteVertexArrays(1, &array);

        /// <summary>
        /// Specify point parameters.
        /// </summary>
        /// <param name="paramName">Specifies a single-valued point parameter.<para>GL_POINT_FADE_THRESHOLD_SIZE, and POINT_SPRITE_COORD_ORIGIN are accepted.</para></param>
        /// <param name="param">Specifies the value that paramName will be set to.</param>
        public static void PointParameterf(int paramName, float param) => _glPointParameterf(paramName, param);

        /// <summary>
        /// Specify point parameters.
        /// </summary>
        /// <param name="paramName">Specifies a single-valued point parameter.<para>GL_POINT_FADE_THRESHOLD_SIZE, and POINT_SPRITE_COORD_ORIGIN are accepted.</para></param>
        /// <param name="param">Specifies the value that paramName will be set to.</param>
        public static void PointParameteri(int paramName, int param) => _glPointParameteri(paramName, param);

        /// <summary>
        /// Specify point parameters.
        /// </summary>
        /// <param name="paramName">Specifies a single-valued point parameter.<para>GL_POINT_FADE_THRESHOLD_SIZE, and POINT_SPRITE_COORD_ORIGIN are accepted.</para></param>
        /// <param name="args">A pointer to an array where the value or values to be assigned to paramName are stored.</param>
        public static void PointParameterfv(int paramName, /*const*/ float* args) => _glPointParameterfv(paramName, args);

        /// <summary>
        /// Specify point parameters.
        /// </summary>
        /// <param name="paramName">Specifies a single-valued point parameter.<para>GL_POINT_FADE_THRESHOLD_SIZE, and POINT_SPRITE_COORD_ORIGIN are accepted.</para></param>
        /// <param name="args">A pointer to an array where the value or values to be assigned to paramName are stored.</param>
        public static void PointParameteriv(int paramName, /*const*/ int* args) => _glPointParameteriv(paramName, args);

        /// <summary>
        /// Specify point parameters.
        /// </summary>
        /// <param name="paramName">Specifies a single-valued point parameter.<para>GL_POINT_FADE_THRESHOLD_SIZE, and POINT_SPRITE_COORD_ORIGIN are accepted.</para></param>
        /// <param name="args">An array of the values to be assigned.</param>
        public static void PointParameterfv(int paramName, float[] args) {
            fixed (float* a = &args[0]) {
                _glPointParameterfv(paramName, a);
            }
        }

        /// <summary>
        /// Specify point parameters.
        /// </summary>
        /// <param name="paramName">Specifies a single-valued point parameter.<para>GL_POINT_FADE_THRESHOLD_SIZE, and POINT_SPRITE_COORD_ORIGIN are accepted.</para></param>
        /// <param name="args">An array of the values to be assigned.</param>
        public static void PointParameteriv(int paramName, int[] args) {
            fixed (int* a = &args[0]) {
                _glPointParameteriv(paramName, a);
            }
        }

        /// <summary>
        ///     Set sampler parameters.
        /// </summary>
        /// <param name="sampler">Specifies the sampler object whose parameter to modify.</param>
        /// <param name="paramName">Specifies the symbolic name of a sampler parameter.</param>
        /// <param name="param">The value to set.</param>
        public static void SamplerParameteri(uint sampler, int paramName, int param) => _glSamplerParameteri(sampler, paramName, param);

        /// <summary>
        ///     Set sampler parameters.
        /// </summary>
        /// <param name="sampler">Specifies the sampler object whose parameter to modify.</param>
        /// <param name="paramName">Specifies the symbolic name of a sampler parameter.</param>
        /// <param name="param">The value to set.</param>
        public static void SamplerParameterf(uint sampler, int paramName, float param) => _glSamplerParameterf(sampler, paramName, param);

        /// <summary>
        ///     Set sampler parameters.
        /// </summary>
        /// <param name="sampler">Specifies the sampler object whose parameter to modify.</param>
        /// <param name="paramName">Specifies the symbolic name of a sampler parameter.</param>
        /// <param name="param">The value to set.</param>
        public static void SamplerParameteriv(uint sampler, int paramName, int[] param) {
            fixed (int* p = &param[0]) {
                _glSamplerParameteriv(sampler, paramName, p);
            }
        }

        /// <summary>
        ///     Set sampler parameters.
        /// </summary>
        /// <param name="sampler">Specifies the sampler object whose parameter to modify.</param>
        /// <param name="paramName">Specifies the symbolic name of a sampler parameter.</param>
        /// <param name="param">The value to set.</param>
        public static void SamplerParameterfv(uint sampler, int paramName, float[] param) {
            fixed (float* p = &param[0]) {
                _glSamplerParameterfv(sampler, paramName, p);
            }
        }

        /// <summary>
        ///     Set sampler parameters.
        /// </summary>
        /// <param name="sampler">Specifies the sampler object whose parameter to modify.</param>
        /// <param name="paramName">Specifies the symbolic name of a sampler parameter.</param>
        /// <param name="param">The value to set.</param>
        public static void SamplerParameteriv(uint sampler, int paramName, /*const*/ int* param) => _glSamplerParameteriv(sampler, paramName, param);

        /// <summary>
        ///     Set sampler parameters.
        /// </summary>
        /// <param name="sampler">Specifies the sampler object whose parameter to modify.</param>
        /// <param name="paramName">Specifies the symbolic name of a sampler parameter.</param>
        /// <param name="param">The value to set.</param>
        public static void SamplerParameterfv(uint sampler, int paramName, /*const*/ float* param) => _glSamplerParameterfv(sampler, paramName, param);

        /// <summary>
        ///     Set sampler parameters.
        /// </summary>
        /// <param name="sampler">Specifies the sampler object whose parameter to modify.</param>
        /// <param name="paramName">Specifies the symbolic name of a sampler parameter.</param>
        /// <param name="param">The value to set.</param>
        public static void SamplerParameterIiv(uint sampler, int paramName, /*const*/ int* param) => _glSamplerParameterIiv(sampler, paramName, param);

        /// <summary>
        ///     Set sampler parameters.
        /// </summary>
        /// <param name="sampler">Specifies the sampler object whose parameter to modify.</param>
        /// <param name="paramName">Specifies the symbolic name of a sampler parameter.</param>
        /// <param name="param">The value to set.</param>
        public static void SamplerParameterIuiv(uint sampler, int paramName, /*const*/ uint* param) => _glSamplerParameterIuiv(sampler, paramName, param);

        /// <summary>
        ///     Set sampler parameters.
        /// </summary>
        /// <param name="sampler">Specifies the sampler object whose parameter to modify.</param>
        /// <param name="paramName">Specifies the symbolic name of a sampler parameter.</param>
        /// <param name="param">The value to set.</param>
        public static void SamplerParameterIiv(uint sampler, int paramName, int[] param) {
            fixed (int* p = &param[0]) {
                _glSamplerParameterIiv(sampler, paramName, p);
            }
        }

        /// <summary>
        ///     Set sampler parameters.
        /// </summary>
        /// <param name="sampler">Specifies the sampler object whose parameter to modify.</param>
        /// <param name="paramName">Specifies the symbolic name of a sampler parameter.</param>
        /// <param name="param">The value to set.</param>
        public static void SamplerParameterIuiv(uint sampler, int paramName, uint[] param) {
            fixed (uint* p = &param[0]) {
                _glSamplerParameterIuiv(sampler, paramName, p);
            }
        }

        /// <summary>
        ///     Specify the value of a uniform variable for the current program object.
        /// </summary>
        /// <param name="location">Specifies the location of the uniform value to be modified.</param>
        /// <param name="v0">The value.</param>
        public static void Uniform1f(int location, float v0) => _glUniform1f(location, v0);

        /// <summary>
        ///     Specify the value of a uniform variable for the current program object.
        /// </summary>
        /// <param name="location">Specifies the location of the uniform value to be modified.</param>
        /// <param name="v0">The first value.</param>
        /// <param name="v1">The second value.</param>
        public static void Uniform2f(int location, float v0, float v1) => _glUniform2f(location, v0, v1);

        /// <summary>
        ///     Specify the value of a uniform variable for the current program object.
        /// </summary>
        /// <param name="location">Specifies the location of the uniform value to be modified.</param>
        /// <param name="v0">The first value.</param>
        /// <param name="v1">The second value.</param>
        /// <param name="v2">The third value.</param>
        public static void Uniform3f(int location, float v0, float v1, float v2) => _glUniform3f(location, v0, v1, v2);

        /// <summary>
        ///     Specify the value of a uniform variable for the current program object.
        /// </summary>
        /// <param name="location">Specifies the location of the uniform value to be modified.</param>
        /// <param name="v0">The first value.</param>
        /// <param name="v1">The second value.</param>
        /// <param name="v2">The third value.</param>
        /// <param name="v3">The fourth value.</param>
        public static void Uniform4f(int location, float v0, float v1, float v2, float v3) => _glUniform4f(location, v0, v1, v2, v3);

        /// <summary>
        ///     Specify the value of a uniform variable for the current program object.
        /// </summary>
        /// <param name="location">Specifies the location of the uniform value to be modified.</param>
        /// <param name="v0">The first value.</param>
        public static void Uniform1ui(int location, uint v0) => _glUniform1ui(location, v0);

        /// <summary>
        ///     Specify the value of a uniform variable for the current program object.
        /// </summary>
        /// <param name="location">Specifies the location of the uniform value to be modified.</param>
        /// <param name="v0">The first value.</param>
        /// <param name="v1">The second value.</param>
        public static void Uniform2ui(int location, uint v0, uint v1) => _glUniform2ui(location, v0, v1);

        /// <summary>
        ///     Specify the value of a uniform variable for the current program object.
        /// </summary>
        /// <param name="location">Specifies the location of the uniform value to be modified.</param>
        /// <param name="v0">The first value.</param>
        /// <param name="v1">The second value.</param>
        /// <param name="v2">The third value.</param>
        public static void Uniform3ui(int location, uint v0, uint v1, uint v2) => _glUniform3ui(location, v0, v1, v2);

        /// <summary>
        ///     Specify the value of a uniform variable for the current program object.
        /// </summary>
        /// <param name="location">Specifies the location of the uniform value to be modified.</param>
        /// <param name="v0">The first value.</param>
        /// <param name="v1">The second value.</param>
        /// <param name="v2">The third value.</param>
        /// <param name="v3">The fourth value.</param>
        public static void Uniform4ui(int location, uint v0, uint v1, uint v2, uint v3) => _glUniform4ui(location, v0, v1, v2, v3);

        /// <summary>
        ///     Specify the value of a uniform variable for the current program object.
        /// </summary>
        /// <param name="location">Specifies the location of the uniform value to be modified.</param>
        /// <param name="v0">The first value.</param>
        public static void Uniform1i(int location, int v0) => _glUniform1i(location, v0);

        /// <summary>
        ///     Specify the value of a uniform variable for the current program object.
        /// </summary>
        /// <param name="location">Specifies the location of the uniform value to be modified.</param>
        /// <param name="v0">The first value.</param>
        /// <param name="v1">The second value.</param>
        public static void Uniform2i(int location, int v0, int v1) => _glUniform2i(location, v0, v1);

        /// <summary>
        ///     Specify the value of a uniform variable for the current program object.
        /// </summary>
        /// <param name="location">Specifies the location of the uniform value to be modified.</param>
        /// <param name="v0">The first value.</param>
        /// <param name="v1">The second value.</param>
        /// <param name="v2">The third value.</param>
        public static void Uniform3i(int location, int v0, int v1, int v2) => _glUniform3i(location, v0, v1, v2);

        /// <summary>
        ///     Specify the value of a uniform variable for the current program object.
        /// </summary>
        /// <param name="location">Specifies the location of the uniform value to be modified.</param>
        /// <param name="v0">The first value.</param>
        /// <param name="v1">The second value.</param>
        /// <param name="v2">The third value.</param>
        /// <param name="v3">The fourth value.</param>
        public static void Uniform4i(int location, int v0, int v1, int v2, int v3) => _glUniform4i(location, v0, v1, v2, v3);

        /// <summary>
        ///     Specify the value of a uniform variable for the current program object.
        /// </summary>
        /// <param name="location">Specifies the location of the uniform value to be modified.</param>
        /// <param name="count">Specifies the number of elements that are to be modified.</param>
        /// <param name="value">The values to set.</param>
        public static void Uniform1fv(int location, int count, /*const*/ float* value) => _glUniform1fv(location, count, value);

        /// <summary>
        ///     Specify the value of a uniform variable for the current program object.
        /// </summary>
        /// <param name="location">Specifies the location of the uniform value to be modified.</param>
        /// <param name="count">Specifies the number of elements that are to be modified.</param>
        /// <param name="value">The values to set.</param>
        public static void Uniform2fv(int location, int count, /*const*/ float* value) => _glUniform2fv(location, count, value);

        /// <summary>
        ///     Specify the value of a uniform variable for the current program object.
        /// </summary>
        /// <param name="location">Specifies the location of the uniform value to be modified.</param>
        /// <param name="count">Specifies the number of elements that are to be modified.</param>
        /// <param name="value">The values to set.</param>
        public static void Uniform3fv(int location, int count, /*const*/ float* value) => _glUniform3fv(location, count, value);

        /// <summary>
        ///     Specify the value of a uniform variable for the current program object.
        /// </summary>
        /// <param name="location">Specifies the location of the uniform value to be modified.</param>
        /// <param name="count">Specifies the number of elements that are to be modified.</param>
        /// <param name="value">The values to set.</param>
        public static void Uniform4fv(int location, int count, /*const*/ float* value) => _glUniform4fv(location, count, value);

        /// <summary>
        ///     Specify the value of a uniform variable for the current program object.
        /// </summary>
        /// <param name="location">Specifies the location of the uniform value to be modified.</param>
        /// <param name="count">Specifies the number of elements that are to be modified.</param>
        /// <param name="value">The values to set.</param>
        public static void Uniform1fv(int location, int count, float[] value) {
            fixed (float* v = &value[0]) {
                _glUniform1fv(location, count, v);
            }
        }

        /// <summary>
        ///     Specify the value of a uniform variable for the current program object.
        /// </summary>
        /// <param name="location">Specifies the location of the uniform value to be modified.</param>
        /// <param name="count">Specifies the number of elements that are to be modified.</param>
        /// <param name="value">The values to set.</param>
        public static void Uniform2fv(int location, int count, float[] value) {
            fixed (float* v = &value[0]) {
                _glUniform2fv(location, count, v);
            }
        }

        /// <summary>
        ///     Specify the value of a uniform variable for the current program object.
        /// </summary>
        /// <param name="location">Specifies the location of the uniform value to be modified.</param>
        /// <param name="count">Specifies the number of elements that are to be modified.</param>
        /// <param name="value">The values to set.</param>
        public static void Uniform3fv(int location, int count, float[] value) {
            fixed (float* v = &value[0]) {
                _glUniform3fv(location, count, v);
            }
        }

        /// <summary>
        ///     Specify the value of a uniform variable for the current program object.
        /// </summary>
        /// <param name="location">Specifies the location of the uniform value to be modified.</param>
        /// <param name="count">Specifies the number of elements that are to be modified.</param>
        /// <param name="value">The values to set.</param>
        public static void Uniform4fv(int location, int count, float[] value) {
            fixed (float* v = &value[0]) {
                _glUniform4fv(location, count, v);
            }
        }

        /// <summary>
        ///     Specify the value of a uniform variable for the current program object.
        /// </summary>
        /// <param name="location">Specifies the location of the uniform value to be modified.</param>
        /// <param name="count">Specifies the number of elements that are to be modified.</param>
        /// <param name="value">The values to set.</param>
        public static void Uniform1iv(int location, int count, /*const*/ int* value) => _glUniform1iv(location, count, value);

        /// <summary>
        ///     Specify the value of a uniform variable for the current program object.
        /// </summary>
        /// <param name="location">Specifies the location of the uniform value to be modified.</param>
        /// <param name="count">Specifies the number of elements that are to be modified.</param>
        /// <param name="value">The values to set.</param>
        public static void Uniform2iv(int location, int count, /*const*/ int* value) => _glUniform2iv(location, count, value);

        /// <summary>
        ///     Specify the value of a uniform variable for the current program object.
        /// </summary>
        /// <param name="location">Specifies the location of the uniform value to be modified.</param>
        /// <param name="count">Specifies the number of elements that are to be modified.</param>
        /// <param name="value">The values to set.</param>
        public static void Uniform3iv(int location, int count, /*const*/ int* value) => _glUniform3iv(location, count, value);

        /// <summary>
        ///     Specify the value of a uniform variable for the current program object.
        /// </summary>
        /// <param name="location">Specifies the location of the uniform value to be modified.</param>
        /// <param name="count">Specifies the number of elements that are to be modified.</param>
        /// <param name="value">The values to set.</param>
        public static void Uniform4iv(int location, int count, /*const*/ int* value) => _glUniform4iv(location, count, value);

        /// <summary>
        ///     Specify the value of a uniform variable for the current program object.
        /// </summary>
        /// <param name="location">Specifies the location of the uniform value to be modified.</param>
        /// <param name="count">Specifies the number of elements that are to be modified.</param>
        /// <param name="value">The values to set.</param>
        public static void Uniform1iv(int location, int count, int[] value) {
            fixed (int* v = &value[0]) {
                _glUniform1iv(location, count, v);
            }
        }

        /// <summary>
        ///     Specify the value of a uniform variable for the current program object.
        /// </summary>
        /// <param name="location">Specifies the location of the uniform value to be modified.</param>
        /// <param name="count">Specifies the number of elements that are to be modified.</param>
        /// <param name="value">The values to set.</param>
        public static void Uniform2iv(int location, int count, int[] value) {
            fixed (int* v = &value[0]) {
                _glUniform2iv(location, count, v);
            }
        }

        /// <summary>
        ///     Specify the value of a uniform variable for the current program object.
        /// </summary>
        /// <param name="location">Specifies the location of the uniform value to be modified.</param>
        /// <param name="count">Specifies the number of elements that are to be modified.</param>
        /// <param name="value">The values to set.</param>
        public static void Uniform3iv(int location, int count, int[] value) {
            fixed (int* v = &value[0]) {
                _glUniform3iv(location, count, v);
            }
        }

        /// <summary>
        ///     Specify the value of a uniform variable for the current program object.
        /// </summary>
        /// <param name="location">Specifies the location of the uniform value to be modified.</param>
        /// <param name="count">Specifies the number of elements that are to be modified.</param>
        /// <param name="value">The values to set.</param>
        public static void Uniform4iv(int location, int count, int[] value) {
            fixed (int* v = &value[0]) {
                _glUniform4iv(location, count, v);
            }
        }

        /// <summary>
        ///     Specify the value of a uniform variable for the current program object.
        /// </summary>
        /// <param name="location">Specifies the location of the uniform value to be modified.</param>
        /// <param name="count">Specifies the number of elements that are to be modified.</param>
        /// <param name="value">The values to set.</param>
        public static void Uniform1uiv(int location, int count, /*const*/ uint* value) => _glUniform1uiv(location, count, value);

        /// <summary>
        ///     Specify the value of a uniform variable for the current program object.
        /// </summary>
        /// <param name="location">Specifies the location of the uniform value to be modified.</param>
        /// <param name="count">Specifies the number of elements that are to be modified.</param>
        /// <param name="value">The values to set.</param>
        public static void Uniform2uiv(int location, int count, /*const*/ uint* value) => _glUniform2uiv(location, count, value);

        /// <summary>
        ///     Specify the value of a uniform variable for the current program object.
        /// </summary>
        /// <param name="location">Specifies the location of the uniform value to be modified.</param>
        /// <param name="count">Specifies the number of elements that are to be modified.</param>
        /// <param name="value">The values to set.</param>
        public static void Uniform3uiv(int location, int count, /*const*/ uint* value) => _glUniform3uiv(location, count, value);

        /// <summary>
        ///     Specify the value of a uniform variable for the current program object.
        /// </summary>
        /// <param name="location">Specifies the location of the uniform value to be modified.</param>
        /// <param name="count">Specifies the number of elements that are to be modified.</param>
        /// <param name="value">The values to set.</param>
        public static void Uniform4uiv(int location, int count, /*const*/ uint* value) => _glUniform4uiv(location, count, value);

        /// <summary>
        ///     Specify the value of a uniform variable for the current program object.
        /// </summary>
        /// <param name="location">Specifies the location of the uniform value to be modified.</param>
        /// <param name="count">Specifies the number of elements that are to be modified.</param>
        /// <param name="value">The values to set.</param>
        public static void Uniform1uiv(int location, int count, uint[] value) {
            fixed (uint* v = &value[0]) {
                _glUniform1uiv(location, count, v);
            }
        }

        /// <summary>
        ///     Specify the value of a uniform variable for the current program object.
        /// </summary>
        /// <param name="location">Specifies the location of the uniform value to be modified.</param>
        /// <param name="count">Specifies the number of elements that are to be modified.</param>
        /// <param name="value">The values to set.</param>
        public static void Uniform2uiv(int location, int count, uint[] value) {
            fixed (uint* v = &value[0]) {
                _glUniform2uiv(location, count, v);
            }
        }

        /// <summary>
        ///     Specify the value of a uniform variable for the current program object.
        /// </summary>
        /// <param name="location">Specifies the location of the uniform value to be modified.</param>
        /// <param name="count">Specifies the number of elements that are to be modified.</param>
        /// <param name="value">The values to set.</param>
        public static void Uniform3uiv(int location, int count, uint[] value) {
            fixed (uint* v = &value[0]) {
                _glUniform3uiv(location, count, v);
            }
        }

        /// <summary>
        ///     Specify the value of a uniform variable for the current program object.
        /// </summary>
        /// <param name="location">Specifies the location of the uniform value to be modified.</param>
        /// <param name="count">Specifies the number of elements that are to be modified.</param>
        /// <param name="value">The values to set.</param>
        public static void Uniform4uiv(int location, int count, uint[] value) {
            fixed (uint* v = &value[0]) {
                _glUniform4uiv(location, count, v);
            }
        }

        /// <summary>
        ///     Set the current texture coordinates.
        /// </summary>
        /// <param name="texture">
        ///     Specifies the texture unit whose coordinates should be modified.
        ///     <para>
        ///         The number of texture units is implementation dependent, but must be at least two. Symbolic constant must be
        ///         one of TEXTUREi, where i ranges from 0 to MAX_TEXTURE_COORDS - 1, which is an implementation-dependent
        ///         value.
        ///     </para>
        /// </param>
        /// <param name="type">The data type.</param>
        /// <param name="coords">The value of the coordinates to set.</param>
        public static void MultiTexCoordP1ui(int texture, int type, uint coords) => _glMultiTexCoordP1ui(texture, type, coords);

        /// <summary>
        ///     Set the current texture coordinates.
        /// </summary>
        /// <param name="texture">
        ///     Specifies the texture unit whose coordinates should be modified.
        ///     <para>
        ///         The number of texture units is implementation dependent, but must be at least two. Symbolic constant must be
        ///         one of TEXTUREi, where i ranges from 0 to MAX_TEXTURE_COORDS - 1, which is an implementation-dependent
        ///         value.
        ///     </para>
        /// </param>
        /// <param name="type">The data type.</param>
        /// <param name="coords">The value of the coordinates to set.</param>
        public static void MultiTexCoordP2ui(int texture, int type, uint coords) => _glMultiTexCoordP2ui(texture, type, coords);

        /// <summary>
        ///     Set the current texture coordinates.
        /// </summary>
        /// <param name="texture">
        ///     Specifies the texture unit whose coordinates should be modified.
        ///     <para>
        ///         The number of texture units is implementation dependent, but must be at least two. Symbolic constant must be
        ///         one of TEXTUREi, where i ranges from 0 to MAX_TEXTURE_COORDS - 1, which is an implementation-dependent
        ///         value.
        ///     </para>
        /// </param>
        /// <param name="type">The data type.</param>
        /// <param name="coords">The value of the coordinates to set.</param>
        public static void MultiTexCoordP3ui(int texture, int type, uint coords) => _glMultiTexCoordP3ui(texture, type, coords);

        /// <summary>
        ///     Set the current texture coordinates.
        /// </summary>
        /// <param name="texture">
        ///     Specifies the texture unit whose coordinates should be modified.
        ///     <para>
        ///         The number of texture units is implementation dependent, but must be at least two. Symbolic constant must be
        ///         one of TEXTUREi, where i ranges from 0 to MAX_TEXTURE_COORDS - 1, which is an implementation-dependent
        ///         value.
        ///     </para>
        /// </param>
        /// <param name="type">The data type.</param>
        /// <param name="coords">The value of the coordinates to set.</param>
        public static void MultiTexCoordP4ui(int texture, int type, uint coords) => _glMultiTexCoordP4ui(texture, type, coords);

        /// <summary>
        ///     Set the current texture coordinates.
        /// </summary>
        /// <param name="texture">
        ///     Specifies the texture unit whose coordinates should be modified.
        ///     <para>
        ///         The number of texture units is implementation dependent, but must be at least two. Symbolic constant must be
        ///         one of TEXTUREi, where i ranges from 0 to MAX_TEXTURE_COORDS - 1, which is an implementation-dependent
        ///         value.
        ///     </para>
        /// </param>
        /// <param name="type">The data type.</param>
        /// <param name="coords">The value of the coordinates to set.</param>
        public static void MultiTexCoordP1uiv(int texture, int type, /*const*/ uint* coords) => _glMultiTexCoordP1uiv(texture, type, coords);

        /// <summary>
        ///     Set the current texture coordinates.
        /// </summary>
        /// <param name="texture">
        ///     Specifies the texture unit whose coordinates should be modified.
        ///     <para>
        ///         The number of texture units is implementation dependent, but must be at least two. Symbolic constant must be
        ///         one of TEXTUREi, where i ranges from 0 to MAX_TEXTURE_COORDS - 1, which is an implementation-dependent
        ///         value.
        ///     </para>
        /// </param>
        /// <param name="type">The data type.</param>
        /// <param name="coords">The value of the coordinates to set.</param>
        public static void MultiTexCoordP2uiv(int texture, int type, /*const*/ uint* coords) => _glMultiTexCoordP2uiv(texture, type, coords);

        /// <summary>
        ///     Set the current texture coordinates.
        /// </summary>
        /// <param name="texture">
        ///     Specifies the texture unit whose coordinates should be modified.
        ///     <para>
        ///         The number of texture units is implementation dependent, but must be at least two. Symbolic constant must be
        ///         one of TEXTUREi, where i ranges from 0 to MAX_TEXTURE_COORDS - 1, which is an implementation-dependent
        ///         value.
        ///     </para>
        /// </param>
        /// <param name="type">The data type.</param>
        /// <param name="coords">The value of the coordinates to set.</param>
        public static void MultiTexCoordP3uiv(int texture, int type, /*const*/ uint* coords) => _glMultiTexCoordP3uiv(texture, type, coords);

        /// <summary>
        ///     Set the current texture coordinates.
        /// </summary>
        /// <param name="texture">
        ///     Specifies the texture unit whose coordinates should be modified.
        ///     <para>
        ///         The number of texture units is implementation dependent, but must be at least two. Symbolic constant must be
        ///         one of TEXTUREi, where i ranges from 0 to MAX_TEXTURE_COORDS - 1, which is an implementation-dependent
        ///         value.
        ///     </para>
        /// </param>
        /// <param name="type">The data type.</param>
        /// <param name="coords">The value of the coordinates to set.</param>
        public static void MultiTexCoordP4uiv(int texture, int type, /*const*/ uint* coords) => _glMultiTexCoordP4uiv(texture, type, coords);

        /// <summary>
        ///     Set the current texture coordinates.
        /// </summary>
        /// <param name="texture">
        ///     Specifies the texture unit whose coordinates should be modified.
        ///     <para>
        ///         The number of texture units is implementation dependent, but must be at least two. Symbolic constant must be
        ///         one of TEXTUREi, where i ranges from 0 to MAX_TEXTURE_COORDS - 1, which is an implementation-dependent
        ///         value.
        ///     </para>
        /// </param>
        /// <param name="type">The data type.</param>
        /// <param name="coords">The value of the coordinates to set.</param>
        public static void MultiTexCoordP1uiv(int texture, int type, uint[] coords) {
            fixed (uint* c = &coords[0]) {
                _glMultiTexCoordP1uiv(texture, type, c);
            }
        }

        /// <summary>
        ///     Set the current texture coordinates.
        /// </summary>
        /// <param name="texture">
        ///     Specifies the texture unit whose coordinates should be modified.
        ///     <para>
        ///         The number of texture units is implementation dependent, but must be at least two. Symbolic constant must be
        ///         one of TEXTUREi, where i ranges from 0 to MAX_TEXTURE_COORDS - 1, which is an implementation-dependent
        ///         value.
        ///     </para>
        /// </param>
        /// <param name="type">The data type.</param>
        /// <param name="coords">The value of the coordinates to set.</param>
        public static void MultiTexCoordP2uiv(int texture, int type, uint[] coords) {
            fixed (uint* c = &coords[0]) {
                _glMultiTexCoordP2uiv(texture, type, c);
            }
        }

        /// <summary>
        ///     Set the current texture coordinates.
        /// </summary>
        /// <param name="texture">
        ///     Specifies the texture unit whose coordinates should be modified.
        ///     <para>
        ///         The number of texture units is implementation dependent, but must be at least two. Symbolic constant must be
        ///         one of TEXTUREi, where i ranges from 0 to MAX_TEXTURE_COORDS - 1, which is an implementation-dependent
        ///         value.
        ///     </para>
        /// </param>
        /// <param name="type">The data type.</param>
        /// <param name="coords">The value of the coordinates to set.</param>
        public static void MultiTexCoordP3uiv(int texture, int type, uint[] coords) {
            fixed (uint* c = &coords[0]) {
                _glMultiTexCoordP3uiv(texture, type, c);
            }
        }

        /// <summary>
        ///     Set the current texture coordinates.
        /// </summary>
        /// <param name="texture">
        ///     Specifies the texture unit whose coordinates should be modified.
        ///     <para>
        ///         The number of texture units is implementation dependent, but must be at least two. Symbolic constant must be
        ///         one of TEXTUREi, where i ranges from 0 to MAX_TEXTURE_COORDS - 1, which is an implementation-dependent
        ///         value.
        ///     </para>
        /// </param>
        /// <param name="type">The data type.</param>
        /// <param name="coords">The value of the coordinates to set.</param>
        public static void MultiTexCoordP4uiv(int texture, int type, uint[] coords) {
            fixed (uint* c = &coords[0]) {
                _glMultiTexCoordP4uiv(texture, type, c);
            }
        }

        /// <summary>
        /// Set the current texture coordinates.
        /// </summary>
        /// <param name="type">Specifies the texture unit whose coordinates should be modified.<para>The number of texture units is implementation dependent, but must be at least two. Symbolic constant must be one of TEXTUREi, where i ranges from 0 to MAX_TEXTURE_COORDS - 1, which is an implementation-dependent value.</para></param>
        /// <param name="coords">A packed value.</param>
        public static void TexCoordP1ui(int type, uint coords) => _glTexCoordP1ui(type, coords);

        /// <summary>
        /// Set the current texture coordinates.
        /// </summary>
        /// <param name="type">Specifies the texture unit whose coordinates should be modified.<para>The number of texture units is implementation dependent, but must be at least two. Symbolic constant must be one of TEXTUREi, where i ranges from 0 to MAX_TEXTURE_COORDS - 1, which is an implementation-dependent value.</para></param>
        /// <param name="coords">Specifies a pointer to an array of packed elements.</param>
        public static void TexCoordP1uiv(int type, /*const*/ uint* coords) => _glTexCoordP1uiv(type, coords);

        /// <summary>
        /// Set the current texture coordinates.
        /// </summary>
        /// <param name="type">Specifies the texture unit whose coordinates should be modified.<para>The number of texture units is implementation dependent, but must be at least two. Symbolic constant must be one of TEXTUREi, where i ranges from 0 to MAX_TEXTURE_COORDS - 1, which is an implementation-dependent value.</para></param>
        /// <param name="coords">An array of packed elements.</param>
        public static void TexCoordP1uiv(int type, uint[] coords) {
            fixed (uint* c = &coords[0]) {
                _glTexCoordP1uiv(type, c);
            }
        }

        /// <summary>
        /// Set the current texture coordinates.
        /// </summary>
        /// <param name="type">Specifies the texture unit whose coordinates should be modified.<para>The number of texture units is implementation dependent, but must be at least two. Symbolic constant must be one of TEXTUREi, where i ranges from 0 to MAX_TEXTURE_COORDS - 1, which is an implementation-dependent value.</para></param>
        /// <param name="coords">A packed value.</param>
        public static void TexCoordP2ui(int type, uint coords) => _glTexCoordP2ui(type, coords);

        /// <summary>
        /// Set the current texture coordinates.
        /// </summary>
        /// <param name="type">Specifies the texture unit whose coordinates should be modified.<para>The number of texture units is implementation dependent, but must be at least two. Symbolic constant must be one of TEXTUREi, where i ranges from 0 to MAX_TEXTURE_COORDS - 1, which is an implementation-dependent value.</para></param>
        /// <param name="coords">An array of packed elements.</param>
        public static void TexCoordP2uiv(int type, /*const*/ uint* coords) => _glTexCoordP2uiv(type, coords);

        /// <summary>
        /// Set the current texture coordinates.
        /// </summary>
        /// <param name="type">Specifies the texture unit whose coordinates should be modified.<para>The number of texture units is implementation dependent, but must be at least two. Symbolic constant must be one of TEXTUREi, where i ranges from 0 to MAX_TEXTURE_COORDS - 1, which is an implementation-dependent value.</para></param>
        /// <param name="coords">An array of packed elements.</param>
        public static void TexCoordP2uiv(int type, uint[] coords) {
            fixed (uint* c = &coords[0]) {
                _glTexCoordP2uiv(type, c);
            }
        }

        /// <summary>
        /// Set the current texture coordinates.
        /// </summary>
        /// <param name="type">Specifies the texture unit whose coordinates should be modified.<para>The number of texture units is implementation dependent, but must be at least two. Symbolic constant must be one of TEXTUREi, where i ranges from 0 to MAX_TEXTURE_COORDS - 1, which is an implementation-dependent value.</para></param>
        /// <param name="coords">A packed value.</param>
        public static void TexCoordP3ui(int type, uint coords) => _glTexCoordP3ui(type, coords);

        /// <summary>
        /// Set the current texture coordinates.
        /// </summary>
        /// <param name="type">Specifies the texture unit whose coordinates should be modified.<para>The number of texture units is implementation dependent, but must be at least two. Symbolic constant must be one of TEXTUREi, where i ranges from 0 to MAX_TEXTURE_COORDS - 1, which is an implementation-dependent value.</para></param>
        /// <param name="coords">An array of packed elements.</param>
        public static void TexCoordP3uiv(int type, /*const*/ uint* coords) => _glTexCoordP3uiv(type, coords);

        /// <summary>
        /// Set the current texture coordinates.
        /// </summary>
        /// <param name="type">Specifies the texture unit whose coordinates should be modified.<para>The number of texture units is implementation dependent, but must be at least two. Symbolic constant must be one of TEXTUREi, where i ranges from 0 to MAX_TEXTURE_COORDS - 1, which is an implementation-dependent value.</para></param>
        /// <param name="coords">An array of packed elements.</param>
        public static void TexCoordP3uiv(int type, uint[] coords) {
            fixed (uint* c = &coords[0]) {
                _glTexCoordP3uiv(type, c);
            }
        }

        /// <summary>
        /// Set the current texture coordinates.
        /// </summary>
        /// <param name="type">Specifies the texture unit whose coordinates should be modified.<para>The number of texture units is implementation dependent, but must be at least two. Symbolic constant must be one of TEXTUREi, where i ranges from 0 to MAX_TEXTURE_COORDS - 1, which is an implementation-dependent value.</para></param>
        /// <param name="coords">A packed value.</param>
        public static void TexCoordP4ui(int type, uint coords) => _glTexCoordP4ui(type, coords);

        /// <summary>
        /// Set the current texture coordinates.
        /// </summary>
        /// <param name="type">Specifies the texture unit whose coordinates should be modified.<para>The number of texture units is implementation dependent, but must be at least two. Symbolic constant must be one of TEXTUREi, where i ranges from 0 to MAX_TEXTURE_COORDS - 1, which is an implementation-dependent value.</para></param>
        /// <param name="coords">An array of packed elements.</param>
        public static void TexCoordP4uiv(int type, /*const*/ uint* coords) => _glTexCoordP4uiv(type, coords);

        /// <summary>
        /// Set the current texture coordinates.
        /// </summary>
        /// <param name="type">Specifies the texture unit whose coordinates should be modified.<para>The number of texture units is implementation dependent, but must be at least two. Symbolic constant must be one of TEXTUREi, where i ranges from 0 to MAX_TEXTURE_COORDS - 1, which is an implementation-dependent value.</para></param>
        /// <param name="coords">An array of packed elements.</param>
        public static void TexCoordP4uiv(int type, uint[] coords) {
            fixed (uint* c = &coords[0]) {
                _glTexCoordP4uiv(type, c);
            }
        }

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="x">The value.</param>
        public static void VertexAttrib1d(uint index, double x) => _glVertexAttrib1d(index, x);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="x">The value.</param>
        public static void VertexAttrib1f(uint index, float x) => _glVertexAttrib1f(index, x);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="x">The value.</param>
        public static void VertexAttrib1s(uint index, short x) => _glVertexAttrib1s(index, x);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="x">The first value.</param>
        /// <param name="y">The second value.</param>
        public static void VertexAttrib2d(uint index, double x, double y) => _glVertexAttrib2d(index, x, y);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="x">The first value.</param>
        /// <param name="y">The second value.</param>
        public static void VertexAttrib2f(uint index, float x, float y) => _glVertexAttrib2f(index, x, y);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="x">The first value.</param>
        /// <param name="y">The second value.</param>
        public static void VertexAttrib2s(uint index, short x, short y) => _glVertexAttrib2s(index, x, y);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="x">The first value.</param>
        /// <param name="y">The second value.</param>
        /// <param name="z">The third value.</param>
        public static void VertexAttrib3d(uint index, double x, double y, double z) => _glVertexAttrib3d(index, x, y, z);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="x">The first value.</param>
        /// <param name="y">The second value.</param>
        /// <param name="z">The third value.</param>
        public static void VertexAttrib3f(uint index, float x, float y, float z) => _glVertexAttrib3f(index, x, y, z);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="x">The first value.</param>
        /// <param name="y">The second value.</param>
        /// <param name="z">The third value.</param>
        public static void VertexAttrib3s(uint index, short x, short y, short z) => _glVertexAttrib3s(index, x, y, z);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="x">The first value.</param>
        /// <param name="y">The second value.</param>
        /// <param name="z">The third value.</param>
        /// <param name="w">The fourth value.</param>
        public static void VertexAttrib4Nub(uint index, byte x, byte y, byte z, byte w) => _glVertexAttrib4Nub(index, x, y, z, w);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="x">The first value.</param>
        /// <param name="y">The second value.</param>
        /// <param name="z">The third value.</param>
        /// <param name="w">The fourth value.</param>
        public static void VertexAttrib4d(uint index, double x, double y, double z, double w) => _glVertexAttrib4d(index, x, y, z, w);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="x">The first value.</param>
        /// <param name="y">The second value.</param>
        /// <param name="z">The third value.</param>
        /// <param name="w">The fourth value.</param>
        public static void VertexAttrib4f(uint index, float x, float y, float z, float w) => _glVertexAttrib4f(index, x, y, z, w);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="x">The first value.</param>
        /// <param name="y">The second value.</param>
        /// <param name="z">The third value.</param>
        /// <param name="w">The fourth value.</param>
        public static void VertexAttrib4s(uint index, short x, short y, short z, short w) => _glVertexAttrib4s(index, x, y, z, w);

        /// <summary>
        ///     Disable a generic vertex attribute array.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be disabled.</param>
        public static void DisableVertexAttribArray(uint index) => _glDisableVertexAttribArray(index);

        /// <summary>
        ///     Enable a generic vertex attribute array.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be disabled.</param>
        public static void EnableVertexAttribArray(uint index) => _glEnableVertexAttribArray(index);

        /// <summary>
        ///     Specify the primitive restart index.
        /// </summary>
        /// <param name="index">Specifies the value to be interpreted as the primitive restart index.</param>
        public static void PrimitiveRestartIndex(uint index) => _glPrimitiveRestartIndex(index);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="v">A pointer to the vertex data.</param>
        public static void VertexAttrib1dv(uint index, /*const*/ double* v) => _glVertexAttrib1dv(index, v);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="v">A pointer to the vertex data.</param>
        public static void VertexAttrib1fv(uint index, /*const*/ float* v) => _glVertexAttrib1fv(index, v);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="v">A pointer to the vertex data.</param>
        public static void VertexAttrib1sv(uint index, /*const*/ short* v) => _glVertexAttrib1sv(index, v);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="v">A pointer to the vertex data.</param>
        public static void VertexAttrib2dv(uint index, /*const*/ double* v) => _glVertexAttrib2dv(index, v);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="v">A pointer to the vertex data.</param>
        public static void VertexAttrib2fv(uint index, /*const*/ float* v) => _glVertexAttrib2fv(index, v);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="v">A pointer to the vertex data.</param>
        public static void VertexAttrib2sv(uint index, /*const*/ short* v) => _glVertexAttrib2sv(index, v);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="v">A pointer to the vertex data.</param>
        public static void VertexAttrib3dv(uint index, /*const*/ double* v) => _glVertexAttrib3dv(index, v);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="v">A pointer to the vertex data.</param>
        public static void VertexAttrib3fv(uint index, /*const*/ float* v) => _glVertexAttrib3fv(index, v);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="v">A pointer to the vertex data.</param>
        public static void VertexAttrib3sv(uint index, /*const*/ short* v) => _glVertexAttrib3sv(index, v);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="v">A pointer to the vertex data.</param>
        public static void VertexAttrib4bv(uint index, /*const*/ sbyte* v) => _glVertexAttrib4bv(index, v);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="v">A pointer to the vertex data.</param>
        public static void VertexAttrib4dv(uint index, /*const*/ double* v) => _glVertexAttrib4dv(index, v);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="v">A pointer to the vertex data.</param>
        public static void VertexAttrib4fv(uint index, /*const*/ float* v) => _glVertexAttrib4fv(index, v);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="v">A pointer to the vertex data.</param>
        public static void VertexAttrib4iv(uint index, /*const*/ int* v) => _glVertexAttrib4iv(index, v);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="v">A pointer to the vertex data.</param>
        public static void VertexAttrib4sv(uint index, /*const*/ short* v) => _glVertexAttrib4sv(index, v);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="v">A pointer to the vertex data.</param>
        public static void VertexAttrib4ubv(uint index, /*const*/ byte* v) => _glVertexAttrib4ubv(index, v);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="v">A pointer to the vertex data.</param>
        public static void VertexAttrib4uiv(uint index, /*const*/ uint* v) => _glVertexAttrib4uiv(index, v);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="v">A pointer to the vertex data.</param>
        public static void VertexAttrib4usv(uint index, /*const*/ ushort* v) => _glVertexAttrib4usv(index, v);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="value">The vertex data.</param>
        public static void VertexAttrib1dv(uint index, double[] value) {
            fixed (double* v = &value[0]) {
                _glVertexAttrib1dv(index, v);
            }
        }

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="value">The vertex data.</param>
        public static void VertexAttrib1fv(uint index, float[] value) {
            fixed (float* v = &value[0]) {
                _glVertexAttrib1fv(index, v);
            }
        }

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="value">The vertex data.</param>
        public static void VertexAttrib1sv(uint index, short[] value) {
            fixed (short* v = &value[0]) {
                _glVertexAttrib1sv(index, v);
            }
        }

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="value">The vertex data.</param>
        public static void VertexAttrib2dv(uint index, double[] value) {
            fixed (double* v = &value[0]) {
                _glVertexAttrib2dv(index, v);
            }
        }

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="value">The vertex data.</param>
        public static void VertexAttrib2fv(uint index, float[] value) {
            fixed (float* v = &value[0]) {
                _glVertexAttrib2fv(index, v);
            }
        }

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="value">The vertex data.</param>
        public static void VertexAttrib2sv(uint index, short[] value) {
            fixed (short* v = &value[0]) {
                _glVertexAttrib2sv(index, v);
            }
        }

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="value">The vertex data.</param>
        public static void VertexAttrib3dv(uint index, double[] value) {
            fixed (double* v = &value[0]) {
                _glVertexAttrib3dv(index, v);
            }
        }

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="value">The vertex data.</param>
        public static void VertexAttrib3fv(uint index, float[] value) {
            fixed (float* v = &value[0]) {
                _glVertexAttrib3fv(index, v);
            }
        }

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="value">The vertex data.</param>
        public static void VertexAttrib3sv(uint index, short[] value) {
            fixed (short* v = &value[0]) {
                _glVertexAttrib3sv(index, v);
            }
        }

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="value">The vertex data.</param>
        public static void VertexAttrib4bv(uint index, sbyte[] value) {
            fixed (sbyte* v = &value[0]) {
                _glVertexAttrib4bv(index, v);
            }
        }

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="value">The vertex data.</param>
        public static void VertexAttrib4dv(uint index, double[] value) {
            fixed (double* v = &value[0]) {
                _glVertexAttrib4dv(index, v);
            }
        }

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="value">The vertex data.</param>
        public static void VertexAttrib4fv(uint index, float[] value) {
            fixed (float* v = &value[0]) {
                _glVertexAttrib4fv(index, v);
            }
        }

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="value">The vertex data.</param>
        public static void VertexAttrib4iv(uint index, int[] value) {
            fixed (int* v = &value[0]) {
                _glVertexAttrib4iv(index, v);
            }
        }

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="value">The vertex data.</param>
        public static void VertexAttrib4sv(uint index, short[] value) {
            fixed (short* v = &value[0]) {
                _glVertexAttrib4sv(index, v);
            }
        }

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="value">The vertex data.</param>
        public static void VertexAttrib4ubv(uint index, byte[] value) {
            fixed (byte* v = &value[0]) {
                _glVertexAttrib4ubv(index, v);
            }
        }

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="value">The vertex data.</param>
        public static void VertexAttrib4uiv(uint index, uint[] value) {
            fixed (uint* v = &value[0]) {
                _glVertexAttrib4uiv(index, v);
            }
        }

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="value">The vertex data.</param>
        public static void VertexAttrib4usv(uint index, ushort[] value) {
            fixed (ushort* v = &value[0]) {
                _glVertexAttrib4usv(index, v);
            }
        }

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="v">A pointer to the vertex data.</param>
        /// <remarks>Values will be scaled to normalized values.</remarks>
        public static void VertexAttrib4Nbv(uint index, /*const*/ sbyte* v) => _glVertexAttrib4Nbv(index, v);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="v">A pointer to the vertex data.</param>
        /// <remarks>Values will be scaled to normalized values.</remarks>
        public static void VertexAttrib4Niv(uint index, /*const*/ int* v) => _glVertexAttrib4Niv(index, v);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="v">A pointer to the vertex data.</param>
        /// <remarks>Values will be scaled to normalized values.</remarks>
        public static void VertexAttrib4Nsv(uint index, /*const*/ short* v) => _glVertexAttrib4Nsv(index, v);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="v">A pointer to the vertex data.</param>
        /// <remarks>Values will be scaled to normalized values.</remarks>
        public static void VertexAttrib4Nubv(uint index, /*const*/ byte* v) => _glVertexAttrib4Nubv(index, v);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="v">A pointer to the vertex data.</param>
        /// <remarks>Values will be scaled to normalized values.</remarks>
        public static void VertexAttrib4Nuiv(uint index, /*const*/ uint* v) => _glVertexAttrib4Nuiv(index, v);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="v">A pointer to the vertex data.</param>
        /// <remarks>Values will be scaled to normalized values.</remarks>
        public static void VertexAttrib4Nusv(uint index, /*const*/ ushort* v) => _glVertexAttrib4Nusv(index, v);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="value">The vertex data.</param>
        /// <remarks>Values will be scaled to normalized values.</remarks>
        public static void VertexAttrib4Nbv(uint index, sbyte[] value) {
            fixed (sbyte* v = &value[0]) {
                _glVertexAttrib4Nbv(index, v);
            }
        }

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="value">The vertex data.</param>
        /// <remarks>Values will be scaled to normalized values.</remarks>
        public static void VertexAttrib4Niv(uint index, int[] value) {
            fixed (int* v = &value[0]) {
                _glVertexAttrib4Niv(index, v);
            }
        }

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="value">The vertex data.</param>
        /// <remarks>Values will be scaled to normalized values.</remarks>
        public static void VertexAttrib4Nsv(uint index, short[] value) {
            fixed (short* v = &value[0]) {
                _glVertexAttrib4Nsv(index, v);
            }
        }

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="value">The vertex data.</param>
        /// <remarks>Values will be scaled to normalized values.</remarks>
        public static void VertexAttrib4Nubv(uint index, byte[] value) {
            fixed (byte* v = &value[0]) {
                _glVertexAttrib4Nubv(index, v);
            }
        }

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="value">The vertex data.</param>
        /// <remarks>Values will be scaled to normalized values.</remarks>
        public static void VertexAttrib4Nuiv(uint index, uint[] value) {
            fixed (uint* v = &value[0]) {
                _glVertexAttrib4Nuiv(index, v);
            }
        }

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="value">The vertex data.</param>
        /// <remarks>Values will be scaled to normalized values.</remarks>
        public static void VertexAttrib4Nusv(uint index, ushort[] value) {
            fixed (ushort* v = &value[0]) {
                _glVertexAttrib4Nusv(index, v);
            }
        }

        /// <summary>
        ///     Define an array of generic vertex attribute data
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="size">
        ///     Specifies the number of components per generic vertex attribute.
        ///     <para>Must be 1, 2, 3, 4, or <see cref="GL_BGRA" />.</para>
        /// </param>
        /// <param name="type">Specifies the data type of each component in the array.</param>
        /// <param name="normalized">
        ///     Specifies whether fixed-point data values should be normalized (true) or converted directly as
        ///     fixed-point values (false) when they are accessed.
        /// </param>
        /// <param name="stride">
        ///     Specifies the byte offset between consecutive generic vertex attributes.
        ///     <para>If stride is 0, the generic vertex attributes are understood to be tightly packed in the array.</para>
        /// </param>
        /// <param name="pointer">
        ///     Specifies a offset of the first component of the first generic vertex attribute in the array in
        ///     the data store of the buffer currently bound to the ARRAY_BUFFER target.
        /// </param>
        public static void VertexAttribPointer(uint index, int size, int type, bool normalized, int stride, /*const*/void* pointer) => _glVertexAttribPointer(index, size, type, normalized, stride, pointer);

        /// <summary>
        ///     Define an array of generic vertex attribute data
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="size">
        ///     Specifies the number of components per generic vertex attribute.
        ///     <para>Must be 1, 2, 3, 4, or <see cref="GL_BGRA" />.</para>
        /// </param>
        /// <param name="type">Specifies the data type of each component in the array.</param>
        /// <param name="normalized">
        ///     Specifies whether fixed-point data values should be normalized (true) or converted directly as
        ///     fixed-point values (false) when they are accessed.
        /// </param>
        /// <param name="stride">
        ///     Specifies the byte offset between consecutive generic vertex attributes.
        ///     <para>If stride is 0, the generic vertex attributes are understood to be tightly packed in the array.</para>
        /// </param>
        /// <param name="pointer">
        ///     Specifies a offset of the first component of the first generic vertex attribute in the array in
        ///     the data store of the buffer currently bound to the ARRAY_BUFFER target.
        /// </param>
        public static void VertexAttribPointer(uint index, int size, int type, bool normalized, int stride, IntPtr pointer) => _glVertexAttribPointer(index, size, type, normalized, stride, pointer.ToPointer());

        /// <summary>
        ///     Return sampler parameter value(s).
        /// </summary>
        /// <param name="sampler">Specifies name of the sampler object from which to retrieve parameters.</param>
        /// <param name="paramName">Specifies the symbolic name of a sampler parameter.</param>
        /// <param name="count">The number of elements in the sampler parameters.</param>
        /// <returns>An array of the sampler parameter values.</returns>


        public static int[] GetSamplerParameteriv(uint sampler, int paramName, int count) {
            var values = new int[count];
            fixed (int* args = &values[0]) {
                _glGetSamplerParameteriv(sampler, paramName, args);
            }

            return values;
        }

        /// <summary>
        ///     Return sampler parameter value(s).
        /// </summary>
        /// <param name="sampler">Specifies name of the sampler object from which to retrieve parameters.</param>
        /// <param name="paramName">Specifies the symbolic name of a sampler parameter.</param>
        /// <param name="count">The number of elements in the sampler parameters.</param>
        /// <returns>An array of the sampler parameter values.</returns>
        /// <remarks>Values are interpreted as fully signed or unsigned.</remarks>


        public static int[] GetSamplerParameterIiv(uint sampler, int paramName, int count) {
            var values = new int[count];
            fixed (int* args = &values[0]) {
                _glGetSamplerParameterIiv(sampler, paramName, args);
            }

            return values;
        }

        /// <summary>
        ///     Return sampler parameter value(s).
        /// </summary>
        /// <param name="sampler">Specifies name of the sampler object from which to retrieve parameters.</param>
        /// <param name="paramName">Specifies the symbolic name of a sampler parameter.</param>
        /// <param name="count">The number of elements in the sampler parameters.</param>
        /// <returns>An array of the sampler parameter values.</returns>


        public static float[] GetSamplerParameterfv(uint sampler, int paramName, int count) {
            var values = new float[count];
            fixed (float* args = &values[0]) {
                _glGetSamplerParameterfv(sampler, paramName, args);
            }

            return values;
        }

        /// <summary>
        ///     Return sampler parameter value(s).
        /// </summary>
        /// <param name="sampler">Specifies name of the sampler object from which to retrieve parameters.</param>
        /// <param name="paramName">Specifies the symbolic name of a sampler parameter.</param>
        /// <param name="count">The number of elements in the sampler parameters.</param>
        /// <returns>An array of the sampler parameter values.</returns>
        /// <remarks>Values are interpreted as fully signed or unsigned.</remarks>


        public static uint[] GetSamplerParameterIuiv(uint sampler, int paramName, int count) {
            var values = new uint[count];
            fixed (uint* args = &values[0]) {
                _glGetSamplerParameterIuiv(sampler, paramName, args);
            }

            return values;
        }

        /// <summary>
        ///     Return a single sampler parameter value.
        /// </summary>
        /// <param name="sampler">Specifies name of the sampler object from which to retrieve parameters.</param>
        /// <param name="paramName">Specifies the symbolic name of a sampler parameter.</param>
        /// <returns>An single sampler parameter values.</returns>

        public static int GetSamplerParameteriv(uint sampler, int paramName) {
            int value;
            _glGetSamplerParameteriv(sampler, paramName, &value);
            return value;
        }

        /// <summary>
        ///     Return a single sampler parameter value.
        /// </summary>
        /// <param name="sampler">Specifies name of the sampler object from which to retrieve parameters.</param>
        /// <param name="paramName">Specifies the symbolic name of a sampler parameter.</param>
        /// <returns>An single sampler parameter values.</returns>
        /// <remarks>Values are interpreted as fully signed or unsigned.</remarks>

        public static int GetSamplerParameterIiv(uint sampler, int paramName) {
            int value;
            _glGetSamplerParameterIiv(sampler, paramName, &value);
            return value;
        }

        /// <summary>
        ///     Return a single sampler parameter value.
        /// </summary>
        /// <param name="sampler">Specifies name of the sampler object from which to retrieve parameters.</param>
        /// <param name="paramName">Specifies the symbolic name of a sampler parameter.</param>
        /// <returns>An single sampler parameter values.</returns>

        public static float GetSamplerParameterfv(uint sampler, int paramName) {
            float value;
            _glGetSamplerParameterfv(sampler, paramName, &value);
            return value;
        }

        /// <summary>
        ///     Return a single sampler parameter value.
        /// </summary>
        /// <param name="sampler">Specifies name of the sampler object from which to retrieve parameters.</param>
        /// <param name="paramName">Specifies the symbolic name of a sampler parameter.</param>
        /// <returns>An single sampler parameter values.</returns>
        /// <remarks>Values are interpreted as fully signed or unsigned.</remarks>

        public static uint GetSamplerParameterIui(uint sampler, int paramName) {
            uint value;
            _glGetSamplerParameterIuiv(sampler, paramName, &value);
            return value;
        }

        /// <summary>
        ///     Return sampler parameter value(s).
        /// </summary>
        /// <param name="sampler">Specifies name of the sampler object from which to retrieve parameters.</param>
        /// <param name="paramName">Specifies the symbolic name of a sampler parameter.</param>
        /// <param name="args">Returns the sampler parameters.</param>
        public static void GetSamplerParameteriv(uint sampler, int paramName, int* args) => _glGetSamplerParameteriv(sampler, paramName, args);

        /// <summary>
        ///     Return sampler parameter value(s).
        /// </summary>
        /// <param name="sampler">Specifies name of the sampler object from which to retrieve parameters.</param>
        /// <param name="paramName">Specifies the symbolic name of a sampler parameter.</param>
        /// <param name="args">Returns the sampler parameters.</param>
        public static void GetSamplerParameterIiv(uint sampler, int paramName, int* args) => _glGetSamplerParameterIiv(sampler, paramName, args);

        /// <summary>
        ///     Return sampler parameter value(s).
        /// </summary>
        /// <param name="sampler">Specifies name of the sampler object from which to retrieve parameters.</param>
        /// <param name="paramName">Specifies the symbolic name of a sampler parameter.</param>
        /// <param name="args">Returns the sampler parameters.</param>
        public static void GetSamplerParameterfv(uint sampler, int paramName, float* args) => _glGetSamplerParameterfv(sampler, paramName, args);

        /// <summary>
        ///     Return sampler parameter value(s).
        /// </summary>
        /// <param name="sampler">Specifies name of the sampler object from which to retrieve parameters.</param>
        /// <param name="paramName">Specifies the symbolic name of a sampler parameter.</param>
        /// <param name="args">Returns the sampler parameters.</param>
        public static void GetSamplerParameterIuiv(uint sampler, int paramName, uint* args) => _glGetSamplerParameterIuiv(sampler, paramName, args);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="x">The first value.</param>
        /// <remarks>Values will be extended to fully signed or unsigned integers.</remarks>
        public static void VertexAttribI1i(uint index, int x) => _glVertexAttribI1i(index, x);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="x">The first value.</param>
        /// <remarks>Values will be extended to fully signed or unsigned integers.</remarks>
        public static void VertexAttribI1ui(uint index, uint x) => _glVertexAttribI1ui(index, x);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="x">The first value.</param>
        /// <param name="y">The second value.</param>
        /// <remarks>Values will be extended to fully signed or unsigned integers.</remarks>
        public static void VertexAttribI2i(uint index, int x, int y) => _glVertexAttribI2i(index, x, y);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="x">The first value.</param>
        /// <param name="y">The second value.</param>
        /// <remarks>Values will be extended to fully signed or unsigned integers.</remarks>
        public static void VertexAttribI2ui(uint index, uint x, uint y) => _glVertexAttribI2ui(index, x, y);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="x">The first value.</param>
        /// <param name="y">The second value.</param>
        /// <param name="z">The third value.</param>
        /// <remarks>Values will be extended to fully signed or unsigned integers.</remarks>
        public static void VertexAttribI3i(uint index, int x, int y, int z) => _glVertexAttribI3i(index, x, y, z);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="x">The first value.</param>
        /// <param name="y">The second value.</param>
        /// <param name="z">The third value.</param>
        /// <remarks>Values will be extended to fully signed or unsigned integers.</remarks>
        public static void VertexAttribI3ui(uint index, uint x, uint y, uint z) => _glVertexAttribI3ui(index, x, y, z);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="x">The first value.</param>
        /// <param name="y">The second value.</param>
        /// <param name="z">The third value.</param>
        /// <param name="w">The fourth value.</param>
        /// <remarks>Values will be extended to fully signed or unsigned integers.</remarks>
        public static void VertexAttribI4i(uint index, int x, int y, int z, int w) => _glVertexAttribI4i(index, x, y, z, w);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="x">The first value.</param>
        /// <param name="y">The second value.</param>
        /// <param name="z">The third value.</param>
        /// <param name="w">The fourth value.</param>
        /// <remarks>Values will be extended to fully signed or unsigned integers.</remarks>
        public static void VertexAttribI4ui(uint index, uint x, uint y, uint z, uint w) => _glVertexAttribI4ui(index, x, y, z, w);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="v">A pointer to the vertex data.</param>
        /// <remarks>Values will be extended to fully signed or unsigned integers.</remarks>
        public static void VertexAttribI1iv(uint index, /*const*/ int* v) => _glVertexAttribI1iv(index, v);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="v">A pointer to the vertex data.</param>
        /// <remarks>Values will be extended to fully signed or unsigned integers.</remarks>
        public static void VertexAttribI1uiv(uint index, /*const*/ uint* v) => _glVertexAttribI1uiv(index, v);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="v">A pointer to the vertex data.</param>
        /// <remarks>Values will be extended to fully signed or unsigned integers.</remarks>
        public static void VertexAttribI2iv(uint index, /*const*/ int* v) => _glVertexAttribI2iv(index, v);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="v">A pointer to the vertex data.</param>
        /// <remarks>Values will be extended to fully signed or unsigned integers.</remarks>
        public static void VertexAttribI2uiv(uint index, /*const*/ uint* v) => _glVertexAttribI2uiv(index, v);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="v">A pointer to the vertex data.</param>
        /// <remarks>Values will be extended to fully signed or unsigned integers.</remarks>
        public static void VertexAttribI3iv(uint index, /*const*/ int* v) => _glVertexAttribI3iv(index, v);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="v">A pointer to the vertex data.</param>
        /// <remarks>Values will be extended to fully signed or unsigned integers.</remarks>
        public static void VertexAttribI3uiv(uint index, /*const*/ uint* v) => _glVertexAttribI3uiv(index, v);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="v">A pointer to the vertex data.</param>
        /// <remarks>Values will be extended to fully signed or unsigned integers.</remarks>
        public static void VertexAttribI4iv(uint index, /*const*/ int* v) => _glVertexAttribI4iv(index, v);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="v">A pointer to the vertex data.</param>
        /// <remarks>Values will be extended to fully signed or unsigned integers.</remarks>
        public static void VertexAttribI4uiv(uint index, /*const*/ uint* v) => _glVertexAttribI4uiv(index, v);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="v">A pointer to the vertex data.</param>
        /// <remarks>Values will be extended to fully signed or unsigned integers.</remarks>
        public static void VertexAttribI4bv(uint index, /*const*/ sbyte* v) => _glVertexAttribI4bv(index, v);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="v">A pointer to the vertex data.</param>
        /// <remarks>Values will be extended to fully signed or unsigned integers.</remarks>
        public static void VertexAttribI4sv(uint index, /*const*/ short* v) => _glVertexAttribI4sv(index, v);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="v">A pointer to the vertex data.</param>
        /// <remarks>Values will be extended to fully signed or unsigned integers.</remarks>
        public static void VertexAttribI4ubv(uint index, /*const*/ byte* v) => _glVertexAttribI4ubv(index, v);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="v">A pointer to the vertex data.</param>
        /// <remarks>Values will be extended to fully signed or unsigned integers.</remarks>
        public static void VertexAttribI4usv(uint index, /*const*/ ushort* v) => _glVertexAttribI4usv(index, v);

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="value">The vertex data.</param>
        /// <remarks>Values will be extended to fully signed or unsigned integers.</remarks>
        public static void VertexAttribI1iv(uint index, int[] value) {
            fixed (int* v = &value[0]) {
                _glVertexAttribI1iv(index, v);
            }
        }

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="value">The vertex data.</param>
        /// <remarks>Values will be extended to fully signed or unsigned integers.</remarks>
        public static void VertexAttribI1uiv(uint index, uint[] value) {
            fixed (uint* v = &value[0]) {
                _glVertexAttribI1uiv(index, v);
            }
        }

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="value">The vertex data.</param>
        /// <remarks>Values will be extended to fully signed or unsigned integers.</remarks>
        public static void VertexAttribI2iv(uint index, int[] value) {
            fixed (int* v = &value[0]) {
                _glVertexAttribI2iv(index, v);
            }
        }

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="value">The vertex data.</param>
        /// <remarks>Values will be extended to fully signed or unsigned integers.</remarks>
        public static void VertexAttribI2uiv(uint index, uint[] value) {
            fixed (uint* v = &value[0]) {
                _glVertexAttribI2uiv(index, v);
            }
        }

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="value">The vertex data.</param>
        /// <remarks>Values will be extended to fully signed or unsigned integers.</remarks>
        public static void VertexAttribI3iv(uint index, int[] value) {
            fixed (int* v = &value[0]) {
                _glVertexAttribI3iv(index, v);
            }
        }

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="value">The vertex data.</param>
        /// <remarks>Values will be extended to fully signed or unsigned integers.</remarks>
        public static void VertexAttribI3uiv(uint index, uint[] value) {
            fixed (uint* v = &value[0]) {
                _glVertexAttribI3uiv(index, v);
            }
        }

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="value">The vertex data.</param>
        /// <remarks>Values will be extended to fully signed or unsigned integers.</remarks>
        public static void VertexAttribI4iv(uint index, int[] value) {
            fixed (int* v = &value[0]) {
                _glVertexAttribI4iv(index, v);
            }
        }

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="value">The vertex data.</param>
        /// <remarks>Values will be extended to fully signed or unsigned integers.</remarks>
        public static void VertexAttribI4uiv(uint index, uint[] value) {
            fixed (uint* v = &value[0]) {
                _glVertexAttribI4uiv(index, v);
            }
        }

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="value">The vertex data.</param>
        /// <remarks>Values will be extended to fully signed or unsigned integers.</remarks>
        public static void VertexAttribI4bv(uint index, sbyte[] value) {
            fixed (sbyte* v = &value[0]) {
                _glVertexAttribI4bv(index, v);
            }
        }

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="value">The vertex data.</param>
        /// <remarks>Values will be extended to fully signed or unsigned integers.</remarks>
        public static void VertexAttribI4sv(uint index, short[] value) {
            fixed (short* v = &value[0]) {
                _glVertexAttribI4sv(index, v);
            }
        }

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="value">The vertex data.</param>
        /// <remarks>Values will be extended to fully signed or unsigned integers.</remarks>
        public static void VertexAttribI4ubv(uint index, byte[] value) {
            fixed (byte* v = &value[0]) {
                _glVertexAttribI4ubv(index, v);
            }
        }

        /// <summary>
        ///     Specifies the value of a generic vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="value">The vertex data.</param>
        /// <remarks>Values will be extended to fully signed or unsigned integers.</remarks>
        public static void VertexAttribI4usv(uint index, ushort[] value) {
            fixed (ushort* v = &value[0]) {
                _glVertexAttribI4usv(index, v);
            }
        }

        /// <summary>
        ///     Modify the rate at which generic vertex attributes advance during instanced rendering.
        /// </summary>
        /// <param name="index">Specify the index of the generic vertex attribute.</param>
        /// <param name="divisor">
        ///     Specify the number of instances that will pass between updates of the generic attribute at slot
        ///     <paramref name="index" />.
        /// </param>
        public static void VertexAttribDivisor(uint index, uint divisor) => _glVertexAttribDivisor(index, divisor);

        /// <summary>
        ///     Specified a packed vertex.
        /// </summary>
        /// <param name="type">Specify the vertex data type.</param>
        /// <param name="value">A pointer to the vertex data.</param>
        public static void VertexP2uiv(int type, /*const*/ uint* value) => _glVertexP2uiv(type, value);

        /// <summary>
        ///     Specified a packed vertex.
        /// </summary>
        /// <param name="type">Specify the vertex data type.</param>
        /// <param name="value">A pointer to the vertex data.</param>
        public static void VertexP3uiv(int type, /*const*/ uint* value) => _glVertexP3uiv(type, value);

        /// <summary>
        ///     Specified a packed vertex.
        /// </summary>
        /// <param name="type">Specify the vertex data type.</param>
        /// <param name="value">A pointer to the vertex data.</param>
        public static void VertexP4uiv(int type, /*const*/ uint* value) => _glVertexP4uiv(type, value);

        /// <summary>
        ///     Specified a packed vertex.
        /// </summary>
        /// <param name="type">Specify the vertex data type.</param>
        /// <param name="value">A array of vertex data.</param>
        public static void VertexP2uiv(int type, uint[] value) {
            fixed (uint* v = &value[0]) {
                _glVertexP2uiv(type, v);
            }
        }

        /// <summary>
        ///     Specified a packed vertex.
        /// </summary>
        /// <param name="type">Specify the vertex data type.</param>
        /// <param name="value">A array of vertex data.</param>
        public static void VertexP3uiv(int type, uint[] value) {
            fixed (uint* v = &value[0]) {
                _glVertexP3uiv(type, v);
            }
        }

        /// <summary>
        ///     Specified a packed vertex.
        /// </summary>
        /// <param name="type">Specify the vertex data type.</param>
        /// <param name="value">A array of vertex data.</param>
        public static void VertexP4uiv(int type, uint[] value) {
            fixed (uint* v = &value[0]) {
                _glVertexP4uiv(type, v);
            }
        }

        /// <summary>
        ///     Specified a packed vertex.
        /// </summary>
        /// <param name="type">Specify the vertex data type.</param>
        /// <param name="value">The vertex data.</param>
        public static void VertexP2ui(int type, uint value) => _glVertexP2ui(type, value);

        /// <summary>
        ///     Specified a packed vertex.
        /// </summary>
        /// <param name="type">Specify the vertex data type.</param>
        /// <param name="value">The vertex data.</param>
        public static void VertexP3ui(int type, uint value) => _glVertexP3ui(type, value);

        /// <summary>
        ///     Specified a packed vertex.
        /// </summary>
        /// <param name="type">Specify the vertex data type.</param>
        /// <param name="value">The vertex data.</param>
        public static void VertexP4ui(int type, uint value) => _glVertexP4ui(type, value);

        /// <summary>
        ///     Specifies the value of a generic packed vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="type">Specifies the type of packing used on the data.</param>
        /// <param name="normalized">
        ///     <c>true</c>  values are to be converted to floating point values by normalizing.
        ///     <para>
        ///         Otherwise, they are converted directly to floating-point values. If type indicates a floating-point format,
        ///         then normalized value must be <c>false</c>.
        ///     </para>
        /// </param>
        /// <param name="value">A pointer to the new packed values to be used for the specified vertex attribute.</param>
        public static void VertexAttribP1uiv(uint index, int type, bool normalized, /*const*/ uint* value) => _glVertexAttribP1uiv(index, type, normalized, value);

        /// <summary>
        ///     Specifies the value of a generic packed vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="type">Specifies the type of packing used on the data.</param>
        /// <param name="normalized">
        ///     <c>true</c>  values are to be converted to floating point values by normalizing.
        ///     <para>
        ///         Otherwise, they are converted directly to floating-point values. If type indicates a floating-point format,
        ///         then normalized value must be <c>false</c>.
        ///     </para>
        /// </param>
        /// <param name="value">A pointer to the new packed values to be used for the specified vertex attribute.</param>
        public static void VertexAttribP2uiv(uint index, int type, bool normalized, /*const*/ uint* value) => _glVertexAttribP2uiv(index, type, normalized, value);

        /// <summary>
        ///     Specifies the value of a generic packed vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="type">Specifies the type of packing used on the data.</param>
        /// <param name="normalized">
        ///     <c>true</c>  values are to be converted to floating point values by normalizing.
        ///     <para>
        ///         Otherwise, they are converted directly to floating-point values. If type indicates a floating-point format,
        ///         then normalized value must be <c>false</c>.
        ///     </para>
        /// </param>
        /// <param name="value">A pointer to the new packed values to be used for the specified vertex attribute.</param>
        public static void VertexAttribP3uiv(uint index, int type, bool normalized, /*const*/ uint* value) => _glVertexAttribP3uiv(index, type, normalized, value);

        /// <summary>
        ///     Specifies the value of a generic packed vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="type">Specifies the type of packing used on the data.</param>
        /// <param name="normalized">
        ///     <c>true</c>  values are to be converted to floating point values by normalizing.
        ///     <para>
        ///         Otherwise, they are converted directly to floating-point values. If type indicates a floating-point format,
        ///         then normalized value must be <c>false</c>.
        ///     </para>
        /// </param>
        /// <param name="value">A pointer to the new packed values to be used for the specified vertex attribute.</param>
        public static void VertexAttribP4uiv(uint index, int type, bool normalized, /*const*/ uint* value) => _glVertexAttribP4uiv(index, type, normalized, value);

        /// <summary>
        ///     Specifies the value of a generic packed vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="type">Specifies the type of packing used on the data.</param>
        /// <param name="normalized">
        ///     <c>true</c>  values are to be converted to floating point values by normalizing.
        ///     <para>
        ///         Otherwise, they are converted directly to floating-point values. If type indicates a floating-point format,
        ///         then normalized value must be <c>false</c>.
        ///     </para>
        /// </param>
        /// <param name="value">The new packed values to be used for the specified vertex attribute.</param>
        public static void VertexAttribP1uiv(uint index, int type, bool normalized, uint[] value) {
            fixed (uint* v = &value[0]) {
                _glVertexAttribP1uiv(index, type, normalized, v);
            }
        }

        /// <summary>
        ///     Specifies the value of a generic packed vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="type">Specifies the type of packing used on the data.</param>
        /// <param name="normalized">
        ///     <c>true</c>  values are to be converted to floating point values by normalizing.
        ///     <para>
        ///         Otherwise, they are converted directly to floating-point values. If type indicates a floating-point format,
        ///         then normalized value must be <c>false</c>.
        ///     </para>
        /// </param>
        /// <param name="value">The new packed values to be used for the specified vertex attribute.</param>
        public static void VertexAttribP2uiv(uint index, int type, bool normalized, uint[] value) {
            fixed (uint* v = &value[0]) {
                _glVertexAttribP2uiv(index, type, normalized, v);
            }
        }

        /// <summary>
        ///     Specifies the value of a generic packed vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="type">Specifies the type of packing used on the data.</param>
        /// <param name="normalized">
        ///     <c>true</c>  values are to be converted to floating point values by normalizing.
        ///     <para>
        ///         Otherwise, they are converted directly to floating-point values. If type indicates a floating-point format,
        ///         then normalized value must be <c>false</c>.
        ///     </para>
        /// </param>
        /// <param name="value">The new packed values to be used for the specified vertex attribute.</param>
        public static void VertexAttribP3uiv(uint index, int type, bool normalized, uint[] value) {
            fixed (uint* v = &value[0]) {
                _glVertexAttribP3uiv(index, type, normalized, v);
            }
        }

        /// <summary>
        ///     Specifies the value of a generic packed vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="type">Specifies the type of packing used on the data.</param>
        /// <param name="normalized">
        ///     <c>true</c>  values are to be converted to floating point values by normalizing.
        ///     <para>
        ///         Otherwise, they are converted directly to floating-point values. If type indicates a floating-point format,
        ///         then normalized value must be <c>false</c>.
        ///     </para>
        /// </param>
        /// <param name="value">The new packed values to be used for the specified vertex attribute.</param>
        public static void VertexAttribP4uiv(uint index, int type, bool normalized, uint[] value) {
            fixed (uint* v = &value[0]) {
                _glVertexAttribP4uiv(index, type, normalized, v);
            }
        }

        /// <summary>
        ///     Specifies the value of a generic packed vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="type">Specifies the type of packing used on the data.</param>
        /// <param name="normalized">
        ///     <c>true</c>  values are to be converted to floating point values by normalizing.
        ///     <para>
        ///         Otherwise, they are converted directly to floating-point values. If type indicates a floating-point format,
        ///         then normalized value must be <c>false</c>.
        ///     </para>
        /// </param>
        /// <param name="value">Specifies the new packed value to be used for the specified vertex attribute.</param>
        public static void VertexAttribP1ui(uint index, int type, bool normalized, uint value) => _glVertexAttribP1ui(index, type, normalized, value);

        /// <summary>
        ///     Specifies the value of a generic packed vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="type">Specifies the type of packing used on the data.</param>
        /// <param name="normalized">
        ///     <c>true</c>  values are to be converted to floating point values by normalizing.
        ///     <para>
        ///         Otherwise, they are converted directly to floating-point values. If type indicates a floating-point format,
        ///         then normalized value must be <c>false</c>.
        ///     </para>
        /// </param>
        /// <param name="value">Specifies the new packed value to be used for the specified vertex attribute.</param>
        public static void VertexAttribP2ui(uint index, int type, bool normalized, uint value) => _glVertexAttribP2ui(index, type, normalized, value);

        /// <summary>
        ///     Specifies the value of a generic packed vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="type">Specifies the type of packing used on the data.</param>
        /// <param name="normalized">
        ///     <c>true</c>  values are to be converted to floating point values by normalizing.
        ///     <para>
        ///         Otherwise, they are converted directly to floating-point values. If type indicates a floating-point format,
        ///         then normalized value must be <c>false</c>.
        ///     </para>
        /// </param>
        /// <param name="value">Specifies the new packed value to be used for the specified vertex attribute.</param>
        public static void VertexAttribP3ui(uint index, int type, bool normalized, uint value) => _glVertexAttribP3ui(index, type, normalized, value);

        /// <summary>
        ///     Specifies the value of a generic packed vertex attribute.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="type">Specifies the type of packing used on the data.</param>
        /// <param name="normalized">
        ///     <c>true</c>  values are to be converted to floating point values by normalizing.
        ///     <para>
        ///         Otherwise, they are converted directly to floating-point values. If type indicates a floating-point format,
        ///         then normalized value must be <c>false</c>.
        ///     </para>
        /// </param>
        /// <param name="value">Specifies the new packed value to be used for the specified vertex attribute.</param>
        public static void VertexAttribP4ui(uint index, int type, bool normalized, uint value) => _glVertexAttribP4ui(index, type, normalized, value);


        /// <summary>
        ///     Attach a buffer object's data store to a buffer texture object
        /// </summary>
        /// <param name="target">Specifies the target to which the texture is bound.<para>Must be TEXTURE_BUFFER.</para></param>
        /// <param name="internalFormat">Specifies the internal format of the data in the store belonging to buffer.</param>
        /// <param name="buffer">Specifies the name of the buffer object whose storage to attach to the active buffer texture.</param>
        public static void TexBuffer(int target, int internalFormat, uint buffer) => _glTexBuffer(target, internalFormat, buffer);

        /// <summary>
        /// Query information about an active uniform block.
        /// </summary>
        /// <param name="program">Specifies the name of a program containing the uniform block.</param>
        /// <param name="uniformBlockIndex">Specifies the index of the uniform block within program.</param>
        /// <param name="pname">Specifies the name of the parameter to query.</param>
        /// <param name="args">Specifies the address of a variable to receive the result of the query.</param>
        public static void GetActiveUniformBlockiv(uint program, uint uniformBlockIndex, int pname, int* args) => _glGetActiveUniformBlockiv(program, uniformBlockIndex, pname, args);

        /// <summary>
        /// Query information about an active uniform block.
        /// </summary>
        /// <param name="program">Specifies the name of a program containing the uniform block.</param>
        /// <param name="uniformBlockIndex">Specifies the index of the uniform block within program.</param>
        /// <param name="pname">Specifies the name of the parameter to query.</param>
        /// <param name="count">Specifies the number of values to receive.</param>


        public static int[] GetActiveUniformBlockiv(uint program, uint uniformBlockIndex, int pname, int count) {
            var values = new int[count];
            fixed (int* args = &values[0]) {
                _glGetActiveUniformBlockiv(program, uniformBlockIndex, pname, args);
            }
            return values;
        }

        /// <summary>
        /// Retrieve the name of an active uniform block.
        /// </summary>
        /// <param name="program">Specifies the name of a program containing the uniform block.</param>
        /// <param name="uniformBlockIndex">Specifies the index of the uniform block within program.</param>
        /// <param name="bufSize">Specifies the size of the buffer addressed by uniformBlockName.</param>
        /// <returns>The name of the uniform block.</returns>


        public static string GetActiveUniformBlockName(uint program, uint uniformBlockIndex, int bufSize = 512) {
            int length;
            var buffer = new byte[bufSize];
            fixed (byte* name = &buffer[0]) {
                _glGetActiveUniformBlockName(program, uniformBlockIndex, bufSize, &length, name);
            }
            return Encoding.UTF8.GetString(buffer, 0, Math.Min(bufSize, length));
        }


        /// <summary>
        /// Bind a user-defined varying out variable to a fragment shader color number and index.
        /// </summary>
        /// <param name="program">The name of the program containing varying out variable whose binding to modify.</param>
        /// <param name="colorNumber">The color number to bind the user-defined varying out variable to.</param>
        /// <param name="index">The index of the color input to bind the user-defined varying out variable to.</param>
        /// <param name="name">The name of the user-defined varying out variable whose binding to modify.</param>
        public static void BindFragDataLocationIndexed(uint program, uint colorNumber, uint index, string name) {
            var buffer = Encoding.UTF8.GetBytes(name);
            fixed (byte* b = &buffer[0]) {
                _glBindFragDataLocationIndexed(program, colorNumber, index, b);
            }
        }

        /// <summary>
        /// Return parameters of a query object.
        /// </summary>
        /// <param name="id">Specifies the name of a query object.</param>
        /// <param name="pname">Specifies the symbolic name of a query object parameter.<para>Accepted values are QUERY_RESULT or QUERY_RESULT_AVAILABLE.</para></param>
        /// <param name="args">Returns the requested data.</param>
        public static void GetQueryObjectiv(uint id, int pname, int* args) => _glGetQueryObjectiv(id, pname, args);

        /// <summary>
        /// Return parameters of a query object.
        /// </summary>
        /// <param name="id">Specifies the name of a query object.</param>
        /// <param name="pname">Specifies the symbolic name of a query object parameter.<para>Accepted values are QUERY_RESULT or QUERY_RESULT_AVAILABLE.</para></param>
        /// <param name="args">Returns the requested data.</param>
        public static void GetQueryObjectuiv(uint id, int pname, uint* args) => _glGetQueryObjectuiv(id, pname, args);

        /// <summary>
        /// Return parameters of a query object.
        /// </summary>
        /// <param name="id">Specifies the name of a query object.</param>
        /// <param name="pname">Specifies the symbolic name of a query object parameter.<para>Accepted values are QUERY_RESULT or QUERY_RESULT_AVAILABLE.</para></param>
        /// <param name="args">Returns the requested data.</param>
        public static void GetQueryObjecti64v(uint id, int pname, long* args) => _glGetQueryObjecti64v(id, pname, args);

        /// <summary>
        /// Return parameters of a query object.
        /// </summary>
        /// <param name="id">Specifies the name of a query object.</param>
        /// <param name="pname">Specifies the symbolic name of a query object parameter.<para>Accepted values are QUERY_RESULT or QUERY_RESULT_AVAILABLE.</para></param>
        /// <param name="args">Returns the requested data.</param>
        public static void GetQueryObjectui64v(uint id, int pname, ulong* args) => _glGetQueryObjectui64v(id, pname, args);

        /// <summary>
        /// Return parameters of a query object.
        /// </summary>
        /// <param name="id">Specifies the name of a query object.</param>
        /// <param name="pname">Specifies the symbolic name of a query object parameter.<para>Accepted values are QUERY_RESULT or QUERY_RESULT_AVAILABLE.</para></param>
        /// <param name="count">The number of values to receive.</param>
        /// <returns>The retrieved values.</returns>


        public static ulong[] GetQueryObjectui64v(uint id, int pname, int count) {
            var values = new ulong[count];
            fixed (ulong* args = &values[0]) {
                _glGetQueryObjectui64v(id, pname, args);
            }
            return values;
        }

        /// <summary>
        /// Return parameters of a query object.
        /// </summary>
        /// <param name="id">Specifies the name of a query object.</param>
        /// <param name="pname">Specifies the symbolic name of a query object parameter.<para>Accepted values are QUERY_RESULT or QUERY_RESULT_AVAILABLE.</para></param>
        /// <param name="count">The number of values to receive.</param>
        /// <returns>The retrieved values.</returns>


        public static long[] GetQueryObjecti64v(uint id, int pname, int count) {
            var values = new long[count];
            fixed (long* args = &values[0]) {
                _glGetQueryObjecti64v(id, pname, args);
            }
            return values;
        }

        /// <summary>
        /// Return parameters of a query object.
        /// </summary>
        /// <param name="id">Specifies the name of a query object.</param>
        /// <param name="pname">Specifies the symbolic name of a query object parameter.<para>Accepted values are QUERY_RESULT or QUERY_RESULT_AVAILABLE.</para></param>
        /// <param name="count">The number of values to receive.</param>
        /// <returns>The retrieved values.</returns>


        public static uint[] GetQueryObjectuiv(uint id, int pname, int count) {
            var values = new uint[count];
            fixed (uint* args = &values[0]) {
                _glGetQueryObjectuiv(id, pname, args);
            }
            return values;
        }

        /// <summary>
        /// Return parameters of a query object.
        /// </summary>
        /// <param name="id">Specifies the name of a query object.</param>
        /// <param name="pname">Specifies the symbolic name of a query object parameter.<para>Accepted values are QUERY_RESULT or QUERY_RESULT_AVAILABLE.</para></param>
        /// <param name="count">The number of values to receive.</param>
        /// <returns>The retrieved values.</returns>


        public static int[] GetQueryObjectiv(uint id, int pname, int count) {
            var values = new int[count];
            fixed (int* args = &values[0]) {
                _glGetQueryObjectiv(id, pname, args);
            }
            return values;
        }

        /// <summary>
        /// Query the name of an active uniform.
        /// </summary>
        /// <param name="program">Specifies the program containing the active uniform index <paramref name="uniformIndex"/>.</param>
        /// <param name="uniformIndex">Specifies the index of the active uniform whose name to query.</param>
        /// <param name="bufSize">Specifies the size of the buffer for the string.</param>
        /// <returns>The name of the active uniform.</returns>


        public static string GetActiveUniformName(uint program, uint uniformIndex, int bufSize = 512) {
            int length;
            var buffer = new byte[bufSize];
            fixed (byte* name = &buffer[0]) {
                _glGetActiveUniformName(program, uniformIndex, bufSize, &length, name);
            }
            return Encoding.UTF8.GetString(buffer, 0, Math.Min(length, bufSize));
        }

        /// <summary>
        /// Bind a framebuffer to a framebuffer target.
        /// </summary>
        /// <param name="target">Specifies the framebuffer target of the binding operation.</param>
        /// <param name="framebuffer">Specifies the name of the framebuffer object to bind.</param>
        public static void BindFramebuffer(int target, uint framebuffer) => _glBindFramebuffer(target, framebuffer);

        /// <summary>
        /// Assign a binding point to an active uniform block.
        /// </summary>
        /// <param name="program">The name of a program object containing the active uniform block whose binding to assign.</param>
        /// <param name="uniformBlockIndex">The index of the active uniform block within program whose binding to assign.</param>
        /// <param name="uniformBlockBinding">Specifies the binding point to which to bind the uniform block with index uniformBlockIndex within program.</param>
        public static void UniformBlockBinding(uint program, uint uniformBlockIndex, uint uniformBlockBinding) => _glUniformBlockBinding(program, uniformBlockIndex, uniformBlockBinding);

        /// <summary>
        /// Return a parameter from a program object.
        /// </summary>
        /// <param name="program">Specifies the program object to be queried.</param>
        /// <param name="pname">Specifies the object parameter.</param>
        /// <param name="args">Returns the requested object parameter.</param>
        public static void GetProgramiv(uint program, int pname, int* args) => _glGetProgramiv(program, pname, args);

        /// <summary>
        /// Return a parameter from a program object.
        /// </summary>
        /// <param name="program">Specifies the program object to be queried.</param>
        /// <param name="pname">Specifies the object parameter.</param>
        /// <param name="count">The number of parameters to return..</param>
        /// <returns>The requested parameters.</returns>


        public static int[] GetProgramiv(uint program, int pname, int count) {
            var values = new int[count];
            fixed (int* args = &values[0]) {
                _glGetProgramiv(program, pname, args);
            }
            return values;
        }

        /// <summary>
        /// Return a parameter from a shader object.
        /// </summary>
        /// <param name="shader">Specifies the shader object to be queried.</param>
        /// <param name="pname">Specifies the object parameter.<para>Must be SHADER_TYPE, DELETE_STATUS, COMPILE_STATUS, INFO_LOG_LENGTH, or SHADER_SOURCE_LENGTH.</para></param>
        /// <param name="args">Returns the requested object parameter.</param>
        public static void GetShaderiv(uint shader, int pname, int* args) => _glGetShaderiv(shader, pname, args);

        /// <summary>
        /// Return a parameter from a shader object.
        /// </summary>
        /// <param name="shader">Specifies the shader object to be queried.</param>
        /// <param name="pname">Specifies the object parameter.<para>Must be SHADER_TYPE, DELETE_STATUS, COMPILE_STATUS, INFO_LOG_LENGTH, or SHADER_SOURCE_LENGTH.</para></param>
        /// <param name="count">The number of parameters to return..</param>
        /// <returns>The requested parameters.</returns>


        public static int[] GetShaderiv(uint shader, int pname, int count) {
            var values = new int[count];
            fixed (int* args = &values[0]) {
                _glGetShaderiv(shader, pname, args);
            }
            return values;
        }

        /// <summary>
        /// Return parameters of a query object target.
        /// </summary>
        /// <param name="target">Specifies a query object target.</param>
        /// <param name="pname">Specifies the symbolic name of a query object target parameter.<para>Accepted values are CURRENT_QUERY or QUERY_COUNTER_BITS.</para></param>
        /// <param name="args">Returns the requested data.</param>
        public static void GetQueryiv(int target, int pname, int* args) => _glGetQueryiv(target, pname, args);

        /// <summary>
        /// Return parameters of a query object target.
        /// </summary>
        /// <param name="target">Specifies a query object target.</param>
        /// <param name="pname">Specifies the symbolic name of a query object target parameter.<para>Accepted values are CURRENT_QUERY or QUERY_COUNTER_BITS.</para></param>
        /// <param name="count">The number of parameters to return..</param>
        /// <returns>The requested parameters.</returns>


        public static int[] GetQueryiv(int target, int pname, int count) {
            var values = new int[count];
            fixed (int* args = &values[0]) {
                _glGetQueryiv(target, pname, args);
            }
            return values;
        }

        /// <summary>
        /// Returns the value of a uniform variable.
        /// </summary>
        /// <param name="program">Specifies the program object to be queried.</param>
        /// <param name="location">Specifies the location of the uniform variable to be queried.</param>
        /// <param name="args">Returns the value of the specified uniform variable</param>
        public static void GetUniformfv(uint program, int location, float* args) => _glGetUniformfv(program, location, args);

        /// <summary>
        /// Returns the value of a uniform variable.
        /// </summary>
        /// <param name="program">Specifies the program object to be queried.</param>
        /// <param name="location">Specifies the location of the uniform variable to be queried.</param>
        /// <param name="count">The number of values to retrieve.</param>
        /// <returns>The requested values.</returns>


        public static float[] GetUniformfv(uint program, int location, int count) {
            var values = new float[count];
            fixed (float* args = &values[0]) {
                _glGetUniformfv(program, location, args);
            }
            return values;
        }

        /// <summary>
        /// Returns the value of a uniform variable.
        /// </summary>
        /// <param name="program">Specifies the program object to be queried.</param>
        /// <param name="location">Specifies the location of the uniform variable to be queried.</param>
        /// <param name="args">Returns the value of the specified uniform variable</param>
        public static void GetUniformuiv(uint program, int location, uint* args) => _glGetUniformuiv(program, location, args);

        /// <summary>
        /// Returns the value of a uniform variable.
        /// </summary>
        /// <param name="program">Specifies the program object to be queried.</param>
        /// <param name="location">Specifies the location of the uniform variable to be queried.</param>
        /// <param name="count">The number of values to retrieve.</param>
        /// <returns>The requested values.</returns>


        public static uint[] GetUniformuiv(uint program, int location, int count) {
            var values = new uint[count];
            fixed (uint* args = &values[0]) {
                _glGetUniformuiv(program, location, args);
            }
            return values;
        }

        /// <summary>
        /// Returns the value of a uniform variable.
        /// </summary>
        /// <param name="program">Specifies the program object to be queried.</param>
        /// <param name="location">Specifies the location of the uniform variable to be queried.</param>
        /// <param name="args">Returns the value of the specified uniform variable</param>
        public static void GetUniformiv(uint program, int location, int* args) => _glGetUniformiv(program, location, args);

        /// <summary>
        /// Returns the value of a uniform variable.
        /// </summary>
        /// <param name="program">Specifies the program object to be queried.</param>
        /// <param name="location">Specifies the location of the uniform variable to be queried.</param>
        /// <param name="count">The number of values to retrieve.</param>
        /// <returns>The requested values.</returns>


        public static int[] GetUniformiv(uint program, int location, int count) {
            var values = new int[count];
            fixed (int* args = &values[0]) {
                _glGetUniformiv(program, location, args);
            }
            return values;
        }

        /// <summary>
        /// Copy all or part of the data store of a buffer object to the data store of another buffer object.
        /// </summary>
        /// <param name="readTarget">Specifies the target to which the source buffer object is bound.</param>
        /// <param name="writeTarget">Specifies the target to which the destination buffer object is bound.</param>
        /// <param name="readOffset">Specifies the offset, in basic machine units, within the data store of the source buffer object at which data will be read.</param>
        /// <param name="writeOffset">Specifies the offset, in basic machine units, within the data store of the destination buffer object at which data will be written.</param>
        /// <param name="size">Specifies the size, in basic machine units, of the data to be copied from the source buffer object to the destination buffer object.</param>
        public static void CopyBufferSubData(int readTarget, int writeTarget, int readOffset, int writeOffset, int size) => _glCopyBufferSubData(readTarget, writeTarget, new IntPtr(readOffset), new IntPtr(writeOffset), new IntPtr(size));

        /// <summary>
        /// Copy all or part of the data store of a buffer object to the data store of another buffer object.
        /// </summary>
        /// <param name="readTarget">Specifies the target to which the source buffer object is bound.</param>
        /// <param name="writeTarget">Specifies the target to which the destination buffer object is bound.</param>
        /// <param name="readOffset">Specifies the offset, in basic machine units, within the data store of the source buffer object at which data will be read.</param>
        /// <param name="writeOffset">Specifies the offset, in basic machine units, within the data store of the destination buffer object at which data will be written.</param>
        /// <param name="size">Specifies the size, in basic machine units, of the data to be copied from the source buffer object to the destination buffer object.</param>
        public static void CopyBufferSubData(int readTarget, int writeTarget, long readOffset, long writeOffset, long size) => _glCopyBufferSubData(readTarget, writeTarget, new IntPtr(readOffset), new IntPtr(writeOffset), new IntPtr(size));

        /// <summary>
        /// Return a generic vertex attribute parameter.
        /// </summary>
        /// <param name="index">Specifies the generic vertex attribute parameter to be queried.</param>
        /// <param name="pname">Specifies the symbolic name of the vertex attribute parameter to be queried. </param>
        /// <param name="args">Returns the requested data.</param>
        public static void GetVertexAttribdv(uint index, int pname, double* args) => _glGetVertexAttribdv(index, pname, args);

        /// <summary>
        /// Return a generic vertex attribute parameter.
        /// </summary>
        /// <param name="index">Specifies the generic vertex attribute parameter to be queried.</param>
        /// <param name="pname">Specifies the symbolic name of the vertex attribute parameter to be queried. </param>
        /// <param name="args">Returns the requested data.</param>
        public static void GetVertexAttribfv(uint index, int pname, float* args) => _glGetVertexAttribfv(index, pname, args);

        /// <summary>
        /// Return a generic vertex attribute parameter.
        /// </summary>
        /// <param name="index">Specifies the generic vertex attribute parameter to be queried.</param>
        /// <param name="pname">Specifies the symbolic name of the vertex attribute parameter to be queried. </param>
        /// <param name="args">Returns the requested data.</param>
        public static void GetVertexAttribiv(uint index, int pname, int* args) => _glGetVertexAttribiv(index, pname, args);

        /// <summary>
        /// Return a generic vertex attribute parameter.
        /// </summary>
        /// <param name="index">Specifies the generic vertex attribute parameter to be queried.</param>
        /// <param name="pname">Specifies the symbolic name of the vertex attribute parameter to be queried. </param>
        /// <param name="args">Returns the requested data.</param>
        public static void GetVertexAttribIiv(uint index, int pname, int* args) => _glGetVertexAttribIiv(index, pname, args);

        /// <summary>
        /// Return a generic vertex attribute parameter.
        /// </summary>
        /// <param name="index">Specifies the generic vertex attribute parameter to be queried.</param>
        /// <param name="pname">Specifies the symbolic name of the vertex attribute parameter to be queried. </param>
        /// <param name="args">Returns the requested data.</param>
        public static void GetVertexAttribIuiv(uint index, int pname, uint* args) => _glGetVertexAttribIuiv(index, pname, args);

        /// <summary>
        /// Return a generic vertex attribute parameter.
        /// </summary>
        /// <param name="index">Specifies the generic vertex attribute parameter to be queried.</param>
        /// <param name="pname">Specifies the symbolic name of the vertex attribute parameter to be queried. </param>
        /// <param name="count">The number of values to retrieve.</param>
        /// <returns>The requested values.</returns>


        public static double[] GetVertexAttribdv(uint index, int pname, int count) {
            var values = new double[count];
            fixed (double* args = &values[0]) {
                _glGetVertexAttribdv(index, pname, args);
            }
            return values;
        }

        /// <summary>
        /// Return a generic vertex attribute parameter.
        /// </summary>
        /// <param name="index">Specifies the generic vertex attribute parameter to be queried.</param>
        /// <param name="pname">Specifies the symbolic name of the vertex attribute parameter to be queried. </param>
        /// <param name="count">The number of values to retrieve.</param>
        /// <returns>The requested values.</returns>


        public static float[] GetVertexAttribfv(uint index, int pname, int count) {
            var values = new float[count];
            fixed (float* args = &values[0]) {
                _glGetVertexAttribfv(index, pname, args);
            }
            return values;
        }

        /// <summary>
        /// Return a generic vertex attribute parameter.
        /// </summary>
        /// <param name="index">Specifies the generic vertex attribute parameter to be queried.</param>
        /// <param name="pname">Specifies the symbolic name of the vertex attribute parameter to be queried. </param>
        /// <param name="count">The number of values to retrieve.</param>
        /// <returns>The requested values.</returns>


        public static int[] GetVertexAttribiv(uint index, int pname, int count) {
            var values = new int[count];
            fixed (int* args = &values[0]) {
                _glGetVertexAttribiv(index, pname, args);
            }
            return values;
        }

        /// <summary>
        /// Return a generic vertex attribute parameter.
        /// </summary>
        /// <param name="index">Specifies the generic vertex attribute parameter to be queried.</param>
        /// <param name="pname">Specifies the symbolic name of the vertex attribute parameter to be queried. </param>
        /// <param name="count">The number of values to retrieve.</param>
        /// <returns>The requested values.</returns>


        public static int[] GetVertexAttribIiv(uint index, int pname, int count) {
            var values = new int[count];
            fixed (int* args = &values[0]) {
                _glGetVertexAttribIiv(index, pname, args);
            }
            return values;
        }

        /// <summary>
        /// Return a generic vertex attribute parameter.
        /// </summary>
        /// <param name="index">Specifies the generic vertex attribute parameter to be queried.</param>
        /// <param name="pname">Specifies the symbolic name of the vertex attribute parameter to be queried. </param>
        /// <param name="count">The number of values to retrieve.</param>
        /// <returns>The requested values.</returns>


        public static uint[] GetVertexAttribIuiv(uint index, int pname, int count) {
            var values = new uint[count];
            fixed (uint* args = &values[0]) {
                _glGetVertexAttribIuiv(index, pname, args);
            }
            return values;
        }

        /// <summary>
        /// Define an array of generic vertex attribute data.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="size">Specifies the number of components per generic vertex attribute.<para>Must be 1, 2, 3, 4 or BGRA.</para></param>
        /// <param name="type">Specifies the data type of each component in the array.</param>
        /// <param name="stride">Specifies the byte offset between consecutive generic vertex attributes.<para>If stride is 0, the generic vertex attributes are understood to be tightly packed in the array.</para>The initial value is 0.</param>
        /// <param name="pointer">Specifies a offset of the first component of the first generic vertex attribute in the array in the data store of the buffer currently bound to the ARRAY_BUFFER target.<para>The initial value is 0.</para></param>
        public static void VertexAttribIPointer(uint index, int size, int type, int stride, /*const*/ void* pointer) => _glVertexAttribIPointer(index, size, type, stride, pointer);

        /// <summary>
        /// Define an array of generic vertex attribute data.
        /// </summary>
        /// <param name="index">Specifies the index of the generic vertex attribute to be modified.</param>
        /// <param name="size">Specifies the number of components per generic vertex attribute.<para>Must be 1, 2, 3, 4 or BGRA.</para></param>
        /// <param name="type">Specifies the data type of each component in the array.</param>
        /// <param name="stride">Specifies the byte offset between consecutive generic vertex attributes.<para>If stride is 0, the generic vertex attributes are understood to be tightly packed in the array.</para>The initial value is 0.</param>
        /// <param name="pointer">Specifies a offset of the first component of the first generic vertex attribute in the array in the data store of the buffer currently bound to the ARRAY_BUFFER target.<para>The initial value is 0.</para></param>
        public static void VertexAttribIPointer(uint index, int size, int type, int stride, IntPtr pointer) => _glVertexAttribIPointer(index, size, type, stride, pointer.ToPointer());


        /// <summary>
        /// Establish the data storage, format, dimensions, and number of samples of a multisample texture's image
        /// </summary>
        /// <param name="target">Specifies the target of the operation.<para>Must be TEXTURE_2D_MULTISAMPLE or PROXY_TEXTURE_2D_MULTISAMPLE.</para></param>
        /// <param name="samples">The number of samples in the multisample texture's image.</param>
        /// <param name="internalformat">The internal format to be used to store the multisample texture's image.<para>Must specify a color-renderable, depth-renderable, or stencil-renderable format.</para></param>
        /// <param name="width">The width of the multisample texture's image, in texels.</param>
        /// <param name="height">The height of the multisample texture's image, in texels.</param>
        /// <param name="fixedsamplelocations">Specifies whether the image will use identical sample locations and the same number of samples for all texels in the image, and the sample locations will not depend on the internal format or size of the image.</param>
        public static void TexImage2DMultisample(int target, int samples, int internalformat, int width, int height, bool fixedsamplelocations) => _glTexImage2DMultisample(target, samples, internalformat, width, height, fixedsamplelocations);

        /// <summary>
        /// Establish the data storage, format, dimensions, and number of samples of a multisample texture's image
        /// </summary>
        /// <param name="target">Specifies the target of the operation.<para>Must be TEXTURE_2D_MULTISAMPLE_ARRAY or PROXY_TEXTURE_2D_MULTISAMPLE_ARRAY.</para></param>
        /// <param name="samples">The number of samples in the multisample texture's image.</param>
        /// <param name="internalformat">The internal format to be used to store the multisample texture's image.<para>Must specify a color-renderable, depth-renderable, or stencil-renderable format.</para></param>
        /// <param name="width">The width of the multisample texture's image, in texels.</param>
        /// <param name="height">The height of the multisample texture's image, in texels.</param>
        /// <param name="depth">The depth of the multisample texture's image, in texels.</param>
        /// <param name="fixedsamplelocations">Specifies whether the image will use identical sample locations and the same number of samples for all texels in the image, and the sample locations will not depend on the internal format or size of the image.</param>
        public static void TexImage3DMultisample(int target, int samples, int internalformat, int width, int height, int depth, bool fixedsamplelocations) => _glTexImage3DMultisample(target, samples, internalformat, width, height, depth, fixedsamplelocations);

        /// <summary>
        /// Specify the value of a uniform variable for the current program object.
        /// </summary>
        /// <param name="location">Specifies the location of the uniform variable to be modified.</param>
        /// <param name="count">Specifies the number of matrices that are to be modified.</param>
        /// <param name="transpose">Specifies whether to transpose the matrix as the values are loaded into the uniform variable.</param>
        /// <param name="value">Specifies a pointer to an array of count values that will be used to update the specified uniform variable.</param>
        public static void UniformMatrix2fv(int location, int count, bool transpose, /*const*/ float* value) => _glUniformMatrix2fv(location, count, transpose, value);

        /// <summary>
        /// Specify the value of a uniform variable for the current program object.
        /// </summary>
        /// <param name="location">Specifies the location of the uniform variable to be modified.</param>
        /// <param name="count">Specifies the number of matrices that are to be modified.</param>
        /// <param name="transpose">Specifies whether to transpose the matrix as the values are loaded into the uniform variable.</param>
        /// <param name="value">Specifies a pointer to an array of count values that will be used to update the specified uniform variable.</param>
        public static void UniformMatrix3fv(int location, int count, bool transpose, /*const*/ float* value) => _glUniformMatrix3fv(location, count, transpose, value);

        /// <summary>
        /// Specify the value of a uniform variable for the current program object.
        /// </summary>
        /// <param name="location">Specifies the location of the uniform variable to be modified.</param>
        /// <param name="count">Specifies the number of matrices that are to be modified.</param>
        /// <param name="transpose">Specifies whether to transpose the matrix as the values are loaded into the uniform variable.</param>
        /// <param name="value">Specifies a pointer to an array of count values that will be used to update the specified uniform variable.</param>
        public static void UniformMatrix4fv(int location, int count, bool transpose, /*const*/ float* value) => _glUniformMatrix4fv(location, count, transpose, value);

        /// <summary>
        /// Specify the value of a uniform variable for the current program object.
        /// </summary>
        /// <param name="location">Specifies the location of the uniform variable to be modified.</param>
        /// <param name="count">Specifies the number of matrices that are to be modified.</param>
        /// <param name="transpose">Specifies whether to transpose the matrix as the values are loaded into the uniform variable.</param>
        /// <param name="value">Specifies a pointer to an array of count values that will be used to update the specified uniform variable.</param>
        public static void UniformMatrix2x3fv(int location, int count, bool transpose, /*const*/ float* value) => _glUniformMatrix2x3fv(location, count, transpose, value);

        /// <summary>
        /// Specify the value of a uniform variable for the current program object.
        /// </summary>
        /// <param name="location">Specifies the location of the uniform variable to be modified.</param>
        /// <param name="count">Specifies the number of matrices that are to be modified.</param>
        /// <param name="transpose">Specifies whether to transpose the matrix as the values are loaded into the uniform variable.</param>
        /// <param name="value">Specifies a pointer to an array of count values that will be used to update the specified uniform variable.</param>
        public static void UniformMatrix3x2fv(int location, int count, bool transpose, /*const*/ float* value) => _glUniformMatrix3x2fv(location, count, transpose, value);

        /// <summary>
        /// Specify the value of a uniform variable for the current program object.
        /// </summary>
        /// <param name="location">Specifies the location of the uniform variable to be modified.</param>
        /// <param name="count">Specifies the number of matrices that are to be modified.</param>
        /// <param name="transpose">Specifies whether to transpose the matrix as the values are loaded into the uniform variable.</param>
        /// <param name="value">Specifies a pointer to an array of count values that will be used to update the specified uniform variable.</param>
        public static void UniformMatrix2x4fv(int location, int count, bool transpose, /*const*/ float* value) => _glUniformMatrix2x4fv(location, count, transpose, value);

        /// <summary>
        /// Specify the value of a uniform variable for the current program object.
        /// </summary>
        /// <param name="location">Specifies the location of the uniform variable to be modified.</param>
        /// <param name="count">Specifies the number of matrices that are to be modified.</param>
        /// <param name="transpose">Specifies whether to transpose the matrix as the values are loaded into the uniform variable.</param>
        /// <param name="value">Specifies a pointer to an array of count values that will be used to update the specified uniform variable.</param>
        public static void UniformMatrix4x2fv(int location, int count, bool transpose, /*const*/ float* value) => _glUniformMatrix4x2fv(location, count, transpose, value);

        /// <summary>
        /// Specify the value of a uniform variable for the current program object.
        /// </summary>
        /// <param name="location">Specifies the location of the uniform variable to be modified.</param>
        /// <param name="count">Specifies the number of matrices that are to be modified.</param>
        /// <param name="transpose">Specifies whether to transpose the matrix as the values are loaded into the uniform variable.</param>
        /// <param name="value">Specifies a pointer to an array of count values that will be used to update the specified uniform variable.</param>
        public static void UniformMatrix3x4fv(int location, int count, bool transpose, /*const*/ float* value) => _glUniformMatrix3x4fv(location, count, transpose, value);

        /// <summary>
        /// Specify the value of a uniform variable for the current program object.
        /// </summary>
        /// <param name="location">Specifies the location of the uniform variable to be modified.</param>
        /// <param name="count">Specifies the number of matrices that are to be modified.</param>
        /// <param name="transpose">Specifies whether to transpose the matrix as the values are loaded into the uniform variable.</param>
        /// <param name="value">Specifies a pointer to an array of count values that will be used to update the specified uniform variable.</param>
        public static void UniformMatrix4x3fv(int location, int count, bool transpose, /*const*/ float* value) => _glUniformMatrix4x3fv(location, count, transpose, value);

        /// <summary>
        /// Specify the value of a uniform variable for the current program object.
        /// </summary>
        /// <param name="location">Specifies the location of the uniform variable to be modified.</param>
        /// <param name="count">Specifies the number of matrices that are to be modified.</param>
        /// <param name="transpose">Specifies whether to transpose the matrix as the values are loaded into the uniform variable.</param>
        /// <param name="values">An array of count values that will be used to update the specified uniform variable.</param>
        public static void UniformMatrix2fv(int location, int count, bool transpose, float[] values) {
            fixed (float* value = &values[0]) {
                _glUniformMatrix2fv(location, count, transpose, value);
            }
        }

        /// <summary>
        /// Specify the value of a uniform variable for the current program object.
        /// </summary>
        /// <param name="location">Specifies the location of the uniform variable to be modified.</param>
        /// <param name="count">Specifies the number of matrices that are to be modified.</param>
        /// <param name="transpose">Specifies whether to transpose the matrix as the values are loaded into the uniform variable.</param>
        /// <param name="values">An array of count values that will be used to update the specified uniform variable.</param>
        public static void UniformMatrix3fv(int location, int count, bool transpose, float[] values) {
            fixed (float* value = &values[0]) {
                _glUniformMatrix3fv(location, count, transpose, value);
            }
        }

        /// <summary>
        /// Specify the value of a uniform variable for the current program object.
        /// </summary>
        /// <param name="location">Specifies the location of the uniform variable to be modified.</param>
        /// <param name="count">Specifies the number of matrices that are to be modified.</param>
        /// <param name="transpose">Specifies whether to transpose the matrix as the values are loaded into the uniform variable.</param>
        /// <param name="values">An array of count values that will be used to update the specified uniform variable.</param>
        public static void UniformMatrix4fv(int location, int count, bool transpose, float[] values) {
            fixed (float* value = &values[0]) {
                _glUniformMatrix4fv(location, count, transpose, value);
            }
        }

        /// <summary>
        /// Specify the value of a uniform variable for the current program object.
        /// </summary>
        /// <param name="location">Specifies the location of the uniform variable to be modified.</param>
        /// <param name="count">Specifies the number of matrices that are to be modified.</param>
        /// <param name="transpose">Specifies whether to transpose the matrix as the values are loaded into the uniform variable.</param>
        /// <param name="values">An array of count values that will be used to update the specified uniform variable.</param>
        public static void UniformMatrix2x3fv(int location, int count, bool transpose, float[] values) {
            fixed (float* value = &values[0]) {
                _glUniformMatrix2x3fv(location, count, transpose, value);
            }
        }

        /// <summary>
        /// Specify the value of a uniform variable for the current program object.
        /// </summary>
        /// <param name="location">Specifies the location of the uniform variable to be modified.</param>
        /// <param name="count">Specifies the number of matrices that are to be modified.</param>
        /// <param name="transpose">Specifies whether to transpose the matrix as the values are loaded into the uniform variable.</param>
        /// <param name="values">An array of count values that will be used to update the specified uniform variable.</param>
        public static void UniformMatrix3x2fv(int location, int count, bool transpose, float[] values) {
            fixed (float* value = &values[0]) {
                _glUniformMatrix3x2fv(location, count, transpose, value);
            }
        }

        /// <summary>
        /// Specify the value of a uniform variable for the current program object.
        /// </summary>
        /// <param name="location">Specifies the location of the uniform variable to be modified.</param>
        /// <param name="count">Specifies the number of matrices that are to be modified.</param>
        /// <param name="transpose">Specifies whether to transpose the matrix as the values are loaded into the uniform variable.</param>
        /// <param name="values">An array of count values that will be used to update the specified uniform variable.</param>
        public static void UniformMatrix2x4fv(int location, int count, bool transpose, float[] values) {
            fixed (float* value = &values[0]) {
                _glUniformMatrix2x4fv(location, count, transpose, value);
            }
        }

        /// <summary>
        /// Specify the value of a uniform variable for the current program object.
        /// </summary>
        /// <param name="location">Specifies the location of the uniform variable to be modified.</param>
        /// <param name="count">Specifies the number of matrices that are to be modified.</param>
        /// <param name="transpose">Specifies whether to transpose the matrix as the values are loaded into the uniform variable.</param>
        /// <param name="values">An array of count values that will be used to update the specified uniform variable.</param>
        public static void UniformMatrix4x2fv(int location, int count, bool transpose, float[] values) {
            fixed (float* value = &values[0]) {
                _glUniformMatrix4x2fv(location, count, transpose, value);
            }
        }

        /// <summary>
        /// Specify the value of a uniform variable for the current program object.
        /// </summary>
        /// <param name="location">Specifies the location of the uniform variable to be modified.</param>
        /// <param name="count">Specifies the number of matrices that are to be modified.</param>
        /// <param name="transpose">Specifies whether to transpose the matrix as the values are loaded into the uniform variable.</param>
        /// <param name="values">An array of count values that will be used to update the specified uniform variable.</param>
        public static void UniformMatrix3x4fv(int location, int count, bool transpose, float[] values) {
            fixed (float* value = &values[0]) {
                _glUniformMatrix3x4fv(location, count, transpose, value);
            }
        }

        /// <summary>
        /// Specify the value of a uniform variable for the current program object.
        /// </summary>
        /// <param name="location">Specifies the location of the uniform variable to be modified.</param>
        /// <param name="count">Specifies the number of matrices that are to be modified.</param>
        /// <param name="transpose">Specifies whether to transpose the matrix as the values are loaded into the uniform variable.</param>
        /// <param name="values">An array of count values that will be used to update the specified uniform variable.</param>
        public static void UniformMatrix4x3fv(int location, int count, bool transpose, float[] values) {
            fixed (float* value = &values[0]) {
                _glUniformMatrix4x3fv(location, count, transpose, value);
            }
        }

        /// <summary>
        /// Set texture parameters.
        /// </summary>
        /// <param name="target">Specifies the target texture.</param>
        /// <param name="pname">Specifies the symbolic name of a single-valued texture parameter.</param>
        /// <param name="args">Specifies the value of the parameters..</param>
        public static void TexParameterIiv(int target, int pname, /*const*/ int* args) => _glTexParameterIiv(target, pname, args);

        /// <summary>
        /// Set texture parameters.
        /// </summary>
        /// <param name="target">Specifies the target texture.</param>
        /// <param name="pname">Specifies the symbolic name of a single-valued texture parameter.</param>
        /// <param name="args">Specifies the value of the parameters..</param>
        public static void TexParameterIuiv(int target, int pname, /*const*/ uint* args) => _glTexParameterIuiv(target, pname, args);

        /// <summary>
        /// Set texture parameters.
        /// </summary>
        /// <param name="target">Specifies the target texture.</param>
        /// <param name="pname">Specifies the symbolic name of a single-valued texture parameter.</param>
        /// <param name="args">Specifies the value of the parameters..</param>
        public static void TexParameterIiv(int target, int pname, int[] args) {
            fixed (int* arg = &args[0]) {
                _glTexParameterIiv(target, pname, arg);
            }
        }

        /// <summary>
        /// Set texture parameters.
        /// </summary>
        /// <param name="target">Specifies the target texture.</param>
        /// <param name="pname">Specifies the symbolic name of a single-valued texture parameter.</param>
        /// <param name="args">Specifies the value of the parameters..</param>
        public static void TexParameterIuiv(int target, int pname, uint[] args) {
            fixed (uint* arg = &args[0]) {
                _glTexParameterIuiv(target, pname, arg);
            }
        }

        /// <summary>
        /// Establish data storage, format, dimensions and sample count of a renderbuffer object's image.
        /// </summary>
        /// <param name="target">Specifies a binding target of the allocation.<para>Must be RENDERBUFFER.</para></param>
        /// <param name="samples">Specifies the number of samples to be used for the renderbuffer object's storage.</param>
        /// <param name="internalformat">Specifies the internal format to use for the renderbuffer object's image.</param>
        /// <param name="width">Specifies the width of the renderbuffer, in pixels.</param>
        /// <param name="height">Specifies the height of the renderbuffer, in pixels.</param>
        public static void RenderbufferStorageMultisample(int target, int samples, int internalformat, int width, int height) => _glRenderbufferStorageMultisample(target, samples, internalformat, width, height);

        /// <summary>
        /// Draw multiple instances of a range of elements.
        /// </summary>
        /// <param name="mode">Specifies what kind of primitives to render.</param>
        /// <param name="first">Specifies the starting index in the enabled arrays.</param>
        /// <param name="count">Specifies the number of indices to be rendered.</param>
        /// <param name="instanceCount">Specifies the number of instances of the specified range of indices to be rendered.</param>
        public static void DrawArraysInstanced(int mode, int first, int count, int instanceCount) => _glDrawArraysInstanced(mode, first, count, instanceCount);

        /// <summary>
        /// Return the address of the specified generic vertex attribute pointer.
        /// </summary>
        /// <param name="index">Specifies the generic vertex attribute parameter to be returned.</param>
        /// <param name="pname">Specifies the symbolic name of the generic vertex attribute parameter to be returned.<para>Must be VERTEX_ATTRIB_ARRAY_POINTER.</para></param>
        /// <returns>The pointer value.</returns>
        public static IntPtr GetVertexAttribPointerv(uint index, int pname) {
            _glGetVertexAttribPointerv(index, pname, out var pointer);
            return pointer;
        }

        /// <summary>
        /// Return the pointer to a mapped buffer object's data store
        /// </summary>
        /// <param name="target">Specifies the target to which the buffer object is bound.</param>
        /// <param name="pname">Specifies the name of the pointer to be returned.<para>Must be BUFFER_MAP_POINTER.</para></param>
        /// <returns>Returns the pointer value specified by pname.</returns>
        public static IntPtr GetBufferPointerv(int target, int pname) {
            _glGetBufferPointerv(target, pname, out var pointer);
            return pointer;
        }

        /// <summary>
        /// Return texture parameter values.
        /// </summary>
        /// <param name="target">Specifies the symbolic name of the target texture.</param>
        /// <param name="pname">Specifies the symbolic name of a texture parameter.</param>
        /// <param name="args">Returns the texture parameters.</param>
        public static void GetTexParameterIiv(int target, int pname, int* args) => _glGetTexParameterIiv(target, pname, args);

        /// <summary>
        /// Return texture parameter values.
        /// </summary>
        /// <param name="target">Specifies the symbolic name of the target texture.</param>
        /// <param name="pname">Specifies the symbolic name of a texture parameter.</param>
        /// <param name="args">Returns the texture parameters.</param>
        public static void GetTexParameterIuiv(int target, int pname, uint* args) => _glGetTexParameterIuiv(target, pname, args);

        /// <summary>
        /// Return texture parameter values.
        /// </summary>
        /// <param name="target">Specifies the symbolic name of the target texture.</param>
        /// <param name="pname">Specifies the symbolic name of a texture parameter.</param>
        /// <param name="count">The number of values to retrieve.</param>
        /// <returns>The texture parameters.</returns>


        public static int[] GetTexParameterIiv(int target, int pname, int count) {
            var values = new int[count];
            fixed (int* args = &values[0]) {
                _glGetTexParameterIiv(target, pname, args);
            }
            return values;
        }

        /// <summary>
        /// Return texture parameter values.
        /// </summary>
        /// <param name="target">Specifies the symbolic name of the target texture.</param>
        /// <param name="pname">Specifies the symbolic name of a texture parameter.</param>
        /// <param name="count">The number of values to retrieve.</param>
        /// <returns>The texture parameters.</returns>


        public static uint[] GetTexParameterIuiv(int target, int pname, int count) {
            var values = new uint[count];
            fixed (uint* args = &values[0]) {
                _glGetTexParameterIuiv(target, pname, args);
            }
            return values;
        }

        /// <summary>
        /// Retrieve the index of a named uniform block.
        /// </summary>
        /// <param name="program">Specifies the name of a program containing the uniform block.</param>
        /// <param name="uniformBlockName">The name of the uniform block whose index to retrieve.</param>
        /// <returns>The index of a uniform block within program.</returns>

        public static uint GetUniformBlockIndex(uint program, string uniformBlockName) {
            var buffer = Encoding.UTF8.GetBytes(uniformBlockName);
            fixed (byte* b = &buffer[0]) {
                return _glGetUniformBlockIndex(program, b);
            }
        }

        /// <summary>
        /// Returns information about several active uniform variables for the specified program object.
        /// </summary>
        /// <param name="program">Specifies the program object to be queried.</param>
        /// <param name="uniformCount">Specifies both the number of elements in the array of indices <paramref name="uniformIndices"/> and the number of parameters written to params upon successful return.</param>
        /// <param name="uniformIndices">Specifies the address of an array of <paramref name="uniformCount"/> integers containing the indices of uniforms within program whose parameter <paramref name="pname"/> should be queried.</param>
        /// <param name="pname">Specifies the property of each uniform in uniformIndices that should be written into the corresponding element of <paramref name="args"/>.</param>
        /// <param name="args">Specifies the address of an array of <paramref name="uniformCount"/> integers which are to receive the value of <paramref name="pname"/> for each uniform in <paramref name="uniformIndices"/>.</param>
        public static void GetActiveUniformsiv(uint program, int uniformCount, /*const*/ uint* uniformIndices, int pname, int* args) => _glGetActiveUniformsiv(program, uniformCount, uniformIndices, pname, args);


        /// <summary>
        /// Returns information about several active uniform variables for the specified program object.
        /// </summary>
        /// <param name="program">Specifies the program object to be queried.</param>
        /// <param name="uniformCount">Specifies both the number of elements in the array of indices <paramref name="uniformIndices"/> and the number of parameters written to params upon successful return.</param>
        /// <param name="uniformIndices">Specifies an array of <paramref name="uniformCount"/> integers containing the indices of uniforms within program whose parameter <paramref name="pname"/> should be queried.</param>
        /// <param name="pname">Specifies the property of each uniform in uniformIndices that should be written into the corresponding element of <paramref name="args"/>.</param>
        /// <param name="args">Specifies an array of <paramref name="uniformCount"/> integers which are to receive the value of <paramref name="pname"/> for each uniform in <paramref name="uniformIndices"/>.</param>
        public static void GetActiveUniformsiv(uint program, int uniformCount, uint[] uniformIndices, int pname, int[] args) {
            fixed (uint* i = &uniformIndices[0]) {
                fixed (int* a = &args[0]) {
                    _glGetActiveUniformsiv(program, uniformCount, i, pname, a);
                }
            }
        }

        /// <summary>
        /// Return parameters of a buffer object.
        /// </summary>
        /// <param name="target">Specifies the target buffer object.<para>Must be ARRAY_BUFFER or ELEMENT_ARRAY_BUFFER.</para></param>
        /// <param name="pname">Specifies the symbolic name of a buffer object parameter.<para>Must be BUFFER_SIZE or BUFFER_USAGE.</para></param>
        /// <param name="args">Returns the requested parameter.</param>
        public static void GetBufferParameteriv(int target, int pname, int* args) => _glGetBufferParameteriv(target, pname, args);

        /// <summary>
        /// Return parameters of a buffer object.
        /// </summary>
        /// <param name="target">Specifies the target buffer object.<para>Must be ARRAY_BUFFER or ELEMENT_ARRAY_BUFFER.</para></param>
        /// <param name="pname">Specifies the symbolic name of a buffer object parameter.<para>Must be BUFFER_SIZE or BUFFER_USAGE.</para></param>
        /// <param name="count">The number of values to return.</param>
        /// <returns>The requested parameter.</returns>


        public static int[] GetBufferParameteriv(int target, int pname, int count) {
            var values = new int[count];
            fixed (int* args = &values[0]) {
                _glGetBufferParameteriv(target, pname, args);
            }
            return values;
        }

        /// <summary>
        /// Query the properties of a sync object.
        /// </summary>
        /// <param name="sync">Specifies the sync object whose properties to query.</param>
        /// <param name="pname">Specifies the parameter whose value to retrieve from the sync object specified in <paramref name="sync"/>.</param>
        /// <param name="bufSize">Specifies the size of the buffer whose address is given in <paramref name="values"/>.</param>
        /// <param name="length">Specifies the address of an variable to receive the number of integers placed in <paramref name="values"/>.</param>
        /// <param name="values">Specifies the address of an array to receive the values of the queried parameter.</param>
        public static void GetSynciv(IntPtr sync, int pname, int bufSize, int* length, int* values) => _glGetSynciv(sync, pname, bufSize, length, values);

        /// <summary>
        /// Query the properties of a sync object.
        /// </summary>
        /// <param name="sync">Specifies the sync object whose properties to query.</param>
        /// <param name="pname">Specifies the parameter whose value to retrieve from the sync object specified in <paramref name="sync"/>.</param>
        /// <param name="count">The number of properties to retrieve.</param>
        /// <param name="length">Specifies the number of integers placed in the return value.</param>
        /// <returns>The specified properties.</returns>


        public static int[] GetSynciv(IntPtr sync, int pname, int count, out int length) {
            var bufSize = count * sizeof(int);
            var values = new int[count];
            fixed (int* v = &values[0]) {
                int len;
                _glGetSynciv(sync, pname, bufSize, &len, v);
                length = len;
            }
            return values;
        }

        /// <summary>
        /// Return parameters of a renderbuffer object.
        /// </summary>
        /// <param name="target">Specifies the target renderbuffer object.<para>Must be RENDERBUFFER.</para></param>
        /// <param name="pname">Specifies the symbolic name of a renderbuffer object parameter.</param>
        /// <param name="args">Returns the requested parameter.</param>
        public static void GetRenderbufferParameteriv(int target, int pname, int* args) => _glGetRenderbufferParameteriv(target, pname, args);

        /// <summary>
        /// Return parameters of a renderbuffer object.
        /// </summary>
        /// <param name="target">Specifies the target renderbuffer object.<para>Must be RENDERBUFFER.</para></param>
        /// <param name="pname">Specifies the symbolic name of a renderbuffer object parameter.</param>
        /// <param name="args">An array to write the requested parameter(s).</param>
        public static void GetRenderbufferParameteriv(int target, int pname, int[] args) {
            fixed (int* a = &args[0]) {
                _glGetRenderbufferParameteriv(target, pname, a);
            }
        }

        /// <summary>
        /// Retrieve the location of a sample.
        /// </summary>
        /// <param name="pname">Specifies the sample parameter name.<para>Must be SAMPLE_POSITION.</para></param>
        /// <param name="index">Specifies the index of the sample whose position to query.</param>
        /// <param name="val">Specifies the address of an array to receive the position of the sample.</param>
        public static void GetMultisamplefv(int pname, uint index, float* val) => _glGetMultisamplefv(pname, index, val);

        /// <summary>
        /// Retrieve the location of a sample.
        /// </summary>
        /// <param name="pname">Specifies the sample parameter name.<para>Must be SAMPLE_POSITION.</para></param>
        /// <param name="index">Specifies the index of the sample whose position to query.</param>
        /// <param name="count">The number of values to recieve.</param>
        /// <returns>The position of the sample.</returns>


        public static float[] GetMultisamplefv(int pname, uint index, int count) {
            var values = new float[count];
            fixed (float* val = &values[0]) {
                _glGetMultisamplefv(pname, index, val);
            }
            return values;
        }

        /// <summary>
        /// Draw multiple instances of a set of elements.
        /// </summary>
        /// <param name="mode">Specifies what kind of primitives to render.</param>
        /// <param name="count">Specifies the number of elements to be rendered.</param>
        /// <param name="type">Specifies the type of the values in indices.<para>Must be one of UNSIGNED_BYTE, UNSIGNED_SHORT, or UNSIGNED_INT.</para></param>
        /// <param name="indices">Specifies a pointer to the location where the indices are stored.</param>
        /// <param name="instanceCount">Specifies the number of instances of the specified range of indices to be rendered.</param>
        public static void DrawElementsInstanced(int mode, int count, int type, /*const*/ void* indices, int instanceCount) => _glDrawElementsInstanced(mode, count, type, indices, instanceCount);


        /// <summary>
        /// Draw multiple instances of a set of elements.
        /// </summary>
        /// <param name="mode">Specifies what kind of primitives to render.</param>
        /// <param name="count">Specifies the number of elements to be rendered.</param>
        /// <param name="indices">The indices.</param>
        /// <param name="instanceCount">Specifies the number of instances of the specified range of indices to be rendered.</param>
        public static void DrawElementsInstanced(int mode, int count, byte[] indices, int instanceCount) {
            fixed (byte* i = &indices[0]) {
                _glDrawElementsInstanced(mode, count, UNSIGNED_BYTE, i, instanceCount);
            }
        }

        /// <summary>
        /// Draw multiple instances of a set of elements.
        /// </summary>
        /// <param name="mode">Specifies what kind of primitives to render.</param>
        /// <param name="count">Specifies the number of elements to be rendered.</param>
        /// <param name="indices">The indices.</param>
        /// <param name="instanceCount">Specifies the number of instances of the specified range of indices to be rendered.</param>
        public static void DrawElementsInstanced(int mode, int count, ushort[] indices, int instanceCount) {
            fixed (ushort* i = &indices[0]) {
                _glDrawElementsInstanced(mode, count, UNSIGNED_SHORT, i, instanceCount);
            }
        }

        /// <summary>
        /// Draw multiple instances of a set of elements.
        /// </summary>
        /// <param name="mode">Specifies what kind of primitives to render.</param>
        /// <param name="count">Specifies the number of elements to be rendered.</param>
        /// <param name="indices">The indices.</param>
        /// <param name="instanceCount">Specifies the number of instances of the specified range of indices to be rendered.</param>
        public static void DrawElementsInstanced(int mode, int count, uint[] indices, int instanceCount) {
            fixed (uint* i = &indices[0]) {
                _glDrawElementsInstanced(mode, count, UNSIGNED_INT, i, instanceCount);
            }
        }

        /// <summary>
        /// Render primitives from array data with a per-element offset.
        /// </summary>
        /// <param name="mode">Specifies what kind of primitives to render. </param>
        /// <param name="count">Specifies the number of elements to be rendered.</param>
        /// <param name="type">Specifies the type of the values in indices.<para>Must be one of UNSIGNED_BYTE, UNSIGNED_SHORT, or UNSIGNED_INT.</para></param>
        /// <param name="indices">Specifies a pointer to the location where the indices are stored.</param>
        /// <param name="baseVertex">Specifies a constant that should be added to each element of indices when choosing elements from the enabled vertex arrays.</param>
        public static void DrawElementsBaseVertex(int mode, int count, int type, /*const*/ void* indices, int baseVertex) => _glDrawElementsBaseVertex(mode, count, type, indices, baseVertex);


        /// <summary>
        /// Render primitives from array data with a per-element offset.
        /// </summary>
        /// <param name="mode">Specifies what kind of primitives to render. </param>
        /// <param name="count">Specifies the number of elements to be rendered.</param>
        /// <param name="indices">Specifies a pointer to the location where the indices are stored.</param>
        /// <param name="baseVertex">Specifies a constant that should be added to each element of indices when choosing elements from the enabled vertex arrays.</param>
        public static void DrawElementsBaseVertex(int mode, int count, byte[] indices, int baseVertex) {
            fixed (byte* i = &indices[0]) {
                DrawElementsBaseVertex(mode, count, UNSIGNED_BYTE, i, baseVertex);
            }
        }

        /// <summary>
        /// Render primitives from array data with a per-element offset.
        /// </summary>
        /// <param name="mode">Specifies what kind of primitives to render. </param>
        /// <param name="count">Specifies the number of elements to be rendered.</param>
        /// <param name="indices">Specifies a pointer to the location where the indices are stored.</param>
        /// <param name="baseVertex">Specifies a constant that should be added to each element of indices when choosing elements from the enabled vertex arrays.</param>
        public static void DrawElementsBaseVertex(int mode, int count, ushort[] indices, int baseVertex) {
            fixed (ushort* i = &indices[0]) {
                DrawElementsBaseVertex(mode, count, UNSIGNED_SHORT, i, baseVertex);
            }
        }

        /// <summary>
        /// Render primitives from array data with a per-element offset.
        /// </summary>
        /// <param name="mode">Specifies what kind of primitives to render. </param>
        /// <param name="count">Specifies the number of elements to be rendered.</param>
        /// <param name="indices">Specifies a pointer to the location where the indices are stored.</param>
        /// <param name="baseVertex">Specifies a constant that should be added to each element of indices when choosing elements from the enabled vertex arrays.</param>
        public static void DrawElementsBaseVertex(int mode, int count, uint[] indices, int baseVertex) {
            fixed (uint* i = &indices[0]) {
                DrawElementsBaseVertex(mode, count, UNSIGNED_INT, i, baseVertex);
            }
        }

        /// <summary>
        /// Render primitives from array data with a per-element offset.
        /// </summary>
        /// <param name="mode">Specifies what kind of primitives to render.</param>
        /// <param name="start">Specifies the minimum array index contained in <paramref name="indices"/>.</param>
        /// <param name="end">Specifies the maximum array index contained in <paramref name="indices"/>.</param>
        /// <param name="count">Specifies the number of elements to be rendered.</param>
        /// <param name="type">Specifies the type of the values in indices.<para>Must be one of UNSIGNED_BYTE, UNSIGNED_SHORT, or UNSIGNED_INT.</para></param>
        /// <param name="indices">Specifies a pointer to the location where the indices are stored.</param>
        /// <param name="baseVertex">Specifies a constant that should be added to each element of indices when choosing elements from the enabled vertex arrays.</param>
        public static void DrawRangeElementsBaseVertex(int mode, uint start, uint end, int count, int type, /*const*/void* indices, int baseVertex) => _glDrawRangeElementsBaseVertex(mode, start, end, count, type, indices, baseVertex);


        /// <summary>
        /// Render primitives from array data with a per-element offset.
        /// </summary>
        /// <param name="mode">Specifies what kind of primitives to render.</param>
        /// <param name="start">Specifies the minimum array index contained in <paramref name="indices"/>.</param>
        /// <param name="end">Specifies the maximum array index contained in <paramref name="indices"/>.</param>
        /// <param name="count">Specifies the number of elements to be rendered.</param>
        /// <param name="indices">The indices.</param>
        /// <param name="baseVertex">Specifies a constant that should be added to each element of indices when choosing elements from the enabled vertex arrays.</param>
        public static void DrawRangeElementsBaseVertex(int mode, uint start, uint end, int count, byte[] indices, int baseVertex) {
            fixed (byte* i = &indices[0]) {
                _glDrawRangeElementsBaseVertex(mode, start, end, count, UNSIGNED_BYTE, i, baseVertex);
            }
        }

        /// <summary>
        /// Render primitives from array data with a per-element offset.
        /// </summary>
        /// <param name="mode">Specifies what kind of primitives to render.</param>
        /// <param name="start">Specifies the minimum array index contained in <paramref name="indices"/>.</param>
        /// <param name="end">Specifies the maximum array index contained in <paramref name="indices"/>.</param>
        /// <param name="count">Specifies the number of elements to be rendered.</param>
        /// <param name="indices">The indices.</param>
        /// <param name="baseVertex">Specifies a constant that should be added to each element of indices when choosing elements from the enabled vertex arrays.</param>
        public static void DrawRangeElementsBaseVertex(int mode, uint start, uint end, int count, ushort[] indices, int baseVertex) {
            fixed (ushort* i = &indices[0]) {
                _glDrawRangeElementsBaseVertex(mode, start, end, count, UNSIGNED_BYTE, i, baseVertex);
            }
        }

        /// <summary>
        /// Render primitives from array data with a per-element offset.
        /// </summary>
        /// <param name="mode">Specifies what kind of primitives to render.</param>
        /// <param name="start">Specifies the minimum array index contained in <paramref name="indices"/>.</param>
        /// <param name="end">Specifies the maximum array index contained in <paramref name="indices"/>.</param>
        /// <param name="count">Specifies the number of elements to be rendered.</param>
        /// <param name="indices">The indices.</param>
        /// <param name="baseVertex">Specifies a constant that should be added to each element of indices when choosing elements from the enabled vertex arrays.</param>
        public static void DrawRangeElementsBaseVertex(int mode, uint start, uint end, int count, uint[] indices, int baseVertex) {
            fixed (uint* i = &indices[0]) {
                _glDrawRangeElementsBaseVertex(mode, start, end, count, UNSIGNED_BYTE, i, baseVertex);
            }
        }

        /// <summary>
        /// Render multiple instances of a set of primitives from array data with a per-element offset.
        /// </summary>
        /// <param name="mode">Specifies what kind of primitives to render.</param>
        /// <param name="count">Specifies the number of elements to be rendered.</param>
        /// <param name="type">Specifies the type of the values in indices.<para>Must be one of UNSIGNED_BYTE, UNSIGNED_SHORT, or UNSIGNED_INT.</para></param>
        /// <param name="indices">Specifies a pointer to the location where the indices are stored.</param>
        /// <param name="instanceCount">Specifies the number of instances of the indexed geometry that should be drawn.</param>
        /// <param name="baseVertex">Specifies a constant that should be added to each element of indices when choosing elements from the enabled vertex arrays.</param>
        public static void DrawElementsInstancedBaseVertex(int mode, int count, int type, /*const*/ void* indices, int instanceCount, int baseVertex) => _glDrawElementsInstancedBaseVertex(mode, count, type, indices, instanceCount, baseVertex);

        /// <summary>
        /// Render multiple instances of a set of primitives from array data with a per-element offset.
        /// </summary>
        /// <param name="mode">Specifies what kind of primitives to render.</param>
        /// <param name="count">Specifies the number of elements to be rendered.</param>
        /// <param name="indices">The indices.</param>
        /// <param name="instanceCount">Specifies the number of instances of the indexed geometry that should be drawn.</param>
        /// <param name="baseVertex">Specifies a constant that should be added to each element of indices when choosing elements from the enabled vertex arrays.</param>
        public static void DrawElementsInstancedBaseVertex(int mode, int count, byte[] indices, int instanceCount, int baseVertex) {
            fixed (byte* i = &indices[0]) {
                _glDrawElementsInstancedBaseVertex(mode, count, UNSIGNED_BYTE, i, instanceCount, baseVertex);
            }
        }

        /// <summary>
        /// Render multiple instances of a set of primitives from array data with a per-element offset.
        /// </summary>
        /// <param name="mode">Specifies what kind of primitives to render.</param>
        /// <param name="count">Specifies the number of elements to be rendered.</param>
        /// <param name="indices">The indices.</param>
        /// <param name="instanceCount">Specifies the number of instances of the indexed geometry that should be drawn.</param>
        /// <param name="baseVertex">Specifies a constant that should be added to each element of indices when choosing elements from the enabled vertex arrays.</param>
        public static void DrawElementsInstancedBaseVertex(int mode, int count, ushort[] indices, int instanceCount, int baseVertex) {
            fixed (ushort* i = &indices[0]) {
                _glDrawElementsInstancedBaseVertex(mode, count, UNSIGNED_SHORT, i, instanceCount, baseVertex);
            }
        }

        /// <summary>
        /// Render multiple instances of a set of primitives from array data with a per-element offset.
        /// </summary>
        /// <param name="mode">Specifies what kind of primitives to render.</param>
        /// <param name="count">Specifies the number of elements to be rendered.</param>
        /// <param name="indices">The indices.</param>
        /// <param name="instanceCount">Specifies the number of instances of the indexed geometry that should be drawn.</param>
        /// <param name="baseVertex">Specifies a constant that should be added to each element of indices when choosing elements from the enabled vertex arrays.</param>
        public static void DrawElementsInstancedBaseVertex(int mode, int count, uint[] indices, int instanceCount, int baseVertex) {
            fixed (uint* i = &indices[0]) {
                _glDrawElementsInstancedBaseVertex(mode, count, UNSIGNED_INT, i, instanceCount, baseVertex);
            }
        }

        /// <summary>
        /// Retrieve the index of a named uniform block.
        /// </summary>
        /// <param name="program">Specifies the name of a program containing uniforms whose indices to query.</param>
        /// <param name="uniformName">The names of the uniform to query.</param>
        /// <returns>The index of the uniform.</returns>
        public static uint GetUniformIndex(uint program, string uniformName) {
            uint index;
            var bytes = new[] { Encoding.UTF8.GetBytes(uniformName) };
            fixed (byte* names = &bytes[0][0]) {
                _glGetUniformIndices(program, 1, &names, &index);
            }
            return index;
        }

        /// <summary>
        /// Return parameters of a buffer object.
        /// </summary>
        /// <param name="target">Specifies the target to which the buffer object is bound.</param>
        /// <param name="pname">Specifies the symbolic name of a buffer object parameter.</param>
        /// <param name="args">Returns the requested parameter.</param>
        public static void GetBufferParameteri64v(int target, int pname, long* args) => _glGetBufferParameteri64v(target, pname, args);


        /// <summary>
        /// Return parameters of a buffer object.
        /// </summary>
        /// <param name="target">Specifies the target to which the buffer object is bound.</param>
        /// <param name="pname">Specifies the symbolic name of a buffer object parameter.</param>
        /// <param name="count">The number of parameters to retrieve.</param>
        /// <returns>The requested parameters.</returns>


        public static long[] GetBufferParameteri64v(int target, int pname, int count) {
            var values = new long[count];
            fixed (long* args = &values[0]) {
                _glGetBufferParameteri64v(target, pname, args);
            }
            return values;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="program">The name of the target program object.</param>
        /// <param name="index">The index of the varying variable whose information to retrieve.</param>
        /// <param name="size">The size of the varying.</param>
        /// <param name="type">The type of the varying.</param>
        /// <param name="name">The name of the varying.</param>
        /// <param name="bufSize">The maximum number of characters, including the null terminator, that may be written into name.</param>
        public static void GetTransformFeedbackVarying(uint program, uint index, out int size, out int type, out string name, int bufSize = 512) {
            var buffer = Marshal.AllocHGlobal(bufSize);
            _glGetTransformFeedbackVarying(program, index, bufSize, out var length, out size, out type, buffer);
            name = PtrToStringUtf8(buffer, length);
            Marshal.FreeHGlobal(buffer);
        }

        /// <summary>
        /// Retrieve information about attachments of a framebuffer object.
        /// </summary>
        /// <param name="target">Specifies the target to which the framebuffer object is bound.</param>
        /// <param name="attachment">Specifies the attachment of the framebuffer object to query.</param>
        /// <param name="pname">Specifies the parameter of attachment to query.</param>
        /// <param name="args">Returns the value of parameter pname for attachment.</param>
        public static void GetFramebufferAttachmentParameteriv(int target, int attachment, int pname, int* args) => _glGetFramebufferAttachmentParameteriv(target, attachment, pname, args);

        /// <summary>
        /// Retrieve information about attachments of a framebuffer object.
        /// </summary>
        /// <param name="target">Specifies the target to which the framebuffer object is bound.</param>
        /// <param name="attachment">Specifies the attachment of the framebuffer object to query.</param>
        /// <param name="pname">Specifies the parameter of attachment to query.</param>
        /// <param name="count">The number of parameters to retrieve.</param>
        /// <returns>Returns the value of parameter pname for attachment.</returns>


        public static int[] GetFramebufferAttachmentParameteriv(int target, int attachment, int pname, int count) {
            var values = new int[count];
            fixed (int* args = &values[0]) {
                _glGetFramebufferAttachmentParameteriv(target, attachment, pname, args);
            }
            return values;
        }

        /// <summary>
        /// Render multiple sets of primitives by specifying indices of array data elements and an index to apply to each index.
        /// </summary>
        /// <param name="mode">Specifies what kind of primitives to render.</param>
        /// <param name="count">Points to an array of the elements counts.</param>
        /// <param name="type">Specifies the type of the values in indices.<para>Must be one of UNSIGNED_BYTE, UNSIGNED_SHORT, or UNSIGNED_INT.</para></param>
        /// <param name="indices">Specifies a pointer to the location where the indices are stored.</param>
        /// <param name="drawCount">Specifies the size of the count, indices and <paramref name="baseVertex"/> arrays.</param>
        /// <param name="baseVertex">Specifies a pointer to the location where the base vertices are stored.</param>
        public static void MultiDrawElementsBaseVertex(int mode, /*const*/ int* count, int type, /*const*/void* /*const*/* indices, int drawCount, /*const*/ int* baseVertex) => _glMultiDrawElementsBaseVertex(mode, count, type, indices, drawCount, baseVertex);

        /// <summary>
        /// Specify values to record in transform feedback buffers.
        /// </summary>
        /// <param name="program">The name of the target program object.</param>
        /// <param name="count">The number of varying variables used for transform feedback.</param>
        /// <param name="varyings">An array of count zero-terminated strings specifying the names of the varying variables to use for transform feedback.</param>
        /// <param name="bufferMode">Identifies the mode used to capture the varying variables when transform feedback is active.<para>ust be INTERLEAVED_ATTRIBS or SEPARATE_ATTRIBS.</para></param>
        public static void TransformFeedbackVaryings(uint program, int count, /*const*/ byte** varyings, int bufferMode) => _glTransformFeedbackVaryings(program, count, varyings, bufferMode);

        public const int DEPTH_BUFFER_BIT = 0x00000100;
        public const int STENCIL_BUFFER_BIT = 0x00000400;
        public const int COLOR_BUFFER_BIT = 0x00004000;
        public const int FALSE = 0;
        public const int TRUE = 1;
        public const int POINTS = 0x0000;
        public const int LINES = 0x0001;
        public const int LINE_LOOP = 0x0002;
        public const int LINE_STRIP = 0x0003;
        public const int TRIANGLES = 0x0004;
        public const int TRIANGLE_STRIP = 0x0005;
        public const int TRIANGLE_FAN = 0x0006;
        public const int NEVER = 0x0200;
        public const int LESS = 0x0201;
        public const int EQUAL = 0x0202;
        public const int LEQUAL = 0x0203;
        public const int GREATER = 0x0204;
        public const int NOTEQUAL = 0x0205;
        public const int GEQUAL = 0x0206;
        public const int ALWAYS = 0x0207;
        public const int ZERO = 0;
        public const int ONE = 1;
        public const int SRC_COLOR = 0x0300;
        public const int ONE_MINUS_SRC_COLOR = 0x0301;
        public const int SRC_ALPHA = 0x0302;
        public const int ONE_MINUS_SRC_ALPHA = 0x0303;
        public const int DST_ALPHA = 0x0304;
        public const int ONE_MINUS_DST_ALPHA = 0x0305;
        public const int DST_COLOR = 0x0306;
        public const int ONE_MINUS_DST_COLOR = 0x0307;
        public const int SRC_ALPHA_SATURATE = 0x0308;
        public const int NONE = 0;
        public const int FRONT_LEFT = 0x0400;
        public const int FRONT_RIGHT = 0x0401;
        public const int BACK_LEFT = 0x0402;
        public const int BACK_RIGHT = 0x0403;
        public const int FRONT = 0x0404;
        public const int BACK = 0x0405;
        public const int LEFT = 0x0406;
        public const int RIGHT = 0x0407;
        public const int FRONT_AND_BACK = 0x0408;
        public const int NO_ERROR = 0;
        public const int INVALID_ENUM = 0x0500;
        public const int INVALID_VALUE = 0x0501;
        public const int INVALID_OPERATION = 0x0502;
        public const int OUT_OF_MEMORY = 0x0505;
        public const int CW = 0x0900;
        public const int CCW = 0x0901;
        public const int POINT_SIZE = 0x0B11;
        public const int POINT_SIZE_RANGE = 0x0B12;
        public const int POINT_SIZE_GRANULARITY = 0x0B13;
        public const int LINE_SMOOTH = 0x0B20;
        public const int LINE_WIDTH = 0x0B21;
        public const int LINE_WIDTH_RANGE = 0x0B22;
        public const int LINE_WIDTH_GRANULARITY = 0x0B23;
        public const int POLYGON_MODE = 0x0B40;
        public const int POLYGON_SMOOTH = 0x0B41;
        public const int CULL_FACE = 0x0B44;
        public const int CULL_FACE_MODE = 0x0B45;
        public const int FRONT_FACE = 0x0B46;
        public const int DEPTH_RANGE = 0x0B70;
        public const int DEPTH_TEST = 0x0B71;
        public const int DEPTH_WRITEMASK = 0x0B72;
        public const int DEPTH_CLEAR_VALUE = 0x0B73;
        public const int DEPTH_FUNC = 0x0B74;
        public const int STENCIL_TEST = 0x0B90;
        public const int STENCIL_CLEAR_VALUE = 0x0B91;
        public const int STENCIL_FUNC = 0x0B92;
        public const int STENCIL_VALUE_MASK = 0x0B93;
        public const int STENCIL_FAIL = 0x0B94;
        public const int STENCIL_PASS_DEPTH_FAIL = 0x0B95;
        public const int STENCIL_PASS_DEPTH_PASS = 0x0B96;
        public const int STENCIL_REF = 0x0B97;
        public const int STENCIL_WRITEMASK = 0x0B98;
        public const int VIEWPORT = 0x0BA2;
        public const int DITHER = 0x0BD0;
        public const int BLEND_DST = 0x0BE0;
        public const int BLEND_SRC = 0x0BE1;
        public const int BLEND = 0x0BE2;
        public const int LOGIC_OP_MODE = 0x0BF0;
        public const int DRAW_BUFFER = 0x0C01;
        public const int READ_BUFFER = 0x0C02;
        public const int SCISSOR_BOX = 0x0C10;
        public const int SCISSOR_TEST = 0x0C11;
        public const int COLOR_CLEAR_VALUE = 0x0C22;
        public const int COLOR_WRITEMASK = 0x0C23;
        public const int DOUBLEBUFFER = 0x0C32;
        public const int STEREO = 0x0C33;
        public const int LINE_SMOOTH_HINT = 0x0C52;
        public const int POLYGON_SMOOTH_HINT = 0x0C53;
        public const int UNPACK_SWAP_BYTES = 0x0CF0;
        public const int UNPACK_LSB_FIRST = 0x0CF1;
        public const int UNPACK_ROW_LENGTH = 0x0CF2;
        public const int UNPACK_SKIP_ROWS = 0x0CF3;
        public const int UNPACK_SKIP_PIXELS = 0x0CF4;
        public const int UNPACK_ALIGNMENT = 0x0CF5;
        public const int PACK_SWAP_BYTES = 0x0D00;
        public const int PACK_LSB_FIRST = 0x0D01;
        public const int PACK_ROW_LENGTH = 0x0D02;
        public const int PACK_SKIP_ROWS = 0x0D03;
        public const int PACK_SKIP_PIXELS = 0x0D04;
        public const int PACK_ALIGNMENT = 0x0D05;
        public const int MAX_TEXTURE_SIZE = 0x0D33;
        public const int MAX_VIEWPORT_DIMS = 0x0D3A;
        public const int SUBPIXEL_BITS = 0x0D50;
        public const int TEXTURE_1D = 0x0DE0;
        public const int TEXTURE_2D = 0x0DE1;
        public const int TEXTURE_WIDTH = 0x1000;
        public const int TEXTURE_HEIGHT = 0x1001;
        public const int TEXTURE_BORDER_COLOR = 0x1004;
        public const int DONT_CARE = 0x1100;
        public const int FASTEST = 0x1101;
        public const int NICEST = 0x1102;
        public const int BYTE = 0x1400;
        public const int UNSIGNED_BYTE = 0x1401;
        public const int SHORT = 0x1402;
        public const int UNSIGNED_SHORT = 0x1403;
        public const int INT = 0x1404;
        public const int UNSIGNED_INT = 0x1405;
        public const int FLOAT = 0x1406;
        public const int CLEAR = 0x1500;
        public const int AND = 0x1501;
        public const int AND_REVERSE = 0x1502;
        public const int COPY = 0x1503;
        public const int AND_INVERTED = 0x1504;
        public const int NOOP = 0x1505;
        public const int XOR = 0x1506;
        public const int OR = 0x1507;
        public const int NOR = 0x1508;
        public const int EQUIV = 0x1509;
        public const int INVERT = 0x150A;
        public const int OR_REVERSE = 0x150B;
        public const int COPY_INVERTED = 0x150C;
        public const int OR_INVERTED = 0x150D;
        public const int NAND = 0x150E;
        public const int SET = 0x150F;
        public const int TEXTURE = 0x1702;
        public const int COLOR = 0x1800;
        public const int DEPTH = 0x1801;
        public const int STENCIL = 0x1802;
        public const int STENCIL_INDEX = 0x1901;
        public const int DEPTH_COMPONENT = 0x1902;
        public const int RED = 0x1903;
        public const int GREEN = 0x1904;
        public const int BLUE = 0x1905;
        public const int ALPHA = 0x1906;
        public const int RGB = 0x1907;
        public const int RGBA = 0x1908;
        public const int POINT = 0x1B00;
        public const int LINE = 0x1B01;
        public const int FILL = 0x1B02;
        public const int KEEP = 0x1E00;
        public const int REPLACE = 0x1E01;
        public const int INCR = 0x1E02;
        public const int DECR = 0x1E03;
        public const int VENDOR = 0x1F00;
        public const int RENDERER = 0x1F01;
        public const int VERSION = 0x1F02;
        public const int EXTENSIONS = 0x1F03;
        public const int NEAREST = 0x2600;
        public const int LINEAR = 0x2601;
        public const int NEAREST_MIPMAP_NEAREST = 0x2700;
        public const int LINEAR_MIPMAP_NEAREST = 0x2701;
        public const int NEAREST_MIPMAP_LINEAR = 0x2702;
        public const int LINEAR_MIPMAP_LINEAR = 0x2703;
        public const int TEXTURE_MAG_FILTER = 0x2800;
        public const int TEXTURE_MIN_FILTER = 0x2801;
        public const int TEXTURE_WRAP_S = 0x2802;
        public const int TEXTURE_WRAP_T = 0x2803;
        public const int REPEAT = 0x2901;
        public const int COLOR_LOGIC_OP = 0x0BF2;
        public const int POLYGON_OFFSET_UNITS = 0x2A00;
        public const int POLYGON_OFFSET_POINT = 0x2A01;
        public const int POLYGON_OFFSET_LINE = 0x2A02;
        public const int POLYGON_OFFSET_FILL = 0x8037;
        public const int POLYGON_OFFSET_FACTOR = 0x8038;
        public const int TEXTURE_BINDING_1D = 0x8068;
        public const int TEXTURE_BINDING_2D = 0x8069;
        public const int TEXTURE_INTERNAL_FORMAT = 0x1003;
        public const int TEXTURE_RED_SIZE = 0x805C;
        public const int TEXTURE_GREEN_SIZE = 0x805D;
        public const int TEXTURE_BLUE_SIZE = 0x805E;
        public const int TEXTURE_ALPHA_SIZE = 0x805F;
        public const int DOUBLE = 0x140A;
        public const int PROXY_TEXTURE_1D = 0x8063;
        public const int PROXY_TEXTURE_2D = 0x8064;
        public const int R3_G3_B2 = 0x2A10;
        public const int RGB4 = 0x804F;
        public const int RGB5 = 0x8050;
        public const int RGB8 = 0x8051;
        public const int RGB10 = 0x8052;
        public const int RGB12 = 0x8053;
        public const int RGB16 = 0x8054;
        public const int RGBA2 = 0x8055;
        public const int RGBA4 = 0x8056;
        public const int RGB5_A1 = 0x8057;
        public const int RGBA8 = 0x8058;
        public const int RGB10_A2 = 0x8059;
        public const int RGBA12 = 0x805A;
        public const int RGBA16 = 0x805B;
        public const int UNSIGNED_BYTE_3_3_2 = 0x8032;
        public const int UNSIGNED_SHORT_4_4_4_4 = 0x8033;
        public const int UNSIGNED_SHORT_5_5_5_1 = 0x8034;
        public const int UNSIGNED_INT_8_8_8_8 = 0x8035;
        public const int UNSIGNED_INT_10_10_10_2 = 0x8036;
        public const int TEXTURE_BINDING_3D = 0x806A;
        public const int PACK_SKIP_IMAGES = 0x806B;
        public const int PACK_IMAGE_HEIGHT = 0x806C;
        public const int UNPACK_SKIP_IMAGES = 0x806D;
        public const int UNPACK_IMAGE_HEIGHT = 0x806E;
        public const int TEXTURE_3D = 0x806F;
        public const int PROXY_TEXTURE_3D = 0x8070;
        public const int TEXTURE_DEPTH = 0x8071;
        public const int TEXTURE_WRAP_R = 0x8072;
        public const int MAX_3D_TEXTURE_SIZE = 0x8073;
        public const int UNSIGNED_BYTE_2_3_3_REV = 0x8362;
        public const int UNSIGNED_SHORT_5_6_5 = 0x8363;
        public const int UNSIGNED_SHORT_5_6_5_REV = 0x8364;
        public const int UNSIGNED_SHORT_4_4_4_4_REV = 0x8365;
        public const int UNSIGNED_SHORT_1_5_5_5_REV = 0x8366;
        public const int UNSIGNED_INT_8_8_8_8_REV = 0x8367;
        public const int UNSIGNED_INT_2_10_10_10_REV = 0x8368;
        public const int BGR = 0x80E0;
        public const int BGRA = 0x80E1;
        public const int MAX_ELEMENTS_VERTICES = 0x80E8;
        public const int MAX_ELEMENTS_INDICES = 0x80E9;
        public const int CLAMP_TO_EDGE = 0x812F;
        public const int TEXTURE_MIN_LOD = 0x813A;
        public const int TEXTURE_MAX_LOD = 0x813B;
        public const int TEXTURE_BASE_LEVEL = 0x813C;
        public const int TEXTURE_MAX_LEVEL = 0x813D;
        public const int SMOOTH_POINT_SIZE_RANGE = 0x0B12;
        public const int SMOOTH_POINT_SIZE_GRANULARITY = 0x0B13;
        public const int SMOOTH_LINE_WIDTH_RANGE = 0x0B22;
        public const int SMOOTH_LINE_WIDTH_GRANULARITY = 0x0B23;
        public const int ALIASED_LINE_WIDTH_RANGE = 0x846E;
        public const int TEXTURE0 = 0x84C0;
        public const int TEXTURE1 = 0x84C1;
        public const int TEXTURE2 = 0x84C2;
        public const int TEXTURE3 = 0x84C3;
        public const int TEXTURE4 = 0x84C4;
        public const int TEXTURE5 = 0x84C5;
        public const int TEXTURE6 = 0x84C6;
        public const int TEXTURE7 = 0x84C7;
        public const int TEXTURE8 = 0x84C8;
        public const int TEXTURE9 = 0x84C9;
        public const int TEXTURE10 = 0x84CA;
        public const int TEXTURE11 = 0x84CB;
        public const int TEXTURE12 = 0x84CC;
        public const int TEXTURE13 = 0x84CD;
        public const int TEXTURE14 = 0x84CE;
        public const int TEXTURE15 = 0x84CF;
        public const int TEXTURE16 = 0x84D0;
        public const int TEXTURE17 = 0x84D1;
        public const int TEXTURE18 = 0x84D2;
        public const int TEXTURE19 = 0x84D3;
        public const int TEXTURE20 = 0x84D4;
        public const int TEXTURE21 = 0x84D5;
        public const int TEXTURE22 = 0x84D6;
        public const int TEXTURE23 = 0x84D7;
        public const int TEXTURE24 = 0x84D8;
        public const int TEXTURE25 = 0x84D9;
        public const int TEXTURE26 = 0x84DA;
        public const int TEXTURE27 = 0x84DB;
        public const int TEXTURE28 = 0x84DC;
        public const int TEXTURE29 = 0x84DD;
        public const int TEXTURE30 = 0x84DE;
        public const int TEXTURE31 = 0x84DF;
        public const int ACTIVE_TEXTURE = 0x84E0;
        public const int MULTISAMPLE = 0x809D;
        public const int SAMPLE_ALPHA_TO_COVERAGE = 0x809E;
        public const int SAMPLE_ALPHA_TO_ONE = 0x809F;
        public const int SAMPLE_COVERAGE = 0x80A0;
        public const int SAMPLE_BUFFERS = 0x80A8;
        public const int SAMPLES = 0x80A9;
        public const int SAMPLE_COVERAGE_VALUE = 0x80AA;
        public const int SAMPLE_COVERAGE_INVERT = 0x80AB;
        public const int TEXTURE_CUBE_MAP = 0x8513;
        public const int TEXTURE_BINDING_CUBE_MAP = 0x8514;
        public const int TEXTURE_CUBE_MAP_POSITIVE_X = 0x8515;
        public const int TEXTURE_CUBE_MAP_NEGATIVE_X = 0x8516;
        public const int TEXTURE_CUBE_MAP_POSITIVE_Y = 0x8517;
        public const int TEXTURE_CUBE_MAP_NEGATIVE_Y = 0x8518;
        public const int TEXTURE_CUBE_MAP_POSITIVE_Z = 0x8519;
        public const int TEXTURE_CUBE_MAP_NEGATIVE_Z = 0x851A;
        public const int PROXY_TEXTURE_CUBE_MAP = 0x851B;
        public const int MAX_CUBE_MAP_TEXTURE_SIZE = 0x851C;
        public const int COMPRESSED_RGB = 0x84ED;
        public const int COMPRESSED_RGBA = 0x84EE;
        public const int TEXTURE_COMPRESSION_HINT = 0x84EF;
        public const int TEXTURE_COMPRESSED_IMAGE_SIZE = 0x86A0;
        public const int TEXTURE_COMPRESSED = 0x86A1;
        public const int NUM_COMPRESSED_TEXTURE_FORMATS = 0x86A2;
        public const int COMPRESSED_TEXTURE_FORMATS = 0x86A3;
        public const int CLAMP_TO_BORDER = 0x812D;
        public const int BLEND_DST_RGB = 0x80C8;
        public const int BLEND_SRC_RGB = 0x80C9;
        public const int BLEND_DST_ALPHA = 0x80CA;
        public const int BLEND_SRC_ALPHA = 0x80CB;
        public const int POINT_FADE_THRESHOLD_SIZE = 0x8128;
        public const int DEPTH_COMPONENT16 = 0x81A5;
        public const int DEPTH_COMPONENT24 = 0x81A6;
        public const int DEPTH_COMPONENT32 = 0x81A7;
        public const int MIRRORED_REPEAT = 0x8370;
        public const int MAX_TEXTURE_LOD_BIAS = 0x84FD;
        public const int TEXTURE_LOD_BIAS = 0x8501;
        public const int INCR_WRAP = 0x8507;
        public const int DECR_WRAP = 0x8508;
        public const int TEXTURE_DEPTH_SIZE = 0x884A;
        public const int TEXTURE_COMPARE_MODE = 0x884C;
        public const int TEXTURE_COMPARE_FUNC = 0x884D;
        public const int BLEND_COLOR = 0x8005;
        public const int BLEND_EQUATION = 0x8009;
        public const int CONSTANT_COLOR = 0x8001;
        public const int ONE_MINUS_CONSTANT_COLOR = 0x8002;
        public const int CONSTANT_ALPHA = 0x8003;
        public const int ONE_MINUS_CONSTANT_ALPHA = 0x8004;
        public const int FUNC_ADD = 0x8006;
        public const int FUNC_REVERSE_SUBTRACT = 0x800B;
        public const int FUNC_SUBTRACT = 0x800A;
        public const int MIN = 0x8007;
        public const int MAX = 0x8008;
        public const int BUFFER_SIZE = 0x8764;
        public const int BUFFER_USAGE = 0x8765;
        public const int QUERY_COUNTER_BITS = 0x8864;
        public const int CURRENT_QUERY = 0x8865;
        public const int QUERY_RESULT = 0x8866;
        public const int QUERY_RESULT_AVAILABLE = 0x8867;
        public const int ARRAY_BUFFER = 0x8892;
        public const int ELEMENT_ARRAY_BUFFER = 0x8893;
        public const int ARRAY_BUFFER_BINDING = 0x8894;
        public const int ELEMENT_ARRAY_BUFFER_BINDING = 0x8895;
        public const int VERTEX_ATTRIB_ARRAY_BUFFER_BINDING = 0x889F;
        public const int READ_ONLY = 0x88B8;
        public const int WRITE_ONLY = 0x88B9;
        public const int READ_WRITE = 0x88BA;
        public const int BUFFER_ACCESS = 0x88BB;
        public const int BUFFER_MAPPED = 0x88BC;
        public const int BUFFER_MAP_POINTER = 0x88BD;
        public const int STREAM_DRAW = 0x88E0;
        public const int STREAM_READ = 0x88E1;
        public const int STREAM_COPY = 0x88E2;
        public const int STATIC_DRAW = 0x88E4;
        public const int STATIC_READ = 0x88E5;
        public const int STATIC_COPY = 0x88E6;
        public const int DYNAMIC_DRAW = 0x88E8;
        public const int DYNAMIC_READ = 0x88E9;
        public const int DYNAMIC_COPY = 0x88EA;
        public const int SAMPLES_PASSED = 0x8914;
        public const int SRC1_ALPHA = 0x8589;
        public const int BLEND_EQUATION_RGB = 0x8009;
        public const int VERTEX_ATTRIB_ARRAY_ENABLED = 0x8622;
        public const int VERTEX_ATTRIB_ARRAY_SIZE = 0x8623;
        public const int VERTEX_ATTRIB_ARRAY_STRIDE = 0x8624;
        public const int VERTEX_ATTRIB_ARRAY_TYPE = 0x8625;
        public const int CURRENT_VERTEX_ATTRIB = 0x8626;
        public const int VERTEX_PROGRAM_POINT_SIZE = 0x8642;
        public const int VERTEX_ATTRIB_ARRAY_POINTER = 0x8645;
        public const int STENCIL_BACK_FUNC = 0x8800;
        public const int STENCIL_BACK_FAIL = 0x8801;
        public const int STENCIL_BACK_PASS_DEPTH_FAIL = 0x8802;
        public const int STENCIL_BACK_PASS_DEPTH_PASS = 0x8803;
        public const int MAX_DRAW_BUFFERS = 0x8824;
        public const int DRAW_BUFFER0 = 0x8825;
        public const int DRAW_BUFFER1 = 0x8826;
        public const int DRAW_BUFFER2 = 0x8827;
        public const int DRAW_BUFFER3 = 0x8828;
        public const int DRAW_BUFFER4 = 0x8829;
        public const int DRAW_BUFFER5 = 0x882A;
        public const int DRAW_BUFFER6 = 0x882B;
        public const int DRAW_BUFFER7 = 0x882C;
        public const int DRAW_BUFFER8 = 0x882D;
        public const int DRAW_BUFFER9 = 0x882E;
        public const int DRAW_BUFFER10 = 0x882F;
        public const int DRAW_BUFFER11 = 0x8830;
        public const int DRAW_BUFFER12 = 0x8831;
        public const int DRAW_BUFFER13 = 0x8832;
        public const int DRAW_BUFFER14 = 0x8833;
        public const int DRAW_BUFFER15 = 0x8834;
        public const int BLEND_EQUATION_ALPHA = 0x883D;
        public const int MAX_VERTEX_ATTRIBS = 0x8869;
        public const int VERTEX_ATTRIB_ARRAY_NORMALIZED = 0x886A;
        public const int MAX_TEXTURE_IMAGE_UNITS = 0x8872;
        public const int FRAGMENT_SHADER = 0x8B30;
        public const int VERTEX_SHADER = 0x8B31;
        public const int MAX_FRAGMENT_UNIFORM_COMPONENTS = 0x8B49;
        public const int MAX_VERTEX_UNIFORM_COMPONENTS = 0x8B4A;
        public const int MAX_VARYING_FLOATS = 0x8B4B;
        public const int MAX_VERTEX_TEXTURE_IMAGE_UNITS = 0x8B4C;
        public const int MAX_COMBINED_TEXTURE_IMAGE_UNITS = 0x8B4D;
        public const int SHADER_TYPE = 0x8B4F;
        public const int FLOAT_VEC2 = 0x8B50;
        public const int FLOAT_VEC3 = 0x8B51;
        public const int FLOAT_VEC4 = 0x8B52;
        public const int INT_VEC2 = 0x8B53;
        public const int INT_VEC3 = 0x8B54;
        public const int INT_VEC4 = 0x8B55;
        public const int BOOL = 0x8B56;
        public const int BOOL_VEC2 = 0x8B57;
        public const int BOOL_VEC3 = 0x8B58;
        public const int BOOL_VEC4 = 0x8B59;
        public const int FLOAT_MAT2 = 0x8B5A;
        public const int FLOAT_MAT3 = 0x8B5B;
        public const int FLOAT_MAT4 = 0x8B5C;
        public const int SAMPLER_1D = 0x8B5D;
        public const int SAMPLER_2D = 0x8B5E;
        public const int SAMPLER_3D = 0x8B5F;
        public const int SAMPLER_CUBE = 0x8B60;
        public const int SAMPLER_1D_SHADOW = 0x8B61;
        public const int SAMPLER_2D_SHADOW = 0x8B62;
        public const int DELETE_STATUS = 0x8B80;
        public const int COMPILE_STATUS = 0x8B81;
        public const int LINK_STATUS = 0x8B82;
        public const int VALIDATE_STATUS = 0x8B83;
        public const int INFO_LOG_LENGTH = 0x8B84;
        public const int ATTACHED_SHADERS = 0x8B85;
        public const int ACTIVE_UNIFORMS = 0x8B86;
        public const int ACTIVE_UNIFORM_MAX_LENGTH = 0x8B87;
        public const int SHADER_SOURCE_LENGTH = 0x8B88;
        public const int ACTIVE_ATTRIBUTES = 0x8B89;
        public const int ACTIVE_ATTRIBUTE_MAX_LENGTH = 0x8B8A;
        public const int FRAGMENT_SHADER_DERIVATIVE_HINT = 0x8B8B;
        public const int SHADING_LANGUAGE_VERSION = 0x8B8C;
        public const int CURRENT_PROGRAM = 0x8B8D;
        public const int POINT_SPRITE_COORD_ORIGIN = 0x8CA0;
        public const int LOWER_LEFT = 0x8CA1;
        public const int UPPER_LEFT = 0x8CA2;
        public const int STENCIL_BACK_REF = 0x8CA3;
        public const int STENCIL_BACK_VALUE_MASK = 0x8CA4;
        public const int STENCIL_BACK_WRITEMASK = 0x8CA5;
        public const int PIXEL_PACK_BUFFER = 0x88EB;
        public const int PIXEL_UNPACK_BUFFER = 0x88EC;
        public const int PIXEL_PACK_BUFFER_BINDING = 0x88ED;
        public const int PIXEL_UNPACK_BUFFER_BINDING = 0x88EF;
        public const int FLOAT_MAT2x3 = 0x8B65;
        public const int FLOAT_MAT2x4 = 0x8B66;
        public const int FLOAT_MAT3x2 = 0x8B67;
        public const int FLOAT_MAT3x4 = 0x8B68;
        public const int FLOAT_MAT4x2 = 0x8B69;
        public const int FLOAT_MAT4x3 = 0x8B6A;
        public const int SRGB = 0x8C40;
        public const int SRGB8 = 0x8C41;
        public const int SRGB_ALPHA = 0x8C42;
        public const int SRGB8_ALPHA8 = 0x8C43;
        public const int COMPRESSED_SRGB = 0x8C48;
        public const int COMPRESSED_SRGB_ALPHA = 0x8C49;
        public const int COMPARE_REF_TO_TEXTURE = 0x884E;
        public const int CLIP_DISTANCE0 = 0x3000;
        public const int CLIP_DISTANCE1 = 0x3001;
        public const int CLIP_DISTANCE2 = 0x3002;
        public const int CLIP_DISTANCE3 = 0x3003;
        public const int CLIP_DISTANCE4 = 0x3004;
        public const int CLIP_DISTANCE5 = 0x3005;
        public const int CLIP_DISTANCE6 = 0x3006;
        public const int CLIP_DISTANCE7 = 0x3007;
        public const int MAX_CLIP_DISTANCES = 0x0D32;
        public const int MAJOR_VERSION = 0x821B;
        public const int MINOR_VERSION = 0x821C;
        public const int NUM_EXTENSIONS = 0x821D;
        public const int CONTEXT_FLAGS = 0x821E;
        public const int COMPRESSED_RED = 0x8225;
        public const int COMPRESSED_RG = 0x8226;
        public const int CONTEXT_FLAG_FORWARD_COMPATIBLE_BIT = 0x00000001;
        public const int RGBA32F = 0x8814;
        public const int RGB32F = 0x8815;
        public const int RGBA16F = 0x881A;
        public const int RGB16F = 0x881B;
        public const int VERTEX_ATTRIB_ARRAY_INTEGER = 0x88FD;
        public const int MAX_ARRAY_TEXTURE_LAYERS = 0x88FF;
        public const int MIN_PROGRAM_TEXEL_OFFSET = 0x8904;
        public const int MAX_PROGRAM_TEXEL_OFFSET = 0x8905;
        public const int CLAMP_READ_COLOR = 0x891C;
        public const int FIXED_ONLY = 0x891D;
        public const int MAX_VARYING_COMPONENTS = 0x8B4B;
        public const int TEXTURE_1D_ARRAY = 0x8C18;
        public const int PROXY_TEXTURE_1D_ARRAY = 0x8C19;
        public const int TEXTURE_2D_ARRAY = 0x8C1A;
        public const int PROXY_TEXTURE_2D_ARRAY = 0x8C1B;
        public const int TEXTURE_BINDING_1D_ARRAY = 0x8C1C;
        public const int TEXTURE_BINDING_2D_ARRAY = 0x8C1D;
        public const int R11F_G11F_B10F = 0x8C3A;
        public const int UNSIGNED_INT_10F_11F_11F_REV = 0x8C3B;
        public const int RGB9_E5 = 0x8C3D;
        public const int UNSIGNED_INT_5_9_9_9_REV = 0x8C3E;
        public const int TEXTURE_SHARED_SIZE = 0x8C3F;
        public const int TRANSFORM_FEEDBACK_VARYING_MAX_LENGTH = 0x8C76;
        public const int TRANSFORM_FEEDBACK_BUFFER_MODE = 0x8C7F;
        public const int MAX_TRANSFORM_FEEDBACK_SEPARATE_COMPONENTS = 0x8C80;
        public const int TRANSFORM_FEEDBACK_VARYINGS = 0x8C83;
        public const int TRANSFORM_FEEDBACK_BUFFER_START = 0x8C84;
        public const int TRANSFORM_FEEDBACK_BUFFER_SIZE = 0x8C85;
        public const int PRIMITIVES_GENERATED = 0x8C87;
        public const int TRANSFORM_FEEDBACK_PRIMITIVES_WRITTEN = 0x8C88;
        public const int RASTERIZER_DISCARD = 0x8C89;
        public const int MAX_TRANSFORM_FEEDBACK_INTERLEAVED_COMPONENTS = 0x8C8A;
        public const int MAX_TRANSFORM_FEEDBACK_SEPARATE_ATTRIBS = 0x8C8B;
        public const int INTERLEAVED_ATTRIBS = 0x8C8C;
        public const int SEPARATE_ATTRIBS = 0x8C8D;
        public const int TRANSFORM_FEEDBACK_BUFFER = 0x8C8E;
        public const int TRANSFORM_FEEDBACK_BUFFER_BINDING = 0x8C8F;
        public const int RGBA32UI = 0x8D70;
        public const int RGB32UI = 0x8D71;
        public const int RGBA16UI = 0x8D76;
        public const int RGB16UI = 0x8D77;
        public const int RGBA8UI = 0x8D7C;
        public const int RGB8UI = 0x8D7D;
        public const int RGBA32I = 0x8D82;
        public const int RGB32I = 0x8D83;
        public const int RGBA16I = 0x8D88;
        public const int RGB16I = 0x8D89;
        public const int RGBA8I = 0x8D8E;
        public const int RGB8I = 0x8D8F;
        public const int RED_INTEGER = 0x8D94;
        public const int GREEN_INTEGER = 0x8D95;
        public const int BLUE_INTEGER = 0x8D96;
        public const int RGB_INTEGER = 0x8D98;
        public const int RGBA_INTEGER = 0x8D99;
        public const int BGR_INTEGER = 0x8D9A;
        public const int BGRA_INTEGER = 0x8D9B;
        public const int SAMPLER_1D_ARRAY = 0x8DC0;
        public const int SAMPLER_2D_ARRAY = 0x8DC1;
        public const int SAMPLER_1D_ARRAY_SHADOW = 0x8DC3;
        public const int SAMPLER_2D_ARRAY_SHADOW = 0x8DC4;
        public const int SAMPLER_CUBE_SHADOW = 0x8DC5;
        public const int UNSIGNED_INT_VEC2 = 0x8DC6;
        public const int UNSIGNED_INT_VEC3 = 0x8DC7;
        public const int UNSIGNED_INT_VEC4 = 0x8DC8;
        public const int INT_SAMPLER_1D = 0x8DC9;
        public const int INT_SAMPLER_2D = 0x8DCA;
        public const int INT_SAMPLER_3D = 0x8DCB;
        public const int INT_SAMPLER_CUBE = 0x8DCC;
        public const int INT_SAMPLER_1D_ARRAY = 0x8DCE;
        public const int INT_SAMPLER_2D_ARRAY = 0x8DCF;
        public const int UNSIGNED_INT_SAMPLER_1D = 0x8DD1;
        public const int UNSIGNED_INT_SAMPLER_2D = 0x8DD2;
        public const int UNSIGNED_INT_SAMPLER_3D = 0x8DD3;
        public const int UNSIGNED_INT_SAMPLER_CUBE = 0x8DD4;
        public const int UNSIGNED_INT_SAMPLER_1D_ARRAY = 0x8DD6;
        public const int UNSIGNED_INT_SAMPLER_2D_ARRAY = 0x8DD7;
        public const int QUERY_WAIT = 0x8E13;
        public const int QUERY_NO_WAIT = 0x8E14;
        public const int QUERY_BY_REGION_WAIT = 0x8E15;
        public const int QUERY_BY_REGION_NO_WAIT = 0x8E16;
        public const int BUFFER_ACCESS_FLAGS = 0x911F;
        public const int BUFFER_MAP_LENGTH = 0x9120;
        public const int BUFFER_MAP_OFFSET = 0x9121;
        public const int DEPTH_COMPONENT32F = 0x8CAC;
        public const int DEPTH32F_STENCIL8 = 0x8CAD;
        public const int FLOAT_32_UNSIGNED_INT_24_8_REV = 0x8DAD;
        public const int INVALID_FRAMEBUFFER_OPERATION = 0x0506;
        public const int FRAMEBUFFER_ATTACHMENT_COLOR_ENCODING = 0x8210;
        public const int FRAMEBUFFER_ATTACHMENT_COMPONENT_TYPE = 0x8211;
        public const int FRAMEBUFFER_ATTACHMENT_RED_SIZE = 0x8212;
        public const int FRAMEBUFFER_ATTACHMENT_GREEN_SIZE = 0x8213;
        public const int FRAMEBUFFER_ATTACHMENT_BLUE_SIZE = 0x8214;
        public const int FRAMEBUFFER_ATTACHMENT_ALPHA_SIZE = 0x8215;
        public const int FRAMEBUFFER_ATTACHMENT_DEPTH_SIZE = 0x8216;
        public const int FRAMEBUFFER_ATTACHMENT_STENCIL_SIZE = 0x8217;
        public const int FRAMEBUFFER_DEFAULT = 0x8218;
        public const int FRAMEBUFFER_UNDEFINED = 0x8219;
        public const int DEPTH_STENCIL_ATTACHMENT = 0x821A;
        public const int MAX_RENDERBUFFER_SIZE = 0x84E8;
        public const int DEPTH_STENCIL = 0x84F9;
        public const int UNSIGNED_INT_24_8 = 0x84FA;
        public const int DEPTH24_STENCIL8 = 0x88F0;
        public const int TEXTURE_STENCIL_SIZE = 0x88F1;
        public const int TEXTURE_RED_TYPE = 0x8C10;
        public const int TEXTURE_GREEN_TYPE = 0x8C11;
        public const int TEXTURE_BLUE_TYPE = 0x8C12;
        public const int TEXTURE_ALPHA_TYPE = 0x8C13;
        public const int TEXTURE_DEPTH_TYPE = 0x8C16;
        public const int UNSIGNED_NORMALIZED = 0x8C17;
        public const int FRAMEBUFFER_BINDING = 0x8CA6;
        public const int DRAW_FRAMEBUFFER_BINDING = 0x8CA6;
        public const int RENDERBUFFER_BINDING = 0x8CA7;
        public const int READ_FRAMEBUFFER = 0x8CA8;
        public const int DRAW_FRAMEBUFFER = 0x8CA9;
        public const int READ_FRAMEBUFFER_BINDING = 0x8CAA;
        public const int RENDERBUFFER_SAMPLES = 0x8CAB;
        public const int FRAMEBUFFER_ATTACHMENT_OBJECT_TYPE = 0x8CD0;
        public const int FRAMEBUFFER_ATTACHMENT_OBJECT_NAME = 0x8CD1;
        public const int FRAMEBUFFER_ATTACHMENT_TEXTURE_LEVEL = 0x8CD2;
        public const int FRAMEBUFFER_ATTACHMENT_TEXTURE_CUBE_MAP_FACE = 0x8CD3;
        public const int FRAMEBUFFER_ATTACHMENT_TEXTURE_LAYER = 0x8CD4;
        public const int FRAMEBUFFER_COMPLETE = 0x8CD5;
        public const int FRAMEBUFFER_INCOMPLETE_ATTACHMENT = 0x8CD6;
        public const int FRAMEBUFFER_INCOMPLETE_MISSING_ATTACHMENT = 0x8CD7;
        public const int FRAMEBUFFER_INCOMPLETE_DRAW_BUFFER = 0x8CDB;
        public const int FRAMEBUFFER_INCOMPLETE_READ_BUFFER = 0x8CDC;
        public const int FRAMEBUFFER_UNSUPPORTED = 0x8CDD;
        public const int MAX_COLOR_ATTACHMENTS = 0x8CDF;
        public const int COLOR_ATTACHMENT0 = 0x8CE0;
        public const int COLOR_ATTACHMENT1 = 0x8CE1;
        public const int COLOR_ATTACHMENT2 = 0x8CE2;
        public const int COLOR_ATTACHMENT3 = 0x8CE3;
        public const int COLOR_ATTACHMENT4 = 0x8CE4;
        public const int COLOR_ATTACHMENT5 = 0x8CE5;
        public const int COLOR_ATTACHMENT6 = 0x8CE6;
        public const int COLOR_ATTACHMENT7 = 0x8CE7;
        public const int COLOR_ATTACHMENT8 = 0x8CE8;
        public const int COLOR_ATTACHMENT9 = 0x8CE9;
        public const int COLOR_ATTACHMENT10 = 0x8CEA;
        public const int COLOR_ATTACHMENT11 = 0x8CEB;
        public const int COLOR_ATTACHMENT12 = 0x8CEC;
        public const int COLOR_ATTACHMENT13 = 0x8CED;
        public const int COLOR_ATTACHMENT14 = 0x8CEE;
        public const int COLOR_ATTACHMENT15 = 0x8CEF;
        public const int COLOR_ATTACHMENT16 = 0x8CF0;
        public const int COLOR_ATTACHMENT17 = 0x8CF1;
        public const int COLOR_ATTACHMENT18 = 0x8CF2;
        public const int COLOR_ATTACHMENT19 = 0x8CF3;
        public const int COLOR_ATTACHMENT20 = 0x8CF4;
        public const int COLOR_ATTACHMENT21 = 0x8CF5;
        public const int COLOR_ATTACHMENT22 = 0x8CF6;
        public const int COLOR_ATTACHMENT23 = 0x8CF7;
        public const int COLOR_ATTACHMENT24 = 0x8CF8;
        public const int COLOR_ATTACHMENT25 = 0x8CF9;
        public const int COLOR_ATTACHMENT26 = 0x8CFA;
        public const int COLOR_ATTACHMENT27 = 0x8CFB;
        public const int COLOR_ATTACHMENT28 = 0x8CFC;
        public const int COLOR_ATTACHMENT29 = 0x8CFD;
        public const int COLOR_ATTACHMENT30 = 0x8CFE;
        public const int COLOR_ATTACHMENT31 = 0x8CFF;
        public const int DEPTH_ATTACHMENT = 0x8D00;
        public const int STENCIL_ATTACHMENT = 0x8D20;
        public const int FRAMEBUFFER = 0x8D40;
        public const int RENDERBUFFER = 0x8D41;
        public const int RENDERBUFFER_WIDTH = 0x8D42;
        public const int RENDERBUFFER_HEIGHT = 0x8D43;
        public const int RENDERBUFFER_INTERNAL_FORMAT = 0x8D44;
        public const int STENCIL_INDEX1 = 0x8D46;
        public const int STENCIL_INDEX4 = 0x8D47;
        public const int STENCIL_INDEX8 = 0x8D48;
        public const int STENCIL_INDEX16 = 0x8D49;
        public const int RENDERBUFFER_RED_SIZE = 0x8D50;
        public const int RENDERBUFFER_GREEN_SIZE = 0x8D51;
        public const int RENDERBUFFER_BLUE_SIZE = 0x8D52;
        public const int RENDERBUFFER_ALPHA_SIZE = 0x8D53;
        public const int RENDERBUFFER_DEPTH_SIZE = 0x8D54;
        public const int RENDERBUFFER_STENCIL_SIZE = 0x8D55;
        public const int FRAMEBUFFER_INCOMPLETE_MULTISAMPLE = 0x8D56;
        public const int MAX_SAMPLES = 0x8D57;
        public const int FRAMEBUFFER_SRGB = 0x8DB9;
        public const int HALF_FLOAT = 0x140B;
        public const int MAP_READ_BIT = 0x0001;
        public const int MAP_WRITE_BIT = 0x0002;
        public const int MAP_INVALIDATE_RANGE_BIT = 0x0004;
        public const int MAP_INVALIDATE_BUFFER_BIT = 0x0008;
        public const int MAP_FLUSH_EXPLICIT_BIT = 0x0010;
        public const int MAP_UNSYNCHRONIZED_BIT = 0x0020;
        public const int COMPRESSED_RED_RGTC1 = 0x8DBB;
        public const int COMPRESSED_SIGNED_RED_RGTC1 = 0x8DBC;
        public const int COMPRESSED_RG_RGTC2 = 0x8DBD;
        public const int COMPRESSED_SIGNED_RG_RGTC2 = 0x8DBE;
        public const int RG = 0x8227;
        public const int RG_INTEGER = 0x8228;
        public const int R8 = 0x8229;
        public const int R16 = 0x822A;
        public const int RG8 = 0x822B;
        public const int RG16 = 0x822C;
        public const int R16F = 0x822D;
        public const int R32F = 0x822E;
        public const int RG16F = 0x822F;
        public const int RG32F = 0x8230;
        public const int R8I = 0x8231;
        public const int R8UI = 0x8232;
        public const int R16I = 0x8233;
        public const int R16UI = 0x8234;
        public const int R32I = 0x8235;
        public const int R32UI = 0x8236;
        public const int RG8I = 0x8237;
        public const int RG8UI = 0x8238;
        public const int RG16I = 0x8239;
        public const int RG16UI = 0x823A;
        public const int RG32I = 0x823B;
        public const int RG32UI = 0x823C;
        public const int VERTEX_ARRAY_BINDING = 0x85B5;
        public const int SAMPLER_2D_RECT = 0x8B63;
        public const int SAMPLER_2D_RECT_SHADOW = 0x8B64;
        public const int SAMPLER_BUFFER = 0x8DC2;
        public const int INT_SAMPLER_2D_RECT = 0x8DCD;
        public const int INT_SAMPLER_BUFFER = 0x8DD0;
        public const int UNSIGNED_INT_SAMPLER_2D_RECT = 0x8DD5;
        public const int UNSIGNED_INT_SAMPLER_BUFFER = 0x8DD8;
        public const int TEXTURE_BUFFER = 0x8C2A;
        public const int MAX_TEXTURE_BUFFER_SIZE = 0x8C2B;
        public const int TEXTURE_BINDING_BUFFER = 0x8C2C;
        public const int TEXTURE_BUFFER_DATA_STORE_BINDING = 0x8C2D;
        public const int TEXTURE_RECTANGLE = 0x84F5;
        public const int TEXTURE_BINDING_RECTANGLE = 0x84F6;
        public const int PROXY_TEXTURE_RECTANGLE = 0x84F7;
        public const int MAX_RECTANGLE_TEXTURE_SIZE = 0x84F8;
        public const int R8_SNORM = 0x8F94;
        public const int RG8_SNORM = 0x8F95;
        public const int RGB8_SNORM = 0x8F96;
        public const int RGBA8_SNORM = 0x8F97;
        public const int R16_SNORM = 0x8F98;
        public const int RG16_SNORM = 0x8F99;
        public const int RGB16_SNORM = 0x8F9A;
        public const int RGBA16_SNORM = 0x8F9B;
        public const int SIGNED_NORMALIZED = 0x8F9C;
        public const int PRIMITIVE_RESTART = 0x8F9D;
        public const int PRIMITIVE_RESTART_INDEX = 0x8F9E;
        public const int COPY_READ_BUFFER = 0x8F36;
        public const int COPY_WRITE_BUFFER = 0x8F37;
        public const int UNIFORM_BUFFER = 0x8A11;
        public const int UNIFORM_BUFFER_BINDING = 0x8A28;
        public const int UNIFORM_BUFFER_START = 0x8A29;
        public const int UNIFORM_BUFFER_SIZE = 0x8A2A;
        public const int MAX_VERTEX_UNIFORM_BLOCKS = 0x8A2B;
        public const int MAX_GEOMETRY_UNIFORM_BLOCKS = 0x8A2C;
        public const int MAX_FRAGMENT_UNIFORM_BLOCKS = 0x8A2D;
        public const int MAX_COMBINED_UNIFORM_BLOCKS = 0x8A2E;
        public const int MAX_UNIFORM_BUFFER_BINDINGS = 0x8A2F;
        public const int MAX_UNIFORM_BLOCK_SIZE = 0x8A30;
        public const int MAX_COMBINED_VERTEX_UNIFORM_COMPONENTS = 0x8A31;
        public const int MAX_COMBINED_GEOMETRY_UNIFORM_COMPONENTS = 0x8A32;
        public const int MAX_COMBINED_FRAGMENT_UNIFORM_COMPONENTS = 0x8A33;
        public const int UNIFORM_BUFFER_OFFSET_ALIGNMENT = 0x8A34;
        public const int ACTIVE_UNIFORM_BLOCK_MAX_NAME_LENGTH = 0x8A35;
        public const int ACTIVE_UNIFORM_BLOCKS = 0x8A36;
        public const int UNIFORM_TYPE = 0x8A37;
        public const int UNIFORM_SIZE = 0x8A38;
        public const int UNIFORM_NAME_LENGTH = 0x8A39;
        public const int UNIFORM_BLOCK_INDEX = 0x8A3A;
        public const int UNIFORM_OFFSET = 0x8A3B;
        public const int UNIFORM_ARRAY_STRIDE = 0x8A3C;
        public const int UNIFORM_MATRIX_STRIDE = 0x8A3D;
        public const int UNIFORM_IS_ROW_MAJOR = 0x8A3E;
        public const int UNIFORM_BLOCK_BINDING = 0x8A3F;
        public const int UNIFORM_BLOCK_DATA_SIZE = 0x8A40;
        public const int UNIFORM_BLOCK_NAME_LENGTH = 0x8A41;
        public const int UNIFORM_BLOCK_ACTIVE_UNIFORMS = 0x8A42;
        public const int UNIFORM_BLOCK_ACTIVE_UNIFORM_INDICES = 0x8A43;
        public const int UNIFORM_BLOCK_REFERENCED_BY_VERTEX_SHADER = 0x8A44;
        public const int UNIFORM_BLOCK_REFERENCED_BY_GEOMETRY_SHADER = 0x8A45;
        public const int UNIFORM_BLOCK_REFERENCED_BY_FRAGMENT_SHADER = 0x8A46;
        public const uint INVALID_INDEX = 0xFFFFFFFF;
        public const int CONTEXT_CORE_PROFILE_BIT = 0x00000001;
        public const int CONTEXT_COMPATIBILITY_PROFILE_BIT = 0x00000002;
        public const int LINES_ADJACENCY = 0x000A;
        public const int LINE_STRIP_ADJACENCY = 0x000B;
        public const int TRIANGLES_ADJACENCY = 0x000C;
        public const int TRIANGLE_STRIP_ADJACENCY = 0x000D;
        public const int PROGRAM_POINT_SIZE = 0x8642;
        public const int MAX_GEOMETRY_TEXTURE_IMAGE_UNITS = 0x8C29;
        public const int FRAMEBUFFER_ATTACHMENT_LAYERED = 0x8DA7;
        public const int FRAMEBUFFER_INCOMPLETE_LAYER_TARGETS = 0x8DA8;
        public const int GEOMETRY_SHADER = 0x8DD9;
        public const int GEOMETRY_VERTICES_OUT = 0x8916;
        public const int GEOMETRY_INPUT_TYPE = 0x8917;
        public const int GEOMETRY_OUTPUT_TYPE = 0x8918;
        public const int MAX_GEOMETRY_UNIFORM_COMPONENTS = 0x8DDF;
        public const int MAX_GEOMETRY_OUTPUT_VERTICES = 0x8DE0;
        public const int MAX_GEOMETRY_TOTAL_OUTPUT_COMPONENTS = 0x8DE1;
        public const int MAX_VERTEX_OUTPUT_COMPONENTS = 0x9122;
        public const int MAX_GEOMETRY_INPUT_COMPONENTS = 0x9123;
        public const int MAX_GEOMETRY_OUTPUT_COMPONENTS = 0x9124;
        public const int MAX_FRAGMENT_INPUT_COMPONENTS = 0x9125;
        public const int CONTEXT_PROFILE_MASK = 0x9126;
        public const int DEPTH_CLAMP = 0x864F;
        public const int QUADS_FOLLOW_PROVOKING_VERTEX_CONVENTION = 0x8E4C;
        public const int FIRST_VERTEX_CONVENTION = 0x8E4D;
        public const int LAST_VERTEX_CONVENTION = 0x8E4E;
        public const int PROVOKING_VERTEX = 0x8E4F;
        public const int TEXTURE_CUBE_MAP_SEAMLESS = 0x884F;
        public const int MAX_SERVER_WAIT_TIMEOUT = 0x9111;
        public const int OBJECT_TYPE = 0x9112;
        public const int SYNC_CONDITION = 0x9113;
        public const int SYNC_STATUS = 0x9114;
        public const int SYNC_FLAGS = 0x9115;
        public const int SYNC_FENCE = 0x9116;
        public const int SYNC_GPU_COMMANDS_COMPLETE = 0x9117;
        public const int UNSIGNALED = 0x9118;
        public const int SIGNALED = 0x9119;
        public const int ALREADY_SIGNALED = 0x911A;
        public const int TIMEOUT_EXPIRED = 0x911B;
        public const int CONDITION_SATISFIED = 0x911C;
        public const int WAIT_FAILED = 0x911D;
        public const ulong TIMEOUT_IGNORED = 0xFFFFFFFFFFFFFFFF;
        public const int SYNC_FLUSH_COMMANDS_BIT = 0x00000001;
        public const int SAMPLE_POSITION = 0x8E50;
        public const int SAMPLE_MASK = 0x8E51;
        public const int SAMPLE_MASK_VALUE = 0x8E52;
        public const int MAX_SAMPLE_MASK_WORDS = 0x8E59;
        public const int TEXTURE_2D_MULTISAMPLE = 0x9100;
        public const int PROXY_TEXTURE_2D_MULTISAMPLE = 0x9101;
        public const int TEXTURE_2D_MULTISAMPLE_ARRAY = 0x9102;
        public const int PROXY_TEXTURE_2D_MULTISAMPLE_ARRAY = 0x9103;
        public const int TEXTURE_BINDING_2D_MULTISAMPLE = 0x9104;
        public const int TEXTURE_BINDING_2D_MULTISAMPLE_ARRAY = 0x9105;
        public const int TEXTURE_SAMPLES = 0x9106;
        public const int TEXTURE_FIXED_SAMPLE_LOCATIONS = 0x9107;
        public const int SAMPLER_2D_MULTISAMPLE = 0x9108;
        public const int INT_SAMPLER_2D_MULTISAMPLE = 0x9109;
        public const int UNSIGNED_INT_SAMPLER_2D_MULTISAMPLE = 0x910A;
        public const int SAMPLER_2D_MULTISAMPLE_ARRAY = 0x910B;
        public const int INT_SAMPLER_2D_MULTISAMPLE_ARRAY = 0x910C;
        public const int UNSIGNED_INT_SAMPLER_2D_MULTISAMPLE_ARRAY = 0x910D;
        public const int MAX_COLOR_TEXTURE_SAMPLES = 0x910E;
        public const int MAX_DEPTH_TEXTURE_SAMPLES = 0x910F;
        public const int MAX_INTEGER_SAMPLES = 0x9110;
        public const int VERTEX_ATTRIB_ARRAY_DIVISOR = 0x88FE;
        public const int SRC1_COLOR = 0x88F9;
        public const int ONE_MINUS_SRC1_COLOR = 0x88FA;
        public const int ONE_MINUS_SRC1_ALPHA = 0x88FB;
        public const int MAX_DUAL_SOURCE_DRAW_BUFFERS = 0x88FC;
        public const int ANY_SAMPLES_PASSED = 0x8C2F;
        public const int SAMPLER_BINDING = 0x8919;
        public const int RGB10_A2UI = 0x906F;
        public const int TEXTURE_SWIZZLE_R = 0x8E42;
        public const int TEXTURE_SWIZZLE_G = 0x8E43;
        public const int TEXTURE_SWIZZLE_B = 0x8E44;
        public const int TEXTURE_SWIZZLE_A = 0x8E45;
        public const int TEXTURE_SWIZZLE_RGBA = 0x8E46;
        public const int TIME_ELAPSED = 0x88BF;
        public const int TIMESTAMP = 0x8E28;
        public const int INT_2_10_10_10_REV = 0x8D9F;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLCULLFACEPROC(int mode);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLFRONTFACEPROC(int mode);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLHINTPROC(int target, int mode);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLLINEWIDTHPROC(float width);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLPOINTSIZEPROC(float size);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLPOLYGONMODEPROC(int face, int mode);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLSCISSORPROC(int x, int y, int width, int height);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLTEXPARAMETERFPROC(int target, int pname, float param);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLTEXPARAMETERFVPROC(int target, int pname, /*const*/ float* args);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLTEXPARAMETERIPROC(int target, int pname, int param);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLTEXPARAMETERIVPROC(int target, int pname, /*const*/ int* args);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLTEXIMAGE1DPROC(int target, int level, int internalformat, int width, int border, int format, int type, /*const*/ void* pixels);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLTEXIMAGE2DPROC(int target, int level, int internalformat, int width, int height, int border, int format, int type, /*const*/ void* pixels);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLDRAWBUFFERPROC(int buf);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLCLEARPROC(uint mask);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLCLEARCOLORPROC(float red, float green, float blue, float alpha);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLCLEARSTENCILPROC(int s);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLCLEARDEPTHPROC(double depth);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLSTENCILMASKPROC(uint mask);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLCOLORMASKPROC(bool red, bool green, bool blue, bool alpha);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLDEPTHMASKPROC(bool flag);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLDISABLEPROC(int cap);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLENABLEPROC(int cap);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLALPHAFUNCPROC(int proc, float threshold);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLFINISHPROC();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLFLUSHPROC();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLBLENDFUNCPROC(int sfactor, int dfactor);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLLOGICOPPROC(int opcode);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLSTENCILFUNCPROC(int func, int reference, uint mask);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLSTENCILOPPROC(int fail, int zfail, int zpass);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLDEPTHFUNCPROC(int func);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLPIXELSTOREFPROC(int pname, float param);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLPIXELSTOREIPROC(int pname, int param);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLREADBUFFERPROC(int src);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLREADPIXELSPROC(int x, int y, int width, int height, int format, int type, void* pixels);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGETBOOLEANVPROC(int pname, bool* data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGETDOUBLEVPROC(int pname, double* data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int PFNGLGETERRORPROC();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGETFLOATVPROC(int pname, float* data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGETINTEGERVPROC(int pname, int* data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate byte* PFNGLGETSTRINGPROC(int name);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGETTEXIMAGEPROC(int target, int level, int format, int type, void* pixels);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGETTEXPARAMETERFVPROC(int target, int pname, float* args);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGETTEXPARAMETERIVPROC(int target, int pname, int* args);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGETTEXLEVELPARAMETERFVPROC(int target, int level, int pname, float* args);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGETTEXLEVELPARAMETERIVPROC(int target, int level, int pname, int* args);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool PFNGLISENABLEDPROC(int cap);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLDEPTHRANGEPROC(double n, double f);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVIEWPORTPROC(int x, int y, int width, int height);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLDRAWARRAYSPROC(int mode, int first, int count);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLDRAWELEMENTSPROC(int mode, int count, int type, /*const*/ void* indices);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLPOLYGONOFFSETPROC(float factor, float units);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLCOPYTEXIMAGE1DPROC(int target, int level, int internalformat, int x, int y, int width, int border);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLCOPYTEXIMAGE2DPROC(int target, int level, int internalformat, int x, int y, int width, int height, int border);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLCOPYTEXSUBIMAGE1DPROC(int target, int level, int xoffset, int x, int y, int width);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLCOPYTEXSUBIMAGE2DPROC(int target, int level, int xoffset, int yoffset, int x, int y, int width, int height);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLTEXSUBIMAGE1DPROC(int target, int level, int xoffset, int width, int format, int type, /*const*/ void* pixels);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLTEXSUBIMAGE2DPROC(int target, int level, int xoffset, int yoffset, int width, int height, int format, int type, /*const*/ void* pixels);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLBINDTEXTUREPROC(int target, uint texture);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLDELETETEXTURESPROC(int n, /*const*/ uint* textures);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGENTEXTURESPROC(int n, uint* textures);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool PFNGLISTEXTUREPROC(uint texture);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLDRAWRANGEELEMENTSPROC(int mode, uint start, uint end, int count, int type, /*const*/ void* indices);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLTEXIMAGE3DPROC(int target, int level, int internalformat, int width, int height, int depth, int border, int format, int type, /*const*/ void* pixels);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLTEXSUBIMAGE3DPROC(int target, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, int format, int type, /*const*/ void* pixels);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLCOPYTEXSUBIMAGE3DPROC(int target, int level, int xoffset, int yoffset, int zoffset, int x, int y, int width, int height);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLACTIVETEXTUREPROC(int texture);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLSAMPLECOVERAGEPROC(float value, bool invert);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLCOMPRESSEDTEXIMAGE3DPROC(int target, int level, int internalformat, int width, int height, int depth, int border, int imageSize, /*const*/ void* data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLCOMPRESSEDTEXIMAGE2DPROC(int target, int level, int internalformat, int width, int height, int border, int imageSize, /*const*/ void* data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLCOMPRESSEDTEXIMAGE1DPROC(int target, int level, int internalformat, int width, int border, int imageSize, /*const*/ void* data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLCOMPRESSEDTEXSUBIMAGE3DPROC(int target, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, int format, int imageSize, /*const*/ void* data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLCOMPRESSEDTEXSUBIMAGE2DPROC(int target, int level, int xoffset, int yoffset, int width, int height, int format, int imageSize, /*const*/ void* data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLCOMPRESSEDTEXSUBIMAGE1DPROC(int target, int level, int xoffset, int width, int format, int imageSize, /*const*/ void* data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGETCOMPRESSEDTEXIMAGEPROC(int target, int level, void* img);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLBLENDFUNCSEPARATEPROC(int sfactorRGB, int dfactorRGB, int sfactorAlpha, int dfactorAlpha);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLMULTIDRAWARRAYSPROC(int mode, /*const*/ int* first, /*const*/ int* count, int drawCount);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLMULTIDRAWELEMENTSPROC(int mode, /*const*/ int* count, int type, /*const*/ void*/*const*/* indices, int drawCount);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLPOINTPARAMETERFPROC(int pname, float param);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLPOINTPARAMETERFVPROC(int pname, /*const*/ float* args);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLPOINTPARAMETERIPROC(int pname, int param);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLPOINTPARAMETERIVPROC(int pname, /*const*/ int* args);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLBLENDCOLORPROC(float red, float green, float blue, float alpha);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLBLENDEQUATIONPROC(int mode);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGENQUERIESPROC(int n, uint* ids);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLDELETEQUERIESPROC(int n, /*const*/ uint* ids);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool PFNGLISQUERYPROC(uint id);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLBEGINQUERYPROC(int target, uint id);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLENDQUERYPROC(int target);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGETQUERYIVPROC(int target, int pname, int* args);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGETQUERYOBJECTIVPROC(uint id, int pname, int* args);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGETQUERYOBJECTUIVPROC(uint id, int pname, uint* args);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLBINDBUFFERPROC(int target, uint buffer);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLDELETEBUFFERSPROC(int n, /*const*/ uint* buffers);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGENBUFFERSPROC(int n, uint* buffers);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool PFNGLISBUFFERPROC(uint buffer);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLBUFFERDATAPROC(int target, IntPtr size, /*const*/ void* data, int usage);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLBUFFERSUBDATAPROC(int target, IntPtr offset, IntPtr size, /*const*/ void* data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGETBUFFERSUBDATAPROC(int target, IntPtr offset, IntPtr size, void* data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void* PFNGLMAPBUFFERPROC(int target, int access);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool PFNGLUNMAPBUFFERPROC(int target);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGETBUFFERPARAMETERIVPROC(int target, int pname, int* args);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGETBUFFERPOINTERVPROC(int target, int pname, out IntPtr args);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLBLENDEQUATIONSEPARATEPROC(int modeRGB, int modeAlpha);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLDRAWBUFFERSPROC(int n, /*const*/ int* bufs);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLSTENCILOPSEPARATEPROC(int face, int sfail, int dpfail, int dppass);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLSTENCILFUNCSEPARATEPROC(int face, int func, int reference, uint mask);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLSTENCILMASKSEPARATEPROC(int face, uint mask);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLATTACHSHADERPROC(uint program, uint shader);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLBINDATTRIBLOCATIONPROC(uint program, uint index, /*const*/ byte* name);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLCOMPILESHADERPROC(uint shader);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate uint PFNGLCREATEPROGRAMPROC();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate uint PFNGLCREATESHADERPROC(int type);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLDELETEPROGRAMPROC(uint program);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLDELETESHADERPROC(uint shader);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLDETACHSHADERPROC(uint program, uint shader);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLDISABLEVERTEXATTRIBARRAYPROC(uint index);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLENABLEVERTEXATTRIBARRAYPROC(uint index);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGETACTIVEATTRIBPROC(uint program, uint index, int bufSize, out int length, out int size, out int type, IntPtr name);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGETACTIVEUNIFORMPROC(uint program, uint index, int bufSize, out int length, out int size, out int type, IntPtr name);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGETATTACHEDSHADERSPROC(uint program, int maxCount, int* count, uint* shaders);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int PFNGLGETATTRIBLOCATIONPROC(uint program, /*const*/ byte* name);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGETPROGRAMIVPROC(uint program, int pname, int* args);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGETPROGRAMINFOLOGPROC(uint program, int bufSize, int* length, byte* infoLog);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGETSHADERIVPROC(uint shader, int pname, int* args);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGETSHADERINFOLOGPROC(uint shader, int bufSize, int* length, byte* infoLog);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGETSHADERSOURCEPROC(uint shader, int bufSize, int* length, byte* source);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int PFNGLGETUNIFORMLOCATIONPROC(uint program, /*const*/ byte* name);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGETUNIFORMFVPROC(uint program, int location, float* args);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGETUNIFORMIVPROC(uint program, int location, int* args);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGETVERTEXATTRIBDVPROC(uint index, int pname, double* args);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGETVERTEXATTRIBFVPROC(uint index, int pname, float* args);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGETVERTEXATTRIBIVPROC(uint index, int pname, int* args);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGETVERTEXATTRIBPOINTERVPROC(uint index, int pname, out IntPtr pointer);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool PFNGLISPROGRAMPROC(uint program);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool PFNGLISSHADERPROC(uint shader);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLLINKPROGRAMPROC(uint program);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLSHADERSOURCEPROC(uint shader, int count, /*const*/ byte** str, /*const*/ int* length);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLUSEPROGRAMPROC(uint program);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLUNIFORM1FPROC(int location, float v0);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLUNIFORM2FPROC(int location, float v0, float v1);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLUNIFORM3FPROC(int location, float v0, float v1, float v2);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLUNIFORM4FPROC(int location, float v0, float v1, float v2, float v3);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLUNIFORM1IPROC(int location, int v0);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLUNIFORM2IPROC(int location, int v0, int v1);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLUNIFORM3IPROC(int location, int v0, int v1, int v2);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLUNIFORM4IPROC(int location, int v0, int v1, int v2, int v3);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLUNIFORM1FVPROC(int location, int count, /*const*/ float* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLUNIFORM2FVPROC(int location, int count, /*const*/ float* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLUNIFORM3FVPROC(int location, int count, /*const*/ float* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLUNIFORM4FVPROC(int location, int count, /*const*/ float* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLUNIFORM1IVPROC(int location, int count, /*const*/ int* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLUNIFORM2IVPROC(int location, int count, /*const*/ int* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLUNIFORM3IVPROC(int location, int count, /*const*/ int* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLUNIFORM4IVPROC(int location, int count, /*const*/ int* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLUNIFORMMATRIX2FVPROC(int location, int count, bool transpose, /*const*/ float* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLUNIFORMMATRIX3FVPROC(int location, int count, bool transpose, /*const*/ float* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLUNIFORMMATRIX4FVPROC(int location, int count, bool transpose, /*const*/ float* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVALIDATEPROGRAMPROC(uint program);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIB1DPROC(uint index, double x);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIB1DVPROC(uint index, /*const*/ double* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIB1FPROC(uint index, float x);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIB1FVPROC(uint index, /*const*/ float* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIB1SPROC(uint index, short x);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIB1SVPROC(uint index, /*const*/ short* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIB2DPROC(uint index, double x, double y);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIB2DVPROC(uint index, /*const*/ double* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIB2FPROC(uint index, float x, float y);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIB2FVPROC(uint index, /*const*/ float* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIB2SPROC(uint index, short x, short y);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIB2SVPROC(uint index, /*const*/ short* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIB3DPROC(uint index, double x, double y, double z);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIB3DVPROC(uint index, /*const*/ double* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIB3FPROC(uint index, float x, float y, float z);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIB3FVPROC(uint index, /*const*/ float* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIB3SPROC(uint index, short x, short y, short z);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIB3SVPROC(uint index, /*const*/ short* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIB4NBVPROC(uint index, /*const*/ sbyte* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIB4NIVPROC(uint index, /*const*/ int* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIB4NSVPROC(uint index, /*const*/ short* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIB4NUBPROC(uint index, byte x, byte y, byte z, byte w);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIB4NUBVPROC(uint index, /*const*/ byte* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIB4NUIVPROC(uint index, /*const*/ uint* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIB4NUSVPROC(uint index, /*const*/ ushort* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIB4BVPROC(uint index, /*const*/ sbyte* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIB4DPROC(uint index, double x, double y, double z, double w);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIB4DVPROC(uint index, /*const*/ double* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIB4FPROC(uint index, float x, float y, float z, float w);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIB4FVPROC(uint index, /*const*/ float* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIB4IVPROC(uint index, /*const*/ int* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIB4SPROC(uint index, short x, short y, short z, short w);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIB4SVPROC(uint index, /*const*/ short* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIB4UBVPROC(uint index, /*const*/ byte* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIB4UIVPROC(uint index, /*const*/ uint* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIB4USVPROC(uint index, /*const*/ ushort* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIBPOINTERPROC(uint index, int size, int type, bool normalized, int stride, /*const*/ void* pointer);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLUNIFORMMATRIX2X3FVPROC(int location, int count, bool transpose, /*const*/ float* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLUNIFORMMATRIX3X2FVPROC(int location, int count, bool transpose, /*const*/ float* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLUNIFORMMATRIX2X4FVPROC(int location, int count, bool transpose, /*const*/ float* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLUNIFORMMATRIX4X2FVPROC(int location, int count, bool transpose, /*const*/ float* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLUNIFORMMATRIX3X4FVPROC(int location, int count, bool transpose, /*const*/ float* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLUNIFORMMATRIX4X3FVPROC(int location, int count, bool transpose, /*const*/ float* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLCOLORMASKIPROC(uint index, bool r, bool g, bool b, bool a);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGETBOOLEANI_VPROC(int target, uint index, bool* data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGETINTEGERI_VPROC(int target, uint index, int* data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLENABLEIPROC(int target, uint index);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLDISABLEIPROC(int target, uint index);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool PFNGLISENABLEDIPROC(int target, uint index);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLBEGINTRANSFORMFEEDBACKPROC(int primitiveMode);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLENDTRANSFORMFEEDBACKPROC();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLBINDBUFFERRANGEPROC(int target, uint index, uint buffer, IntPtr offset, IntPtr size);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLBINDBUFFERBASEPROC(int target, uint index, uint buffer);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLTRANSFORMFEEDBACKVARYINGSPROC(uint program, int count, /*const*/ byte** varyings, int bufferMode);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGETTRANSFORMFEEDBACKVARYINGPROC(uint program, uint index, int bufSize, out int length, out int size, out int type, IntPtr name);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLCLAMPCOLORPROC(int target, int clamp);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLBEGINCONDITIONALRENDERPROC(uint id, int mode);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLENDCONDITIONALRENDERPROC();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIBIPOINTERPROC(uint index, int size, int type, int stride, /*const*/ void* pointer);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGETVERTEXATTRIBIIVPROC(uint index, int pname, int* args);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGETVERTEXATTRIBIUIVPROC(uint index, int pname, uint* args);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIBI1IPROC(uint index, int x);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIBI2IPROC(uint index, int x, int y);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIBI3IPROC(uint index, int x, int y, int z);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIBI4IPROC(uint index, int x, int y, int z, int w);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIBI1UIPROC(uint index, uint x);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIBI2UIPROC(uint index, uint x, uint y);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIBI3UIPROC(uint index, uint x, uint y, uint z);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIBI4UIPROC(uint index, uint x, uint y, uint z, uint w);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIBI1IVPROC(uint index, /*const*/ int* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIBI2IVPROC(uint index, /*const*/ int* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIBI3IVPROC(uint index, /*const*/ int* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIBI4IVPROC(uint index, /*const*/ int* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIBI1UIVPROC(uint index, /*const*/ uint* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIBI2UIVPROC(uint index, /*const*/ uint* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIBI3UIVPROC(uint index, /*const*/ uint* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIBI4UIVPROC(uint index, /*const*/ uint* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIBI4BVPROC(uint index, /*const*/ sbyte* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIBI4SVPROC(uint index, /*const*/ short* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIBI4UBVPROC(uint index, /*const*/ byte* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIBI4USVPROC(uint index, /*const*/ ushort* v);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGETUNIFORMUIVPROC(uint program, int location, uint* args);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLBINDFRAGDATALOCATIONPROC(uint program, uint color, /*const*/ byte* name);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int PFNGLGETFRAGDATALOCATIONPROC(uint program, /*const*/ byte* name);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLUNIFORM1UIPROC(int location, uint v0);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLUNIFORM2UIPROC(int location, uint v0, uint v1);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLUNIFORM3UIPROC(int location, uint v0, uint v1, uint v2);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLUNIFORM4UIPROC(int location, uint v0, uint v1, uint v2, uint v3);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLUNIFORM1UIVPROC(int location, int count, /*const*/ uint* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLUNIFORM2UIVPROC(int location, int count, /*const*/ uint* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLUNIFORM3UIVPROC(int location, int count, /*const*/ uint* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLUNIFORM4UIVPROC(int location, int count, /*const*/ uint* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLTEXPARAMETERIIVPROC(int target, int pname, /*const*/ int* args);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLTEXPARAMETERIUIVPROC(int target, int pname, /*const*/ uint* args);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGETTEXPARAMETERIIVPROC(int target, int pname, int* args);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGETTEXPARAMETERIUIVPROC(int target, int pname, uint* args);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLCLEARBUFFERIVPROC(int buffer, int drawbuffer, /*const*/ int* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLCLEARBUFFERUIVPROC(int buffer, int drawbuffer, /*const*/ uint* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLCLEARBUFFERFVPROC(int buffer, int drawbuffer, /*const*/ float* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLCLEARBUFFERFIPROC(int buffer, int drawbuffer, float depth, int stencil);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate byte* PFNGLGETSTRINGIPROC(int name, uint index);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool PFNGLISRENDERBUFFERPROC(uint renderbuffer);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLBINDRENDERBUFFERPROC(int target, uint renderbuffer);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLDELETERENDERBUFFERSPROC(int n, /*const*/ uint* renderbuffers);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGENRENDERBUFFERSPROC(int n, uint* renderbuffers);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLRENDERBUFFERSTORAGEPROC(int target, int internalformat, int width, int height);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGETRENDERBUFFERPARAMETERIVPROC(int target, int pname, int* args);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool PFNGLISFRAMEBUFFERPROC(uint framebuffer);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLBINDFRAMEBUFFERPROC(int target, uint framebuffer);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLDELETEFRAMEBUFFERSPROC(int n, /*const*/ uint* framebuffers);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGENFRAMEBUFFERSPROC(int n, uint* framebuffers);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int PFNGLCHECKFRAMEBUFFERSTATUSPROC(int target);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLFRAMEBUFFERTEXTURE1DPROC(int target, int attachment, int textarget, uint texture, int level);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLFRAMEBUFFERTEXTURE2DPROC(int target, int attachment, int textarget, uint texture, int level);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLFRAMEBUFFERTEXTURE3DPROC(int target, int attachment, int textarget, uint texture, int level, int zoffset);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLFRAMEBUFFERRENDERBUFFERPROC(int target, int attachment, int renderbuffertarget, uint renderbuffer);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGETFRAMEBUFFERATTACHMENTPARAMETERIVPROC(int target, int attachment, int pname, int* args);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGENERATEMIPMAPPROC(int target);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLBLITFRAMEBUFFERPROC(int srcX0, int srcY0, int srcX1, int srcY1, int dstX0, int dstY0, int dstX1, int dstY1, uint mask, int filter);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLRENDERBUFFERSTORAGEMULTISAMPLEPROC(int target, int samples, int internalformat, int width, int height);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLFRAMEBUFFERTEXTURELAYERPROC(int target, int attachment, uint texture, int level, int layer);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void* PFNGLMAPBUFFERRANGEPROC(int target, IntPtr offset, IntPtr length, uint access);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLFLUSHMAPPEDBUFFERRANGEPROC(int target, IntPtr offset, IntPtr length);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLBINDVERTEXARRAYPROC(uint array);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLDELETEVERTEXARRAYSPROC(int n, /*const*/ uint* arrays);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGENVERTEXARRAYSPROC(int n, uint* arrays);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool PFNGLISVERTEXARRAYPROC(uint array);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLDRAWARRAYSINSTANCEDPROC(int mode, int first, int count, int instanceCount);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLDRAWELEMENTSINSTANCEDPROC(int mode, int count, int type, /*const*/ void* indices, int instanceCount);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLTEXBUFFERPROC(int target, int internalformat, uint buffer);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLPRIMITIVERESTARTINDEXPROC(uint index);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLCOPYBUFFERSUBDATAPROC(int readTarget, int writeTarget, IntPtr readOffset, IntPtr writeOffset, IntPtr size);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGETUNIFORMINDICESPROC(uint program, int uniformCount, /*const*/ byte** uniformNames, uint* uniformIndices);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGETACTIVEUNIFORMSIVPROC(uint program, int uniformCount, /*const*/ uint* uniformIndices, int pname, int* args);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGETACTIVEUNIFORMNAMEPROC(uint program, uint uniformIndex, int bufSize, int* length, byte* uniformName);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate uint PFNGLGETUNIFORMBLOCKINDEXPROC(uint program, /*const*/ byte* uniformBlockName);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGETACTIVEUNIFORMBLOCKIVPROC(uint program, uint uniformBlockIndex, int pname, int* args);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGETACTIVEUNIFORMBLOCKNAMEPROC(uint program, uint uniformBlockIndex, int bufSize, int* length, byte* uniformBlockName);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLUNIFORMBLOCKBINDINGPROC(uint program, uint uniformBlockIndex, uint uniformBlockBinding);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLDRAWELEMENTSBASEVERTEXPROC(int mode, int count, int type, /*const*/ void* indices, int baseVertex);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLDRAWRANGEELEMENTSBASEVERTEXPROC(int mode, uint start, uint end, int count, int type, /*const*/ void* indices, int baseVertex);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLDRAWELEMENTSINSTANCEDBASEVERTEXPROC(int mode, int count, int type, /*const*/ void* indices, int instanceCount, int baseVertex);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLMULTIDRAWELEMENTSBASEVERTEXPROC(int mode, /*const*/ int* count, int type, /*const*/ void*/*const*/* indices, int drawCount, /*const*/ int* baseVertex);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLPROVOKINGVERTEXPROC(int mode);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr PFNGLFENCESYNCPROC(int condition, uint flags);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool PFNGLISSYNCPROC(IntPtr sync);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLDELETESYNCPROC(IntPtr sync);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int PFNGLCLIENTWAITSYNCPROC(IntPtr sync, uint flags, ulong timeout);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLWAITSYNCPROC(IntPtr sync, uint flags, ulong timeout);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGETINTEGER64VPROC(int pname, long* data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGETSYNCIVPROC(IntPtr sync, int pname, int bufSize, int* length, int* values);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGETINTEGER64I_VPROC(int target, uint index, long* data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGETBUFFERPARAMETERI64VPROC(int target, int pname, long* args);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLFRAMEBUFFERTEXTUREPROC(int target, int attachment, uint texture, int level);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLTEXIMAGE2DMULTISAMPLEPROC(int target, int samples, int internalformat, int width, int height, bool fixedsamplelocations);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLTEXIMAGE3DMULTISAMPLEPROC(int target, int samples, int internalformat, int width, int height, int depth, bool fixedsamplelocations);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGETMULTISAMPLEFVPROC(int pname, uint index, float* val);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLSAMPLEMASKIPROC(uint maskNumber, uint mask);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLBINDFRAGDATALOCATIONINDEXEDPROC(uint program, uint colorNumber, uint index, /*const*/ byte* name);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int PFNGLGETFRAGDATAINDEXPROC(uint program, /*const*/ byte* name);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGENSAMPLERSPROC(int count, uint* samplers);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLDELETESAMPLERSPROC(int count, /*const*/ uint* samplers);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool PFNGLISSAMPLERPROC(uint sampler);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLBINDSAMPLERPROC(uint unit, uint sampler);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLSAMPLERPARAMETERIPROC(uint sampler, int pname, int param);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLSAMPLERPARAMETERIVPROC(uint sampler, int pname, /*const*/ int* param);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLSAMPLERPARAMETERFPROC(uint sampler, int pname, float param);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLSAMPLERPARAMETERFVPROC(uint sampler, int pname, /*const*/ float* param);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLSAMPLERPARAMETERIIVPROC(uint sampler, int pname, /*const*/ int* param);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLSAMPLERPARAMETERIUIVPROC(uint sampler, int pname, /*const*/ uint* param);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGETSAMPLERPARAMETERIVPROC(uint sampler, int pname, int* args);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGETSAMPLERPARAMETERIIVPROC(uint sampler, int pname, int* args);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGETSAMPLERPARAMETERFVPROC(uint sampler, int pname, float* args);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGETSAMPLERPARAMETERIUIVPROC(uint sampler, int pname, uint* args);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGETQUERYOBJECTI64VPROC(uint id, int pname, long* args);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLGETQUERYOBJECTUI64VPROC(uint id, int pname, ulong* args);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIBDIVISORPROC(uint index, uint divisor);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIBP1UIPROC(uint index, int type, bool normalized, uint value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIBP1UIVPROC(uint index, int type, bool normalized, /*const*/ uint* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIBP2UIPROC(uint index, int type, bool normalized, uint value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIBP2UIVPROC(uint index, int type, bool normalized, /*const*/ uint* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIBP3UIPROC(uint index, int type, bool normalized, uint value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIBP3UIVPROC(uint index, int type, bool normalized, /*const*/ uint* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIBP4UIPROC(uint index, int type, bool normalized, uint value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXATTRIBP4UIVPROC(uint index, int type, bool normalized, /*const*/ uint* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXP2UIPROC(int type, uint value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXP2UIVPROC(int type, /*const*/ uint* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXP3UIPROC(int type, uint value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXP3UIVPROC(int type, /*const*/ uint* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXP4UIPROC(int type, uint value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLVERTEXP4UIVPROC(int type, /*const*/ uint* value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLTEXCOORDP1UIPROC(int type, uint coords);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLTEXCOORDP1UIVPROC(int type, /*const*/ uint* coords);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLTEXCOORDP2UIPROC(int type, uint coords);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLTEXCOORDP2UIVPROC(int type, /*const*/ uint* coords);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLTEXCOORDP3UIPROC(int type, uint coords);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLTEXCOORDP3UIVPROC(int type, /*const*/ uint* coords);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLTEXCOORDP4UIPROC(int type, uint coords);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLTEXCOORDP4UIVPROC(int type, /*const*/ uint* coords);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLMULTITEXCOORDP1UIPROC(int texture, int type, uint coords);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLMULTITEXCOORDP1UIVPROC(int texture, int type, /*const*/ uint* coords);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLMULTITEXCOORDP2UIPROC(int texture, int type, uint coords);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLMULTITEXCOORDP2UIVPROC(int texture, int type, /*const*/ uint* coords);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLMULTITEXCOORDP3UIPROC(int texture, int type, uint coords);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLMULTITEXCOORDP3UIVPROC(int texture, int type, /*const*/ uint* coords);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLMULTITEXCOORDP4UIPROC(int texture, int type, uint coords);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLMULTITEXCOORDP4UIVPROC(int texture, int type, /*const*/ uint* coords);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLNORMALP3UIPROC(int type, uint coords);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLNORMALP3UIVPROC(int type, /*const*/ uint* coords);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLCOLORP3UIPROC(int type, uint color);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLCOLORP3UIVPROC(int type, /*const*/ uint* color);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLCOLORP4UIPROC(int type, uint color);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLCOLORP4UIVPROC(int type, /*const*/ uint* color);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLSECONDARYCOLORP3UIPROC(int type, uint color);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFNGLSECONDARYCOLORP3UIVPROC(int type, /*const*/ uint* color);

        private static PFNGLCULLFACEPROC _glCullFace;
        private static PFNGLFRONTFACEPROC _glFrontFace;
        private static PFNGLHINTPROC _glHint;
        private static PFNGLLINEWIDTHPROC _glLineWidth;
        private static PFNGLPOINTSIZEPROC _glPointSize;
        private static PFNGLPOLYGONMODEPROC _glPolygonMode;
        private static PFNGLSCISSORPROC _glScissor;
        private static PFNGLTEXPARAMETERFPROC _glTexParameterf;
        private static PFNGLTEXPARAMETERFVPROC _glTexParameterfv;
        private static PFNGLTEXPARAMETERIPROC _glTexParameteri;
        private static PFNGLTEXPARAMETERIVPROC _glTexParameteriv;
        private static PFNGLTEXIMAGE1DPROC _glTexImage1D;
        private static PFNGLTEXIMAGE2DPROC _glTexImage2D;
        private static PFNGLDRAWBUFFERPROC _glDrawBuffer;
        private static PFNGLCLEARPROC _glClear;
        private static PFNGLCLEARCOLORPROC _glClearColor;
        private static PFNGLCLEARSTENCILPROC _glClearStencil;
        private static PFNGLCLEARDEPTHPROC _glClearDepth;
        private static PFNGLSTENCILMASKPROC _glStencilMask;
        private static PFNGLCOLORMASKPROC _glColorMask;
        private static PFNGLDEPTHMASKPROC _glDepthMask;
        private static PFNGLDISABLEPROC _glDisable;
        private static PFNGLENABLEPROC _glEnable;
        private static PFNGLALPHAFUNCPROC _glAlphaFunc;
        private static PFNGLFINISHPROC _glFinish;
        private static PFNGLFLUSHPROC _glFlush;
        private static PFNGLBLENDFUNCPROC _glBlendFunc;
        private static PFNGLLOGICOPPROC _glLogicOp;
        private static PFNGLSTENCILFUNCPROC _glStencilFunc;
        private static PFNGLSTENCILOPPROC _glStencilOp;
        private static PFNGLDEPTHFUNCPROC _glDepthFunc;
        private static PFNGLPIXELSTOREFPROC _glPixelStoref;
        private static PFNGLPIXELSTOREIPROC _glPixelStorei;
        private static PFNGLREADBUFFERPROC _glReadBuffer;
        private static PFNGLREADPIXELSPROC _glReadPixels;
        private static PFNGLGETBOOLEANVPROC _glGetBooleanv;
        private static PFNGLGETDOUBLEVPROC _glGetDoublev;
        private static PFNGLGETERRORPROC _glGetError;
        private static PFNGLGETFLOATVPROC _glGetFloatv;
        private static PFNGLGETINTEGERVPROC _glGetIntegerv;
        private static PFNGLGETSTRINGPROC _glGetString;
        private static PFNGLGETTEXIMAGEPROC _glGetTexImage;
        private static PFNGLGETTEXPARAMETERFVPROC _glGetTexParameterfv;
        private static PFNGLGETTEXPARAMETERIVPROC _glGetTexParameteriv;
        private static PFNGLGETTEXLEVELPARAMETERFVPROC _glGetTexLevelParameterfv;
        private static PFNGLGETTEXLEVELPARAMETERIVPROC _glGetTexLevelParameteriv;
        private static PFNGLISENABLEDPROC _glIsEnabled;
        private static PFNGLDEPTHRANGEPROC _glDepthRange;
        private static PFNGLVIEWPORTPROC _glViewport;
        private static PFNGLDRAWARRAYSPROC _glDrawArrays;
        private static PFNGLDRAWELEMENTSPROC _glDrawElements;
        private static PFNGLPOLYGONOFFSETPROC _glPolygonOffset;
        private static PFNGLCOPYTEXIMAGE1DPROC _glCopyTexImage1D;
        private static PFNGLCOPYTEXIMAGE2DPROC _glCopyTexImage2D;
        private static PFNGLCOPYTEXSUBIMAGE1DPROC _glCopyTexSubImage1D;
        private static PFNGLCOPYTEXSUBIMAGE2DPROC _glCopyTexSubImage2D;
        private static PFNGLTEXSUBIMAGE1DPROC _glTexSubImage1D;
        private static PFNGLTEXSUBIMAGE2DPROC _glTexSubImage2D;
        private static PFNGLBINDTEXTUREPROC _glBindTexture;
        private static PFNGLDELETETEXTURESPROC _glDeleteTextures;
        private static PFNGLGENTEXTURESPROC _glGenTextures;
        private static PFNGLISTEXTUREPROC _glIsTexture;
        private static PFNGLDRAWRANGEELEMENTSPROC _glDrawRangeElements;
        private static PFNGLTEXIMAGE3DPROC _glTexImage3D;
        private static PFNGLTEXSUBIMAGE3DPROC _glTexSubImage3D;
        private static PFNGLCOPYTEXSUBIMAGE3DPROC _glCopyTexSubImage3D;
        private static PFNGLACTIVETEXTUREPROC _glActiveTexture;
        private static PFNGLSAMPLECOVERAGEPROC _glSampleCoverage;
        private static PFNGLCOMPRESSEDTEXIMAGE3DPROC _glCompressedTexImage3D;
        private static PFNGLCOMPRESSEDTEXIMAGE2DPROC _glCompressedTexImage2D;
        private static PFNGLCOMPRESSEDTEXIMAGE1DPROC _glCompressedTexImage1D;
        private static PFNGLCOMPRESSEDTEXSUBIMAGE3DPROC _glCompressedTexSubImage3D;
        private static PFNGLCOMPRESSEDTEXSUBIMAGE2DPROC _glCompressedTexSubImage2D;
        private static PFNGLCOMPRESSEDTEXSUBIMAGE1DPROC _glCompressedTexSubImage1D;
        private static PFNGLGETCOMPRESSEDTEXIMAGEPROC _glGetCompressedTexImage;
        private static PFNGLBLENDFUNCSEPARATEPROC _glBlendFuncSeparate;
        private static PFNGLMULTIDRAWARRAYSPROC _glMultiDrawArrays;
        private static PFNGLMULTIDRAWELEMENTSPROC _glMultiDrawElements;
        private static PFNGLPOINTPARAMETERFPROC _glPointParameterf;
        private static PFNGLPOINTPARAMETERFVPROC _glPointParameterfv;
        private static PFNGLPOINTPARAMETERIPROC _glPointParameteri;
        private static PFNGLPOINTPARAMETERIVPROC _glPointParameteriv;
        private static PFNGLBLENDCOLORPROC _glBlendColor;
        private static PFNGLBLENDEQUATIONPROC _glBlendEquation;
        private static PFNGLGENQUERIESPROC _glGenQueries;
        private static PFNGLDELETEQUERIESPROC _glDeleteQueries;
        private static PFNGLISQUERYPROC _glIsQuery;
        private static PFNGLBEGINQUERYPROC _glBeginQuery;
        private static PFNGLENDQUERYPROC _glEndQuery;
        private static PFNGLGETQUERYIVPROC _glGetQueryiv;
        private static PFNGLGETQUERYOBJECTIVPROC _glGetQueryObjectiv;
        private static PFNGLGETQUERYOBJECTUIVPROC _glGetQueryObjectuiv;
        private static PFNGLBINDBUFFERPROC _glBindBuffer;
        private static PFNGLDELETEBUFFERSPROC _glDeleteBuffers;
        private static PFNGLGENBUFFERSPROC _glGenBuffers;
        private static PFNGLISBUFFERPROC _glIsBuffer;
        private static PFNGLBUFFERDATAPROC _glBufferData;
        private static PFNGLBUFFERSUBDATAPROC _glBufferSubData;
        private static PFNGLGETBUFFERSUBDATAPROC _glGetBufferSubData;
        private static PFNGLMAPBUFFERPROC _glMapBuffer;
        private static PFNGLUNMAPBUFFERPROC _glUnmapBuffer;
        private static PFNGLGETBUFFERPARAMETERIVPROC _glGetBufferParameteriv;
        private static PFNGLGETBUFFERPOINTERVPROC _glGetBufferPointerv;
        private static PFNGLBLENDEQUATIONSEPARATEPROC _glBlendEquationSeparate;
        private static PFNGLDRAWBUFFERSPROC _glDrawBuffers;
        private static PFNGLSTENCILOPSEPARATEPROC _glStencilOpSeparate;
        private static PFNGLSTENCILFUNCSEPARATEPROC _glStencilFuncSeparate;
        private static PFNGLSTENCILMASKSEPARATEPROC _glStencilMaskSeparate;
        private static PFNGLATTACHSHADERPROC _glAttachShader;
        private static PFNGLBINDATTRIBLOCATIONPROC _glBindAttribLocation;
        private static PFNGLCOMPILESHADERPROC _glCompileShader;
        private static PFNGLCREATEPROGRAMPROC _glCreateProgram;
        private static PFNGLCREATESHADERPROC _glCreateShader;
        private static PFNGLDELETEPROGRAMPROC _glDeleteProgram;
        private static PFNGLDELETESHADERPROC _glDeleteShader;
        private static PFNGLDETACHSHADERPROC _glDetachShader;
        private static PFNGLDISABLEVERTEXATTRIBARRAYPROC _glDisableVertexAttribArray;
        private static PFNGLENABLEVERTEXATTRIBARRAYPROC _glEnableVertexAttribArray;
        private static PFNGLGETACTIVEATTRIBPROC _glGetActiveAttrib;
        private static PFNGLGETACTIVEUNIFORMPROC _glGetActiveUniform;
        private static PFNGLGETATTACHEDSHADERSPROC _glGetAttachedShaders;
        private static PFNGLGETATTRIBLOCATIONPROC _glGetAttribLocation;
        private static PFNGLGETPROGRAMIVPROC _glGetProgramiv;
        private static PFNGLGETPROGRAMINFOLOGPROC _glGetProgramInfoLog;
        private static PFNGLGETSHADERIVPROC _glGetShaderiv;
        private static PFNGLGETSHADERINFOLOGPROC _glGetShaderInfoLog;
        private static PFNGLGETSHADERSOURCEPROC _glGetShaderSource;
        private static PFNGLGETUNIFORMLOCATIONPROC _glGetUniformLocation;
        private static PFNGLGETUNIFORMFVPROC _glGetUniformfv;
        private static PFNGLGETUNIFORMIVPROC _glGetUniformiv;
        private static PFNGLGETVERTEXATTRIBDVPROC _glGetVertexAttribdv;
        private static PFNGLGETVERTEXATTRIBFVPROC _glGetVertexAttribfv;
        private static PFNGLGETVERTEXATTRIBIVPROC _glGetVertexAttribiv;
        private static PFNGLGETVERTEXATTRIBPOINTERVPROC _glGetVertexAttribPointerv;
        private static PFNGLISPROGRAMPROC _glIsProgram;
        private static PFNGLISSHADERPROC _glIsShader;
        private static PFNGLLINKPROGRAMPROC _glLinkProgram;
        private static PFNGLSHADERSOURCEPROC _glShaderSource;
        private static PFNGLUSEPROGRAMPROC _glUseProgram;
        private static PFNGLUNIFORM1FPROC _glUniform1f;
        private static PFNGLUNIFORM2FPROC _glUniform2f;
        private static PFNGLUNIFORM3FPROC _glUniform3f;
        private static PFNGLUNIFORM4FPROC _glUniform4f;
        private static PFNGLUNIFORM1IPROC _glUniform1i;
        private static PFNGLUNIFORM2IPROC _glUniform2i;
        private static PFNGLUNIFORM3IPROC _glUniform3i;
        private static PFNGLUNIFORM4IPROC _glUniform4i;
        private static PFNGLUNIFORM1FVPROC _glUniform1fv;
        private static PFNGLUNIFORM2FVPROC _glUniform2fv;
        private static PFNGLUNIFORM3FVPROC _glUniform3fv;
        private static PFNGLUNIFORM4FVPROC _glUniform4fv;
        private static PFNGLUNIFORM1IVPROC _glUniform1iv;
        private static PFNGLUNIFORM2IVPROC _glUniform2iv;
        private static PFNGLUNIFORM3IVPROC _glUniform3iv;
        private static PFNGLUNIFORM4IVPROC _glUniform4iv;
        private static PFNGLUNIFORMMATRIX2FVPROC _glUniformMatrix2fv;
        private static PFNGLUNIFORMMATRIX3FVPROC _glUniformMatrix3fv;
        private static PFNGLUNIFORMMATRIX4FVPROC _glUniformMatrix4fv;
        private static PFNGLVALIDATEPROGRAMPROC _glValidateProgram;
        private static PFNGLVERTEXATTRIB1DPROC _glVertexAttrib1d;
        private static PFNGLVERTEXATTRIB1DVPROC _glVertexAttrib1dv;
        private static PFNGLVERTEXATTRIB1FPROC _glVertexAttrib1f;
        private static PFNGLVERTEXATTRIB1FVPROC _glVertexAttrib1fv;
        private static PFNGLVERTEXATTRIB1SPROC _glVertexAttrib1s;
        private static PFNGLVERTEXATTRIB1SVPROC _glVertexAttrib1sv;
        private static PFNGLVERTEXATTRIB2DPROC _glVertexAttrib2d;
        private static PFNGLVERTEXATTRIB2DVPROC _glVertexAttrib2dv;
        private static PFNGLVERTEXATTRIB2FPROC _glVertexAttrib2f;
        private static PFNGLVERTEXATTRIB2FVPROC _glVertexAttrib2fv;
        private static PFNGLVERTEXATTRIB2SPROC _glVertexAttrib2s;
        private static PFNGLVERTEXATTRIB2SVPROC _glVertexAttrib2sv;
        private static PFNGLVERTEXATTRIB3DPROC _glVertexAttrib3d;
        private static PFNGLVERTEXATTRIB3DVPROC _glVertexAttrib3dv;
        private static PFNGLVERTEXATTRIB3FPROC _glVertexAttrib3f;
        private static PFNGLVERTEXATTRIB3FVPROC _glVertexAttrib3fv;
        private static PFNGLVERTEXATTRIB3SPROC _glVertexAttrib3s;
        private static PFNGLVERTEXATTRIB3SVPROC _glVertexAttrib3sv;
        private static PFNGLVERTEXATTRIB4NBVPROC _glVertexAttrib4Nbv;
        private static PFNGLVERTEXATTRIB4NIVPROC _glVertexAttrib4Niv;
        private static PFNGLVERTEXATTRIB4NSVPROC _glVertexAttrib4Nsv;
        private static PFNGLVERTEXATTRIB4NUBPROC _glVertexAttrib4Nub;
        private static PFNGLVERTEXATTRIB4NUBVPROC _glVertexAttrib4Nubv;
        private static PFNGLVERTEXATTRIB4NUIVPROC _glVertexAttrib4Nuiv;
        private static PFNGLVERTEXATTRIB4NUSVPROC _glVertexAttrib4Nusv;
        private static PFNGLVERTEXATTRIB4BVPROC _glVertexAttrib4bv;
        private static PFNGLVERTEXATTRIB4DPROC _glVertexAttrib4d;
        private static PFNGLVERTEXATTRIB4DVPROC _glVertexAttrib4dv;
        private static PFNGLVERTEXATTRIB4FPROC _glVertexAttrib4f;
        private static PFNGLVERTEXATTRIB4FVPROC _glVertexAttrib4fv;
        private static PFNGLVERTEXATTRIB4IVPROC _glVertexAttrib4iv;
        private static PFNGLVERTEXATTRIB4SPROC _glVertexAttrib4s;
        private static PFNGLVERTEXATTRIB4SVPROC _glVertexAttrib4sv;
        private static PFNGLVERTEXATTRIB4UBVPROC _glVertexAttrib4ubv;
        private static PFNGLVERTEXATTRIB4UIVPROC _glVertexAttrib4uiv;
        private static PFNGLVERTEXATTRIB4USVPROC _glVertexAttrib4usv;
        private static PFNGLVERTEXATTRIBPOINTERPROC _glVertexAttribPointer;
        private static PFNGLUNIFORMMATRIX2X3FVPROC _glUniformMatrix2x3fv;
        private static PFNGLUNIFORMMATRIX3X2FVPROC _glUniformMatrix3x2fv;
        private static PFNGLUNIFORMMATRIX2X4FVPROC _glUniformMatrix2x4fv;
        private static PFNGLUNIFORMMATRIX4X2FVPROC _glUniformMatrix4x2fv;
        private static PFNGLUNIFORMMATRIX3X4FVPROC _glUniformMatrix3x4fv;
        private static PFNGLUNIFORMMATRIX4X3FVPROC _glUniformMatrix4x3fv;
        private static PFNGLCOLORMASKIPROC _glColorMaski;
        private static PFNGLGETBOOLEANI_VPROC _glGetBooleani_v;
        private static PFNGLENABLEIPROC _glEnablei;
        private static PFNGLDISABLEIPROC _glDisablei;
        private static PFNGLISENABLEDIPROC _glIsEnabledi;
        private static PFNGLBEGINTRANSFORMFEEDBACKPROC _glBeginTransformFeedback;
        private static PFNGLENDTRANSFORMFEEDBACKPROC _glEndTransformFeedback;
        private static PFNGLTRANSFORMFEEDBACKVARYINGSPROC _glTransformFeedbackVaryings;
        private static PFNGLGETTRANSFORMFEEDBACKVARYINGPROC _glGetTransformFeedbackVarying;
        private static PFNGLCLAMPCOLORPROC _glClampColor;
        private static PFNGLBEGINCONDITIONALRENDERPROC _glBeginConditionalRender;
        private static PFNGLENDCONDITIONALRENDERPROC _glEndConditionalRender;
        private static PFNGLVERTEXATTRIBIPOINTERPROC _glVertexAttribIPointer;
        private static PFNGLGETVERTEXATTRIBIIVPROC _glGetVertexAttribIiv;
        private static PFNGLGETVERTEXATTRIBIUIVPROC _glGetVertexAttribIuiv;
        private static PFNGLVERTEXATTRIBI1IPROC _glVertexAttribI1i;
        private static PFNGLVERTEXATTRIBI2IPROC _glVertexAttribI2i;
        private static PFNGLVERTEXATTRIBI3IPROC _glVertexAttribI3i;
        private static PFNGLVERTEXATTRIBI4IPROC _glVertexAttribI4i;
        private static PFNGLVERTEXATTRIBI1UIPROC _glVertexAttribI1ui;
        private static PFNGLVERTEXATTRIBI2UIPROC _glVertexAttribI2ui;
        private static PFNGLVERTEXATTRIBI3UIPROC _glVertexAttribI3ui;
        private static PFNGLVERTEXATTRIBI4UIPROC _glVertexAttribI4ui;
        private static PFNGLVERTEXATTRIBI1IVPROC _glVertexAttribI1iv;
        private static PFNGLVERTEXATTRIBI2IVPROC _glVertexAttribI2iv;
        private static PFNGLVERTEXATTRIBI3IVPROC _glVertexAttribI3iv;
        private static PFNGLVERTEXATTRIBI4IVPROC _glVertexAttribI4iv;
        private static PFNGLVERTEXATTRIBI1UIVPROC _glVertexAttribI1uiv;
        private static PFNGLVERTEXATTRIBI2UIVPROC _glVertexAttribI2uiv;
        private static PFNGLVERTEXATTRIBI3UIVPROC _glVertexAttribI3uiv;
        private static PFNGLVERTEXATTRIBI4UIVPROC _glVertexAttribI4uiv;
        private static PFNGLVERTEXATTRIBI4BVPROC _glVertexAttribI4bv;
        private static PFNGLVERTEXATTRIBI4SVPROC _glVertexAttribI4sv;
        private static PFNGLVERTEXATTRIBI4UBVPROC _glVertexAttribI4ubv;
        private static PFNGLVERTEXATTRIBI4USVPROC _glVertexAttribI4usv;
        private static PFNGLGETUNIFORMUIVPROC _glGetUniformuiv;
        private static PFNGLBINDFRAGDATALOCATIONPROC _glBindFragDataLocation;
        private static PFNGLGETFRAGDATALOCATIONPROC _glGetFragDataLocation;
        private static PFNGLUNIFORM1UIPROC _glUniform1ui;
        private static PFNGLUNIFORM2UIPROC _glUniform2ui;
        private static PFNGLUNIFORM3UIPROC _glUniform3ui;
        private static PFNGLUNIFORM4UIPROC _glUniform4ui;
        private static PFNGLUNIFORM1UIVPROC _glUniform1uiv;
        private static PFNGLUNIFORM2UIVPROC _glUniform2uiv;
        private static PFNGLUNIFORM3UIVPROC _glUniform3uiv;
        private static PFNGLUNIFORM4UIVPROC _glUniform4uiv;
        private static PFNGLTEXPARAMETERIIVPROC _glTexParameterIiv;
        private static PFNGLTEXPARAMETERIUIVPROC _glTexParameterIuiv;
        private static PFNGLGETTEXPARAMETERIIVPROC _glGetTexParameterIiv;
        private static PFNGLGETTEXPARAMETERIUIVPROC _glGetTexParameterIuiv;
        private static PFNGLCLEARBUFFERIVPROC _glClearBufferiv;
        private static PFNGLCLEARBUFFERUIVPROC _glClearBufferuiv;
        private static PFNGLCLEARBUFFERFVPROC _glClearBufferfv;
        private static PFNGLCLEARBUFFERFIPROC _glClearBufferfi;
        private static PFNGLGETSTRINGIPROC _glGetStringi;
        private static PFNGLISRENDERBUFFERPROC _glIsRenderbuffer;
        private static PFNGLBINDRENDERBUFFERPROC _glBindRenderbuffer;
        private static PFNGLDELETERENDERBUFFERSPROC _glDeleteRenderbuffers;
        private static PFNGLGENRENDERBUFFERSPROC _glGenRenderbuffers;
        private static PFNGLRENDERBUFFERSTORAGEPROC _glRenderbufferStorage;
        private static PFNGLGETRENDERBUFFERPARAMETERIVPROC _glGetRenderbufferParameteriv;
        private static PFNGLISFRAMEBUFFERPROC _glIsFramebuffer;
        private static PFNGLBINDFRAMEBUFFERPROC _glBindFramebuffer;
        private static PFNGLDELETEFRAMEBUFFERSPROC _glDeleteFramebuffers;
        private static PFNGLGENFRAMEBUFFERSPROC _glGenFramebuffers;
        private static PFNGLCHECKFRAMEBUFFERSTATUSPROC _glCheckFramebufferStatus;
        private static PFNGLFRAMEBUFFERTEXTURE1DPROC _glFramebufferTexture1D;
        private static PFNGLFRAMEBUFFERTEXTURE2DPROC _glFramebufferTexture2D;
        private static PFNGLFRAMEBUFFERTEXTURE3DPROC _glFramebufferTexture3D;
        private static PFNGLFRAMEBUFFERRENDERBUFFERPROC _glFramebufferRenderbuffer;
        private static PFNGLGETFRAMEBUFFERATTACHMENTPARAMETERIVPROC _glGetFramebufferAttachmentParameteriv;
        private static PFNGLGENERATEMIPMAPPROC _glGenerateMipmap;
        private static PFNGLBLITFRAMEBUFFERPROC _glBlitFramebuffer;
        private static PFNGLRENDERBUFFERSTORAGEMULTISAMPLEPROC _glRenderbufferStorageMultisample;
        private static PFNGLFRAMEBUFFERTEXTURELAYERPROC _glFramebufferTextureLayer;
        private static PFNGLMAPBUFFERRANGEPROC _glMapBufferRange;
        private static PFNGLFLUSHMAPPEDBUFFERRANGEPROC _glFlushMappedBufferRange;
        private static PFNGLBINDVERTEXARRAYPROC _glBindVertexArray;
        private static PFNGLDELETEVERTEXARRAYSPROC _glDeleteVertexArrays;
        private static PFNGLGENVERTEXARRAYSPROC _glGenVertexArrays;
        private static PFNGLISVERTEXARRAYPROC _glIsVertexArray;
        private static PFNGLDRAWARRAYSINSTANCEDPROC _glDrawArraysInstanced;
        private static PFNGLDRAWELEMENTSINSTANCEDPROC _glDrawElementsInstanced;
        private static PFNGLTEXBUFFERPROC _glTexBuffer;
        private static PFNGLPRIMITIVERESTARTINDEXPROC _glPrimitiveRestartIndex;
        private static PFNGLCOPYBUFFERSUBDATAPROC _glCopyBufferSubData;
        private static PFNGLGETUNIFORMINDICESPROC _glGetUniformIndices;
        private static PFNGLGETACTIVEUNIFORMSIVPROC _glGetActiveUniformsiv;
        private static PFNGLGETACTIVEUNIFORMNAMEPROC _glGetActiveUniformName;
        private static PFNGLGETUNIFORMBLOCKINDEXPROC _glGetUniformBlockIndex;
        private static PFNGLGETACTIVEUNIFORMBLOCKIVPROC _glGetActiveUniformBlockiv;
        private static PFNGLGETACTIVEUNIFORMBLOCKNAMEPROC _glGetActiveUniformBlockName;
        private static PFNGLUNIFORMBLOCKBINDINGPROC _glUniformBlockBinding;
        private static PFNGLBINDBUFFERRANGEPROC _glBindBufferRange;
        private static PFNGLBINDBUFFERBASEPROC _glBindBufferBase;
        private static PFNGLGETINTEGERI_VPROC _glGetIntegeri_v;
        private static PFNGLDRAWELEMENTSBASEVERTEXPROC _glDrawElementsBaseVertex;
        private static PFNGLDRAWRANGEELEMENTSBASEVERTEXPROC _glDrawRangeElementsBaseVertex;
        private static PFNGLDRAWELEMENTSINSTANCEDBASEVERTEXPROC _glDrawElementsInstancedBaseVertex;
        private static PFNGLMULTIDRAWELEMENTSBASEVERTEXPROC _glMultiDrawElementsBaseVertex;
        private static PFNGLPROVOKINGVERTEXPROC _glProvokingVertex;
        private static PFNGLFENCESYNCPROC _glFenceSync;
        private static PFNGLISSYNCPROC _glIsSync;
        private static PFNGLDELETESYNCPROC _glDeleteSync;
        private static PFNGLCLIENTWAITSYNCPROC _glClientWaitSync;
        private static PFNGLWAITSYNCPROC _glWaitSync;
        private static PFNGLGETINTEGER64VPROC _glGetInteger64v;
        private static PFNGLGETSYNCIVPROC _glGetSynciv;
        private static PFNGLGETINTEGER64I_VPROC _glGetInteger64i_v;
        private static PFNGLGETBUFFERPARAMETERI64VPROC _glGetBufferParameteri64v;
        private static PFNGLFRAMEBUFFERTEXTUREPROC _glFramebufferTexture;
        private static PFNGLTEXIMAGE2DMULTISAMPLEPROC _glTexImage2DMultisample;
        private static PFNGLTEXIMAGE3DMULTISAMPLEPROC _glTexImage3DMultisample;
        private static PFNGLGETMULTISAMPLEFVPROC _glGetMultisamplefv;
        private static PFNGLSAMPLEMASKIPROC _glSampleMaski;
        private static PFNGLBINDFRAGDATALOCATIONINDEXEDPROC _glBindFragDataLocationIndexed;
        private static PFNGLGETFRAGDATAINDEXPROC _glGetFragDataIndex;
        private static PFNGLGENSAMPLERSPROC _glGenSamplers;
        private static PFNGLDELETESAMPLERSPROC _glDeleteSamplers;
        private static PFNGLISSAMPLERPROC _glIsSampler;
        private static PFNGLBINDSAMPLERPROC _glBindSampler;
        private static PFNGLSAMPLERPARAMETERIPROC _glSamplerParameteri;
        private static PFNGLSAMPLERPARAMETERIVPROC _glSamplerParameteriv;
        private static PFNGLSAMPLERPARAMETERFPROC _glSamplerParameterf;
        private static PFNGLSAMPLERPARAMETERFVPROC _glSamplerParameterfv;
        private static PFNGLSAMPLERPARAMETERIIVPROC _glSamplerParameterIiv;
        private static PFNGLSAMPLERPARAMETERIUIVPROC _glSamplerParameterIuiv;
        private static PFNGLGETSAMPLERPARAMETERIVPROC _glGetSamplerParameteriv;
        private static PFNGLGETSAMPLERPARAMETERIIVPROC _glGetSamplerParameterIiv;
        private static PFNGLGETSAMPLERPARAMETERFVPROC _glGetSamplerParameterfv;
        private static PFNGLGETSAMPLERPARAMETERIUIVPROC _glGetSamplerParameterIuiv;
        private static PFNGLGETQUERYOBJECTI64VPROC _glGetQueryObjecti64v;
        private static PFNGLGETQUERYOBJECTUI64VPROC _glGetQueryObjectui64v;
        private static PFNGLVERTEXATTRIBDIVISORPROC _glVertexAttribDivisor;
        private static PFNGLVERTEXATTRIBP1UIPROC _glVertexAttribP1ui;
        private static PFNGLVERTEXATTRIBP1UIVPROC _glVertexAttribP1uiv;
        private static PFNGLVERTEXATTRIBP2UIPROC _glVertexAttribP2ui;
        private static PFNGLVERTEXATTRIBP2UIVPROC _glVertexAttribP2uiv;
        private static PFNGLVERTEXATTRIBP3UIPROC _glVertexAttribP3ui;
        private static PFNGLVERTEXATTRIBP3UIVPROC _glVertexAttribP3uiv;
        private static PFNGLVERTEXATTRIBP4UIPROC _glVertexAttribP4ui;
        private static PFNGLVERTEXATTRIBP4UIVPROC _glVertexAttribP4uiv;
        private static PFNGLVERTEXP2UIPROC _glVertexP2ui;
        private static PFNGLVERTEXP2UIVPROC _glVertexP2uiv;
        private static PFNGLVERTEXP3UIPROC _glVertexP3ui;
        private static PFNGLVERTEXP3UIVPROC _glVertexP3uiv;
        private static PFNGLVERTEXP4UIPROC _glVertexP4ui;
        private static PFNGLVERTEXP4UIVPROC _glVertexP4uiv;
        private static PFNGLTEXCOORDP1UIPROC _glTexCoordP1ui;
        private static PFNGLTEXCOORDP1UIVPROC _glTexCoordP1uiv;
        private static PFNGLTEXCOORDP2UIPROC _glTexCoordP2ui;
        private static PFNGLTEXCOORDP2UIVPROC _glTexCoordP2uiv;
        private static PFNGLTEXCOORDP3UIPROC _glTexCoordP3ui;
        private static PFNGLTEXCOORDP3UIVPROC _glTexCoordP3uiv;
        private static PFNGLTEXCOORDP4UIPROC _glTexCoordP4ui;
        private static PFNGLTEXCOORDP4UIVPROC _glTexCoordP4uiv;
        private static PFNGLMULTITEXCOORDP1UIPROC _glMultiTexCoordP1ui;
        private static PFNGLMULTITEXCOORDP1UIVPROC _glMultiTexCoordP1uiv;
        private static PFNGLMULTITEXCOORDP2UIPROC _glMultiTexCoordP2ui;
        private static PFNGLMULTITEXCOORDP2UIVPROC _glMultiTexCoordP2uiv;
        private static PFNGLMULTITEXCOORDP3UIPROC _glMultiTexCoordP3ui;
        private static PFNGLMULTITEXCOORDP3UIVPROC _glMultiTexCoordP3uiv;
        private static PFNGLMULTITEXCOORDP4UIPROC _glMultiTexCoordP4ui;
        private static PFNGLMULTITEXCOORDP4UIVPROC _glMultiTexCoordP4uiv;
        private static PFNGLNORMALP3UIPROC _glNormalP3ui;
        private static PFNGLNORMALP3UIVPROC _glNormalP3uiv;
        private static PFNGLCOLORP3UIPROC _glColorP3ui;
        private static PFNGLCOLORP3UIVPROC _glColorP3uiv;
        private static PFNGLCOLORP4UIPROC _glColorP4ui;
        private static PFNGLCOLORP4UIVPROC _glColorP4uiv;
        private static PFNGLSECONDARYCOLORP3UIPROC _glSecondaryColorP3ui;
        private static PFNGLSECONDARYCOLORP3UIVPROC _glSecondaryColorP3uiv;

        private static T GetDelegateForFunctionPointer<T>(IntPtr ptr) {
            if(ptr == IntPtr.Zero){
                return default;
            }
            return Marshal.GetDelegateForFunctionPointer<T>(ptr);
        }

        /// <summary>
        ///     Imports all OpenGL functions using the specified loader.
        /// </summary>
        /// <param name="loader">A loader to retrieve a fuction pointer.</param>
		public static void Import(GetProcAddressHandler loader) {
            _glCullFace = GetDelegateForFunctionPointer<PFNGLCULLFACEPROC>(loader.Invoke("glCullFace"));
            _glFrontFace = GetDelegateForFunctionPointer<PFNGLFRONTFACEPROC>(loader.Invoke("glFrontFace"));
            _glHint = GetDelegateForFunctionPointer<PFNGLHINTPROC>(loader.Invoke("glHint"));
            _glLineWidth = GetDelegateForFunctionPointer<PFNGLLINEWIDTHPROC>(loader.Invoke("glLineWidth"));
            _glPointSize = GetDelegateForFunctionPointer<PFNGLPOINTSIZEPROC>(loader.Invoke("glPointSize"));
            _glPolygonMode = GetDelegateForFunctionPointer<PFNGLPOLYGONMODEPROC>(loader.Invoke("glPolygonMode"));
            _glScissor = GetDelegateForFunctionPointer<PFNGLSCISSORPROC>(loader.Invoke("glScissor"));
            _glTexParameterf = GetDelegateForFunctionPointer<PFNGLTEXPARAMETERFPROC>(loader.Invoke("glTexParameterf"));
            _glTexParameterfv = GetDelegateForFunctionPointer<PFNGLTEXPARAMETERFVPROC>(loader.Invoke("glTexParameterfv"));
            _glTexParameteri = GetDelegateForFunctionPointer<PFNGLTEXPARAMETERIPROC>(loader.Invoke("glTexParameteri"));
            _glTexParameteriv = GetDelegateForFunctionPointer<PFNGLTEXPARAMETERIVPROC>(loader.Invoke("glTexParameteriv"));
            _glTexImage1D = GetDelegateForFunctionPointer<PFNGLTEXIMAGE1DPROC>(loader.Invoke("glTexImage1D"));
            _glTexImage2D = GetDelegateForFunctionPointer<PFNGLTEXIMAGE2DPROC>(loader.Invoke("glTexImage2D"));
            _glDrawBuffer = GetDelegateForFunctionPointer<PFNGLDRAWBUFFERPROC>(loader.Invoke("glDrawBuffer"));
            _glClear = GetDelegateForFunctionPointer<PFNGLCLEARPROC>(loader.Invoke("glClear"));
            _glClearColor = GetDelegateForFunctionPointer<PFNGLCLEARCOLORPROC>(loader.Invoke("glClearColor"));
            _glClearStencil = GetDelegateForFunctionPointer<PFNGLCLEARSTENCILPROC>(loader.Invoke("glClearStencil"));
            _glClearDepth = GetDelegateForFunctionPointer<PFNGLCLEARDEPTHPROC>(loader.Invoke("glClearDepth"));
            _glStencilMask = GetDelegateForFunctionPointer<PFNGLSTENCILMASKPROC>(loader.Invoke("glStencilMask"));
            _glColorMask = GetDelegateForFunctionPointer<PFNGLCOLORMASKPROC>(loader.Invoke("glColorMask"));
            _glDepthMask = GetDelegateForFunctionPointer<PFNGLDEPTHMASKPROC>(loader.Invoke("glDepthMask"));
            _glDisable = GetDelegateForFunctionPointer<PFNGLDISABLEPROC>(loader.Invoke("glDisable"));
            _glEnable = GetDelegateForFunctionPointer<PFNGLENABLEPROC>(loader.Invoke("glEnable"));
            _glAlphaFunc = GetDelegateForFunctionPointer<PFNGLALPHAFUNCPROC>(loader.Invoke("glAlphaFunc"));
            _glFinish = GetDelegateForFunctionPointer<PFNGLFINISHPROC>(loader.Invoke("glFinish"));
            _glFlush = GetDelegateForFunctionPointer<PFNGLFLUSHPROC>(loader.Invoke("glFlush"));
            _glBlendFunc = GetDelegateForFunctionPointer<PFNGLBLENDFUNCPROC>(loader.Invoke("glBlendFunc"));
            _glLogicOp = GetDelegateForFunctionPointer<PFNGLLOGICOPPROC>(loader.Invoke("glLogicOp"));
            _glStencilFunc = GetDelegateForFunctionPointer<PFNGLSTENCILFUNCPROC>(loader.Invoke("glStencilFunc"));
            _glStencilOp = GetDelegateForFunctionPointer<PFNGLSTENCILOPPROC>(loader.Invoke("glStencilOp"));
            _glDepthFunc = GetDelegateForFunctionPointer<PFNGLDEPTHFUNCPROC>(loader.Invoke("glDepthFunc"));
            _glPixelStoref = GetDelegateForFunctionPointer<PFNGLPIXELSTOREFPROC>(loader.Invoke("glPixelStoref"));
            _glPixelStorei = GetDelegateForFunctionPointer<PFNGLPIXELSTOREIPROC>(loader.Invoke("glPixelStorei"));
            _glReadBuffer = GetDelegateForFunctionPointer<PFNGLREADBUFFERPROC>(loader.Invoke("glReadBuffer"));
            _glReadPixels = GetDelegateForFunctionPointer<PFNGLREADPIXELSPROC>(loader.Invoke("glReadPixels"));
            _glGetBooleanv = GetDelegateForFunctionPointer<PFNGLGETBOOLEANVPROC>(loader.Invoke("glGetBooleanv"));
            _glGetDoublev = GetDelegateForFunctionPointer<PFNGLGETDOUBLEVPROC>(loader.Invoke("glGetDoublev"));
            _glGetError = GetDelegateForFunctionPointer<PFNGLGETERRORPROC>(loader.Invoke("glGetError"));
            _glGetFloatv = GetDelegateForFunctionPointer<PFNGLGETFLOATVPROC>(loader.Invoke("glGetFloatv"));
            _glGetIntegerv = GetDelegateForFunctionPointer<PFNGLGETINTEGERVPROC>(loader.Invoke("glGetIntegerv"));
            _glGetString = GetDelegateForFunctionPointer<PFNGLGETSTRINGPROC>(loader.Invoke("glGetString"));
            _glGetTexImage = GetDelegateForFunctionPointer<PFNGLGETTEXIMAGEPROC>(loader.Invoke("glGetTexImage"));
            _glGetTexParameterfv = GetDelegateForFunctionPointer<PFNGLGETTEXPARAMETERFVPROC>(loader.Invoke("glGetTexParameterfv"));
            _glGetTexParameteriv = GetDelegateForFunctionPointer<PFNGLGETTEXPARAMETERIVPROC>(loader.Invoke("glGetTexParameteriv"));
            _glGetTexLevelParameterfv = GetDelegateForFunctionPointer<PFNGLGETTEXLEVELPARAMETERFVPROC>(loader.Invoke("glGetTexLevelParameterfv"));
            _glGetTexLevelParameteriv = GetDelegateForFunctionPointer<PFNGLGETTEXLEVELPARAMETERIVPROC>(loader.Invoke("glGetTexLevelParameteriv"));
            _glIsEnabled = GetDelegateForFunctionPointer<PFNGLISENABLEDPROC>(loader.Invoke("glIsEnabled"));
            _glDepthRange = GetDelegateForFunctionPointer<PFNGLDEPTHRANGEPROC>(loader.Invoke("glDepthRange"));
            _glViewport = GetDelegateForFunctionPointer<PFNGLVIEWPORTPROC>(loader.Invoke("glViewport"));
            _glDrawArrays = GetDelegateForFunctionPointer<PFNGLDRAWARRAYSPROC>(loader.Invoke("glDrawArrays"));
            _glDrawElements = GetDelegateForFunctionPointer<PFNGLDRAWELEMENTSPROC>(loader.Invoke("glDrawElements"));
            _glPolygonOffset = GetDelegateForFunctionPointer<PFNGLPOLYGONOFFSETPROC>(loader.Invoke("glPolygonOffset"));
            _glCopyTexImage1D = GetDelegateForFunctionPointer<PFNGLCOPYTEXIMAGE1DPROC>(loader.Invoke("glCopyTexImage1D"));
            _glCopyTexImage2D = GetDelegateForFunctionPointer<PFNGLCOPYTEXIMAGE2DPROC>(loader.Invoke("glCopyTexImage2D"));
            _glCopyTexSubImage1D = GetDelegateForFunctionPointer<PFNGLCOPYTEXSUBIMAGE1DPROC>(loader.Invoke("glCopyTexSubImage1D"));
            _glCopyTexSubImage2D = GetDelegateForFunctionPointer<PFNGLCOPYTEXSUBIMAGE2DPROC>(loader.Invoke("glCopyTexSubImage2D"));
            _glTexSubImage1D = GetDelegateForFunctionPointer<PFNGLTEXSUBIMAGE1DPROC>(loader.Invoke("glTexSubImage1D"));
            _glTexSubImage2D = GetDelegateForFunctionPointer<PFNGLTEXSUBIMAGE2DPROC>(loader.Invoke("glTexSubImage2D"));
            _glBindTexture = GetDelegateForFunctionPointer<PFNGLBINDTEXTUREPROC>(loader.Invoke("glBindTexture"));
            _glDeleteTextures = GetDelegateForFunctionPointer<PFNGLDELETETEXTURESPROC>(loader.Invoke("glDeleteTextures"));
            _glGenTextures = GetDelegateForFunctionPointer<PFNGLGENTEXTURESPROC>(loader.Invoke("glGenTextures"));
            _glIsTexture = GetDelegateForFunctionPointer<PFNGLISTEXTUREPROC>(loader.Invoke("glIsTexture"));
            _glDrawRangeElements = GetDelegateForFunctionPointer<PFNGLDRAWRANGEELEMENTSPROC>(loader.Invoke("glDrawRangeElements"));
            _glTexImage3D = GetDelegateForFunctionPointer<PFNGLTEXIMAGE3DPROC>(loader.Invoke("glTexImage3D"));
            _glTexSubImage3D = GetDelegateForFunctionPointer<PFNGLTEXSUBIMAGE3DPROC>(loader.Invoke("glTexSubImage3D"));
            _glCopyTexSubImage3D = GetDelegateForFunctionPointer<PFNGLCOPYTEXSUBIMAGE3DPROC>(loader.Invoke("glCopyTexSubImage3D"));
            _glActiveTexture = GetDelegateForFunctionPointer<PFNGLACTIVETEXTUREPROC>(loader.Invoke("glActiveTexture"));
            _glSampleCoverage = GetDelegateForFunctionPointer<PFNGLSAMPLECOVERAGEPROC>(loader.Invoke("glSampleCoverage"));
            _glCompressedTexImage3D = GetDelegateForFunctionPointer<PFNGLCOMPRESSEDTEXIMAGE3DPROC>(loader.Invoke("glCompressedTexImage3D"));
            _glCompressedTexImage2D = GetDelegateForFunctionPointer<PFNGLCOMPRESSEDTEXIMAGE2DPROC>(loader.Invoke("glCompressedTexImage2D"));
            _glCompressedTexImage1D = GetDelegateForFunctionPointer<PFNGLCOMPRESSEDTEXIMAGE1DPROC>(loader.Invoke("glCompressedTexImage1D"));
            _glCompressedTexSubImage3D = GetDelegateForFunctionPointer<PFNGLCOMPRESSEDTEXSUBIMAGE3DPROC>(loader.Invoke("glCompressedTexSubImage3D"));
            _glCompressedTexSubImage2D = GetDelegateForFunctionPointer<PFNGLCOMPRESSEDTEXSUBIMAGE2DPROC>(loader.Invoke("glCompressedTexSubImage2D"));
            _glCompressedTexSubImage1D = GetDelegateForFunctionPointer<PFNGLCOMPRESSEDTEXSUBIMAGE1DPROC>(loader.Invoke("glCompressedTexSubImage1D"));
            _glGetCompressedTexImage = GetDelegateForFunctionPointer<PFNGLGETCOMPRESSEDTEXIMAGEPROC>(loader.Invoke("glGetCompressedTexImage"));
            _glBlendFuncSeparate = GetDelegateForFunctionPointer<PFNGLBLENDFUNCSEPARATEPROC>(loader.Invoke("glBlendFuncSeparate"));
            _glMultiDrawArrays = GetDelegateForFunctionPointer<PFNGLMULTIDRAWARRAYSPROC>(loader.Invoke("glMultiDrawArrays"));
            _glMultiDrawElements = GetDelegateForFunctionPointer<PFNGLMULTIDRAWELEMENTSPROC>(loader.Invoke("glMultiDrawElements"));
            _glPointParameterf = GetDelegateForFunctionPointer<PFNGLPOINTPARAMETERFPROC>(loader.Invoke("glPointParameterf"));
            _glPointParameterfv = GetDelegateForFunctionPointer<PFNGLPOINTPARAMETERFVPROC>(loader.Invoke("glPointParameterfv"));
            _glPointParameteri = GetDelegateForFunctionPointer<PFNGLPOINTPARAMETERIPROC>(loader.Invoke("glPointParameteri"));
            _glPointParameteriv = GetDelegateForFunctionPointer<PFNGLPOINTPARAMETERIVPROC>(loader.Invoke("glPointParameteriv"));
            _glBlendColor = GetDelegateForFunctionPointer<PFNGLBLENDCOLORPROC>(loader.Invoke("glBlendColor"));
            _glBlendEquation = GetDelegateForFunctionPointer<PFNGLBLENDEQUATIONPROC>(loader.Invoke("glBlendEquation"));
            _glGenQueries = GetDelegateForFunctionPointer<PFNGLGENQUERIESPROC>(loader.Invoke("glGenQueries"));
            _glDeleteQueries = GetDelegateForFunctionPointer<PFNGLDELETEQUERIESPROC>(loader.Invoke("glDeleteQueries"));
            _glIsQuery = GetDelegateForFunctionPointer<PFNGLISQUERYPROC>(loader.Invoke("glIsQuery"));
            _glBeginQuery = GetDelegateForFunctionPointer<PFNGLBEGINQUERYPROC>(loader.Invoke("glBeginQuery"));
            _glEndQuery = GetDelegateForFunctionPointer<PFNGLENDQUERYPROC>(loader.Invoke("glEndQuery"));
            _glGetQueryiv = GetDelegateForFunctionPointer<PFNGLGETQUERYIVPROC>(loader.Invoke("glGetQueryiv"));
            _glGetQueryObjectiv = GetDelegateForFunctionPointer<PFNGLGETQUERYOBJECTIVPROC>(loader.Invoke("glGetQueryObjectiv"));
            _glGetQueryObjectuiv = GetDelegateForFunctionPointer<PFNGLGETQUERYOBJECTUIVPROC>(loader.Invoke("glGetQueryObjectuiv"));
            _glBindBuffer = GetDelegateForFunctionPointer<PFNGLBINDBUFFERPROC>(loader.Invoke("glBindBuffer"));
            _glDeleteBuffers = GetDelegateForFunctionPointer<PFNGLDELETEBUFFERSPROC>(loader.Invoke("glDeleteBuffers"));
            _glGenBuffers = GetDelegateForFunctionPointer<PFNGLGENBUFFERSPROC>(loader.Invoke("glGenBuffers"));
            _glIsBuffer = GetDelegateForFunctionPointer<PFNGLISBUFFERPROC>(loader.Invoke("glIsBuffer"));
            _glBufferData = GetDelegateForFunctionPointer<PFNGLBUFFERDATAPROC>(loader.Invoke("glBufferData"));
            _glBufferSubData = GetDelegateForFunctionPointer<PFNGLBUFFERSUBDATAPROC>(loader.Invoke("glBufferSubData"));
            _glGetBufferSubData = GetDelegateForFunctionPointer<PFNGLGETBUFFERSUBDATAPROC>(loader.Invoke("glGetBufferSubData"));
            _glMapBuffer = GetDelegateForFunctionPointer<PFNGLMAPBUFFERPROC>(loader.Invoke("glMapBuffer"));
            _glUnmapBuffer = GetDelegateForFunctionPointer<PFNGLUNMAPBUFFERPROC>(loader.Invoke("glUnmapBuffer"));
            _glGetBufferParameteriv = GetDelegateForFunctionPointer<PFNGLGETBUFFERPARAMETERIVPROC>(loader.Invoke("glGetBufferParameteriv"));
            _glGetBufferPointerv = GetDelegateForFunctionPointer<PFNGLGETBUFFERPOINTERVPROC>(loader.Invoke("glGetBufferPointerv"));
            _glBlendEquationSeparate = GetDelegateForFunctionPointer<PFNGLBLENDEQUATIONSEPARATEPROC>(loader.Invoke("glBlendEquationSeparate"));
            _glDrawBuffers = GetDelegateForFunctionPointer<PFNGLDRAWBUFFERSPROC>(loader.Invoke("glDrawBuffers"));
            _glStencilOpSeparate = GetDelegateForFunctionPointer<PFNGLSTENCILOPSEPARATEPROC>(loader.Invoke("glStencilOpSeparate"));
            _glStencilFuncSeparate = GetDelegateForFunctionPointer<PFNGLSTENCILFUNCSEPARATEPROC>(loader.Invoke("glStencilFuncSeparate"));
            _glStencilMaskSeparate = GetDelegateForFunctionPointer<PFNGLSTENCILMASKSEPARATEPROC>(loader.Invoke("glStencilMaskSeparate"));
            _glAttachShader = GetDelegateForFunctionPointer<PFNGLATTACHSHADERPROC>(loader.Invoke("glAttachShader"));
            _glBindAttribLocation = GetDelegateForFunctionPointer<PFNGLBINDATTRIBLOCATIONPROC>(loader.Invoke("glBindAttribLocation"));
            _glCompileShader = GetDelegateForFunctionPointer<PFNGLCOMPILESHADERPROC>(loader.Invoke("glCompileShader"));
            _glCreateProgram = GetDelegateForFunctionPointer<PFNGLCREATEPROGRAMPROC>(loader.Invoke("glCreateProgram"));
            _glCreateShader = GetDelegateForFunctionPointer<PFNGLCREATESHADERPROC>(loader.Invoke("glCreateShader"));
            _glDeleteProgram = GetDelegateForFunctionPointer<PFNGLDELETEPROGRAMPROC>(loader.Invoke("glDeleteProgram"));
            _glDeleteShader = GetDelegateForFunctionPointer<PFNGLDELETESHADERPROC>(loader.Invoke("glDeleteShader"));
            _glDetachShader = GetDelegateForFunctionPointer<PFNGLDETACHSHADERPROC>(loader.Invoke("glDetachShader"));
            _glDisableVertexAttribArray = GetDelegateForFunctionPointer<PFNGLDISABLEVERTEXATTRIBARRAYPROC>(loader.Invoke("glDisableVertexAttribArray"));
            _glEnableVertexAttribArray = GetDelegateForFunctionPointer<PFNGLENABLEVERTEXATTRIBARRAYPROC>(loader.Invoke("glEnableVertexAttribArray"));
            _glGetActiveAttrib = GetDelegateForFunctionPointer<PFNGLGETACTIVEATTRIBPROC>(loader.Invoke("glGetActiveAttrib"));
            _glGetActiveUniform = GetDelegateForFunctionPointer<PFNGLGETACTIVEUNIFORMPROC>(loader.Invoke("glGetActiveUniform"));
            _glGetAttachedShaders = GetDelegateForFunctionPointer<PFNGLGETATTACHEDSHADERSPROC>(loader.Invoke("glGetAttachedShaders"));
            _glGetAttribLocation = GetDelegateForFunctionPointer<PFNGLGETATTRIBLOCATIONPROC>(loader.Invoke("glGetAttribLocation"));
            _glGetProgramiv = GetDelegateForFunctionPointer<PFNGLGETPROGRAMIVPROC>(loader.Invoke("glGetProgramiv"));
            _glGetProgramInfoLog = GetDelegateForFunctionPointer<PFNGLGETPROGRAMINFOLOGPROC>(loader.Invoke("glGetProgramInfoLog"));
            _glGetShaderiv = GetDelegateForFunctionPointer<PFNGLGETSHADERIVPROC>(loader.Invoke("glGetShaderiv"));
            _glGetShaderInfoLog = GetDelegateForFunctionPointer<PFNGLGETSHADERINFOLOGPROC>(loader.Invoke("glGetShaderInfoLog"));
            _glGetShaderSource = GetDelegateForFunctionPointer<PFNGLGETSHADERSOURCEPROC>(loader.Invoke("glGetShaderSource"));
            _glGetUniformLocation = GetDelegateForFunctionPointer<PFNGLGETUNIFORMLOCATIONPROC>(loader.Invoke("glGetUniformLocation"));
            _glGetUniformfv = GetDelegateForFunctionPointer<PFNGLGETUNIFORMFVPROC>(loader.Invoke("glGetUniformfv"));
            _glGetUniformiv = GetDelegateForFunctionPointer<PFNGLGETUNIFORMIVPROC>(loader.Invoke("glGetUniformiv"));
            _glGetVertexAttribdv = GetDelegateForFunctionPointer<PFNGLGETVERTEXATTRIBDVPROC>(loader.Invoke("glGetVertexAttribdv"));
            _glGetVertexAttribfv = GetDelegateForFunctionPointer<PFNGLGETVERTEXATTRIBFVPROC>(loader.Invoke("glGetVertexAttribfv"));
            _glGetVertexAttribiv = GetDelegateForFunctionPointer<PFNGLGETVERTEXATTRIBIVPROC>(loader.Invoke("glGetVertexAttribiv"));
            _glGetVertexAttribPointerv = GetDelegateForFunctionPointer<PFNGLGETVERTEXATTRIBPOINTERVPROC>(loader.Invoke("glGetVertexAttribPointerv"));
            _glIsProgram = GetDelegateForFunctionPointer<PFNGLISPROGRAMPROC>(loader.Invoke("glIsProgram"));
            _glIsShader = GetDelegateForFunctionPointer<PFNGLISSHADERPROC>(loader.Invoke("glIsShader"));
            _glLinkProgram = GetDelegateForFunctionPointer<PFNGLLINKPROGRAMPROC>(loader.Invoke("glLinkProgram"));
            _glShaderSource = GetDelegateForFunctionPointer<PFNGLSHADERSOURCEPROC>(loader.Invoke("glShaderSource"));
            _glUseProgram = GetDelegateForFunctionPointer<PFNGLUSEPROGRAMPROC>(loader.Invoke("glUseProgram"));
            _glUniform1f = GetDelegateForFunctionPointer<PFNGLUNIFORM1FPROC>(loader.Invoke("glUniform1f"));
            _glUniform2f = GetDelegateForFunctionPointer<PFNGLUNIFORM2FPROC>(loader.Invoke("glUniform2f"));
            _glUniform3f = GetDelegateForFunctionPointer<PFNGLUNIFORM3FPROC>(loader.Invoke("glUniform3f"));
            _glUniform4f = GetDelegateForFunctionPointer<PFNGLUNIFORM4FPROC>(loader.Invoke("glUniform4f"));
            _glUniform1i = GetDelegateForFunctionPointer<PFNGLUNIFORM1IPROC>(loader.Invoke("glUniform1i"));
            _glUniform2i = GetDelegateForFunctionPointer<PFNGLUNIFORM2IPROC>(loader.Invoke("glUniform2i"));
            _glUniform3i = GetDelegateForFunctionPointer<PFNGLUNIFORM3IPROC>(loader.Invoke("glUniform3i"));
            _glUniform4i = GetDelegateForFunctionPointer<PFNGLUNIFORM4IPROC>(loader.Invoke("glUniform4i"));
            _glUniform1fv = GetDelegateForFunctionPointer<PFNGLUNIFORM1FVPROC>(loader.Invoke("glUniform1fv"));
            _glUniform2fv = GetDelegateForFunctionPointer<PFNGLUNIFORM2FVPROC>(loader.Invoke("glUniform2fv"));
            _glUniform3fv = GetDelegateForFunctionPointer<PFNGLUNIFORM3FVPROC>(loader.Invoke("glUniform3fv"));
            _glUniform4fv = GetDelegateForFunctionPointer<PFNGLUNIFORM4FVPROC>(loader.Invoke("glUniform4fv"));
            _glUniform1iv = GetDelegateForFunctionPointer<PFNGLUNIFORM1IVPROC>(loader.Invoke("glUniform1iv"));
            _glUniform2iv = GetDelegateForFunctionPointer<PFNGLUNIFORM2IVPROC>(loader.Invoke("glUniform2iv"));
            _glUniform3iv = GetDelegateForFunctionPointer<PFNGLUNIFORM3IVPROC>(loader.Invoke("glUniform3iv"));
            _glUniform4iv = GetDelegateForFunctionPointer<PFNGLUNIFORM4IVPROC>(loader.Invoke("glUniform4iv"));
            _glUniformMatrix2fv = GetDelegateForFunctionPointer<PFNGLUNIFORMMATRIX2FVPROC>(loader.Invoke("glUniformMatrix2fv"));
            _glUniformMatrix3fv = GetDelegateForFunctionPointer<PFNGLUNIFORMMATRIX3FVPROC>(loader.Invoke("glUniformMatrix3fv"));
            _glUniformMatrix4fv = GetDelegateForFunctionPointer<PFNGLUNIFORMMATRIX4FVPROC>(loader.Invoke("glUniformMatrix4fv"));
            _glValidateProgram = GetDelegateForFunctionPointer<PFNGLVALIDATEPROGRAMPROC>(loader.Invoke("glValidateProgram"));
            _glVertexAttrib1d = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIB1DPROC>(loader.Invoke("glVertexAttrib1d"));
            _glVertexAttrib1dv = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIB1DVPROC>(loader.Invoke("glVertexAttrib1dv"));
            _glVertexAttrib1f = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIB1FPROC>(loader.Invoke("glVertexAttrib1f"));
            _glVertexAttrib1fv = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIB1FVPROC>(loader.Invoke("glVertexAttrib1fv"));
            _glVertexAttrib1s = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIB1SPROC>(loader.Invoke("glVertexAttrib1s"));
            _glVertexAttrib1sv = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIB1SVPROC>(loader.Invoke("glVertexAttrib1sv"));
            _glVertexAttrib2d = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIB2DPROC>(loader.Invoke("glVertexAttrib2d"));
            _glVertexAttrib2dv = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIB2DVPROC>(loader.Invoke("glVertexAttrib2dv"));
            _glVertexAttrib2f = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIB2FPROC>(loader.Invoke("glVertexAttrib2f"));
            _glVertexAttrib2fv = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIB2FVPROC>(loader.Invoke("glVertexAttrib2fv"));
            _glVertexAttrib2s = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIB2SPROC>(loader.Invoke("glVertexAttrib2s"));
            _glVertexAttrib2sv = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIB2SVPROC>(loader.Invoke("glVertexAttrib2sv"));
            _glVertexAttrib3d = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIB3DPROC>(loader.Invoke("glVertexAttrib3d"));
            _glVertexAttrib3dv = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIB3DVPROC>(loader.Invoke("glVertexAttrib3dv"));
            _glVertexAttrib3f = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIB3FPROC>(loader.Invoke("glVertexAttrib3f"));
            _glVertexAttrib3fv = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIB3FVPROC>(loader.Invoke("glVertexAttrib3fv"));
            _glVertexAttrib3s = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIB3SPROC>(loader.Invoke("glVertexAttrib3s"));
            _glVertexAttrib3sv = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIB3SVPROC>(loader.Invoke("glVertexAttrib3sv"));
            _glVertexAttrib4Nbv = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIB4NBVPROC>(loader.Invoke("glVertexAttrib4Nbv"));
            _glVertexAttrib4Niv = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIB4NIVPROC>(loader.Invoke("glVertexAttrib4Niv"));
            _glVertexAttrib4Nsv = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIB4NSVPROC>(loader.Invoke("glVertexAttrib4Nsv"));
            _glVertexAttrib4Nub = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIB4NUBPROC>(loader.Invoke("glVertexAttrib4Nub"));
            _glVertexAttrib4Nubv = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIB4NUBVPROC>(loader.Invoke("glVertexAttrib4Nubv"));
            _glVertexAttrib4Nuiv = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIB4NUIVPROC>(loader.Invoke("glVertexAttrib4Nuiv"));
            _glVertexAttrib4Nusv = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIB4NUSVPROC>(loader.Invoke("glVertexAttrib4Nusv"));
            _glVertexAttrib4bv = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIB4BVPROC>(loader.Invoke("glVertexAttrib4bv"));
            _glVertexAttrib4d = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIB4DPROC>(loader.Invoke("glVertexAttrib4d"));
            _glVertexAttrib4dv = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIB4DVPROC>(loader.Invoke("glVertexAttrib4dv"));
            _glVertexAttrib4f = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIB4FPROC>(loader.Invoke("glVertexAttrib4f"));
            _glVertexAttrib4fv = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIB4FVPROC>(loader.Invoke("glVertexAttrib4fv"));
            _glVertexAttrib4iv = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIB4IVPROC>(loader.Invoke("glVertexAttrib4iv"));
            _glVertexAttrib4s = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIB4SPROC>(loader.Invoke("glVertexAttrib4s"));
            _glVertexAttrib4sv = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIB4SVPROC>(loader.Invoke("glVertexAttrib4sv"));
            _glVertexAttrib4ubv = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIB4UBVPROC>(loader.Invoke("glVertexAttrib4ubv"));
            _glVertexAttrib4uiv = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIB4UIVPROC>(loader.Invoke("glVertexAttrib4uiv"));
            _glVertexAttrib4usv = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIB4USVPROC>(loader.Invoke("glVertexAttrib4usv"));
            _glVertexAttribPointer = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIBPOINTERPROC>(loader.Invoke("glVertexAttribPointer"));
            _glUniformMatrix2x3fv = GetDelegateForFunctionPointer<PFNGLUNIFORMMATRIX2X3FVPROC>(loader.Invoke("glUniformMatrix2x3fv"));
            _glUniformMatrix3x2fv = GetDelegateForFunctionPointer<PFNGLUNIFORMMATRIX3X2FVPROC>(loader.Invoke("glUniformMatrix3x2fv"));
            _glUniformMatrix2x4fv = GetDelegateForFunctionPointer<PFNGLUNIFORMMATRIX2X4FVPROC>(loader.Invoke("glUniformMatrix2x4fv"));
            _glUniformMatrix4x2fv = GetDelegateForFunctionPointer<PFNGLUNIFORMMATRIX4X2FVPROC>(loader.Invoke("glUniformMatrix4x2fv"));
            _glUniformMatrix3x4fv = GetDelegateForFunctionPointer<PFNGLUNIFORMMATRIX3X4FVPROC>(loader.Invoke("glUniformMatrix3x4fv"));
            _glUniformMatrix4x3fv = GetDelegateForFunctionPointer<PFNGLUNIFORMMATRIX4X3FVPROC>(loader.Invoke("glUniformMatrix4x3fv"));
            _glColorMaski = GetDelegateForFunctionPointer<PFNGLCOLORMASKIPROC>(loader.Invoke("glColorMaski"));
            _glGetBooleani_v = GetDelegateForFunctionPointer<PFNGLGETBOOLEANI_VPROC>(loader.Invoke("glGetBooleani_v"));
            _glGetIntegeri_v = GetDelegateForFunctionPointer<PFNGLGETINTEGERI_VPROC>(loader.Invoke("glGetIntegeri_v"));
            _glEnablei = GetDelegateForFunctionPointer<PFNGLENABLEIPROC>(loader.Invoke("glEnablei"));
            _glDisablei = GetDelegateForFunctionPointer<PFNGLDISABLEIPROC>(loader.Invoke("glDisablei"));
            _glIsEnabledi = GetDelegateForFunctionPointer<PFNGLISENABLEDIPROC>(loader.Invoke("glIsEnabledi"));
            _glBeginTransformFeedback = GetDelegateForFunctionPointer<PFNGLBEGINTRANSFORMFEEDBACKPROC>(loader.Invoke("glBeginTransformFeedback"));
            _glEndTransformFeedback = GetDelegateForFunctionPointer<PFNGLENDTRANSFORMFEEDBACKPROC>(loader.Invoke("glEndTransformFeedback"));
            _glBindBufferRange = GetDelegateForFunctionPointer<PFNGLBINDBUFFERRANGEPROC>(loader.Invoke("glBindBufferRange"));
            _glBindBufferBase = GetDelegateForFunctionPointer<PFNGLBINDBUFFERBASEPROC>(loader.Invoke("glBindBufferBase"));
            _glTransformFeedbackVaryings = GetDelegateForFunctionPointer<PFNGLTRANSFORMFEEDBACKVARYINGSPROC>(loader.Invoke("glTransformFeedbackVaryings"));
            _glGetTransformFeedbackVarying = GetDelegateForFunctionPointer<PFNGLGETTRANSFORMFEEDBACKVARYINGPROC>(loader.Invoke("glGetTransformFeedbackVarying"));
            _glClampColor = GetDelegateForFunctionPointer<PFNGLCLAMPCOLORPROC>(loader.Invoke("glClampColor"));
            _glBeginConditionalRender = GetDelegateForFunctionPointer<PFNGLBEGINCONDITIONALRENDERPROC>(loader.Invoke("glBeginConditionalRender"));
            _glEndConditionalRender = GetDelegateForFunctionPointer<PFNGLENDCONDITIONALRENDERPROC>(loader.Invoke("glEndConditionalRender"));
            _glVertexAttribIPointer = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIBIPOINTERPROC>(loader.Invoke("glVertexAttribIPointer"));
            _glGetVertexAttribIiv = GetDelegateForFunctionPointer<PFNGLGETVERTEXATTRIBIIVPROC>(loader.Invoke("glGetVertexAttribIiv"));
            _glGetVertexAttribIuiv = GetDelegateForFunctionPointer<PFNGLGETVERTEXATTRIBIUIVPROC>(loader.Invoke("glGetVertexAttribIuiv"));
            _glVertexAttribI1i = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIBI1IPROC>(loader.Invoke("glVertexAttribI1i"));
            _glVertexAttribI2i = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIBI2IPROC>(loader.Invoke("glVertexAttribI2i"));
            _glVertexAttribI3i = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIBI3IPROC>(loader.Invoke("glVertexAttribI3i"));
            _glVertexAttribI4i = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIBI4IPROC>(loader.Invoke("glVertexAttribI4i"));
            _glVertexAttribI1ui = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIBI1UIPROC>(loader.Invoke("glVertexAttribI1ui"));
            _glVertexAttribI2ui = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIBI2UIPROC>(loader.Invoke("glVertexAttribI2ui"));
            _glVertexAttribI3ui = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIBI3UIPROC>(loader.Invoke("glVertexAttribI3ui"));
            _glVertexAttribI4ui = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIBI4UIPROC>(loader.Invoke("glVertexAttribI4ui"));
            _glVertexAttribI1iv = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIBI1IVPROC>(loader.Invoke("glVertexAttribI1iv"));
            _glVertexAttribI2iv = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIBI2IVPROC>(loader.Invoke("glVertexAttribI2iv"));
            _glVertexAttribI3iv = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIBI3IVPROC>(loader.Invoke("glVertexAttribI3iv"));
            _glVertexAttribI4iv = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIBI4IVPROC>(loader.Invoke("glVertexAttribI4iv"));
            _glVertexAttribI1uiv = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIBI1UIVPROC>(loader.Invoke("glVertexAttribI1uiv"));
            _glVertexAttribI2uiv = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIBI2UIVPROC>(loader.Invoke("glVertexAttribI2uiv"));
            _glVertexAttribI3uiv = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIBI3UIVPROC>(loader.Invoke("glVertexAttribI3uiv"));
            _glVertexAttribI4uiv = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIBI4UIVPROC>(loader.Invoke("glVertexAttribI4uiv"));
            _glVertexAttribI4bv = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIBI4BVPROC>(loader.Invoke("glVertexAttribI4bv"));
            _glVertexAttribI4sv = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIBI4SVPROC>(loader.Invoke("glVertexAttribI4sv"));
            _glVertexAttribI4ubv = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIBI4UBVPROC>(loader.Invoke("glVertexAttribI4ubv"));
            _glVertexAttribI4usv = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIBI4USVPROC>(loader.Invoke("glVertexAttribI4usv"));
            _glGetUniformuiv = GetDelegateForFunctionPointer<PFNGLGETUNIFORMUIVPROC>(loader.Invoke("glGetUniformuiv"));
            _glBindFragDataLocation = GetDelegateForFunctionPointer<PFNGLBINDFRAGDATALOCATIONPROC>(loader.Invoke("glBindFragDataLocation"));
            _glGetFragDataLocation = GetDelegateForFunctionPointer<PFNGLGETFRAGDATALOCATIONPROC>(loader.Invoke("glGetFragDataLocation"));
            _glUniform1ui = GetDelegateForFunctionPointer<PFNGLUNIFORM1UIPROC>(loader.Invoke("glUniform1ui"));
            _glUniform2ui = GetDelegateForFunctionPointer<PFNGLUNIFORM2UIPROC>(loader.Invoke("glUniform2ui"));
            _glUniform3ui = GetDelegateForFunctionPointer<PFNGLUNIFORM3UIPROC>(loader.Invoke("glUniform3ui"));
            _glUniform4ui = GetDelegateForFunctionPointer<PFNGLUNIFORM4UIPROC>(loader.Invoke("glUniform4ui"));
            _glUniform1uiv = GetDelegateForFunctionPointer<PFNGLUNIFORM1UIVPROC>(loader.Invoke("glUniform1uiv"));
            _glUniform2uiv = GetDelegateForFunctionPointer<PFNGLUNIFORM2UIVPROC>(loader.Invoke("glUniform2uiv"));
            _glUniform3uiv = GetDelegateForFunctionPointer<PFNGLUNIFORM3UIVPROC>(loader.Invoke("glUniform3uiv"));
            _glUniform4uiv = GetDelegateForFunctionPointer<PFNGLUNIFORM4UIVPROC>(loader.Invoke("glUniform4uiv"));
            _glTexParameterIiv = GetDelegateForFunctionPointer<PFNGLTEXPARAMETERIIVPROC>(loader.Invoke("glTexParameterIiv"));
            _glTexParameterIuiv = GetDelegateForFunctionPointer<PFNGLTEXPARAMETERIUIVPROC>(loader.Invoke("glTexParameterIuiv"));
            _glGetTexParameterIiv = GetDelegateForFunctionPointer<PFNGLGETTEXPARAMETERIIVPROC>(loader.Invoke("glGetTexParameterIiv"));
            _glGetTexParameterIuiv = GetDelegateForFunctionPointer<PFNGLGETTEXPARAMETERIUIVPROC>(loader.Invoke("glGetTexParameterIuiv"));
            _glClearBufferiv = GetDelegateForFunctionPointer<PFNGLCLEARBUFFERIVPROC>(loader.Invoke("glClearBufferiv"));
            _glClearBufferuiv = GetDelegateForFunctionPointer<PFNGLCLEARBUFFERUIVPROC>(loader.Invoke("glClearBufferuiv"));
            _glClearBufferfv = GetDelegateForFunctionPointer<PFNGLCLEARBUFFERFVPROC>(loader.Invoke("glClearBufferfv"));
            _glClearBufferfi = GetDelegateForFunctionPointer<PFNGLCLEARBUFFERFIPROC>(loader.Invoke("glClearBufferfi"));
            _glGetStringi = GetDelegateForFunctionPointer<PFNGLGETSTRINGIPROC>(loader.Invoke("glGetStringi"));
            _glIsRenderbuffer = GetDelegateForFunctionPointer<PFNGLISRENDERBUFFERPROC>(loader.Invoke("glIsRenderbuffer"));
            _glBindRenderbuffer = GetDelegateForFunctionPointer<PFNGLBINDRENDERBUFFERPROC>(loader.Invoke("glBindRenderbuffer"));
            _glDeleteRenderbuffers = GetDelegateForFunctionPointer<PFNGLDELETERENDERBUFFERSPROC>(loader.Invoke("glDeleteRenderbuffers"));
            _glGenRenderbuffers = GetDelegateForFunctionPointer<PFNGLGENRENDERBUFFERSPROC>(loader.Invoke("glGenRenderbuffers"));
            _glRenderbufferStorage = GetDelegateForFunctionPointer<PFNGLRENDERBUFFERSTORAGEPROC>(loader.Invoke("glRenderbufferStorage"));
            _glGetRenderbufferParameteriv = GetDelegateForFunctionPointer<PFNGLGETRENDERBUFFERPARAMETERIVPROC>(loader.Invoke("glGetRenderbufferParameteriv"));
            _glIsFramebuffer = GetDelegateForFunctionPointer<PFNGLISFRAMEBUFFERPROC>(loader.Invoke("glIsFramebuffer"));
            _glBindFramebuffer = GetDelegateForFunctionPointer<PFNGLBINDFRAMEBUFFERPROC>(loader.Invoke("glBindFramebuffer"));
            _glDeleteFramebuffers = GetDelegateForFunctionPointer<PFNGLDELETEFRAMEBUFFERSPROC>(loader.Invoke("glDeleteFramebuffers"));
            _glGenFramebuffers = GetDelegateForFunctionPointer<PFNGLGENFRAMEBUFFERSPROC>(loader.Invoke("glGenFramebuffers"));
            _glCheckFramebufferStatus = GetDelegateForFunctionPointer<PFNGLCHECKFRAMEBUFFERSTATUSPROC>(loader.Invoke("glCheckFramebufferStatus"));
            _glFramebufferTexture1D = GetDelegateForFunctionPointer<PFNGLFRAMEBUFFERTEXTURE1DPROC>(loader.Invoke("glFramebufferTexture1D"));
            _glFramebufferTexture2D = GetDelegateForFunctionPointer<PFNGLFRAMEBUFFERTEXTURE2DPROC>(loader.Invoke("glFramebufferTexture2D"));
            _glFramebufferTexture3D = GetDelegateForFunctionPointer<PFNGLFRAMEBUFFERTEXTURE3DPROC>(loader.Invoke("glFramebufferTexture3D"));
            _glFramebufferRenderbuffer = GetDelegateForFunctionPointer<PFNGLFRAMEBUFFERRENDERBUFFERPROC>(loader.Invoke("glFramebufferRenderbuffer"));
            _glGetFramebufferAttachmentParameteriv = GetDelegateForFunctionPointer<PFNGLGETFRAMEBUFFERATTACHMENTPARAMETERIVPROC>(loader.Invoke("glGetFramebufferAttachmentParameteriv"));
            _glGenerateMipmap = GetDelegateForFunctionPointer<PFNGLGENERATEMIPMAPPROC>(loader.Invoke("glGenerateMipmap"));
            _glBlitFramebuffer = GetDelegateForFunctionPointer<PFNGLBLITFRAMEBUFFERPROC>(loader.Invoke("glBlitFramebuffer"));
            _glRenderbufferStorageMultisample = GetDelegateForFunctionPointer<PFNGLRENDERBUFFERSTORAGEMULTISAMPLEPROC>(loader.Invoke("glRenderbufferStorageMultisample"));
            _glFramebufferTextureLayer = GetDelegateForFunctionPointer<PFNGLFRAMEBUFFERTEXTURELAYERPROC>(loader.Invoke("glFramebufferTextureLayer"));
            _glMapBufferRange = GetDelegateForFunctionPointer<PFNGLMAPBUFFERRANGEPROC>(loader.Invoke("glMapBufferRange"));
            _glFlushMappedBufferRange = GetDelegateForFunctionPointer<PFNGLFLUSHMAPPEDBUFFERRANGEPROC>(loader.Invoke("glFlushMappedBufferRange"));
            _glBindVertexArray = GetDelegateForFunctionPointer<PFNGLBINDVERTEXARRAYPROC>(loader.Invoke("glBindVertexArray"));
            _glDeleteVertexArrays = GetDelegateForFunctionPointer<PFNGLDELETEVERTEXARRAYSPROC>(loader.Invoke("glDeleteVertexArrays"));
            _glGenVertexArrays = GetDelegateForFunctionPointer<PFNGLGENVERTEXARRAYSPROC>(loader.Invoke("glGenVertexArrays"));
            _glIsVertexArray = GetDelegateForFunctionPointer<PFNGLISVERTEXARRAYPROC>(loader.Invoke("glIsVertexArray"));
            _glDrawArraysInstanced = GetDelegateForFunctionPointer<PFNGLDRAWARRAYSINSTANCEDPROC>(loader.Invoke("glDrawArraysInstanced"));
            _glDrawElementsInstanced = GetDelegateForFunctionPointer<PFNGLDRAWELEMENTSINSTANCEDPROC>(loader.Invoke("glDrawElementsInstanced"));
            _glTexBuffer = GetDelegateForFunctionPointer<PFNGLTEXBUFFERPROC>(loader.Invoke("glTexBuffer"));
            _glPrimitiveRestartIndex = GetDelegateForFunctionPointer<PFNGLPRIMITIVERESTARTINDEXPROC>(loader.Invoke("glPrimitiveRestartIndex"));
            _glCopyBufferSubData = GetDelegateForFunctionPointer<PFNGLCOPYBUFFERSUBDATAPROC>(loader.Invoke("glCopyBufferSubData"));
            _glGetUniformIndices = GetDelegateForFunctionPointer<PFNGLGETUNIFORMINDICESPROC>(loader.Invoke("glGetUniformIndices"));
            _glGetActiveUniformsiv = GetDelegateForFunctionPointer<PFNGLGETACTIVEUNIFORMSIVPROC>(loader.Invoke("glGetActiveUniformsiv"));
            _glGetActiveUniformName = GetDelegateForFunctionPointer<PFNGLGETACTIVEUNIFORMNAMEPROC>(loader.Invoke("glGetActiveUniformName"));
            _glGetUniformBlockIndex = GetDelegateForFunctionPointer<PFNGLGETUNIFORMBLOCKINDEXPROC>(loader.Invoke("glGetUniformBlockIndex"));
            _glGetActiveUniformBlockiv = GetDelegateForFunctionPointer<PFNGLGETACTIVEUNIFORMBLOCKIVPROC>(loader.Invoke("glGetActiveUniformBlockiv"));
            _glGetActiveUniformBlockName = GetDelegateForFunctionPointer<PFNGLGETACTIVEUNIFORMBLOCKNAMEPROC>(loader.Invoke("glGetActiveUniformBlockName"));
            _glUniformBlockBinding = GetDelegateForFunctionPointer<PFNGLUNIFORMBLOCKBINDINGPROC>(loader.Invoke("glUniformBlockBinding"));
            _glBindBufferRange = GetDelegateForFunctionPointer<PFNGLBINDBUFFERRANGEPROC>(loader.Invoke("glBindBufferRange"));
            _glBindBufferBase = GetDelegateForFunctionPointer<PFNGLBINDBUFFERBASEPROC>(loader.Invoke("glBindBufferBase"));
            _glGetIntegeri_v = GetDelegateForFunctionPointer<PFNGLGETINTEGERI_VPROC>(loader.Invoke("glGetIntegeri_v"));
            _glDrawElementsBaseVertex = GetDelegateForFunctionPointer<PFNGLDRAWELEMENTSBASEVERTEXPROC>(loader.Invoke("glDrawElementsBaseVertex"));
            _glDrawRangeElementsBaseVertex = GetDelegateForFunctionPointer<PFNGLDRAWRANGEELEMENTSBASEVERTEXPROC>(loader.Invoke("glDrawRangeElementsBaseVertex"));
            _glDrawElementsInstancedBaseVertex = GetDelegateForFunctionPointer<PFNGLDRAWELEMENTSINSTANCEDBASEVERTEXPROC>(loader.Invoke("glDrawElementsInstancedBaseVertex"));
            _glMultiDrawElementsBaseVertex = GetDelegateForFunctionPointer<PFNGLMULTIDRAWELEMENTSBASEVERTEXPROC>(loader.Invoke("glMultiDrawElementsBaseVertex"));
            _glProvokingVertex = GetDelegateForFunctionPointer<PFNGLPROVOKINGVERTEXPROC>(loader.Invoke("glProvokingVertex"));
            _glFenceSync = GetDelegateForFunctionPointer<PFNGLFENCESYNCPROC>(loader.Invoke("glFenceSync"));
            _glIsSync = GetDelegateForFunctionPointer<PFNGLISSYNCPROC>(loader.Invoke("glIsSync"));
            _glDeleteSync = GetDelegateForFunctionPointer<PFNGLDELETESYNCPROC>(loader.Invoke("glDeleteSync"));
            _glClientWaitSync = GetDelegateForFunctionPointer<PFNGLCLIENTWAITSYNCPROC>(loader.Invoke("glClientWaitSync"));
            _glWaitSync = GetDelegateForFunctionPointer<PFNGLWAITSYNCPROC>(loader.Invoke("glWaitSync"));
            _glGetInteger64v = GetDelegateForFunctionPointer<PFNGLGETINTEGER64VPROC>(loader.Invoke("glGetInteger64v"));
            _glGetSynciv = GetDelegateForFunctionPointer<PFNGLGETSYNCIVPROC>(loader.Invoke("glGetSynciv"));
            _glGetInteger64i_v = GetDelegateForFunctionPointer<PFNGLGETINTEGER64I_VPROC>(loader.Invoke("glGetInteger64i_v"));
            _glGetBufferParameteri64v = GetDelegateForFunctionPointer<PFNGLGETBUFFERPARAMETERI64VPROC>(loader.Invoke("glGetBufferParameteri64v"));
            _glFramebufferTexture = GetDelegateForFunctionPointer<PFNGLFRAMEBUFFERTEXTUREPROC>(loader.Invoke("glFramebufferTexture"));
            _glTexImage2DMultisample = GetDelegateForFunctionPointer<PFNGLTEXIMAGE2DMULTISAMPLEPROC>(loader.Invoke("glTexImage2DMultisample"));
            _glTexImage3DMultisample = GetDelegateForFunctionPointer<PFNGLTEXIMAGE3DMULTISAMPLEPROC>(loader.Invoke("glTexImage3DMultisample"));
            _glGetMultisamplefv = GetDelegateForFunctionPointer<PFNGLGETMULTISAMPLEFVPROC>(loader.Invoke("glGetMultisamplefv"));
            _glSampleMaski = GetDelegateForFunctionPointer<PFNGLSAMPLEMASKIPROC>(loader.Invoke("glSampleMaski"));
            _glBindFragDataLocationIndexed = GetDelegateForFunctionPointer<PFNGLBINDFRAGDATALOCATIONINDEXEDPROC>(loader.Invoke("glBindFragDataLocationIndexed"));
            _glGetFragDataIndex = GetDelegateForFunctionPointer<PFNGLGETFRAGDATAINDEXPROC>(loader.Invoke("glGetFragDataIndex"));
            _glGenSamplers = GetDelegateForFunctionPointer<PFNGLGENSAMPLERSPROC>(loader.Invoke("glGenSamplers"));
            _glDeleteSamplers = GetDelegateForFunctionPointer<PFNGLDELETESAMPLERSPROC>(loader.Invoke("glDeleteSamplers"));
            _glIsSampler = GetDelegateForFunctionPointer<PFNGLISSAMPLERPROC>(loader.Invoke("glIsSampler"));
            _glBindSampler = GetDelegateForFunctionPointer<PFNGLBINDSAMPLERPROC>(loader.Invoke("glBindSampler"));
            _glSamplerParameteri = GetDelegateForFunctionPointer<PFNGLSAMPLERPARAMETERIPROC>(loader.Invoke("glSamplerParameteri"));
            _glSamplerParameteriv = GetDelegateForFunctionPointer<PFNGLSAMPLERPARAMETERIVPROC>(loader.Invoke("glSamplerParameteriv"));
            _glSamplerParameterf = GetDelegateForFunctionPointer<PFNGLSAMPLERPARAMETERFPROC>(loader.Invoke("glSamplerParameterf"));
            _glSamplerParameterfv = GetDelegateForFunctionPointer<PFNGLSAMPLERPARAMETERFVPROC>(loader.Invoke("glSamplerParameterfv"));
            _glSamplerParameterIiv = GetDelegateForFunctionPointer<PFNGLSAMPLERPARAMETERIIVPROC>(loader.Invoke("glSamplerParameterIiv"));
            _glSamplerParameterIuiv = GetDelegateForFunctionPointer<PFNGLSAMPLERPARAMETERIUIVPROC>(loader.Invoke("glSamplerParameterIuiv"));
            _glGetSamplerParameteriv = GetDelegateForFunctionPointer<PFNGLGETSAMPLERPARAMETERIVPROC>(loader.Invoke("glGetSamplerParameteriv"));
            _glGetSamplerParameterIiv = GetDelegateForFunctionPointer<PFNGLGETSAMPLERPARAMETERIIVPROC>(loader.Invoke("glGetSamplerParameterIiv"));
            _glGetSamplerParameterfv = GetDelegateForFunctionPointer<PFNGLGETSAMPLERPARAMETERFVPROC>(loader.Invoke("glGetSamplerParameterfv"));
            _glGetSamplerParameterIuiv = GetDelegateForFunctionPointer<PFNGLGETSAMPLERPARAMETERIUIVPROC>(loader.Invoke("glGetSamplerParameterIuiv"));
            _glGetQueryObjecti64v = GetDelegateForFunctionPointer<PFNGLGETQUERYOBJECTI64VPROC>(loader.Invoke("glGetQueryObjecti64v"));
            _glGetQueryObjectui64v = GetDelegateForFunctionPointer<PFNGLGETQUERYOBJECTUI64VPROC>(loader.Invoke("glGetQueryObjectui64v"));
            _glVertexAttribDivisor = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIBDIVISORPROC>(loader.Invoke("glVertexAttribDivisor"));
            _glVertexAttribP1ui = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIBP1UIPROC>(loader.Invoke("glVertexAttribP1ui"));
            _glVertexAttribP1uiv = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIBP1UIVPROC>(loader.Invoke("glVertexAttribP1uiv"));
            _glVertexAttribP2ui = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIBP2UIPROC>(loader.Invoke("glVertexAttribP2ui"));
            _glVertexAttribP2uiv = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIBP2UIVPROC>(loader.Invoke("glVertexAttribP2uiv"));
            _glVertexAttribP3ui = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIBP3UIPROC>(loader.Invoke("glVertexAttribP3ui"));
            _glVertexAttribP3uiv = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIBP3UIVPROC>(loader.Invoke("glVertexAttribP3uiv"));
            _glVertexAttribP4ui = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIBP4UIPROC>(loader.Invoke("glVertexAttribP4ui"));
            _glVertexAttribP4uiv = GetDelegateForFunctionPointer<PFNGLVERTEXATTRIBP4UIVPROC>(loader.Invoke("glVertexAttribP4uiv"));
            _glVertexP2ui = GetDelegateForFunctionPointer<PFNGLVERTEXP2UIPROC>(loader.Invoke("glVertexP2ui"));
            _glVertexP2uiv = GetDelegateForFunctionPointer<PFNGLVERTEXP2UIVPROC>(loader.Invoke("glVertexP2uiv"));
            _glVertexP3ui = GetDelegateForFunctionPointer<PFNGLVERTEXP3UIPROC>(loader.Invoke("glVertexP3ui"));
            _glVertexP3uiv = GetDelegateForFunctionPointer<PFNGLVERTEXP3UIVPROC>(loader.Invoke("glVertexP3uiv"));
            _glVertexP4ui = GetDelegateForFunctionPointer<PFNGLVERTEXP4UIPROC>(loader.Invoke("glVertexP4ui"));
            _glVertexP4uiv = GetDelegateForFunctionPointer<PFNGLVERTEXP4UIVPROC>(loader.Invoke("glVertexP4uiv"));
            _glTexCoordP1ui = GetDelegateForFunctionPointer<PFNGLTEXCOORDP1UIPROC>(loader.Invoke("glTexCoordP1ui"));
            _glTexCoordP1uiv = GetDelegateForFunctionPointer<PFNGLTEXCOORDP1UIVPROC>(loader.Invoke("glTexCoordP1uiv"));
            _glTexCoordP2ui = GetDelegateForFunctionPointer<PFNGLTEXCOORDP2UIPROC>(loader.Invoke("glTexCoordP2ui"));
            _glTexCoordP2uiv = GetDelegateForFunctionPointer<PFNGLTEXCOORDP2UIVPROC>(loader.Invoke("glTexCoordP2uiv"));
            _glTexCoordP3ui = GetDelegateForFunctionPointer<PFNGLTEXCOORDP3UIPROC>(loader.Invoke("glTexCoordP3ui"));
            _glTexCoordP3uiv = GetDelegateForFunctionPointer<PFNGLTEXCOORDP3UIVPROC>(loader.Invoke("glTexCoordP3uiv"));
            _glTexCoordP4ui = GetDelegateForFunctionPointer<PFNGLTEXCOORDP4UIPROC>(loader.Invoke("glTexCoordP4ui"));
            _glTexCoordP4uiv = GetDelegateForFunctionPointer<PFNGLTEXCOORDP4UIVPROC>(loader.Invoke("glTexCoordP4uiv"));
            _glMultiTexCoordP1ui = GetDelegateForFunctionPointer<PFNGLMULTITEXCOORDP1UIPROC>(loader.Invoke("glMultiTexCoordP1ui"));
            _glMultiTexCoordP1uiv = GetDelegateForFunctionPointer<PFNGLMULTITEXCOORDP1UIVPROC>(loader.Invoke("glMultiTexCoordP1uiv"));
            _glMultiTexCoordP2ui = GetDelegateForFunctionPointer<PFNGLMULTITEXCOORDP2UIPROC>(loader.Invoke("glMultiTexCoordP2ui"));
            _glMultiTexCoordP2uiv = GetDelegateForFunctionPointer<PFNGLMULTITEXCOORDP2UIVPROC>(loader.Invoke("glMultiTexCoordP2uiv"));
            _glMultiTexCoordP3ui = GetDelegateForFunctionPointer<PFNGLMULTITEXCOORDP3UIPROC>(loader.Invoke("glMultiTexCoordP3ui"));
            _glMultiTexCoordP3uiv = GetDelegateForFunctionPointer<PFNGLMULTITEXCOORDP3UIVPROC>(loader.Invoke("glMultiTexCoordP3uiv"));
            _glMultiTexCoordP4ui = GetDelegateForFunctionPointer<PFNGLMULTITEXCOORDP4UIPROC>(loader.Invoke("glMultiTexCoordP4ui"));
            _glMultiTexCoordP4uiv = GetDelegateForFunctionPointer<PFNGLMULTITEXCOORDP4UIVPROC>(loader.Invoke("glMultiTexCoordP4uiv"));
            _glNormalP3ui = GetDelegateForFunctionPointer<PFNGLNORMALP3UIPROC>(loader.Invoke("glNormalP3ui"));
            _glNormalP3uiv = GetDelegateForFunctionPointer<PFNGLNORMALP3UIVPROC>(loader.Invoke("glNormalP3uiv"));
            _glColorP3ui = GetDelegateForFunctionPointer<PFNGLCOLORP3UIPROC>(loader.Invoke("glColorP3ui"));
            _glColorP3uiv = GetDelegateForFunctionPointer<PFNGLCOLORP3UIVPROC>(loader.Invoke("glColorP3uiv"));
            _glColorP4ui = GetDelegateForFunctionPointer<PFNGLCOLORP4UIPROC>(loader.Invoke("glColorP4ui"));
            _glColorP4uiv = GetDelegateForFunctionPointer<PFNGLCOLORP4UIVPROC>(loader.Invoke("glColorP4uiv"));
            _glSecondaryColorP3ui = GetDelegateForFunctionPointer<PFNGLSECONDARYCOLORP3UIPROC>(loader.Invoke("glSecondaryColorP3ui"));
            _glSecondaryColorP3uiv = GetDelegateForFunctionPointer<PFNGLSECONDARYCOLORP3UIVPROC>(loader.Invoke("glSecondaryColorP3uiv"));
        }
    }
}