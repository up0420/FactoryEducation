using UnityEngine;

public class PpeDemoUI : MonoBehaviour {
    public NetSession net;
    bool[] sel = new bool[5]; // Helmet, Goggles, Gloves, Shoes, Ears
    int maxPick = 3;

    public void Toggle(int idx) {
        int count = Count();
        if (sel[idx]) sel[idx] = false;
        else if (count < maxPick) sel[idx] = true;
    }

    public void Confirm() {
        string[] picks = BuildPicks();
        net.SendPpeSelect(picks);
        Debug.Log("PPE select sent: " + string.Join(",", picks));
    }

    int Count() { int c=0; for(int i=0;i<sel.Length;i++) if(sel[i]) c++; return c; }
    string[] BuildPicks() {
        var list = new System.Collections.Generic.List<string>();
        string[] names = {"Helmet","Goggles","Gloves","SafetyShoes","EarPlugs"};
        for (int i=0;i<sel.Length;i++) if(sel[i]) list.Add(names[i]);
        return list.ToArray();
    }
}
