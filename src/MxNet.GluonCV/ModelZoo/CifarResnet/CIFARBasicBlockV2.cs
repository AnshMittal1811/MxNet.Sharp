﻿using MxNet.Gluon;
using System;
using System.Collections.Generic;
using System.Text;

namespace MxNet.GluonCV.ModelZoo.CifarResnet
{
    public class CIFARBasicBlockV2 : HybridBlock
    {
        public CIFARBasicBlockV2(int channels, int stride, bool downsample = false, int in_channels = 0, string norm_layer = "BatchNorm", FuncArgs norm_kwargs = null, string prefix = null, ParameterDict @params = null) : base(prefix, @params)
        {
            throw new NotImplementedException();
        }

        public override NDArrayOrSymbol HybridForward(NDArrayOrSymbol x, params NDArrayOrSymbol[] args)
        {
            throw new NotImplementedException();
        }
    }
}
