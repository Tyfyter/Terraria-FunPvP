using Microsoft.Xna.Framework;
using Terraria;

namespace FunPvP {
	public static class Extensions {
		public static void SetCompositeArm(this Player player, bool leftSide, Player.CompositeArmStretchAmount stretch, float rotation, bool enabled) {
			if ((player.direction == 1) ^ leftSide) {
				player.SetCompositeArmFront(enabled, stretch, rotation);
			} else {
				player.SetCompositeArmBack(enabled, stretch, rotation);
			}
		}
		public static void GetCompositeArms(this Player player, out Player.CompositeArmData left, out Player.CompositeArmData right) {
			if (player.direction == 1) {
				left = player.compositeBackArm;
				right = player.compositeFrontArm;
			} else {
				right = player.compositeBackArm;
				left = player.compositeFrontArm;
			}
		}
		public static Vector2? GetCompositeArmPosition(this Player player, bool leftSide) {
			Vector2? pos = null;
			if (player.gravDir == -1) {
				if ((player.direction == 1) == leftSide) {
					float rotation = player.compositeBackArm.rotation - MathHelper.PiOver2;
					Vector2 offset = rotation.ToRotationVector2();
					switch (player.compositeBackArm.stretch) {
						case Player.CompositeArmStretchAmount.Full:
						offset *= new Vector2(10f, 12f);
						break;
						case Player.CompositeArmStretchAmount.None:
						offset *= new Vector2(4f, 6f);
						break;
						case Player.CompositeArmStretchAmount.Quarter:
						offset *= new Vector2(6f, 8f);
						break;
						case Player.CompositeArmStretchAmount.ThreeQuarters:
						offset *= new Vector2(8f, 10f);
						break;
					}
					if (player.direction == -1) {
						offset += new Vector2(-6f, 2f);
					} else {
						offset += new Vector2(6f, 2f);
					}
					pos = player.MountedCenter + offset;
				} else {
					Vector2 offset = new(-1, 3 * player.direction);
					switch (player.compositeFrontArm.stretch) {
						case Player.CompositeArmStretchAmount.Full:
						offset.X *= 10f;
						break;
						case Player.CompositeArmStretchAmount.None:
						offset.X *= 4f;
						break;
						case Player.CompositeArmStretchAmount.Quarter:
						offset.X *= 6f;
						break;
						case Player.CompositeArmStretchAmount.ThreeQuarters:
						offset.X *= 8f;
						break;
					}
					offset = offset.RotatedBy(player.compositeFrontArm.rotation + MathHelper.PiOver2);
					if (player.direction == -1) {
						offset += new Vector2(4f, 2f);
					} else {
						offset += new Vector2(-4f, 2f);
					}
					pos = player.MountedCenter + offset;
				}
			} else {
				if ((player.direction == 1) == leftSide) {
					if (player.compositeBackArm.enabled) pos = player.GetBackHandPosition(player.compositeBackArm.stretch, player.compositeBackArm.rotation);
				} else {
					if (player.compositeFrontArm.enabled) pos = player.GetFrontHandPosition(player.compositeFrontArm.stretch, player.compositeFrontArm.rotation);
				}
			}
			return pos;
		}
		public static void AddBuff(this Entity entity, int type, int timeToAdd) {
			if (entity is Player player) {
				player.AddBuff(type, timeToAdd, false);
			} else if (entity is NPC npc) {
				npc.AddBuff(type, timeToAdd, false);
			}
		}
	}
}
