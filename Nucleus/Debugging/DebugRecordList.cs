using Nucleus.Common.Types;
using Nucleus.Core;
using Nucleus.Rendering;
using Nucleus.Types;
using Nucleus.Util;

namespace Nucleus.Debugging
{
	public class DebugRecordList : ICanDraw
	{
		public InlineArray128<DebugRecord> Records;
		public int NumRecords;
		public int Spacing;

		private static readonly Color BackgroundDrawColor = new Color(10, 10, 10, 180);
		
		public void Reset() {
			NumRecords = Spacing = 0;
		}

		public ref DebugRecord GetRecord(int i) => ref Records[i];
		public void Write() => Records[NumRecords++] = default;
		public void Write(ReadOnlySpan<char> key) => Records[NumRecords++] = new(Spacing * SpacingCharacters, key, null, true);
		public void Write(ReadOnlySpan<char> key, ReadOnlySpan<char> value) => Records[NumRecords++] = new(Spacing * SpacingCharacters, key, value);
		
		private static readonly char[] TempFormatBuffer = new char[256];

		public void Write<T>(ReadOnlySpan<char> key, T value) where T : ISpanFormattable {
			value.TryFormat(TempFormatBuffer, out int chars, default, null);
			Records[NumRecords++] = new DebugRecord(Spacing * SpacingCharacters, key, TempFormatBuffer.AsSpan()[..chars]);
		}
		
		public void EnterScope() => Spacing += 1;
		public void ExitScope() => Spacing -= 1;

		public int SpacingCharacters => 4;

		public DebugRecordState CompileState() {
			DebugRecordState state = new();
			for (int i = 0; i < NumRecords; i++) {
				ref DebugRecord record = ref Records[i];

				if (record.HasValue) {
					if (state.MaxKeySize < record.KeySize) state.LargestKeyIdx = i;
					state.MaxKeySize = Math.Max(state.MaxKeySize, record.KeySize);
					state.MaxKeyPlusSpacingSize = Math.Max(state.MaxKeyPlusSpacingSize, record.KeySize + record.Spacing);
				}

				if (state.MaxValueSize < record.ValueSize) state.LargestValueIdx = i;

				state.MaxValueSize = Math.Max(state.MaxValueSize, record.ValueSize);
			}

			return state;
		}

		public void Draw(FrameState frameState) {
			DebugRecordState state = CompileState();
			const float sizePer = 12;
			float ty = (frameState.WindowHeight - 8) - (NumRecords * sizePer);
			DrawRecords(in state, sizePer, ref ty);
		}
		
		public void DrawRecords(in DebugRecordState state, float sizePer, ref float ty) {
			for (int i = 0; i < NumRecords; i++, ty += sizePer) {
				ref DebugRecord record = ref GetRecord(i);
				const int tx = 12;

				ReadOnlySpan<char> text = record.Print(in state);
				Graphics2D.SetDrawColor(BackgroundDrawColor);
				Graphics2D.DrawRectangle(new Vector2F(tx - 1, ty - 1), Graphics2D.GetTextSize(text, "Consolas", 11) + new Vector2F(2));
				Graphics2D.SetDrawColor(Color.White);
				Graphics2D.DrawText(tx, ty, text, "Consolas", 11, Anchor.TopLeft);
			}
		}
	}
}