using System;
using System.Collections.Generic;

namespace Deepsea
{
    // Persistent state contains committed harbour progress only. Expedition inventory
    // never enters the save until docking. This file deliberately has no Unity API.
    [Serializable]
    public sealed class HarbourProgress
    {
        public int schema = 1;
        public int parts;
        public int equipment;
        public int records;
        public int residents = 5;
        public int recoveredBoats;
        public int voyages;
        public int repairDebt;
        public bool winch;
        public bool charts;
        public bool net;
        public bool beacon;
        public bool fishRecorded;
        public List<string> completed = new List<string>();

        public void Normalize()
        {
            if (completed == null) completed = new List<string>();
            completed = new List<string>(new HashSet<string>(completed));
            parts = Math.Max(0, parts);
            equipment = Math.Max(0, equipment);
            records = Math.Max(0, records);
            residents = Math.Max(5, residents);
            repairDebt = Math.Max(0, repairDebt);
        }
    }

    public sealed class Expedition
    {
        public int parts, equipment, records, passengers, recoveredBoats;
        public bool fishRecorded;
        public int fieldRepairCredit;
        public readonly HashSet<string> completed = new HashSet<string>();
        public int CargoUsed { get { return parts + equipment; } }

        public bool TryLoad(string id, int addParts, int addEquipment, int capacity)
        {
            if (completed.Contains(id) || addParts < 0 || addEquipment < 0 ||
                CargoUsed + addParts + addEquipment > capacity) return false;
            parts += addParts;
            equipment += addEquipment;
            completed.Add(id);
            return true;
        }

        public bool TryRescue(string id, int people, int capacity)
        {
            if (completed.Contains(id) || people < 1 || passengers + people > capacity) return false;
            passengers += people;
            completed.Add(id);
            return true;
        }

        public bool HasFindings { get { return completed.Count > 0 || recoveredBoats > 0; } }

        public void Commit(HarbourProgress harbour)
        {
            // Idempotent even if docking input is repeated after a frame hitch.
            if (!HasFindings) return;
            int deliveredParts = parts + fieldRepairCredit;
            int repairPayment = Math.Min(deliveredParts, harbour.repairDebt);
            harbour.parts += deliveredParts - repairPayment;
            harbour.repairDebt -= repairPayment;
            harbour.equipment += equipment;
            harbour.records += records;
            harbour.residents += passengers;
            harbour.recoveredBoats += recoveredBoats;
            harbour.fishRecorded |= fishRecorded;
            harbour.voyages++;
            foreach (string id in completed)
                if (!harbour.completed.Contains(id)) harbour.completed.Add(id);
            Clear();
        }

        public void Clear()
        {
            parts = equipment = records = passengers = recoveredBoats = 0;
            fieldRepairCredit = 0;
            fishRecorded = false;
            completed.Clear();
        }
    }

    public enum Building { Winch, Charts, Net }

    public static class Economy
    {
        public static bool TryBuild(HarbourProgress harbour, Building building,
            int costParts, int costEquipment, int costRecords)
        {
            bool exists = building == Building.Winch ? harbour.winch :
                building == Building.Charts ? harbour.charts : harbour.net;
            if (exists || costParts < 0 || costEquipment < 0 || costRecords < 0 ||
                harbour.parts < costParts || harbour.equipment < costEquipment ||
                harbour.records < costRecords) return false;
            harbour.parts -= costParts;
            harbour.equipment -= costEquipment;
            harbour.records -= costRecords;
            if (building == Building.Winch) harbour.winch = true;
            else if (building == Building.Charts) harbour.charts = true;
            else harbour.net = true;
            return true;
        }

        public static float TowSpeedMultiplier(bool improved, bool towing)
        { return towing ? (improved ? .76f : .49f) : 1f; }
    }
}
