using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace Emzi0767.Devi.Crypto
{
    internal sealed class KeyManager
    {
        private static Encoding Encoding { get; }

        private Dictionary<string, byte[]> Keys { get; }
        private byte XorKey { get; }
        private string Filename { get; }

        static KeyManager()
        {
            Encoding = new UTF8Encoding(false);
        }

        public KeyManager(string filename)
        {
            this.Filename = filename;
            this.Keys = new Dictionary<string, byte[]>();

            if (File.Exists(filename))
            {
                var json = "{}";

                using (var fs = File.OpenRead(filename))
                using (var sr = new StreamReader(fs, Encoding))
                    json = sr.ReadToEnd();

                var ks = JsonConvert.DeserializeObject<KeyStore>(json);
                var b = ks.XorKey;

                foreach (var (k, v) in ks.RawKeys)
                {
                    var bv = Convert.FromBase64String(v);
                    for (var i = 0; i < bv.Length; i++)
                        bv[i] ^= b;

                    this.Keys[k] = bv;
                }
                this.XorKey = b;
            }
            else
            {
                var b = new byte[1];
                using (var rng = RandomNumberGenerator.Create())
                    rng.GetNonZeroBytes(b);
                this.XorKey = b[0];

                var ks = new KeyStore
                {
                    XorKey = this.XorKey,
                    RawKeys = new Dictionary<string, string>()
                };

                var json = JsonConvert.SerializeObject(ks);

                using (var fs = File.Create(this.Filename))
                using (var sw = new StreamWriter(fs, Encoding))
                    sw.WriteLine(json);
            }
        }

        public bool HasKey(string name)
        {
            return this.Keys.ContainsKey(name);
        }

        public void AddKey(string name, int bit_size)
        {
            if (bit_size % 8 != 0)
                throw new ArgumentException("Bit size must be divisible by 8.", nameof(bit_size));

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));

            if (this.Keys.ContainsKey(name))
                throw new ArgumentException("A key with that name is already present.", nameof(name));

            var buff = new byte[bit_size / 8];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(buff);

            this.Keys[name] = buff;
            this.UpdateStore();
        }

        public void RemoveKey(string name)
        {
            if (!this.Keys.ContainsKey(name))
                throw new ArgumentException("A key with that name does not exist.", nameof(name));

            this.Keys.Remove(name);
            this.UpdateStore();
        }

        public void RegenerateKey(string name)
        {
            if (!this.Keys.ContainsKey(name))
                throw new ArgumentException("A key with that name does not exist.", nameof(name));

            var buff = this.Keys[name];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(buff);

            this.UpdateStore();
        }

        public void Encrypt(ref byte[] data, string key_name)
        {
            if (!this.Keys.ContainsKey(key_name))
                throw new ArgumentException("A key with that name does not exist.", nameof(key_name));

            if (data == null)
                throw new ArgumentNullException(nameof(data));

            var rdat = data;

            using (var aes = Rijndael.Create())
            {
                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                var iv = new byte[aes.BlockSize / 8];
                var key = this.Keys[key_name];
                var b = this.XorKey;
                using (var rng = RandomNumberGenerator.Create())
                    rng.GetBytes(iv);

                aes.Key = key;
                aes.IV = iv;

                using (var aes_enc = aes.CreateEncryptor())
                    data = aes_enc.TransformFinalBlock(rdat, 0, rdat.Length);

                for (var i = 0; i < data.Length; i++)
                    data[i] ^= b;

                var cdata = data;
                data = new byte[data.Length + iv.Length];
                Array.Copy(iv, data, iv.Length);
                Array.Copy(cdata, 0, data, iv.Length, cdata.Length);
            }
        }

        public void Encrypt(string str, out byte[] data, string key_name)
        {
            if (str == null)
                throw new ArgumentNullException(nameof(str));

            data = Encoding.GetBytes(str);
            this.Encrypt(ref data, key_name);
        }

        public void Decrypt(ref byte[] data, string key_name)
        {
            if (!this.Keys.ContainsKey(key_name))
                throw new ArgumentException("A key with that name does not exist.", nameof(key_name));

            if (data == null)
                throw new ArgumentNullException(nameof(data));

            var rdat = data;

            using (var aes = Rijndael.Create())
            {
                aes.KeySize = 256;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                var iv = new byte[aes.BlockSize / 8];
                var key = this.Keys[key_name];
                var b = this.XorKey;
                Array.Copy(rdat, iv, iv.Length);

                aes.Key = key;
                aes.IV = iv;

                for (var i = iv.Length; i < data.Length; i++)
                    data[i] ^= b;

                using (var aes_dec = aes.CreateDecryptor())
                    data = aes_dec.TransformFinalBlock(data, iv.Length, data.Length - iv.Length);
            }
        }

        public void Decrypt(byte[] data, out string str, string key_name)
        {
            this.Decrypt(ref data, key_name);
            str = Encoding.GetString(data);
        }

        private void UpdateStore()
        {
            var kd = new Dictionary<string, string>();
            var b = this.XorKey;

            foreach (var (k, v) in this.Keys)
            {
                var bv = new byte[v.Length];
                for (var i = 0; i < bv.Length; i++)
                    bv[i] = (byte)(v[i] ^ b);

                kd[k] = Convert.ToBase64String(bv);
            }

            var ks = new KeyStore
            {
                XorKey = b,
                RawKeys = kd
            };
            var json = JsonConvert.SerializeObject(ks);

            using (var fs = File.Create(this.Filename))
            using (var sw = new StreamWriter(fs, Encoding))
                sw.WriteLine(json);
        }
    }
}
