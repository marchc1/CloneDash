using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nucleus.Engine
{
	public static class NucleusSingleton
	{
		private static FileStream? lockFileStream;
		private static CancellationTokenSource? redirectListenerCancelToken;

		public delegate void OnProcessRedirect(string[] args);

		private static string GetLockFilePath(string name) {
			string basePath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
				? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Nucleus")
				: Path.Combine(Path.GetTempPath(), "nucleus");

			Directory.CreateDirectory(basePath);
			return Path.Combine(basePath, $"{name}.lock");
		}

		static string pipeName(string name) {
			return $"nucleus_singleton_{name}";
		}

		public static void Request(string name) {
			if (!isDesktop())
				return;

			var lockFilePath = GetLockFilePath(name);
			try {
				lockFileStream = new FileStream(lockFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
				lockFileStream.WriteByte(0);
				lockFileStream.Flush(true);
			}
			catch (IOException) {
				throw new InvalidOperationException($"Another instance of {name} is already running.");
			}
			redirectListenerCancelToken = new CancellationTokenSource();
			Task.Run(() => redirectListener(pipeName(name), redirectListenerCancelToken.Token));
		}

		public static bool TryRedirect(string name, string[] args) {
			if (!isDesktop())
				return false;

			try {
				var pipeName = NucleusSingleton.pipeName(name);
				using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.Out);
				client.Connect(100);

				using var writer = new StreamWriter(client, Encoding.UTF8) { AutoFlush = true };
				writer.WriteLine(string.Join('\0', args.Select(escapeNulls)));
				return false;
			}
			catch {
				return true;
			}
		}

		public static event OnProcessRedirect? Redirect;
		static ConcurrentQueue<string[]> argQueue = [];
		public static void Spin() {
			if (!isDesktop())
				return;

			while (argQueue.TryDequeue(out string[]? args))
				Redirect?.Invoke(args);
		}

		static async Task redirectListener(string pipeName, CancellationToken token) {
			while (!token.IsCancellationRequested) {
				using var server = new NamedPipeServerStream(pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
				try {
					await server.WaitForConnectionAsync(token);
					using var reader = new StreamReader(server, Encoding.UTF8);
					string? line = await reader.ReadLineAsync();

					if (line != null) {
						string[] args = line.Split('\0').Select(unescapeNulls).ToArray();
						argQueue.Enqueue(args);
					}
				}
				catch (OperationCanceledException) { }
				catch (Exception) { }
			}
		}

		static string escapeNulls(string s) => s.Replace("\0", "\\0");
		static string unescapeNulls(string s) => s.Replace("\\0", "\0");

		static bool isDesktop() {
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				return true;
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
				return true;
			if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
				return true;

			return false;
		}
	}
}
