using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using static FunPvP.Items.Daybreaker_P;

namespace FunPvP.Core {
	public abstract class PvPProjectile : ModProjectile {
		protected override bool CloneNewInstances => true;
		/// <summary>
		/// Should begin with an <see cref="IdleState"/>
		/// </summary>
		[CloneByReference]
		public AttackSlot StateTree { get; protected set; }
		AttackSlot currentState;
		public AttackSlot CurrentState {
			get => currentState ??= StateTree;
			set {
				if (currentState != value) {
					currentState = value ?? StateTree;
					Projectile.netUpdate = true;
				}
			}
		}
		AttackSlot bufferedState;
		public AttackSlot BufferedState {
			get => bufferedState ??= StateTree;
			set {
				if (bufferedState != value) {
					bufferedState = value ?? StateTree;
					Projectile.netUpdate = true;
				}
			}
		}
		public override bool ShouldUpdatePosition() => false;
		public bool HitTarget { get; protected set; } = false;
		public override void AI() {
			Player player = Main.player[Projectile.owner];
			CurrentState.attack.Update(player, this);
			bool endAttack = CurrentState.attack.CheckFinished(player, this, out bool canBuffer);
			if (canBuffer && (CurrentState.GetCombo(InputData.GetBitMask(player, this)) ?? StateTree.GetCombo(InputData.GetBitMask(player, this))) is AttackSlot nextAttack) {
				BufferedState = nextAttack;
			}
			if (endAttack) {
				AttackSlot previousState = CurrentState;
				CurrentState = BufferedState;
				BufferedState = null;
				currentState.attack.OnStart(player, this, previousState.attack);
				HitTarget = false;
			}
			player.GetModPlayer<FunPlayer>().heldProjectile.Set(Projectile.whoAmI);
		}
		public sealed override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers) {
			ModifyHit(target, new(ref modifiers));
			CurrentState.attack.ModifyHit(target, this, new(ref modifiers));
		}
		public sealed override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
			ModifyHit(target, new(ref modifiers));
			CurrentState.attack.ModifyHit(target, this, new(ref modifiers));
		}
		public sealed override void OnHitPlayer(Player target, Player.HurtInfo info) {
			OnHit(target, new(info));
			CurrentState.attack.OnHit(target, this, new(info));
			HitTarget = true;
		}
		public sealed override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			OnHit(target, new(hit));
			CurrentState.attack.OnHit(target, this, new(hit));
			HitTarget = true;
		}
		public virtual void ModifyHit(Entity target, HitModifiers modifiers) { }
		public virtual void OnHit(Entity target, HitInfo hitInfo) { }
		public override void SendExtraAI(BinaryWriter writer) {
			writer.Write(currentState.attack.Type);
			writer.Write(bufferedState.attack.Type);
		}
		public override void ReceiveExtraAI(BinaryReader reader) {
			int type = reader.ReadInt32();
			int bufferType = reader.ReadInt32();
			if (Projectile.owner != Main.myPlayer) {
				Attack oldState = currentState.attack;
				currentState = Attack.byType[type].NetSlot;
				bufferedState = Attack.byType[bufferType].NetSlot;
				if (currentState.attack.Type != oldState.Type) currentState.attack.OnStart(Main.player[Projectile.owner], this, oldState);
			}
		}
		public virtual void DefaultAttackSetup(Player player) { }
		public void GetStats(out float damage, out float useTime, out float knockback, out float armorPenetration) => GetStats(Main.player[Projectile.owner].HeldItem.useAnimation, out damage, out useTime, out knockback, out armorPenetration);
		public void GetStats(int baseUseTime, out float damage, out float useTime, out float knockback, out float armorPenetration) {
			Player player = Main.player[Projectile.owner];
			Item item = player.HeldItem;
			StatModifier damageMod = player.GetTotalDamage(item.DamageType);
			CombinedHooks.ModifyWeaponDamage(player, item, ref damageMod);
			StatModifier knockbackMod = player.GetTotalKnockback(item.DamageType);
			CombinedHooks.ModifyWeaponKnockback(player, item, ref knockbackMod);
			damage = damageMod.ApplyTo(item.damage);
			useTime = CombinedHooks.TotalAnimationTime(baseUseTime, player, item);
			knockback = knockbackMod.ApplyTo(item.knockBack);
			armorPenetration = item.ArmorPenetration + player.GetTotalArmorPenetration(item.DamageType);
		}
		public void ResetIFrames() {
			Projectile.ResetLocalNPCHitImmunity();
			Array.Clear(Projectile.playerImmune);
		}
		public static TState GetState<TState>() where TState : Attack => ModContent.GetInstance<TState>();
	}
	public ref struct HitModifiers {
		public readonly bool isPlayer;
		public ref Player.HurtModifiers hurtModifiers;
		public ref NPC.HitModifiers hitModifiers;
		public HitModifiers(ref Player.HurtModifiers hurtModifiers) {
			isPlayer = true;
			this.hurtModifiers = ref hurtModifiers;
		}
		public HitModifiers(ref NPC.HitModifiers hitModifiers) {
			isPlayer = false;
			this.hitModifiers = ref hitModifiers;
		}
		public int HitDirection {
			readonly get => isPlayer ? hurtModifiers.HitDirection : hitModifiers.HitDirection;
			set {
				if (isPlayer) hurtModifiers.HitDirectionOverride = value;
				else hitModifiers.HitDirectionOverride = value;
			}
		}
		public ref StatModifier SourceDamage {
			get {
				if (isPlayer) return ref hurtModifiers.SourceDamage;
				return ref hitModifiers.SourceDamage;
			}
		}
		public ref StatModifier FinalDamage {
			get {
				if (isPlayer) return ref hurtModifiers.FinalDamage;
				return ref hitModifiers.FinalDamage;
			}
		}
		public ref StatModifier Knockback {
			get {
				if (isPlayer) return ref hurtModifiers.Knockback;
				return ref hitModifiers.Knockback;
			}
		}
	}
	public readonly struct HitInfo {
		public readonly bool isPlayer;
		public readonly Player.HurtInfo hurtInfo;
		public readonly NPC.HitInfo hitInfo;
		public HitInfo(Player.HurtInfo hurtInfo) {
			isPlayer = true;
			this.hurtInfo = hurtInfo;
		}
		public HitInfo(NPC.HitInfo hitInfo) {
			isPlayer = false;
			this.hitInfo = hitInfo;
		}
		public int SourceDamage => isPlayer ? hurtInfo.SourceDamage : hitInfo.SourceDamage;
		public int Damage => isPlayer ? hurtInfo.Damage : hitInfo.Damage;
		public int HitDirection => isPlayer ? hurtInfo.HitDirection : hitInfo.HitDirection;
		public float Knockback => isPlayer ? hurtInfo.Knockback : hitInfo.Knockback;
	}
	public abstract class Attack : ILoadable {
		public static readonly List<Attack> byType = [];
		public int Type { get; private set; }
		public AttackSlot NetSlot { get; private set; }
		public void Load(Mod mod) {
			if (mod.Side != ModSide.Both) throw new InvalidOperationException("Attacks can only be added by Both-side mods");
			Type = byType.Count;
			byType.Add(this);
			NetSlot = new(this);
		}
		public void Unload() { }
		/// <summary>
		/// Called on all sides when this attack is started
		/// </summary>
		public virtual void OnStart(Player player, PvPProjectile projectile, Attack previousState) {
			projectile.ResetIFrames();
			if (player.whoAmI != Main.myPlayer) return;
			projectile.DefaultAttackSetup(player);
		}
		public abstract void Update(Player player, PvPProjectile projectile);
		public abstract bool CheckFinished(Player player, PvPProjectile projectile, out bool canBuffer);
		public virtual void ModifyHit(Entity target, PvPProjectile projectile, HitModifiers modifiers) { }
		public virtual void OnHit(Entity target, PvPProjectile projectile, HitInfo hitInfo) { }
		public static bool ComboWindow(PvPProjectile projectile, float timer, float preBufferFrames, float hangTime, out bool canBuffer) {
			canBuffer = timer <= preBufferFrames;
			return timer <= -hangTime || (timer <= 0 && projectile.BufferedState.attack is not IdleState);
		}
	}
	public abstract class IdleState : Attack {
		public override void OnStart(Player player, PvPProjectile projectile, Attack previousState) { }
		public override bool CheckFinished(Player player, PvPProjectile projectile, out bool canBuffer) {
			canBuffer = true;
			return projectile.BufferedState != projectile.StateTree;
		}
	}
	public class AttackSlot(Attack attack, params (ulong inputMask, AttackSlot attack)[] combos) {
		public List<(ulong inputMask, AttackSlot attack)> combos = [..combos];
		public readonly Attack attack = attack;
		public AttackSlot GetCombo(ulong inputMask) {
			if (inputMask == 0) return null;
			ulong bestMatch = 0;
			AttackSlot bestAttack = null;
			for (int i = 0; i < combos.Count; i++) {
				ulong combined = combos[i].inputMask & inputMask;
				if (combined == combos[i].inputMask && (combined > bestMatch || bestAttack is null)) {
					bestMatch = combined;
					bestAttack = combos[i].attack;
				}
			}
			return bestAttack;
		}
	}
}