using UnityEngine;

namespace Deepsea
{
    public enum InteractionKind { Supplies, Rescue, HeavySalvage, Beacon, FishRecord }

    public sealed class WorldInteractable : MonoBehaviour
    {
        public string id;
        public string displayName;
        public InteractionKind kind;
        public int parts, equipment, people;
        public GameDirector game;
        public Towable towable;
        public bool Scanned { get; private set; }
        public bool Taken { get; private set; }
        public bool InView { get { return Scanned || Vector3.Distance(transform.position, game.Boat.transform.position) < 20; } }

        public void MarkScanned() { Scanned = true; }
        public void MarkTaken() { Taken = true; }
        public void ResetTaken() { Taken = false; }

        public string Hint()
        {
            if (Taken) return "";
            switch (kind)
            {
                case InteractionKind.Rescue:
                    if (!game.IsDone(id + "-people")) return "E 接应 " + people + " 人（乘员位独立）";
                    return towable.Attached ? "X 解开拖缆" : "E 拖回故障小船";
                case InteractionKind.HeavySalvage:
                    return game.progress.winch ? "E 连接重型设备拖缆" : "设备过重 · 先建设船坞轻绞盘";
                case InteractionKind.Beacon: return game.progress.beacon ? "航标已修复" : "E 修复航标 · 消耗 2 构件";
                case InteractionKind.FishRecord: return "E 观察并记录荧鳞鱼";
                default: return "E 回收 · " + parts + " 构件 / " + equipment + " 设备";
            }
        }

        public void Interact()
        {
            if (Taken) return;
            if (Vector3.Distance(game.Boat.GetComponent<Rigidbody>().position, transform.position) > game.settings.interactionRadius ||
                game.Boat.Speed > game.settings.interactionSpeed) return;
            Expedition trip = game.trip;
            switch (kind)
            {
                case InteractionKind.Supplies:
                    if (!trip.TryLoad(id, parts, equipment, game.settings.cargoSlots))
                    { game.Notify("货舱装不下这件回收物。先返村卸货。"); return; }
                    Taken = true;
                    game.Notify("已装船：" + displayName + "。返港后进入仓库。");
                    game.Audio.PlayCue(false);
                    gameObject.SetActive(false);
                    break;
                case InteractionKind.Rescue:
                    string rescueId = id + "-people";
                    if (!game.IsDone(rescueId))
                    {
                        if (!trip.TryRescue(rescueId, people, game.settings.passengerSlots))
                        { game.Notify("乘员接应位已满。先送当前乘员回村。"); return; }
                        game.Notify("已接应 " + people + " 人。还可接引船体，或继续探索。");
                        Transform survivor = transform.Find("居民");
                        if (survivor != null) survivor.gameObject.SetActive(false);
                        game.Audio.PlayCue(false);
                    }
                    else if (game.Boat.Attach(towable)) game.Notify("拖缆连接。减速转弯，X 可随时解缆。");
                    else game.Notify("只能拖一个目标，先解开或送回当前目标。");
                    break;
                case InteractionKind.HeavySalvage:
                    if (!game.progress.winch) { game.Notify("这件设备超出一级拖缆承载。港内轻绞盘可以改善它。"); return; }
                    if (game.Boat.Attach(towable)) game.Notify("重型设备已接引。拖回港口直接交付，不占船内货舱。");
                    else game.Notify("已有拖带目标，请先交付或解缆。");
                    break;
                case InteractionKind.FishRecord:
                    if (game.IsDone(id)) return;
                    trip.completed.Add(id);
                    trip.records++;
                    trip.fishRecorded = true;
                    Taken = true;
                    game.Notify("记录完成。返村可用于海图屋；鱼群继续留在浅湾。");
                    game.Audio.PlayCue(false);
                    break;
                case InteractionKind.Beacon:
                    if (game.progress.beacon) return;
                    if (trip.parts >= 2)
                    {
                        trip.parts -= 2;
                        trip.fieldRepairCredit += 2;
                        game.progress.repairDebt += 2;
                    }
                    else if (game.progress.parts >= 2) game.progress.parts -= 2;
                    else { game.Notify("修复需要 2 构件，可使用船上或村仓材料。"); return; }
                    // Field repair is a world change committed immediately, not a loot reward.
                    game.progress.beacon = true;
                    game.Save();
                    game.World.SetBeacon(true);
                    game.Notify("旧航标已恢复。航灯成为永久归航参照。");
                    game.Audio.PlayCue(true);
                    break;
            }
        }
    }
}
