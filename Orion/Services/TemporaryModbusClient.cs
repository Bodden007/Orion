using System;
using System.Buffers.Binary;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Orion.Services;

// TODO: TEMPORARY TEST IMPLEMENTATION.
// Direct Modbus TCP client used only for Orion UI integration testing.
// Remove this class when CoreLink transport is integrated.
public sealed class TemporaryModbusClient : IDisposable
{
    private readonly TcpClient _client =
    new(AddressFamily.InterNetwork);
    private NetworkStream? _stream;
    private ushort _transactionId;

    public async Task ConnectAsync(string host, int port)
    {
        await _client.ConnectAsync(host, port);
        _stream = _client.GetStream();
    }

    public async Task<float[]> ReadValuesAsync()
    {
        if (_stream is null)
            throw new InvalidOperationException("Modbus client is not connected.");

        var transactionId = ++_transactionId;

        // MBAP + FC03
        // HR[0..15] = 8 x Float32
        var request = new byte[12];

        BinaryPrimitives.WriteUInt16BigEndian(
            request.AsSpan(0, 2),
            transactionId);

        // Protocol ID = 0
        BinaryPrimitives.WriteUInt16BigEndian(
            request.AsSpan(2, 2),
            0);

        // Length = Unit ID + PDU = 6
        BinaryPrimitives.WriteUInt16BigEndian(
            request.AsSpan(4, 2),
            6);

        // Unit ID
        request[6] = 1;

        // Function Code 03
        request[7] = 0x03;

        // Start Address = 0
        BinaryPrimitives.WriteUInt16BigEndian(
            request.AsSpan(8, 2),
            0);

        // Quantity = 16 registers
        BinaryPrimitives.WriteUInt16BigEndian(
            request.AsSpan(10, 2),
            16);

        await _stream.WriteAsync(request);

        // MBAP
        var mbap = new byte[7];
        await ReadExactAsync(_stream, mbap);

        var length =
            BinaryPrimitives.ReadUInt16BigEndian(
                mbap.AsSpan(4, 2));

        // Unit ID уже прочитан в MBAP.
        var pdu = new byte[length - 1];

        await ReadExactAsync(_stream, pdu);

        if (pdu[0] != 0x03)
            throw new IOException(
                $"Unexpected Modbus function: 0x{pdu[0]:X2}");

        if (pdu[1] != 32)
            throw new IOException(
                $"Unexpected byte count: {pdu[1]}");

        var values = new float[8];

        for (var i = 0; i < values.Length; i++)
        {
            var offset = 2 + i * 4;

            var bits =
                BinaryPrimitives.ReadInt32BigEndian(
                    pdu.AsSpan(offset, 4));

            values[i] =
                BitConverter.Int32BitsToSingle(bits);
        }

        return values;
    }

    private static async Task ReadExactAsync(
        NetworkStream stream,
        byte[] buffer)
    {
        var offset = 0;

        while (offset < buffer.Length)
        {
            var read = await stream.ReadAsync(
                buffer.AsMemory(offset));

            if (read == 0)
                throw new IOException("Modbus connection closed.");

            offset += read;
        }
    }

    public void Dispose()
    {
        _stream?.Dispose();
        _client.Dispose();
    }
}