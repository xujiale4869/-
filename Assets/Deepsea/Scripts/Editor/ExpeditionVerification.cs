using System;
using UnityEngine;

namespace Deepsea.Editor
{
    public static class ExpeditionVerification
    {
        static void Check(bool ok,string why){if(!ok)throw new InvalidOperationException("Expedition rules: "+why);}
        public static void RunRules()
        {
            var p=new HarbourProgress();var e=new Expedition();
            AssetRules.Initialize(p);int initial=p.parts;
            AssetRules.Initialize(p);Check(p.parts==initial,"starter provisions cannot be duplicated");
            Check(p.modules.Count==2&&!AssetRules.ToggleModule(p,ShipModule.FieldRepair),"two module slots enforced");
            Check(AssetRules.ToggleModule(p,ShipModule.TowAssist)&&AssetRules.ToggleModule(p,ShipModule.FieldRepair),"alternative repair loadout available");
            Check(AssetRules.TransferParts(p,e,ItemLocation.Village,ItemLocation.Ship,4,10),"village materials load into ship");
            Check(p.parts==0&&p.shipParts==4,"loaded materials do not remain in warehouse");
            Check(AssetRules.TransferParts(p,e,ItemLocation.Ship,ItemLocation.Backpack,2,10),"physical ship to backpack transfer");
            Check(AssetRules.ConsumeFieldParts(p,e,2,true),"repair consumes backpack stock");
            Check(p.packParts==0&&p.shipParts==2&&p.repairDebt==0,"owned repair has no duplicate deduction");
            Check(!AssetRules.ConsumeFieldParts(p,e,3,true),"remote warehouse cannot fund field repair");
            Check(AssetRules.Transfer(p,"bait",ItemLocation.Village,ItemLocation.Ship,1),"bait transfer");
            Check(AssetRules.Transfer(p,"bait",ItemLocation.Ship,ItemLocation.Backpack,1),"bait placed in backpack");
            int total=AssetRules.Stock(p,"bait").village+AssetRules.Stock(p,"bait").ship+AssetRules.Stock(p,"bait").backpack;
            Check(total==3&&AssetRules.Consume(p,"bait",ItemLocation.Backpack),"consumable stock conserved before use");
            Check(!AssetRules.Consume(p,"bait",ItemLocation.Backpack),"same bait cannot be used twice");
            e.TryLoad("field-loot",3,0,10);
            Check(AssetRules.TransferParts(p,e,ItemLocation.Ship,ItemLocation.Backpack,3,10),"pending loot retains identity on transfer");
            Check(AssetRules.ConsumeFieldParts(p,e,3,true)&&p.repairDebt==3,"field construction journals pending costs");
            AssetRules.UnloadOwned(p);e.Commit(p);int settled=p.parts;e.Commit(p);
            Check(p.parts==settled&&p.repairDebt==0,"docking commits pending field repair once");
            AssetRules.AddResident(p,new NamedResident{id="linzhou",name="林舟"});AssetRules.AddResident(p,new NamedResident{id="linzhou",name="林舟"});
            Check(p.people.Count==1,"named resident not duplicated");
            AssetRules.AddBoat(p,new NamedBoat{id="qinglu-boat",name="青鹭号",repaired=false});
            AssetRules.AddBoat(p,new NamedBoat{id="qinglu-boat",name="青鹭号",repaired=true});
            Check(p.boats.Count==1&&p.boats[0].repaired,"tow and repair retain one vessel identity");
            p.island.stationPowered=true;p.technicianAssigned=true;
            p.nodes.Add(new SurveyNode{id="reload-node",x=53,z=94});
            string path=System.IO.Path.Combine(Application.temporaryCachePath,"deepsea-expedition-rules-"+Guid.NewGuid()+".json");
            try
            {
                var store=new SaveStore(path);
                Check(store.Save(p),"expedition save writes to isolated test file");
                HarbourProgress restored=store.Load();
                Check(restored.island.stationPowered&&restored.technicianAssigned&&restored.modules.Contains(ShipModule.FieldRepair),"island progress and loadout survive reload");
                Check(restored.people.Count==1&&restored.boats.Count==1&&restored.boats[0].repaired&&restored.nodes.Count==1,"named assets and survey nodes survive reload");
                Check(AssetRules.Stock(restored,"bait").village==2&&restored.shipParts==p.shipParts&&restored.packParts==p.packParts,"carried stock survives reload without duplication");
            }
            finally
            {
                if(System.IO.File.Exists(path))System.IO.File.Delete(path);
                if(System.IO.File.Exists(path+".bak"))System.IO.File.Delete(path+".bak");
            }
            Debug.Log("DEEPSEA_EXPEDITION_RULES_OK");
        }
    }
}
