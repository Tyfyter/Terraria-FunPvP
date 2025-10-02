using Terraria;

namespace FunPvP {
	public struct HeldProjectile(int index) {
		public bool active = true;
		public int index = index;
		public int type = Main.projectile[index].type;
		public void Set(int index) {
			active = true;
			this.index = index;
			type = Main.projectile[index].type;
		}
		public bool CheckActive(out Projectile projectile) {
			Update();
			projectile = active ? Main.projectile[index] : null;
			return active;
		}
		public void Update() {
			if (!active) return;
			Projectile projectile = Main.projectile[index];
			if (!projectile.active || projectile.type != type) active = false;
		}
	}
}
