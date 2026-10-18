using UnityEngine;
namespace MotorCoast {
public sealed class WorldSpec {
    public string Name,Description,Surface;public int Seed,Layout;public float Relief,Sun;
    public WorldSpec(string name,string description,string surface,int seed,int layout,float relief,float sun){Name=name;Description=description;Surface=surface;Seed=seed;Layout=layout;Relief=relief;Sun=sun;}
}
public static class WorldCatalog {
    public static readonly WorldSpec[] Maps={
        new WorldSpec("Побережье","Дрифт-парк, рампы и морской воздух","grass",3218,0,0,37),
        new WorldSpec("Марина","Порт, склады и контейнерный маршрут","concrete",1893,1,0,24),
        new WorldSpec("Альпийский перевал","Горная петля и подъёмы среди скал","rock",701,2,125,42),
        new WorldSpec("Красный каньон","Извилистая дорога среди каменных гряд","sand",191,2,90,19),
        new WorldSpec("Бульвар","Городские кварталы, площади и парковки","grass",543,3,0,48),
        new WorldSpec("Сосновый бор","Лесные дороги и открытые поляны","grass",4829,4,48,30),
        new WorldSpec("Дюны","Песчаные холмы и свободное бездорожье","sand",7721,5,52,52),
        new WorldSpec("Снежная долина","Снежные горы и длинные повороты","snow",284,2,95,22),
        new WorldSpec("Вулканический берег","Тёмные скалы и приморское шоссе","rock",922,2,64,16),
        new WorldSpec("Солёное озеро","Скоростная прямая и парк трюков","snow",4109,6,0,60)
    };
}
}
