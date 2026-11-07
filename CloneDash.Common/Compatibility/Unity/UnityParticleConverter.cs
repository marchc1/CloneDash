using AssetStudio;
using Nucleus.Common.Graphics;
using Nucleus.Particles;
using System.Collections;
using System.Collections.Specialized;
using System.Numerics;
using Matrix4x4 = System.Numerics.Matrix4x4;
using Quaternion = System.Numerics.Quaternion;
using Transform = AssetStudio.Transform;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace CloneDash.Compatibility.Unity;

public static class UnityParticleConverter
{
	const float UNITY_GRAVITY = -9.81f;

	public static ParticleSystemDefinition Convert(GameObject root, Func<Texture2D, ITexture> loadTexture, Func<string, bool>? loadMissingFile = null) {
		var definition = CreateDefinition(root, loadTexture, loadMissingFile);
		var transform = root.GetFirstComponent<Transform>();
		if (transform != null)
			AddChildren(definition, transform, Matrix4x4.Identity, loadTexture, loadMissingFile);
		return definition;
	}

	public static ParticleSystemDefinition ConvertSystem(GameObject gameObject, Func<Texture2D, ITexture> loadTexture, Func<string, bool>? loadMissingFile = null) => CreateDefinition(gameObject, loadTexture, loadMissingFile);

	public static Matrix4x4 GetLocalTransform(Transform transform) {
		var position = transform.m_LocalPosition;
		var rotation = transform.m_LocalRotation;
		var scale = transform.m_LocalScale;
		return Matrix4x4.CreateScale(scale.X, scale.Y, scale.Z)
			* Matrix4x4.CreateFromQuaternion(new(rotation.X, rotation.Y, rotation.Z, rotation.W))
			* Matrix4x4.CreateTranslation(position.X, position.Y, position.Z);
	}

	static void AddChildren(ParticleSystemDefinition parent, Transform transform, in Matrix4x4 offset, Func<Texture2D, ITexture> loadTexture, Func<string, bool>? loadMissingFile) {
		foreach (var childTransform in transform.GetChildren()) {
			var child = childTransform.GetGameObject();
			if (child == null || !child.m_IsActive)
				continue;

			var local = GetLocalTransform(childTransform) * offset;
			var system = child.GetFirstComponent<AssetStudio.ParticleSystem>();
			if (system == null) {
				AddChildren(parent, childTransform, local, loadTexture, loadMissingFile);
				continue;
			}

			var definition = CreateDefinition(child, loadTexture, loadMissingFile);
			AddChildren(definition, childTransform, Matrix4x4.Identity, loadTexture, loadMissingFile);
			var data = system.ToType();
			parent.Children.Add(new(definition, ReadRange(Dict(data, "startDelay")).Evaluate(0, 0.5f), local));
		}
	}

	static ParticleSystemDefinition CreateDefinition(GameObject gameObject, Func<Texture2D, ITexture> loadTexture, Func<string, bool>? loadMissingFile) {
		var definition = new ParticleSystemDefinition(gameObject.m_Name ?? "");
		var system = gameObject.GetFirstComponent<AssetStudio.ParticleSystem>();
		if (system == null)
			return definition;

		var data = system.ToType();
		definition.Duration = Float(data, "lengthInSec", 5);
		definition.Looping = Bool(data, "looping");
		definition.TimeScale = Float(data, "simulationSpeed", 1);
		if (Int(data, "moveWithTransform") == 0)
			definition.Operators.Add(new ControlPointLockOperator());

		var initial = Dict(data, "InitialModule");
		if (initial != null) {
			definition.MaxParticles = Math.Max(Int(initial, "maxNumParticles", 1000), 1);
			definition.Initializers.Add(new LifetimeInitializer { Lifetime = ReadRange(Dict(initial, "startLifetime")) });
			definition.Initializers.Add(new ColorInitializer { Color = ReadColorRange(Dict(initial, "startColor")) });
			definition.Initializers.Add(new RadiusInitializer { Radius = ReadRange(Dict(initial, "startSize")).Scaled(0.5f) });
			definition.Initializers.Add(new RotationInitializer { Rotation = ReadRange(Dict(initial, "startRotation")).Scaled(-1) });
		}

		var shape = CreateShape(Enabled(data, "ShapeModule"));
		if (initial != null)
			shape.Speed = ReadRange(Dict(initial, "startSpeed"));
		definition.Initializers.Add(shape);

		var emission = Enabled(data, "EmissionModule");
		if (emission != null)
			AddEmitters(definition, emission);

		definition.Operators.Add(new DecayOperator());

		var limit = Enabled(data, "ClampVelocityModule");
		if (limit != null) {
			definition.Operators.Add(new LimitVelocityOperator {
				Speed = ReadRange(Dict(limit, "magnitude")),
				SeparateAxes = Bool(limit, "separateAxis"),
				X = ReadRange(Dict(limit, "x")),
				Y = ReadRange(Dict(limit, "y")),
				Z = ReadRange(Dict(limit, "z")),
				Dampen = Float(limit, "dampen", 1),
				Drag = ReadRange(Dict(limit, "drag")),
				DragBySize = Bool(limit, "multiplyDragByParticleSize"),
				DragBySpeed = Bool(limit, "multiplyDragByParticleVelocity")
			});
		}

		definition.Operators.Add(new MovementOperator {
			Gravity = new(0, UNITY_GRAVITY, 0),
			GravityScale = initial != null ? ReadRange(Dict(initial, "gravityModifier")) : ParticleRange.Constant(0)
		});

		var force = Enabled(data, "ForceModule");
		if (force != null) {
			definition.Forces.Add(new LifetimeForce {
				X = ReadRange(Dict(force, "x")),
				Y = ReadRange(Dict(force, "y")),
				Z = ReadRange(Dict(force, "z")),
				WorldSpace = Bool(force, "inWorldSpace")
			});
		}

		var velocity = Enabled(data, "VelocityModule");
		if (velocity != null) {
			definition.Operators.Add(new VelocityOverLifetimeOperator {
				X = ReadRange(Dict(velocity, "x")),
				Y = ReadRange(Dict(velocity, "y")),
				Z = ReadRange(Dict(velocity, "z")),
				WorldSpace = Bool(velocity, "inWorldSpace")
			});
		}

		var rotation = Enabled(data, "RotationModule");
		if (rotation != null)
			definition.Operators.Add(new SpinOverLifetimeOperator { Speed = ReadRange(Dict(rotation, "curve")).Scaled(-1) });

		var size = Enabled(data, "SizeModule");
		if (size != null)
			definition.Operators.Add(new RadiusOverLifetimeOperator { Scale = ReadRange(Dict(size, "curve")) });

		var color = Enabled(data, "ColorModule");
		if (color != null)
			definition.Operators.Add(new ColorOverLifetimeOperator { Color = ReadColorRange(Dict(color, "gradient")) });

		var uv = Enabled(data, "UVModule");
		int columns = 1, rows = 1;
		if (uv != null && Int(uv, "mode") == 0) {
			columns = Math.Max(Int(uv, "tilesX", 1), 1);
			rows = Math.Max(Int(uv, "tilesY", 1), 1);
			bool singleRow = Int(uv, "animationType") == 1;
			definition.Operators.Add(new SequenceOverLifetimeOperator {
				FrameOverLifetime = ReadRange(Dict(uv, "frameOverTime")),
				StartFrame = ReadRange(Dict(uv, "startFrame")),
				Cycles = Float(uv, "cycles", 1),
				FrameCount = singleRow ? columns : columns * rows,
				Row = singleRow ? Int(uv, "rowIndex") : 0,
				RowCount = singleRow ? rows : 1,
				RandomRow = singleRow && Int(uv, "rowMode", 1) == 1
			});
		}

		var renderer = gameObject.GetFirstComponent<ParticleSystemRenderer>();
		if (renderer != null && renderer.m_Enabled) {
			ParticleRenderer? particleRenderer = renderer.m_RenderMode switch {
				0 => new ParticleSpriteRenderer(),
				1 => new ParticleStretchedRenderer { LengthScale = renderer.m_LengthScale, VelocityScale = renderer.m_VelocityScale },
				_ => null
			};
			if (particleRenderer != null) {
				particleRenderer.SortOrder = renderer.m_SortingOrder;
				particleRenderer.Material = CreateMaterial(renderer, loadTexture, loadMissingFile);
				particleRenderer.Material.SheetColumns = columns;
				particleRenderer.Material.SheetRows = rows;
				definition.Renderers.Add(particleRenderer);
			}
		}

		return definition;
	}

	static void AddEmitters(ParticleSystemDefinition definition, OrderedDictionary emission) {
		var rate = ReadRange(Dict(emission, "rateOverTime"));
		if (!rate.IsZero)
			definition.Emitters.Add(new ContinuousEmitter { Rate = rate });

		if (emission["m_Bursts"] is not IList bursts)
			return;

		int count = Math.Min(Int(emission, "m_BurstCount", bursts.Count), bursts.Count);
		for (int i = 0; i < count; i++) {
			if (bursts[i] is not OrderedDictionary burst)
				continue;

			var countCurve = Dict(burst, "countCurve");
			definition.Emitters.Add(new BurstEmitter {
				Time = Float(burst, "time"),
				Count = countCurve != null ? ReadRange(countCurve) : ParticleRange.Between(Int(burst, "minCount"), Int(burst, "maxCount")),
				Cycles = Int(burst, "cycleCount", 1),
				Interval = Float(burst, "repeatInterval", 0.01f),
				Probability = Float(burst, "probability", 1)
			});
		}
	}

	static ParticleMaterial CreateMaterial(ParticleSystemRenderer renderer, Func<Texture2D, ITexture> loadTexture, Func<string, bool>? loadMissingFile) {
		var result = new ParticleMaterial();
		if (renderer.m_Materials.Count == 0 || !TryResolve(renderer, renderer.m_Materials[0], loadMissingFile, out var material))
			return result;

		string shaderName = TryResolve(material, material.m_Shader, loadMissingFile, out var shader) ? shader.m_ParsedForm?.m_Name ?? shader.m_Name ?? "" : "";
		var properties = material.m_SavedProperties;

		Vector4? tintColor = null, mainColor = null;
		foreach (var (key, color) in properties.m_Colors) {
			if (key == "_TintColor")
				tintColor = new(color.R, color.G, color.B, color.A);
			else if (key == "_Color")
				mainColor = new(color.R, color.G, color.B, color.A);
		}

		float mode = 0;
		foreach (var (key, value) in properties.m_Floats)
			if (key == "_Mode")
				mode = value;

		if (shaderName.Contains("Premultiply", StringComparison.OrdinalIgnoreCase))
			result.BlendMode = ParticleBlendMode.Premultiplied;
		else if (shaderName.Contains("Additive", StringComparison.OrdinalIgnoreCase))
			result.BlendMode = ParticleBlendMode.Additive;
		else if (shaderName.Contains("Multiply", StringComparison.OrdinalIgnoreCase))
			result.BlendMode = ParticleBlendMode.Multiply;
		else if (shaderName.Contains("Standard", StringComparison.OrdinalIgnoreCase))
			result.BlendMode = mode switch {
				4 => ParticleBlendMode.Additive,
				6 => ParticleBlendMode.Multiply,
				_ => ParticleBlendMode.Alpha
			};

		if (shaderName.StartsWith("Legacy Shaders/Particles", StringComparison.OrdinalIgnoreCase) && tintColor.HasValue)
			result.Color = tintColor.Value * 2;
		else if (shaderName.Contains("Standard", StringComparison.OrdinalIgnoreCase) && mainColor.HasValue)
			result.Color = mainColor.Value;

		foreach (var (key, environment) in properties.m_TexEnvs) {
			if (key != "_MainTex" || !TryResolve(material, environment.m_Texture, loadMissingFile, out var texture) || texture is not Texture2D texture2D)
				continue;

			result.Texture = loadTexture(texture2D);
			result.TextureScale = new(environment.m_Scale.X, environment.m_Scale.Y);
			result.TextureOffset = new(environment.m_Offset.X, 1 - environment.m_Scale.Y - environment.m_Offset.Y);
		}

		return result;
	}

	static ShapeInitializer CreateShape(OrderedDictionary? module) {
		var shape = new ShapeInitializer();
		if (module == null)
			return shape;

		float thickness = Float(module, "radiusThickness", 1);
		int type = Int(module, "type");
		shape.Shape = type switch {
			0 or 1 => ParticleShape.Sphere,
			2 or 3 => ParticleShape.Hemisphere,
			4 or 7 or 8 or 9 => ParticleShape.Cone,
			5 or 15 or 16 => ParticleShape.Box,
			10 or 11 or 17 => ParticleShape.Circle,
			12 => ParticleShape.Edge,
			18 => ParticleShape.Rectangle,
			_ => ParticleShape.Point
		};
		shape.RadiusThickness = type is 1 or 3 or 7 or 9 or 11 ? 0 : thickness;
		shape.EmitFromVolume = type is 8 or 9;
		shape.Radius = module["radius"] is OrderedDictionary radius ? Float(radius, "value", 1) : Float(module, "radius", 1);
		shape.Arc = (module["arc"] is OrderedDictionary arc ? Float(arc, "value", 360) : Float(module, "arc", 360)) * MathF.PI / 180;
		shape.Angle = Float(module, "angle", 25) * MathF.PI / 180;
		shape.Length = Float(module, "length", 5);
		shape.RandomDirection = Float(module, "randomDirectionAmount");
		shape.SphericalDirection = Float(module, "sphericalDirectionAmount");
		shape.RandomPosition = Float(module, "randomPositionAmount");

		var position = ReadVector(Dict(module, "m_Position"), Vector3.Zero);
		var rotation = ReadVector(Dict(module, "m_Rotation"), Vector3.Zero) * (MathF.PI / 180);
		var scale = ReadVector(Dict(module, "m_Scale"), Vector3.One);
		shape.Transform = Matrix4x4.CreateScale(scale)
			* Matrix4x4.CreateFromQuaternion(Quaternion.CreateFromYawPitchRoll(rotation.Y, rotation.X, rotation.Z))
			* Matrix4x4.CreateTranslation(position);
		return shape;
	}

	static ParticleColorRange ReadColorRange(OrderedDictionary? data) {
		if (data == null)
			return ParticleColorRange.Constant(Vector4.One);

		return new() {
			Mode = Int(data, "minMaxState") switch {
				1 => ParticleColorRangeMode.Gradient,
				2 => ParticleColorRangeMode.RandomBetweenColors,
				3 => ParticleColorRangeMode.RandomBetweenGradients,
				4 => ParticleColorRangeMode.RandomColor,
				_ => ParticleColorRangeMode.Color
			},
			MaxColor = ReadColor(Dict(data, "maxColor")),
			MinColor = ReadColor(Dict(data, "minColor")),
			MaxGradient = ReadGradient(Dict(data, "maxGradient")),
			MinGradient = ReadGradient(Dict(data, "minGradient"))
		};
	}

	static ParticleCurve? ReadCurve(OrderedDictionary? data) {
		if (data?["m_Curve"] is not IList keys || keys.Count == 0)
			return null;

		var frames = new ParticleKeyframe[keys.Count];
		for (int i = 0; i < frames.Length; i++) {
			var key = (OrderedDictionary)keys[i]!;
			frames[i] = new(Float(key, "time"), Float(key, "value"), Float(key, "inSlope"), Float(key, "outSlope"));
		}
		return new(frames);
	}

	static ParticleGradient? ReadGradient(OrderedDictionary? data) {
		if (data == null)
			return null;

		int colorCount = Math.Clamp(Int(data, "m_NumColorKeys", 2), 0, 8);
		int alphaCount = Math.Clamp(Int(data, "m_NumAlphaKeys", 2), 0, 8);
		var colors = new ParticleColorKey[colorCount];
		var alphas = new ParticleAlphaKey[alphaCount];
		for (int i = 0; i < 8; i++) {
			var key = ReadColor(Dict(data, $"key{i}"));
			if (i < colorCount)
				colors[i] = new(Float(data, $"ctime{i}") / 65535f, new(key.X, key.Y, key.Z));
			if (i < alphaCount)
				alphas[i] = new(Float(data, $"atime{i}") / 65535f, key.W);
		}
		return new(colors, alphas, Int(data, "m_Mode") == 1);
	}

	static ParticleRange ReadRange(OrderedDictionary? data) {
		if (data == null)
			return ParticleRange.Constant(0);

		return new() {
			Mode = Int(data, "minMaxState") switch {
				1 => ParticleRangeMode.Curve,
				2 => ParticleRangeMode.RandomBetweenCurves,
				3 => ParticleRangeMode.RandomBetweenConstants,
				_ => ParticleRangeMode.Constant
			},
			Max = Float(data, "scalar"),
			Min = Float(data, "minScalar"),
			MaxCurve = ReadCurve(Dict(data, "maxCurve")),
			MinCurve = ReadCurve(Dict(data, "minCurve"))
		};
	}

	static bool TryResolve<T>(AssetStudio.Object owner, PPtr<T> pointer, Func<string, bool>? loadMissingFile, out T result) where T : AssetStudio.Object {
		if (pointer.TryGet(out result!))
			return true;
		if (loadMissingFile == null || pointer.m_PathID == 0 || pointer.m_FileID <= 0 || pointer.m_FileID > owner.assetsFile.m_Externals.Count)
			return false;
		return loadMissingFile(Path.GetFileName(owner.assetsFile.m_Externals[pointer.m_FileID - 1].fileName)) && pointer.TryGet(out result!);
	}

	static Vector4 ReadColor(OrderedDictionary? data) => data == null ? Vector4.One : new(Float(data, "r", 1), Float(data, "g", 1), Float(data, "b", 1), Float(data, "a", 1));

	static Vector3 ReadVector(OrderedDictionary? data, Vector3 fallback) => data == null ? fallback : new(Float(data, "x", fallback.X), Float(data, "y", fallback.Y), Float(data, "z", fallback.Z));

	static bool Bool(OrderedDictionary data, string key) => data.Contains(key) && System.Convert.ToBoolean(data[key]);

	static OrderedDictionary? Dict(OrderedDictionary data, string key) => data[key] as OrderedDictionary;

	static OrderedDictionary? Enabled(OrderedDictionary data, string key) => Dict(data, key) is OrderedDictionary module && Bool(module, "enabled") ? module : null;

	static float Float(OrderedDictionary data, string key, float fallback = 0) => data.Contains(key) ? System.Convert.ToSingle(data[key]) : fallback;

	static int Int(OrderedDictionary data, string key, int fallback = 0) => data.Contains(key) ? System.Convert.ToInt32(data[key]) : fallback;
}
