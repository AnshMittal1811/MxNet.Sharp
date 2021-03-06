﻿/*****************************************************************************
   Copyright 2018 The MxNet.Sharp Authors. All Rights Reserved.

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
******************************************************************************/
namespace MxNet.Optimizers
{
    public class AdaGrad : Optimizer
    {
        public AdaGrad(float lr, float epsilon = 1e-07f)
        {
            LearningRate = lr;
            Epsilon = epsilon;
        }

        public float Epsilon { get; set; }

        public override NDArrayDict CreateState(int index, NDArray weight)
        {
            var state = new NDArrayDict("history");
            state["history"] = nd.Zeros(weight.Shape, weight.Context, weight.DataType).ToSType(weight.SType);
            return state;
        }

        public override void Update(int index, NDArray weight, NDArray grad, NDArrayDict state)
        {
            UpdateCount(index);
            var lr = GetLr(index);
            var wd = GetWd(index);
            var is_sparse = grad.SType == StorageStype.RowSparse;
            var history = state["history"];

            if (is_sparse)
            {
                nd.SparseAdagradUpdate(weight, grad, history, lr, Epsilon, wd, RescaleGrad,
                    ClipGradient.HasValue ? ClipGradient.Value : -1);
            }
            else
            {
                grad = grad * RescaleGrad;
                if (ClipGradient.HasValue)
                    grad = nd.Clip(grad, -ClipGradient.Value, ClipGradient.Value);

                history += nd.Square(grad);
                var div = grad / nd.Sqrt(history + Epsilon);
                weight += (div + weight * wd) * -lr;
            }
        }
    }
}