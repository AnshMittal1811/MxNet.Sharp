﻿using MxNet;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace MxNet.IO
{
    public class NDArrayIter : DataIter
    {
        //private NDArray _data;
        //private NDArray _label;

        //private uint num_data;

        //public NDArrayIter(NDArray data, NDArray label)
        //{
        //    _data = data;
        //    _label = label;
        //    BatchSize = 32;
        //    num_data = data.GetShape()[0];
        //    cursor = (int)-BatchSize;
        //}

        //public override void BeforeFirst()
        //{
        //    cursor = (int)-BatchSize;
        //}

        //public override NDArray GetData()
        //{
        //    uint start = (uint)cursor;
        //    uint end = (uint)cursor + BatchSize;
        //    if(end > num_data)
        //    {
        //        end = num_data;
        //    }

        //    return _data.Slice(start, end);
        //}

        //public override int[] GetIndex()
        //{
        //    uint start = (uint)cursor;
        //    uint end = (uint)cursor + BatchSize;
        //    if (end > num_data)
        //    {
        //        end = num_data;
        //    }

        //    List<int> idx = new List<int>();
        //    for (int i = (int)start; i < end; i++)
        //    {
        //        idx.Add((int)i);
        //    }

        //    return idx.ToArray();
        //}

        //public override NDArray GetLabel()
        //{
        //    if (_label == null)
        //        return null;

        //    uint start = (uint)cursor;
        //    uint end = (uint)cursor + BatchSize;
        //    if (end >= num_data)
        //    {
        //        end = num_data;
        //    }

        //    return _label.Slice(start, end).Ravel();
        //}

        //public override int GetPadNum()
        //{
        //    return 0;
        //}

        //public override bool Next()
        //{
        //    cursor += (int)BatchSize;
        //    if(cursor < num_data)
        //    {
        //        return true;
        //    }
        //    else
        //    {
        //        return false;
        //    }
        //}

        private string last_batch_handle;
        private DataBatch first_batch = null;
        private Dictionary<string, NDArray> data;
        private Dictionary<string, NDArray> label;
        private int cursor;
        private uint num_data;
        private int num_source;
        private bool shuffle;
        private NDArray[] _cache_data;
        private NDArray[] _cache_label;
        private NDArray idx;
        private List<NDArray> data_list = new List<NDArray>();

        public override DataDesc[] ProvideData 
        { 
            get
            {
                List<DataDesc> result = new List<DataDesc>();
                foreach (var kv in data)
                {
                    var shape = kv.Value.Shape.Data.ToList();
                    shape.RemoveAt(0);
                    shape.Insert(0, BatchSize);
                    result.Add(new DataDesc(kv.Key, new Shape(shape), kv.Value.DataType));
                }

                return result.ToArray();
            }
        }

        public override DataDesc[] ProvideLabel
        {
            get
            {
                List<DataDesc> result = new List<DataDesc>();
                foreach (var kv in label)
                {
                    var shape = kv.Value.Shape.Data.ToList();
                    shape.RemoveAt(0);
                    shape.Insert(0, BatchSize);
                    result.Add(new DataDesc(kv.Key, new Shape(shape), kv.Value.DataType));
                }

                return result.ToArray();
            }
        }

        public NDArrayIter(NDArray data, NDArray label = null, uint batch_size = 1, bool shuffle = false,
                           string last_batch_handle = "pad", string data_name = "data", string label_name = "softmax_label")
            : this(new NDArray[] { data }, new NDArray[] { label }, batch_size, shuffle, last_batch_handle, data_name, label_name)
        {

        }

        public NDArrayIter(NDArray[] data, NDArray[] label = null, uint batch_size = 1, bool shuffle = false,
                            string last_batch_handle = "pad", string data_name = "data", string label_name = "softmax_label")
        {
            this.data = IOUtils.InitData(data, false, data_name);
            this.label = IOUtils.InitData(label, false, label_name);
            this.idx = nd.Arange((int)data[0].Shape[0]);
            this.BatchSize = batch_size;
            this.cursor = (int)batch_size;
            this.num_data = this.idx.Shape[0];
            this.last_batch_handle = last_batch_handle;
            this.shuffle = shuffle;
            
            this.Reset();
            data_list.AddRange(data);
            data_list.AddRange(label);
            _cache_data = null;
            _cache_label = null;
        }

        public override NDArray[] GetData()
        {
            return _batchify(this.data);
        }

        public override int[] GetIndex()
        {
            throw new NotImplementedException();
        }

        public override NDArray[] GetLabel()
        {
            return _batchify(this.label);
        }

        public override int GetPad()
        {
            if(last_batch_handle == "pad" && cursor + BatchSize > num_data)
            {
                return cursor + (int)BatchSize - (int)num_data;
            }
            else if(last_batch_handle == "roll_over" && ((int)-BatchSize < cursor) && (cursor < 0))
            {
                return -cursor;
            }

            return 0;
        }

        public override bool IterNext()
        {
            cursor += (int)BatchSize;
            return cursor < num_data;
        }

        public override DataBatch Next()
        {
            if (IterNext())
                throw new Exception("Stop Iteration");
            var d = this.GetData();
            var l = GetLabel();
            // iter should stop when last batch is not complete
            if (d[0].Shape[0] != BatchSize)
            {
                //in this case, cache it for next epoch
                _cache_data = d;
                _cache_label = l;
                throw new Exception("Stop Iteration");
            }


            return new DataBatch(data: d, label: l, pad: GetPad());
        }

        private void HardReset()
        {
            if (shuffle)
                ShuffleData();
            cursor = (int)-BatchSize;
            _cache_data = null;
            _cache_label = null;
        }

        public override void Reset()
        {
            if (shuffle)
                ShuffleData();

            if (last_batch_handle == "roll_over" && (num_data - BatchSize < cursor && cursor < num_data)) // (self.cursor - self.num_data) represents the data we have for the last batch
                cursor = (int)(cursor - num_data - BatchSize);
            else
                cursor = (int)-BatchSize;
        }

        public static NDArrayIter FromBatch(DataBatch data_batch)
        {
            var iter = new NDArrayIter(data_batch.Data, data_batch.Label);
            iter.DefaultBucketKey = data_batch.BucketKey.HasValue ? data_batch.BucketKey.Value : new Random().Next();
            return iter;
        }

        private void ShuffleData()
        {
            idx = nd.Shuffle(idx);
            this.data = IOUtils.GetDataByIdx(data, idx);
            this.label = IOUtils.GetDataByIdx(label, idx);
        }

        private NDArray[] _getdata(Dictionary<string, NDArray> data_source, int? start= null, int? end= null)
        {
            if (!start.HasValue && !end.HasValue)
                throw new ArgumentException("Should atleast specify start or end");

            start = start.HasValue ? start : 0;
            end = end.HasValue ? end : (int)data_source.First().Value.Shape[0];

            List<NDArray> result = new List<NDArray>();
            foreach (var x in data_source)
            {
                result.Add(x.Value.Slice((uint)start.Value, (uint)end));
            }

            return result.ToArray();
        }

        private NDArray[] _concat(NDArray[] first_data, NDArray[] second_data)
        {
            if (first_data.Length != second_data.Length)
                throw new Exception("Data source should be of same size.");

            List<NDArray> result = new List<NDArray>();
            for (int i = 0; i < first_data.Length; i++)
            {
                result.Add(
                    nd.Concat(new NDArray[] { first_data[i], second_data[i] }, 2, 0)
                    );
            }

            return result.ToArray();
        }

        private NDArray[] _batchify(Dictionary<string, NDArray> data_source)
        {
            if (cursor > num_data)
                throw new Exception("DataIter need reset");

            if(last_batch_handle == "roll_over" && ((int)-(BatchSize) < cursor && cursor < 0))
            {
                if (_cache_data == null && _cache_label == null)
                    throw new Exception("Next epoch should have cached data");

                var cache_data = this._cache_data != null ? this._cache_data : this._cache_label;
                var second_data = _getdata(data_source, end: cursor + (int)BatchSize);
                if (_cache_data != null)
                    _cache_data = null;
                else
                    _cache_label = null;

                return _concat(cache_data, second_data);
            }
            else if (last_batch_handle == "pad" && (cursor + (int)(BatchSize) > num_data))
            {
                var pad = BatchSize - num_data + cursor;
                var first_data = _getdata(data_source, start: cursor);
                var second_data = _getdata(data_source, end: (int)pad);
                return _concat(first_data, second_data);
            }
            else
            {
                int end_idx = 0;
                if (cursor + BatchSize < num_data)
                {
                    end_idx = cursor + (int)BatchSize;
                }
                else
                {
                    end_idx = (int)num_data;
                }

                return _getdata(data_source, cursor, end_idx);
            }
        }
    }
}
