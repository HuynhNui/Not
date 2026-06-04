# Enemy Creation Guide

Tai lieu nay dung de tao enemy moi trong project "True Gate?". Pipeline hien tai spawn theo prefab co weight, dung pool, va chia lam hai nhom chinh: enemy melee/chase nhu `Enemy.prefab`, va enemy shooter/hold-position nhu `Vomfy.prefab`.

> Ghi chu hien trang: project da co `EnemyArchetypeData`, nhung enemy runtime hien tai chua apply archetype data truc tiep. Khi tao enemy moi trong v1, hay cau hinh bang prefab + `UnitData`/fallback fields + weighted spawn entry.

## Quick Checklist

1. Chuan bi asset visual/animation.
   - Import sprite sheet, PNG sliced, Aseprite, hoac animation controller.
   - Dat animation state name khop voi script neu enemy co behavior rieng.
   - Voi shooter kieu Vomfy, cac state dang dung la `Hop`, `Idle`, `Attackaction`, `ow`.

2. Tao prefab enemy moi.
   - Melee/chase enemy: duplicate `Assets/_Project/Prefabs/Enemies/Enemy.prefab`.
   - Shooter enemy: duplicate `Assets/_Project/Prefabs/Enemies/Vomfy.prefab`.
   - Dat prefab moi cung folder `Assets/_Project/Prefabs/Enemies`.

3. Kiem tra root prefab.
   - Root phai co `EnemyController`.
   - Root layer nen la `Enemy`.
   - Collider/Rigidbody2D giu setup tu prefab goc.
   - Health bar reference giu nguyen neu muon dung HP bar world-space hien tai.
   - Enemy layer self-collision da duoc tat/excluded de enemy khong day nhau.

4. Cau hinh `EnemyController`.
   - Melee: `movementMode = ChaseTarget`.
   - Shooter dung top band: `movementMode = EnterAndHoldTopBand`.
   - `despawnImmediatelyOnDeath = true` cho enemy chet roi bien mat ngay.
   - `despawnImmediatelyOnDeath = false` neu can choi death animation roi script rieng goi `Despawn()`.
   - Dieu chinh `scoreValue`, `coinReward`, `destroyOnPlayerHit`, HP, speed, contact damage.

5. Them visual child neu can.
   - Nen co child ten `Visual` de chinh scale/Animator/SpriteRenderer ma khong lam lech root collider.
   - Chinh visual scale tren child, khong scale root neu khong muon thay doi collider/movement.

6. Neu enemy co projectile.
   - Tao projectile prefab rieng, gan `EnemyProjectile`.
   - Projectile nen co trigger collider, sprite renderer, animation frames, speed, damage, lifetime.
   - Projectile cua enemy chi damage `MainPlayerUnit`, khong damage enemy.

7. Them vao PoolSystem.
   - Them pool definition cho prefab enemy moi.
   - Neu co projectile, them pool definition cho projectile.
   - Set initial size vua du cho gameplay mobile, tang dan neu thay instantiate spike.

8. Them vao EnemySpawner.
   - Mo scene `Assets/_Project/Scenes/Main.unity`.
   - Chon object co `EnemySpawnerSystem`.
   - Them enemy prefab moi vao `spawnEntries`.
   - Set `spawnWeight` va `unlockAfterSeconds`.
   - Giu `Enemy.prefab` lam fallback/entry co weight cao neu enemy moi la bien the dac biet.

9. Playtest.
   - Enemy spawn dung prefab.
   - HP bar reset full sau pool reuse.
   - Bullet player damage enemy.
   - Enemy damage player khi cham.
   - Kill/coin tang dung.
   - Enemy khong day nhau.
   - Shooter dung animation timing va khong ban khi idle/death.

## Melee Enemy

Melee enemy la enemy duoi theo player va gay contact damage.

### Base Setup

- Bat dau tu `Enemy.prefab`.
- Root dung layer `Enemy`.
- Root co `EnemyController`, Rigidbody2D, BoxCollider2D.
- `movementMode = ChaseTarget`.
- `destroyOnPlayerHit = true` neu enemy bien mat sau khi cham player.
- `destroyOnPlayerHit = false` neu enemy co the tiep tuc ton tai sau khi hit, vi du tanker.

### Tuning

- Neu co `UnitData`, `EnemyController` lay:
  - `MaxHealth`
  - `MoveSpeed`
  - `ContactDamage`
- Neu khong co `UnitData`, script dung fallback fields:
  - `fallbackMaxHealth`
  - `fallbackMoveSpeed`
  - `fallbackContactDamage`
- `ScoreValue` va `CoinReward` nam tren `EnemyController`. Neu `coinReward <= 0`, reward fallback ve score.

### Behavior Flow

1. `EnemySpawnerSystem` spawn prefab tu pool hoac instantiate fallback.
2. Spawner goi `SetPoolSystem`, `Init`, roi `Spawn`.
3. `EnemyController` reset HP, active state, health bar.
4. Trong `Update`, enemy move ve player.
5. Khi cham `MainPlayerUnit`, enemy gay contact damage.
6. Khi bi bullet damage den 0 HP, enemy emit `Killed`.
7. Neu `despawnImmediatelyOnDeath = true`, enemy goi `Despawn` ngay.

## Shooter Enemy

Shooter enemy la enemy di vao top band, dung lai, choi attack animation, va ban projectile xuong player. Vomfy la mau hien tai.

### Base Setup

- Bat dau tu `Vomfy.prefab`.
- Root:
  - tag `shooter`
  - layer `Enemy`
  - `EnemyController`
  - `VomfyRangedAttackController` hoac behavior script shooter moi
- `EnemyController`:
  - `movementMode = EnterAndHoldTopBand`
  - `topBandViewportY = 0.75`
  - `despawnImmediatelyOnDeath = false`
- Visual child:
  - co `SpriteRenderer`
  - co `Animator`
  - scale visual tren child de khong lam lech collider

### Animation Contract

Neu dung `VomfyRangedAttackController`, animation state name phai khop:

- `Hop`: khi spawn va di vao vi tri top band.
- `Attackaction`: khi bat dau tan cong.
- `Idle`: nghi giua cac dot tan cong.
- `ow`: death animation.

Script se kiem tra `Animator.HasState` truoc khi play state. Neu state name sai, enemy van chay logic nhung animation co the khong doi dung.

### Attack Timing

Vomfy khong ban theo global timer nua. Script doc normalized time cua state `Attackaction`:

- `fireWindowStartNormalized = 0.45`
- `fireWindowEndNormalized = 0.75`
- `fireOncePerAttackLoop = false`
- `shotInterval = 0.2`

Nghia la Vomfy chi ban trong doan phase B cua `Attackaction`, va lap projectile theo `shotInterval` khi animation dang khè beam. Neu enemy moi co doan khè khac, chi can chinh hai gia tri fire window tren prefab.

### Shooter Lifecycle

1. Spawn: play `Hop`.
2. Enemy di den top band bang `EnterAndHoldTopBand`.
3. Khi `HasArrivedAtHoldPosition = true`, script vao `Attackaction`.
4. Trong phase B, projectile duoc spawn theo `shotInterval`.
5. Het `attackDuration`, script vao `Idle`.
6. Het `idleDuration`, quay lai `Attackaction`.
7. Khi bi kill, play `ow`, dung ban, cho `deathDespawnDelay`, roi goi `Despawn`.

## Enemy Projectile

Projectile enemy hien tai dung `EnemyProjectile`.

### Required Setup

- Root projectile co `EnemyProjectile`.
- Co `Collider2D` bat `IsTrigger`.
- Co `Rigidbody2D` neu can physics callback on trigger/collision on dinh.
- Co `SpriteRenderer`.
- Neu can animation, gan `animationFrames` va `animationFps`.

### Runtime Behavior

- `Init(damage, speed)` duoc shooter goi truoc `Spawn`.
- Khi active, projectile di thang xuong bang `Vector3.down`.
- Khi hit `MainPlayerUnit`, projectile gay damage va despawn.
- Neu het `lifetime`, projectile despawn.
- Projectile co pool support qua `SetPoolSystem`.

### Projectile Pitfalls

- Neu projectile khong hit player, kiem tra collider trigger va layer collision.
- Neu projectile damage enemy, nghia la script khac dang xu ly collision; `EnemyProjectile` mac dinh chi tim `MainPlayerUnit`.
- Neu animation frame khong reset khi reuse, kiem tra `Spawn()` co duoc goi sau khi lay tu pool khong.

## Spawner And Pool

### Weighted Spawn Entries

`EnemySpawnerSystem` dung `spawnEntries` truoc. Moi entry co:

- `prefab`
- `spawnWeight`
- `unlockAfterSeconds`

Neu `spawnEntries` rong hoac total weight khong hop le, spawner fallback ve `enemyPrefab`.

### Recommended Weighting

- Enemy co ban: weight cao, unlock 0s.
- Enemy dac biet/shooter: weight thap hon, unlock 0s hoac sau mot moc thoi gian.
- Enemy nguy hiem hon: weight thap, unlock muon.

### Pool Rules

- Moi enemy prefab spawn trong gameplay nen co pool entry.
- Moi projectile prefab lap lai nhieu lan nen co pool entry.
- Khi tao behavior script rieng, reset runtime state trong spawn/lifecycle event, khong chi reset trong `Awake`.
- Luon unsubscribe event neu object bi destroy, va tranh subscribe double khi pool reuse.

## Common Pitfalls

- **Prefab trong rong**: SpriteRenderer/Animator dang reference sai asset hoac sub-asset chua import. Kiem tra sprite assigned va controller.
- **Enemy day nhau**: root prefab chua o layer `Enemy`, hoac collider/Rigidbody2D chua exclude layer `Enemy`.
- **Scale lam lech collider**: da scale root thay vi scale child `Visual`.
- **HP bar khong reset**: pool reuse nhung `EnemyController.Spawn()`/`Init()` khong duoc goi.
- **Kill/coin double count**: event `Killed` bi subscribe nhieu lan. Spawner hien tai unsubscribe truoc khi subscribe lai.
- **Shooter ban lech animation**: dung global timer thay vi normalized attack window.
- **Shooter van ban khi chet**: behavior script phai chuyen sang dying state khi `EnemyController.Killed` emit.
- **Projectile khong duoc pool**: shooter instantiate fallback lien tuc, can them projectile prefab vao PoolSystem.

## Suggested Workflow For A New Enemy

1. Duplicate prefab gan nhat voi enemy moi.
2. Doi ten prefab va folder asset.
3. Gan visual/animator dung asset moi.
4. Chinh `EnemyController` stats va movement mode.
5. Neu co behavior rieng, tao script rieng layer tren `EnemyController`, dung events `Spawned`, `Killed`, `Despawned`.
6. Neu co projectile, tao projectile prefab rieng va gan vao behavior script.
7. Them enemy/projectile vao PoolSystem.
8. Them enemy vao `EnemySpawnerSystem.spawnEntries`.
9. Playtest trong `Main.unity`.
10. Balance weight, unlock time, speed, HP, damage, attack timing.

## Runtime Scaling V1

Enemy trong V1 duoc scale theo thoi gian boi `RunProgressionConfig` va `EnemySpawnerSystem`.

- Enemy co ban chi can `EnemyController`; HP, move speed, contact damage, score, coin va destroy-on-hit se duoc apply bang runtime stats khi spawn.
- Enemy co damage rieng ngoai contact damage nen implement `IEnemyRuntimeTunable`.
- `ChomboomController` dung interface nay de scale explosion damage.
- `VomfyRangedAttackController` dung interface nay de scale projectile damage va projectile speed.
- Khi tao enemy moi, hay gan enemy vao `EnemySpawnerSystem.spawnEntries`; role co the de Auto neu prefab co behavior ro rang, hoac set role cu the neu can tuning chinh xac.
- V1 khong tao boss/miniboss. Enemy moi nen dong vai tro trong timeline pressure: fodder, exploder, ranged, tank, swarmer, hoac elite-like variant ve stat.

## Future Notes

`EnemyArchetypeData` da ton tai cho huong mo rong enemy progression, gom body color, visual scale, HP, speed, damage, score, coin, va destroy-on-hit. Tuy nhien pipeline prefab hien tai chua apply data nay vao `EnemyController`. Khi nao implement archetype runtime, tai lieu nay nen duoc cap nhat them phan tao enemy bang mot prefab chung + archetype data.
