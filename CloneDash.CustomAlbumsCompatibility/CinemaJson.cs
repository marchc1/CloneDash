using CloneDash.Common.Game;
using CloneDash.Game;
using FFMediaToolkit;
using FFMediaToolkit.Decoding;
using FFMediaToolkit.Graphics;
using FFmpeg.AutoGen;
using Newtonsoft.Json;
using Nucleus;
using Nucleus.Common.Graphics;
using SharpCompress.Compressors.Xz;
using SharpCompress.Readers.Tar;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;

namespace CloneDash.CustomAlbumsCompatibility;

public class CinemaJson
{
	[JsonProperty("file_name")] public string VideoName { get; set; }
	[JsonProperty("opacity")] public float Opacity { get; set; }
	[JsonProperty("difficulties")] public int[]? Difficulties { get; set; }
}

public class CinemaBackgroundGenerator : ITextureRegenerator
{
	static string SharedBuildUrl() {
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
			return RuntimeInformation.OSArchitecture == Architecture.Arm64
				? "https://github.com/BtbN/FFmpeg-Builds/releases/download/autobuild-2026-04-30-13-44/ffmpeg-n7.1.3-46-g957b06a788-winarm64-gpl-shared-7.1.zip"
				: "https://github.com/BtbN/FFmpeg-Builds/releases/download/autobuild-2026-04-30-13-44/ffmpeg-n7.1.3-46-g957b06a788-win64-gpl-shared-7.1.zip";
		}
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
			return RuntimeInformation.OSArchitecture == Architecture.Arm64
				? "https://github.com/BtbN/FFmpeg-Builds/releases/download/autobuild-2026-04-30-13-44/ffmpeg-n7.1.3-46-g957b06a788-linuxarm64-gpl-shared-7.1.tar.xz"
				: "https://github.com/BtbN/FFmpeg-Builds/releases/download/autobuild-2026-04-30-13-44/ffmpeg-n7.1.3-46-g957b06a788-linux64-gpl-shared-7.1.tar.xz";
		}
		if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
			throw new PlatformNotSupportedException(
				"No BtbN macOS build!!! Fixme!!!");
		}
		throw new PlatformNotSupportedException("Unsupported OS for FFmpeg auto-download.");
	}

	static bool HasAvcodec(string dir) =>
		Directory.Exists(dir) && Directory.GetFiles(dir, "*avcodec*").Length > 0;

	sealed class LoadProgress
	{
		public volatile string Message = "Preparing FFmpeg...";
		public volatile string? SubMessage;
		public volatile bool Done;
		public volatile Exception? Error;
	}

	static bool ffmpegReady = false;
	public static void EnsureLoaded() {
		if (ffmpegReady) return;

		string cache = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			"Clone Dash", "ffmpeg", "7");

		if (HasAvcodec(cache)) {
			FFmpegLoader.FFmpegPath = cache;
			ffmpegReady = true;
			return;
		}

		if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && TryFindSystemFFmpeg(out string sys)) {
			FFmpegLoader.FFmpegPath = sys;
			ffmpegReady = true;
			return;
		}

		var progress = new LoadProgress();

		var worker = new Thread(() => {
			try {
				DownloadSharedBuild(cache, progress);
			}
			catch (Exception ex) {
				progress.Error = ex;
			}
			finally {
				progress.Done = true;
			}
		}) {
			IsBackground = true,
			Name = "FFmpeg-Download"
		};
		worker.Start();

		while (!progress.Done) {
			Interlude.Spin(progress.Message, progress.SubMessage);
			Thread.Sleep(16);
		}

		if (progress.Error != null)
			throw new InvalidOperationException("Failed to load FFmpeg.", progress.Error);

		if (!HasAvcodec(cache))
			throw new InvalidOperationException("FFmpeg download completed but no avcodec library was found in " + cache + ".");

		FFmpegLoader.FFmpegPath = cache;
		ffmpegReady = true;
	}

	static bool TryFindSystemFFmpeg(out string dir) {
		foreach (var candidate in new[] { "/opt/homebrew/lib", "/usr/local/lib", "/opt/local/lib" }) {
			if (Directory.Exists(candidate) && Directory.GetFiles(candidate, "*avcodec*").Length > 0) {
				dir = candidate;
				return true;
			}
		}
		dir = "";
		return false;
	}

	static void DownloadSharedBuild(string destDir, LoadProgress progress) {
		Directory.CreateDirectory(destDir);
		string url = SharedBuildUrl();
		bool isTarXz = url.EndsWith(".tar.xz", StringComparison.OrdinalIgnoreCase);
		string tmp = Path.Combine(Path.GetTempPath(), isTarXz ? "ffmpeg-shared.tar.xz" : "ffmpeg-shared.zip");

		try {
			progress.Message = "Downloading FFmpeg...";
			progress.SubMessage = null;

			using (var http = new HttpClient { Timeout = Timeout.InfiniteTimeSpan }) {
				using var resp = http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult();
				resp.EnsureSuccessStatusCode();

				long? total = resp.Content.Headers.ContentLength;
				using var src = resp.Content.ReadAsStream();
				using var dst = File.Create(tmp);

				byte[] buffer = new byte[1 << 20];
				long read = 0;
				int n;
				while ((n = src.Read(buffer, 0, buffer.Length)) > 0) {
					dst.Write(buffer, 0, n);
					read += n;
					progress.SubMessage = total.HasValue
						? $"{read / (1024 * 1024)} / {total.Value / (1024 * 1024)} MiB"
						: $"{read / (1024 * 1024)} MiB";
				}
			}

			progress.Message = "Extracting FFmpeg...";
			progress.SubMessage = null;

			if (isTarXz)
				ExtractSharedLibsFromTarXz(tmp, destDir, progress);
			else
				ExtractDllsFromZip(tmp, destDir, progress);
		}
		finally {
			try { if (File.Exists(tmp)) File.Delete(tmp); } catch { /* best effort */ }
		}
	}

	static void ExtractDllsFromZip(string zipPath, string destDir, LoadProgress progress) {
		using var zip = ZipFile.OpenRead(zipPath);
		foreach (var e in zip.Entries) {
			if (e.Name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) &&
				e.FullName.Contains("/bin/", StringComparison.OrdinalIgnoreCase)) {
				progress.SubMessage = e.Name;
				e.ExtractToFile(Path.Combine(destDir, e.Name), overwrite: true);
			}
		}
	}

	static void ExtractSharedLibsFromTarXz(string tarXzPath, string destDir, LoadProgress progress) {
		using FileStream fs = File.OpenRead(tarXzPath);
		using var xz = new XZStream(fs);
		using var reader = TarReader.OpenReader(xz);
		while (reader.MoveToNextEntry()) {
			var entry = reader.Entry;
			if (entry.IsDirectory) continue;
			string name = Path.GetFileName(entry.Key ?? "");
			if ((entry.Key?.Contains("/lib/") ?? false) &&
				name.Contains(".so", StringComparison.OrdinalIgnoreCase)) {
				progress.SubMessage = name;
				string outPath = Path.Combine(destDir, name);
				using FileStream outFs = File.Create(outPath);
				reader.WriteEntryTo(outFs);
			}
		}
	}

	readonly MediaFile media;

	readonly int frameWidth;
	readonly int frameHeight;
	readonly int frameByteCount;

	public int Width => frameWidth;
	public int Height => frameHeight;

	readonly Stream stream;
	readonly bool disposeStream;
	bool disposed;

	readonly byte[] lastFrame;

	public CinemaBackgroundGenerator(Stream stream, bool disposeStream = true) {
		EnsureLoaded();
		Interlude.Spin("Loading Cinema background...");
		var opts = new MediaOptions {
			VideoPixelFormat = ImagePixelFormat.Rgba32,
			StreamsToLoad = MediaMode.Video
		};
		media = MediaFile.Open(stream, opts);
		this.stream = stream;
		this.disposeStream = disposeStream;

		var size = media.Video.Info.FrameSize;
		frameWidth = size.Width;
		frameHeight = size.Height;
		frameByteCount = frameWidth * frameHeight * 4;
		lastFrame = new byte[frameByteCount];

		double rate = media.Video.Info.AvgFrameRate;
		FrameDuration = rate > 0 ? 1.0 / rate : 1.0 / 30.0;
	}

	public bool IsValid() => !disposed;

	public void Dispose() {
		disposed = true;
		media.Dispose();
		if (disposeStream)
			stream.Dispose();
	}

	bool haveFrame;

	public bool GetFrame(TimeSpan timestamp, Span<byte> dst) {
		int valid = frameByteCount;
		long wantIndex = (long)(timestamp.TotalSeconds / FrameDuration);

		if (haveFrame && wantIndex == lastFrameIndex) {
			lastFrame.AsSpan(0, valid).CopyTo(dst);
			return true;
		}

		bool forward = haveFrame && wantIndex == lastFrameIndex + 1;
		bool ok = forward
			? media.Video.TryGetNextFrame(dst)
			: media.Video.TryGetFrame(timestamp, dst);

		if (ok) {
			dst.Slice(0, valid).CopyTo(lastFrame);
			lastFrameIndex = wantIndex;
			haveFrame = true;
			return true;
		}

		if (haveFrame) { lastFrame.AsSpan(0, valid).CopyTo(dst); return true; }
		return false;
	}

	readonly double FrameDuration;
	long lastFrameIndex = -1;

	public void Regenerate(ITextureCanvas canvas) {
		IConductor conductor = EngineCore.Level.As<MuseDash1Game>().Conductor; // TODO: better modding api
		GetFrame(TimeSpan.FromSeconds(Math.Max(conductor?.GetTime() ?? 0d, 0d)), canvas.Pixels);
	}
}