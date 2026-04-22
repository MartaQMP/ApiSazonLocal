using Newtonsoft.Json;

namespace MvcSazonLocal.Extensions
{
    public static class SessionExtension
    {
        public static void SetObject(this ISession session, string key, object value)
        {
            string json = JsonConvert.SerializeObject(value);
            session.SetString(key, json);
        }

        public static T GetObject<T>(this ISession session, string key)
        {
            string datos = session.GetString(key);
            if (datos == null)
            {
                return default(T);
            }
            return JsonConvert.DeserializeObject<T>(datos);
        }
    }
}
