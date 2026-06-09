using Org.BouncyCastle.OpenSsl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using System.Text.RegularExpressions;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Crypto.Parameters;
using System.IO.Compression;
using System.Linq;
using System.Web;

namespace GEN_Shell_MobileBackend_API.Utilities
{
    public static class RsaCrypto
    {
        
        public static string EncryptDataBatchByte(string requestId, string publicKeyText, string plainTxt)
        {
            string encryptedbytes = string.Empty;
            try
            {
                Console.WriteLine("RequestId:{0} EncryptData-plainTextData : '{1}'", requestId, plainTxt);
                byte[] bytes = plainTxt.ToUtf8EncodedByteArray();
                IEnumerable<IEnumerable<byte>> batches = bytes.ChunkBy(2048);
                LinkedList<byte[]> encryptedChunks = new LinkedList<byte[]>();                
                RSACryptoServiceProvider rsaPublicKey = ImportPublicKey(publicKeyText);
                rsaPublicKey.PersistKeyInCsp = false;

                //for encryption, always handle bytes...
                byte[] encryptedBatch = null;
                foreach (IEnumerable<byte> batch in batches)
                {
                    //apply pkcs#1.5 padding and encrypt our data 
                    encryptedBatch = rsaPublicKey.Encrypt(batch.ToArray(), RSAEncryptionPadding.Pkcs1);
                    encryptedChunks.AddLast(encryptedBatch);
                }

                byte[] encryptedBytes = encryptedChunks.SelectMany(chunk => chunk).ToArray();

                //we might want a string representation of our cypher text... base64 will do
                var cypherText = Convert.ToBase64String(encryptedBytes);
                Console.WriteLine("RequestId:{0} EncryptData-cypherText : {1} ", requestId, cypherText);
                cypherText = HttpUtility.UrlEncode(cypherText);
                Console.WriteLine("RequestId:{0} EncryptData-cypherText-Encoded : {1} ", requestId, cypherText);
                return cypherText;
            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0} EncryptData err : '{1}'", requestId, e.StackTrace);
            }
            return encryptedbytes;

        }

        public static string DecryptDataBatchByte(string requestId, string privateKeyText, string encryptedCypherTxt)
        {
            string decryptedData = string.Empty;
            try
            {
                Console.WriteLine("RequestId:{0} DecryptData-cypherTxt : {1}", requestId, encryptedCypherTxt);

                RSACryptoServiceProvider rsaPrivateKey = ImportPrivateKey(privateKeyText);
                var urlDecodedCipherTxt = HttpUtility.UrlDecode(encryptedCypherTxt);
                var bytesEncryptedCypherText = Convert.FromBase64String(urlDecodedCipherTxt);

                //we want to decrypt, therefore we need a csp and load our private key
                //decrypt and strip pkcs#1.5 padding
                var bytesPlainTextData = rsaPrivateKey.Decrypt(bytesEncryptedCypherText, RSAEncryptionPadding.Pkcs1);

                //get our original plainText back...
                string plainTextData = bytesPlainTextData.ToUtf8String();

                Console.WriteLine("RequestId:{0} DecryptData : '{1}'", requestId, plainTextData);
                return plainTextData;
            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0} DecryptData err : {1}", requestId, e.StackTrace);
            }
            return decryptedData;
        }

        public static string EncryptDataCompress(string requestId, string publicKeyText, string plainTxt)
        {
            string encryptedbytes = string.Empty;
            try
            {
                Console.WriteLine("RequestId:{0} EncryptData-plainTextData : '{1}'", requestId, plainTxt);

                RSACryptoServiceProvider rsaPublicKey = ImportPublicKey(publicKeyText);
                //for encryption, always handle bytes...
                var bytesPlainTextData = Zip(plainTxt); //System.Text.Encoding.Unicode.GetBytes(plainTxt);

                //apply pkcs#1.5 padding and encrypt our data 
                var bytesCypherText = rsaPublicKey.Encrypt(bytesPlainTextData, RSAEncryptionPadding.Pkcs1);

                //we might want a string representation of our cypher text... base64 will do
                var cypherText = Convert.ToBase64String(bytesCypherText);

                Console.WriteLine("RequestId:{0} EncryptData-cypherText : {1} ", requestId, cypherText);
                return cypherText;
            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0} EncryptData err : '{1}'", requestId, e.StackTrace);
            }
            return encryptedbytes;

        }

        public static string DecryptDataDecompress(string requestId, string privateKeyText, string cypherTxt)
        {
            string decryptedData = string.Empty;
            try
            {
                Console.WriteLine("RequestId:{0} DecryptData-cypherTxt : {1}", requestId, cypherTxt);                
                //first, get our bytes back from the base64 string ...
                var bytesCypherText = Convert.FromBase64String(cypherTxt);
                RSACryptoServiceProvider rsaPrivateKey = ImportPrivateKey(privateKeyText);
                //we want to decrypt, therefore we need a csp and load our private key
                //decrypt and strip pkcs#1.5 padding
                var bytesPlainTextData = rsaPrivateKey.Decrypt(bytesCypherText, RSAEncryptionPadding.Pkcs1);

                //get our original plainText back...
                var plainTextData = Unzip(bytesPlainTextData);//System.Text.Encoding.Unicode.GetString(bytesPlainTextData);

                Console.WriteLine("RequestId:{0} DecryptData : '{1}'", requestId, plainTextData);
                return plainTextData;
            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0} DecryptData err : {1}", requestId, e.StackTrace);
            }
            return decryptedData;
        }

        public static string EncryptData(string requestId, string publicKeyText, string plainTxt)
        {
            string encryptedbytes = string.Empty;
            try
            {
                Console.WriteLine("RequestId:{0} EncryptData-plainTextData : '{1}'", requestId, plainTxt);

                RSACryptoServiceProvider rsaPublicKey = ImportPublicKey(publicKeyText);
                //for encryption, always handle bytes...
                var bytesPlainTextData = System.Text.Encoding.Unicode.GetBytes(plainTxt);

                //apply pkcs#1.5 padding and encrypt our data 
                var bytesCypherText = rsaPublicKey.Encrypt(bytesPlainTextData, RSAEncryptionPadding.Pkcs1);

                //we might want a string representation of our cypher text... base64 will do
                var cypherText = Convert.ToBase64String(bytesCypherText);

                Console.WriteLine("RequestId:{0} EncryptData-cypherText : {1} ", requestId, cypherText);
                return cypherText;
            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0} EncryptData err : '{1}'", requestId, e.StackTrace);
            }
            return encryptedbytes;

        }

        public static string DecryptData(string requestId, string privateKeyText, string cypherTxt)
        {
            string decryptedData = string.Empty;
            try
            {
                Console.WriteLine("RequestId:{0} DecryptData-cypherTxt : {1}", requestId, cypherTxt);
                
                //first, get our bytes back from the base64 string ...
                var bytesCypherText = Convert.FromBase64String(cypherTxt);
                RSACryptoServiceProvider rsaPrivateKey = ImportPrivateKey(privateKeyText);
                //we want to decrypt, therefore we need a csp and load our private key
                //decrypt and strip pkcs#1.5 padding
                var bytesPlainTextData = rsaPrivateKey.Decrypt(bytesCypherText, RSAEncryptionPadding.Pkcs1);

                //get our original plainText back...
                var plainTextData = System.Text.Encoding.Unicode.GetString(bytesPlainTextData);

                Console.WriteLine("RequestId:{0} DecryptData : '{1}'", requestId, plainTextData);
                return plainTextData;
            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0} DecryptData err : {1}", requestId, e.StackTrace);
            }
            return decryptedData;
        }

        public static RSACryptoServiceProvider ImportPrivateKey(string pem)
        {
            Console.WriteLine("ImportPrivateKey initiated");
            PemReader pr = new PemReader(new StringReader(pem));
            AsymmetricCipherKeyPair KeyPair = (AsymmetricCipherKeyPair)pr.ReadObject();
            RSAParameters rsaParams = DotNetUtilities.ToRSAParameters((RsaPrivateCrtKeyParameters)KeyPair.Private);

            RSACryptoServiceProvider csp = new RSACryptoServiceProvider(2048);// cspParams);
            csp.ImportParameters(rsaParams);
            Console.WriteLine("ImportPrivateKey imported");
            return csp;
        }

        public static RSACryptoServiceProvider ImportPublicKey(string pem)
        {

            Console.WriteLine("ImportPublicKey initiated");
            PemReader pr = new PemReader(new StringReader(pem));
            AsymmetricKeyParameter publicKey = (AsymmetricKeyParameter)pr.ReadObject();
            RSAParameters rsaParams = DotNetUtilities.ToRSAParameters((RsaKeyParameters)publicKey);

            RSACryptoServiceProvider csp = new RSACryptoServiceProvider(2048);// cspParams);
            

            csp.ImportParameters(rsaParams);
            Console.WriteLine("ImportPublicKey imported");
            return csp;
        }

        public static byte[] Zip(string str)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(str);
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(mso, CompressionMode.Compress))
                {
                    CopyTo(msi, gs);
                }
                return mso.ToArray();
            }
        }
        public static string Unzip(byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                {
                    CopyTo(gs, mso);
                }
                return System.Text.Encoding.UTF8.GetString(mso.ToArray());
            }
        }

        public static void CopyTo(Stream src, Stream dest)
        {
            byte[] bytes = new byte[2048];

            int cnt;

            while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
            {
                dest.Write(bytes, 0, cnt);
            }
        }

        public static IEnumerable<IEnumerable<TElement>> ChunkBy<TElement>(this IEnumerable<TElement> source, int chunkSize)
        {
            return source
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / chunkSize)
                .Select(x => x.Select(v => v.Value).ToArray())
                .ToArray();
        }

        public static byte[] ToUtf8EncodedByteArray(this string source)
        {
            // Changed: instead of source.ToCharArray() use source directly
            return Encoding.UTF8.GetBytes(source);
        }

        public static string ToUtf8String(this byte[] sourceBytes)
        {
            return Encoding.UTF8.GetString(sourceBytes);
        }
    }




}
