using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;

namespace Sharp.Xmpp
{
    internal class Crypto
    {
        public static byte[] HMACSHA1(byte[] key, byte[] data)
        {
            MacAlgorithmProvider provider = MacAlgorithmProvider.OpenAlgorithm(MacAlgorithmNames.HmacSha1);
            CryptographicHash hash = provider.CreateHash(CryptographicBuffer.CreateFromByteArray(key));
            hash.Append(CryptographicBuffer.CreateFromByteArray(data));
            IBuffer buffer = hash.GetValueAndReset();

            byte[] hashData;
            CryptographicBuffer.CopyToByteArray(buffer, out hashData);

            return hashData;
        }

        public static byte[] SHA1Managed(byte[] data)
        {
            HashAlgorithmProvider provider = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Sha1);
            CryptographicHash hash = provider.CreateHash();
            hash.Append(CryptographicBuffer.CreateFromByteArray(data));

            IBuffer buffer = hash.GetValueAndReset();

            byte[] hashData;
            CryptographicBuffer.CopyToByteArray(buffer, out hashData);

            return hashData;
        }

        public static byte[] RFC2898DerivesBytes(string password, byte[] salt, uint iteration)
        {
            IBuffer saltBuffer = CryptographicBuffer.CreateFromByteArray(salt);
            KeyDerivationParameters kdfParameters = KeyDerivationParameters.BuildForPbkdf2(saltBuffer, iteration);  // 10000 iterations

            // Get a KDF provider for PBKDF2 and hash the source password to a Cryptographic Key using the SHA256 algorithm.
            // The generated key for the SHA256 algorithm is 256 bits (32 bytes) in length.
            KeyDerivationAlgorithmProvider kdf = KeyDerivationAlgorithmProvider.OpenAlgorithm(KeyDerivationAlgorithmNames.Pbkdf2Sha256);
            IBuffer passwordBuffer = CryptographicBuffer.ConvertStringToBinary(password, BinaryStringEncoding.Utf8);
            CryptographicKey passwordSourceKey = kdf.CreateKey(passwordBuffer);

            // Generate key material from the source password, salt, and iteration count
            const int keySize = 256 / 8;  // 256 bits = 32 bytes  
            IBuffer key = CryptographicEngine.DeriveKeyMaterial(passwordSourceKey, kdfParameters, keySize);

            byte[] encryptionKeyOut;

            // send the generated key back to the caller
            CryptographicBuffer.CopyToByteArray(key, out encryptionKeyOut);

            return encryptionKeyOut;
        }
    }
}
