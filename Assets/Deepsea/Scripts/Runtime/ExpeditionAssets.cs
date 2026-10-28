using System;
using System.Collections.Generic;

namespace Deepsea
{
    public enum ItemLocation { Village, Ship, Backpack }
    public enum HandTool { Observe, Repair, CutNet, Buoy, Bait }
    public enum ShipModule { Observation, TowAssist, FieldRepair }

    [Serializable]
    public sealed class ItemStock
    {
        public string id;
        public int village, ship, backpack;
        public int At(ItemLocation place) { return place==ItemLocation.Village?village:place==ItemLocation.Ship?ship:backpack; }
        public void Add(ItemLocation place,int value)
        { if(place==ItemLocation.Village)village+=value;else if(place==ItemLocation.Ship)ship+=value;else backpack+=value; }
    }

    [Serializable]
    public sealed class NamedResident
    {
        public string id, name, skill;
        public string home = "归潮村接应屋";
    }

    [Serializable]
    public sealed class NamedBoat
    {
        public string id, name;
        public bool repaired;
        public string berth = "东侧泊位";
    }

    [Serializable]
    public sealed class SurveyNode
    {
        public string id;
        public float x,z;
    }

    [Serializable]
    public sealed class IslandProgress
    {
        public bool connectorDiagnosed, stationPowered, signalRead, echoRead;
        public bool netCleared, wildlifeLearned, boatDiagnosed, boatRepaired;
    }

    // Inventory rules have no Unity API. Owned provisions are saved separately
    // from pending salvage so quitting cannot duplicate loaded village stock.
    public static class AssetRules
    {
        public static void Normalize(HarbourProgress p)
        {
            if(p.items==null)p.items=new List<ItemStock>();
            if(p.modules==null)p.modules=new List<ShipModule>();
            if(p.people==null)p.people=new List<NamedResident>();
            if(p.boats==null)p.boats=new List<NamedBoat>();
            if(p.nodes==null)p.nodes=new List<SurveyNode>();
            if(p.island==null)p.island=new IslandProgress();
            p.shipParts=Math.Max(0,p.shipParts);p.packParts=Math.Max(0,p.packParts);
            foreach(ItemStock s in p.items)
            {s.village=Math.Max(0,s.village);s.ship=Math.Max(0,s.ship);s.backpack=Math.Max(0,s.backpack);}
        }
        public static void Initialize(HarbourProgress p)
        {
            Normalize(p);
            if(p.expeditionKitReady)return;
            p.expeditionKitReady=true;
            p.parts+=4;
            p.items.Add(new ItemStock{id="buoy",village=2});
            p.items.Add(new ItemStock{id="bait",village=3});
            p.modules.Add(ShipModule.Observation);p.modules.Add(ShipModule.TowAssist);
        }

        public static ItemStock Stock(HarbourProgress p,string id)
        {
            ItemStock found=p.items.Find(s=>s.id==id);
            if(found!=null)return found;
            found=new ItemStock{id=id};p.items.Add(found);return found;
        }
        public static bool Transfer(HarbourProgress p,string id,ItemLocation from,ItemLocation to,int amount)
        {
            ItemStock item=Stock(p,id);
            if(from==to||amount<1||item.At(from)<amount)return false;
            item.Add(from,-amount);item.Add(to,amount);return true;
        }
        public static bool Consume(HarbourProgress p,string id,ItemLocation from)
        {ItemStock item=Stock(p,id);if(item.At(from)<1)return false;item.Add(from,-1);return true;}

        public static bool ToggleModule(HarbourProgress p,ShipModule module)
        {
            if(p.modules.Contains(module)){p.modules.Remove(module);return true;}
            if(p.modules.Count>=2)return false;
            p.modules.Add(module);return true;
        }
        public static int PartsAt(HarbourProgress p,Expedition e,ItemLocation location)
        {return location==ItemLocation.Village?p.parts:location==ItemLocation.Ship?p.shipParts+e.parts:p.packParts+e.packParts;}

        public static bool TransferParts(HarbourProgress p,Expedition e,ItemLocation from,ItemLocation to,int amount,int cargoCapacity)
        {
            if(from==to||amount<1||PartsAt(p,e,from)<amount)return false;
            if(to==ItemLocation.Ship&&p.shipParts+e.CargoUsed+amount>cargoCapacity)return false;
            if(to==ItemLocation.Backpack&&p.packParts+e.packParts+amount>6)return false;
            if(from!=ItemLocation.Village&&to==ItemLocation.Village)return false; // pending loot must be docked
            int pending=from==ItemLocation.Ship?Math.Min(e.parts,amount):from==ItemLocation.Backpack?Math.Min(e.packParts,amount):0;
            int owned=amount-pending;
            if(from==ItemLocation.Village)p.parts-=owned;
            else if(from==ItemLocation.Ship){e.parts-=pending;p.shipParts-=owned;}
            else{e.packParts-=pending;p.packParts-=owned;}
            if(to==ItemLocation.Ship){e.parts+=pending;p.shipParts+=owned;}
            else if(to==ItemLocation.Backpack){e.packParts+=pending;p.packParts+=owned;}
            return true;
        }

        public static bool ConsumeFieldParts(HarbourProgress p,Expedition e,int amount,bool backpack)
        {
            if(amount<0)return false;
            int owned=backpack?p.packParts:p.shipParts;
            int pending=backpack?e.packParts:e.parts;
            if(owned+pending<amount)return false;
            int paid=Math.Min(owned,amount),deferred=amount-paid;
            if(backpack){p.packParts-=paid;e.packParts-=deferred;}else{p.shipParts-=paid;e.parts-=deferred;}
            e.fieldRepairCredit+=deferred;p.repairDebt+=deferred;
            return true;
        }

        public static void UnloadOwned(HarbourProgress p)
        {p.parts+=p.shipParts+p.packParts;p.shipParts=p.packParts=0;}

        public static void AddResident(HarbourProgress p,NamedResident person)
        {if(!p.people.Exists(x=>x.id==person.id))p.people.Add(person);}
        public static void AddBoat(HarbourProgress p,NamedBoat boat)
        {
            NamedBoat found=p.boats.Find(x=>x.id==boat.id);
            if(found==null)p.boats.Add(boat);else found.repaired|=boat.repaired;
        }
        public static bool RepairBerthBoat(HarbourProgress p,string id)
        {
            NamedBoat boat=p.boats.Find(b=>b.id==id);
            if(boat==null||boat.repaired||p.parts<2)return false;
            p.parts-=2;boat.repaired=true;return true;
        }
    }
}
