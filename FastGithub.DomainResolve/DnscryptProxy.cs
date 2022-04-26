using FastGithub.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using static PInvoke.AdvApi32;

namespace FastGithub.DomainResolve
{
    /// <summary>
    /// DnscryptProxy服务
    /// </summary>
    sealed class DnscryptProxy
    {
        private readonly ILogger<DnscryptProxy> logger;
        private readonly string processName;
        private readonly string serviceName;
        private readonly string exeFilePath;
        private readonly string tomlFilePath;

        /// <summary>
        /// 相关进程
        /// </summary>
        private Process? process;

        /// <summary>
        /// 获取监听的节点
        /// </summary>
        public IPEndPoint? LocalEndPoint { get; private set; }

        /// <summary>
        /// DnscryptProxy服务
        /// </summary>
        /// <param name="logger"></param>
        public DnscryptProxy(ILogger<DnscryptProxy> logger)
        {
            const string PATH = "dnscrypt-proxy";
            const string NAME = "dnscrypt-proxy";

            this.logger = logger;
            this.processName = NAME;
            this.serviceName = $"{nameof(FastGithub)}.{NAME}";
            this.exeFilePath = Path.Combine(PATH, false ? $"{NAME}.exe" : NAME);
            this.tomlFilePath = Path.Combine(PATH, $"{NAME}.toml");
        }
        private void LogErrorWindowsNoAdmin(Exception ex)
        {
            logger.LogError(ex, "处理服务异常，没有管理员权限时，这种情况是正常的");
        }
        /// <summary>
        /// 启动dnscrypt-proxy
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                await this.StartCoreAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                this.logger.LogWarning($"{this.processName}启动失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 启动dnscrypt-proxy
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task StartCoreAsync(CancellationToken cancellationToken)
        {
            var port = GlobalListener.GetAvailablePort(5533);
            var localEndPoint = new IPEndPoint(IPAddress.Loopback, port);

            await TomlUtil.SetListensAsync(this.tomlFilePath, localEndPoint, cancellationToken);
            await TomlUtil.SetLogLevelAsync(this.tomlFilePath, 6, cancellationToken);
            await TomlUtil.SetLBStrategyAsync(this.tomlFilePath, "ph", cancellationToken);
            await TomlUtil.SetMinMaxTTLAsync(this.tomlFilePath, TimeSpan.FromMinutes(1d), TimeSpan.FromMinutes(2d), cancellationToken);
            this.process = StartDnscryptProxy();

            if (this.process != null)
            {
                this.LocalEndPoint = localEndPoint;
                this.process.EnableRaisingEvents = true;
                this.process.Exited += (s, e) => this.LocalEndPoint = null;
            }
        }

        /// <summary>
        /// 停止服务
        /// </summary>
        public void Stop()
        {
            try
            {
                if (OperatingSystem.IsWindows() && Environment.UserInteractive == false)
                {
                    try
                    {
                        ServiceInstallUtil.StopAndDeleteService(this.serviceName);
                    }
                    catch (Exception ex)
                    {

                        LogErrorWindowsNoAdmin(ex);
                    }
                }

                if (this.process != null && this.process.HasExited == false)
                {
                    this.process.Kill();
                }
            }
            catch (Exception ex)
            {
                this.logger.LogWarning($"{this.processName}停止失败：{ex.Message }");
            }
            finally
            {
                this.LocalEndPoint = null;
            }
        }

        /// <summary>
        /// 启动DnscryptProxy进程
        /// </summary> 
        /// <returns></returns>
        private Process? StartDnscryptProxy()
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = this.exeFilePath,
                WorkingDirectory = Path.GetDirectoryName(this.exeFilePath),
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });
        }

        /// <summary>
        /// 转换为字符串
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.processName;
        }
    }
}
