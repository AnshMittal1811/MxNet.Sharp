﻿using MxNet.Gluon;
using System;
using System.Collections.Generic;
using System.Text;

namespace MxNet.GluonCV.ModelZoo.Centernet
{
    public class DLAUp : HybridBlock
    {
        public DLAUp(int startp, int channels, int[] scales, int in_channels, bool use_dcnv2 = false, string norm_layer = "BatchNorm", FuncArgs norm_kwargs = null, string prefix = null, ParameterDict @params = null) : base(prefix, @params)
        {
            throw new NotImplementedException();
        }

        public override NDArrayOrSymbol HybridForward(NDArrayOrSymbol x, params NDArrayOrSymbol[] args)
        {
            throw new NotImplementedException();
        }
    }
}
