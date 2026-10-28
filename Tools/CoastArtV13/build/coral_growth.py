# Space-colonization growth adapted from NejcHirci/procedural-corals, MIT.
# Copyright (c) 2022 Maya Nedeljkovich Batić. Full upstream license is retained.
import random,math
from mathutils import Vector
def grow(seed,fan=False):
    rng=random.Random(seed)
    targets=[]
    for i in range(150):
        a=rng.uniform(0,2*math.pi);r=math.sqrt(rng.random())
        targets.append(Vector((math.cos(a)*r*.60,math.sin(a)*r*(.095 if fan else .48),rng.uniform(.25,1.12))))
    nodes=[Vector((0,0,.04))];parents=[-1];directions=[Vector((0,0,1))]
    for i in range(5):
        a=i*2.399;nodes.append(Vector((math.cos(a)*.13,math.sin(a)*(.025 if fan else .12),.18)));parents.append(0);directions.append((nodes[-1]-nodes[0]).normalized())
    for iteration in range(20):
        influence={};remaining=[]
        for target in targets:
            distances=[(target-p).length for p in nodes];nearest=min(range(len(nodes)),key=lambda k:distances[k])
            if distances[nearest]<.12:continue
            remaining.append(target)
            if distances[nearest]<.62:influence.setdefault(nearest,[]).append(target)
        targets=remaining;new=[]
        for parent,attractors in influence.items():
            centre=sum(attractors,Vector())/len(attractors)
            desired=(centre-nodes[parent]).normalized()
            direction=(desired*.80+directions[parent]*.20+Vector((0,0,.04))).normalized()
            end=nodes[parent]+direction*.135
            if all((end-p).length>.055 for p in nodes):new.append((end,parent,direction))
        if not new:break
        for end,parent,direction in new:
            if len(nodes)>=83:break
            nodes.append(end);parents.append(parent);directions.append(direction)
        if len(nodes)>=83:break
    children=[0]*len(nodes)
    for i in range(len(nodes)-1,0,-1):children[parents[i]]+=1+children[i]
    segments=[]
    for i in range(1,len(nodes)):
        parent=parents[i];r1=.009+math.sqrt(children[i])*.0045;r0=.012+math.sqrt(children[parent])*.0045
        segments.append((nodes[parent],nodes[i],r0,r1))
    return segments
