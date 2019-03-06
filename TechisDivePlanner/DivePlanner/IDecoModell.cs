using System;
using System.Collections.Generic;
using System.Text;

namespace DivePlanner
{
    public abstract class DecoModel
    {
        double Ceiling { get; }
        void AddDivePlan();

    }


    enum LegType
    {
        Ascend,
        Descend,
        ConstantDepth,
        DecoStop
    }
}
