using HashidsNet;

namespace HEMS.Services
{
    public interface IIdObfuscator
    {
        string Encode(int id);
        bool TryDecode(string? hash, out int id);
    }

    public class HashidsIdObfuscator : IIdObfuscator
    {
        private readonly Hashids _hashids;

        public HashidsIdObfuscator(IConfiguration configuration)
        {
            var salt = configuration["Hashids:Salt"] ?? "hems-default-salt";
            var minLength = int.TryParse(configuration["Hashids:MinLength"], out var length) ? length : 8;
            _hashids = new Hashids(salt, minLength);
        }

        public string Encode(int id) => _hashids.Encode(id);

        public bool TryDecode(string? hash, out int id)
        {
            id = 0;
            if (string.IsNullOrWhiteSpace(hash))
            {
                return false;
            }

            var decoded = _hashids.Decode(hash);
            if (decoded.Length != 1)
            {
                return false;
            }

            id = decoded[0];
            return true;
        }
    }
}
