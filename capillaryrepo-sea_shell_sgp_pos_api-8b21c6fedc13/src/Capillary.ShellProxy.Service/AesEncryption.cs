using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using System.Net.Http;
using System.Linq;
using System.Threading.Tasks;
namespace Capillary.ShellProxy.Service
{
    public interface IEncryptionService
    {
        string AESEncryptAsync(string requestId, string message ,string token);
        string AESDecryptAsync(string requestId, string message ,string token);
    }

    public class AesEncryption : IEncryptionService
    {

        int _block_bit_size;
        int _key_bit_size;
        int _salt_bit_size;
        int _iteration_count;
#region Name
	        public AesEncryption()
	        {
	            _block_bit_size = 128;
	            _key_bit_size = 256;
	            _salt_bit_size = 64;
	            _iteration_count =10000;
	        }
	
#endregion
        public string AESDecryptAsync(string requestId , string message ,string token)
        {
           try
            {
                Console.WriteLine("Token to be decrypt {0}", token);
                //  string message1 = System.Convert.ToBase64String(message);
                //  byte[] bytes = Encoding.ASCII.GetBytes(author);
                byte[] encryptedMessage = Convert.FromBase64String(message);
                int SaltLength = _salt_bit_size / 8;
                var cryptSalt = new byte[SaltLength];
                Array.Copy(encryptedMessage, 0, cryptSalt, 0, cryptSalt.Length);
                byte[] cryptKey;
                using (var generator = new Rfc2898DeriveBytes(token, cryptSalt, _iteration_count))
                {
                    cryptKey = generator.GetBytes(_key_bit_size / 8);
                }
                using (var aes = new AesManaged
                {
                    KeySize = _key_bit_size,
                    BlockSize = _block_bit_size,
                    Mode = CipherMode.CBC,
                    Padding = PaddingMode.PKCS7
                })
                {
                    var iv = new byte[(_block_bit_size / 8)];
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

        public string AESEncryptAsync(string requestId, string message ,string token)
        {
            byte[] cryptKey;
            byte[] salt;
            using (var generator = new Rfc2898DeriveBytes(token, _salt_bit_size / 8, _iteration_count))
            {
                salt = generator.Salt;
                cryptKey = generator.GetBytes(_key_bit_size / 8);
            }
            byte[] cipherText;
            byte[] iv;
            using (var aes = new AesManaged
            {
                KeySize = _key_bit_size,
                BlockSize = _block_bit_size,
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
    }
}
