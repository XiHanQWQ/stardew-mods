# Changelog / 更新日志

## [1.5.5] - 2026-08-13

## AutomaticTodoList

### Added / 新增
- New config option "Check Interval (seconds)" (`CheckIntervalSeconds`, default 1), adjustable via Generic Mod Config Menu (1-120 seconds). Higher values reduce performance overhead.
- 新增配置项"检查间隔（秒）"（`CheckIntervalSeconds`，默认 1 秒），可通过 Generic Mod Config Menu 调整（1-120 秒），数值越大性能开销越低。
- New UI text for the option in English, Chinese and Portuguese.
- 新增中、英、葡三语界面文案。

### Fixed / 修复
- **Performance**: all todo-item background checks now run once per second instead of every tick (~60/sec).
- **性能优化**：所有待办事项的后台检查从每帧（约 60 次/秒）改为每秒一次。
  - Updated 12 items: pet, animals, festivals, birthdays, bulletin board, gifting, harvestable crops, ready machines, tool pickup, special orders, Queen of Sauce, passive festivals.
  - 宠物、动物、节日、生日、布告栏、送礼、可收获作物、机器、工具、特殊订单、酱料女王、被动节日等 12 个待办项均已调整。
  - Removed the `UpdateTicked` event subscription and its interface/method plumbing.
  - 移除了 `UpdateTicked` 事件订阅与相关接口/方法。
- **Config toggles now actually work**: disabling a check now fully stops that engine's background work (previously it only hid the panel entry while checks kept running).
- **配置开关真正生效**：关闭某个检查开关后，后台将完全停止对应引擎的检查工作（此前仅隐藏面板显示，检查仍每帧执行）。
- **Pet check log spam fixed**: pet status is no longer checked every tick, eliminating log spam.
- **修复宠物检查日志刷屏**：宠物状态检查不再每帧执行，避免日志刷屏。
- `WaterableCropsEngine` now updates every second instead of every tick.
- `WaterableCropsEngine` 改为每秒更新（原为每帧）。

### Other / 其他
- `Frequency.EveryTick` is no longer used by any engine.
- `Frequency.EveryTick` 不再被任何引擎使用。