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
