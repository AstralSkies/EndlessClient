﻿// Original Work Copyright (c) Ethan Moffat 2014-2016
// This file is subject to the GPL v2 License
// For additional details, see the LICENSE file

using System.Collections.Generic;
using System.IO;
using System.Text;
using EOLib.IO.Services;

namespace EOLib.IO.Pub
{
    public abstract class BasePubFile<T> : IPubFile<T>
        where T : class, IPubRecord, new()
    {
        protected readonly List<T> _data;

        public abstract string FileType { get; }

        public int CheckSum { get; set; }

        public int Length => _data.Count;

        public T this[int id]
        {
            get
            {
                if (id < 1 || id > _data.Count)
                    return null;

                return _data[id - 1];
            }
        }

        public IReadOnlyList<T> Data => _data;

        protected BasePubFile()
        {
            _data = new List<T>();
        }

        public byte[] SerializeToByteArray(INumberEncoderService numberEncoderService)
        {
            using (var mem = new MemoryStream()) //write to memory so we can get a CRC for the new RID value
            {
                mem.Write(Encoding.ASCII.GetBytes(FileType), 0, 3);
                mem.Write(numberEncoderService.EncodeNumber(CheckSum, 4), 0, 4);
                mem.Write(numberEncoderService.EncodeNumber(Length, 2), 0, 2);

                mem.WriteByte(numberEncoderService.EncodeNumber(1, 1)[0]);

                foreach (var dataRecord in _data)
                {
                    var toWrite = dataRecord.SerializeToByteArray(numberEncoderService);
                    mem.Write(toWrite, 0, toWrite.Length);
                }

                return mem.ToArray();
            }
        }

        public abstract void DeserializeFromByteArray(byte[] bytes, INumberEncoderService numberEncoderService);
    }
}
