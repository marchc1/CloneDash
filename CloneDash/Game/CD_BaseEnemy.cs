using CloneDash.Game.Entities;
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
        public CD_BaseEnemy(EntityType type) {
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
			return game.Add((CD_BaseEnemy)Activator.CreateInstance(type));
		}
	}
}
