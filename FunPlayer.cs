using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;

namespace FunPvP {
	public class FunPlayer : ModPlayer {
		public HeldProjectile heldProjectile;
		public bool releaseUseItem;
		Vector2 preUpdateVel;
		public (sbyte x, sbyte y) collide;
		const sbyte yoteTime = 16;
		public (sbyte x, sbyte y) yoteTimeCollide;
		public override void ResetEffects() {
			heldProjectile.Update();
		}
		public override void PreUpdateMovement() {
			preUpdateVel = Player.velocity;
		}
		public static void SlopingCollision(On_Player.orig_SlopingCollision orig, Player self, bool fallThrough, bool ignorePlats) {
			orig(self, fallThrough, ignorePlats);
			sbyte x = 0, y = 0;
			FunPlayer funPlayer = self.GetModPlayer<FunPlayer>();
			if (Math.Abs(self.velocity.X) < 0.01f && Math.Abs(funPlayer.preUpdateVel.X) >= 0.01f) {
				x = (sbyte)Math.Sign(funPlayer.preUpdateVel.X);
				funPlayer.yoteTimeCollide.x = (sbyte)(x * yoteTime);
			}
			if (Math.Abs(self.velocity.Y) < 0.01f && Math.Abs(funPlayer.preUpdateVel.Y) >= 0.01f) {
				y = (sbyte)Math.Sign(funPlayer.preUpdateVel.Y);
				funPlayer.yoteTimeCollide.y = (sbyte)(y * yoteTime);
			}
			funPlayer.collide = (x, y);
		}
		public override bool PreItemCheck() {
			releaseUseItem = Player.releaseUseItem;
			return true;
		}
	}
}
