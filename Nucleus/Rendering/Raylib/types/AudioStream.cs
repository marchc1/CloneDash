using unsafe AudioCallback = delegate* unmanaged<nint, int, void>;
using ma_format = int;
using ma_dither_mode = int;
using ma_data_converter_execution_path = int;
using ma_channel_mix_mode = int;
using ma_channel_conversion_path = int;
using System;
using System.Runtime.InteropServices;


namespace Raylib_cs;

// These MiniAudio structs are for the sake of having a valid AudioBuffer in terms of sizing.

[StructLayout(LayoutKind.Sequential)]
public unsafe struct ma_lpf
{
	public ma_format Format;
	public uint Channels;
	public uint SampleRate;
	public uint LPF1Count;
	public uint LPF2Count;
	public nint LPF1;
	public nint LPF2;
	public void* Heap;
	public int OwnsHeap;
}
[StructLayout(LayoutKind.Sequential)]
public unsafe struct ma_linear_resampler_config
{
	public ma_format Format;
	public uint Channels;
	public uint SampleRateIn;
	public uint SampleRateOut;
	public uint Order;
	public double NyquistFactor;
}
[StructLayout(LayoutKind.Sequential)]
public unsafe struct ma_linear_resampler
{
	public ma_linear_resampler_config Config;
	public uint InAdvanceInt;
	public uint InAdvanceFrac;
	public uint InTimeInt;
	public uint InTimeFrac;
	public nint X0;
	public nint X1;
	public ma_lpf LPF;

	public void* Heap;
	public int OwnsHeap;
}
[StructLayout(LayoutKind.Sequential)]
public unsafe struct ma_resampler
{
	public nint Backend;
	public nint BackendVTable;
	public nint BackendUserData;
	public ma_format Format;
	public uint Channels;
	public uint SampleRateIn;
	public uint SampleRateOut;
	public ma_linear_resampler Linear;

	public void* Heap;
	public int OwnsHeap;
}
[StructLayout(LayoutKind.Sequential)]
public unsafe struct ma_channel_converter
{
	public ma_format Format;
	public uint ChannelsIn;
	public uint ChannelsOut;
	public ma_channel_mix_mode MixingMode;
	public ma_channel_conversion_path ConversionPath;
	public nint ChannelMapIn;
	public nint ChannelMapOut;
	public nint ShuffleTable;
	public nint Weights;
	public void* Heap;
	public int OwnsHeap;
}
[StructLayout(LayoutKind.Sequential)]
public unsafe struct ma_data_converter
{
	public ma_format FormatIn;
	public ma_format FormatOut;
	public uint ChannelsIn;
	public uint ChannelsOut;
	public uint SampleRateIn;
	public uint SampleRateOut;
	public ma_dither_mode DitherMode;
	public ma_data_converter_execution_path ExecutionPath;
	public ma_channel_converter ChannelConverter;
	public ma_resampler Resampler;
	public bool HasPreFormatConversion;
	public bool HasPostFormatConversion;
	public bool HasChannelConverter;
	public bool HasResampler;
	public bool IsPassthrough;
	public bool OwnsHeap;
	public void* Heap;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct AudioProcessor
{
	public AudioCallback Process;
	public AudioProcessor* Next;
	public AudioProcessor* Prev;
}
[StructLayout(LayoutKind.Sequential)]
public unsafe struct AudioBuffer
{
	public ma_data_converter Converter;
	public AudioCallback AudioCallback;
	public nint Processor;

	public float Volume;
	public float Pitch;
	public float Pan;
	public bool Playing;
	public bool Paused;
	public bool Looping;
	public int Usage;
	public bool IsSubBufferProcessed_0;
	public bool IsSubBufferProcessed_1;
	public uint SizeInFrames;
	public uint FrameCursorPos;
	public uint FramesProcessed;
	public byte* Data;
	public AudioBuffer* Next;
	public AudioBuffer* Prev;
}

/// <summary>
/// Audio stream type<br/>
/// NOTE: Useful to create custom audio streams not bound to a specific file
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public partial struct AudioStream
{
	//TODO: convert
	/// <summary>
	/// Pointer to internal data(rAudioBuffer *) used by the audio system
	/// </summary>
	public unsafe AudioBuffer* Buffer;

	/// <summary>
	/// Pointer to internal data processor, useful for audio effects
	/// </summary>
	public IntPtr Processor;

	/// <summary>
	/// Frequency (samples per second)
	/// </summary>
	public uint SampleRate;

	/// <summary>
	/// Bit depth (bits per sample): 8, 16, 32 (24 not supported)
	/// </summary>
	public uint SampleSize;

	/// <summary>
	/// Number of channels (1-mono, 2-stereo)
	/// </summary>
	public uint Channels;
}
