namespace NServiceBus.RavenDB.Persistence.SubscriptionStorage
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography;
    using System.Text;
    using Raven.Imports.Newtonsoft.Json;
    using Unicast.Subscriptions;

    class Subscription
    {
        public string Id { get; set; }

        [JsonConverter(typeof(MessageTypeConverter))]
        public MessageType MessageType { get; set; }

        public List<Address> Clients { get; set; }

        public static string FormatId(MessageType messageType)
        {
            // use MD5 hash to get a 16-byte hash of the string
            using (var provider = new MD5CryptoServiceProvider())
            {
                var inputBytes = Encoding.Default.GetBytes(messageType.TypeName + "/" + messageType.Version.Major);
                var hashBytes = provider.ComputeHash(inputBytes);
                // generate a guid from the hash:
                var id = new Guid(hashBytes);

                return string.Format("Subscriptions/{0}", id);
            }
        }
    }
}