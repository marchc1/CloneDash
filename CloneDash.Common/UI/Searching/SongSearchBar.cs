using Nucleus.Extensions;
using Nucleus.Types;
using Nucleus.UI;

namespace CloneDash.Menu.Searching;

public class SongSearchBar : Button
{
	public string? SearchQuery = null;
	public SongSearchBar(Element? parent) : base(parent){ 
		Origin = Anchor.Center;
	}

	protected override void PerformLayout(float width, float height) {
		base.PerformLayout(width, height);
		SetTextSize(height / 1.5f);
	}

	public override void Paint(float width, float height) {
		SetText(SearchQuery ?? "Search...");
		SetTextColor(SearchQuery == null ? DefaultTextColor.Adjust(0, 0, -0.3f) : DefaultTextColor);

		base.Paint(width, height);
	}
}