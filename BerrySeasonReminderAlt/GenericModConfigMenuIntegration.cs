using System;
using StardewModdingAPI;

namespace BerrySeasonReminder
{
    /// <summary>
    /// GMCM API 接口的最小定义，仅包含本模组使用的方法。
    /// </summary>
    public interface IGenericModConfigMenuApi
    {
        void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);
        void AddBoolOption(IManifest mod, Func<bool> getValue, Action<bool> setValue, Func<string> name, Func<string>? tooltip = null, string? fieldId = null);
    }

    /// <summary>
    /// 负责与 Generic Mod Config Menu 集成。
    /// </summary>
    internal static class GenericModConfigMenuIntegration
    {
        /// <summary>
        /// 注册模组到 GMCM（如果已安装）。
        /// </summary>
        /// <param name="mod">当前模组的 manifest。</param>
        /// <param name="helper">SMAPI helper。</param>
        /// <param name="getConfig">获取当前配置的委托。</param>
        /// <param name="setConfig">设置并保存配置的委托。</param>
        public static void Register(IManifest mod, IModHelper helper, Func<BerrySeasonReminderConfig> getConfig, Action<BerrySeasonReminderConfig> setConfig)
        {
            // 获取 GMCM API（如果已安装）
            var api = helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (api is null)
                return;

            // 注册模组
            api.Register(
                mod: mod,
                reset: () => setConfig(new BerrySeasonReminderConfig()),
                save: () => helper.WriteConfig(getConfig())
            );

            // 添加配置选项
            api.AddBoolOption(
                mod: mod,
                name: () => helper.Translation.Get("config.require-bears-knowledge.name"),
                tooltip: () => helper.Translation.Get("config.require-bears-knowledge.tooltip"),
                getValue: () => getConfig().RequireBearsKnowledge,
                setValue: value =>
                {
                    var config = getConfig();
                    config.RequireBearsKnowledge = value;
                    setConfig(config);
                }
            );
        }
    }
}