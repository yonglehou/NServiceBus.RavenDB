﻿using System;
using System.Collections.Generic;

namespace NServiceBus.RavenDB.Outbox
{
    using System.Linq;
    using NServiceBus.Outbox;
    using NServiceBus.RavenDB.Persistence;
    using Raven.Client;

    class OutboxPersister : IOutboxStorage
    {
        public OutboxPersister(ISessionProvider sessionProvider)
        {
            this.sessionProvider = sessionProvider;
        }

        // TODO this is a friction point, as it potentially allows for TryGet and SetAsDispatched to work
        // TODO against a different DocumentStore than the one sessionProvider is working against
        public IDocumentStore DocumentStore { get; set; }

        public bool TryGet(string messageId, out OutboxMessage message)
        {
            OutboxRecord result;
            using (var session = DocumentStore.OpenSession())
            {
                // We use Load operation and not queries to avoid stale results
                result = session.Load<OutboxRecord>(GetOutboxRecordId(messageId));
            }

            if (result == null)
            {
                message = null;
                return false;
            }

            message = new OutboxMessage(result.MessageId);
            message.TransportOperations.AddRange(
                result.TransportOperations.Select(t => new TransportOperation(t.MessageId, t.Options, t.Message, t.Headers))
                );

            return true;
        }

        public void Store(string messageId, IEnumerable<TransportOperation> transportOperations)
        {
            using (var session = sessionProvider.Session)
            {
                session.Store(new OutboxRecord
                {
                    MessageId = messageId,
                    Dispatched = false,
                    TransportOperations = transportOperations.Select(t => new OutboxRecord.OutboxOperation
                    {
                        Message = t.Body,
                        Headers = t.Headers,
                        MessageId = t.MessageId,
                        Options = t.Options,
                    }).ToList()
                }, GetOutboxRecordId(messageId));

                session.SaveChanges();
            }
        }

        public void SetAsDispatched(string messageId)
        {
            using (var session = DocumentStore.OpenSession())
            {
                session.Advanced.UseOptimisticConcurrency = true;
                var outboxMessage = session.Load<OutboxRecord>(GetOutboxRecordId(messageId));
                if (outboxMessage == null || outboxMessage.Dispatched)
                    return;

                outboxMessage.Dispatched = true;
                outboxMessage.DispatchedAt = DateTime.UtcNow;

                // TODO the NHibernate implementation cleans up all operations associated with this message
                // We can either do this here as well, or hard delete the outbox message

                session.SaveChanges();
            }
        }

        readonly ISessionProvider sessionProvider;

        private static string GetOutboxRecordId(string messageId)
        {
            return "Outbox/" + messageId;
        }
    }
}
