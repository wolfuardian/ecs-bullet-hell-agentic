# 東方風彈幕系統設計文件

> **用途**：作為 Agent Team 協同開發的唯一真相來源（Single Source of Truth）  
> **對象**：Unity ECS/DOTS 專案，C# 為主  
> **參考**：Danmakufu ph3 ShotA 系列函式、東方 Project 攻略 Wiki 用語集

---

## 1. Sprite Sheet 佈局規格

本系統基於單張 sprite sheet，**行 = 彈型（Shape）、列 = 顏色（Color）**。

### 1.1 彈型行對照表

| Row | 日文名 | 英文 ID | Danmakufu 常數 | 判定形狀 | 旋轉模式 | 備註 |
|-----|-------|---------|---------------|---------|---------|------|
| 0 | 粒弾 | Pellet | DS_BALL_SS | Circle(2) | Fixed | 最小彈 |
| 1 | 小弾 | BallS | DS_BALL_S | Circle(4) | Fixed | 標準圓彈 |
| 2 | 枠小弾 | RingBall | DS_BALL_BS | Circle(4) | Fixed | 帶外框的小彈 |
| 3 | 中弾 | BallM | DS_BALL_M | Circle(6) | Fixed | 僅中央白色有判定 |
| 4 | 米粒弾 | RiceS | DS_RICE_S | Oval(2,4) | Velocity | 尖端無判定 |
| 5 | 苦無弾 | Kunai | DS_KUNAI | Oval(2,5) | Velocity | 環狀部分無判定 |
| 6 | 鱗弾 | Scale | DS_SCALE | Circle(3) | Velocity | 亦稱楔彈 |
| 7 | 札弾 | Ofuda | DS_BILL | Rect(4,6) | Velocity | 判定比外觀小一圈 |
| 8 | 小星弾 | StarS | DS_STAR_S | Circle(4) | Spin | 尖端無判定 |
| 9 | 大星弾 | StarM | DS_STAR_M | Circle(6) | Spin | 尖端無判定 |
| 10 | 十字弾 | Cross | — | Circle(4) | Spin | 自訂彈型 |
| 11 | 楕円弾 | Oval | DS_OVAL | Oval(4,6) | Velocity | 風神録起橫幅≈外觀 |
| 12 | 矢弾 | Arrow | DS_ARROW | Oval(2,6) | Velocity | 僅箭頭有判定 |
| 13 | ナイフ | Knife | DS_KNIFE | Oval(2,5) | Velocity | 靄狀部分為基準 |
| 14 | 蝶弾 | Butterfly | DS_BUTTERFLY | Circle(3) | Velocity | 翅膀無判定 |
| 15 | 針弾 | Needle | DS_NEEDLE | Line(1,8) | Velocity | 尖端無判定 |
| 16 | 銃弾 | Bullet | DS_FIRE | Oval(2,4) | Velocity | 兩端無判定 |
| 17 | 光弾 | Glow | — | Circle(3) | Fixed | 重疊時難辨識 |
| 18 | レーザー節 | LaserSeg | DS_BEAM | Rect(w,tile) | None | 僅作為 texture tile |

> **判定數值單位**：像素半徑（pixel radius），實際遊戲中需乘以世界座標縮放係數。

### 1.2 顏色列對照表

| Col | 名稱 | Hex 參考 | Danmakufu 後綴 |
|-----|------|---------|---------------|
| 0 | White | #FFFFFF | _WHITE |
| 1 | Red | #FF2020 | _RED |
| 2 | Orange | #FF8820 | _ORANGE |
| 3 | Yellow | #FFE020 | _YELLOW |
| 4 | Green | #20DD40 | _GREEN |
| 5 | Cyan | #20DDDD | _SKY |
| 6 | Blue | #2040FF | _BLUE |
| 7 | Purple | #AA20FF | _PURPLE |
| 8 | Black | #222222 | — |
| 9 | DarkRed | #881010 | — |
| 10 | DarkOrange | #884410 | — |
| 11 | DarkYellow | #887010 | — |
| 12 | DarkGreen | #106E20 | — |
| 13 | DarkCyan | #106E6E | — |
| 14 | DarkBlue | #102088 | — |
| 15 | DarkPurple | #551088 | — |

---

## 2. 列舉定義

```csharp
// ============================================================
// BulletEnums.cs
// 彈型與顏色的列舉定義
// ============================================================

/// <summary>
/// 彈型 — 對應 sprite sheet 的「行」
/// byte 大小，可直接作為 BlobArray 索引
/// </summary>
public enum BulletShape : byte
{
    Pellet      = 0,   // 粒弾
    BallS       = 1,   // 小弾
    RingBall    = 2,   // 枠小弾
    BallM       = 3,   // 中弾
    RiceS       = 4,   // 米粒弾
    Kunai       = 5,   // 苦無弾
    Scale       = 6,   // 鱗弾
    Ofuda       = 7,   // 札弾
    StarS       = 8,   // 小星弾
    StarM       = 9,   // 大星弾
    Cross       = 10,  // 十字弾
    Oval        = 11,  // 楕円弾
    Arrow       = 12,  // 矢弾
    Knife       = 13,  // ナイフ弾
    Butterfly   = 14,  // 蝶弾
    Needle      = 15,  // 針弾
    Bullet      = 16,  // 銃弾（座薬）
    Glow        = 17,  // 光弾
    LaserSeg    = 18,  // レーザー節（tile 用）

    COUNT       = 19
}

/// <summary>
/// 顏色 — 對應 sprite sheet 的「列」
/// </summary>
public enum BulletColor : byte
{
    White       = 0,
    Red         = 1,
    Orange      = 2,
    Yellow      = 3,
    Green       = 4,
    Cyan        = 5,
    Blue        = 6,
    Purple      = 7,
    Black       = 8,
    DarkRed     = 9,
    DarkOrange  = 10,
    DarkYellow  = 11,
    DarkGreen   = 12,
    DarkCyan    = 13,
    DarkBlue    = 14,
    DarkPurple  = 15,

    COUNT       = 16
}

/// <summary>
/// 判定形狀類型
/// </summary>
public enum HitboxType : byte
{
    Circle,  // 圓形：Size.x = 半徑
    Oval,    // 橢圓：Size.x = 短軸半徑, Size.y = 長軸半徑
    Rect,    // 矩形：Size.x = 半寬, Size.y = 半高
    Line,    // 線段：Size.x = 半寬, Size.y = 半長
    None     // 無判定（裝飾用）
}

/// <summary>
/// 旋轉模式
/// </summary>
public enum RotateMode : byte
{
    Fixed,     // 固定朝向（圓形彈）
    Velocity,  // 朝速度方向（米粒、ナイフ等）
    Spin,      // 持續旋轉（星弾等）
}
```

---

## 3. 資料結構

```csharp
// ============================================================
// BulletData.cs
// 彈型定義的資料結構，用於 BlobAsset 或 NativeArray
// ============================================================

using Unity.Mathematics;

/// <summary>
/// 判定定義（8 bytes）
/// </summary>
public struct HitboxDef
{
    public HitboxType Type;     // 1 byte
    public byte _pad;           // 1 byte padding
    public float2 Size;         // 8 bytes — (rx, ry) or (halfW, halfH)
    public float2 Offset;       // 8 bytes — 判定中心偏移（多數彈為 0）
}

/// <summary>
/// 單一彈型的完整定義（BlobArray 的元素）
/// </summary>
public struct BulletShapeDef
{
    public byte Row;            // sprite sheet 行索引
    public byte FrameCount;     // 動畫幀數（1 = 靜態）
    public byte RenderOrder;    // 描繪排序層（0=最底, 255=最頂）
    public RotateMode Rotate;   // 旋轉模式

    public int2 CellSize;       // 單格 pixel 尺寸 (w, h)
    public HitboxDef Hitbox;    // 碰撞判定
    public float SpinSpeed;     // RotateMode.Spin 時的角速度 (rad/s)
}

/// <summary>
/// ECS Component：附加在每顆子彈 Entity 上
/// 僅 2 bytes，cache-friendly
/// </summary>
public struct BulletVisual : IComponentData
{
    public BulletShape Shape;   // 1 byte
    public BulletColor Color;   // 1 byte
}

/// <summary>
/// ECS Component：子彈運動
/// </summary>
public struct BulletMotion : IComponentData
{
    public float Speed;         // 像素/秒
    public float Angle;         // 弧度
    public float Accel;         // 加速度
    public float MaxSpeed;      // 速度上限（0 = 無限）
    public float AngularVel;    // 角速度（自機狙追蹤用）
}

/// <summary>
/// ECS Component：生成延遲
/// </summary>
public struct SpawnDelay : IComponentData
{
    public int FramesRemaining; // 倒數幀數，> 0 時不顯示不判定
}
```

---

## 4. 資料表建構

```csharp
// ============================================================
// BulletShapeTable.cs
// 靜態資料表 — 從 enum 查詢 BulletShapeDef
// ============================================================

using Unity.Mathematics;

public static class BulletShapeTable
{
    // 陣列索引 = (int)BulletShape
    private static readonly BulletShapeDef[] Table = new BulletShapeDef[]
    {
        // Row 0: Pellet — 粒弾
        new BulletShapeDef {
            Row = 0, FrameCount = 1, RenderOrder = 10, Rotate = RotateMode.Fixed,
            CellSize = new int2(8, 8), SpinSpeed = 0f,
            Hitbox = new HitboxDef {
                Type = HitboxType.Circle, Size = new float2(2f, 0f), Offset = float2.zero
            }
        },
        // Row 1: BallS — 小弾
        new BulletShapeDef {
            Row = 1, FrameCount = 1, RenderOrder = 20, Rotate = RotateMode.Fixed,
            CellSize = new int2(16, 16), SpinSpeed = 0f,
            Hitbox = new HitboxDef {
                Type = HitboxType.Circle, Size = new float2(4f, 0f), Offset = float2.zero
            }
        },
        // Row 2: RingBall — 枠小弾
        new BulletShapeDef {
            Row = 2, FrameCount = 1, RenderOrder = 20, Rotate = RotateMode.Fixed,
            CellSize = new int2(16, 16), SpinSpeed = 0f,
            Hitbox = new HitboxDef {
                Type = HitboxType.Circle, Size = new float2(4f, 0f), Offset = float2.zero
            }
        },
        // Row 3: BallM — 中弾
        new BulletShapeDef {
            Row = 3, FrameCount = 1, RenderOrder = 30, Rotate = RotateMode.Fixed,
            CellSize = new int2(24, 24), SpinSpeed = 0f,
            Hitbox = new HitboxDef {
                Type = HitboxType.Circle, Size = new float2(6f, 0f), Offset = float2.zero
            }
        },
        // Row 4: RiceS — 米粒弾
        new BulletShapeDef {
            Row = 4, FrameCount = 1, RenderOrder = 15, Rotate = RotateMode.Velocity,
            CellSize = new int2(8, 16), SpinSpeed = 0f,
            Hitbox = new HitboxDef {
                Type = HitboxType.Oval, Size = new float2(2f, 4f), Offset = float2.zero
            }
        },
        // Row 5: Kunai — 苦無弾
        new BulletShapeDef {
            Row = 5, FrameCount = 1, RenderOrder = 25, Rotate = RotateMode.Velocity,
            CellSize = new int2(12, 24), SpinSpeed = 0f,
            Hitbox = new HitboxDef {
                Type = HitboxType.Oval, Size = new float2(2f, 5f), Offset = float2.zero
            }
        },
        // Row 6: Scale — 鱗弾
        new BulletShapeDef {
            Row = 6, FrameCount = 1, RenderOrder = 15, Rotate = RotateMode.Velocity,
            CellSize = new int2(12, 12), SpinSpeed = 0f,
            Hitbox = new HitboxDef {
                Type = HitboxType.Circle, Size = new float2(3f, 0f), Offset = float2.zero
            }
        },
        // Row 7: Ofuda — 札弾
        new BulletShapeDef {
            Row = 7, FrameCount = 1, RenderOrder = 25, Rotate = RotateMode.Velocity,
            CellSize = new int2(16, 24), SpinSpeed = 0f,
            Hitbox = new HitboxDef {
                Type = HitboxType.Rect, Size = new float2(4f, 6f), Offset = float2.zero
            }
        },
        // Row 8: StarS — 小星弾
        new BulletShapeDef {
            Row = 8, FrameCount = 1, RenderOrder = 20, Rotate = RotateMode.Spin,
            CellSize = new int2(16, 16), SpinSpeed = 3.14f,
            Hitbox = new HitboxDef {
                Type = HitboxType.Circle, Size = new float2(4f, 0f), Offset = float2.zero
            }
        },
        // Row 9: StarM — 大星弾
        new BulletShapeDef {
            Row = 9, FrameCount = 1, RenderOrder = 30, Rotate = RotateMode.Spin,
            CellSize = new int2(24, 24), SpinSpeed = 2.5f,
            Hitbox = new HitboxDef {
                Type = HitboxType.Circle, Size = new float2(6f, 0f), Offset = float2.zero
            }
        },
        // Row 10: Cross — 十字弾
        new BulletShapeDef {
            Row = 10, FrameCount = 1, RenderOrder = 20, Rotate = RotateMode.Spin,
            CellSize = new int2(16, 16), SpinSpeed = 4.0f,
            Hitbox = new HitboxDef {
                Type = HitboxType.Circle, Size = new float2(4f, 0f), Offset = float2.zero
            }
        },
        // Row 11: Oval — 楕円弾
        new BulletShapeDef {
            Row = 11, FrameCount = 1, RenderOrder = 25, Rotate = RotateMode.Velocity,
            CellSize = new int2(16, 20), SpinSpeed = 0f,
            Hitbox = new HitboxDef {
                Type = HitboxType.Oval, Size = new float2(4f, 6f), Offset = float2.zero
            }
        },
        // Row 12: Arrow — 矢弾
        new BulletShapeDef {
            Row = 12, FrameCount = 1, RenderOrder = 25, Rotate = RotateMode.Velocity,
            CellSize = new int2(12, 28), SpinSpeed = 0f,
            Hitbox = new HitboxDef {
                Type = HitboxType.Oval, Size = new float2(2f, 6f),
                Offset = new float2(0f, -4f) // 判定偏向箭頭
            }
        },
        // Row 13: Knife — ナイフ弾
        new BulletShapeDef {
            Row = 13, FrameCount = 1, RenderOrder = 25, Rotate = RotateMode.Velocity,
            CellSize = new int2(10, 24), SpinSpeed = 0f,
            Hitbox = new HitboxDef {
                Type = HitboxType.Oval, Size = new float2(2f, 5f), Offset = float2.zero
            }
        },
        // Row 14: Butterfly — 蝶弾
        new BulletShapeDef {
            Row = 14, FrameCount = 2, RenderOrder = 15, Rotate = RotateMode.Velocity,
            CellSize = new int2(20, 20), SpinSpeed = 0f,
            Hitbox = new HitboxDef {
                Type = HitboxType.Circle, Size = new float2(3f, 0f), Offset = float2.zero
            }
        },
        // Row 15: Needle — 針弾
        new BulletShapeDef {
            Row = 15, FrameCount = 1, RenderOrder = 15, Rotate = RotateMode.Velocity,
            CellSize = new int2(6, 32), SpinSpeed = 0f,
            Hitbox = new HitboxDef {
                Type = HitboxType.Line, Size = new float2(1f, 8f), Offset = float2.zero
            }
        },
        // Row 16: Bullet — 銃弾
        new BulletShapeDef {
            Row = 16, FrameCount = 1, RenderOrder = 20, Rotate = RotateMode.Velocity,
            CellSize = new int2(10, 20), SpinSpeed = 0f,
            Hitbox = new HitboxDef {
                Type = HitboxType.Oval, Size = new float2(2f, 4f), Offset = float2.zero
            }
        },
        // Row 17: Glow — 光弾
        new BulletShapeDef {
            Row = 17, FrameCount = 1, RenderOrder = 12, Rotate = RotateMode.Fixed,
            CellSize = new int2(16, 16), SpinSpeed = 0f,
            Hitbox = new HitboxDef {
                Type = HitboxType.Circle, Size = new float2(3f, 0f), Offset = float2.zero
            }
        },
        // Row 18: LaserSeg — レーザー節（tile 用，不直接作為子彈）
        new BulletShapeDef {
            Row = 18, FrameCount = 1, RenderOrder = 50, Rotate = RotateMode.Fixed,
            CellSize = new int2(16, 16), SpinSpeed = 0f,
            Hitbox = new HitboxDef {
                Type = HitboxType.None, Size = float2.zero, Offset = float2.zero
            }
        },
    };

    /// <summary>查詢彈型定義 — O(1)</summary>
    public static ref readonly BulletShapeDef Get(BulletShape shape)
        => ref Table[(int)shape];

    /// <summary>計算 sprite sheet 上的格子座標 (col, row)</summary>
    public static int2 GetSpriteCell(BulletShape shape, BulletColor color)
        => new int2((int)color, Get(shape).Row);

    /// <summary>計算 UV 矩形（需要 sheet 總尺寸）</summary>
    public static float4 GetUV(BulletShape shape, BulletColor color,
                                int sheetWidth, int sheetHeight)
    {
        var def = Get(shape);
        int col = (int)color;
        float u = (float)(col * def.CellSize.x) / sheetWidth;
        float v = (float)(def.Row * def.CellSize.y) / sheetHeight;
        float w = (float)def.CellSize.x / sheetWidth;
        float h = (float)def.CellSize.y / sheetHeight;
        return new float4(u, v, w, h); // (u, v, width, height)
    }
}
```

---

## 5. 發射函式 API

```csharp
// ============================================================
// BulletFactory.cs
// 彈幕發射 API — 仿 Danmakufu ShotA 系列
// ============================================================

using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// 靜態彈幕發射工具類
/// 所有方法回傳 Entity 以便後續追加組件（變速、追蹤等）
/// </summary>
public static class BulletFactory
{
    // ---------------------------------------------------------
    // 核心：Entity 建立（內部用）
    // ---------------------------------------------------------

    private static EntityManager EM => World.DefaultGameObjectInjectionWorld.EntityManager;
    private static EntityArchetype _archetype;

    /// <summary>初始化 Archetype — 在系統 OnCreate 中呼叫一次</summary>
    public static void Initialize(EntityManager em)
    {
        _archetype = em.CreateArchetype(
            typeof(LocalTransform),
            typeof(BulletVisual),
            typeof(BulletMotion),
            typeof(SpawnDelay),
            typeof(BulletHitbox)    // runtime 用，由 Shape 查表填入
        );
    }

    private static Entity CreateBullet(
        float2 pos, float speed, float angle,
        BulletShape shape, BulletColor color,
        int delay)
    {
        var entity = EM.CreateEntity(_archetype);

        EM.SetComponentData(entity, LocalTransform.FromPosition(
            new float3(pos.x, pos.y, 0f)));

        EM.SetComponentData(entity, new BulletVisual {
            Shape = shape, Color = color
        });

        EM.SetComponentData(entity, new BulletMotion {
            Speed = speed, Angle = angle,
            Accel = 0f, MaxSpeed = 0f, AngularVel = 0f
        });

        EM.SetComponentData(entity, new SpawnDelay {
            FramesRemaining = delay
        });

        // 從資料表查詢判定並寫入
        var def = BulletShapeTable.Get(shape);
        EM.SetComponentData(entity, new BulletHitbox {
            Type = def.Hitbox.Type,
            Size = def.Hitbox.Size,
            Offset = def.Hitbox.Offset
        });

        return entity;
    }

    // ---------------------------------------------------------
    // 公開 API：單發
    // ---------------------------------------------------------

    /// <summary>
    /// 基礎單發 — 等同 Danmakufu CreateShotA1
    /// </summary>
    /// <param name="pos">發射位置（世界座標）</param>
    /// <param name="speed">速度（pixel/sec）</param>
    /// <param name="angle">角度（degree，0=右，90=下）</param>
    /// <param name="shape">彈型</param>
    /// <param name="color">顏色</param>
    /// <param name="delay">生成延遲（幀）— 延遲中不顯示不判定</param>
    public static Entity Shot(
        float2 pos, float speed, float angle,
        BulletShape shape, BulletColor color,
        int delay = 0)
    {
        return CreateBullet(pos, speed, math.radians(angle), shape, color, delay);
    }

    /// <summary>
    /// 加速彈 — 等同 Danmakufu CreateShotA2
    /// </summary>
    public static Entity ShotAccel(
        float2 pos, float speed, float angle,
        float accel, float maxSpeed,
        BulletShape shape, BulletColor color,
        int delay = 0)
    {
        var entity = CreateBullet(pos, speed, math.radians(angle), shape, color, delay);
        EM.SetComponentData(entity, new BulletMotion {
            Speed = speed,
            Angle = math.radians(angle),
            Accel = accel,
            MaxSpeed = maxSpeed,
            AngularVel = 0f
        });
        return entity;
    }

    /// <summary>
    /// 追蹤彈 — 每幀朝目標旋轉
    /// </summary>
    /// <param name="turnRate">最大轉向速度（degree/sec）</param>
    public static Entity ShotHoming(
        float2 pos, float speed, float angle,
        float turnRate,
        BulletShape shape, BulletColor color,
        int delay = 0)
    {
        var entity = CreateBullet(pos, speed, math.radians(angle), shape, color, delay);
        EM.SetComponentData(entity, new BulletMotion {
            Speed = speed,
            Angle = math.radians(angle),
            Accel = 0f,
            MaxSpeed = 0f,
            AngularVel = math.radians(turnRate)
        });
        // 追蹤目標由額外的 HomingTarget component 指定
        return entity;
    }

    // ---------------------------------------------------------
    // 公開 API：批次發射
    // ---------------------------------------------------------

    /// <summary>
    /// 扇形 n-way 彈
    /// </summary>
    /// <param name="centerAngle">扇形中心角度（degree）</param>
    /// <param name="spreadAngle">扇形總張角（degree）</param>
    /// <param name="count">彈數</param>
    public static void ShotFan(
        float2 pos, float speed,
        float centerAngle, float spreadAngle, int count,
        BulletShape shape, BulletColor color,
        int delay = 0)
    {
        if (count <= 0) return;
        if (count == 1)
        {
            Shot(pos, speed, centerAngle, shape, color, delay);
            return;
        }

        float step = spreadAngle / (count - 1);
        float startAngle = centerAngle - spreadAngle * 0.5f;

        for (int i = 0; i < count; i++)
        {
            Shot(pos, speed, startAngle + step * i, shape, color, delay);
        }
    }

    /// <summary>
    /// 環形彈（等間距全方位）
    /// </summary>
    /// <param name="startAngle">第一顆的角度（degree）</param>
    /// <param name="count">彈數</param>
    public static void ShotRing(
        float2 pos, float speed,
        float startAngle, int count,
        BulletShape shape, BulletColor color,
        int delay = 0)
    {
        if (count <= 0) return;
        float step = 360f / count;

        for (int i = 0; i < count; i++)
        {
            Shot(pos, speed, startAngle + step * i, shape, color, delay);
        }
    }

    /// <summary>
    /// 隨機散射
    /// </summary>
    /// <param name="baseAngle">基準角度（degree）</param>
    /// <param name="randomRange">隨機偏移範圍（±degree）</param>
    /// <param name="speedRange">速度隨機範圍（min, max）</param>
    public static void ShotSpread(
        float2 pos,
        float baseAngle, float randomRange,
        float2 speedRange, int count,
        BulletShape shape, BulletColor color,
        int delay = 0)
    {
        var rng = Random.CreateFromIndex((uint)System.Environment.TickCount);

        for (int i = 0; i < count; i++)
        {
            float angle = baseAngle + rng.NextFloat(-randomRange, randomRange);
            float speed = rng.NextFloat(speedRange.x, speedRange.y);
            Shot(pos, speed, angle, shape, color, delay);
        }
    }

    /// <summary>
    /// 自機狙 — 以目標位置計算角度後發射
    /// </summary>
    public static Entity ShotAim(
        float2 pos, float speed,
        float2 targetPos,
        BulletShape shape, BulletColor color,
        int delay = 0)
    {
        float angle = math.degrees(math.atan2(
            targetPos.y - pos.y,
            targetPos.x - pos.x));
        return Shot(pos, speed, angle, shape, color, delay);
    }

    /// <summary>
    /// 自機狙 n-way
    /// </summary>
    public static void ShotAimFan(
        float2 pos, float speed,
        float2 targetPos,
        float spreadAngle, int count,
        BulletShape shape, BulletColor color,
        int delay = 0)
    {
        float centerAngle = math.degrees(math.atan2(
            targetPos.y - pos.y,
            targetPos.x - pos.x));
        ShotFan(pos, speed, centerAngle, spreadAngle, count, shape, color, delay);
    }
}
```

---

## 6. 雷射系統（獨立架構）

雷射與普通子彈差異太大，使用獨立的 Entity Archetype。

```csharp
// ============================================================
// LaserData.cs
// 雷射專用組件
// ============================================================

/// <summary>直線雷射</summary>
public struct LaserBeam : IComponentData
{
    public float2 Origin;       // 發射點
    public float Angle;         // 方向（rad）
    public float Length;         // 當前長度
    public float MaxLength;      // 最大長度
    public float Width;          // 寬度
    public float GrowSpeed;      // 延伸速度（pixel/sec）
    public BulletColor Color;    // 顏色（決定 texture tile）
    public byte GrazeInterval;   // Graze 判定間隔（幀）
    public bool Active;          // 判定是否啟用（預告線時 false）
}

/// <summary>扭曲雷射（へにょり）— 多節段貝茲曲線</summary>
public struct CurveLaser : IComponentData
{
    public float Width;
    public BulletColor Color;
    public byte SegmentCount;    // 節段數
    public byte GrazeInterval;
}

/// <summary>扭曲雷射的控制點（DynamicBuffer）</summary>
public struct CurveLaserPoint : IBufferElementData
{
    public float2 Position;
    public float2 Velocity;
}

// ---------------------------------------------------------
// 雷射工廠
// ---------------------------------------------------------

public static class LaserFactory
{
    /// <summary>
    /// 直線雷射 — 先以預告線顯示，delay 幀後啟用判定
    /// </summary>
    public static Entity CreateBeam(
        float2 origin, float angle,
        float maxLength, float width,
        float growSpeed, BulletColor color,
        int warningFrames = 30)
    {
        // 建立 Entity with LaserBeam component
        // warningFrames 期間 Active = false, 顯示細預告線
        // 之後 Active = true, 開始延伸
        // ...
        return Entity.Null; // placeholder
    }

    /// <summary>
    /// 扭曲雷射 — 由多個控制點組成的曲線
    /// </summary>
    public static Entity CreateCurveLaser(
        float2 startPos, float startAngle,
        float speed, float width,
        BulletColor color, int segmentCount = 16)
    {
        // 建立 Entity with CurveLaser + DynamicBuffer<CurveLaserPoint>
        // ...
        return Entity.Null; // placeholder
    }
}
```

---

## 7. ECS System 概覽

```csharp
// ============================================================
// 各 System 職責摘要（實作時各自獨立檔案）
// ============================================================

// --- 更新順序 ---
// [UpdateInGroup(typeof(SimulationSystemGroup))]
// 1. SpawnDelaySystem          — 遞減 SpawnDelay，歸零時啟用
// 2. BulletMotionSystem        — 套用 speed/angle/accel 更新位置
// 3. HomingSystem              — 追蹤彈轉向
// 4. BulletBoundsSystem        — 超出畫面範圍的彈 Destroy
// 5. CollisionSystem           — 自機 vs 全部子彈碰撞判定
// 6. GrazeSystem               — 自機 vs 全部子彈擦彈判定
// 7. LaserBeamSystem           — 直線雷射延伸/判定
// 8. CurveLaserSystem          — 曲線雷射更新控制點/判定

// [UpdateInGroup(typeof(PresentationSystemGroup))]
// 9. BulletRenderSystem        — GPU Instancing，按 (Shape) batch
// 10. LaserRenderSystem        — 雷射專用渲染
// 11. SpawnEffectSystem        — 出現/消失特效
```

---

## 8. Agent Team 分工定義

每個 Agent 負責一個獨立模組，以本文件作為介面契約。

```
┌─────────────────────────────────────────────────────────┐
│  Agent A：SpriteSheet 解析器                             │
│  ─────────────────────────────────────────────           │
│  輸入：sprite sheet PNG + BulletShapeTable               │
│  輸出：BlobAsset<BulletShapeDef[]> + UV atlas            │
│  職責：                                                   │
│    - 解析 PNG 佈局，驗證行列對應 enum                      │
│    - 產生 BlobAsset 供 runtime 查詢                       │
│    - 處理動畫幀切分（蝶弾等多幀彈型）                       │
│    - 提供 Editor 工具預覽每個 (Shape, Color) 的切圖         │
│  不碰：Entity 建立、物理判定、渲染                          │
├─────────────────────────────────────────────────────────┤
│  Agent B：彈幕發射 API                                    │
│  ─────────────────────────────────────────────           │
│  輸入：BulletEnums + BulletData + BulletShapeTable       │
│  輸出：BulletFactory 靜態類                               │
│  職責：                                                   │
│    - 實作 Shot/ShotFan/ShotRing/ShotAccel 等全部 API      │
│    - 管理 EntityArchetype 和 ECB (EntityCommandBuffer)    │
│    - SpawnDelay 系統                                      │
│    - 確保 burst-compatible                                │
│  不碰：渲染、碰撞、Pattern 腳本                            │
├─────────────────────────────────────────────────────────┤
│  Agent C：碰撞判定系統                                    │
│  ─────────────────────────────────────────────           │
│  輸入：HitboxDef + 自機判定                               │
│  輸出：CollisionSystem + GrazeSystem                      │
│  職責：                                                   │
│    - Circle vs Circle                                     │
│    - Circle vs Oval（含旋轉）                              │
│    - Circle vs Rect（含旋轉）                              │
│    - Circle vs Line                                       │
│    - 空間分割加速（Grid 或 QuadTree）                      │
│    - Graze 判定（判定半徑 + grazeMargin）                  │
│    - 雷射碰撞（線段 vs 圓）                                │
│  不碰：渲染、Entity 建立、Pattern                          │
├─────────────────────────────────────────────────────────┤
│  Agent D：渲染系統                                        │
│  ─────────────────────────────────────────────           │
│  輸入：BulletVisual + LocalTransform + BulletShapeTable  │
│  輸出：BulletRenderSystem + LaserRenderSystem            │
│  職責：                                                   │
│    - GPU Instancing — 按 BulletShape 分 batch             │
│    - 顏色切換只改 UV.x，不換 batch                         │
│    - 旋轉模式處理（Fixed/Velocity/Spin）                   │
│    - SpawnDelay > 0 時不渲染                               │
│    - 雷射：直線用 quad strip，曲線用 mesh                  │
│    - Spawn/Despawn 粒子特效                                │
│  不碰：物理、Entity 建立、Pattern                          │
├─────────────────────────────────────────────────────────┤
│  Agent E：彈幕 Pattern 腳本                               │
│  ─────────────────────────────────────────────           │
│  輸入：BulletFactory API + LaserFactory API               │
│  輸出：各 Boss/道中的彈幕邏輯                              │
│  職責：                                                   │
│    - 通常彈幕（n-way、散射、自機狙組合）                    │
│    - 符卡彈幕（固定 pattern + 難度分歧）                    │
│    - 以 Coroutine / ISystem + Timer 驅動節奏               │
│    - 彈幕參數表（速度、密度、間隔等按難度調整）             │
│  不碰：底層 Entity 結構、渲染、碰撞                        │
└─────────────────────────────────────────────────────────┘
```

---

## 9. 設計決策 FAQ

### Q: 為什麼用 (Shape, Color) 雙 byte 而非單一 ShotID int？

Danmakufu 的 `DS_BALL_S_RED` 是腳本語言限制下的折衷。ECS 中：
- **2 bytes** 比 4 bytes int 更省，cache 更友好
- 渲染可按 `Shape` batch（同一行同一 texture region）
- 顏色切換只改 UV.x，**不換 batch**
- Hitbox 只依賴 Shape，與 Color **完全解耦**

### Q: Laser 為何獨立 Archetype？

普通子彈是「點 + 速度向量」，雷射是「線段/曲線 + 寬度 + 持續時間」。混用會：
- 浪費 chunk 空間（多數彈不需要 Length/Width）
- 碰撞判定邏輯完全不同（點 vs 線段）
- 渲染方式不同（instancing vs mesh strip）

### Q: 大玉/核彈等超大彈怎麼辦？

在 `BulletShapeTable` 新增行即可。大玉的判定特性是「外圈白色無判定」，用 `Circle` 但半徑設為內圈。核彈因不同符卡判定差異極大，建議在 Pattern 腳本中覆寫 `BulletHitbox.Size`。

### Q: 陰陽玉等特殊彈的消彈耐性？

增加 `BulletFlags` component：

```csharp
[System.Flags]
public enum BulletFlags : byte
{
    None          = 0,
    BombImmune    = 1 << 0, // Bomb 無法消除
    CardImmune    = 1 << 1, // 卡片無法消除
    Persistent    = 1 << 2, // 不受全消影響
}
```

### Q: Pattern 腳本用什麼驅動？

推薦兩種方案（可混用）：

1. **ISystem + Timer**：純 ECS，Burst 可編譯，適合簡單規律彈幕
2. **Coroutine（UniTask）**：寫起來最接近 Danmakufu 腳本，適合複雜符卡

```csharp
// Coroutine 範例：蓮花彈幕
async UniTaskVoid LotusPattern(float2 bossPos)
{
    for (int wave = 0; wave < 8; wave++)
    {
        float baseAngle = wave * 15f;
        BulletFactory.ShotRing(bossPos, 120f, baseAngle, 24,
            BulletShape.Scale, BulletColor.Purple);

        await UniTask.DelayFrame(12); // 等待 12 幀

        BulletFactory.ShotRing(bossPos, 80f, baseAngle + 7.5f, 24,
            BulletShape.RiceS, BulletColor.Cyan);

        await UniTask.DelayFrame(8);
    }
}
```

---

## 10. 檔案結構建議

```
Assets/
├── BulletSystem/
│   ├── Data/
│   │   ├── BulletEnums.cs              ← §2 列舉
│   │   ├── BulletData.cs               ← §3 資料結構
│   │   └── BulletShapeTable.cs         ← §4 資料表
│   ├── Factory/
│   │   ├── BulletFactory.cs            ← §5 發射 API
│   │   └── LaserFactory.cs             ← §6 雷射工廠
│   ├── Systems/
│   │   ├── SpawnDelaySystem.cs
│   │   ├── BulletMotionSystem.cs
│   │   ├── HomingSystem.cs
│   │   ├── BulletBoundsSystem.cs
│   │   ├── CollisionSystem.cs
│   │   ├── GrazeSystem.cs
│   │   ├── LaserBeamSystem.cs
│   │   └── CurveLaserSystem.cs
│   ├── Rendering/
│   │   ├── BulletRenderSystem.cs
│   │   ├── LaserRenderSystem.cs
│   │   └── SpawnEffectSystem.cs
│   ├── Patterns/
│   │   ├── Stage1/
│   │   ├── Stage2/
│   │   └── ...
│   └── Resources/
│       └── bullet_sheet.png            ← Sprite Sheet
└── CLAUDE.md                           ← 引用本文件
```
