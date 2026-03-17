using System;

namespace Raylib_cs;

/// <summary>
/// Trace log level<br/>
/// NOTE: Organized by priority level
/// </summary>
public enum TraceLogLevel
{
    /// <summary>
    /// Display all logs
    /// </summary>
    LOG_ALL = 0,

    /// <summary>
    /// Trace logging, intended for internal use only
    /// </summary>
    LOG_TRACE,

    /// <summary>
    /// Debug logging, used for internal debugging, it should be disabled on release builds
    /// </summary>
    LOG_DEBUG,

    /// <summary>
    /// Info logging, used for program execution info
    /// </summary>
    LOG_INFO,

    /// <summary>
    /// Warning logging, used on recoverable failures
    /// </summary>
    LOG_WARNING,

    /// <summary>
    /// Error logging, used on unrecoverable failures
    /// </summary>
    LOG_ERROR,

    /// <summary>
    /// Fatal logging, used to abort program: exit(EXIT_FAILURE)
    /// </summary>
    LOG_FATAL,

    /// <summary>
    /// Disable logging
    /// </summary>
    LOG_NONE
}

/// <summary>
/// Color blending modes (pre-defined)
/// </summary>
public enum BlendMode
{
    /// <summary>
    /// Blend textures considering alpha (default)
    /// </summary>
    BLEND_ALPHA = 0,

    /// <summary>
    /// Blend textures adding colors
    /// </summary>
    BLEND_ADDITIVE,

    /// <summary>
    /// Blend textures multiplying colors
    /// </summary>
    BLEND_MULTIPLIED,

    /// <summary>
    /// Blend textures adding colors (alternative)
    /// </summary>
    BLEND_ADD_COLORS,

    /// <summary>
    /// Blend textures subtracting colors (alternative)
    /// </summary>
    BLEND_SUBTRACT_COLORS,

    /// <summary>
    /// Blend premultiplied textures considering alpha
    /// </summary>
    BLEND_ALPHA_PREMULTIPLY,

    /// <summary>
    /// Blend textures using custom src/dst factors (use rlSetBlendFactors())
    /// </summary>
    BLEND_CUSTOM,

    /// <summary>
    /// Blend textures using custom rgb/alpha separate src/dst factors (use rlSetBlendFactorsSeparate())
    /// </summary>
    BLEND_CUSTOM_SEPARATE
}
