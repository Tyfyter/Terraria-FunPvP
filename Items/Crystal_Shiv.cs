using FunPvP.Core;
using FunPvP.Networking;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PegasusLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace FunPvP.Items {
	public class Crystal_Shiv : ModItem {
		public override string Texture => "Terraria/Images/Item_" + ItemID.FalconBlade;
		public static int ID { get; private set; }
		public override void SetStaticDefaults() {
			ID = Type;
		}
		public override void SetDefaults() {
			Item.DefaultToMagicWeapon(ModContent.ProjectileType<Crystal_Shiv_P>(), 12, 5);
			Item.DamageType = DamageClass.Melee;
			Item.useStyle = ItemUseStyleID.RaiseLamp;
			Item.damage = 60;
			Item.knockBack = 6;
			Item.noUseGraphic = true;
			Item.width = 20;
			Item.height = 16;
			Item.rare = ItemRarityID.Quest;
			Item.maxStack = 1;
			Item.value = 0;
		}
		public override void HoldStyle(Player player, Rectangle heldItemFrame) {
			UseStyle(player, heldItemFrame);
		}
		public override void UseStyle(Player player, Rectangle heldItemFrame) {
			(bool enabled, Player.CompositeArmStretchAmount stretch, float rotation) arm = default;
			FunPlayer funPlayer = player.GetModPlayer<FunPlayer>();
			Player.CompositeArmStretchAmount stretchAmount;
			float armRotation;
			if (funPlayer.heldProjectile.CheckActive(out Projectile sword)) {
				if (sword.type == Item.shoot) {
					player.direction = Math.Sign((Main.MouseWorld - player.MountedCenter).X);
					if (((Crystal_Shiv_P)sword.ModProjectile).CurrentState.attack is not IdleState) {
						Vector2 diff = sword.Center - player.MountedCenter;
						stretchAmount = diff.LengthSquared() switch {
							>= 48 * 48 => Player.CompositeArmStretchAmount.Full,
							>= 32 * 32 => Player.CompositeArmStretchAmount.ThreeQuarters,
							>= 24 * 24 => Player.CompositeArmStretchAmount.Quarter,
							_ => Player.CompositeArmStretchAmount.None
						};
						armRotation = (diff * new Vector2(1, player.gravDir)).ToRotation() - MathHelper.PiOver2;
						arm = (true, stretchAmount, armRotation);
					}
				} else {
					sword.Kill();
					sword = null;
				}
			}
			if (player.whoAmI == Main.myPlayer && !player.CCed) {
				if (sword is null) {
					Projectile.NewProjectile(
						player.GetSource_ItemUse(Item),
						player.MountedCenter,
						default,
						Item.shoot,
						player.GetWeaponDamage(Item),
						player.GetWeaponKnockback(Item),
						player.whoAmI
					);
				}
			}
			player.SetCompositeArm(player.direction == -1, arm.stretch, arm.rotation, arm.enabled);
		}
		public override bool CanUseItem(Player player) => false;
	}
	public class Crystal_Shiv_P : PvPProjectile {
		public override string Texture => "Terraria/Images/Item_" + ItemID.FalconBlade;
		public override void SetStaticDefaults() {
			ProjectileID.Sets.TrailingMode[Type] = 2;
			ProjectileID.Sets.TrailCacheLength[Type] = 20;
			ProjectileID.Sets.CanDistortWater[Type] = true;
			AttackSlot repeatedlyStab = new(GetState<Flurry>());
			repeatedlyStab.combos.Add((InputData.GetBitMask<HitTarget>(), repeatedlyStab));
			StateTree = new AttackSlot(GetState<Idle>(),
				(InputData.GetBitMask<LeftClick>(), new(GetState<Neutral>())),
				(InputData.GetBitMask<LeftClick, Backward>(), new(GetState<Backstep>())),
				(InputData.GetBitMask<LeftClick, Up>(), new(GetState<Swipe_Up>())),
				(InputData.GetBitMask<RightClick>(), repeatedlyStab)
			);
		}
		public override void SetDefaults() {
			Projectile.width = Projectile.height = 28;
			Projectile.aiStyle = 0;
			Projectile.DamageType = DamageClass.Melee;
			Projectile.penetrate = -1;
			Projectile.extraUpdates = 1;
			Projectile.friendly = false;
			Projectile.ContinuouslyUpdateDamageStats = false;
			Projectile.tileCollide = false;
			Projectile.localNPCHitCooldown = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.ignoreWater = true;
			Projectile.hide = true;
		}
		const int timeForComboBefore = 15;
		const int timeForComboAfter = 15;
		public override void AI() {
			Projectile.friendly = false;
			base.AI();
		}
		public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac) => false;
		public override bool CanHitPvp(Player target) => Projectile.ai[0] != 0;
		public override bool PreDraw(ref Color lightColor) {
			Main.EntitySpriteDraw(
				TextureAssets.Projectile[Type].Value,
				Projectile.Center - Main.screenPosition,
				null,
				lightColor,
				Projectile.rotation + MathHelper.PiOver4,
				TextureAssets.Projectile[Type].Size() * new Vector2(0.65f, 0.35f),
				Projectile.scale,
				Projectile.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally
			);
			return false;
		}
		public override void DefaultAttackSetup(Player player) {
			GetStats(out float damage, out float useTime, out float knockback, out float armorPenetration);
			Projectile.damage = (int)damage;
			Projectile.knockBack = knockback;
			Projectile.ArmorPenetration = (int)armorPenetration;
			useTime = (int)useTime;
			Projectile.ai[1] = useTime;
			Projectile.ai[2] = useTime;
			Projectile.velocity = new Vector2(Math.Sign(Main.MouseWorld.X - player.MountedCenter.X), 0);
		}
		static float GetProgressScaled(float ai1, float ai2) {
			float progress = MathHelper.Clamp(1 - (ai1 / ai2), 0, 1);
			return MathHelper.Lerp(MathF.Pow(progress, 4f), MathF.Pow(progress, 0.25f), progress * progress);
		}
		static float GetDashSpeed(float progressScaled, float speed, float ratio = 0.8f) => (progressScaled * speed - progressScaled * progressScaled * speed * ratio);
		public class Idle : IdleState {
			public override void OnStart(Player player, PvPProjectile projectile, Attack previousState) {
				projectile.Projectile.ai[0] = 0;
				projectile.Projectile.ai[1] = 0;
				projectile.Projectile.ai[2] = 0;
			}
			public override void Update(Player player, PvPProjectile projectile) {
				projectile.Projectile.Center = player.MountedCenter;
			}
		}
		public class Neutral : Attack {
			public override void Update(Player player, PvPProjectile projectile) {
				Projectile Projectile = projectile.Projectile;
				player.heldProj = Projectile.whoAmI;
				player.velocity = (Projectile.ai[1] / Projectile.ai[2]) switch {
					> 0.9f or < 0.1f => player.velocity * 0.95f,
					_ => Projectile.velocity * 9
				};
				Projectile.Center = player.MountedCenter + Projectile.velocity * (GetProgressScaled(Projectile.ai[1], Projectile.ai[2]) + 1.5f) * 16;
				Projectile.rotation = Projectile.velocity.ToRotation();
				Projectile.friendly = true;
			}
			public override bool CheckFinished(Player player, PvPProjectile projectile, out bool canBuffer) {
				Projectile Projectile = projectile.Projectile;
				return ComboWindow(projectile, --Projectile.ai[1], timeForComboBefore, timeForComboAfter, out canBuffer);
			}
		}
		public class Backstep : Attack {
			public override void Update(Player player, PvPProjectile projectile) {
				Projectile Projectile = projectile.Projectile;
				player.heldProj = Projectile.whoAmI;
				float progress = GetProgressScaled(Projectile.ai[1], Projectile.ai[2]);
				if (Projectile.ai[1] > Projectile.ai[2] * 0.2f) {
					player.velocity.X *= 0.8f;
				} else if (player.GetModPlayer<FunPlayer>().collide.y != 0) {
					player.velocity.X -= player.direction * 4;
					player.velocity.Y -= 2;
				}
				Vector2 offset = Projectile.velocity.RotatedBy((progress * -2 + 1) * player.direction);
				Projectile.Center = player.MountedCenter + offset * 48;
				Projectile.rotation = offset.ToRotation();
				Projectile.friendly = true;
			}
			public override bool CheckFinished(Player player, PvPProjectile projectile, out bool canBuffer) {
				Projectile Projectile = projectile.Projectile;
				return ComboWindow(projectile, --Projectile.ai[1], timeForComboBefore, timeForComboAfter, out canBuffer);
			}
		}
		public class Swipe_Up : Attack {
			public override void Update(Player player, PvPProjectile projectile) {
				Projectile Projectile = projectile.Projectile;
				player.heldProj = Projectile.whoAmI;
				float progress = GetProgressScaled(Projectile.ai[1], Projectile.ai[2]);

				Vector2 offset = Projectile.velocity.RotatedBy((progress * 2 - 1 - MathHelper.PiOver2) * player.direction);
				Projectile.Center = player.MountedCenter + offset * 48;
				Projectile.rotation = offset.ToRotation();
				Projectile.friendly = true;
			}
			public override bool CheckFinished(Player player, PvPProjectile projectile, out bool canBuffer) {
				Projectile Projectile = projectile.Projectile;
				return ComboWindow(projectile, --Projectile.ai[1], timeForComboBefore, timeForComboAfter, out canBuffer);
			}
		}
		public class Flurry : Attack {
			public override void OnStart(Player player, PvPProjectile projectile, Attack previousState) {
				base.OnStart(player, projectile, previousState);
				Projectile Projectile = projectile.Projectile;
				Projectile.velocity = Projectile.velocity.RotatedByRandom(0.5f);
			}
			public override void Update(Player player, PvPProjectile projectile) {
				Projectile Projectile = projectile.Projectile;
				player.heldProj = Projectile.whoAmI;
				Projectile.Center = player.MountedCenter + Projectile.velocity * (GetProgressScaled(Projectile.ai[1], Projectile.ai[2]) + 1.5f) * 16;
				Projectile.rotation = Projectile.velocity.ToRotation();
				Projectile.friendly = true;
			}
			public override bool CheckFinished(Player player, PvPProjectile projectile, out bool canBuffer) {
				Projectile Projectile = projectile.Projectile;
				return ComboWindow(projectile, --Projectile.ai[1], timeForComboBefore, timeForComboAfter, out canBuffer);
			}
		}
	}
}
