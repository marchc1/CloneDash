using CloneDash.Game.Entities;

namespace CloneDash.Game
{
    public class CD_BaseEnemy : CD_BaseMEntity {
        public static Dictionary<EntityType, Type> TypeConvert = new() {
            { EntityType.Single, typeof(SingleHitEnemy) },
            { EntityType.Double, typeof(DoubleHitEnemy) },
            // { EntityType.Score, typeof(Score) },
            { EntityType.Hammer, typeof(Hammer) },
            // { EntityType.Masher, typeof(Masher) },
            { EntityType.Gear, typeof(Gear) },
            // { EntityType.Ghost, typeof(Ghost) },
            // { EntityType.Raider, typeof(Raider) },
            // { EntityType.Heart, typeof(Health) },
            { EntityType.SustainBeam, typeof(SustainBeam) },
        };
        public CD_BaseEnemy(EntityType type) {
            Type = type;
        }

        public static CD_BaseEnemy CreateFromType(CD_GameLevel game, EntityType type) {
            return game.Add((CD_BaseEnemy)Activator.CreateInstance(TypeConvert[type]));
        }
    }
}
