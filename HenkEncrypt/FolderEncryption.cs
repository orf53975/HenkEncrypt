/* HenkEncrypt
 * Copyright (C) 2019  henkje (henkje@pm.me)
 * 
 * MIT license
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System.Text;
using System.Security.Cryptography;
using System.IO;
using System.IO.Compression;/* References>Add Reference>System.IO.Compression.FileSystem */

namespace HenkEncrypt
{
    public static class FolderEncryption
    {
        public static byte[] CreateKey(SymmetricAlgorithm Algorithm, string Password, string Salt = "HenkEncryptSalt", int Iterations = 10000, int KeySize = 0)
        {
            if (Salt.Length < 8) throw new System.Exception("Salt is too short.");

            Rfc2898DeriveBytes Key = new Rfc2898DeriveBytes(Password, Encoding.UTF8.GetBytes(Salt), Iterations);
            if (KeySize <= 0) { return Key.GetBytes(Algorithm.Key.Length); }
            else { return Key.GetBytes(KeySize); }
        }

        public static void Encrypt(SymmetricAlgorithm Algorithm, string InputPath, string OutputPath, string Password, string Salt = "HenkEncryptSalt", int Iterations = 10000, int KeySize = 0, int BufferSize = 1048576) => Encrypt(Algorithm, InputPath, OutputPath, CreateKey(Algorithm, Password, Salt, Iterations, KeySize), BufferSize);
        public static void Encrypt(SymmetricAlgorithm Algorithm, string InputPath, string OutputPath, byte[] Key, int BufferSize = 1048576)
        {
            ZipFile.CreateFromDirectory(InputPath, OutputPath + ".Temp");
            using (FileStream OutputStream = new FileStream(OutputPath, FileMode.Create))
            {
                Algorithm.Key = Key;
                Algorithm.GenerateIV();

                OutputStream.Write(Algorithm.IV, 0, Algorithm.IV.Length);

                using (CryptoStream cs = new CryptoStream(OutputStream, Algorithm.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    using (FileStream InputStream = new FileStream(OutputPath + ".Temp", FileMode.Open))
                    {
                        byte[] Buffer = new byte[BufferSize];
                        int Read;

                        while ((Read = InputStream.Read(Buffer, 0, Buffer.Length)) > 0)
                            cs.Write(Buffer, 0, Read);
                    }
                }
            }
            File.Delete(OutputPath + ".Temp");
        }

        public static void Decrypt(SymmetricAlgorithm Algorithm, string InputPath, string OutputPath, string Password, string Salt = "HenkEncryptSalt", int Iterations = 10000, int KeySize = 0, int BufferSize = 1048576) => Decrypt(Algorithm, InputPath, OutputPath, CreateKey(Algorithm, Password, Salt, Iterations, KeySize), BufferSize);
        public static void Decrypt(SymmetricAlgorithm Algorithm, string InputPath, string OutputPath, byte[] Key, int BufferSize = 1048576)
        {
            using (FileStream InputStream = new FileStream(InputPath, FileMode.Open))
            {
                byte[] IV = new byte[Algorithm.IV.Length];
                InputStream.Read(IV, 0, IV.Length);

                Algorithm.Key = Key;
                Algorithm.IV = IV;

                using (CryptoStream cs = new CryptoStream(InputStream, Algorithm.CreateDecryptor(), CryptoStreamMode.Read))
                {
                    using (FileStream OutputStream = new FileStream(OutputPath + ".Temp", FileMode.Create))
                    {
                        int Read;
                        byte[] Buffer = new byte[BufferSize];

                        while ((Read = cs.Read(Buffer, 0, Buffer.Length)) > 0)
                            OutputStream.Write(Buffer, 0, Read);
                    }
                }
            }
            ZipFile.ExtractToDirectory(OutputPath + ".Temp", OutputPath);
            File.Delete(OutputPath + ".Temp");
        }
    }
}
