namespace AssetStudio
{
    public enum GraphicsFormat
    {
        /// <summary>
        /// The format is not specified.
        /// </summary>
        None,
        /// <summary>
        /// A one-component, 8-bit unsigned normalized format that has a single 8-bit R component stored with sRGB nonlinear encoding.
        /// </summary>
        R8_SRGB,
        /// <summary>
        /// A two-component, 16-bit unsigned normalized format that has an 8-bit R component stored with sRGB nonlinear encoding in byte 0, and an 8-bit G component stored with sRGB nonlinear encoding in byte 1.
        /// </summary>
        R8G8_SRGB,
        /// <summary>
        /// A three-component, 24-bit unsigned normalized format that has an 8-bit R component stored with sRGB nonlinear encoding in byte 0, an 8-bit G component stored with sRGB nonlinear encoding in byte 1, and an 8-bit B component stored with sRGB nonlinear encoding in byte 2.
        /// </summary>
        R8G8B8_SRGB,
        /// <summary>
        /// A four-component, 32-bit unsigned normalized format that has an 8-bit R component stored with sRGB nonlinear encoding in byte 0, an 8-bit G component stored with sRGB nonlinear encoding in byte 1, an 8-bit B component stored with sRGB nonlinear encoding in byte 2, and an 8-bit A component in byte 3.
        /// </summary>
        R8G8B8A8_SRGB,
        /// <summary>
        /// A one-component, 8-bit unsigned normalized format that has a single 8-bit R component.
        /// </summary>
        R8_UNorm,
        /// <summary>
        /// A two-component, 16-bit unsigned normalized format that has an 8-bit R component stored with sRGB nonlinear encoding in byte 0, and an 8-bit G component stored with sRGB nonlinear encoding in byte 1.
        /// </summary>
        R8G8_UNorm,
        /// <summary>
        /// A three-component, 24-bit unsigned normalized format that has an 8-bit R component in byte 0, an 8-bit G component in byte 1, and an 8-bit B component in byte 2.
        /// </summary>
        R8G8B8_UNorm,
        /// <summary>
        /// A four-component, 32-bit unsigned normalized format that has an 8-bit R component in byte 0, an 8-bit G component in byte 1, an 8-bit B component in byte 2, and an 8-bit A component in byte 3.
        /// </summary>
        R8G8B8A8_UNorm,
        /// <summary>
        /// A one-component, 8-bit signed normalized format that has a single 8-bit R component.
        /// </summary>
        R8_SNorm,
        /// <summary>
        /// A two-component, 16-bit signed normalized format that has an 8-bit R component stored with sRGB nonlinear encoding in byte 0, and an 8-bit G component stored with sRGB nonlinear encoding in byte 1.
        /// </summary>
        R8G8_SNorm,
        /// <summary>
        /// A three-component, 24-bit signed normalized format that has an 8-bit R component in byte 0, an 8-bit G component in byte 1, and an 8-bit B component in byte 2.
        /// </summary>
        R8G8B8_SNorm,
        /// <summary>
        /// A four-component, 32-bit signed normalized format that has an 8-bit R component in byte 0, an 8-bit G component in byte 1, an 8-bit B component in byte 2, and an 8-bit A component in byte 3.
        /// </summary>
        R8G8B8A8_SNorm,
        /// <summary>
        /// A one-component, 8-bit unsigned integer format that has a single 8-bit R component.
        /// </summary>
        R8_UInt,
        /// <summary>
        /// A two-component, 16-bit unsigned integer format that has an 8-bit R component in byte 0, and an 8-bit G component in byte 1.
        /// </summary>
        R8G8_UInt,
        /// <summary>
        /// A three-component, 24-bit unsigned integer format that has an 8-bit R component in byte 0, an 8-bit G component in byte 1, and an 8-bit B component in byte 2.
        /// </summary>
        R8G8B8_UInt,
        /// <summary>
        /// A four-component, 32-bit unsigned integer format that has an 8-bit R component in byte 0, an 8-bit G component in byte 1, an 8-bit B component in byte 2, and an 8-bit A component in byte 3.
        /// </summary>
        R8G8B8A8_UInt,
        /// <summary>
        /// A one-component, 8-bit signed integer format that has a single 8-bit R component.
        /// </summary>
        R8_SInt,
        /// <summary>
        /// A two-component, 16-bit signed integer format that has an 8-bit R component in byte 0, and an 8-bit G component in byte 1.
        /// </summary>
        R8G8_SInt,
        /// <summary>
        /// A three-component, 24-bit signed integer format that has an 8-bit R component in byte 0, an 8-bit G component in byte 1, and an 8-bit B component in byte 2.
        /// </summary>
        R8G8B8_SInt,
        /// <summary>
        /// A four-component, 32-bit signed integer format that has an 8-bit R component in byte 0, an 8-bit G component in byte 1, an 8-bit B component in byte 2, and an 8-bit A component in byte 3.
        /// </summary>
        R8G8B8A8_SInt,
        /// <summary>
        /// A one-component, 16-bit unsigned normalized format that has a single 16-bit R component.
        /// </summary>
        R16_UNorm,
        /// <summary>
        /// A two-component, 32-bit unsigned normalized format that has a 16-bit R component in bytes 0..1, and a 16-bit G component in bytes 2..3.
        /// </summary>
        R16G16_UNorm,
        /// <summary>
        /// A three-component, 48-bit unsigned normalized format that has a 16-bit R component in bytes 0..1, a 16-bit G component in bytes 2..3, and a 16-bit B component in bytes 4..5.
        /// </summary>
        R16G16B16_UNorm,
        /// <summary>
        /// A four-component, 64-bit unsigned normalized format that has a 16-bit R component in bytes 0..1, a 16-bit G component in bytes 2..3, a 16-bit B component in bytes 4..5, and a 16-bit A component in bytes 6..7.
        /// </summary>
        R16G16B16A16_UNorm,
        /// <summary>
        /// A one-component, 16-bit signed normalized format that has a single 16-bit R component.
        /// </summary>
        R16_SNorm,
        /// <summary>
        /// A two-component, 32-bit signed normalized format that has a 16-bit R component in bytes 0..1, and a 16-bit G component in bytes 2..3.
        /// </summary>
        R16G16_SNorm,
        /// <summary>
        /// A three-component, 48-bit signed normalized format that has a 16-bit R component in bytes 0..1, a 16-bit G component in bytes 2..3, and a 16-bit B component in bytes 4..5.
        /// </summary>
        R16G16B16_SNorm,
        /// <summary>
        /// A four-component, 64-bit signed normalized format that has a 16-bit R component in bytes 0..1, a 16-bit G component in bytes 2..3, a 16-bit B component in bytes 4..5, and a 16-bit A component in bytes 6..7.
        /// </summary>
        R16G16B16A16_SNorm,
        /// <summary>
        /// A one-component, 16-bit unsigned integer format that has a single 16-bit R component.
        /// </summary>
        R16_UInt,
        /// <summary>
        /// A two-component, 32-bit unsigned integer format that has a 16-bit R component in bytes 0..1, and a 16-bit G component in bytes 2..3.
        /// </summary>
        R16G16_UInt,
        /// <summary>
        /// A three-component, 48-bit unsigned integer format that has a 16-bit R component in bytes 0..1, a 16-bit G component in bytes 2..3, and a 16-bit B component in bytes 4..5.
        /// </summary>
        R16G16B16_UInt,
        /// <summary>
        /// A four-component, 64-bit unsigned integer format that has a 16-bit R component in bytes 0..1, a 16-bit G component in bytes 2..3, a 16-bit B component in bytes 4..5, and a 16-bit A component in bytes 6..7.
        /// </summary>
        R16G16B16A16_UInt,
        /// <summary>
        /// A one-component, 16-bit signed integer format that has a single 16-bit R component.
        /// </summary>
        R16_SInt,
        /// <summary>
        /// A two-component, 32-bit signed integer format that has a 16-bit R component in bytes 0..1, and a 16-bit G component in bytes 2..3.
        /// </summary>
        R16G16_SInt,
        /// <summary>
        /// A three-component, 48-bit signed integer format that has a 16-bit R component in bytes 0..1, a 16-bit G component in bytes 2..3, and a 16-bit B component in bytes 4..5.
        /// </summary>
        R16G16B16_SInt,
        /// <summary>
        /// A four-component, 64-bit signed integer format that has a 16-bit R component in bytes 0..1, a 16-bit G component in bytes 2..3, a 16-bit B component in bytes 4..5, and a 16-bit A component in bytes 6..7.
        /// </summary>
        R16G16B16A16_SInt,
        /// <summary>
        /// A one-component, 32-bit unsigned integer format that has a single 32-bit R component.
        /// </summary>
        R32_UInt,
        /// <summary>
        /// A two-component, 64-bit unsigned integer format that has a 32-bit R component in bytes 0..3, and a 32-bit G component in bytes 4..7.
        /// </summary>
        R32G32_UInt,
        /// <summary>
        /// A three-component, 96-bit unsigned integer format that has a 32-bit R component in bytes 0..3, a 32-bit G component in bytes 4..7, and a 32-bit B component in bytes 8..11.
        /// </summary>
        R32G32B32_UInt,
        /// <summary>
        /// A four-component, 128-bit unsigned integer format that has a 32-bit R component in bytes 0..3, a 32-bit G component in bytes 4..7, a 32-bit B component in bytes 8..11, and a 32-bit A component in bytes 12..15.
        /// </summary>
        R32G32B32A32_UInt,
        /// <summary>
        /// A one-component, 32-bit signed integer format that has a single 32-bit R component.
        /// </summary>
        R32_SInt,
        /// <summary>
        /// A two-component, 64-bit signed integer format that has a 32-bit R component in bytes 0..3, and a 32-bit G component in bytes 4..7.
        /// </summary>
        R32G32_SInt,
        /// <summary>
        /// A three-component, 96-bit signed integer format that has a 32-bit R component in bytes 0..3, a 32-bit G component in bytes 4..7, and a 32-bit B component in bytes 8..11.
        /// </summary>
        R32G32B32_SInt,
        /// <summary>
        /// A four-component, 128-bit signed integer format that has a 32-bit R component in bytes 0..3, a 32-bit G component in bytes 4..7, a 32-bit B component in bytes 8..11, and a 32-bit A component in bytes 12..15.
        /// </summary>
        R32G32B32A32_SInt,
        /// <summary>
        /// A one-component, 16-bit signed floating-point format that has a single 16-bit R component.
        /// </summary>
        R16_SFloat,
        /// <summary>
        /// A two-component, 32-bit signed floating-point format that has a 16-bit R component in bytes 0..1, and a 16-bit G component in bytes 2..3.
        /// </summary>
        R16G16_SFloat,
        /// <summary>
        /// A three-component, 48-bit signed floating-point format that has a 16-bit R component in bytes 0..1, a 16-bit G component in bytes 2..3, and a 16-bit B component in bytes 4..5.
        /// </summary>
        R16G16B16_SFloat,
        /// <summary>
        /// A four-component, 64-bit signed floating-point format that has a 16-bit R component in bytes 0..1, a 16-bit G component in bytes 2..3, a 16-bit B component in bytes 4..5, and a 16-bit A component in bytes 6..7.
        /// </summary>
        R16G16B16A16_SFloat,
        /// <summary>
        /// A one-component, 32-bit signed floating-point format that has a single 32-bit R component.
        /// </summary>
        R32_SFloat,
        /// <summary>
        /// A two-component, 64-bit signed floating-point format that has a 32-bit R component in bytes 0..3, and a 32-bit G component in bytes 4..7.
        /// </summary>
        R32G32_SFloat,
        /// <summary>
        /// A three-component, 96-bit signed floating-point format that has a 32-bit R component in bytes 0..3, a 32-bit G component in bytes 4..7, and a 32-bit B component in bytes 8..11.
        /// </summary>
        R32G32B32_SFloat,
        /// <summary>
        /// A four-component, 128-bit signed floating-point format that has a 32-bit R component in bytes 0..3, a 32-bit G component in bytes 4..7, a 32-bit B component in bytes 8..11, and a 32-bit A component in bytes 12..15.
        /// </summary>
        R32G32B32A32_SFloat,
        /// <summary>
        /// A three-component, 24-bit unsigned normalized format that has an 8-bit B component stored with sRGB nonlinear encoding in byte 0, an 8-bit G component stored with sRGB nonlinear encoding in byte 1, and an 8-bit R component stored with sRGB nonlinear encoding in byte 2.
        /// </summary>
        B8G8R8_SRGB = 56,
        /// <summary>
        /// A four-component, 32-bit unsigned normalized format that has an 8-bit B component stored with sRGB nonlinear encoding in byte 0, an 8-bit G component stored with sRGB nonlinear encoding in byte 1, an 8-bit R component stored with sRGB nonlinear encoding in byte 2, and an 8-bit A component in byte 3.
        /// </summary>
        B8G8R8A8_SRGB,
        /// <summary>
        /// A three-component, 24-bit unsigned normalized format that has an 8-bit B component in byte 0, an 8-bit G component in byte 1, and an 8-bit R component in byte 2.
        /// </summary>
        B8G8R8_UNorm,
        /// <summary>
        /// A four-component, 32-bit unsigned normalized format that has an 8-bit B component in byte 0, an 8-bit G component in byte 1, an 8-bit R component in byte 2, and an 8-bit A component in byte 3.
        /// </summary>
        B8G8R8A8_UNorm,
        /// <summary>
        /// A three-component, 24-bit signed normalized format that has an 8-bit B component in byte 0, an 8-bit G component in byte 1, and an 8-bit R component in byte 2.
        /// </summary>
        B8G8R8_SNorm,
        /// <summary>
        /// A four-component, 32-bit signed normalized format that has an 8-bit B component in byte 0, an 8-bit G component in byte 1, an 8-bit R component in byte 2, and an 8-bit A component in byte 3.
        /// </summary>
        B8G8R8A8_SNorm,
        /// <summary>
        /// A three-component, 24-bit unsigned integer format that has an 8-bit B component in byte 0, an 8-bit G component in byte 1, and an 8-bit R component in byte 2
        /// </summary>
        B8G8R8_UInt,
        /// <summary>
        /// A four-component, 32-bit unsigned integer format that has an 8-bit B component in byte 0, an 8-bit G component in byte 1, an 8-bit R component in byte 2, and an 8-bit A component in byte 3.
        /// </summary>
        B8G8R8A8_UInt,
        /// <summary>
        /// A three-component, 24-bit signed integer format that has an 8-bit B component in byte 0, an 8-bit G component in byte 1, and an 8-bit R component in byte 2.
        /// </summary>
        B8G8R8_SInt,
        /// <summary>
        /// A four-component, 32-bit signed integer format that has an 8-bit B component in byte 0, an 8-bit G component in byte 1, an 8-bit R component in byte 2, and an 8-bit A component in byte 3.
        /// </summary>
        B8G8R8A8_SInt,
        /// <summary>
        /// A four-component, 16-bit packed unsigned normalized format that has a 4-bit R component in bits 12..15, a 4-bit G component in bits 8..11, a 4-bit B component in bits 4..7, and a 4-bit A component in bits 0..3.
        /// </summary>
        R4G4B4A4_UNormPack16,
        /// <summary>
        /// A four-component, 16-bit packed unsigned normalized format that has a 4-bit B component in bits 12..15, a 4-bit G component in bits 8..11, a 4-bit R component in bits 4..7, and a 4-bit A component in bits 0..3.
        /// </summary>
        B4G4R4A4_UNormPack16,
        /// <summary>
        /// A three-component, 16-bit packed unsigned normalized format that has a 5-bit R component in bits 11..15, a 6-bit G component in bits 5..10, and a 5-bit B component in bits 0..4.
        /// </summary>
        R5G6B5_UNormPack16,
        /// <summary>
        /// A three-component, 16-bit packed unsigned normalized format that has a 5-bit B component in bits 11..15, a 6-bit G component in bits 5..10, and a 5-bit R component in bits 0..4.
        /// </summary>
        B5G6R5_UNormPack16,
        /// <summary>
        /// A four-component, 16-bit packed unsigned normalized format that has a 5-bit R component in bits 11..15, a 5-bit G component in bits 6..10, a 5-bit B component in bits 1..5, and a 1-bit A component in bit 0.
        /// </summary>
        R5G5B5A1_UNormPack16,
        /// <summary>
        /// A four-component, 16-bit packed unsigned normalized format that has a 5-bit B component in bits 11..15, a 5-bit G component in bits 6..10, a 5-bit R component in bits 1..5, and a 1-bit A component in bit 0.
        /// </summary>
        B5G5R5A1_UNormPack16,
        /// <summary>
        /// A four-component, 16-bit packed unsigned normalized format that has a 1-bit A component in bit 15, a 5-bit R component in bits 10..14, a 5-bit G component in bits 5..9, and a 5-bit B component in bits 0..4.
        /// </summary>
        A1R5G5B5_UNormPack16,
        /// <summary>
        /// A three-component, 32-bit packed unsigned floating-point format that has a 5-bit shared exponent in bits 27..31, a 9-bit B component mantissa in bits 18..26, a 9-bit G component mantissa in bits 9..17, and a 9-bit R component mantissa in bits 0..8.
        /// </summary>
        E5B9G9R9_UFloatPack32,
        /// <summary>
        /// A three-component, 32-bit packed unsigned floating-point format that has a 10-bit B component in bits 22..31, an 11-bit G component in bits 11..21, an 11-bit R component in bits 0..10. 
        /// </summary>
        B10G11R11_UFloatPack32,
        /// <summary>
        /// A four-component, 32-bit packed unsigned normalized format that has a 2-bit A component in bits 30..31, a 10-bit B component in bits 20..29, a 10-bit G component in bits 10..19, and a 10-bit R component in bits 0..9.
        /// </summary>
        A2B10G10R10_UNormPack32,
        /// <summary>
        /// A four-component, 32-bit packed unsigned integer format that has a 2-bit A component in bits 30..31, a 10-bit B component in bits 20..29, a 10-bit G component in bits 10..19, and a 10-bit R component in bits 0..9.
        /// </summary>
        A2B10G10R10_UIntPack32,
        /// <summary>
        /// A four-component, 32-bit packed signed integer format that has a 2-bit A component in bits 30..31, a 10-bit B component in bits 20..29, a 10-bit G component in bits 10..19, and a 10-bit R component in bits 0..9.
        /// </summary>
        A2B10G10R10_SIntPack32,
        /// <summary>
        /// A four-component, 32-bit packed unsigned normalized format that has a 2-bit A component in bits 30..31, a 10-bit R component in bits 20..29, a 10-bit G component in bits 10..19, and a 10-bit B component in bits 0..9.
        /// </summary>
        A2R10G10B10_UNormPack32,
        /// <summary>
        /// A four-component, 32-bit packed unsigned integer format that has a 2-bit A component in bits 30..31, a 10-bit R component in bits 20..29, a 10-bit G component in bits 10..19, and a 10-bit B component in bits 0..9.
        /// </summary>
        A2R10G10B10_UIntPack32,
        /// <summary>
        /// A four-component, 32-bit packed signed integer format that has a 2-bit A component in bits 30..31, a 10-bit R component in bits 20..29, a 10-bit G component in bits 10..19, and a 10-bit B component in bits 0..9.
        /// </summary>
        A2R10G10B10_SIntPack32,
        /// <summary>
        /// A four-component, 32-bit packed unsigned normalized format that has a 2-bit A component in bits 30..31, a 10-bit R component in bits 20..29, a 10-bit G component in bits 10..19, and a 10-bit B component in bits 0..9. The components are gamma encoded and their values range from -0.5271 to 1.66894. The alpha component is clamped to either 0.0 or 1.0 on sampling, rendering, and writing operations.
        /// </summary>
        A2R10G10B10_XRSRGBPack32,
        /// <summary>
        /// A four-component, 32-bit packed unsigned normalized format that has a 2-bit A component in bits 30..31, a 10-bit R component in bits 20..29, a 10-bit G component in bits 10..19, and a 10-bit B component in bits 0..9. The components are linearly encoded and their values range from -0.752941 to 1.25098 (pre-expansion). The alpha component is clamped to either 0.0 or 1.0 on sampling, rendering, and writing operations.
        /// </summary>
        A2R10G10B10_XRUNormPack32,
        /// <summary>
        /// A four-component, 32-bit packed unsigned normalized format that has a 10-bit R component in bits 20..29, a 10-bit G component in bits 10..19, and a 10-bit B component in bits 0..9. The components are gamma encoded and their values range from -0.5271 to 1.66894. The alpha component is clamped to either 0.0 or 1.0 on sampling, rendering, and writing operations.
        /// </summary>
        R10G10B10_XRSRGBPack32,
        /// <summary>
        /// A four-component, 32-bit packed unsigned normalized format that has a 10-bit R component in bits 20..29, a 10-bit G component in bits 10..19, and a 10-bit B component in bits 0..9. The components are linearly encoded and their values range from -0.752941 to 1.25098 (pre-expansion).
        /// </summary>
        R10G10B10_XRUNormPack32,
        /// <summary>
        /// A four-component, 64-bit packed unsigned normalized format that has a 10-bit A component in bits 30..39, a 10-bit R component in bits 20..29, a 10-bit G component in bits 10..19, and a 10-bit B component in bits 0..9. The components are gamma encoded and their values range from -0.5271 to 1.66894. The alpha component is clamped to either 0.0 or 1.0 on sampling, rendering, and writing operations.
        /// </summary>
        A10R10G10B10_XRSRGBPack32,
        /// <summary>
        /// A four-component, 64-bit packed unsigned normalized format that has a 10-bit A component in bits 30..39, a 10-bit R component in bits 20..29, a 10-bit G component in bits 10..19, and a 10-bit B component in bits 0..9. The components are linearly encoded and their values range from -0.752941 to 1.25098 (pre-expansion). The alpha component is clamped to either 0.0 or 1.0 on sampling, rendering, and writing operations.
        /// </summary>
        A10R10G10B10_XRUNormPack32,
        /// <summary>
        /// A one-component, 16-bit unsigned normalized format that has a single 16-bit depth component.
        /// </summary>
        D16_UNorm = 90,
        /// <summary>
        /// A two-component, 32-bit format that has 24 unsigned normalized bits in the depth component and, optionally: 8 bits that are unused.
        /// </summary>
        D24_UNorm,
        /// <summary>
        /// A two-component, 32-bit packed format that has 8 unsigned integer bits in the stencil component, and 24 unsigned normalized bits in the depth component.
        /// </summary>
        D24_UNorm_S8_UInt,
        /// <summary>
        /// A one-component, 32-bit signed floating-point format that has 32-bits in the depth component.
        /// </summary>
        D32_SFloat,
        /// <summary>
        /// A two-component format that has 32 signed float bits in the depth component and 8 unsigned integer bits in the stencil component. There are optionally: 24-bits that are unused.
        /// </summary>
        D32_SFloat_S8_UInt,
        /// <summary>
        /// A one-component, 8-bit unsigned integer format that has 8-bits in the stencil component.
        /// </summary>
        S8_UInt,
        /// <summary>
        /// A three-component, block-compressed format (also known as BC1). Each 64-bit compressed texel block encodes a 4×4 rectangle of unsigned normalized RGB texel data with sRGB nonlinear encoding. This format has a 1 bit alpha channel.
        /// </summary>
        RGBA_DXT1_SRGB,
        /// <summary>
        /// A three-component, block-compressed format (also known as BC1). Each 64-bit compressed texel block encodes a 4×4 rectangle of unsigned normalized RGB texel data. This format has a 1 bit alpha channel.
        /// </summary>
        RGBA_DXT1_UNorm,
        /// <summary>
        /// A four-component, block-compressed format (also known as BC2) where each 128-bit compressed texel block encodes a 4×4 rectangle of unsigned normalized RGBA texel data with the first 64 bits encoding alpha values followed by 64 bits encoding RGB values with sRGB nonlinear encoding.
        /// </summary>
        RGBA_DXT3_SRGB,
        /// <summary>
        /// A four-component, block-compressed format (also known as BC2) where each 128-bit compressed texel block encodes a 4×4 rectangle of unsigned normalized RGBA texel data with the first 64 bits encoding alpha values followed by 64 bits encoding RGB values.
        /// </summary>
        RGBA_DXT3_UNorm,
        /// <summary>
        /// A four-component, block-compressed format (also known as BC3) where each 128-bit compressed texel block encodes a 4×4 rectangle of unsigned normalized RGBA texel data with the first 64 bits encoding alpha values followed by 64 bits encoding RGB values with sRGB nonlinear encoding.
        /// </summary>
        RGBA_DXT5_SRGB,
        /// <summary>
        /// A four-component, block-compressed format (also known as BC3) where each 128-bit compressed texel block encodes a 4×4 rectangle of unsigned normalized RGBA texel data with the first 64 bits encoding alpha values followed by 64 bits encoding RGB values.
        /// </summary>
        RGBA_DXT5_UNorm,
        /// <summary>
        /// A one-component, block-compressed format where each 64-bit compressed texel block encodes a 4×4 rectangle of unsigned normalized red texel data.
        /// </summary>
        R_BC4_UNorm,
        /// <summary>
        /// A one-component, block-compressed format where each 64-bit compressed texel block encodes a 4×4 rectangle of signed normalized red texel data.
        /// </summary>
        R_BC4_SNorm,
        /// <summary>
        /// A two-component, block-compressed format where each 128-bit compressed texel block encodes a 4×4 rectangle of unsigned normalized RG texel data with the first 64 bits encoding red values followed by 64 bits encoding green values.
        /// </summary>
        RG_BC5_UNorm,
        /// <summary>
        /// A two-component, block-compressed format where each 128-bit compressed texel block encodes a 4×4 rectangle of signed normalized RG texel data with the first 64 bits encoding red values followed by 64 bits encoding green values.
        /// </summary>
        RG_BC5_SNorm,
        /// <summary>
        /// A three-component, block-compressed format where each 128-bit compressed texel block encodes a 4×4 rectangle of unsigned floating-point RGB texel data.
        /// </summary>
        RGB_BC6H_UFloat,
        /// <summary>
        /// A three-component, block-compressed format where each 128-bit compressed texel block encodes a 4×4 rectangle of signed floating-point RGB texel data.
        /// </summary>
        RGB_BC6H_SFloat,
        /// <summary>
        /// A four-component, block-compressed format where each 128-bit compressed texel block encodes a 4×4 rectangle of unsigned normalized RGBA texel data with sRGB nonlinear encoding applied to the RGB components.
        /// </summary>
        RGBA_BC7_SRGB,
        /// <summary>
        /// A four-component, block-compressed format where each 128-bit compressed texel block encodes a 4×4 rectangle of unsigned normalized RGBA texel data.
        /// </summary>
        RGBA_BC7_UNorm,
        /// <summary>
        /// A three-component, PVRTC compressed format where each 64-bit compressed texel block encodes a 8×4 rectangle of unsigned normalized RGB texel data with sRGB nonlinear encoding. This format has no alpha and is considered opaque.
        /// </summary>
        RGB_PVRTC_2Bpp_SRGB,
        /// <summary>
        /// A three-component, PVRTC compressed format where each 64-bit compressed texel block encodes a 8×4 rectangle of unsigned normalized RGB texel data. This format has no alpha and is considered opaque.
        /// </summary>
        RGB_PVRTC_2Bpp_UNorm,
        /// <summary>
        /// A three-component, PVRTC compressed format where each 64-bit compressed texel block encodes a 4×4 rectangle of unsigned normalized RGB texel data with sRGB nonlinear encoding. This format has no alpha and is considered opaque.
        /// </summary>
        RGB_PVRTC_4Bpp_SRGB,
        /// <summary>
        /// A three-component, PVRTC compressed format where each 64-bit compressed texel block encodes a 4×4 rectangle of unsigned normalized RGB texel data. This format has no alpha and is considered opaque.
        /// </summary>
        RGB_PVRTC_4Bpp_UNorm,
        /// <summary>
        /// A four-component, PVRTC compressed format where each 64-bit compressed texel block encodes a 8×4 rectangle of unsigned normalized RGBA texel data with the first 32 bits encoding alpha values followed by 32 bits encoding RGB values with sRGB nonlinear encoding applied.
        /// </summary>
        RGBA_PVRTC_2Bpp_SRGB,
        /// <summary>
        /// A four-component, PVRTC compressed format where each 64-bit compressed texel block encodes a 8×4 rectangle of unsigned normalized RGBA texel data with the first 32 bits encoding alpha values followed by 32 bits encoding RGB values.
        /// </summary>
        RGBA_PVRTC_2Bpp_UNorm,
        /// <summary>
        /// A four-component, PVRTC compressed format where each 64-bit compressed texel block encodes a 4×4 rectangle of unsigned normalized RGBA texel data with the first 32 bits encoding alpha values followed by 32 bits encoding RGB values with sRGB nonlinear encoding applied.
        /// </summary>
        RGBA_PVRTC_4Bpp_SRGB,
        /// <summary>
        /// A four-component, PVRTC compressed format where each 64-bit compressed texel block encodes a 4×4 rectangle of unsigned normalized RGBA texel data with the first 32 bits encoding alpha values followed by 32 bits encoding RGB values.
        /// </summary>
        RGBA_PVRTC_4Bpp_UNorm,
        /// <summary>
        /// A three-component, ETC compressed format where each 64-bit compressed texel block encodes a 4×4 rectangle of unsigned normalized RGB texel data. This format has no alpha and is considered opaque.
        /// </summary>
        RGB_ETC_UNorm,
        /// <summary>
        /// A three-component, ETC2 compressed format where each 64-bit compressed texel block encodes a 4×4 rectangle of unsigned normalized RGB texel data with sRGB nonlinear encoding. This format has no alpha and is considered opaque.
        /// </summary>
        RGB_ETC2_SRGB,
        /// <summary>
        /// A three-component, ETC2 compressed format where each 64-bit compressed texel block encodes a 4×4 rectangle of unsigned normalized RGB texel data. This format has no alpha and is considered opaque.
        /// </summary>
        RGB_ETC2_UNorm,
        /// <summary>
        /// A four-component, ETC2 compressed format where each 64-bit compressed texel block encodes a 4×4 rectangle of unsigned normalized RGB texel data with sRGB nonlinear encoding, and provides 1 bit of alpha.
        /// </summary>
        RGB_A1_ETC2_SRGB,
        /// <summary>
        /// A four-component, ETC2 compressed format where each 64-bit compressed texel block encodes a 4×4 rectangle of unsigned normalized RGB texel data, and provides 1 bit of alpha.
        /// </summary>
        RGB_A1_ETC2_UNorm,
        /// <summary>
        /// A four-component, ETC2 compressed format where each 128-bit compressed texel block encodes a 4×4 rectangle of unsigned normalized RGBA texel data with the first 64 bits encoding alpha values followed by 64 bits encoding RGB values with sRGB nonlinear encoding applied.
        /// </summary>
        RGBA_ETC2_SRGB,
        /// <summary>
        /// A four-component, ETC2 compressed format where each 128-bit compressed texel block encodes a 4×4 rectangle of unsigned normalized RGBA texel data with the first 64 bits encoding alpha values followed by 64 bits encoding RGB values.
        /// </summary>
        RGBA_ETC2_UNorm,
        /// <summary>
        /// A one-component, ETC2 compressed format where each 64-bit compressed texel block encodes a 4×4 rectangle of unsigned normalized red texel data.
        /// </summary>
        R_EAC_UNorm,
        /// <summary>
        /// A one-component, ETC2 compressed format where each 64-bit compressed texel block encodes a 4×4 rectangle of signed normalized red texel data.
        /// </summary>
        R_EAC_SNorm,
        /// <summary>
        /// A two-component, ETC2 compressed format where each 128-bit compressed texel block encodes a 4×4 rectangle of unsigned normalized RG texel data with the first 64 bits encoding red values followed by 64 bits encoding green values.
        /// </summary>
        RG_EAC_UNorm,
        /// <summary>
        /// A two-component, ETC2 compressed format where each 128-bit compressed texel block encodes a 4×4 rectangle of signed normalized RG texel data with the first 64 bits encoding red values followed by 64 bits encoding green values.
        /// </summary>
        RG_EAC_SNorm,
        /// <summary>
        /// A four-component, ASTC compressed format where each 128-bit compressed texel block encodes a 4×4 rectangle of unsigned normalized RGBA texel data with sRGB nonlinear encoding applied to the RGB components.
        /// </summary>
        RGBA_ASTC4X4_SRGB,
        /// <summary>
        /// A four-component, ASTC compressed format where each 128-bit compressed texel block encodes a 4×4 rectangle of unsigned normalized RGBA texel data.
        /// </summary>
        RGBA_ASTC4X4_UNorm,
        /// <summary>
        /// A four-component, ASTC compressed format where each 128-bit compressed texel block encodes a 5×5 rectangle of unsigned normalized RGBA texel data with sRGB nonlinear encoding applied to the RGB components.
        /// </summary>
        RGBA_ASTC5X5_SRGB,
        /// <summary>
        /// A four-component, ASTC compressed format where each 128-bit compressed texel block encodes a 5×5 rectangle of unsigned normalized RGBA texel data.
        /// </summary>
        RGBA_ASTC5X5_UNorm,
        /// <summary>
        /// A four-component, ASTC compressed format where each 128-bit compressed texel block encodes a 6×6 rectangle of unsigned normalized RGBA texel data with sRGB nonlinear encoding applied to the RGB components.
        /// </summary>
        RGBA_ASTC6X6_SRGB,
        /// <summary>
        /// A four-component, ASTC compressed format where each 128-bit compressed texel block encodes a 6×6 rectangle of unsigned normalized RGBA texel data.
        /// </summary>
        RGBA_ASTC6X6_UNorm,
        /// <summary>
        /// A four-component, ASTC compressed format where each 128-bit compressed texel block encodes an 8×8 rectangle of unsigned normalized RGBA texel data with sRGB nonlinear encoding applied to the RGB components.
        /// </summary>
        RGBA_ASTC8X8_SRGB,
        /// <summary>
        /// A four-component, ASTC compressed format where each 128-bit compressed texel block encodes an 8×8 rectangle of unsigned normalized RGBA texel data.
        /// </summary>
        RGBA_ASTC8X8_UNorm,
        /// <summary>
        /// A four-component, ASTC compressed format where each 128-bit compressed texel block encodes a 10×10 rectangle of unsigned normalized RGBA texel data with sRGB nonlinear encoding applied to the RGB components.
        /// </summary>
        RGBA_ASTC10X10_SRGB,
        /// <summary>
        /// A four-component, ASTC compressed format where each 128-bit compressed texel block encodes a 10×10 rectangle of unsigned normalized RGBA texel data.
        /// </summary>
        RGBA_ASTC10X10_UNorm,
        /// <summary>
        /// A four-component, ASTC compressed format where each 128-bit compressed texel block encodes a 12×12 rectangle of unsigned normalized RGBA texel data with sRGB nonlinear encoding applied to the RGB components.
        /// </summary>
        RGBA_ASTC12X12_SRGB,
        /// <summary>
        /// A four-component, ASTC compressed format where each 128-bit compressed texel block encodes a 12×12 rectangle of unsigned normalized RGBA texel data.
        /// </summary>
        RGBA_ASTC12X12_UNorm,
        /// <summary>
        /// YUV 4:2:2 Video resource format.
        /// </summary>
        YUV2,
        /// <summary>
        /// GraphicsFormat.YUV2.
        /// </summary>
        VideoAuto = 144,
        /// <summary>
        /// A four-component, ASTC compressed format where each 128-bit compressed texel block encodes a 4×4 rectangle of float RGBA texel data.
        /// </summary>
        RGBA_ASTC4X4_UFloat,
        /// <summary>
        /// A four-component, ASTC compressed format where each 128-bit compressed texel block encodes a 5×5 rectangle of float RGBA texel data.
        /// </summary>
        RGBA_ASTC5X5_UFloat,
        /// <summary>
        /// A four-component, ASTC compressed format where each 128-bit compressed texel block encodes a 6×6 rectangle of float RGBA texel data.
        /// </summary>
        RGBA_ASTC6X6_UFloat,
        /// <summary>
        /// A four-component, ASTC compressed format where each 128-bit compressed texel block encodes an 8×8 rectangle of float RGBA texel data.
        /// </summary>
        RGBA_ASTC8X8_UFloat,
        /// <summary>
        /// A four-component, ASTC compressed format where each 128-bit compressed texel block encodes a 10×10 rectangle of float RGBA texel data.
        /// </summary>
        RGBA_ASTC10X10_UFloat,
        /// <summary>
        /// A four-component, ASTC compressed format where each 128-bit compressed texel block encodes a 12×12 rectangle of float RGBA texel data.
        /// </summary>
        RGBA_ASTC12X12_UFloat,
        /// <summary>
        /// A two-component, 24-bit format that has 16 unsigned normalized bits in the depth component and 8 unsigned integer bits in the stencil component. Most platforms do not support this format.
        /// </summary>
        D16_UNorm_S8_UInt,
    }

    public static class GraphicsFormatExtension
    {
        public static TextureFormat ToTextureFormat(this GraphicsFormat graphicsFormat)
        {
            switch (graphicsFormat)
            {
                case GraphicsFormat.R8_SRGB:
                case GraphicsFormat.R8_UInt:
                case GraphicsFormat.R8_UNorm:
                    return TextureFormat.R8;
                case GraphicsFormat.R8G8_SRGB:
                case GraphicsFormat.R8G8_UInt:
                case GraphicsFormat.R8G8_UNorm:
                    return TextureFormat.RG16;
                case GraphicsFormat.R8G8B8_SRGB:
                case GraphicsFormat.R8G8B8_UInt:
                case GraphicsFormat.R8G8B8_UNorm:
                    return TextureFormat.RGB24;
                case GraphicsFormat.R8G8B8A8_SRGB:
                case GraphicsFormat.R8G8B8A8_UInt:
                case GraphicsFormat.R8G8B8A8_UNorm:
                    return TextureFormat.RGBA32;
                case GraphicsFormat.R16_UInt:
                case GraphicsFormat.R16_UNorm:
                    return TextureFormat.R16;
                case GraphicsFormat.R16G16_UInt:
                case GraphicsFormat.R16G16_UNorm:
                    return TextureFormat.RG32;
                case GraphicsFormat.R16G16B16_UInt:
                case GraphicsFormat.R16G16B16_UNorm:
                    return TextureFormat.RGB48;
                case GraphicsFormat.R16G16B16A16_UInt:
                case GraphicsFormat.R16G16B16A16_UNorm:
                    return TextureFormat.RGBA64;
                case GraphicsFormat.R16_SFloat:
                    return TextureFormat.RHalf;
                case GraphicsFormat.R16G16_SFloat:
                    return TextureFormat.RGHalf;
                case GraphicsFormat.R16G16B16_SFloat: //?
                case GraphicsFormat.R16G16B16A16_SFloat:
                    return TextureFormat.RGBAHalf;
                case GraphicsFormat.R32_SFloat:
                    return TextureFormat.RFloat;
                case GraphicsFormat.R32G32_SFloat:
                    return TextureFormat.RGFloat;
                case GraphicsFormat.R32G32B32_SFloat: //?
                case GraphicsFormat.R32G32B32A32_SFloat:
                    return TextureFormat.RGBAFloat;
                case GraphicsFormat.B8G8R8A8_SRGB:
                case GraphicsFormat.B8G8R8A8_UInt:
                case GraphicsFormat.B8G8R8A8_UNorm:
                    return TextureFormat.BGRA32;
                case GraphicsFormat.E5B9G9R9_UFloatPack32:
                    return TextureFormat.RGB9e5Float;
                case GraphicsFormat.RGBA_DXT1_SRGB:
                case GraphicsFormat.RGBA_DXT1_UNorm:
                    return TextureFormat.DXT1;
                case GraphicsFormat.RGBA_DXT3_SRGB:
                case GraphicsFormat.RGBA_DXT3_UNorm:
                    return TextureFormat.DXT3;
                case GraphicsFormat.RGBA_DXT5_SRGB:
                case GraphicsFormat.RGBA_DXT5_UNorm:
                    return TextureFormat.DXT5;
                case GraphicsFormat.R_BC4_UNorm:
                    return TextureFormat.BC4;
                case GraphicsFormat.RG_BC5_UNorm:
                    return TextureFormat.BC5;
                case GraphicsFormat.RGB_BC6H_SFloat:
                case GraphicsFormat.RGB_BC6H_UFloat:
                    return TextureFormat.BC6H;
                case GraphicsFormat.RGBA_BC7_SRGB:
                case GraphicsFormat.RGBA_BC7_UNorm:
                    return TextureFormat.BC7;
                case GraphicsFormat.RGB_PVRTC_2Bpp_SRGB:
                case GraphicsFormat.RGB_PVRTC_2Bpp_UNorm:
                case GraphicsFormat.RGBA_PVRTC_2Bpp_SRGB:
                case GraphicsFormat.RGBA_PVRTC_2Bpp_UNorm:
                    return TextureFormat.PVRTC_RGBA2;
                case GraphicsFormat.RGB_PVRTC_4Bpp_SRGB:
                case GraphicsFormat.RGB_PVRTC_4Bpp_UNorm:
                case GraphicsFormat.RGBA_PVRTC_4Bpp_SRGB:
                case GraphicsFormat.RGBA_PVRTC_4Bpp_UNorm:
                    return TextureFormat.PVRTC_RGBA4;
                case GraphicsFormat.RGB_ETC_UNorm:
                    return TextureFormat.ETC_RGB4;
                case GraphicsFormat.RGB_ETC2_SRGB:
                case GraphicsFormat.RGB_ETC2_UNorm:
                    return TextureFormat.ETC2_RGB;
                case GraphicsFormat.RGB_A1_ETC2_SRGB:
                case GraphicsFormat.RGB_A1_ETC2_UNorm:
                    return TextureFormat.ETC2_RGBA1;
                case GraphicsFormat.RGBA_ETC2_SRGB:
                case GraphicsFormat.RGBA_ETC2_UNorm:
                    return TextureFormat.ETC2_RGBA8;
                case GraphicsFormat.R_EAC_UNorm:
                    return TextureFormat.EAC_R;
                case GraphicsFormat.R_EAC_SNorm:
                    return TextureFormat.EAC_R_SIGNED;
                case GraphicsFormat.RG_EAC_UNorm:
                    return TextureFormat.EAC_RG;
                case GraphicsFormat.RG_EAC_SNorm:
                    return TextureFormat.EAC_RG_SIGNED;
                case GraphicsFormat.RGBA_ASTC4X4_SRGB:
                case GraphicsFormat.RGBA_ASTC4X4_UNorm:
                    return TextureFormat.ASTC_RGBA_4x4;
                case GraphicsFormat.RGBA_ASTC5X5_SRGB:
                case GraphicsFormat.RGBA_ASTC5X5_UNorm:
                    return TextureFormat.ASTC_RGBA_5x5;
                case GraphicsFormat.RGBA_ASTC6X6_SRGB:
                case GraphicsFormat.RGBA_ASTC6X6_UNorm: 
                    return TextureFormat.ASTC_RGBA_6x6;
                case GraphicsFormat.RGBA_ASTC8X8_SRGB:
                case GraphicsFormat.RGBA_ASTC8X8_UNorm:
                    return TextureFormat.ASTC_RGBA_8x8;
                case GraphicsFormat.RGBA_ASTC10X10_SRGB:
                case GraphicsFormat.RGBA_ASTC10X10_UNorm:
                    return TextureFormat.ASTC_RGBA_10x10;
                case GraphicsFormat.RGBA_ASTC12X12_SRGB:
                case GraphicsFormat.RGBA_ASTC12X12_UNorm:
                    return TextureFormat.ASTC_RGBA_12x12;
                case GraphicsFormat.YUV2:
                case GraphicsFormat.VideoAuto:
                    return TextureFormat.YUY2;
                default:
                    return 0;
            }
        }
    }
}
