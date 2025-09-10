using System;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Security.Cryptography;

namespace GameBoy.Core.Persistence;

/// <summary>
/// Handles serialization and deserialization of save states with compression and validation.
/// </summary>
public static class SaveStateSerializer
{
    private const byte SAVE_STATE_MAGIC = 0x47; // 'G' for Game Boy
    private const byte SAVE_STATE_VERSION = 1;

    /// <summary>
    /// Serializes a save state to a compressed byte array.
    /// </summary>
    /// <param name="saveState">The save state to serialize</param>
    /// <returns>Compressed binary data</returns>
    public static byte[] Serialize(SaveState saveState)
    {
        if (saveState == null)
            throw new ArgumentNullException(nameof(saveState));

        using var outputStream = new MemoryStream();

        // Write header
        outputStream.WriteByte(SAVE_STATE_MAGIC);
        outputStream.WriteByte(SAVE_STATE_VERSION);

        // Serialize to JSON first (for compatibility and debugging)
        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(saveState, jsonOptions);

        // Write uncompressed size
        byte[] sizeBytes = BitConverter.GetBytes(jsonBytes.Length);
        outputStream.Write(sizeBytes, 0, sizeof(int));

        // Compress the JSON data
        using (var deflateStream = new DeflateStream(outputStream, CompressionLevel.Optimal, leaveOpen: true))
        {
            deflateStream.Write(jsonBytes, 0, jsonBytes.Length);
        }

        byte[] result = outputStream.ToArray();

        // Add checksum at the end
        using (var sha256 = SHA256.Create())
        {
            byte[] hash = sha256.ComputeHash(result);
            byte[] checksum = new byte[4];
            Array.Copy(hash, 0, checksum, 0, 4); // First 4 bytes of hash

            using var finalStream = new MemoryStream();
            finalStream.Write(result, 0, result.Length);
            finalStream.Write(checksum, 0, checksum.Length);
            return finalStream.ToArray();
        }
    }

    /// <summary>
    /// Deserializes a save state from compressed byte array.
    /// </summary>
    /// <param name="data">Compressed binary data</param>
    /// <returns>Deserialized save state</returns>
    public static SaveState Deserialize(byte[] data)
    {
        if (data == null || data.Length < 10)
            throw new ArgumentException("Invalid save state data", nameof(data));

        using var inputStream = new MemoryStream(data);

        // Verify checksum
        if (data.Length < 4)
            throw new ArgumentException("Save state data too short for checksum");

        byte[] dataWithoutChecksum = new byte[data.Length - 4];
        byte[] expectedChecksum = new byte[4];
        Array.Copy(data, 0, dataWithoutChecksum, 0, dataWithoutChecksum.Length);
        Array.Copy(data, dataWithoutChecksum.Length, expectedChecksum, 0, 4);

        using (var sha256 = SHA256.Create())
        {
            byte[] hash = sha256.ComputeHash(dataWithoutChecksum);
            byte[] actualChecksum = new byte[4];
            Array.Copy(hash, 0, actualChecksum, 0, 4);

            if (!actualChecksum.AsSpan().SequenceEqual(expectedChecksum.AsSpan()))
                throw new InvalidDataException("Save state checksum verification failed");
        }

        // Reset stream to read data without checksum
        inputStream.SetLength(dataWithoutChecksum.Length);
        inputStream.Position = 0;
        inputStream.Write(dataWithoutChecksum, 0, dataWithoutChecksum.Length);
        inputStream.Position = 0;

        // Read and verify header
        byte magic = (byte)inputStream.ReadByte();
        if (magic != SAVE_STATE_MAGIC)
            throw new InvalidDataException("Invalid save state magic number");

        byte version = (byte)inputStream.ReadByte();
        if (version != SAVE_STATE_VERSION)
            throw new InvalidDataException($"Unsupported save state version: {version}");

        // Read uncompressed size
        byte[] sizeBytes = new byte[sizeof(int)];
        inputStream.Read(sizeBytes, 0, sizeof(int));
        int uncompressedSize = BitConverter.ToInt32(sizeBytes, 0);

        if (uncompressedSize <= 0 || uncompressedSize > 10 * 1024 * 1024) // 10MB limit
            throw new InvalidDataException("Invalid uncompressed size");

        // Decompress the data
        byte[] jsonBytes = new byte[uncompressedSize];
        using (var deflateStream = new DeflateStream(inputStream, CompressionMode.Decompress))
        {
            int totalRead = 0;
            while (totalRead < uncompressedSize)
            {
                int read = deflateStream.Read(jsonBytes, totalRead, uncompressedSize - totalRead);
                if (read == 0)
                    throw new InvalidDataException("Unexpected end of compressed data");
                totalRead += read;
            }
        }

        // Deserialize from JSON
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        SaveState? saveState = JsonSerializer.Deserialize<SaveState>(jsonBytes, jsonOptions);
        if (saveState == null)
            throw new InvalidDataException("Failed to deserialize save state");

        return saveState;
    }

    /// <summary>
    /// Validates that a save state is compatible with a given ROM.
    /// </summary>
    /// <param name="saveState">The save state to validate</param>
    /// <param name="romData">The ROM data to validate against</param>
    /// <returns>True if compatible</returns>
    public static bool ValidateCompatibility(SaveState saveState, byte[] romData)
    {
        if (saveState == null || romData == null)
            return false;

        // Generate ROM hash and compare
        string romKey = BatteryStore.GenerateRomKey(romData);
        using (var sha256 = SHA256.Create())
        {
            byte[] hash = sha256.ComputeHash(romData[..Math.Min(romData.Length, 32768)]);
            return hash.AsSpan().SequenceEqual(saveState.CartridgeHash.AsSpan());
        }
    }

    /// <summary>
    /// Gets information about compressed save state data.
    /// </summary>
    /// <param name="data">Compressed save state data</param>
    /// <returns>Information about the save state</returns>
    public static SaveStateInfo GetSaveStateInfo(byte[] data)
    {
        try
        {
            using var inputStream = new MemoryStream(data);

            // Skip checksum validation for info retrieval
            byte[] dataWithoutChecksum = new byte[data.Length - 4];
            Array.Copy(data, 0, dataWithoutChecksum, 0, dataWithoutChecksum.Length);

            inputStream.SetLength(dataWithoutChecksum.Length);
            inputStream.Position = 0;
            inputStream.Write(dataWithoutChecksum, 0, dataWithoutChecksum.Length);
            inputStream.Position = 0;

            // Read header
            byte magic = (byte)inputStream.ReadByte();
            byte version = (byte)inputStream.ReadByte();

            if (magic != SAVE_STATE_MAGIC)
                return new SaveStateInfo { IsValid = false };

            // Read uncompressed size
            byte[] sizeBytes = new byte[sizeof(int)];
            inputStream.Read(sizeBytes, 0, sizeof(int));
            int uncompressedSize = BitConverter.ToInt32(sizeBytes, 0);

            return new SaveStateInfo
            {
                IsValid = true,
                Version = version,
                CompressedSize = data.Length,
                UncompressedSize = uncompressedSize,
                CompressionRatio = (double)data.Length / uncompressedSize
            };
        }
        catch
        {
            return new SaveStateInfo { IsValid = false };
        }
    }
}

/// <summary>
/// Information about a save state file.
/// </summary>
public class SaveStateInfo
{
    public bool IsValid { get; set; }
    public byte Version { get; set; }
    public int CompressedSize { get; set; }
    public int UncompressedSize { get; set; }
    public double CompressionRatio { get; set; }
}