using UnityEngine;

public class Stat
{

    public int hp { get; set; }
    public int mp { get; set; }
    public int atk { get; set; }
    public int def { get; set; }
    public int spd { get; set; }
    public int mst { get; set; }
    public int mdf { get; set; }


    public Stat(int hp, int mp, int atk, int def, int spd, int mst, int mdf)
    {
        this.hp = hp;
        this.mp = mp;
        this.atk = atk;
        this.def = def;
        this.spd = spd;
        this.mst = mst;
        this.mdf = mdf;
    }



}