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
		public override void AI() {
			Player player = Main.player[Projectile.owner];
			CurrentState.attack.Update(player, this);
			bool endAttack = CurrentState.attack.CheckFinished(player, this, out bool canBuffer);
			if (canBuffer && CurrentState.GetCombo(InputData.GetBitMask(player)) is AttackSlot nextAttack) {
				BufferedState = nextAttack;
			}
			if (endAttack) {
				CurrentState = BufferedState;
				BufferedState = null;
				currentState.attack.OnStart(player, this);
			}
		}
		public sealed override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers) {
			ModifyHit(target, new(ref modifiers));
		}
		public sealed override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
			ModifyHit(target, new(ref modifiers));
		}
		public sealed override void OnHitPlayer(Player target, Player.HurtInfo info) {
			OnHit(target, new(info));
		}
		public sealed override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			OnHit(target, new(hit));
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
				int oldAttack = currentState.attack.Type;
				currentState = new AttackSlot(Attack.byType[type]);
				bufferedState = new AttackSlot(Attack.byType[bufferType]);
				if (currentState.attack.Type != oldAttack) currentState.attack.OnStart(Main.player[Projectile.owner], this);
			}
		}
		public void GetStats(out float damage, out float useTime, out float knockback, out float armorPenetration) {
			Player player = Main.player[Projectile.owner];
			Item item = player.HeldItem;
			StatModifier damageMod = player.GetTotalDamage(item.DamageType);
			CombinedHooks.ModifyWeaponDamage(player, item, ref damageMod);
			StatModifier knockbackMod = player.GetTotalKnockback(item.DamageType);
			CombinedHooks.ModifyWeaponKnockback(player, item, ref knockbackMod);
			damage = damageMod.ApplyTo(item.damage);
			useTime = CombinedHooks.TotalAnimationTime(item.useAnimation, player, item);
			knockback = knockbackMod.ApplyTo(item.knockBack);
			armorPenetration = item.ArmorPenetration + player.GetTotalArmorPenetration(item.DamageType);
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
		public void Load(Mod mod) {
			if (mod.Side != ModSide.Both) throw new InvalidOperationException("Attacks can only be added by Both-side mods");
			Type = byType.Count;
			byType.Add(this);
		}
		public void Unload() { }
		/// <summary>
		/// Called on all sides when this attack is started
		/// </summary>
		public abstract void OnStart(Player player, PvPProjectile projectile);
		public abstract void Update(Player player, PvPProjectile projectile);
		public abstract bool CheckFinished(Player player, PvPProjectile projectile, out bool canBuffer);
		public virtual void OnHit(Entity target) { }
	}
	public abstract class IdleState : Attack {
		public override void OnStart(Player player, PvPProjectile projectile) { }
		public override bool CheckFinished(Player player, PvPProjectile projectile, out bool canBuffer) {
			canBuffer = true;
			return projectile.BufferedState.attack is not IdleState;
		}
	}
	public class AttackSlot(Attack attack, params (ulong inputMask, AttackSlot attack)[] combos) {
		public readonly Attack attack = attack;
		public AttackSlot GetCombo(ulong inputMask) {
			if (inputMask == 0) return null;
			ulong bestMatch = 0;
			AttackSlot bestAttack = null;
			for (int i = 0; i < combos.Length; i++) {
				ulong combined = combos[i].inputMask & inputMask;
				if (combined == combos[i].inputMask && (combined > bestMatch || bestAttack is null)) {
					bestMatch = combined;
					bestAttack = combos[i].attack;
				}
			}
			return bestAttack;
		}
	}
	public abstract class InputData : ILoadable {
		static readonly List<InputData> byType = [];
		static List<InputData> byPriority = [];
		public int Type { get; private set; }
		public int PriorityType { get; private set; }
		public abstract float Priority { get; }
		public abstract bool IsActive(Player player);
		public void Load(Mod mod) {
			if (mod.Side != ModSide.Both) throw new InvalidOperationException("InputData can only be added by Both-side mods");
			Type = byType.Count;
			byType.Add(this);
		}
		public void Unload() { }
		internal static void CreatePriorityList() {
			if (byType is null) return;
			byPriority = byType.OrderBy(x => x.Priority).ToList();
			for (int i = 0; i < byPriority.Count; i++) {
				byPriority[i].PriorityType = i;
			}
		}
		public static ulong GetBitMask(Player player) {
			ulong mask = 0;
			for (int i = 0; i < byPriority.Count; i++) {
				if (byPriority[i].IsActive(player)) mask |= 1ul << i;
			}
			return mask;
		}
		public static ulong GetBitMask(params InputData[] inputDatas) {
			ulong mask = 0;
			for (int i = 0; i < inputDatas.Length; i++) {
				mask |= 1ul << inputDatas[i].PriorityType;
			}
			return mask;
		}
		public static ulong GetBitMask<TInput1>() where TInput1 : InputData => GetBitMask(ModContent.GetInstance<TInput1>());
		public static ulong GetBitMask<TInput1, TInput2>() where TInput1 : InputData where TInput2 : InputData => GetBitMask(
			ModContent.GetInstance<TInput1>(),
			ModContent.GetInstance<TInput2>()
		);
		public static ulong GetBitMask<TInput1, TInput2, TInput3>() where TInput1 : InputData where TInput2 : InputData where TInput3 : InputData => GetBitMask(
			ModContent.GetInstance<TInput1>(),
			ModContent.GetInstance<TInput2>(),
			ModContent.GetInstance<TInput3>()
		);
	}
	public class LeftClick : InputData {
		public override float Priority { get; } = 9998;
		public override bool IsActive(Player player) => player.controlUseItem && player.GetModPlayer<FunPlayer>().releaseUseItem;
	}
	public class RightClick : InputData {
		public override float Priority { get; } = 9999;
		public override bool IsActive(Player player) => player.controlUseTile && player.releaseUseTile;
	}
	public class Forward : InputData {
		public override float Priority { get; } = 0;
		public override bool IsActive(Player player) => player.direction == -1 ? player.controlLeft : player.controlRight;
	}
	public class Backward : InputData {
		public override float Priority { get; } = 0;
		public override bool IsActive(Player player) => player.direction == -1 ? player.controlRight : player.controlLeft;
	}
	public class Up : InputData {
		public override float Priority { get; } = 0;
		public override bool IsActive(Player player) => player.controlUp;
	}
	public class Down : InputData {
		public override float Priority { get; } = 0;
		public override bool IsActive(Player player) => player.controlDown;
	}
	public class Air : InputData {
		public override float Priority { get; } = 1;
		public override bool IsActive(Player player) => player.GetModPlayer<FunPlayer>().collide.y == 0;
	}
}