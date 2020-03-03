﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Reflection;

namespace MxNet.Gluon.RNN
{
    public class RNNCell : HybridRecurrentCell
    {
        private int _hidden_size;
        private int _counter;

        public RNNCell(int hidden_size, string activation = "tanh", string i2h_weight_initializer = null, string h2h_weight_initializer = null,
                        string i2h_bias_initializer = "zeros", string h2h_bias_initializer = "zeros", int input_size = 0,
                        string prefix = null, ParameterDict @params = null) : base(prefix, @params)
        {
            throw new NotImplementedException();
        }

        public override StateInfo StateInfo(int batch_size = 0)
        {
            return new StateInfo() { Layout = "NC", Shape = new Shape(batch_size, _hidden_size) };
        }

        public override string Alias()
        {
            return "rnn";
        }

        public override NDArrayOrSymbol HybridForward(NDArrayOrSymbol x, params NDArrayOrSymbol[] args)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            throw new NotImplementedException();
        }

        internal static StateInfo[] CellsStateInfo(RNNCell[] cells, int batch_size)
        {
            var ret = cells.Select(x => (x.StateInfo(batch_size))).ToList();
            ret.Add(new RNN.StateInfo());
            return ret.ToArray();
        }

        internal static List<List<NDArrayOrSymbol[]>> CellsBeginState(RNNCell[] cells, int batch_size, string state_func)
        {
            var ret = cells.Select(x => (x.BeginState(batch_size, state_func))).ToList();
            return ret;
        }

        internal static NDArrayOrSymbol[] GetBeginState(RecurrentCell cell, NDArrayOrSymbol[] begin_state, NDArrayOrSymbol inputs, int batch_size)
        {
            if (begin_state != null)
            {
                if (inputs.IsNDArray)
                {
                    var ctx = inputs.NdX.context;
                    var args = new FuncArgs();
                    args.Add("ctx", ctx);
                    begin_state = cell.BeginState(batch_size, func: "nd.Zeros", args);
                }
                else
                {
                    begin_state = cell.BeginState(batch_size, func: "sym.Zeros");
                }
            }

            return begin_state;
        }

        internal static NDArrayOrSymbol[] GetBeginState(RecurrentCell cell, NDArrayOrSymbol[] begin_state, NDArrayOrSymbol[] inputs, int batch_size)
        {
            if (begin_state != null)
            {
                if (inputs[0].IsNDArray)
                {
                    var ctx = inputs[0].NdX.context;
                    var args = new FuncArgs();
                    args.Add("ctx", ctx);
                    begin_state = cell.BeginState(batch_size, func: "nd.Zeros", args);
                }
                else
                {
                    begin_state = cell.BeginState(batch_size, func: "sym.Zeros");
                }
            }

            return begin_state;
        }

        internal static (NDArrayOrSymbol[], int, int) FormatSequence(int length, NDArrayOrSymbol inputs, string layout, bool merge, string in_layout = null)
        {
            int axis = layout.IndexOf('T');
            int batch_axis = layout.IndexOf('N');
            int batch_size = 0;
            int in_axis = !string.IsNullOrWhiteSpace(in_layout) ? in_layout.IndexOf('T') : axis;
            NDArrayOrSymbol[] data_inputs = null;
            if (inputs.IsSymbol)
            {
                if (!merge)
                {
                    if (inputs.SymX.ListOutputs().Count != 1)
                        throw new Exception("unroll doesn't allow grouped symbol as input. Please convert " +
                                            "to list with list(inputs) first or let unroll handle splitting.");
                    data_inputs = new NDArrayOrSymbol[] { sym.Split(inputs.SymX, length, in_axis, true) };
                }
            }
            else if (inputs.IsNDArray)
            {
                batch_size = inputs.NdX.Shape[batch_axis];
                if (!merge)
                {
                    if (length != inputs.NdX.Shape[in_axis])
                        throw new Exception("Invalid length!");

                    data_inputs = nd.Split(inputs.NdX, inputs.NdX.Shape[in_axis], in_axis, true).NDArrayOrSymbols;
                }
            }

            return (data_inputs, axis, batch_size);
        }

        internal static (NDArrayOrSymbol[], int, int) FormatSequence(int length, NDArrayOrSymbol[] inputs, string layout, bool merge, string in_layout = null)
        {
            int axis = layout.IndexOf('T');
            NDArrayOrSymbol data_inputs = null;
            if (inputs[0].IsSymbol)
            {
                data_inputs = sym.Stack(inputs.ToList().ToSymbols(), inputs.Length, axis);
            }
            else if (inputs[0].IsNDArray)
            {
                data_inputs = nd.Stack(inputs.ToList().ToNDArrays(), inputs.Length, axis);
            }

            return FormatSequence(length, data_inputs, layout, merge, in_layout);
        }

        internal static NDArrayOrSymbol[] MaskSequenceVariableLength(NDArrayOrSymbol data, int length, NDArrayOrSymbol valid_length, int time_axis, bool merge)
        {
            NDArrayOrSymbol outputs = null;
            NDArrayOrSymbol[] ret = null;
            if (data.IsNDArray)
                outputs = nd.SequenceMask(data, valid_length, true, time_axis);
            else
                outputs = sym.SequenceMask(data, valid_length, true, time_axis);

            if (!merge)
            {
                if (data.IsSymbol)
                {
                    ret = new NDArrayOrSymbol[] { sym.Split(data.SymX, length, time_axis, true) };
                }
                else if (data.IsNDArray)
                {
                    ret = nd.Split(data, length, time_axis, true).NDArrayOrSymbols;
                }
            }

            return ret;
        }

        internal static NDArrayOrSymbol[] MaskSequenceVariableLength(NDArrayOrSymbol[] data, int length, NDArrayOrSymbol valid_length, int time_axis, bool merge)
        {
            NDArrayOrSymbol outputs = null;
            if (data[0].IsNDArray)
                outputs = nd.Stack(data.ToList().ToNDArrays(), data.Length, time_axis);
            else
                outputs = sym.Stack(data.ToList().ToSymbols(), data.Length, time_axis);

            return MaskSequenceVariableLength(outputs, length, valid_length, time_axis, merge);
        }

        internal static NDArrayOrSymbol[] _reverse_sequences(NDArrayOrSymbol[] sequences, int unroll_step, NDArrayOrSymbol valid_length = null)
        {
            NDArrayOrSymbol[] reversed_sequences = null;
            if (valid_length == null)
            {
                reversed_sequences = sequences;
                reversed_sequences.Reverse();
            }
            else
            {
                NDArrayOrSymbol seqRev = null;
                if (sequences[0].IsNDArray)
                    seqRev = nd.SequenceReverse(nd.Stack(sequences.ToList().ToNDArrays(), sequences.Length, 0), valid_length, true);
                else
                    seqRev = sym.SequenceReverse(sym.Stack(sequences.ToList().ToSymbols(), sequences.Length, 0), valid_length, true);


                if (unroll_step > 1 || sequences[0].IsSymbol)
                {
                    if (sequences[0].IsNDArray)
                        reversed_sequences = nd.Split(seqRev, unroll_step, 0, true).NDArrayOrSymbols;
                    else
                        reversed_sequences = new NDArrayOrSymbol[] { sym.Split(seqRev, unroll_step, 0, true) };
                }
                else
                {
                    reversed_sequences = new NDArrayOrSymbol[] { seqRev };
                }
            }

            return reversed_sequences;
        }
    }
}
