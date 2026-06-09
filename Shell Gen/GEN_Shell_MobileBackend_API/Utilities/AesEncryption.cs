using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace GEN_Shell_MobileBackend_API.Utilities
{
    public class AesEncryption
    {
        private static readonly int BLOCK_BIT_SIZE = 128;
        private static readonly int KEY_BIT_SIZE = 256;
        private static readonly int SALT_BIT_SIZE = 64;
        private static readonly int ITERATION_COUNTS = 10000;
        public string Encrypt(string message, string token)
        {
            byte[] cryptKey;
            byte[] salt;
            using (var generator = new Rfc2898DeriveBytes(token, SALT_BIT_SIZE / 8, ITERATION_COUNTS)) //HashAlgorithmName.SHA256 => SHA1(default)
            {
                salt = generator.Salt;
                cryptKey = generator.GetBytes(KEY_BIT_SIZE / 8);
            }
            byte[] cipherText;
            byte[] iv;
            using (var aes = new AesManaged
            {
                KeySize = KEY_BIT_SIZE,
                BlockSize = BLOCK_BIT_SIZE,
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7
            })
            {
                aes.GenerateIV();
                iv = aes.IV;
                using (var encrypter = aes.CreateEncryptor(cryptKey, iv))
                using (var cipherStream = new MemoryStream())
                {
                    using (var cryptoStream = new CryptoStream(cipherStream, encrypter, CryptoStreamMode.Write))
                    using (var binaryWriter = new BinaryWriter(cryptoStream))
                    {
                        binaryWriter.Write(Encoding.UTF8.GetBytes(message));
                    }
                    cipherText = cipherStream.ToArray();
                }
            }
            using (var encryptedStream = new MemoryStream())
            {
                using (var binaryWriter = new BinaryWriter(encryptedStream))
                {
                    binaryWriter.Write(salt);
                    binaryWriter.Write(iv);
                    binaryWriter.Write(cipherText);
                }
                return Convert.ToBase64String(encryptedStream.ToArray());
            }
        }
        public string Decrypt(string message, string token)
        {
            try
            {
                Console.WriteLine("Token to be decrypt {0}", token);
                //  string message1 = System.Convert.ToBase64String(message);
                //  byte[] bytes = Encoding.ASCII.GetBytes(author);
                byte[] encryptedMessage = Convert.FromBase64String(message);
                int SaltLength = SALT_BIT_SIZE / 8;
                var cryptSalt = new byte[SaltLength];
                Array.Copy(encryptedMessage, 0, cryptSalt, 0, cryptSalt.Length);
                byte[] cryptKey;
                using (var generator = new Rfc2898DeriveBytes(token, cryptSalt, ITERATION_COUNTS))
                {
                    cryptKey = generator.GetBytes(KEY_BIT_SIZE / 8);
                }
                using (var aes = new AesManaged
                {
                    KeySize = KEY_BIT_SIZE,
                    BlockSize = BLOCK_BIT_SIZE,
                    Mode = CipherMode.CBC,
                    Padding = PaddingMode.PKCS7
                })
                {
                    var iv = new byte[(BLOCK_BIT_SIZE / 8)];
                    Array.Copy(encryptedMessage, SaltLength, iv, 0, iv.Length);
                    using (var decrypter = aes.CreateDecryptor(cryptKey, iv))
                    using (var plainTextStream = new MemoryStream())
                    {
                        using (var decrypterStream = new CryptoStream(plainTextStream, decrypter, CryptoStreamMode.Write))
                        using (var binaryWriter = new BinaryWriter(decrypterStream))
                        {
                            binaryWriter.Write(
                            encryptedMessage,
                            SaltLength + iv.Length,
                            encryptedMessage.Length - SaltLength - iv.Length
                            );
                        }
                        return Encoding.UTF8.GetString(plainTextStream.ToArray());
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("Exception At Decryption {0}", ex.Message);

            }
            return string.Empty;
        }
    }
}
