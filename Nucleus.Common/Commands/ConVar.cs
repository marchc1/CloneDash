using Nucleus.Common.Commands;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Nucleus.Commands;

public class ConVar : ConCommandBase, IConVar
{
	public ConVar? Parent;

	private CVValue lastNonDefaultValue = new();

	private readonly double? maximum = null;

	private readonly double? minimum = null;

	private CVValue value = new();

	public ConVar(string name, string defaultValue) : this(name, defaultValue, 0, "", null, null, null, null) {
	}

	public ConVar(string name, string defaultValue, FCvar flags) : this(name, defaultValue, flags, "", null, null, null, null) {
	}

	public ConVar(string name, double defaultValue) : this(name, $"{defaultValue}", 0, "", null, null, null, null) {
	}

	public ConVar(string name, double defaultValue, FCvar flags) : this(name, $"{defaultValue}", flags, "", null, null, null, null) {
	}

	public ConVar(string name, double defaultValue, FCvar flags, double min, double max) : this(name, $"{defaultValue}", flags, "", min, max, null, null) {
	}

	public ConVar(string name, double defaultValue, FCvar flags, string helpText, double min, double max) : this(name, $"{defaultValue}", flags, helpText, min, max, null, null) {
	}

	public ConVar(string name, string defaultValue, FCvar flags, string helpString, double? min = null, double? max = null, ChangeCallback? callback = null, AutocompleteDelegate? autocomplete = null) : base(name, helpString, flags) {
		Parent = this;
		SetDefault(defaultValue);

		value.StringLength = DefaultValue.IndexOf('\0');
		if (value.StringLength == -1) value.StringLength = DefaultValue.Length;
		value.Chars = new char[value.StringLength];
		DefaultValue.CopyTo(value.Chars);

		minimum = min;
		maximum = max;

		if (callback != null)
			OnChange += callback;

		OnAutocomplete = autocomplete;

		double.TryParse(value.GetString(), provider: CultureInfo.InvariantCulture, out value.Double);
		int.TryParse(value.GetString(), provider: CultureInfo.InvariantCulture, out value.Int);
	}

	public delegate void OnConvarChangeDelegate(ConVar self, CVValue old, CVValue now);

	public event ChangeCallback? OnChange;

	public override void Init() {
		base.Init();
		if (OnChange != null)
			OnChange?.Invoke(this, "", 0);
	}

	public string DefaultValue { get; set; }

	public static void PrintDescription(ConCommandBase cmdbase) {
		if (!cmdbase.IsCommand()) {
			ConVar var = (ConVar)cmdbase;

			bool hasMin = var.GetMin(out double min);
			bool hasMax = var.GetMax(out double max);

			printCtx ctx = new(stackalloc char[512]);
			ctx.Print('"');
			ctx.Print(var.GetName());
			ctx.Print('"');

			ctx.Print(" = ");
			var value = var.GetString();
			if (value.IsEmpty)
				ctx.Print("\"\"");
			else
				ctx.Print(value);

			if (stricmp(value, var.GetDefault()) != 0) {
				ctx.Print(" ( def. \"");
				ctx.Print(var.GetDefault());
				ctx.Print("\" )");
			}

			if (hasMin) {
				ctx.Print(" min. ");
				ctx.Print(min);
			}

			if (hasMax) {
				ctx.Print(" max. ");
				ctx.Print(max);
			}

			ctx.StringThenReset(Logs.Print);
			PrintFlags(var);

			ReadOnlySpan<char> helpText = var.GetHelpText();
			if (!helpText.IsEmpty && helpText[0] != '\0') {
				ctx.Print(" - ");
				ctx.Print(helpText);
				ctx.StringThenReset(Logs.Print);
			}
		}
	}

	public static void PrintFlags(ConCommandBase var) {
		printCtx ctx = new(stackalloc char[384]);

		if (var.IsFlagSet(FCvar.Saved)) ctx.Print(" saved");
		if (var.IsFlagSet(FCvar.NotInGame)) ctx.Print(" notingame");
		if (var.IsFlagSet(FCvar.AlwaysDefault)) ctx.Print(" alwaysdefault");

		if (ctx.IsEmpty)
			return;

		ctx.StringThenReset(Logs.Print);
	}

	public static void Register() {
		ConCommandBase? cur, next;
		cur = Head;
		while (cur != null) {
			next = cur.Next;
			cur.Init();
			cur = next;
		}
	}

	public bool GetBool() => value.Int >= 1;

	public ReadOnlySpan<char> GetDefault() => DefaultValue;

	public double GetDouble() => value.Double;

	public float GetFloat() => (float)value.Double;

	public int GetInt() => value.Int;

	public bool GetMax(out double v) {
		if (!maximum.HasValue) {
			v = 0;
			return false;
		}
		v = maximum.Value;
		return true;
	}

	public bool GetMin(out double v) {
		if (!minimum.HasValue) {
			v = 0;
			return false;
		}
		v = minimum.Value;
		return true;
	}

	public ReadOnlySpan<char> GetSaveString() => IsLocked() ? lastNonDefaultValue.GetString() : value.GetString();

	public ReadOnlySpan<char> GetString() => value.GetString();

	public override bool IsCommand() => false;

	public bool IsLocked() => (Flags & FCvar.AlwaysDefault) != 0;

	public void Revert() {
		ConVar var = Parent!;
		var.SetValue(var.DefaultValue);
	}

	[MemberNotNull(nameof(DefaultValue))]
	public void SetDefault(string def) {
		DefaultValue = def;
	}

	public void SetValue(ReadOnlySpan<char> str) {
		ConVar var = Parent!;
		if (!var.IsLocked())
			var.InternalSetValue(str);
	}

	public void SetValue(int i) {
		ConVar var = Parent!;
		if (!var.IsLocked())
			var.InternalSetIntValue(i);
	}

	public void SetValue(double d) {
		ConVar var = Parent!;
		if (!var.IsLocked())
			var.InternalSetDoubleValue(d);
	}

	public void SetValue(bool b) {
		ConVar var = Parent!;
		if (!var.IsLocked())
			var.InternalSetIntValue(b ? 1 : 0);
	}

	protected override void CheckFlagChange(FCvar prev, FCvar now) {
		var wasDefault = (prev & FCvar.AlwaysDefault) == FCvar.AlwaysDefault;
		var isDefault = (now & FCvar.AlwaysDefault) == FCvar.AlwaysDefault;

		bool changedToDefault = !wasDefault && isDefault;
		bool changedToNotDefault = wasDefault && !isDefault;

		if (changedToDefault) {
			lastNonDefaultValue = value;
			lastNonDefaultValue.Chars = lastNonDefaultValue.Chars?.ToArray(); // Copy off the char array

			Parent!.InternalSetValue(DefaultValue);
		}
		else if (changedToNotDefault) {
			value = lastNonDefaultValue;
			value.Chars = value.Chars?.ToArray(); // Copy off the char array
		}
	}

	private void ChangeStringValue(scoped ReadOnlySpan<char> val, double oldD) {
		Debug.Assert((Flags & FCvar.NeverAsString) == 0, "NeverAsString, yet ChangeStringValue still called...");

		Span<char> oldValue = stackalloc char[value.StringLength];
		value.Chars?.AsSpan()[..value.StringLength].CopyTo(oldValue);

		int len = val.IndexOf('\0');
		if (len == -1)
			len = val.Length;

		if (len > value.Chars?.Length)
			value.Chars = new char[len];

		val[..len].CopyTo(value.Chars);
		value.StringLength = len;

		if (!val.Equals(oldValue, StringComparison.InvariantCulture)) {
			OnChange?.Invoke(this, oldValue, oldD);
			cvar.CallGlobalChangeCallbacks(this, oldValue, oldD);
		}
	}

	// hack for AlwaysDefault to allow the flag to revert
	private bool ClampValue(ref double value) {
		if (minimum.HasValue && value < minimum.Value) {
			value = minimum.Value;
			return true;
		}

		if (maximum.HasValue && value > maximum.Value) {
			value = maximum.Value;
			return true;
		}

		return false;
	}

	private void InternalSetDoubleValue(double doubleValue) {
		if (doubleValue == this.value.Double)
			return;

		Debug.Assert(Parent == this);

		ClampValue(ref doubleValue);

		double oldValue = this.value.Double;
		this.value.Double = doubleValue;
		this.value.Int = Convert.ToInt32(doubleValue);

		if ((Flags & FCvar.NeverAsString) == 0) {
			Span<char> tempVal = stackalloc char[32];
			this.value.Double.TryFormat(tempVal, out int charsWritten, provider: CultureInfo.InvariantCulture);
			ChangeStringValue(tempVal[..charsWritten], oldValue);
		}
		else
			Debug.Assert(OnChange == null);
	}

	private void InternalSetIntValue(int intValue) {
		if (intValue == this.value.Int)
			return;

		Debug.Assert(Parent == this);

		double doubleValue = (double)intValue;
		if (ClampValue(ref doubleValue))
			intValue = Convert.ToInt32(doubleValue);

		double oldValue = this.value.Double;
		this.value.Double = doubleValue;
		this.value.Int = intValue;

		if ((Flags & FCvar.NeverAsString) == 0) {
			Span<char> tempVal = stackalloc char[32];
			this.value.Int.TryFormat(tempVal, out int charsWritten, provider: CultureInfo.InvariantCulture);
			ChangeStringValue(tempVal[..charsWritten], oldValue);
		}
		else
			Debug.Assert(OnChange == null);
	}

	private void InternalSetValue(ReadOnlySpan<char> value) {
		double newD;
		Span<char> temp = stackalloc char[64];
		scoped ReadOnlySpan<char> val;

		Debug.Assert(Parent == this);

		double oldD = this.value.Double;

		val = value;
		if (value.IsEmpty) newD = 0.0;
		else double.TryParse(value, provider: CultureInfo.InvariantCulture, out newD);

		if (ClampValue(ref newD)) {
			newD.TryFormat(temp, out int charsWritten, provider: CultureInfo.InvariantCulture);
			val = temp[..charsWritten];
		}

		this.value.Double = newD;
		this.value.Int = Convert.ToInt32(newD);

		if ((Flags & FCvar.NeverAsString) == 0)
			ChangeStringValue(val, oldD);
	}

	private ref struct printCtx
	{
		private Span<char> dest;

		private int printStrLength;

		public printCtx(Span<char> dest) {
			this.dest = dest;
		}

		public readonly bool IsEmpty => printStrLength == 0;

		public void Print(ReadOnlySpan<char> incoming) {
			incoming = incoming.SliceNullTerminatedString();
			incoming.CopyTo(dest[printStrLength..]);
			printStrLength += incoming.Length;
		}

		public void Print(char incoming) {
			dest[printStrLength] = incoming;
			printStrLength += 1;
		}

		public void Print(int incoming) {
			incoming.TryFormat(dest[printStrLength..], out int charsWritten, provider: CultureInfo.InvariantCulture);
			printStrLength += charsWritten;
		}

		public void Print(double incoming) {
			incoming.TryFormat(dest[printStrLength..], out int charsWritten, provider: CultureInfo.InvariantCulture);
			printStrLength += charsWritten;
		}

		public void Reset() {
			printStrLength = 0;
		}

		public readonly ReadOnlySpan<char> String() => dest[..printStrLength];

		public void StringThenReset(Action<ReadOnlySpan<char>> output) {
			output(dest[..printStrLength]);
			Reset();
		}
	}
}