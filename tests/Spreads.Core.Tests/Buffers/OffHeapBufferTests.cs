﻿// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using NUnit.Framework;
using Spreads.Buffers;
using Spreads.Utils;
using System;
using System.Linq;

namespace Spreads.Core.Tests.Buffers
{
    [TestFixture]
    public class OffHeapBufferTests
    {
        [Test]
        public unsafe void CouldUseOffHeapBuffer()
        {
#pragma warning disable 618
            Settings.DoAdditionalCorrectnessChecks = false;
#pragma warning restore 618
            var rounds = 1_000;
            var count = 1_00_000;

            OffHeapBuffer<int> ob = default; // this just works
            // OffHeapBuffer<int> ob = new OffHeapBuffer<int>(count);

            using (Benchmark.Run("OB with grow", count * rounds))
            {
                ob.EnsureCapacity((count + 1) * 4);
                for (int r = 0; r < rounds; r++)
                {
                    for (int i = 0; i < count; i++)
                    {
                        // ob.EnsureCapacity((i + 1) * 4);
                        // Unsafe.WriteUnaligned(ob.Pointer + i, i);
                        // ob.DirectBuffer.Write(i * 4, i);
                        ob[i] = i;
                    }
                }
            }

            using (Benchmark.Run("OB via pointer", count * rounds))
            {
                for (int r = 0; r < rounds; r++)
                {
                    for (int i = 0; i < count; i++)
                    {
                        // Unsafe.WriteUnaligned(ob.Pointer + i, i);
                        // ob.DirectBuffer.Write(i * 4, i);
                        ((int*)ob.Data)[i] = i;
                    }
                }
            }

            using (Benchmark.Run("Span read", count * rounds))
            {
                var sp = ob.Span;
                for (int r = 0; r < rounds; r++)
                {
                    for (int i = 0; i < count; i++)
                    {
                        if (sp[i] != i)
                        {
                            Assert.Fail($"sp[i] {sp[i]} != i {i}");
                        }
                    }
                }
            }

            ob.Dispose();
        }
        
        [Test, Explicit("long running")]
        public void OffHeapPoolRentReturnPerformance()
        {
#pragma warning disable 618
            Settings.DoAdditionalCorrectnessChecks = false; // no difference
#pragma warning restore 618

            var sizesKb = new[] { 32, 64, 128, 256, 512, 1024, 2048, 4096 };
            var count = 10_000_000;

            var pool = new OffHeapBufferPool<byte>(2);

            // warmup
            foreach (var size in sizesKb)
            {
                for (int i = 0; i < 1; i++)
                {
                    var offHeapMemory = pool.Rent(size * 1024);
                    pool.Return(offHeapMemory);
                }
            }

            foreach (var size in sizesKb)
            {
                using (Benchmark.Run(size + " kb", count))
                {
                    for (int i = 0; i < count; i++)
                    {
                        var offHeapMemory = pool.Rent(size * 1024);
                        pool.Return(offHeapMemory);
                    }
                }
            }

            Benchmark.Dump();

            Assert.AreEqual(1, pool._pool._objects.Where(x => x != null).Count());
        }

        [Test, Explicit("long running")]
        public void OffHeapPoolRetainDisposePerformance()
        {
#pragma warning disable 618
            Settings.DoAdditionalCorrectnessChecks = false;
#pragma warning restore 618

            var sizesKb = new[] { 32, 64, 128, 256, 512, 1024, 2048, 4096 };
            var count = 5_000_000;
            var maxSize = 4096 * 1024;
            var pool = new OffHeapBufferPool<byte>(2, maxSize);

            // warmup
            foreach (var size in sizesKb)
            {
                for (int i = 0; i < 1; i++)
                {
                    var offHeapMemory = pool.Rent(size * 1024);
                    pool.Return(offHeapMemory);
                }
            }

            foreach (var size in sizesKb)
            {
                if (size * 1024 > maxSize)
                {
                    using (Benchmark.Run(size + " kb, KOPS", count))
                    {
                        for (int i = 0; i < count / 1_000; i++)
                        {
                            var rm = pool.RetainMemory(size * 1024, requireExact: false);
                            rm.Dispose();
                        }
                    }
                }
                else
                {
                    using (Benchmark.Run(size + " kb", count))
                    {
                        for (int i = 0; i < count; i++)
                        {
                            var rm = pool.RetainMemory(size * 1024, requireExact: false);
                            rm.Dispose();
                        }
                    }
                }
            }

            Benchmark.Dump();

            Assert.AreEqual(1, pool._pool._objects.Where(x => x != null).Count());
        }

        [Test]
        public void OffHeapPoolCannotDisposeRetained()
        {
            var pool = new OffHeapBufferPool<byte>(1);
            var offHeapMemory = pool.Rent(32 * 1024);
            var rm = offHeapMemory.Retain();
            Assert.Throws<InvalidOperationException>(() => { pool.Return(offHeapMemory); });
            rm.Dispose();
        }

        [Test]
        public void OffHeapPoolCouldRentReturnWithOverCapacity()
        {
            var pool = new OffHeapBufferPool<byte>(2);
            var offHeapMemory = pool.Rent(32 * 1024);
            var offHeapMemory2 = pool.Rent(32 * 1024);
            var offHeapMemory3 = pool.Rent(32 * 1024);

            ((IDisposable)offHeapMemory).Dispose();
            ((IDisposable)offHeapMemory2).Dispose();
            ((IDisposable)offHeapMemory3).Dispose();


            Assert.Throws<ObjectDisposedException>(() => { pool.Return(offHeapMemory); });
            Assert.Throws<ObjectDisposedException>(() => { pool.Return(offHeapMemory2); });
            Assert.Throws<ObjectDisposedException>(() => { pool.Return(offHeapMemory3); });
            Assert.AreEqual(2, pool._pool._objects.Where(x => x != null).Count());
        }

        [Test]
        public void OffHeapPoolCouldDisposeRented()
        {
            var pool = new OffHeapBufferPool<byte>(2);
            var offHeapMemory = pool.Rent(32 * 1024);
            ((IDisposable)offHeapMemory).Dispose();

            Assert.Throws<ObjectDisposedException>(() => { pool.Return(offHeapMemory); });

            Assert.AreEqual(1, pool._pool._objects.Where(x => x != null).Count());

            var rm = pool.RetainMemory(32 * 1024);
            // ((IDisposable)offHeapMemory).Dispose();
            rm.Dispose();
            Assert.AreEqual(1, pool._pool._objects.Where(x => x != null).Count());
        }
    }
}
