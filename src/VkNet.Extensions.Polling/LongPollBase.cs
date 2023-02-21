using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using VkNet.Abstractions;
using VkNet.Extensions.Polling.Models.Configuration;
using VkNet.Extensions.Polling.Models.State;

namespace VkNet.Extensions.Polling
{
    public abstract class LongPollBase<TLongPollResponse, TLongPollUpdate, TLongPollServerState, TLongPollConfiguration> : IDisposable
        where TLongPollServerState : ILongPollServerState
        where TLongPollConfiguration : ILongPollConfiguration
    {
        private readonly CancellationTokenSource _longPollStopTokenSource;

        private readonly IVkApi _vkApi;

        private readonly ChannelWriter<TLongPollUpdate> _updateChannelWriter;

        private readonly ChannelReader<TLongPollUpdate> _updateChannelReader;

        private bool _disposedValue;

        protected LongPollBase(IVkApi vkApi)
        {
            Channel<TLongPollUpdate> updateChannel = Channel.CreateUnbounded<TLongPollUpdate>(
                new UnboundedChannelOptions()
                {
                    SingleWriter = true
                });

            _updateChannelReader = updateChannel;
            _updateChannelWriter = updateChannel;

            _vkApi = vkApi;
            _longPollStopTokenSource = new CancellationTokenSource();
        }

        protected abstract Task<bool> ValidateAsync(IVkApi vkApi);

        public TLongPollConfiguration Configuration { get; private set; }

        public async Task Start(TLongPollConfiguration longPollConfiguration,
            CancellationToken cancellationToken = default)
        {
            if (!await ValidateAsync(_vkApi).ConfigureAwait(false))
            {
                throw new NotSupportedException("Выбранный тип лонг пулла недоступен для данного аккаунта.");
            }

            Configuration = longPollConfiguration;
            var linkedTokenSource =
                CancellationTokenSource.CreateLinkedTokenSource(_longPollStopTokenSource.Token, cancellationToken);

            _ = ReceiveUpdatesAsync(linkedTokenSource.Token);
        }

        protected abstract Task<TLongPollServerState> GetServerInformationAsync(IVkApi vkApi,
            TLongPollConfiguration longPollConfiguration, CancellationToken cancellationToken = default);

        protected abstract Task<TLongPollResponse> GetUpdatesAsync(IVkApi vkApi,
            TLongPollConfiguration longPollConfiguration, TLongPollServerState longPollServerStatus,
            CancellationToken cancellationToken = default);

        protected abstract IEnumerable<TLongPollUpdate> ConvertLongPollResponse(TLongPollResponse longPollResponse);

        public Task Stop()
        {
            _longPollStopTokenSource.Cancel();
            return Task.CompletedTask;
        }

        public ChannelReader<TLongPollUpdate> AsChannelReader()
        {
            return _updateChannelReader;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _vkApi.Dispose();
                    _longPollStopTokenSource.Dispose();
                }

                _disposedValue = true;
            }
        }

        void IDisposable.Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private async ValueTask ReceiveUpdatesAsync(CancellationToken cancellationToken = default)
        {
            var longPollServerInformation = await GetServerInformationAsync(_vkApi, Configuration, cancellationToken)
                .ConfigureAwait(false);

            while (!cancellationToken.IsCancellationRequested)
            {
                bool needRepeat;

                TLongPollResponse longPollResponse = default;

                do
                {
                    try
                    {
                        longPollResponse = await GetUpdatesAsync(_vkApi, Configuration,
                            longPollServerInformation,
                            cancellationToken)
                            .ConfigureAwait(false);

                        needRepeat = false;
                    }
                    catch
                    {
                        try
                        {
                            longPollServerInformation =
                                await GetServerInformationAsync(_vkApi, Configuration,
                                    cancellationToken)
                                    .ConfigureAwait(false);
                        }
                        catch
                        {
                        }

                        needRepeat = true;
                    }
                } while (needRepeat);

                var updates = ConvertLongPollResponse(longPollResponse);

                foreach (var update in updates)
                {
                    await _updateChannelWriter
                        .WriteAsync(update, cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                }

                await Task
                    .Delay(Configuration.RequestDelay)
                    .ConfigureAwait(false);
            }
        }
    }
}