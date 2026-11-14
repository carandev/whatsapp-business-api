using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Carandev.WhatsApp.Sdk.Flows.Internal;

/// <summary>
/// AES-GCM (NIST SP 800-38D) with a 16-byte tag and an IV of any length. WhatsApp Flows uses a
/// 16-byte IV, but <see cref="AesGcm"/> only accepts 12-byte nonces, so this builds GCM from the
/// platform's AES block cipher (<see cref="SymmetricAlgorithm.EncryptEcb(byte[], PaddingMode)"/>)
/// and a GHASH whose multiply has no branches or table lookups that depend on secret data.
/// </summary>
internal static class FlowAesGcm
{
    public const int TagSize = 16;

    private const int BlockSize = 16;
    private const ulong ReductionPolynomial = 0xE100000000000000UL;

    /// <summary>
    /// Encrypts <paramref name="plaintext"/> and returns <c>ciphertext ‖ tag</c>.
    /// </summary>
    public static byte[] Encrypt(byte[] key, byte[] iv, byte[] plaintext, byte[]? associatedData = null)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(iv);
        ArgumentNullException.ThrowIfNull(plaintext);
        associatedData ??= [];

        using var aes = CreateAes(key);
        var (hashKeyHigh, hashKeyLow) = HashKey(aes);
        var counterBlock = InitialCounterBlock(iv, hashKeyHigh, hashKeyLow);

        var output = new byte[plaintext.Length + TagSize];
        ApplyKeystream(aes, counterBlock, plaintext, output);

        var tag = ComputeTag(aes, counterBlock, hashKeyHigh, hashKeyLow, associatedData, output.AsSpan(0, plaintext.Length));
        tag.CopyTo(output, plaintext.Length);
        return output;
    }

    /// <summary>
    /// Verifies the tag at the end of <paramref name="ciphertextAndTag"/> and returns the plaintext.
    /// </summary>
    /// <exception cref="CryptographicException">The input is shorter than a tag, or the tag does not match.</exception>
    public static byte[] Decrypt(byte[] key, byte[] iv, byte[] ciphertextAndTag, byte[]? associatedData = null)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(iv);
        ArgumentNullException.ThrowIfNull(ciphertextAndTag);
        associatedData ??= [];

        if (ciphertextAndTag.Length < TagSize)
        {
            throw new CryptographicException("The encrypted data is shorter than the authentication tag.");
        }

        var ciphertextLength = ciphertextAndTag.Length - TagSize;
        var ciphertext = ciphertextAndTag.AsSpan(0, ciphertextLength);

        using var aes = CreateAes(key);
        var (hashKeyHigh, hashKeyLow) = HashKey(aes);
        var counterBlock = InitialCounterBlock(iv, hashKeyHigh, hashKeyLow);

        var expectedTag = ComputeTag(aes, counterBlock, hashKeyHigh, hashKeyLow, associatedData, ciphertext);
        if (!CryptographicOperations.FixedTimeEquals(expectedTag, ciphertextAndTag.AsSpan(ciphertextLength)))
        {
            throw new CryptographicException("The authentication tag does not match.");
        }

        var plaintext = new byte[ciphertextLength];
        ApplyKeystream(aes, counterBlock, ciphertext, plaintext);
        return plaintext;
    }

    private static Aes CreateAes(byte[] key)
    {
        if (key.Length is not (16 or 24 or 32))
        {
            throw new CryptographicException("The AES key must be 16, 24, or 32 bytes.");
        }

        var aes = Aes.Create();
        aes.Key = key;
        return aes;
    }

    private static (ulong High, ulong Low) HashKey(Aes aes)
    {
        var hashKey = aes.EncryptEcb(new byte[BlockSize], PaddingMode.None);
        var result = (BinaryPrimitives.ReadUInt64BigEndian(hashKey), BinaryPrimitives.ReadUInt64BigEndian(hashKey.AsSpan(8)));
        CryptographicOperations.ZeroMemory(hashKey);
        return result;
    }

    /// <summary>
    /// J0: <c>IV ‖ 0³¹ ‖ 1</c> for a 12-byte IV, otherwise
    /// <c>GHASH(IV ‖ 0-padding ‖ 0⁶⁴ ‖ [len(IV)·8]₆₄)</c>.
    /// </summary>
    private static byte[] InitialCounterBlock(byte[] iv, ulong hashKeyHigh, ulong hashKeyLow)
    {
        if (iv.Length == 0)
        {
            throw new CryptographicException("The IV must not be empty.");
        }

        var counterBlock = new byte[BlockSize];
        if (iv.Length == 12)
        {
            iv.CopyTo(counterBlock, 0);
            counterBlock[15] = 1;
            return counterBlock;
        }

        ulong high = 0, low = 0;
        Absorb(ref high, ref low, hashKeyHigh, hashKeyLow, iv);
        AbsorbBlock(ref high, ref low, hashKeyHigh, hashKeyLow, 0, (ulong)iv.Length * 8);
        BinaryPrimitives.WriteUInt64BigEndian(counterBlock, high);
        BinaryPrimitives.WriteUInt64BigEndian(counterBlock.AsSpan(8), low);
        return counterBlock;
    }

    /// <summary>
    /// GCTR from <c>inc32(J0)</c>: XORs <paramref name="input"/> with the keystream into
    /// <paramref name="output"/>.
    /// </summary>
    private static void ApplyKeystream(Aes aes, byte[] counterBlock, ReadOnlySpan<byte> input, Span<byte> output)
    {
        if (input.IsEmpty)
        {
            return;
        }

        var blockCount = (input.Length + BlockSize - 1) / BlockSize;
        var counters = new byte[blockCount * BlockSize];
        var counter = BinaryPrimitives.ReadUInt32BigEndian(counterBlock.AsSpan(12));
        for (var i = 0; i < blockCount; i++)
        {
            var block = counters.AsSpan(i * BlockSize, BlockSize);
            counterBlock.AsSpan(0, 12).CopyTo(block);
            counter = unchecked(counter + 1);
            BinaryPrimitives.WriteUInt32BigEndian(block[12..], counter);
        }

        var keystream = aes.EncryptEcb(counters, PaddingMode.None);
        for (var i = 0; i < input.Length; i++)
        {
            output[i] = (byte)(input[i] ^ keystream[i]);
        }

        CryptographicOperations.ZeroMemory(keystream);
    }

    /// <summary>
    /// <c>T = E(K, J0) ⊕ GHASH(A ‖ pad ‖ C ‖ pad ‖ [len(A)·8]₆₄ ‖ [len(C)·8]₆₄)</c>.
    /// </summary>
    private static byte[] ComputeTag(Aes aes, byte[] counterBlock, ulong hashKeyHigh, ulong hashKeyLow, ReadOnlySpan<byte> associatedData, ReadOnlySpan<byte> ciphertext)
    {
        ulong high = 0, low = 0;
        Absorb(ref high, ref low, hashKeyHigh, hashKeyLow, associatedData);
        Absorb(ref high, ref low, hashKeyHigh, hashKeyLow, ciphertext);
        AbsorbBlock(ref high, ref low, hashKeyHigh, hashKeyLow, (ulong)associatedData.Length * 8, (ulong)ciphertext.Length * 8);

        var tag = aes.EncryptEcb(counterBlock, PaddingMode.None);
        BinaryPrimitives.WriteUInt64BigEndian(tag, BinaryPrimitives.ReadUInt64BigEndian(tag) ^ high);
        BinaryPrimitives.WriteUInt64BigEndian(tag.AsSpan(8), BinaryPrimitives.ReadUInt64BigEndian(tag.AsSpan(8)) ^ low);
        return tag;
    }

    /// <summary>
    /// Feeds <paramref name="data"/> to GHASH in 16-byte blocks, zero-padding the last one.
    /// </summary>
    private static void Absorb(ref ulong high, ref ulong low, ulong hashKeyHigh, ulong hashKeyLow, ReadOnlySpan<byte> data)
    {
        Span<byte> last = stackalloc byte[BlockSize];
        for (var offset = 0; offset < data.Length; offset += BlockSize)
        {
            var block = data[offset..Math.Min(offset + BlockSize, data.Length)];
            if (block.Length < BlockSize)
            {
                last.Clear();
                block.CopyTo(last);
                AbsorbBlock(ref high, ref low, hashKeyHigh, hashKeyLow, BinaryPrimitives.ReadUInt64BigEndian(last), BinaryPrimitives.ReadUInt64BigEndian(last[8..]));
                continue;
            }

            AbsorbBlock(ref high, ref low, hashKeyHigh, hashKeyLow, BinaryPrimitives.ReadUInt64BigEndian(block), BinaryPrimitives.ReadUInt64BigEndian(block[8..]));
        }
    }

    private static void AbsorbBlock(ref ulong high, ref ulong low, ulong hashKeyHigh, ulong hashKeyLow, ulong blockHigh, ulong blockLow)
    {
        high ^= blockHigh;
        low ^= blockLow;
        Multiply(ref high, ref low, hashKeyHigh, hashKeyLow);
    }

    /// <summary>
    /// <c>X · H</c> in GF(2¹²⁸) with GCM's bit order (SP 800-38D, algorithm 1). Every step runs the
    /// same instructions whatever the bits are: secret-dependent choices use masks, not branches.
    /// </summary>
    private static void Multiply(ref ulong xHigh, ref ulong xLow, ulong hashKeyHigh, ulong hashKeyLow)
    {
        ulong zHigh = 0, zLow = 0;
        ulong vHigh = hashKeyHigh, vLow = hashKeyLow;

        for (var i = 0; i < 128; i++)
        {
            var bit = i < 64 ? (xHigh >> (63 - i)) & 1 : (xLow >> (127 - i)) & 1;
            var mask = 0UL - bit;
            zHigh ^= vHigh & mask;
            zLow ^= vLow & mask;

            var carry = vLow & 1;
            vLow = (vLow >> 1) | (vHigh << 63);
            vHigh = (vHigh >> 1) ^ (ReductionPolynomial & (0UL - carry));
        }

        xHigh = zHigh;
        xLow = zLow;
    }
}
