using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;

namespace MyAssistant.Core
{
    public class QdrantSupport
    {
        private const string QDRANT_DOWNLOAD_URL = "https://github.com/qdrant/qdrant/releases/download/v1.14.1/qdrant-x86_64-pc-windows-msvc.zip";
        private const string QDRANT_FOLDER = "database";
        private const string QDRANT_EXE = "qdrant.exe";
        private const string QDRANT_CONFIG_FILE = "config.yaml";
        private const int QDRANT_PORT = 6333;
        private const string QDRANT_ENDPOINT = "http://localhost:6333";

        private readonly ILogger<QdrantSupport> _logger;
        private readonly string _appBasePath;
        private readonly string _qdrantPath;
        private readonly string _qdrantExePath;
        private readonly string _qdrantConfigPath;
        private Process? _qdrantProcess;
        private readonly HttpClient _httpClient;

        public QdrantSupport(ILogger<QdrantSupport> logger)
        {
            _logger = logger;
            _appBasePath = AppDomain.CurrentDomain.BaseDirectory;
            _qdrantPath = Path.Combine(_appBasePath, QDRANT_FOLDER);
            _qdrantExePath = Path.Combine(_qdrantPath, QDRANT_EXE);
            _qdrantConfigPath = Path.Combine(_qdrantPath, QDRANT_CONFIG_FILE);
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// 确保 Qdrant 已安装并运行
        /// </summary>
        /// <returns></returns>
        public async Task<bool> EnsureQdrantRunningAsync()
        {
            try
            {
                // 首先检查是否已经在运行
                if (await IsQdrantRunningAsync())
                {
                    _logger.LogInformation("Qdrant 已经在运行");
                    return true;
                }

                // 检查是否已安装
                if (!IsQdrantInstalled())
                {
                    _logger.LogInformation("Qdrant 未安装，开始安装流程");
                    await InstallQdrantAsync();
                }

                // 启动 Qdrant
                await StartQdrantAsync();

                // 等待服务启动
                await WaitForQdrantToStartAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "确保 Qdrant 运行失败");

                // 如果失败，清空文件夹重新安装
                try
                {
                    await ReinstallQdrantAsync();
                    await StartQdrantAsync();
                    await WaitForQdrantToStartAsync();
                    return true;
                }
                catch (Exception reinstallEx)
                {
                    _logger.LogError(reinstallEx, "重新安装 Qdrant 失败");
                    return false;
                }
            }
        }

        /// <summary>
        /// 检查 Qdrant 是否正在运行
        /// </summary>
        /// <returns></returns>
        private async Task<bool> IsQdrantRunningAsync()
        {
            try
            {
                using var response = await _httpClient.GetAsync($"{QDRANT_ENDPOINT}/health");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 检查 Qdrant 是否已安装
        /// </summary>
        /// <returns></returns>
        private bool IsQdrantInstalled()
        {
            return File.Exists(_qdrantExePath);
        }

        /// <summary>
        /// 安装 Qdrant
        /// </summary>
        /// <returns></returns>
        private async Task InstallQdrantAsync()
        {
            _logger.LogInformation("开始下载 Qdrant...");

            // 创建数据库目录
            Directory.CreateDirectory(_qdrantPath);

            // 下载 Qdrant
            var zipPath = Path.Combine(_qdrantPath, "qdrant.zip");
            await DownloadFileAsync(QDRANT_DOWNLOAD_URL, zipPath);

            _logger.LogInformation("开始解压 Qdrant...");

            // 解压
            ZipFile.ExtractToDirectory(zipPath, _qdrantPath, true);

            // 删除 zip 文件
            File.Delete(zipPath);

            // 创建配置文件
            await CreateQdrantConfigAsync();

            _logger.LogInformation("Qdrant 安装完成");
        }

        /// <summary>
        /// 重新安装 Qdrant
        /// </summary>
        /// <returns></returns>
        private async Task ReinstallQdrantAsync()
        {
            _logger.LogInformation("开始重新安装 Qdrant...");

            // 停止现有进程
            StopQdrant();

            // 清空文件夹
            if (Directory.Exists(_qdrantPath))
            {
                Directory.Delete(_qdrantPath, true);
            }

            // 重新安装
            await InstallQdrantAsync();
        }

        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="url"></param>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private async Task DownloadFileAsync(string url, string filePath)
        {
            using var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            await response.Content.CopyToAsync(fileStream);
        }

        /// <summary>
        /// 创建 Qdrant 配置文件
        /// </summary>
        /// <returns></returns>
        private async Task CreateQdrantConfigAsync()
        {
            var config = $@"log_level: INFO
            storage:
              storage_path: ./storage
              snapshots_path: ./snapshots
              temp_path: ./temp
            service:
              host: 0.0.0.0
              http_port: {QDRANT_PORT}
              grpc_port: 6334
              max_request_size_mb: 32
              max_workers: 0
              enable_cors: true
            cluster:
              enabled: false
            telemetry_disabled: true
            ";

            await File.WriteAllTextAsync(_qdrantConfigPath, config);
        }

        /// <summary>
        /// 启动 Qdrant
        /// </summary>
        /// <returns></returns>
        private async Task StartQdrantAsync()
        {
            if (_qdrantProcess != null && !_qdrantProcess.HasExited)
            {
                return;
            }

            _logger.LogInformation("启动 Qdrant 服务...");

            var startInfo = new ProcessStartInfo
            {
                FileName = _qdrantExePath,
                Arguments = $"--config-path {_qdrantConfigPath}",
                WorkingDirectory = _qdrantPath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            _qdrantProcess = Process.Start(startInfo);

            if (_qdrantProcess == null)
            {
                throw new InvalidOperationException("无法启动 Qdrant 进程");
            }

            // 异步读取输出日志
            _ = Task.Run(async () =>
            {
                try
                {
                    while (!_qdrantProcess.StandardOutput.EndOfStream)
                    {
                        var line = await _qdrantProcess.StandardOutput.ReadLineAsync();
                        if (!string.IsNullOrEmpty(line))
                        {
                            _logger.LogDebug($"Qdrant: {line}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "读取 Qdrant 输出时发生错误");
                }
            });

            _ = Task.Run(async () =>
            {
                try
                {
                    while (!_qdrantProcess.StandardError.EndOfStream)
                    {
                        var line = await _qdrantProcess.StandardError.ReadLineAsync();
                        if (!string.IsNullOrEmpty(line))
                        {
                            _logger.LogWarning($"Qdrant Error: {line}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "读取 Qdrant 错误输出时发生错误");
                }
            });

            _logger.LogInformation("Qdrant 进程已启动");
        }

        /// <summary>
        /// 等待 Qdrant 启动完成
        /// </summary>
        /// <returns></returns>
        private async Task WaitForQdrantToStartAsync()
        {
            _logger.LogInformation("等待 Qdrant 服务启动...");

            var maxAttempts = 30;
            var delay = TimeSpan.FromSeconds(2);

            for (int i = 0; i < maxAttempts; i++)
            {
                if (await IsQdrantRunningAsync())
                {
                    _logger.LogInformation("Qdrant 服务启动成功");
                    return;
                }

                await Task.Delay(delay);
            }

            throw new TimeoutException("Qdrant 服务启动超时");
        }

        /// <summary>
        /// 停止 Qdrant
        /// </summary>
        public void StopQdrant()
        {
            if (_qdrantProcess != null && !_qdrantProcess.HasExited)
            {
                _logger.LogInformation("停止 Qdrant 服务...");

                try
                {
                    _qdrantProcess.Kill();
                    _qdrantProcess.WaitForExit(5000);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "停止 Qdrant 进程时发生错误");
                }
                finally
                {
                    _qdrantProcess?.Dispose();
                    _qdrantProcess = null;
                }
            }
        }

        /// <summary>
        /// 获取 Qdrant 端点
        /// </summary>
        /// <returns></returns>
        public string GetQdrantEndpoint()
        {
            return QDRANT_ENDPOINT;
        }

        /// <summary>
        /// 获取 Qdrant 运行状态
        /// </summary>
        /// <returns></returns>
        public async Task<QdrantStatus> GetStatusAsync()
        {
            var isRunning = await IsQdrantRunningAsync();
            var isInstalled = IsQdrantInstalled();

            return new QdrantStatus
            {
                IsInstalled = isInstalled,
                IsRunning = isRunning,
                Endpoint = QDRANT_ENDPOINT,
                InstallPath = _qdrantPath
            };
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            StopQdrant();
            _httpClient?.Dispose();
        }
    }

    /// <summary>
    /// Qdrant 状态信息
    /// </summary>
    public class QdrantStatus
    {
        public bool IsInstalled { get; set; }
        public bool IsRunning { get; set; }
        public string Endpoint { get; set; } = string.Empty;
        public string InstallPath { get; set; } = string.Empty;
    }
}