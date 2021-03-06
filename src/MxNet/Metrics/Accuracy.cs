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
namespace MxNet.Metrics
{
    public class Accuracy : EvalMetric
    {
        public Accuracy(int axis = 1, string output_name = null, string label_name = null) : base("accuracy",
            output_name, label_name, true)
        {
            Axis = axis;
        }

        public int Axis { get; set; }

        public override void Update(NDArray labels, NDArray preds)
        {
            CheckLabelShapes(labels, preds, true);

            NDArray pred_label = null;
            if (preds.Shape != labels.Shape)
                pred_label = preds.Argmax(Axis);
            else
                pred_label = preds;

            var label = labels.Ravel().AsType(DType.Int32);
            pred_label = pred_label.Ravel().AsType(DType.Int32);

            var num_correct = nd.Equal(pred_label, label).AsType(DType.Float32).Sum();

            sum_metric += num_correct;
            global_sum_metric += num_correct;
            num_inst += pred_label.Shape.Size;
            global_num_inst += pred_label.Shape.Size;
        }
    }
}