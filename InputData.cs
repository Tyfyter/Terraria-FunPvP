using FunPvP.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;

namespace FunPvP {
	public abstract class InputData : ILoadable {
		static readonly List<InputData> byType = [];
		static List<InputData> byPriority = [];
		public int Type { get; private set; }
		public int PriorityType { get; private set; }
		public abstract float Priority { get; }
		public abstract bool IsActive(Player player, PvPProjectile projectile);
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
		public static ulong GetBitMask(Player player, PvPProjectile projectile) {
			ulong mask = 0;
			for (int i = 0; i < byPriority.Count; i++) {
				if (byPriority[i].IsActive(player, projectile)) mask |= 1ul << i;
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
		public override float Priority { get; } = 1000;
		public override bool IsActive(Player player, PvPProjectile projectile) => player.controlUseItem && player.GetModPlayer<FunPlayer>().releaseUseItem;
	}
	public class RightClick : InputData {
		public override float Priority { get; } = 1001;
		public override bool IsActive(Player player, PvPProjectile projectile) => player.controlUseTile && player.GetModPlayer<FunPlayer>().releaseUseTile;
	}
	public class Forward : InputData {
		public override float Priority { get; } = 0;
		public override bool IsActive(Player player, PvPProjectile projectile) => player.direction == -1 ? player.controlLeft : player.controlRight;
	}
	public class Backward : InputData {
		public override float Priority { get; } = 0;
		public override bool IsActive(Player player, PvPProjectile projectile) => player.direction == -1 ? player.controlRight : player.controlLeft;
	}
	public class Up : InputData {
		public override float Priority { get; } = 0;
		public override bool IsActive(Player player, PvPProjectile projectile) => player.controlUp;
	}
	public class Down : InputData {
		public override float Priority { get; } = 0;
		public override bool IsActive(Player player, PvPProjectile projectile) => player.controlDown;
	}
	public class Air : InputData {
		public override float Priority { get; } = 1;
		public override bool IsActive(Player player, PvPProjectile projectile) => player.GetModPlayer<FunPlayer>().collide.y == 0;
	}
	public class HitTarget : InputData {
		public override float Priority { get; } = 9999;
		public override bool IsActive(Player player, PvPProjectile projectile) => projectile.HitTarget;
	}
}
