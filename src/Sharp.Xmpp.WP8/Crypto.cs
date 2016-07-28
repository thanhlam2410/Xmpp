using System.Security.Cryptography;

namespace Sharp.Xmpp
{
    internal class Crypto
    {
        public static byte[] HMACSHA1(byte[] key, byte[] data)
        {
            using (var hmac = new HMACSHA1(key))
            {
                return hmac.ComputeHash(data);
            }
        }

        public static byte[] SHA1Managed(byte[] data)
        {
            using (var sha1 = new SHA1Managed())
            {
                return sha1.ComputeHash(data);
            }
        }

        public static byte[] RFC2898DerivesBytes(string password, byte[] salt, uint iteration)
        {
            var db = new Rfc2898DeriveBytes(password, salt, (int)iteration);
            return db.GetBytes(20);
        }
    }
}
