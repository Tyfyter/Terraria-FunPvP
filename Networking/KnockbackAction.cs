using PegasusLib.Networking;
using System;
using System.IO;
using Terraria;

namespace FunPvP.Networking {
	public record class KnockbackAction(Entity Entity, Vector2 Velocity) : SyncedAction {
		public KnockbackAction() : this(default, default) { }
		public override SyncedAction NetReceive(BinaryReader reader) => this with {
			Entity = ((EntityType)reader.ReadByte()) switch {
				EntityType.Player => Main.player[reader.ReadByte()],
				EntityType.NPC => Main.npc[reader.ReadByte()],
				EntityType.Projectile => FunPvPSystem.GetProjectile(reader.ReadByte(), reader.ReadUInt16()),
				_ => throw new NotImplementedException()
			},
			Velocity = reader.ReadVector2()
		};
		public override void NetSend(BinaryWriter writer) {
			if (Entity is Player player) {
				writer.Write((byte)EntityType.Player);
				writer.Write((byte)player.whoAmI);
			} else if (Entity is NPC npc) {
				writer.Write((byte)EntityType.Player);
				writer.Write((byte)npc.whoAmI);
			} else if (Entity is Projectile projectile) {
				writer.Write((byte)EntityType.Player);
				writer.Write((byte)projectile.owner);
				writer.Write((ushort)projectile.identity);
			}
			writer.WriteVector2(Velocity);
		}
		protected override void Perform() {
			Entity.velocity = Velocity;
		}
		public enum EntityType : byte {
			Player,
			NPC,
			Projectile
		}
	}
}
