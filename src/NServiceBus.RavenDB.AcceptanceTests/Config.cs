﻿using System;
using NServiceBus;
using NServiceBus.Persistence;
using Raven.Client.Document;

public class ConfigureRavenDBPersistence
{
    DocumentStore documentStore;

    public void Configure(BusConfiguration config)
    {
        documentStore = new DocumentStore
        {
            Url = "http://localhost:8081",
            DefaultDatabase = Guid.NewGuid().ToString(),

        };

        documentStore.Initialize();

        config.UsePersistence<RavenDBPersistence>().DoNotSetupDatabasePermissions().SetDefaultDocumentStore(documentStore);
    }

    public void Cleanup()
    {
        var client = documentStore.AsyncDatabaseCommands.ForSystemDatabase();

        var deleteUrl = string.Format("/admin/databases/{0}?hard-delete=true", Uri.EscapeDataString(documentStore.DefaultDatabase));

        client.CreateRequest(deleteUrl, "DELETE").ExecuteRequestAsync().Wait();
    }
}