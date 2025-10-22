using BepInEx;
using BepInEx.Logging;
using BepInEx.Preloader.Core.Patching;
using HarmonyLib;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using Utility;
using Utility.Localization;

namespace NFCLocalizationFix
{
    [PatcherPluginInfo("me.Plugin.NFCLocalizationFix", "NFCLocalizationFix", "1.0.0")]
    public class NFCLocalizationFix : BasePatcher
    {
        public override void Finalizer()
        {
            try
            {
                Harmony.CreateAndPatchAll(typeof(NFCLocalizationFixPatch));
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"补丁应用失败: {ex}");
            }
        }
        //覆写语言
        [TargetAssembly("Nebulous.dll")]
        public void PatchNebulousAssembly(AssemblyDefinition assembly)
        {
            foreach (Instruction instruction in assembly.MainModule.Types.First((TypeDefinition t) => t.FullName == "Utility.Localization.LocalizationCore").Methods.First((MethodDefinition t) => t.Name == "InitLocalization").Body.Instructions)
            {
                if (instruction.OpCode == OpCodes.Ldstr && (string)instruction.Operand == "en-US")
                {
                    instruction.Operand = "ZH-fix";
                }
            }
        }
        ////覆写翻译函数
        //[HarmonyPrefix, HarmonyPatch(typeof(LocalizationCore), "InitLocalization")]
        [HarmonyPatch]
        [HarmonyPatch(typeof(LocalizationCore), "InitLocalization")]
        class NFCLocalizationFixPatch
        {
            public static bool Prefix()
            {
                try
                {

                    // 3. 从BepInEx配置目录加载外部翻译文件
                    string bepinexPath = Paths.BepInExRootPath;
                    string externalLocalizationPath = Path.Combine(bepinexPath, "Translation", "ZH-fix");

                    if (Directory.Exists(externalLocalizationPath))
                    {
                        // 加载外部JSON文件
                        string[] jsonFiles = Directory.GetFiles(externalLocalizationPath, "*.json");
                        //提取json文件名称
                        string[] jsonFileNames = new string[jsonFiles.Length];
                        for (int i = 0; i < jsonFiles.Length; i++)
                        {
                            jsonFileNames[i] = Path.GetFileNameWithoutExtension(jsonFiles[i]);
                        }
                        // 创建自定义语言包
                        LocalizedStringTable ZHFixJsonTable = ScriptableObject.CreateInstance<LocalizedStringTable>();
                        ZHFixJsonTable.name = "ZH-fix";
                        TextAsset[] Tables = new TextAsset[jsonFiles.Length];
                        //字符串转TextAsset对象
                        int numi = 0;
                        foreach (string jsonFile in jsonFiles)
                        {
                            try
                            {
                                Tables[numi] = new TextAsset(File.ReadAllText(jsonFile));
                                Tables[numi].name = jsonFileNames[numi];
                                numi++;
                                
                            }
                            catch (Exception ex)
                            {
                                UnityEngine.Debug.LogError($"加载翻译文件失败: {jsonFiles}, 错误: {ex.Message}");
                            }

                        }
                        ZHFixJsonTable.Tables = Tables;
                        //ZHFixJsonTable.Loaded = false;
                        LocalizationCore.AddLocale(ZHFixJsonTable);
                        UnityEngine.Debug.Log("成功加载外部翻译文件: ZH-fix");
                    }
                    else
                    {
                        UnityEngine.Debug.LogWarning($"外部翻译路径不存在: {externalLocalizationPath}");
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"加载外部翻译失败: {ex}");
                }
                return true;
            }
        }
    }
}