using ByteSizeLib;
using MessagePack;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace DMediatR.Tests.Grpc
{
    // <class>
    [Category("Performance")]
    [Remote("Ping")]
    public class PerformanceMeter : IRemote
    {
        public Remote Remote { get; set; } = default!;
        // </class>

        // Test volume configuration, set to 1/1 if the test is not run to
        // measure accurate performance numbers.

        /// <summary>
        /// Duration in seconds for one of the three payload types for each
        /// payload size datapoint in the [DatapointSource].
        /// </summary>
        private const int TEST_DURATION_SECONDS = 1; // 5;

        /// <summary>
        /// Number of tasks to run in parallel.
        /// </summary>
        private const int TEST_PARALLEL_TASKS = 8;

        [DatapointSource]
        private int[] sizes = { 0, 1024, 1024 * 1024, 10 * 1024 * 1024 };

        [OneTimeSetUp]
        public void SetUpInitialCertificatesStartServer()
        {
            SetUp.SetUpDMediatRServices("RemotePing");
            SetUp.SetUpInitialCertificates();
            Remote = SetUp.ServiceProvider.GetRequiredService<Remote>();
        }

        [TearDown]
        public void StopServer()
        {
            SetUp.StopAllServers();
        }

        [TearDown]
        public void TearDownResetMessagePack()
        {
            MessagePackSerializer.Typeless.DefaultOptions =
                MessagePack.Resolvers.TypelessContractlessStandardResolver.Options;
        }

        #region Remote Ping

        [Theory]
        public async Task GrpcRaw(int size)
        {
            SetUp.SetUpDMediatRServices("RemotePing");
            SetUp.SetUpInitialCertificates();
            SetUp.StartServer("Quiet", 18007, 18008);
            SetUp.AssertServersStarted();

            await this.SendRemote(new Ping("Connect"), CancellationToken.None);
            var pings = new Ping[]
            {
                new Ping { Message = "GrpcRaw Zero", Payload = GetBloatedPayload(size) },
                new Ping { Message = "GrpcRaw Text", Payload = GetCompressiblePayload(size) },
                new Ping { Message = "GrpcRaw Rand", Payload = GetIncompressiblePayload(size) }
            };
            foreach (var ping in pings)
            {
                (var count, var elapsed) = await SendMany(ping, TEST_DURATION_SECONDS, TEST_PARALLEL_TASKS);
                WritePings(ping, count, elapsed);
            }
        }

        [Theory]
        public async Task GrpcGzip(int size)
        {
            SetUp.SetUpDMediatRServices("RemotePing");
            SetUp.SetUpInitialCertificates();
            SetUp.StartServer("Gzip", 18007, 18008);
            SetUp.AssertServersStarted();
            // <sendremote>
            await this.SendRemote(new Ping("Connect"), CancellationToken.None);
            // </sendremote>
            var pings = new Ping[]
            {
                new Ping { Message = "GrpcGzip Zero", Payload = GetBloatedPayload(size) },
                new Ping { Message = "GrpcGzip Text", Payload = GetCompressiblePayload(size) },
                new Ping { Message = "GrpcGzip Rand", Payload = GetIncompressiblePayload(size) }
            };
            foreach (var ping in pings)
            {
                (var count, var elapsed) = await SendMany(ping, TEST_DURATION_SECONDS, TEST_PARALLEL_TASKS);
                WritePings(ping, count, elapsed);
            }
        }

        [Theory]
        public async Task GrpcLz4(int size)
        {
            SetUp.SetUpDMediatRServices("RemotePingLz4");
            SetUp.SetUpInitialCertificates();
            SetUp.StartServer("Lz4BlockArray", 18007, 18008);
            SetUp.AssertServersStarted();

            await this.SendRemote(new Ping("Connect"), CancellationToken.None);
            var pings = new Ping[]
            {
                new Ping { Message = "GrpcLz4 Zero", Payload = GetBloatedPayload(size) },
                new Ping { Message = "GrpcLz4 Text", Payload = GetCompressiblePayload(size) },
                new Ping { Message = "GrpcLz4 Rand", Payload = GetIncompressiblePayload(size) }
            };
            foreach (var ping in pings)
            {
                (var count, var elapsed) = await SendMany(ping, TEST_DURATION_SECONDS, TEST_PARALLEL_TASKS);
                WritePings(ping, count, elapsed);
            }
        }

        /// <summary>
        /// Keep sending the ping to the remote until the time in seconds is up.
        /// Send the specified number of parallel tasks simultaneously.
        /// </summary>
        /// <param name="ping">The ping message to send.</param>
        /// <param name="seconds">The duration in seconds to keep sending the
        /// pings.</param>
        /// <param name="parallelTasks">The number of tasks to run
        /// simultaneously.</param>
        /// <returns>A tuple containing the total count of pings sent and the
        /// exact elapsed time.</returns>
        private async Task<(int, TimeSpan)> SendMany(Ping ping, int seconds, int parallelTasks)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            int count = 0;
            while (stopWatch.Elapsed.Seconds < seconds)
            {
                var tasks = new List<Task>();
                for (int i = 0; i < parallelTasks; i++)
                {
                    tasks.Add(this.SendRemote(ping, CancellationToken.None));
                    count++;
                }
                await Task.WhenAll(tasks);
            }
            return (count, stopWatch.Elapsed);
        }

        private void WritePings(Ping ping, int count, TimeSpan elapsed)
        {
            var totalBytes = count * ping.Payload.Length;
            var bytes = ByteSize.FromBytes(totalBytes);
            var bytesSec = ByteSize.FromBytes(totalBytes / elapsed.TotalSeconds);
            var pingsSec = count / elapsed.TotalSeconds;
            TestContext.WriteLine($"{ping.Message} Data: {bytesSec}/Sec Pings: {pingsSec,0:n}/Sec Count:{count}");
        }

        #endregion Remote Ping

        #region MessagePack Serializer Compression

        [Theory]
        public void Lz4Compression(int size)
        {
            var stopWatch = new Stopwatch();

            foreach (var compression in compressions)
            {
                MessagePackSerializer.Typeless.DefaultOptions = compression; // set globally

                stopWatch.Start();
                var bytes = GetBloatedPayload(size);
                var bloated = MessagePackSerializer.Typeless.Serialize(bytes);
                stopWatch.Stop();
                WriteRatio("Zero", compression, size, bloated.Length, stopWatch.Elapsed);

                stopWatch.Start();
                bytes = GetCompressiblePayload(size);
                var commpressible = MessagePackSerializer.Typeless.Serialize(bytes);
                stopWatch.Stop();
                WriteRatio("Text", compression, size, commpressible.Length, stopWatch.Elapsed);

                stopWatch.Start();
                bytes = GetIncompressiblePayload(size);
                var incompressible = MessagePackSerializer.Typeless.Serialize(bytes);
                stopWatch.Stop();
                WriteRatio("Rand", compression, size, incompressible.Length, stopWatch.Elapsed);
            }
        }

        private MessagePackSerializerOptions[] compressions = {
            MessagePack.Resolvers.TypelessContractlessStandardResolver.Options, // None
            MessagePack.Resolvers.TypelessContractlessStandardResolver.Options.WithCompression(MessagePack.MessagePackCompression.Lz4Block),
            MessagePack.Resolvers.TypelessContractlessStandardResolver.Options.WithCompression(MessagePack.MessagePackCompression.Lz4BlockArray)
        };

        private void WriteRatio(string data, MessagePackSerializerOptions compression, int size, int compressed, TimeSpan elapsed)
        {
            var bytes = ByteSize.FromBytes(size);
            TestContext.WriteLine(
                $"{bytes} {data} - Ratio: {(double)compressed / (double)size,3:F3} Time: {elapsed.Seconds}:{elapsed.Milliseconds:000} Compression: {compression.Compression}");
        }

        #endregion MessagePack Serializer Compression

        #region Bloated Payload

        private byte[] GetBloatedPayload(int size)
        {
            return new byte[size];
        }

        #endregion Bloated Payload

        #region Compressible Payload

        private byte[] GetCompressiblePayload(int size)
        {
            if (size == 0) return []; // CompressibleBytes would return 1 byte
            return CompressibleBytes(size).ToArray();
        }

        /// <summary>
        /// Takes a .cs source file and repeats its content until the desired size is reached.
        /// As Lz4 recognizes the identical blocks, dirty them randomly.
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        private IEnumerable<byte> CompressibleBytes(int size)
        {
            var rnd = TestContext.CurrentContext.Random;
            var rndInterval = Byte.MaxValue / 64; // randomize each 64th character

            var bytes = GetCsSrcFile("RemoteExtension.cs");
            var one = bytes.Length;
            var block = size / one;
            int i = 0;
            int page = 0;
            while (true)
            {
                if (i % ((rnd.NextByte() + rndInterval) / rndInterval) == 0)
                {
                    yield return rnd.NextByte();
                }
                else
                {
                    yield return bytes[i];
                }

                if (i < (one - 1))
                {
                    if (page * one + i < (size - 1))
                    {
                        i++;
                    }
                    else
                    {
                        yield break;
                    }
                }
                else if (page * one < size)
                {
                    page++;
                    i = 0;
                }
            }
        }

        private byte[] GetCsSrcFile(string file)
        {
            var workDir = TestContext.CurrentContext.WorkDirectory;
            var rootDir = Path.Combine(workDir, "..", "..", "..", "..", "..");
            var srcFile = Path.Combine(rootDir, "src", "DMediatR", file);
            return File.ReadAllBytes(srcFile);
        }

        #endregion Compressible Payload

        #region Incompressible Payload

        private byte[] GetIncompressiblePayload(int size)
        {
            return IncompressibleBytes(size).ToArray();
        }

        private IEnumerable<byte> IncompressibleBytes(int size)
        {
            var rnd = TestContext.CurrentContext.Random;
            int i = 0;
            while (i < size)
            {
                yield return rnd.NextByte();
                i++;
            }
        }

        #endregion Incompressible Payload
    }
}