﻿#region Licence
/* The MIT License (MIT)
Copyright © 2015 Ian Cooper <ian_hammond_cooper@yahoo.co.uk>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the “Software”), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE. */

#endregion

using System;
using System.Data.SqlServerCe;
using System.IO;
using Machine.Specifications;
using Nito.AsyncEx;
using paramore.brighter.commandprocessor;
using paramore.brighter.commandprocessor.Logging;
using paramore.brighter.commandprocessor.messagestore.mssql;

namespace paramore.commandprocessor.tests.MessageStore.MsSql
{
    [Subject(typeof(MsSqlMessageStore))]
    [Tags("MessageStoreTests")]
    public class When_Writing_A_Message_To_The_Message_Store_Async
    {
        private const string ConnectionString = "DataSource=\"" + TestDbPath + "\"";
        private const string TableName = "test_messages";
        private const string TestDbPath = "test.sdf";
        private static readonly string key1 = "name1";
        private static readonly string key2 = "name2";
        private static Message s_messageEarliest;
        private static MsSqlMessageStore s_sqlMessageStore;
        private static Message s_storedMessage;
        private static readonly string value1 = "value1";
        private static readonly string value2 = "value2";

        private Cleanup _cleanup = () => CleanUpDb();

        private Establish _context = () =>
        {
            CleanUpDb();
            CreateTestDb();

            s_sqlMessageStore = new MsSqlMessageStore(
                new MsSqlMessageStoreConfiguration(ConnectionString, TableName,
                    MsSqlMessageStoreConfiguration.DatabaseType.SqlCe),
                new LogProvider.NoOpLogger());
            var messageHeader = new MessageHeader(Guid.NewGuid(), "test_topic", MessageType.MT_DOCUMENT,
                DateTime.UtcNow.AddDays(-1), 5, 5);
            messageHeader.Bag.Add(key1, value1);
            messageHeader.Bag.Add(key2, value2);

            s_messageEarliest = new Message(messageHeader, new MessageBody("message body"));
            AsyncContext.Run(async () => await s_sqlMessageStore.AddAsync(s_messageEarliest));
        };

        private Because _of = () => { AsyncContext.Run(async () => s_storedMessage = await s_sqlMessageStore.GetAsync(s_messageEarliest.Id)); };

        private It _should_read_the_message_from_the__sql_message_store =
            () => s_storedMessage.Body.Value.ShouldEqual(s_messageEarliest.Body.Value);

        private It _should_read_the_message_header_first_bag_item_from_the__sql_message_store = () =>
        {
            s_storedMessage.Header.Bag.ContainsKey(key1).ShouldBeTrue();
            s_storedMessage.Header.Bag[key1].ShouldEqual(value1);
        };

        private It _should_read_the_message_header_second_bag_item_from_the__sql_message_store = () =>
        {
            s_storedMessage.Header.Bag.ContainsKey(key2).ShouldBeTrue();
            s_storedMessage.Header.Bag[key2].ShouldEqual(value2);
        };

        private It _should_read_the_message_header_timestamp_from_the__sql_message_store =
            () => s_storedMessage.Header.TimeStamp.ShouldEqual(s_messageEarliest.Header.TimeStamp);

        private It _should_read_the_message_header_topic_from_the__sql_message_store =
            () => s_storedMessage.Header.Topic.ShouldEqual(s_messageEarliest.Header.Topic);

        private It _should_read_the_message_header_type_from_the__sql_message_store =
            () => s_storedMessage.Header.MessageType.ShouldEqual(s_messageEarliest.Header.MessageType);

        private static void CleanUpDb()
        {
            File.Delete(TestDbPath);
        }

        private static void CreateTestDb()
        {
            var en = new SqlCeEngine(ConnectionString);
            en.CreateDatabase();

            var sql = SqlMessageStoreBuilder.GetDDL(TableName);

            using (var cnn = new SqlCeConnection(ConnectionString))
            using (var cmd = cnn.CreateCommand())
            {
                cmd.CommandText = sql;
                cnn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}
