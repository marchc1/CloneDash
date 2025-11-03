using Nucleus.Input;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;

namespace CloneDash.Menu.Searching;

public class SongSearchDialog : Window
{
	public SongSearchBar Bar;
	Button applyButton;

	ScrollPanel parameters;
	public delegate void OnUserSubmitD();
	public event OnUserSubmitD? OnUserSubmit;
	public SongSelector Selector;

	public void SetBarText(string text) => Bar.SearchQuery = string.IsNullOrEmpty(text) ? null : text;

	protected override void Initialize() {
		base.Initialize();
		MakePopup();

		DynamicallySized = true;
		Size = new(0.4f);
		Resizable = false;
		HideNonCloseButtons();
		Title = "Song Search Dialog";

		Add(out applyButton);
		applyButton.Text = "Apply";
		applyButton.BorderSize = 0;
		applyButton.Dock = Dock.Bottom;

		applyButton.MouseReleaseEvent += ApplyButton_MouseReleaseEvent;

		Add(out parameters);
		parameters.Dock = Dock.Fill;
		AddParent = parameters;
		
		Center();
	}

	private void ApplyButton_MouseReleaseEvent(Element self, FrameState state, MouseButton button) => Submit();
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

