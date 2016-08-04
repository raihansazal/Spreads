﻿using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Spreads.Buffers;

namespace Spreads.Serialization {

    internal static class BlittableArrayConvertorFactory {
        public static IBinaryConverter<TElement[]> GenericCreate<TElement>() {
            return new BlittableArrayBinaryConverter<TElement>();
        }
        public static object Create(Type type) {
            var method = typeof(BlittableArrayConvertorFactory).GetMethod("GenericCreate");
            var generic = method.MakeGenericMethod(type);
            return generic.Invoke(null, null);
        }
    }

    internal class BlittableArrayBinaryConverter<TElement> : IBinaryConverter<TElement[]> {
        public bool IsFixedSize => false;
        public int Size => 0;
        public byte Version => TypeHelper<TElement>.Version;

        private static readonly int ItemSize = TypeHelper<TElement>.Size;

        public int SizeOf(TElement[] value, out MemoryStream payloadStream) {
            if (ItemSize > 0) {
                payloadStream = null;
                return 8 + ItemSize * value.Length;
            }
            throw new InvalidOperationException("BlittableArrayBinaryConverter must be called only on blittable types");
        }


        public unsafe int Write(TElement[] value, ref DirectBuffer destination, uint offset = 0u, MemoryStream payloadStream = null) {
            if (value == null) throw new ArgumentNullException(nameof(value));
            Debug.Assert(payloadStream == null);
            if (ItemSize > 0) {
                var totalSize = 8 + ItemSize * value.Length;
                if (!destination.HasCapacity(offset, totalSize)) return (int)BinaryConverterErrorCode.NotEnoughCapacity;
                var ptr = destination.Data + (int)offset;

                var pinnedArray = GCHandle.Alloc(value, GCHandleType.Pinned);
                // size
                Marshal.WriteInt32(ptr, totalSize);
                // version
                Marshal.WriteByte(ptr + 4, Version);
                if (value.Length > 0) {
                    var source = Marshal.UnsafeAddrOfPinnedArrayElement(value, 0);
                    ByteUtil.MemoryCopy((byte*)(ptr + 8), (byte*)source, checked((uint)(ItemSize * value.Length)));
                }
                pinnedArray.Free();
                return totalSize;
            }
            throw new InvalidOperationException("BlittableArrayBinaryConverter must be called only on blittable types");
        }

        public unsafe int Read(IntPtr ptr, ref TElement[] value) {
            var totalSize = Marshal.ReadInt32(ptr);
            var version = Marshal.ReadByte(ptr + 4);
            if (version != 0) throw new NotSupportedException("ByteArrayBinaryConverter work only with version 0");
            if (ItemSize > 0) {
                var arraySize = (totalSize - 8) / ItemSize;
                if (arraySize > 0) {
                    var array = new TElement[arraySize];
                    var pinnedArray = GCHandle.Alloc(array, GCHandleType.Pinned);
                    var destination = Marshal.UnsafeAddrOfPinnedArrayElement(array, 0);
                    var source = ptr + 8;
                    ByteUtil.MemoryCopy((byte*)destination, (byte*)source, checked((uint)(totalSize - 8)));
                    value = array;
                    pinnedArray.Free();
                } else {
                    value = new TElement[0];
                }
                return totalSize;
            }
            throw new InvalidOperationException("BlittableArrayBinaryConverter must be called only on blittable types");
        }
    }
}