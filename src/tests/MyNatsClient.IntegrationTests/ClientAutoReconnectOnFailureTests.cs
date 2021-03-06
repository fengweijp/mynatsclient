﻿using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using MyNatsClient.Events;
using Xunit;

namespace MyNatsClient.IntegrationTests
{
    public class ClientAutoReconnectOnFailureTests : ClientIntegrationTests
    {
        private NatsClient _client;
        private readonly ConnectionInfo _cnInfoWithAutoReconnect;
        private readonly ConnectionInfo _cnInfoWithNoAutoReconnect;

        public ClientAutoReconnectOnFailureTests()
        {
            _cnInfoWithAutoReconnect = ConnectionInfo.Clone();
            _cnInfoWithAutoReconnect.AutoReconnectOnFailure = true;

            _cnInfoWithNoAutoReconnect = ConnectionInfo.Clone();
            _cnInfoWithNoAutoReconnect.AutoReconnectOnFailure = false;
        }

        protected override void OnAfterEachTest()
        {
            _client?.Disconnect();
            _client?.Dispose();
            _client = null;
        }

        [Fact]
        public async Task Client_Should_reconnect_after_failure_When_configured_to_do_so()
        {
            const string subject = "test";
            var wasDisconnectedDueToFailure = false;
            var wasReconnected = false;

            _client = new NatsClient("tc1", _cnInfoWithAutoReconnect);
            _client.Connect();

            await _client.SubAsync(subject, "s1");

            _client.Events.OfType<ClientDisconnected>()
                .Where(ev => ev.Reason == DisconnectReason.DueToFailure)
                .Subscribe(ev =>
                {
                    wasDisconnectedDueToFailure = true;
                    ReleaseOne();
                });

            _client.Events.OfType<ClientConnected>()
                .Subscribe(ev =>
                {
                    wasReconnected = true;
                    ReleaseOne();
                });

            _client.MsgOpStream.Subscribe(msg =>
            {
                throw new Exception("FAIL");
            });

            await _client.PubAsync(subject, "This message will fail");

            //Wait for the Disconnected release and the Connected release
            WaitOne();
            WaitOne();

            wasDisconnectedDueToFailure.Should().BeTrue();
            wasReconnected.Should().BeTrue();
            _client.State.Should().Be(NatsClientState.Connected);
        }

        [Fact]
        public async Task Client_Should_not_reconnect_after_failure_When_not_configured_to_do_so()
        {
            const string subject = "test";
            var wasDisconnectedDueToFailure = false;
            var wasReconnected = false;

            _client = new NatsClient("tc1", _cnInfoWithNoAutoReconnect);
            _client.Connect();

            await _client.SubAsync(subject, "s1");

            _client.Events.OfType<ClientDisconnected>()
                .Where(ev => ev.Reason == DisconnectReason.DueToFailure)
                .Subscribe(ev =>
                {
                    wasDisconnectedDueToFailure = true;
                    ReleaseOne();
                });

            _client.Events.OfType<ClientConnected>()
                .Subscribe(ev =>
                {
                    wasReconnected = true;
                    ReleaseOne();
                });

            _client.MsgOpStream.Subscribe(msg =>
            {
                throw new Exception("FAIL");
            });

            await _client.PubAsync(subject, "This message will fail");

            //Wait for the Disconnected release and the potential Connected release
            WaitOne();
            WaitOne();

            wasDisconnectedDueToFailure.Should().BeTrue();
            wasReconnected.Should().BeFalse();
            _client.State.Should().Be(NatsClientState.Disconnected);
        }

        [Fact]
        public async Task Client_Should_not_reconnect_When_user_initiated_disconnect()
        {
            const string subject = "test";
            var wasDisconnectedDueToFailure = false;
            var wasDisconnected = false;
            var wasReconnected = false;

            _client = new NatsClient("tc1", _cnInfoWithAutoReconnect);
            _client.Connect();

            await _client.SubAsync(subject, "s1");

            _client.Events.OfType<ClientDisconnected>()
                .Subscribe(ev =>
                {
                    wasDisconnectedDueToFailure = ev.Reason == DisconnectReason.DueToFailure;
                    wasDisconnected = true;
                    ReleaseOne();
                });

            _client.Events.OfType<ClientConnected>()
                .Subscribe(ev =>
                {
                    wasReconnected = true;
                    ReleaseOne();
                });

            _client.Disconnect();

            //Wait for the Disconnected release and the potentiall Connected release
            WaitOne();
            WaitOne();

            wasDisconnectedDueToFailure.Should().BeFalse();
            wasDisconnected.Should().BeTrue();
            wasReconnected.Should().BeFalse();
            _client.State.Should().Be(NatsClientState.Disconnected);
        }
    }
}