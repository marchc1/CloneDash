using Newtonsoft.Json;
using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.ModelEditor.UI;
using Nucleus.Models;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;
using Nucleus.Util;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Nucleus.ModelEditor
{
	public class ModelEditor : Level
	{
		// Object selection management
		private List<object> __selectedObjectsL = [];
		private HashSet<object> __selectedObjects = [];
		public object? LastSelectedObject => __selectedObjectsL.LastOrDefault();
		public int SelectedObjectsCount => __selectedObjectsL.Count;


		public delegate void OnObjectSelected(object selected);
		public delegate void OnSelectedChanged();

		public event OnObjectSelected? ObjectSelected;
		public event OnObjectSelected? ObjectUnselected;
		public event OnSelectedChanged? SelectedChanged;

		public void SelectObject(object o, bool additive = false) {
			if (!additive) {
				foreach (var obj in __selectedObjects.ToArray()) {
					__selectedObjects.Remove(obj);
					__selectedObjectsL.Remove(obj);
					ObjectUnselected?.Invoke(obj);
				}
				// We don't call UnselectAllObjects because that calls SelectedChanged twice; we don't want that
			}

			__selectedObjects.Add(o);
			__selectedObjectsL.Add(o);
			SelectedChanged?.Invoke();
		}

		public void UnselectObject(object o) {
			__selectedObjects.Remove(o);
			__selectedObjectsL.Remove(o);
			ObjectUnselected?.Invoke(o);
			SelectedChanged?.Invoke();
		}

		public void SelectObjects(params object[] os) {
			foreach (var obj in os) {
				__selectedObjects.Add(obj);
				__selectedObjectsL.Add(obj);
				ObjectSelected?.Invoke(obj);
			}

			SelectedChanged?.Invoke();
		}

		public void UnselectAllObjects() {
			// Copy made to avoid modifying enumerable during iteration
			foreach (var obj in __selectedObjects.ToArray()) {
				__selectedObjects.Remove(obj);
				__selectedObjectsL.Remove(obj);
				ObjectUnselected?.Invoke(obj);
			}
			SelectedChanged?.Invoke();
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="t">The type all objects share</param>
		/// <returns>If objects are selected, and if they all share the same type</returns>
		public bool AreAllSelectedObjectsTheSameType([NotNullWhen(true)] out Type? t) {
			t = null;

			if (__selectedObjectsL.Count <= 0)
				return false;

			foreach (var obj in __selectedObjectsL) {
				if (t == null)
					t = obj.GetType();
				else if (t != obj.GetType()) {
					t = null;
					return false;
				}
			}

			return t != null; // we could return true here but the compiler isn't recognizing that t would not be null.
		}


		public static ModelEditor Active;

		public Panel SetupPanel;
		public EditorPanel Editor;
		public OutlinerPanel Outliner;
		public PropertiesPanel Properties;
		public Button SwitchMode;

		public EditorFile File = new();

		public PreUIDeterminations GetDeterminations() {
			PreUIDeterminations determinations = new();

			var count = SelectedObjectsCount;
			var last = LastSelectedObject;
			if (AreAllSelectedObjectsTheSameType(out Type? type)) {
				determinations.OnlySelectedOne = count == 1;
				determinations.AllShareAType = true;
				determinations.SharedType = type;
			}

			determinations.Last = LastSelectedObject;
			determinations.Count = SelectedObjectsCount;
			determinations.Selected = __selectedObjectsL.ToArray();

			return determinations;
		}

		public bool AnimationMode { get; private set; } = false;
		public void ToggleModes() {
			AnimationMode = !AnimationMode;
			if (AnimationMode) {
				SwitchMode.Text = "Animate Mode";
			}
			else {
				SwitchMode.Text = "Setup Mode";
			}
		}

		public override void Initialize(params object[] args) {
			//EngineCore.ShowDebuggingInfo = true;

			Active = this;
			Menubar menubar = UI.Add<Menubar>();
			Keybinds.AddKeybind([KeyboardLayout.USA.LeftControl, KeyboardLayout.USA.R], () => EngineCore.LoadLevel(new ModelEditor()));

			UI.Add(out SetupPanel);
			SetupPanel.Dock = Dock.Fill;
			SetupPanel.DrawPanelBackground = false;

			var rightSide = SetupPanel.Add<Panel>();
			rightSide.Dock = Dock.Right;
			rightSide.Size = new(384, 0);

			rightSide.Add(out Properties);
			Properties.Size = new(64);
			Properties.Dock = Dock.Bottom;

			var tabOptions = rightSide.Add<TabView>();
			tabOptions.Dock = Dock.Fill;

			var OutlinerTab = tabOptions.AddTab("Outliner");
			var StatisticsTab = tabOptions.AddTab("Statistics");

			OutlinerTab.Panel.Add(out Outliner);
			Outliner.Dock = Dock.Fill;

			SetupPanel.Add(out Editor);
			Editor.Dock = Dock.Fill;

			UI.Add(out SwitchMode);
			SwitchMode.Position = new Vector2F(8) + new Vector2F(0, 36);
			SwitchMode.Size = new(128, 32);
			SwitchMode.TextSize = 20;
			SwitchMode.Text = "Setup Mode";
			SwitchMode.MouseReleaseEvent += (_, _, _) => ToggleModes();

			Outliner.NodeClicked += Outliner_NodeClicked;
			File.NewFile();

			Keybinds.AddKeybind([KeyboardLayout.USA.Delete], AttemptDelete);
			Keybinds.AddKeybind([KeyboardLayout.USA.F2], AttemptRename);
		}

		private void AttemptRename() {
			var determinations = GetDeterminations();

			if (determinations.Count != 1)
				return;

			// This is... a weird way of doing it...
			// but I want to minimize how much code we write here...

			Action<string>? callback = null;
			string typeName = "";
			string currentText = "";

			switch (determinations.Last) {
				case EditorBone bone:
					typeName = "bone";
					currentText = bone.Name;
					callback = (text) => File.RenameBone(bone, text);
					break;
			}

			// cannot rename
			if (callback == null)
				return;

			EditorDialogs.TextInput(
				$"Rename {typeName.CapitalizeFirstCharacter()}",
				$"Enter the new name for this {typeName}",
				currentText,
				true,
				callback,
				null
			);
		}

		private bool CanDelete(object? item) {
			if (item == null)
				return false;

			switch (item) {
				case EditorBone bone:
					if (bone.Model.Root == bone)
						return false;
					return true;
				default:
					return true;
			}
		}
		private void AttemptDelete() {
			var determinations = GetDeterminations();

			if (determinations.Count == 0) 
				return;

			var plural = determinations.Count > 1;
			if (!plural && !CanDelete(determinations.Last))
				return;

			var text = "";

			if (determinations.AllShareAType) {
				switch (determinations.Last) {
					case EditorBone bone:
						text = plural ? "bones" : $"bone '{bone.Name}'";
						break;
				}
			}
			else {
				text = "items";
			}

			if (!string.IsNullOrWhiteSpace(text)) {
				EditorDialogs.ConfirmAction(
					$"Remove {text}",
					$"Are you sure you want to remove {(plural ? $"these {text}" : $"the {text}")}?",
					true,
					() => {
						foreach(var item in determinations.Selected) {
							switch (item) {
								case EditorBone bone:
									File.RemoveBone(bone);
									break;
							}
						}
					}
				);
			}
		}

		private void Outliner_NodeClicked(OutlinerPanel panel, OutlinerNode node, MouseButton btn) {
			object? o = node.GetRepresentingObject();
			if (o == null) return;
			SelectObject(o);
		}
	}

	internal class Program
	{
		static void Main(string[] args) {
			EngineCore.Initialize(1600, 900, "Model v4 Editor", args);
			EngineCore.GameInfo = new() {
				GameName = "Model v4 Editor"
			};
			EngineCore.LimitFramerate(60);

			EngineCore.LoadLevel(new ModelEditor());
			EngineCore.Start();
		}
	}
}
