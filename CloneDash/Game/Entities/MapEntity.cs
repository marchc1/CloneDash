using Raylib_cs;

namespace CloneDash.Game.Entities
{
    public abstract class MapEntity
    {

        public static Dictionary<EntityType, Type> TypeConvert = new() {
            { EntityType.Single, typeof(SingleHitEnemy) },
            { EntityType.Double, typeof(DoubleHitEnemy) },
            { EntityType.Score, typeof(Score) },
            { EntityType.Hammer, typeof(Hammer) },
            { EntityType.Masher, typeof(Masher) },
            { EntityType.Gear, typeof(Gear) },
            { EntityType.Ghost, typeof(Ghost) },
            { EntityType.Raider, typeof(Raider) },
            { EntityType.Heart, typeof(Health) },
            { EntityType.SustainBeam, typeof(SustainBeam) },
        };
        public static MapEntity CreateFromType(DashGame game, EntityType type) {
            return (MapEntity)Activator.CreateInstance(TypeConvert[type], game);
        }

        /// <summary>
        /// The current game this entity belongs to
        /// </summary>
        public DashGame Game { get; private set; }

        /// <summary>
        /// Type of the entity
        /// </summary>
        public EntityType Type { get; set; } = EntityType.Unknown;

        /// <summary>
        /// The entity's texture. Not strictly followed, some entities have forced textures
        /// </summary>
        public Texture2D Texture { get; set; }

        /// <summary>
        /// Does the death of this entity add to the characters combo score?
        /// </summary>
        public bool DeathAddsToCombo { get; protected set; } = true;

        /// <summary>
        /// Does the failure to kill/pass this entity damage the player?
        /// </summary>
        public bool DoesDamagePlayer { get; protected set; } = true;

        /// <summary>
        /// Has the player been damaged already?<br></br>
        /// Used internally to avoid applying damage over and over again
        /// </summary>
        public bool DidDamagePlayer { get; private set; } = false;

        /// <summary>
        /// Damages the player as a punishment (which also resets their combo)
        /// </summary>
        public void DamagePlayer() {
            if (DidDamagePlayer) // Is the player already hurt
                return;

            if (!DoesDamagePlayer) // Does the entity damage the player
                return;

            if (Game.InMashState) // Is the player mashing an entity right now and can't even hit the entity anyway
                return;

            PunishPlayer(); // Reset combo
            Game.PlayerController.Damage(this, DamageTaken);
            DidDamagePlayer = true;
        }

        /// <summary>
        /// Does failure to kill the entity cause a combo loss?
        /// </summary>
        public bool DoesPunishPlayer { get; protected set; } = true;
        /// <summary>
        /// Has the entity punished the player yet?
        /// </summary>
        public bool DidPunishPlayer { get; private set; } = false;

        /// <summary>
        /// Resets the players combo as a punishment
        /// </summary>
        public void PunishPlayer() {
            if (DidPunishPlayer) // Was the player punished
                return;

            if (!DoesPunishPlayer) // Does the entity punish the player
                return;

            if (Game.InMashState) // Is the player in a mash state
                return;

            OnPunishment();
            DidPunishPlayer = true;
        }
        protected virtual void OnPunishment() {
            Game.PlayerController.ResetCombo();
        }

        /// <summary>
        /// Has the player been rewarded yet?
        /// </summary>
        public bool DidRewardPlayer { get; private set; }

        /// <summary>
        /// Does the killing of this entity reward the player, either with healing or score?
        /// </summary>
        public bool DoesRewardPlayer { get; private set; } = true;

        /// <summary>
        /// How much health does the entity give (if any)
        /// </summary>
        public float HealthGiven { get; set; }

        /// <summary>
        /// How much score does the entity give to the player?
        /// </summary>
        public int ScoreGiven { get; set; } = 0;

        public void RewardPlayer(bool heal = false) {
            if (DidRewardPlayer) // Did the entity reward the player already
                return;

            if (!DoesRewardPlayer) // Does the entity reward the player
                return;

            if (Game.InMashState) // Is the player mashing an entity
                return;

            if (heal)
                Game.PlayerController.Heal(HealthGiven);

            OnReward();
            DidRewardPlayer = true;

            //Game.GameplayManager.SpawnTextEffect("PASS", color: new Color(200,200,200,255));
        }

        protected virtual void OnReward() {
            Game.PlayerController.AddScore(ScoreGiven);
        }

        /// <summary>
        /// How much damage does the player take if failing to kill/pass this entity.
        /// </summary>
        public float DamageTaken { get; set; }

        /// <summary>
        /// How much fever does the player get when killing/passing this entity.
        /// </summary>
        public float FeverGiven { get; set; }

        /// <summary>
        /// The low-end range of when a hit/pass is considered "great". <br></br><br></br> <i>Note that this considered to be a positive value.</i>
        /// </summary>
        public float PreGreatRange { get; set; } = 0.08f;
        /// <summary>
        /// The high-end range of when a hit/pass is considered "great". <br></br><br></br> <i>Note that this considered to be a positive value.</i>
        /// </summary>
        public float PostGreatRange { get; set; } = 0.08f;

        /// <summary>
        /// The low-end range of when a hit/pass is considered "perfect". <br></br><br></br> <i>Note that this considered to be a positive value.</i>
        /// </summary>
        public float PrePerfectRange { get; set; } = 0.05f;
        /// <summary>
        /// The high-end range of when a hit/pass is considered "perfect". <br></br><br></br> <i>Note that this considered to be a positive value.</i>
        /// </summary>
        public float PostPerfectRange { get; set; } = 0.05f;

        /// <summary>
        /// Should the entity draw?<br></br> This overrides ForceDraw. Naming is weird, needs to be adjusted.
        /// </summary>
        public bool ShouldDraw { get; protected set; } = true;
        /// <summary>
        /// Forces the entity to draw to the screen even if it would fail a visibility test.<br></br>Note that ShouldDraw will override this value.
        /// </summary>
        public bool ForceDraw { get; protected set; } = false;

        /// <summary>
        /// Is the entity invisible. The difference between this and Should/ForceDraw is that this just doesnt call the drawing function, but still allows it to pass visibility testing.
        /// </summary>
        public bool Invisible { get; set; } = false;
        /// <summary>
        /// The interactivity method of this entity. Different methods of the entity will be called based on this value.
        /// </summary>
        public EntityInteractivity Interactivity { get; set; } = EntityInteractivity.Noninteractive;
        /// <summary>
        /// Is the entity interactive?
        /// </summary>
        public bool Interactive => Interactivity != EntityInteractivity.Noninteractive;
        /// <summary>
        /// Which direction does the entity come in from. Note that this only applies to some entities.
        /// </summary>
        public EntityEnterDirection EnterDirection { get; set; }
        /// <summary>
        /// What pathway is this entity on
        /// </summary>
        public PathwaySide Pathway { get; set; }

        /// <summary>
        /// When does this entity first appear on the screen, in seconds
        /// </summary>
        public double ShowTime { get; set; }
        /// <summary>
        /// When does this entity need to be hit, in seconds
        /// </summary>
        public double HitTime { get; set; }
        /// <summary>
        /// How long does this entity need to be hit/sustained, in seconds
        /// </summary>
        public double Length { get; set; }
        /// <summary>
        /// Unused
        /// </summary>
        public double Speed { get; set; }

        public bool TellBossWhenSpawned { get; set; } = false;

        public MapEntity(DashGame game, EntityType type) {
            Game = game;
            Type = type;
        }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private MapEntity() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        /// <summary>
        /// Optional method entities can use to adjust their drawing position.
        /// </summary>
        /// <param name="pos"></param>
        public virtual void ChangePosition(ref Vector2F pos) {

        }
        /// <summary>
        /// Note: Not strictly adhered to
        /// </summary>
        public Vector2F TextureSize { get; set; } = new(64, 64);

        /// <summary>
        /// The drawing function. Drawn in game-space.
        /// </summary>
        /// <param name="idealPosition"></param>
        public virtual void Draw(Vector2F idealPosition) {
            Graphics.DrawRectangleOutline(idealPosition - (TextureSize / 2), TextureSize, 2);
        }
        /// <summary>
        /// Is the entity dead?
        /// </summary>
        public bool Dead { get; private set; } = false;
        /// <summary>
        /// Is the entity marked for removal from the entities list?
        /// </summary>
        public bool MarkedForRemoval { get; set; } = false;

        /// <summary>
        /// Does the entity warn the player when it is visible?
        /// </summary>
        public bool Warns { get; set; } = false;

        /// <summary>
        /// Kills the entity, which removes a lot of functionality from the entity. Will also mark down FinalBlow time and the Dead field.
        /// </summary>
        public void Kill() {
            Dead = true;

            if (DeathAddsToCombo)
                Game.PlayerController.AddCombo();

            Game.PlayerController.AddFever((int)this.FeverGiven);

            FinalBlow = DateTime.Now;
            RewardPlayer();
        }

        /// <summary>
        /// The distance, in seconds, to when the entity needs to be hit. A negative value means that the player hit too late, a positive means the player hit too early.
        /// </summary>
        public double DistanceToHit => HitTime - Game.Conductor.Time;

        /// <summary>
        /// The distance, in seconds, to when the entity needs to be released.
        /// </summary>
        public double DistanceToEnd => (HitTime + Length) - Game.Conductor.Time;

        /// <summary>
        /// Where is the entity in game-space?
        /// </summary>
        public double XPos { get; protected set; }

        public double XPosFromTimeOffset(float timeOffset = 0) {
            var current = Game.Conductor.Time - timeOffset;
            var tickHit = this.HitTime;
            var tickShow = this.ShowTime;
            var thisPos = DashMath.Remap(current, (float)tickHit, (float)tickShow, Game.ScreenManager.ScrWidth * DashVars.PATHWAY_XDISTANCE, Game.ScreenManager.ScrWidth);

            return thisPos;
        }

        public bool Shown { get; protected set; } = false;

        public bool CheckVisTest() {
            XPos = XPosFromTimeOffset();
            float w = Game.ScreenManager.ScrWidth, h = Game.ScreenManager.ScrHeight;

            var ret = VisTest(w, h, (float)XPos);
            if (Shown == false && ret == true) {
                Shown = true;

                if (RelatedToBoss)
                    Game.Boss.OnEntityShown(this);
            }
            return ret;
        }

        public virtual bool VisTest(float gamewidth, float gameheight, float xPosition) {
            return xPosition >= -500 && xPosition <= gamewidth + 500;
        }

        /// <summary>
        /// Overridden method for when the entity is hit. Applicable to Hit, Avoid, and Sustain interactivity types.
        /// </summary>
        protected virtual void OnHit(PathwaySide side) {

        }
        protected virtual void OnMiss() {

        }
        /// <summary>
        /// Overridden method for when the entity is passed by. Applicable to the SamePath and Avoid interactivity types.
        /// </summary>
        protected virtual void OnPass() {

        }
        /// <summary>
        /// Overridden method for when the entity is released. Only applicable to the Sustain interactivity type.
        /// </summary>
        protected virtual void OnRelease() {

        }

        public delegate void EntityPathwayEvent(MapEntity entity, PathwaySide side);
        public delegate void EntityNoArgumentEvent(MapEntity entity);

        /// <summary>
        /// Per-entity event hook for when an entity is hit.
        /// </summary>
        public event EntityPathwayEvent OnHitEvent;
        public event EntityNoArgumentEvent OnMissEvent;
        /// <summary>
        /// Per-entity event hook for when an entity is passed.
        /// </summary>
        public event EntityNoArgumentEvent OnPassEvent;
        /// <summary>
        /// Per-entity event hook for when an entity is released.
        /// </summary>
        public event EntityNoArgumentEvent OnReleaseEvent;

        /// <summary>
        /// Global event hook for when an entity is hit.
        /// </summary>
        public static event EntityNoArgumentEvent GlobalOnHitEvent;
        /// <summary>
        /// Global event hook for when the player misses an entity.
        /// </summary>
        public static event EntityNoArgumentEvent GlobalOnMissEvent;
        /// <summary>
        /// Global event hook for when an entity is passed.
        /// </summary>
        public static event EntityNoArgumentEvent GlobalOnPassEvent;
        /// <summary>
        /// Global event hook for when an entity is released.
        /// </summary>
        public static event EntityNoArgumentEvent GlobalOnReleaseEvent;

        public int Hits { get; set; } = 0;
        public void Hit(PathwaySide pathway) {
            Hits++;
            OnHit(pathway);
            OnHitEvent?.Invoke(this, pathway);
            GlobalOnHitEvent?.Invoke(this);
        }
        public bool DidMiss { get; private set; }
        public void Miss() {
            if (DidMiss)
                return;
            ConsoleSystem.Print("Miss");

            OnMiss();
            OnMissEvent?.Invoke(this);
            GlobalOnMissEvent?.Invoke(this);
            DidMiss = true;
        }

        public bool DidPass { get; private set; }
        public void Pass() {
            if (DidPass)
                return;

            Game.GameplayManager.SpawnTextEffect("PASS", Game.GetPathway(Pathway).Position, new Color(235, 235, 235, 255));
            OnPass();
            OnPassEvent?.Invoke(this);
            GlobalOnPassEvent?.Invoke(this);

            DidPass = true;
        }

        public void Release() {
            OnRelease();
            OnReleaseEvent?.Invoke(this);
            GlobalOnReleaseEvent?.Invoke(this);
        }


        /// <summary>
        /// This is called every frame, but ONLY when the entity is visible. See WhenFrame() for an every-frame approach
        /// </summary>
        public virtual void WhenVisible() { }
        /// <summary>
        /// This is called every frame, even if the entity isn't visible. See WhenVisible() for an only-when-visible approach
        /// </summary>
        public virtual void WhenFrame() { }
        public DateTime Created { get; private set; } = DateTime.Now;
        public double Lifetime => (DateTime.Now - Created).TotalSeconds;

        public DateTime FinalBlow { get; private set; } = DateTime.MinValue;
        public float SinceDeath => Dead ? (float)(DateTime.Now - FinalBlow).TotalSeconds : 0;

        public void DrawSpriteBasedOnPathway(Texture2D tex, RectangleF rect, Vector2F? origin = null, float rotation = 0, PathwaySide? pathway = null) {
            var p = Pathway;
            if (pathway != null)
                p = pathway.Value;

            var hue = p == PathwaySide.Top ? 200 : 285;
            Graphics.DrawImage(tex, rect, origin, rotation, hsvTransform: new(hue, 1, 1));
        }

        public virtual void Build() {

        }

        public bool RelatedToBoss { get; set; }

        private Color? __hitColor;
        public Color HitColor {
            get { return __hitColor.HasValue ? __hitColor.Value : Game.GetPathway(Pathway).Color; }
            set { __hitColor = value; }
        }
    }
}
