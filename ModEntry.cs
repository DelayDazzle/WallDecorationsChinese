using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using StardewModdingAPI;

namespace WallDecorationsChinese
{
    public class ModEntry : Mod
    {
        private Harmony? _harmony;

        private static readonly Dictionary<string, string> Translations = new()
        {
            ["Show Layer Changes"] = "显示图层变化",
            ["Show a HUD message when changing an item's wall decoration layer."] = "更改物品的墙壁装饰图层时显示 HUD 消息。",
            ["Allow Wall Corner Placement"] = "允许墙角放置",
            ["Allow hanging items on wall corners, e.g. in the farmhouse's hallway."] = "允许将挂件放置在墙角，例如农舍的走廊。",
        };

        public override void Entry(IModHelper helper)
        {
            _harmony = new Harmony(ModManifest.UniqueID);

            Assembly? targetMod = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "WallDecorations");

            if (targetMod == null)
            {
                Monitor.Log("未找到 WallDecorations 程序集，跳过汉化。", LogLevel.Warn);
                return;
            }

            MethodInfo transpiler = typeof(ModEntry).GetMethod(
                nameof(Transpiler),
                BindingFlags.Static | BindingFlags.NonPublic
            )!;

            var harmonyTranspiler = new HarmonyMethod(transpiler);
            int patchedCount = 0;

            foreach (Type type in targetMod.GetTypes())
            {
                foreach (MethodInfo method in type.GetMethods(
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance |
                    BindingFlags.Static |
                    BindingFlags.DeclaredOnly))
                {
                    if (!ShouldPatch(method)) continue;

                    try
                    {
                        _harmony.Patch(method, transpiler: harmonyTranspiler);
                        patchedCount++;
                    }
                    catch
                    {
                        // 忽略无法 Patch 的方法
                    }
                }
            }

            Monitor.Log($"WallDecorations 汉化补丁已加载，共修补 {patchedCount} 个方法。", LogLevel.Info);
        }

        private static bool ShouldPatch(MethodBase method)
        {
            try
            {
                return method.GetMethodBody() != null;
            }
            catch
            {
                return false;
            }
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldstr &&
                    instruction.operand is string original &&
                    Translations.TryGetValue(original, out string? translated))
                {
                    instruction.operand = translated;
                }

                yield return instruction;
            }
        }
    }
}
