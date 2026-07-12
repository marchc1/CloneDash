using System.Text.RegularExpressions;

using Nucleus.UI;
using Nucleus.Core;

namespace CloneDash.Menu.Searching;

/// <summary> A Label which always rendering CJK characters.</summary>
public class SongLabel(Element? parent) : Label(parent)
{
	private string textRaw;
}
