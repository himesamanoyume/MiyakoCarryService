using EFT;

namespace MiyakoCarryService.Client.Extensions
{
    public static class MongoIdExtensions
    {
        public static string MongoIdText(this MongoID id)
        {
            return id?.ToString();
        }

        public static MongoID ToMongoId(this string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            return MongoID.TryParse(id, out var mongoId) ? mongoId : null;
        }
    }
}
