using System;
using System.IO;
using System.Security.Cryptography;
using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;

namespace GameClient.Network.Crypto
{
    public class ECDH
    {
        private BigInteger _privateKey;
        private Org.BouncyCastle.Math.EC.ECPoint _publicKey;
        private static readonly X9ECParameters _curveParams = NistNamedCurves.GetByName("P-256");
        private static readonly ECDomainParameters _domainParams = new ECDomainParameters(
            _curveParams.Curve, _curveParams.G, _curveParams.N, _curveParams.H, _curveParams.GetSeed());

        public ECDH()
        {
            GenerateKeyPair();
        }

        private void GenerateKeyPair()
        {
            var secureRandom = new SecureRandom();
            var keyGenParams = new ECKeyGenerationParameters(_domainParams, secureRandom);
            var keyGenerator = new ECKeyPairGenerator();
            keyGenerator.Init(keyGenParams);

            var keyPair = keyGenerator.GenerateKeyPair();
            _privateKey = ((ECPrivateKeyParameters)keyPair.Private).D;
            _publicKey = ((ECPublicKeyParameters)keyPair.Public).Q;
        }

        public byte[] GetPublicKey()
        {
            var x = _publicKey.AffineXCoord.ToBigInteger().ToByteArrayUnsigned();
            var y = _publicKey.AffineYCoord.ToBigInteger().ToByteArrayUnsigned();

            var publicKey = new byte[64];
            int xOffset = 32 - x.Length;
            int yOffset = 64 - y.Length;
            if (xOffset >= 0)
            {
                for (int i = 0; i < x.Length; i++)
                {
                    publicKey[xOffset + i] = x[i];
                }
            }
            if (yOffset >= 0)
            {
                for (int i = 0; i < y.Length; i++)
                {
                    publicKey[yOffset + i] = y[i];
                }
            }

            return publicKey;
        }

        public byte[] ComputeSharedSecret(byte[] peerPublicKey)
        {
            if (peerPublicKey.Length != 64)
            {
                throw new ArgumentException($"Peer public key must be 64 bytes, got {peerPublicKey.Length}");
            }

            var curve = _domainParams.Curve;
            
            var uncompressedPoint = new byte[65];
            uncompressedPoint[0] = 0x04;
            Array.Copy(peerPublicKey, 0, uncompressedPoint, 1, 64);
            
            var peerPoint = curve.DecodePoint(uncompressedPoint);

            var peerPubKeyParams = new ECPublicKeyParameters("ECDH", peerPoint, _domainParams);

            var agreement = AgreementUtilities.GetBasicAgreement("ECDH");
            agreement.Init(new ECPrivateKeyParameters(_privateKey, _domainParams));
            var sharedSecret = agreement.CalculateAgreement(peerPubKeyParams);

            var sharedSecretBytes = sharedSecret.ToByteArrayUnsigned();

            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(sharedSecretBytes);
                var result = new byte[16];
                Array.Copy(hash, result, 16);
                return result;
            }
        }

        public static byte[] PerformKeyExchange(Stream stream)
        {
            var ecdh = new ECDH();
            byte[] publicKey = ecdh.GetPublicKey();

            stream.Write(publicKey, 0, publicKey.Length);
            stream.Flush();

            byte[] peerPublicKey = new byte[64];
            int totalRead = 0;
            while (totalRead < 64)
            {
                int read = stream.Read(peerPublicKey, totalRead, 64 - totalRead);
                if (read == 0)
                {
                    throw new IOException("Connection closed while reading peer public key");
                }
                totalRead += read;
            }

            return ecdh.ComputeSharedSecret(peerPublicKey);
        }
    }
}
