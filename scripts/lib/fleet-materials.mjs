// Explicit import repairs found in native URP renders. Preserve authored maps.
export function repairMaterials(doc,id){
 let changed=false;
 const fix=(m,color,metal,rough,alpha)=>{m.setExtension('KHR_materials_pbrSpecularGlossiness',null);m.setBaseColorFactor(color).setMetallicFactor(metal).setRoughnessFactor(rough).setAlphaMode(alpha);changed=true;};
 for(const m of doc.getRoot().listMaterials()){
  const name=m.getName(),sg=m.getExtension('KHR_materials_pbrSpecularGlossiness');
  if(/glass|window|windshield|windscreen/i.test(name)&&sg){fix(m,[.16,.22,.24,.23],.06,.13,'BLEND');}
  if(id==='chevrolet-suburban'&&!/glass|window/i.test(name)&&m.getAlphaMode()==='BLEND'){
   const c=m.getBaseColorFactor();c[3]=1;m.setBaseColorFactor(c).setAlphaMode('OPAQUE');changed=true;
  }
  if(id==='ford-mustang-roush'&&/paint/i.test(name))fix(m,[.025,.12,.35,1],.55,.25,'OPAQUE');
 }
 for(const node of doc.getRoot().listNodes()){
  let parent=node,isWheel=false;while(parent){if(/^Wheel_[0-3]$/.test(parent.getName()))isWheel=true;parent=parent.getParentNode();}
  if(!isWheel||!node.getMesh())continue;
  for(const p of node.getMesh().listPrimitives()){
   const m=p.getMaterial();if(!m||m.getAlphaMode()!=='BLEND'||/glass/i.test(m.getName()))continue;
   const sg=m.getExtension('KHR_materials_pbrSpecularGlossiness');if(sg){const c=sg.getDiffuseFactor();c[3]=1;sg.setDiffuseFactor(c);}
   const c=m.getBaseColorFactor();c[3]=1;m.setBaseColorFactor(c).setAlphaMode('OPAQUE');changed=true;
  }
 }
 return changed;
}
