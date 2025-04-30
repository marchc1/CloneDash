using Nucleus.Core;
using Nucleus.Extensions;
using Nucleus.Interfaces;
using Nucleus.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.UI
{
	public class CheckboxButton : Button {
		public bool Checked { get; set; } = false;
		public delegate void CheckboxClicked(CheckboxButton self);
		public event CheckboxClicked? OnCheckedChanged;

		public override void Paint(float width, float height) {
			var bck = BackgroundColor;

			if (Checked)
				BackgroundColor = BackgroundColor.Adjust(0, 0, 2);

			base.Paint(width, height);
			BackgroundColor = bck;
		}

		public override void MouseRelease(Element self, FrameState state, MouseButton button) {
				Checked = !Checked;
			OnCheckedChanged?.Invoke(this);
		}
	}
	public class Checkbox : Button, IBindableToConVar
	{
		public bool Checked { get; set; } = false;
		protected override void Initialize() {
			Text = "";
		}

		public delegate void CheckboxClicked(Checkbox self);
		public event CheckboxClicked? OnCheckedChanged;

		private HashSet<Checkbox> __otherRadioButtons = [];

		public void BindToConVar(string convar) {
			ConVar? cv = (ConVar?)ConCommandBase.Get(convar);
			Debug.Assert(cv != null, "Tried to bind to a non-existant convar");
			if (cv == null) return;

			BindToConVar(cv);
		}

		public void BindToConVar(ConVar cv) {
			Checked = cv.GetBool();
			OnCheckedChanged += (_) 
				=> cv.SetValue(Checked);
		}

		private void Cv_OnChange(ConVar self, CVValue old, CVValue now) {
			Checked = self.GetBool();
		}

		public bool Radio { get; set; } = false;
		public void LinkRadioButton(Checkbox other) {
			if (__otherRadioButtons.Contains(other)) return;
			__otherRadioButtons.Add(other);
			other.OnCheckedChanged += (e) => {
				if (other.Checked && other.Radio)
					this.Checked = false;
			};
			other.LinkRadioButton(this);
		}

		private float? CheckAnim = null;

		public override void Paint(float width, float height) {
			float c = CheckAnim ?? (Checked ? 1 : 0);
			c = Math.Clamp(c + (EngineCore.FrameTime * 6f * (Checked ? 1 : -1)), 0, 1);
			CheckAnim = c;

			DrawAsCircle = Radio;
			if (Radio) {
				var smallest = Math.Min(width, height);
				var largest = Math.Max(width, height);
				var diff = largest - smallest;

				var offset = new Vector2F(
					width > height ? diff / 2 : 0,
					width > height ? 0 : diff / 2
				);
				Graphics2D.OffsetDrawing(offset);

				base.Paint(smallest, smallest);
				if (c > 0) {
					c = NMath.Ease.OutQuart(c);
					Graphics2D.SetDrawColor(TextColor);
					Graphics2D.DrawCircle(new Vector2F(smallest / 2, smallest / 2), new Vector2F((c * smallest) / 5f));
				}

				Graphics2D.OffsetDrawing(-offset);
			}
			else {
				base.Paint(width, height);
				if (c > 0) {
					c = NMath.Ease.InQuad(c);
					Graphics2D.SetDrawColor(TextColor);
					Graphics2D.DrawLineStrip([new(width * 0.25f, height * 0.55f), new(width / 2f, height * 0.8f), new(width * 0.75f, height * 0.28f)], c);
				}
			}
		}

		public override void MouseRelease(Element self, FrameState state, MouseButton button) {
			if (Radio)
				Checked = true;
			else
				Checked = !Checked;
			OnCheckedChanged?.Invoke(this);
		}
	}
}
