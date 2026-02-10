using System;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace GameClient.Network.Crypto
{
    public static class AES
    {
        public static byte[] EncryptGCM(byte[] data, byte[] key, byte[] nonce = null)
        {
            if (key.Length != 16 && key.Length != 24 && key.Length != 32)
            {
                throw new ArgumentException("Key must be 16, 24, or 32 bytes");
            }

            if (nonce == null)
            {
                nonce = new byte[12];
                var secureRandom = new SecureRandom();
                secureRandom.NextBytes(nonce);
            }
            else if (nonce.Length != 12)
            {
                throw new ArgumentException("Nonce must be 12 bytes for GCM mode");
            }

            var cipher = new GcmBlockCipher(new AesEngine());
            var parameters = new AeadParameters(new KeyParameter(key), 128, nonce);
            cipher.Init(true, parameters);

            byte[] ciphertext = new byte[cipher.GetOutputSize(data.Length)];
            int length = cipher.ProcessBytes(data, 0, data.Length, ciphertext, 0);
            length += cipher.DoFinal(ciphertext, length);

            byte[] result = new byte[nonce.Length + length];
            Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
            Buffer.BlockCopy(ciphertext, 0, result, nonce.Length, length);
            return result;
        }

        public static byte[] DecryptGCM(byte[] data, byte[] key)
        {
            if (key.Length != 16 && key.Length != 24 && key.Length != 32)
            {
                throw new ArgumentException("Key must be 16, 24, or 32 bytes");
            }

            const int nonceLength = 12;

            if (data.Length <= nonceLength)
            {
                throw new ArgumentException("Data too short");
            }

            byte[] nonce = new byte[nonceLength];
            Buffer.BlockCopy(data, 0, nonce, 0, nonceLength);

            int ciphertextLength = data.Length - nonceLength;
            byte[] ciphertext = new byte[ciphertextLength];
            Buffer.BlockCopy(data, nonceLength, ciphertext, 0, ciphertextLength);

            var cipher = new GcmBlockCipher(new AesEngine());
            var parameters = new AeadParameters(new KeyParameter(key), 128, nonce);
            cipher.Init(false, parameters);

            byte[] plaintext = new byte[cipher.GetOutputSize(ciphertext.Length)];
            try
            {
                int length = cipher.ProcessBytes(ciphertext, 0, ciphertext.Length, plaintext, 0);
                length += cipher.DoFinal(plaintext, length);

                byte[] result = new byte[length];
                Buffer.BlockCopy(plaintext, 0, result, 0, length);
                return result;
            }
            catch (Exception ex)
            {
                throw new System.Security.Cryptography.CryptographicException("GCM decryption failed: " + ex.Message, ex);
            }
        }
    }
}
