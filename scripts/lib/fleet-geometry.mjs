import {Matrix4,Matrix3,Vector3,Box3} from 'three';
function ancestry(node){const names=[];for(let p=node.getParentNode();p;p=p.getParentNode())names.push(p.getName());return names.join(' ')}
function pointsBounds(points){return new Box3().setFromPoints(points);}
function clusters(points){
 const bounds=pointsBounds(points),c=pointsBounds(points).getCenter(new Vector3());
 let centers=[new Vector3(bounds.min.x,c.y,bounds.max.z),new Vector3(bounds.max.x,c.y,bounds.max.z),new Vector3(bounds.min.x,c.y,bounds.min.z),new Vector3(bounds.max.x,c.y,bounds.min.z)],groups;
 for(let iteration=0;iteration<24;iteration++){
  groups=centers.map(()=>[]);for(const p of points){let index=0,d=Infinity;centers.forEach((c,i)=>{const dd=p.distanceToSquared(c);if(dd<d){d=dd;index=i}});groups[index].push(p)}
  if(groups.some(g=>!g.length))throw Error('Empty wheel cluster');centers=groups.map(g=>pointsBounds(g).getCenter(new Vector3()));
 }
 const items=groups.map((points,i)=>({points,center:centers[i]}));
 items.sort((a,b)=>b.center.z-a.center.z);const front=items.splice(0,2).sort((a,b)=>a.center.x-b.center.x),rear=items.sort((a,b)=>a.center.x-b.center.x);return [...front,...rear];
}
function steeringCorrection(points,center){
 let xx=0,zz=0,xz=0;for(const p of points){const x=p.x-center.x,z=p.z-center.z;xx+=x*x;zz+=z*z;xz+=x*z}
 let angle=.5*Math.atan2(2*xz,xx-zz)+Math.PI/2;while(angle>Math.PI/2)angle-=Math.PI;while(angle< -Math.PI/2)angle+=Math.PI;
 return angle;
}
function getRecords(doc,spec){
 const records=[];for(const node of doc.getRoot().listNodes()){
  if(!node.getMesh()||spec.skip?.(node.getName()))continue;const world=new Matrix4().fromArray(node.getWorldMatrix()),normal=new Matrix3().getNormalMatrix(world);
  for(const primitive of node.getMesh().listPrimitives()){
   if(primitive.getMode()!==4)continue; // Source guide lines are not triangle geometry.
   const positions=[],normals=[],tangents=[];const p=primitive.getAttribute('POSITION'),n=primitive.getAttribute('NORMAL'),t=primitive.getAttribute('TANGENT');
   for(let i=0;i<p.getCount();i++)positions.push(new Vector3().fromArray(p.getElement(i,[])).applyMatrix4(world));
   if(n)for(let i=0;i<n.getCount();i++)normals.push(new Vector3().fromArray(n.getElement(i,[])).applyMatrix3(normal).normalize());
   if(t)for(let i=0;i<t.getCount();i++){const a=t.getElement(i,[]);tangents.push([...new Vector3(...a.slice(0,3)).applyMatrix3(normal).normalize().toArray(),a[3]*(world.determinant()<0?-1:1)])}
   let indices=primitive.getIndices()?Array.from(primitive.getIndices().getArray()):Array.from({length:p.getCount()},(_,i)=>i);if(world.determinant()<0)for(let i=0;i<indices.length;i+=3)[indices[i+1],indices[i+2]]=[indices[i+2],indices[i+1]];
   records.push({name:node.getName(),parent:ancestry(node),material:primitive.getMaterial(),positions,normals,tangents,indices,primitive});
  }
 }return records;
}
function connectedParts(r){
 const parent=r.positions.map((_,i)=>i),seen=new Map(),find=i=>{while(i!==parent[i]){parent[i]=parent[parent[i]];i=parent[i]}return i},join=(a,b)=>{parent[find(a)]=find(b)};
 // Weld for component identification only. Export preserves hard normal seams.
 for(let i=0;i<r.positions.length;i++){const key=r.positions[i].toArray().map(x=>x.toFixed(5)).join(',');if(seen.has(key))join(i,seen.get(key));else seen.set(key,i)}
 for(let i=0;i<r.indices.length;i+=3){join(r.indices[i],r.indices[i+1]);join(r.indices[i],r.indices[i+2])}
 const parts=new Map();for(let i=0;i<r.indices.length;i+=3){const key=find(r.indices[i]);if(!parts.has(key))parts.set(key,[]);parts.get(key).push(...r.indices.slice(i,i+3))}
 return [...parts.values()].map(indices=>({indices,bounds:pointsBounds(indices.map(i=>r.positions[i]))}));
}

export {pointsBounds,clusters,steeringCorrection,getRecords,connectedParts};
