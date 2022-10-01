import matplotlib.pyplot as plt
import clr
clr.AddReference("WireArrangementCalculator")
from WireArrangementCalculator import Slot, WireArrangement

def PlotSlot(wirearrangement):
    fig, ax = plt.subplots()
    ax.axis('equal')
    wda = wirearrangement.Windings
    for i in range(len(wda)):
        for j in range(len(wda[i])):
            wire = wda[i][j]
            circle = plt.Circle((wire.Center.X, wire.Center.Y), wire.Diameter / 2, color = wire.Color)
            ax.add_patch(circle)
            ax.text(wire.Center.X, wire.Center.Y, wire.Circuit, fontsize = 5, ha = 'center', va = 'center').set_clip_on(True)
    
    slot_outline = wirearrangement.Slot.Outline
    slot_inneroutline = wirearrangement.Slot.InnerOutline
    outx = [slot_outline[i].X for i in range(len(list(slot_outline)))]
    outy = [slot_outline[i].Y for i in range(len(list(slot_outline)))]
    ioutx = [slot_inneroutline[i].X for i in range(len(list(slot_inneroutline)))]
    iouty = [slot_inneroutline[i].Y for i in range(len(list(slot_inneroutline)))]
    
    ax.plot(outx, outy, 'k-')
    ax.plot(ioutx, iouty, 'k-')
    
    

sl = Slot(13.5, 0.5, 0.5, 2.5, 6, 8, 35, 0.001, 0.2)
wa = WireArrangement(sl)
wa.GetWires(0, 0.45, 0.15, 38 * 6);
wa.GetWindings(38, 6, "best");
wa.SetAngleByRad(3.14159265 / 2 * 1)
PlotSlot(wa)

wa2 = WireArrangement(sl)
wa2.GetWires(0, 0.45, 0.105, 16 * 7);
wa2.GetWindings(16, 7, "worst");
wa2.SetAngleByRad(3.14159265 / 4)
PlotSlot(wa2)