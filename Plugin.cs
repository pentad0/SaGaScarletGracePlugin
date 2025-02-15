﻿using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using static BattleManager;

namespace SaGaScarletGracePlugin;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
        
    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        // パッチを有効化する。
        new string[] {
            "KillReadyGoCountDown",
        }.ToList().ForEach(patchName => {
            if (Config.Bind("Patces", patchName, true, "Activate or not.").Value)
            {
                Harmony.CreateAndPatchAll(Type.GetType("SaGaScarletGracePlugin." + patchName));
                Logger.LogInfo($"{patchName} is activated!");
            }
        });
    }
}
public class KillReadyGoCountDown
{
    [HarmonyPatch(typeof(BattleManager), "InitCutChange")]
    [HarmonyPrefix]
    public static bool InitCutChangePrefix(ref BattleManager __instance)
    {
        Plugin.Logger.LogDebug("InitCutChangePrefix");
        // internal BattleSequence Sequence => sequence;
        // ↑のようなメンバはプロパティなのでFieldではなくPropertyを使う。
        BattleSequence Sequence = Traverse.Create(__instance).Property("Sequence").GetValue<BattleSequence>();
        GameObject sequencePrefab = Traverse.Create(Sequence).Method("GetSequencePrefab", new object[] {
            4,
        }).GetValue<GameObject>();
        GameObject gameObject = UnityEngine.Object.Instantiate(sequencePrefab);
        gameObject.name = sequencePrefab.name;
        TTSequence_old component = gameObject.GetComponent<TTSequence_old>();
        component.ttInit(casterIsEnemy: false);
        component.isReadyGoLogo = true;
        LocalManagerBase<TeaTimeSequenceManager>.Instance.addSequence(component);
        Traverse.Create(__instance).Field("stepTimer").SetValue(0.5f);
        Traverse.Create(__instance).Field("minCutChangeNum").SetValue(Traverse.Create(__instance).Method("CalcCutChangeCount").GetValue<int>());
        // MethodはGetValue()を呼び出さないと実行されない。
        Traverse.Create(__instance).Method("SelectCutChangeChara").GetValue();
        Traverse.Create(__instance).Field("readyCutNum").SetValue(1);
        Traverse.Create(Sequence).Method("playCameraSequence", new object[] {
            // internal enumを参照する。
            Traverse.Create(typeof(BattleSequence)).Type("eSequenceType").Field("readyGoCutC").GetValue(),
        });
        Traverse.Create(__instance).Field("readyGoCutChangeTime").SetValue(Time.realtimeSinceStartup);
        // nullをobject ChangeType(object value, Type conversionType)でPlayInfoにキャストしてもTraverse Method(string name, params object[] arguments)では
        // ulong Play(SoundLabel label, PlayInfo playInfo = null, bool isFixPitch = false)の呼び出しとして認識してくれないので
        // Typeの配列も渡す方のTraverse Method(string name, Type[] paramTypes, object[] arguments = null)を使わなければならない
        Traverse.Create(Traverse.Create(ManagerBase<SoundManager>.Instance).Property("se").GetValue()).Method("Play", new Type[] {
            typeof(SoundLabel),
            // アクセスが禁止されたインナークラスSoundManager.SE.PlayInfoの型を参照する。
            Traverse.Create(ManagerBase<SoundManager>.Instance).Type("SE").Type("PlayInfo").GetValue<Type>(),
            typeof(bool),
        }, new object[] {
            SoundLabel.SE_M_BTL_UI_READY01,
            null,
            false,
        });
        // このコルーチン呼び出しでReadyGoのカウントダウン処理が行われている。
        // ReadyGoの演出時間の大半はこれのせいなのでコメントアウトする。
        //((MonoBehaviour)(object)__instance).StartCoroutine(Traverse.Create(__instance).Method("ReadyGoCutChangeCoroutine").GetValue<IEnumerator>());

        // 実の所これより上の処理は必要ないので削除して良いのだが説明にちょうどいいので残している。
        
        // この変数設定をしないと次のシーンで処理が永遠に止まる。
        Traverse.Create(__instance).Field("cutChangeStep").SetValue(2);
        // デフォルト引数があるメソッドを呼び出す場合でも引数の省略はできない
        Traverse.Create(__instance).Method("UpdateMenuBPGauge", new object[] {
            eChangeBPType.none,
            0,
        });

        // 本来のInitCutChangeは呼び出さない。
        return false;
    }
}
