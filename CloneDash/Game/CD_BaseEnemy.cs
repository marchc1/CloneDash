using CloneDash.Game.Entities;
using Nucleus;
using Nucleus.Models.Runtime;
using System.Diagnostics.CodeAnalysis;

namespace CloneDash.Game
{
    public class CD_BaseEnemy : CD_BaseMEntity {
        public static Dictionary<EntityType, Type> TypeConvert { get; } = new() {
			{ EntityType.Single, typeof(SingleHitEnemy) },
			{ EntityType.Double, typeof(DoubleHitEnemy) },
            // { EntityType.Score, typeof(Score) },
            { EntityType.Hammer, typeof(Hammer) },
			{ EntityType.Masher, typeof(Masher) },
			{ EntityType.Gear, typeof(Gear) },
            // { EntityType.Ghost, typeof(Ghost) },
            { EntityType.Raider, typeof(Raider) },
            // { EntityType.Heart, typeof(Health) },
            { EntityType.SustainBeam, typeof(SustainBeam) },
		};
        protected CD_BaseEnemy(EntityType type) {
            Type = type;
        }

        public string DebuggingInfo { get; internal set; }

		public static bool TryCreateFromType(CD_GameLevel game, EntityType type, [NotNullWhen(true)] out CD_BaseEnemy? entity) {
			if(!TypeConvert.TryGetValue(type, out var ctype)) {
				entity = null;
				return false;
			}
			entity = CreateFromType(game, ctype);
			return true;
		}
		public static CD_BaseEnemy CreateFromType(CD_GameLevel game, EntityType type) => CreateFromType(game, TypeConvert[type]);
		public static CD_BaseEnemy CreateFromType(CD_GameLevel game, Type type) {
			var enemy = game.Add((CD_BaseEnemy)Activator.CreateInstance(type));
			return enemy;
		}


		public Nucleus.Models.Runtime.Animation? ApproachAnimation;
		public Nucleus.Models.Runtime.Animation? GreatHitAnimation;
		public Nucleus.Models.Runtime.Animation? PerfectHitAnimation;

		public double AnimationTime => Math.Max(0, (ShowTime - GetConductor().Time) * -1);
		private double tth => HitTime - ShowTime; // debugging, places enemy at exact frame position

		public virtual void DetermineAnimationPlayback() {
			ApproachAnimation?.Apply(Model, AnimationTime);
		}

		public override void Render() {
			if (!Visible) return;
			if (Model == null) return;
			if (AnimationTime == 0) return;

			DetermineAnimationPlayback();

			Model.Position = Position;
			Model.Scale = Scale;

			Model.Render();
		}
	}
}
