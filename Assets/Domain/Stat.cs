using UnityEngine;

public class Stat
{

    public int hp;
    public int mp;
    public int atk;
    public int def;
    public int spd;
    public int mst;
    public int mdf;

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