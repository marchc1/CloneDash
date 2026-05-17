using CloneDash.Charts;
using CloneDash.UI;
using Nucleus.Common.Input;
using Nucleus.Input;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;
using System.Reflection;

namespace CloneDash.Menu.Searching;

public class DialogLabelPanel<T> : Panel where T : Element
{
	Label label = null!;
	T element = null!;
	public DialogLabelPanel(Element? parent) : base(parent){ 
		BorderSize = 0;

		label = new(this);
		element = (T)Activator.CreateInstance(typeof(T), [this])!; // This sucks
	}
	public T Get() => element;
	protected override void TextChanged(string oldText, string newText) {
		label.Text = newText;
	}
	protected override void PerformLayout(float width, float height) {
		label.Position = new(0, 0);
		var div = 4f;
		label.Size = new(width / div, height);

		var padding = 4;
		element.Position = new((width / div) + padding, padding);
		element.Size = new((width - (width / div)) - (padding * 2), (height) - (padding * 2));
	}
}

public class SongSearchDialog : Window
{
	public SongSearchBar Bar;
	Button applyButton;

	ScrollPanel parameters;
	public delegate void OnUserSubmitD();
	public event OnUserSubmitD? OnUserSubmit;
	public SongSelector Selector;

	class applyStepData
	{
		public required string target;
		public required Func<object?> valueFn;
	}

	readonly List<applyStepData> applySteps = [];

	public void SetBarText(string text) => Bar.SearchQuery = string.IsNullOrEmpty(text) ? null : text;

	public SongSearchDialog(Element? parent) : base(parent) {
		MakePopup();

		DynamicallySized = true;
		Size = new(0.4f);
		Resizable = false;
		HideNonCloseButtons();
		Title = "Song Search Dialog";

		applyButton = new(this);
		applyButton.Text = "Apply";
		applyButton.BorderSize = 0;
		applyButton.Dock = Dock.Bottom;

		applyButton.MouseReleaseEvent += ApplyButton_MouseReleaseEvent;

		parameters = new(this);
		parameters.Dock = Dock.Fill;
		AddParent = parameters;

		Center();
	}

	public DialogLabelPanel<T> InputPanel<T>(ReadOnlySpan<char> label) where T : Element {
		DialogLabelPanel<T> pnl = new(parameters);
		pnl.Dock = Dock.Top;
		pnl.Size = new(0, 0.15f);
		pnl.DynamicallySized = true;
		pnl.Text = new(label.SliceNullTerminatedString());
		return pnl;
	}

	public void NumberCarouselInput(ReadOnlySpan<char> name, ReadOnlySpan<char> label, int value, int min, int max) {
		var pnl = InputPanel<NumberPickerCarousel>(label);
		pnl.Get().MinimumValue = min;
		pnl.Get().MaximumValue = max;
		pnl.Get().Value = value;
		applySteps.Add(new() { target = new(name.SliceNullTerminatedString()), valueFn = () => Convert.ToInt32(pnl.Get().Value) });
	}

	public void NumberInput(ReadOnlySpan<char> name, ReadOnlySpan<char> label, float value, float? min = null, float? max = null) {
		var pnl = InputPanel<NumSlider>(label);
		pnl.Get().Value = value;
		pnl.Get().Digits = 3;
		pnl.Get().MinimumValue = min;
		pnl.Get().MaximumValue = max;
		applySteps.Add(new() { target = new(name.SliceNullTerminatedString()), valueFn = () => Convert.ToInt32(pnl.Get().Value) });
	}

	public void NumberInput(ReadOnlySpan<char> name, ReadOnlySpan<char> label, int value, int? min = null, int? max = null) {
		var pnl = InputPanel<NumSlider>(label);
		pnl.Get().Value = value;
		pnl.Get().Digits = 3;
		pnl.Get().MinimumValue = min;
		pnl.Get().MaximumValue = max;
		applySteps.Add(new() { target = new(name.SliceNullTerminatedString()), valueFn = () => (float)pnl.Get().Value });
	}

	public void NumberInput(ReadOnlySpan<char> name, ReadOnlySpan<char> label, double value, double? min = null, double? max = null) {
		var pnl = InputPanel<NumSlider>(label);
		pnl.Get().Value = value;
		pnl.Get().Digits = 6;
		pnl.Get().MinimumValue = min;
		pnl.Get().MaximumValue = max;
		applySteps.Add(new() { target = new(name.SliceNullTerminatedString()), valueFn = () => pnl.Get().Value });
	}

	public Textbox TextboxInput(ReadOnlySpan<char> name, ReadOnlySpan<char> label, ReadOnlySpan<char> value) {
		var pnl = InputPanel<Textbox>(label);
		pnl.Text = new(label.SliceNullTerminatedString());
		pnl.Get().Text = new(value.SliceNullTerminatedString());
		applySteps.Add(new() { target = new(name.SliceNullTerminatedString()), valueFn = () => pnl.Get().Text });

		return pnl.Get();
	}

	public void BoolInput(ReadOnlySpan<char> name, ReadOnlySpan<char> label, bool state) {
		var pnl = InputPanel<Checkbox>(label);
		pnl.Text = new(label.SliceNullTerminatedString());
		pnl.Get().Checked = state;
		applySteps.Add(new() { target = new(name.SliceNullTerminatedString()), valueFn = () => pnl.Get().Checked });
	}

	public void EnumInput<T>(ReadOnlySpan<char> name, ReadOnlySpan<char> label, T value) where T : Enum {
		var pnl = InputPanel<DropdownSelector<T>>(label);
		foreach (var enumValue in Enum.GetValuesAsUnderlyingType(typeof(T)))
			pnl.Get().Items.Add(value);

		pnl.Get().Selected = value;
		applySteps.Add(new() { target = new(name.SliceNullTerminatedString()), valueFn = () => pnl.Get().Selected });
	}

	public ISongSourceState Apply<T, F>(T state, F filter) where T : ISongSourceState where F : IChartSongFilter {
		var newFilter = (F)state.NewFilter();
		var type = newFilter.GetType();

		foreach (var applyStep in applySteps) {
			var member = type.GetMember(applyStep.target, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).First();
			switch (member) {
				case FieldInfo field:
					field.SetValue(newFilter, applyStep.valueFn());
					break;
				case PropertyInfo prop:
					prop.SetValue(newFilter, applyStep.valueFn());
					break;
				default:
					throw new Exception();
			}
		}

		var source = state.GetRootSource().ProduceNewSource(newFilter);
		return source;
	}

	private void ApplyButton_MouseReleaseEvent(Element self, FrameState state, ButtonCode button) => Submit();
	public void Submit() {
		OnUserSubmit?.Invoke();
		Close();
	}

	public override void Paint(float width, float height) {
		base.Paint(width, height);
	}

	protected override void PerformLayout(float width, float height) {
		base.PerformLayout(width, height);
		applyButton.Size = new(height * 0.1f);
	}
}

