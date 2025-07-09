using CloneDash.Data;

using Nucleus.UI;
using Nucleus.UI.Elements;

namespace CloneDash.Menu.Searching;

public abstract class SearchFilter
{
	public abstract void Populate(SongSearchDialog dialog);
	public abstract Predicate<ChartSong> BuildPredicate(SongSearchDialog dialog);

	public void TextInput(SongSearchDialog dialog, string field, string helperText, bool enterReturns = false, bool keyboardFocus = false) {
		var type = GetType();
		var fieldInfo = type.GetField(field);
		if (fieldInfo == null) throw new NotImplementedException();

		dialog.Add(out Textbox textbox);
		textbox.Dock = Dock.Top;
		textbox.Size = new(0.12f);

		if (enterReturns)
			textbox.OnUserPressedEnter += (_, _, _) => dialog.Submit();

		textbox.DynamicallySized = true;

		textbox.TextSize = 10;
		textbox.Text = fieldInfo.GetValue(this)?.ToString() ?? "";
		textbox.HelperText = helperText;
		textbox.TextSize = 20;
		textbox.BorderSize = 0;
		textbox.OnTextChanged += (_, _, nt) => fieldInfo.SetValue(this, nt);
		if (keyboardFocus) {
			textbox.DemandKeyboardFocus();
			textbox.SelectAll();
		}
		dialog.SetTag(field, textbox);
	}

	public void EnumInput<T>(SongSearchDialog dialog, string field, T curVal) where T : Enum {
		var type = GetType();
		var fieldInfo = type.GetField(field);
		if (fieldInfo == null) throw new NotImplementedException();

		var selector = dialog.Add(DropdownSelector<T>.FromEnum(curVal));
		selector.Dock = Dock.Top;
		selector.Size = new(48);
		selector.Text = fieldInfo.GetValue(this)?.ToString() ?? "";
		selector.TextSize = 20;
		selector.BorderSize = 0;
		selector.OnSelectionChanged += (_, _, nt) => fieldInfo.SetValue(this, nt);
		dialog.SetTag(field, selector);
	}

	public void CheckboxInput(SongSearchDialog dialog, string field, string label) {
		var type = GetType();
		var fieldInfo = type.GetField(field);
		if (fieldInfo == null) throw new NotImplementedException();

		var selector = dialog.Add<CheckboxButton>();
		selector.Dock = Dock.Top;
		selector.Size = new(48);
		selector.Text = label;
		selector.TextSize = 20;
		selector.BorderSize = 0;
		selector.Checked = (bool?)fieldInfo.GetValue(this) ?? false;
		selector.OnCheckedChanged += (s) => fieldInfo.SetValue(this, s.Checked);
		dialog.SetTag(field, selector);
	}
}
