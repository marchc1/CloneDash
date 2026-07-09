using CloneDash.Common.UI;
using Nucleus;
using Nucleus.Common.Types;
using Nucleus.Core;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;

namespace CloneDash.Menu;

public class MenuFooterButton : Button
{
    public Action? Action { get; set; }

    public string? Icon
    {
        set => _image.SetTexture(string.IsNullOrWhiteSpace(value) ? null : Level.Textures.LoadTextureFromFile(value));
    }

    private readonly Image _image;
    private readonly SecondOrderSystem _sos = new(3, 1, 1, 60);

    public MenuFooterButton(Element? parent, string icon = "", string text = "") : base(parent, text)
    {
        SetSize(new Vector2F(200, 60));
        SetBorderSize(3);
        SetRoundness(8);
        SetClipping(false);
        OnButtonClick += (_, _) => Action?.Invoke();

        _image = new Image(this);
        _image.SetAnchor(Anchor.CenterLeft);
        _image.SetOrigin(Anchor.CenterLeft);
        _image.SetSize(new Vector2F(24));
        Icon = icon;
    }

    protected override void OnThink()
    {
        base.OnThink();

        var y = _sos.Update(Action != null ? -12 : 60);
        SetPos(new Vector2F(GetPos().X, y));
    }

    public override void Paint(float width, float height)
    {
        ColorStateSetup(out _, out var fore);
        Graphics2D.SetDrawColor(fore);
        _image.SetImageColor(fore);

        var right = GetAnchor() == Anchor.BottomRight;

        const float iconSize = 24;

        var textHeight = CloneDashUI.GetFontSize(20);
        var size = Graphics2D.DrawText(new Vector2F(32 + (right ? 0 : iconSize + 20), height / 2f), GetText(),
            CloneDashUI.FontBold, textHeight, Anchor.CenterLeft);
        SetSize(new Vector2F(size.X + 64 + 20 + iconSize, GetSize().Y));

        _image.SetPos(new Vector2F(32 + (right ? 20 + size.X : 0), 0));
    }
}