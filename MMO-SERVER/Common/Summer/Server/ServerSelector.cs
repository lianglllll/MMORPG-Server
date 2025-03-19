using HS.Protobuf.Common;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Common.Summer.Server
{
    public record ServerMetrics(
        double AverageLatency,
        int ActiveConnections,
        double CpuUsage
    );
    public record GeoLocation(double Latitude, double Longitude);
    public interface IGeoLocator
    {
        GeoLocation GetCurrentLocation();
    }
    public class NoServerAvailableException : Exception
    {
        public NoServerAvailableException(string message) : base(message) { }
    }

    public class ServerSelector
    {
        public enum SelectionStrategy
        {
            Random,             // 随机选择，简单但可能不够高效，适合负载均衡的初步实现。
            RoundRobin,         // 轮询，依次选择每个服务器，确保均匀分配请求。
            LowestLatency,      // 最低延迟，适用于对响应时间敏感的应用，比如实时系统。
            Geographic,         // 地理就近，根据客户端位置选择最近的服务器，减少网络延迟。
            LeastConnections,   // 最少连接数，避免过载服务器，优化资源利用。
            Weighted,           // 加权，根据服务器能力分配不同权重，适合异构服务器环境。
            Custom              // 自定义策略，用户可以根据特定需求实现自己的逻辑。
        }

        private readonly object _serversLock = new object();
        private readonly List<ServerInfoNode> _servers = new List<ServerInfoNode>();
        private readonly ReaderWriterLockSlim _metricsLock = new ReaderWriterLockSlim();
        private readonly Dictionary<ServerInfoNode, ServerMetrics> _serverMetrics = new Dictionary<ServerInfoNode, ServerMetrics>();
        private readonly Func<List<ServerInfoNode>, ServerInfoNode> _selectionFunction;
       
        private readonly IGeoLocator _geoLocator;
        private int _roundRobinIndex = -1;
        private static readonly Random _random = new Random();
        private static readonly object _randomLock = new object();

        public ServerSelector(
            IEnumerable<ServerInfoNode> initialServers = null,
            SelectionStrategy strategy = SelectionStrategy.Random,
            Func<List<ServerInfoNode>, ServerInfoNode> customStrategy = null,
            IGeoLocator geoLocator = null)
        {
            if (initialServers != null)
            {
                _servers.AddRange(initialServers);
            }

            _selectionFunction = strategy switch
            {
                SelectionStrategy.Random => RandomStrategy,
                SelectionStrategy.RoundRobin => RoundRobinStrategy,
                SelectionStrategy.LowestLatency => LowestLatencyStrategy,
                SelectionStrategy.Geographic => GeographicStrategy,
                SelectionStrategy.LeastConnections => LeastConnectionsStrategy,
                SelectionStrategy.Weighted => WeightedStrategy,
                SelectionStrategy.Custom => customStrategy ?? throw new ArgumentNullException(nameof(customStrategy)),
                _ => throw new ArgumentException("Invalid selection strategy")
            };

            if(strategy == SelectionStrategy.Geographic)
            {
                _geoLocator = geoLocator ?? throw new ArgumentNullException(nameof(geoLocator), "GeoLocator is required for geographic strategy");
            }
        }

        #region public
        public void AddServer(ServerInfoNode server)
        {
            lock (_serversLock)
            {
                if (!_servers.Contains(server))
                {
                    _servers.Add(server);
                }
            }
        }
        public void RemoveServer(ServerInfoNode server)
        {
            lock (_serversLock)
            {
                _servers.Remove(server);
            }
        }
        public List<ServerInfoNode> GetAvailableServers()
        {
            lock (_serversLock)
            {
                return new List<ServerInfoNode>(_servers);
            }
        }
        public ServerInfoNode SelectServer()
        {
            List<ServerInfoNode> currentServers;
            lock (_serversLock)
            {
                currentServers = new List<ServerInfoNode>(_servers);
            }

            if (currentServers.Count == 0)
            {
                Log.Warning("No servers available to connect");
                return null;
            }

            return _selectionFunction(currentServers);
        }
        public void UpdateServerMetrics(ServerInfoNode server, ServerMetrics metrics)
        {
            _metricsLock.EnterWriteLock();
            try
            {
                _serverMetrics[server] = metrics;
            }
            finally
            {
                _metricsLock.ExitWriteLock();
            }
        }
        #endregion

        #region Strategy
        private ServerInfoNode RandomStrategy(List<ServerInfoNode> servers)
        {
            lock (_randomLock)
            {
                return servers[_random.Next(servers.Count)];
            }
        }
        private ServerInfoNode RoundRobinStrategy(List<ServerInfoNode> servers)
        {
            int index = Interlocked.Increment(ref _roundRobinIndex);
            return servers[Math.Abs(index % servers.Count)];
        }
        private ServerInfoNode LowestLatencyStrategy(List<ServerInfoNode> servers)
        {
            _metricsLock.EnterReadLock();
            try
            {
                return servers.OrderBy(s =>
                    _serverMetrics.TryGetValue(s, out var m) ? m.AverageLatency : double.MaxValue
                ).First();
            }
            finally
            {
                _metricsLock.ExitReadLock();
            }
        }
        private ServerInfoNode GeographicStrategy(List<ServerInfoNode> servers)
        {
            var clientLocation = _geoLocator.GetCurrentLocation();
            return servers
                .Select(s => new { Server = s, Distance = CalculateDistance(clientLocation, GetServerLocation(s)) })
                .OrderBy(x => x.Distance)
                .First().Server;
        }
        private ServerInfoNode LeastConnectionsStrategy(List<ServerInfoNode> servers)
        {
            _metricsLock.EnterReadLock();
            try
            {
                return servers.OrderBy(s =>
                    _serverMetrics.TryGetValue(s, out var m) ? m.ActiveConnections : int.MaxValue
                ).First();
            }
            finally
            {
                _metricsLock.ExitReadLock();
            }
        }
        private ServerInfoNode WeightedStrategy(List<ServerInfoNode> servers)
        {
            var totalWeight = servers.Sum(s => s.Weight);
            var randomValue = _random.NextDouble() * totalWeight;

            foreach (var server in servers)
            {
                randomValue -= server.Weight;
                if (randomValue < 0)
                {
                    return server;
                }
            }
            return servers.Last();
        }
        #endregion

        #region tools
        private GeoLocation GetServerLocation(ServerInfoNode server)
        {
            return new GeoLocation(
                server.GeoLatitude,
                server.GeoLongitude
            );
        }
        private double CalculateDistance(GeoLocation a, GeoLocation b)
        {
            const double earthRadius = 6371e3;
            var lat1Rad = a.Latitude * Math.PI / 180;
            var lat2Rad = b.Latitude * Math.PI / 180;
            var deltaLat = (b.Latitude - a.Latitude) * Math.PI / 180;
            var deltaLon = (b.Longitude - a.Longitude) * Math.PI / 180;

            var haversineA = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                            Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                            Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(haversineA), Math.Sqrt(1 - haversineA));
            return earthRadius * c;
        }
        #endregion
    }
}