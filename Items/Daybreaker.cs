using FunPvP.Core;
using FunPvP.Networking;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PegasusLib;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace FunPvP.Items {
	public class Daybreaker : ModItem {
		public static int ID { get; private set; }
		public override void SetStaticDefaults() {
			ID = Type;
		}
		public override void SetDefaults() {
			Item.DefaultToMagicWeapon(ModContent.ProjectileType<Daybreaker_P>(), 34, 5);
			Item.DamageType = DamageClass.Melee;
			Item.useStyle = ItemUseStyleID.RaiseLamp;
			Item.damage = 111;
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
			(bool enabled, Player.CompositeArmStretchAmount stretch, float rotation) leftArm = default;
			(bool enabled, Player.CompositeArmStretchAmount stretch, float rotation) rightArm = default;
			FunPlayer funPlayer = player.GetModPlayer<FunPlayer>();
			Player.CompositeArmStretchAmount stretchAmount;
			float armRotation;
			if (funPlayer.heldProjectile.CheckActive(out Projectile sword)) {

				if (sword.ai[0] != 0) player.direction = sword.direction;
				else player.direction = Math.Sign((Main.MouseWorld - player.MountedCenter).X);
				stretchAmount = Player.CompositeArmStretchAmount.Full;
				armRotation = ((sword.position - (player.MountedCenter - new Vector2(0, 8))) * new Vector2(1, player.gravDir)).ToRotation() - MathHelper.PiOver2;
				rightArm = (true, stretchAmount, armRotation);
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
			player.SetCompositeArm(false, rightArm.stretch, rightArm.rotation, rightArm.enabled);
			player.SetCompositeArm(true, leftArm.stretch, leftArm.rotation, leftArm.enabled);
		}
		public override bool CanUseItem(Player player) => false;
	}
	public class Daybreaker_P : PvPProjectile {
		public override string Texture => typeof(Daybreaker).GetDefaultTMLName();
		public override void SetStaticDefaults() {
			ProjectileID.Sets.TrailingMode[Type] = 2;
			ProjectileID.Sets.TrailCacheLength[Type] = 20;
			ProjectileID.Sets.CanDistortWater[Type] = true;
			(ulong inputMask, AttackSlot attack) air1;
			StateTree = new AttackSlot(GetState<Idle>(),
				(InputData.GetBitMask<LeftClick>(), new(GetState<Neutral>(),
					(InputData.GetBitMask<LeftClick, Up>(), new(GetState<Spin>(),
						(InputData.GetBitMask<LeftClick, Air>(), new(GetState<Spin_Extend>()))
					))
				)),
				(InputData.GetBitMask<LeftClick, Backward>(), new(GetState<Back>())),
				(InputData.GetBitMask<LeftClick, Down>(), new(GetState<Slam>())),
				(InputData.GetBitMask<LeftClick, Up>(), new(GetState<Uppercut>(), 
					(InputData.GetBitMask<LeftClick, Air>(), new(GetState<Throw>()))
				)),
				air1 = (InputData.GetBitMask<LeftClick, Air>(), new(GetState<Aerial1>()))
			);
			air1.attack.combos.Add((InputData.GetBitMask<LeftClick, Air>(), new(GetState<Aerial2>(), air1)));
		}
		public override void SetDefaults() {
			Projectile.width = Projectile.height = 0;
			Projectile.aiStyle = 0;
			Projectile.DamageType = DamageClass.Melee;
			Projectile.penetrate = -1;
			Projectile.friendly = false;
			Projectile.ContinuouslyUpdateDamageStats = false;
			Projectile.tileCollide = false;
			Projectile.localNPCHitCooldown = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.ignoreWater = true;
			Projectile.hide = true;
		}
		float baseRotation;
		float swingDirectionCorrection;
		Vector2 restPoint;
		bool isHeld;
		bool doResetPoof;
		const int timeForComboBefore = 15;
		const int timeForComboAfter = 15;
		public override void AI() {
			Player player = Main.player[Projectile.owner];
			FunPlayer funPlayer = player.GetModPlayer<FunPlayer>();
			if (CurrentState.attack is IdleState) {
				Projectile.spriteDirection = player.direction;
			} else {
				player.direction = Projectile.spriteDirection = Math.Sign(Projectile.velocity.X);
			}
			(Vector2 pos, float rot) old = (Projectile.position, Projectile.rotation);
			Projectile.friendly = true;
			restPoint = player.MountedCenter + new Vector2(-28 * player.direction, 8 * player.gravDir);

			baseRotation = (MathHelper.PiOver2 + player.direction * (MathHelper.PiOver4 * 1.65f - Projectile.stepSpeed * 0.1f)) * player.gravDir;
			isHeld = true;
			doResetPoof = CurrentState.attack is not IdleState && Projectile.ai[1] == Projectile.ai[2];
			swingDirectionCorrection = player.direction * (int)player.gravDir;
			base.AI();
			if (isHeld) {
				player.SetCompositeArm(false, Player.CompositeArmStretchAmount.Full, (Projectile.rotation - MathHelper.PiOver2) * player.gravDir, true);
				Projectile.position = player.GetCompositeArmPosition(false).Value;
				if (player.direction == 1) player.heldProj = Projectile.whoAmI;
			}
			Projectile.rotation = (Projectile.rotation + MathHelper.TwoPi) % MathHelper.TwoPi;
			if (doResetPoof) {
				Projectile.ResetLocalNPCHitImmunity();
				if (Projectile.DistanceSQ(old.pos) > 8 * 8 || GeometryUtils.AngleDif(Projectile.rotation, old.rot) > 0.8f) {
					int dustType = DustID.Clentaminator_Red;
					Color dustColor = default;
					float dustScale = 0.5f;
					const int steps = 30;
					Vector2 vel = new Vector2(1, 0).RotatedBy(old.rot) * (65f / steps) * Projectile.scale;
					Vector2 pos = old.pos;
					for (int j = 0; j <= steps; j++) {
						Dust dust = Dust.NewDustPerfect(
							pos + Main.rand.NextVector2Square(-1, 1),
							dustType,
							Vector2.Zero,
							newColor: dustColor
						);
						dust.noGravity = true;
						dust.noLight = true;
						dust.velocity = Main.rand.NextVector2Circular(1, 1);
						dust.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
						dust.scale = dustScale;
						pos += vel;
					}
				}
			}
			if (isHeld && (player.dead || player.HeldItem.shoot != Type)) {
				Projectile.position = default;
				Projectile.Kill();
				return;
			}
			Projectile.timeLeft = 5;
			if (Projectile.localAI[2] > 0) {
				if (--Projectile.localAI[2] <= 0) {
					Projectile.localAI[0] = 0;
					Projectile.localAI[1] = 0;
				}
			} else if (Projectile.localAI[2] < 0) {
				if (++Projectile.localAI[2] >= 0) {
					Projectile.localAI[0] = 0;
					Projectile.localAI[1] = 0;
				}
			}
			funPlayer.heldProjectile.Set(Projectile.whoAmI);
			{
				const int steps = 5;
				Vector2 vel = new Vector2(1, 0).RotatedBy(Projectile.rotation) * (86f / steps) * Projectile.scale;
				Vector3 lightColor;
				lightColor = Color.OrangeRed.ToVector3() * 0.5f;
				for (int j = 1; j <= steps; j++) {
					Lighting.AddLight(Projectile.position + vel * j, lightColor);
				}
			}
		}
		public override void ModifyHit(Entity target, HitModifiers modifiers) {
			if (target is NPC npc && npc.type is NPCID.Vampire or NPCID.VampireBat) {
				modifiers.FinalDamage *= 3;
			}
		}
		public override void OnHit(Entity target, HitInfo hitInfo) {
			target.AddBuff(BuffID.OnFire3, Main.rand.Next(300, 480));
		}
		public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac) => false;
		public override bool CanHitPvp(Player target) => Projectile.ai[0] != 0;
		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
			const int steps = 5;
			Vector2 vel = new Vector2(1, 0).RotatedBy(Projectile.rotation) * (86f / steps) * Projectile.scale;
			projHitbox.Inflate(16, 16);
			for (int j = 1; j <= steps; j++) {
				Rectangle hitbox = projHitbox;
				Vector2 offset = vel * j;
				hitbox.Offset((int)offset.X, (int)offset.Y);
				if (hitbox.Intersects(targetHitbox)) {
					return true;
				}
			}
			return false;
		}
		public override bool PreDraw(ref Color lightColor) {
			Main.EntitySpriteDraw(
				TextureAssets.Projectile[Type].Value,
				Projectile.position - Main.screenPosition,
				null,
				lightColor,
				Projectile.rotation + (Projectile.spriteDirection == 1 ? 0 : MathHelper.PiOver2) + MathHelper.PiOver4,
				new Vector2(Projectile.spriteDirection == 1 ? 4 : (76 - 4), 72),
				Projectile.scale,
				Projectile.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally
			);
			DaybreakerSwingDrawer trailDrawer = default;
			trailDrawer.ColorStart = Color.Goldenrod;
			trailDrawer.ColorEnd = Color.OrangeRed * 0.5f;
			trailDrawer.Length = 86 * Projectile.scale;
			trailDrawer.Draw(Projectile);
			return false;
		}
		public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) {
			behindNPCsAndTiles.Add(index);
		}
		public void DoRestingPosition() {
			Player player = Main.player[Projectile.owner];
			Projectile.friendly = false;
			if (CurrentState != StateTree) {
				GeometryUtils.AngleDif(Projectile.rotation, baseRotation, out int oldDir);
				Projectile.rotation += (Projectile.oldRot[0] - Projectile.oldRot[1]) * 0.9f;
				GeometryUtils.AngleDif(Projectile.rotation, baseRotation, out int newDir);
				if (oldDir != newDir) Projectile.rotation = baseRotation;
				return;
			}
			Projectile.rotation += (float)GeometryUtils.AngleDif(Projectile.rotation, baseRotation) * 0.2f;

			Rectangle projHitbox = new((int)Projectile.position.X - 6, (int)Projectile.position.Y - 6, 12, 12);
			Rectangle resultHitbox = default;
			for (int i = 0; i < 4; i++) {
				const int length_steps = 5;
				bool hitGround = false;
				Vector2 vel = new Vector2(1, 0).RotatedBy(baseRotation + 0.1f * player.direction) * (86f / length_steps) * Projectile.scale;
				for (int j = 1; j <= length_steps; j++) {
					Rectangle hitbox = projHitbox;
					Vector2 offset = vel * j;
					hitbox.Offset((int)offset.X, (int)offset.Y);
					if (CollisionExt.OverlapsAnyTiles(hitbox)) {
						hitGround = true;
						resultHitbox = hitbox;
						break;
					}
				}
				if (hitGround) {
					Projectile.stepSpeed -= 0.25f;
					baseRotation += 0.25f * 0.1f * player.direction;
				} else {
					hitGround = false;
					vel = new Vector2(1, 0).RotatedBy(baseRotation - (Projectile.stepSpeed + 0.5f - 1f) * 0.1f * player.direction) * (86f / length_steps) * Projectile.scale;
					for (int j = 1; j <= length_steps; j++) {
						Rectangle hitbox = projHitbox;
						Vector2 offset = vel * j;
						hitbox.Offset((int)offset.X, (int)offset.Y);
						if (CollisionExt.OverlapsAnyTiles(hitbox)) {
							hitGround = true;
							resultHitbox = hitbox;
							break;
						}
					}
					if (!hitGround) {
						Projectile.stepSpeed += 0.25f;
						baseRotation -= 0.25f * 0.1f * player.direction;
					}
				}
			}
			if (Math.Abs(player.velocity.X) > 4f && resultHitbox != default) {
				Dust dust = Dust.NewDustDirect(
					resultHitbox.BottomLeft(),
					resultHitbox.Width,
					0,
					DustID.MinecartSpark
				);
				dust.noGravity = false;
				dust.velocity.Y -= 2;
			}
			Projectile.stepSpeed = MathHelper.Clamp(Projectile.stepSpeed, -1.5f, 1.5f);
		}
		public override void DefaultAttackSetup(Player player) {
			GetStats(out float damage, out float useTime, out float knockback, out float armorPenetration);
			Projectile.damage = (int)damage;
			Projectile.knockBack = knockback;
			Projectile.ArmorPenetration = (int)armorPenetration;
			useTime = (int)useTime;
			Projectile.ai[1] = useTime;
			Projectile.ai[2] = useTime;
			Projectile.velocity = Main.MouseWorld - player.MountedCenter;
			Projectile.velocity.Normalize();
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
				((Daybreaker_P)projectile).DoRestingPosition();
			}
		}
		public class Neutral : Attack {
			public override void Update(Player player, PvPProjectile projectile) {
				Projectile Projectile = projectile.Projectile;
				if (Projectile.ai[1] <= 0) {
					((Daybreaker_P)projectile).DoRestingPosition();
					return;
				}
				float baseRotation = ((Daybreaker_P)projectile).baseRotation;
				float swingDirectionCorrection = ((Daybreaker_P)projectile).swingDirectionCorrection;

				float progressScaled = GetProgressScaled(Projectile.ai[1], Projectile.ai[2]);
				Projectile.rotation = baseRotation + progressScaled * 5 * swingDirectionCorrection;
				if (Projectile.ai[1] != Projectile.ai[2]) {
					const float speed = 48;
					player.velocity.X += (GetDashSpeed(progressScaled, speed) - GetDashSpeed(GetProgressScaled(Projectile.ai[1] + 1, Projectile.ai[2]), speed)) * player.direction;
				} else Projectile.soundDelay = 1;

				Projectile.stepSpeed -= 0.05f * Math.Sign(Projectile.stepSpeed);
				if (Projectile.soundDelay == 1) {
					SoundEngine.PlaySound(SoundID.Item71, Projectile.position);
					SoundEngine.PlaySound(SoundID.DD2_BetsyFireballShot, Projectile.position);
				}
			}
			public override bool CheckFinished(Player player, PvPProjectile projectile, out bool canBuffer) {
				Projectile Projectile = projectile.Projectile;
				return ComboWindow(projectile, --Projectile.ai[1], timeForComboBefore, timeForComboAfter, out canBuffer);
			}
		}
		public class Back : Attack {
			public override void Update(Player player, PvPProjectile projectile) {
				Projectile Projectile = projectile.Projectile;
				if (Projectile.ai[1] <= 0) {
					((Daybreaker_P)projectile).DoRestingPosition();
					return;
				}
				float baseRotation = ((Daybreaker_P)projectile).baseRotation;
				float swingDirectionCorrection = ((Daybreaker_P)projectile).swingDirectionCorrection;

				float progressScaled = GetProgressScaled(Projectile.ai[1], Projectile.ai[2]);
				Projectile.rotation = baseRotation - MathHelper.Lerp(progressScaled * 0.25f, progressScaled, progressScaled) * 5f * swingDirectionCorrection;
				if (Projectile.ai[1] != Projectile.ai[2]) {
					const float speed = 24;
					player.velocity.X += (GetDashSpeed(progressScaled, speed) - GetDashSpeed(GetProgressScaled(Projectile.ai[1] + 1, Projectile.ai[2]), speed)) * player.direction;
				} else Projectile.soundDelay = 1;

				Projectile.stepSpeed -= 0.05f * Math.Sign(Projectile.stepSpeed);
				if (Projectile.soundDelay == 1) {
					SoundEngine.PlaySound(SoundID.Item71, Projectile.position);
					SoundEngine.PlaySound(SoundID.DD2_BetsyFireballShot, Projectile.position);
				}
			}
			public override bool CheckFinished(Player player, PvPProjectile projectile, out bool canBuffer) {
				Projectile Projectile = projectile.Projectile;
				return ComboWindow(projectile, --Projectile.ai[1], timeForComboBefore, timeForComboAfter, out canBuffer);
			}
			public override void OnHit(Entity target, PvPProjectile projectile, HitInfo hitInfo) {
				new KnockbackAction(target, target.velocity + Vector2.UnitX * hitInfo.HitDirection * (hitInfo.Knockback + projectile.Projectile.knockBack)).Perform();
			}
		}
		public class Slam : Attack {
			public override void OnStart(Player player, PvPProjectile projectile, Attack previousState) {
				base.OnStart(player, projectile, previousState);
				if (previousState is Neutral) player.velocity.X *= 0.5f;
			}
			public override void Update(Player player, PvPProjectile projectile) {
				Projectile Projectile = projectile.Projectile;
				float baseRotation = ((Daybreaker_P)projectile).baseRotation;
				float swingDirectionCorrection = ((Daybreaker_P)projectile).swingDirectionCorrection;
				if (Projectile.ai[0] == 1) {
					Projectile.rotation += swingDirectionCorrection * 0.001f;
					return;
				}
				if (Projectile.ai[1] <= 0) {
					((Daybreaker_P)projectile).DoRestingPosition();
					return;
				}

				float progressScaled = GetProgressScaled(Projectile.ai[1], Projectile.ai[2]);
				float oldProgressScaled = GetProgressScaled(Projectile.ai[1] + 1, Projectile.ai[2]);
				Projectile.rotation = baseRotation + MathHelper.Lerp(progressScaled * 0.4f, progressScaled, Math.Clamp(progressScaled - 0.5f, 0, 1) * 2f) * 5 * swingDirectionCorrection;
				if (progressScaled > 0.7f) {
					const int rotation_steps = 3;
					const int length_steps = 5;
					Rectangle projHitbox = new((int)Projectile.position.X - 6, (int)Projectile.position.Y - 6, 12, 12);
					float oldRot = Projectile.rotation;
					bool hitGround = false;
					Vector2 hitPos = default;
					for (int i = 0; i < rotation_steps; i++) {
						float stepRot = oldRot + (float)GeometryUtils.AngleDif(oldRot, Projectile.rotation) * (i + 1f) / rotation_steps;
						Vector2 vel = new Vector2(1, 0).RotatedBy(stepRot) * (86f / length_steps) * Projectile.scale;
						for (int j = 1; j <= length_steps; j++) {
							Rectangle hitbox = projHitbox;
							Vector2 offset = vel * j;
							hitbox.Offset((int)offset.X, (int)offset.Y);
							if (CollisionExt.OverlapsAnyTiles(hitbox)) {
								hitGround = true;
								hitPos = hitbox.Top();
								Projectile.rotation = stepRot;
								break;
							}
						}
					}
					if (hitGround) {
						int projType = ModContent.ProjectileType<Daybreaker_Floor_Fire>();
						Projectile lastFire = null;
						for (int i = 0; i < 18; i++) {
							int currentFire = Projectile.NewProjectile(
								Projectile.GetSource_FromAI(),
								hitPos + new Vector2((i - 1) * 24 * player.direction, -32),
								default,
								projType,
								Projectile.damage / 3,
								Projectile.knockBack,
								Projectile.owner,
								i
							);
							if (lastFire is not null) lastFire.ai[2] = currentFire;
							if (currentFire >= 0) lastFire = Main.projectile[currentFire];
						}
						Projectile.oldRot[0] = Projectile.rotation + swingDirectionCorrection * 0.002f;
						Projectile.ai[0] = 1;
						int extraTime = (int)(Projectile.ai[2] * 0.4f);
						Projectile.ai[1] += extraTime;
						Projectile.ai[2] += extraTime;

						SoundEngine.PlaySound(SoundID.DD2_BetsyFireballImpact, Projectile.position);
						return;
					}
				}
				if (Projectile.ai[1] != Projectile.ai[2]) {
					const float speed = 24;
					player.velocity.X += (GetDashSpeed(progressScaled, speed) - GetDashSpeed(GetProgressScaled(Projectile.ai[1] + 1, Projectile.ai[2]), speed)) * player.direction;
				} else Projectile.soundDelay = 1;

				Projectile.stepSpeed -= 0.05f * Math.Sign(Projectile.stepSpeed);
				if (Projectile.soundDelay == 1) {
					SoundEngine.PlaySound(SoundID.Item71, Projectile.position);
					SoundEngine.PlaySound(SoundID.DD2_BetsyFireballShot, Projectile.position);
				}
			}
			public override bool CheckFinished(Player player, PvPProjectile projectile, out bool canBuffer) {
				Projectile Projectile = projectile.Projectile;
				return ComboWindow(projectile, --Projectile.ai[1], timeForComboBefore, timeForComboAfter, out canBuffer);
			}
		}
		public class Uppercut : Attack {
			public override void Update(Player player, PvPProjectile projectile) {
				Projectile Projectile = projectile.Projectile;
				if (Projectile.ai[1] <= 0) {
					((Daybreaker_P)projectile).DoRestingPosition();
					return;
				}
				float baseRotation = ((Daybreaker_P)projectile).baseRotation;
				float swingDirectionCorrection = ((Daybreaker_P)projectile).swingDirectionCorrection;

				float progressScaled = GetProgressScaled(Projectile.ai[1], Projectile.ai[2]);
				Projectile.rotation = baseRotation - MathHelper.Lerp(progressScaled * 0.25f, progressScaled, Math.Clamp(progressScaled - 0.375f, 0, 1) * 1.6f) * 5 * swingDirectionCorrection;
				if (Projectile.ai[1] != Projectile.ai[2]) {
					float speed = progressScaled > 0.3f ? 196 : 0;
					player.velocity.Y -= (GetDashSpeed(progressScaled, speed) - GetDashSpeed(GetProgressScaled(Projectile.ai[1] + 1, Projectile.ai[2]), speed)) * player.gravDir;
					player.velocity.Y -= player.gravity * player.gravDir;
				}
				if (Projectile.ai[1] <= 2) Projectile.rotation = Projectile.oldRot[0] - swingDirectionCorrection * 0.1f;

				if (Projectile.ai[1] == Projectile.ai[2]) Projectile.soundDelay = (int)(Projectile.ai[2] * 0.4f);

				Projectile.stepSpeed -= 0.05f * Math.Sign(Projectile.stepSpeed);
				if (Projectile.soundDelay == 1) {
					SoundEngine.PlaySound(SoundID.Item71, Projectile.position);
					SoundEngine.PlaySound(SoundID.DD2_BetsyFireballShot, Projectile.position);
				}
			}
			public override bool CheckFinished(Player player, PvPProjectile projectile, out bool canBuffer) {
				Projectile Projectile = projectile.Projectile;
				return ComboWindow(projectile, --Projectile.ai[1], timeForComboBefore, timeForComboAfter + 1, out canBuffer);

			}
			public override void OnHit(Entity target, PvPProjectile projectile, HitInfo hitInfo) {
				new KnockbackAction(target, target.velocity + Vector2.UnitY * (hitInfo.Knockback + projectile.Projectile.knockBack) * 2f).Perform();
			}
		}
		public class Aerial1 : Attack {
			public override void Update(Player player, PvPProjectile projectile) {
				Projectile Projectile = projectile.Projectile;
				if (Projectile.ai[1] <= 0) {
					((Daybreaker_P)projectile).DoRestingPosition();
					return;
				}
				float baseRotation = ((Daybreaker_P)projectile).baseRotation;
				float swingDirectionCorrection = ((Daybreaker_P)projectile).swingDirectionCorrection;

				float progressScaled = GetProgressScaled(Projectile.ai[1], Projectile.ai[2]);

				Projectile.rotation = Projectile.velocity.ToRotation() + (progressScaled * 5 - 3) * swingDirectionCorrection;
				if (Projectile.ai[1] != Projectile.ai[2]) {
					const float speed = 80;
					player.velocity += Projectile.velocity * (GetDashSpeed(progressScaled, speed, 0.9f) - GetDashSpeed(GetProgressScaled(Projectile.ai[1] + 1, Projectile.ai[2]), speed, 0.9f));
					player.velocity.Y -= player.gravity * player.gravDir;
					if (player.GetModPlayer<FunPlayer>().collide.y != 0) player.velocity.Y = 0;
				} else Projectile.soundDelay = 1;

				Projectile.stepSpeed -= 0.05f * Math.Sign(Projectile.stepSpeed);
				if (Projectile.soundDelay == 1) {
					SoundEngine.PlaySound(SoundID.Item71, Projectile.position);
					SoundEngine.PlaySound(SoundID.DD2_BetsyFireballShot, Projectile.position);
				}
			}
			public override bool CheckFinished(Player player, PvPProjectile projectile, out bool canBuffer) {
				Projectile Projectile = projectile.Projectile;
				return ComboWindow(projectile, --Projectile.ai[1], timeForComboBefore, timeForComboAfter, out canBuffer);
			}
		}
		public class Aerial2 : Attack {
			public override void Update(Player player, PvPProjectile projectile) {
				Projectile Projectile = projectile.Projectile;
				if (Projectile.ai[1] <= 0) {
					((Daybreaker_P)projectile).DoRestingPosition();
					return;
				}
				float baseRotation = ((Daybreaker_P)projectile).baseRotation;
				float swingDirectionCorrection = ((Daybreaker_P)projectile).swingDirectionCorrection;

				float progressScaled = GetProgressScaled(Projectile.ai[1], Projectile.ai[2]);

				Projectile.rotation = Projectile.velocity.ToRotation() + (3 - progressScaled * 5) * swingDirectionCorrection;
				if (Projectile.ai[1] != Projectile.ai[2]) {
					const float speed = 40;
					player.velocity += Projectile.velocity * (GetDashSpeed(progressScaled, speed, 0.9f) - GetDashSpeed(GetProgressScaled(Projectile.ai[1] + 1, Projectile.ai[2]), speed, 0.9f));
					player.velocity.Y -= player.gravity * player.gravDir;
					if (player.GetModPlayer<FunPlayer>().collide.y != 0) player.velocity.Y = 0;
				} else Projectile.soundDelay = 1;

				Projectile.stepSpeed -= 0.05f * Math.Sign(Projectile.stepSpeed);
				if (Projectile.soundDelay == 1) {
					SoundEngine.PlaySound(SoundID.Item71, Projectile.position);
					SoundEngine.PlaySound(SoundID.DD2_BetsyFireballShot, Projectile.position);
				}
			}
			public override bool CheckFinished(Player player, PvPProjectile projectile, out bool canBuffer) {
				Projectile Projectile = projectile.Projectile;
				return ComboWindow(projectile, --Projectile.ai[1], timeForComboBefore, timeForComboAfter, out canBuffer);
			}
		}
		public class Throw : Attack {
			public override void Update(Player player, PvPProjectile projectile) {
				Projectile Projectile = projectile.Projectile;
				Daybreaker_P daybreaker = (Daybreaker_P)projectile;
				if (Projectile.ai[1] <= 0) {
					daybreaker.DoRestingPosition();
					return;
				}
				float baseRotation = daybreaker.baseRotation;
				float swingDirectionCorrection = daybreaker.swingDirectionCorrection;


				const float depth = 48;
				Projectile.rotation = Projectile.velocity.ToRotation();
				if (Projectile.ai[1] == Projectile.ai[2]) player.velocity -= Projectile.velocity * 8;
				Projectile.position += Projectile.velocity * depth;
				Rectangle projHitbox = new((int)Projectile.position.X - 6, (int)Projectile.position.Y - 6, 12, 12);
				if (!projHitbox.OverlapsAnyTiles()) {
					const float range = 15 * 16;
					if (Projectile.DistanceSQ(daybreaker.restPoint) < range * range && Projectile.ai[1] < Projectile.ai[2] - 1) {
						Projectile.ai[1]++;
					}
					for (int i = 0; i < 4; i++) {
						Projectile.position += Projectile.velocity * 8;
						projHitbox = new((int)Projectile.position.X - 6, (int)Projectile.position.Y - 6, 12, 12);
						if (projHitbox.OverlapsAnyTiles()) {
							SoundEngine.PlaySound(SoundID.Item14, Projectile.position);
							break;
						}
					}
				}
				Projectile.position -= Projectile.velocity * depth;
				if (Projectile.ai[1] == 1) {
					Projectile.rotation = baseRotation;
					Projectile.position = daybreaker.restPoint;
					daybreaker.doResetPoof = true;
				} else {
					daybreaker.isHeld = false;
				}
				if (Projectile.ai[1] == Projectile.ai[2]) Projectile.soundDelay = 1;

				Projectile.stepSpeed -= 0.05f * Math.Sign(Projectile.stepSpeed);
				if (Projectile.soundDelay == 1) {
					SoundEngine.PlaySound(SoundID.Item71, Projectile.position);
					SoundEngine.PlaySound(SoundID.DD2_BetsyFireballShot, Projectile.position);
				}
			}
			public override bool CheckFinished(Player player, PvPProjectile projectile, out bool canBuffer) {
				Projectile Projectile = projectile.Projectile;
				return ComboWindow(projectile, --Projectile.ai[1], timeForComboBefore, 0, out canBuffer);
			}
		}
		public class Spin : Attack {
			public override void OnStart(Player player, PvPProjectile projectile, Attack previousState) {
				Projectile Projectile = projectile.Projectile;
				projectile.GetStats(out float damage, out float useTime, out float knockback, out float armorPenetration);
				Projectile.damage = (int)damage;
				Projectile.knockBack = knockback;
				Projectile.ArmorPenetration = (int)armorPenetration;
				useTime = (int)(useTime * 1.5f);
				Projectile.ai[1] = useTime;
				Projectile.ai[2] = useTime;
				Projectile.velocity = Main.MouseWorld - player.MountedCenter;
				Projectile.velocity.Normalize();
			}
			public override void Update(Player player, PvPProjectile projectile) {
				Projectile Projectile = projectile.Projectile;
				if (Projectile.ai[1] <= 0) {
					((Daybreaker_P)projectile).DoRestingPosition();
					return;
				}
				float baseRotation = ((Daybreaker_P)projectile).baseRotation;
				float swingDirectionCorrection = ((Daybreaker_P)projectile).swingDirectionCorrection;

				if (Projectile.ai[1] == Projectile.ai[2]) player.velocity += new Vector2(6 * player.direction, -8);
				if (player.velocity.X * player.direction < 12) player.velocity.X = player.direction * 12;
				float spin = player.direction * 0.5f;
				Projectile.rotation += spin;
				if (Projectile.ai[2] - Projectile.ai[1] < 7) Projectile.rotation += spin;
				player.fullRotation += spin;
				player.fullRotationOrigin = player.Size * 0.5f;
				if (Projectile.ai[1] == 1) player.fullRotation = 0;
				if (Projectile.ai[1] == Projectile.ai[2]) Projectile.soundDelay = 1;
				else if (Projectile.soundDelay == 0) Projectile.soundDelay = 13;

				Projectile.stepSpeed -= 0.05f * Math.Sign(Projectile.stepSpeed);
				if (Projectile.soundDelay == 1) {
					SoundEngine.PlaySound(SoundID.Item71, Projectile.position);
					SoundEngine.PlaySound(SoundID.DD2_BetsyFireballShot, Projectile.position);
				}
			}
			public override bool CheckFinished(Player player, PvPProjectile projectile, out bool canBuffer) {
				Projectile Projectile = projectile.Projectile;
				return ComboWindow(projectile, --Projectile.ai[1], timeForComboBefore + 10, 5, out canBuffer);
			}
		}
		public class Spin_Extend : Attack {
			public override void OnStart(Player player, PvPProjectile projectile, Attack previousState) {
				Projectile Projectile = projectile.Projectile;
				projectile.GetStats(5 * 60, out float damage, out float useTime, out float knockback, out float armorPenetration);
				Projectile.damage = (int)damage;
				Projectile.knockBack = knockback;
				Projectile.ArmorPenetration = (int)armorPenetration;
				useTime = (int)useTime;
				Projectile.ai[1] = useTime;
				Projectile.ai[2] = useTime;
				Projectile.velocity = Main.MouseWorld - player.MountedCenter;
				Projectile.velocity.Normalize();
			}
			public override void Update(Player player, PvPProjectile projectile) {
				Projectile Projectile = projectile.Projectile;
				if (Projectile.ai[1] <= 0) {
					((Daybreaker_P)projectile).DoRestingPosition();
					return;
				}
				float baseRotation = ((Daybreaker_P)projectile).baseRotation;
				float swingDirectionCorrection = ((Daybreaker_P)projectile).swingDirectionCorrection;

				float spin = player.direction * 0.5f;
				Projectile.rotation += spin;
				player.fullRotation += spin;
				player.fullRotationOrigin = player.Size * 0.5f;
				player.wingTime = 0;
				if (Projectile.soundDelay == 0) Projectile.soundDelay = 13;
				if (player.GetModPlayer<FunPlayer>().collide.y == player.gravDir || Projectile.ai[1] == 1) {
					player.fullRotation = 0;
					bool doSlam = false;
					for (int i = 0; i < 5; i++) {
						if (Math.Sin(Projectile.rotation) < -0.5f) {
							projectile.CurrentState = new(ModContent.GetInstance<Slam>());
							projectile.CurrentState.attack.OnStart(player, projectile, this);
							doSlam = true;
							Projectile.ai[1] = (int)(Projectile.ai[2] * 0.4f);
							break;
						}
						Projectile.rotation += spin * 0.1f;
					}
					if (!doSlam && Projectile.ai[1] == 1) Projectile.ai[1]++;
				}

				Projectile.stepSpeed -= 0.05f * Math.Sign(Projectile.stepSpeed);
				if (Projectile.soundDelay == 1) {
					SoundEngine.PlaySound(SoundID.Item71, Projectile.position);
					SoundEngine.PlaySound(SoundID.DD2_BetsyFireballShot, Projectile.position);
				}
			}
			public override bool CheckFinished(Player player, PvPProjectile projectile, out bool canBuffer) {
				Projectile Projectile = projectile.Projectile;
				return ComboWindow(projectile, --Projectile.ai[1], timeForComboBefore, timeForComboAfter, out canBuffer);
			}
		}
	}
	public struct DaybreakerSwingDrawer {
		public const int TotalIllusions = 1;

		public const int FramesPerImportantTrail = 60;

		private static VertexStrip _vertexStrip = new();

		public Color ColorStart;

		public Color ColorEnd;

		public float Length;
		public readonly void Draw(Projectile proj) {
			if (proj?.ModProjectile is not Daybreaker_P sword || sword.CurrentState == sword.StateTree) return;
			MiscShaderData miscShaderData = GameShaders.Misc["FlameLash"];
			miscShaderData.UseSaturation(-1f);
			miscShaderData.UseOpacity(4);
			miscShaderData.Apply();
			int maxLength = Math.Min(proj.oldPos.Length, (int)(proj.ai[2] - proj.ai[1]));
			if (proj.ai[0] == 145) maxLength = proj.oldPos.Length;
			float[] oldRot = new float[maxLength];
			Vector2[] oldPos = new Vector2[maxLength];
			Vector2 move = new Vector2(Length * 0.65f, 0) * proj.direction;
			GeometryUtils.AngleDif(proj.oldRot[1], proj.oldRot[0], out int dir);
			for (int i = 0; i < maxLength; i++) {
				oldRot[i] = proj.oldRot[i] + MathHelper.PiOver4 * 2 + MathHelper.PiOver2 * (1 - dir);
				oldPos[i] = proj.oldPos[i] + move.RotatedBy(oldRot[i] - MathHelper.PiOver2 * proj.direction) * dir;
			}
			//spriteDirections = proj.oldSpriteDirection;
			_vertexStrip.PrepareStrip(oldPos, oldRot, StripColors, StripWidth, -Main.screenPosition, maxLength, includeBacksides: false);
			_vertexStrip.DrawTrail();
			Main.pixelShader.CurrentTechnique.Passes[0].Apply();
		}

		private readonly Color StripColors(float progressOnStrip) {
			Color result = Color.Lerp(ColorStart, ColorEnd, Utils.GetLerpValue(0f, 0.7f, progressOnStrip, clamped: true)) * (1f - Utils.GetLerpValue(0f, 0.98f, progressOnStrip, clamped: true));
			result.A /= 2;
			//result *= spriteDirections[Math.Max((int)(progressOnStrip * spriteDirections.Length) - 1, 0)];
			return result;
		}

		private readonly float StripWidth(float progressOnStrip) {
			return Length;
		}
	}
	public class Daybreaker_Floor_Fire : ModProjectile {
		public override string Texture => typeof(Daybreaker).GetDefaultTMLName();
		public override void SetDefaults() {
			Projectile.DamageType = DamageClass.Melee;
			Projectile.friendly = false;
			Projectile.width = 32;
			Projectile.height = 8;
			Projectile.penetrate = -1;
			Projectile.usesIDStaticNPCImmunity = true;
			Projectile.idStaticNPCHitCooldown = 25;
			Projectile.hide = true;
			Projectile.tileCollide = false;
		}
		public override void AI() {
			if (Projectile.ai[0] >= 0) {
				Projectile.ai[0] -= 1f;
				if (Projectile.ai[0] < 0) {
					int tries = 96;
					while (!CollisionExt.OverlapsAnyTiles(Projectile.Hitbox)) {
						Projectile.position.Y += 1;
						if (--tries <= 0) {
							Projectile.Kill();
							return;
						}
					}
					Player player = Main.player[Projectile.owner];
					ArmorShaderData dustShader = GameShaders.Armor.GetSecondaryShader(player.cBody, player);
					int nextFireIndex = (int)Projectile.ai[2];
					if (nextFireIndex >= 0) {
						Projectile nextFire = Main.projectile[nextFireIndex];
						if (nextFire.type == Type && nextFire.ai[0] > 0) {
							nextFire.position.Y = Projectile.position.Y - 32;
						}
					}
					Projectile.position.Y -= 1;
					Projectile.friendly = true;
					Projectile.timeLeft = 45;
					for (int i = 0; i < 8; i++) {
						Dust dust = Dust.NewDustDirect(
							Projectile.position,
							Projectile.width,
							Projectile.height,
							Utils.SelectRandom(Main.rand, 6, 259, 158)
						);
						dust.velocity.Y -= 2;
						dust.shader = dustShader;
					}
				}
			} else {
				if (Projectile.ai[1] < 0.6f) Projectile.ai[1] += 0.05f;
			}
			Projectile.noEnchantmentVisuals = Projectile.hide;
		}
		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
			if (Projectile.ai[1] > 0.1f) {
				modifiers.SourceDamage /= 2f;
				modifiers.DisableKnockback();
			} else modifiers.HitDirectionOverride = 0;
			if (target.type is NPCID.Vampire or NPCID.VampireBat) modifiers.FinalDamage *= 3;
		}
		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			if (Projectile.ai[1] <= 0.1f) {
				int sign = Math.Sign(target.knockBackResist);
				target.velocity = Vector2.Lerp(target.velocity, new Vector2(0, -hit.Knockback), MathF.Pow(target.knockBackResist * sign, 0.5f) * sign * 2);
			}
			target.AddBuff(BuffID.OnFire3, 60);
		}
		public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) {
			behindNPCsAndTiles.Add(index);
		}
		private static VertexStrip _vertexStrip = new();
		public Color ColorStart;
		public Color ColorEnd;
		public override bool PreDraw(ref Color lightColor) {
			Player owner = Main.player[Projectile.owner];
			ColorStart = Color.Goldenrod;
			ColorEnd = Color.OrangeRed * 0.5f;
			if (Projectile.ai[0] < 0) {
				MiscShaderData miscShaderData = GameShaders.Misc["FlameLash"];
				miscShaderData.UseSaturation(-1f);
				miscShaderData.UseOpacity(4);
				miscShaderData.Apply();
				int maxLength = 8;
				float[] oldRot = new float[maxLength];
				Vector2[] oldPos = new Vector2[maxLength];
				float sectionLength = 8 / (Projectile.ai[1] + 0.4f);
				for (int i = 0; i < maxLength; i++) {
					oldRot[i] = MathHelper.PiOver2;
					oldPos[i] = Projectile.Center - Vector2.UnitY * sectionLength * (i - 2);
				}
				_vertexStrip.PrepareStrip(oldPos, oldRot, StripColors, _ => 32, -Main.screenPosition, maxLength, includeBacksides: false);
				_vertexStrip.DrawTrail();
				Main.pixelShader.CurrentTechnique.Passes[0].Apply();
			}
			return false;
		}

		private Color StripColors(float progressOnStrip) {
			Color result = Color.Lerp(ColorStart, ColorEnd, Utils.GetLerpValue(0f, 0.7f, progressOnStrip, clamped: true)) * (1f - Utils.GetLerpValue(0f, 0.98f, progressOnStrip, clamped: true));
			result.A /= 2;
			return result;
		}
	}
}
