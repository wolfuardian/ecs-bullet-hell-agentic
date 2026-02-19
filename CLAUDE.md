# CLAUDE.md — Unity 專案開發規範

## 專案資訊

- **專案名稱**: ecs-bullet-hell-agentic

## 專案環境

- **引擎**: Unity 6.3 LTS (6000.3.9f1)
- **Render Pipeline**: URP (Universal Render Pipeline)
- **語言**: C# 10 / .NET Standard 2.1
- **平台**: 依專案需求（預設 Standalone Windows）

## 核心套件

- **ECS/DOTS**: Unity Entities + Burst + Collections + Mathematics
- **Input**: Input System (New) — 禁用 Legacy Input Manager
- **資源管理**: Addressables — 禁止用 Resources.Load
- **UI**: UnityEngine.UI (Legacy) — 使用 `UnityEngine.UI.Text`，**禁用 TextMeshPro**

## 常用指令

```bash
# 建置（Standalone Windows）
"C:\Program Files\Unity\Hub\Editor\6000.3.9f1\Editor\Unity.exe" -batchmode -nographics -projectPath . -buildWindows64Player Build/game.exe -quit -logFile -

# 執行 EditMode 測試
"C:\Program Files\Unity\Hub\Editor\6000.3.9f1\Editor\Unity.exe" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults TestResults.xml -quit -logFile -

# 執行 PlayMode 測試
"C:\Program Files\Unity\Hub\Editor\6000.3.9f1\Editor\Unity.exe" -batchmode -nographics -projectPath . -runTests -testPlatform PlayMode -testResults TestResults.xml -quit -logFile -
```

> **注意**: 路徑請依實際 Unity 安裝位置調整。

## 資料夾結構

```
Assets/
├── Scripts/
│   ├── Runtime/
│   │   ├── {模組名}/          # MonoBehaviour、SO 等 OOP 層
│   │   └── ECS/
│   │       ├── Components/    # IComponentData
│   │       ├── Systems/       # ISystem / SystemBase
│   │       ├── Aspects/       # IAspect
│   │       ├── Authoring/     # Baker + Authoring MonoBehaviour
│   │       └── Jobs/          # IJobEntity / IJobChunk
│   ├── Editor/                # 自訂 Editor 工具
│   └── Tests/
│       ├── EditMode/
│       └── PlayMode/
├── Scenes/
├── Prefabs/
├── ScriptableObjects/
├── Shaders/
├── Art/
├── Audio/
└── AddressableAssets/         # Addressable Groups 資源
```

## Namespace 規範

```
MyGame.{模組名}           # Runtime OOP
MyGame.ECS.{模組名}       # ECS Components / Systems / Jobs
MyGame.Editor.{模組名}    # Editor 工具
MyGame.Tests.{模組名}     # 測試
```

## Assembly Definition

每個主要模組應有自己的 `.asmdef`，確保編譯隔離：
- `MyGame.Runtime.asmdef`
- `MyGame.ECS.asmdef`（References: Unity.Entities, Unity.Burst, Unity.Collections, Unity.Mathematics）
- `MyGame.Editor.asmdef`（Editor only, References: MyGame.Runtime）
- `MyGame.Tests.EditMode.asmdef` / `MyGame.Tests.PlayMode.asmdef`

## 編碼慣例

### 命名

| 類型 | 慣例 | 範例 |
|------|------|------|
| Class / Struct | PascalCase | `PlayerController` |
| Interface | I + PascalCase | `IDamageable` |
| Public Method | PascalCase | `TakeDamage()` |
| Private Method | PascalCase | `CalculateVelocity()` |
| Public Field | PascalCase | `MaxHealth` |
| Private Field | _camelCase | `_currentHp` |
| Local Variable | camelCase | `moveDir` |
| Constant | UPPER_SNAKE | `MAX_BULLET_COUNT` |
| ECS Component | PascalCase + 後綴 | `HealthData`, `MoveSpeed` |
| ECS System | PascalCase + System | `DamageSystem` |
| ECS Job | PascalCase + Job | `MoveJob` |
| SO | PascalCase + Config/Data | `WeaponConfig` |

### 風格

- 大括號換行（Allman style）
- 每個檔案一個主要型別
- 優先使用 `[SerializeField] private` 而非 `public` field
- ECS Component 盡量保持小而單一職責（SoC）
- 用 `[BurstCompile]` 標記所有可 Burst 編譯的 System 和 Job
- 集合優先用 `NativeArray` / `NativeList` / `NativeHashMap`（ECS 層）

## Shader 規範

- 使用 URP Shader（ShaderLab + HLSL）
- 檔名：`MyGame_{效果名}.shader`
- Include: `Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl`
- 禁止使用 Built-in RP 的 `CG PROGRAM` / `fixed` 型別

## 嚴格禁止事項

### Agent 絕對不可以做的事

1. **禁止建立、修改、刪除 `.meta` 檔案** — 由 Unity Editor 自動管理
2. **禁止修改 `.unity` 場景檔** — YAML 序列化極脆弱，手動編輯必壞
3. **禁止修改 `.prefab` 檔** — 同上理由
4. **禁止修改 `.asset` 檔** — 包含 ScriptableObject 序列化資料、URP Asset 設定等
5. **禁止修改 `ProjectSettings/` 底下任何檔案** — 包含 InputManager、TagManager 等
6. **禁止修改 `Packages/manifest.json`** — 套件安裝由開發者在 Unity 內操作
7. **禁止使用 `Resources.Load`** — 本專案使用 Addressables
8. **禁止使用 TextMeshPro** — 本專案使用 Legacy UI Text
9. **禁止使用 Legacy Input** (`Input.GetKey` 等) — 本專案使用 New Input System
10. **禁止在 ECS Job 中使用 managed type** — 會導致 Burst 編譯失敗

## 鼓勵做法

### Code-First 原則

- 用 `[CreateAssetMenu]` 建立 ScriptableObject，而非期望手動建立 .asset
- 用 `[MenuItem]` / `EditorWindow` 做 Editor 工具
- 用 Baker + Authoring Component 做 ECS 資料轉換
- Input Action 用 C# 生成類別（Generate C# Class）操作

### 品質

- 公開 API 寫 `<summary>` XML 文件註解
- 複雜邏輯加行內註解說明「為什麼」而非「做什麼」
- Editor 工具加 `[Tooltip]` 提示
- 用 `#if UNITY_EDITOR` 隔離 Editor-only 程式碼

## ECS 架構指引

### Component 設計

```csharp
// ✅ 好：小且單一職責
public struct MoveSpeed : IComponentData { public float Value; }
public struct HealthData : IComponentData { public int Current; public int Max; }
```

### System 撰寫

```csharp
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct MovementSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var dt = SystemAPI.Time.DeltaTime;
        // 優先用 foreach + RefRW/RefRO
        // 複雜邏輯用 IJobEntity
    }
}
```

## 與 Agent 的協作流程

### 提交程式碼後

1. 開發者在 Unity Editor 開啟專案，觸發 `.meta` 自動生成
2. 開發者查看 Console，將錯誤訊息貼回給 Agent
3. Agent 根據錯誤修正程式碼
4. 重複直到 Console 無錯誤

### 需要 Editor 操作的事項（Agent 請明確告知開發者手動執行）

- 建立 / 設定場景
- 設定 Prefab 上的元件參數
- 設定 URP Asset / Renderer Feature
- 設定 Addressable Groups
- 設定 Input Action Asset
- Build Settings 調整
- 安裝 / 更新 Package

### 回報格式

當 Agent 完成程式碼但需要開發者手動設定時，用以下格式：

```
⚠️ 手動設定需求：
1. [具體操作步驟]
2. [具體操作步驟]
```

## 版控注意

- 每個 Agent 任務使用獨立 feature branch
- Commit message 格式：`feat(模組): 描述` / `fix(模組): 描述`
- `.meta` 檔必須提交到版控（Unity 協作必要）
- 不可在 `.gitignore` 中排除 `.meta`
- 個人偏好設定可寫在 `.claude.local.md`（應加入 `.gitignore`，不提交至版控）
