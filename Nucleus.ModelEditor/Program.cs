using Nucleus.Core;
using Nucleus.Engine;
using Nucleus.Models;
using Nucleus.Types;
using Nucleus.UI;
using Nucleus.UI.Elements;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Nucleus.ModelEditor
{
	public class EditorFile
	{
		public List<Model> Models = [Model.New()];
	}
	public class ModelEditor : Level
	{

		// Boilerplate for state management.
		// Trying to design it in a way where we can truly add anything without a lot of effort
		// and also make it so we don't interfere with garbage collection stuff by holding states
		private ConditionalWeakTable<object, ObjectState> States = [];
		public class ObjectState
		{
			public WeakReference<object> Object;
			public bool Expanded = true;

			public ObjectState(object reference) {
				Object = new WeakReference<object>(reference);
			}

			public T? GetObject<T>() where T : class => Object.TryGetTarget(out object? target) ? (T)target : null;
		}
		public ObjectState GetObjectState(object o) {
			if (States.TryGetValue(o, out ObjectState? state))
				return state;

			ObjectState objState = new ObjectState(o);
			States.Add(o, objState);
			return objState;
		}

		// Object selection management
		private List<object> __selectedObjectsL = [];
		private HashSet<object> __selectedObjects = [];
		public object? LastSelectedObject => __selectedObjectsL.LastOrDefault();


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

			foreach(var obj in __selectedObjectsL) {
				if (t == null)
					t = obj.GetType();
				else if(t != obj.GetType()) {
					t = null;
					return false;
				}
			}

			return true;
		}

		public bool AreObjectsSelected => __selectedObjects.Count > 0;
		public int SelectedObjectsCount => __selectedObjects.Count;
		public object[] SelectedObjects => __selectedObjects.ToArray();

		public bool IsObjectSelected(object o) => __selectedObjects.Contains(o);

		public bool GetExpanded(object o) => GetObjectState(o).Expanded;
		public void SetExpanded(object o, bool expanded) => GetObjectState(o).Expanded = expanded;
		public void ToggleExpanded(object o) => GetObjectState(o).Expanded = !GetObjectState(o).Expanded;

		public static ModelEditor Active;

		public Panel SetupPanel;
		public EditorPanel Editor;
		public OutlinerPanel Outliner;
		public PropertiesPanel Properties;
		public Button SwitchMode;

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

		public EditorFile File { get; set; } = new();

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
