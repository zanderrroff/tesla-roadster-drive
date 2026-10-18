using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using GLTFast;
using UnityEngine;
using UnityEngine.Networking;
namespace MotorCoast {
public sealed class RemoteVehicle:MonoBehaviour {
    GltfImport imported;
    public static bool Checksum(byte[] data,string expected){using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(data)).Replace("-","").Equals(expected,StringComparison.OrdinalIgnoreCase);}
    public static async Task<GameObject> Load(VehicleSpec spec,bool ultra,Action<string> progress){
        string url=ultra?spec.highModelUrl:spec.modelUrl,sha=ultra?spec.highModelSha256:spec.modelSha256;
        string folder=Path.Combine(Application.persistentDataPath,"vehicles");Directory.CreateDirectory(folder);string cache=Path.Combine(folder,sha+".glb");byte[] bytes=null;
        if(File.Exists(cache)){bytes=await File.ReadAllBytesAsync(cache);if(!Checksum(bytes,sha)){File.Delete(cache);bytes=null;}}
#if UNITY_EDITOR
        string local=Path.GetFullPath("../artifacts/expanded-content/"+spec.id+(ultra?"-ultra":"")+".glb");
        if(!File.Exists(local)&&spec.highModelSha256==spec.modelSha256)local=Path.GetFullPath("../artifacts/expanded-content/"+spec.id+".glb");
        if(bytes==null&&File.Exists(local)){bytes=await File.ReadAllBytesAsync(local);if(!Checksum(bytes,sha))throw new IOException("Local vehicle checksum mismatch");}
#endif
        if(bytes==null){
            using(var request=UnityWebRequest.Get(url)){
                request.timeout=90;var operation=request.SendWebRequest();
                while(!operation.isDone){progress?.Invoke("Загрузка "+spec.label+" · "+Mathf.RoundToInt(request.downloadProgress*100)+"%");await Task.Yield();}
                if(request.result!=UnityWebRequest.Result.Success)throw new IOException("Нет связи с загрузкой машины. Проверьте интернет и повторите.");
                bytes=request.downloadHandler.data;
                if(!Checksum(bytes,sha))throw new IOException("Файл машины повреждён. Повторите загрузку.");
            }
            await File.WriteAllBytesAsync(cache+".part",bytes);File.Move(cache+".part",cache);
        }
        progress?.Invoke("Подготовка салона и материалов…");var log=new GLTFast.Logging.CollectingLogger();var import=new GltfImport(logger:log);GameObject model=null;
        try{
            if(!await import.Load(bytes,new Uri(url),new ImportSettings{GenerateMipMaps=true,AnisotropicFilterLevel=8})){log.LogAll();throw new IOException("Не удалось прочитать модель: "+spec.label);}
            model=new GameObject(spec.label+" model");var owner=model.AddComponent<RemoteVehicle>();owner.imported=import;
            if(!await import.InstantiateMainSceneAsync(model.transform))throw new IOException("Не удалось создать модель машины");
            foreach(string node in new[]{"Chassis","SteeringWheel","Wheel_0","Wheel_1","Wheel_2","Wheel_3"})if(!Nodes.Find(model.transform,node))throw new IOException("Модель не содержит "+node);
            return model;
        }catch{if(model)Destroy(model);else import.Dispose();throw;}
    }
    void OnDestroy(){imported?.Dispose();}
}
}
