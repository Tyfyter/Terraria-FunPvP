global using Color = Microsoft.Xna.Framework.Color;
global using Rectangle = Microsoft.Xna.Framework.Rectangle;
global using Vector2 = Microsoft.Xna.Framework.Vector2;
global using Vector3 = Microsoft.Xna.Framework.Vector3;
global using Vector4 = Microsoft.Xna.Framework.Vector4;
using FunPvP.Core;
using System;
using Terraria;
using Terraria.ModLoader;

namespace FunPvP {
	[ReinitializeDuringResizeArrays]
	public class FunPvP : Mod {
		static FunPvP() {
			InputData.CreatePriorityList();
		}
		public override void Load() {
			On_Player.SlopingCollision += FunPlayer.SlopingCollision;
		}
	}
	public class FunPvPSystem : ModSystem {
		static readonly Projectile[,] projectilesByOwnerAndID = new Projectile[Main.maxPlayers + 1, Main.maxProjectiles];
		public static Projectile GetProjectile(int owner, int identity) => projectilesByOwnerAndID[owner, identity];
		public static bool TryGetProjectile(int owner, int identity, out Projectile projectile) {
			projectile = projectilesByOwnerAndID[owner, identity];
			return projectile is not null;
		}
		public override void PreUpdateProjectiles() {
			Array.Clear(projectilesByOwnerAndID);
			for (int i = 0; i < Main.maxProjectiles; i++) {
				Projectile projectile = Main.projectile[i];
				projectilesByOwnerAndID[projectile.owner, projectile.identity] = projectile;
			}
		}
	}
}
