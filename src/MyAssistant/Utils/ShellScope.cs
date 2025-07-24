namespace MyAssistant.Utils
{
    /// <summary>
    /// 静态Shell作用域，用于在单例服务中访问Scoped服务
    /// </summary>
    public static class ShellScope
    {
        private static IServiceProvider _serviceProvider;
        private static readonly object _lock = new object();

        /// <summary>
        /// 注册ServiceProvider
        /// </summary>
        /// <param name="serviceProvider">服务提供者</param>
        public static void Register(IServiceProvider serviceProvider)
        {
            lock (_lock)
            {
                _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            }
        }

        /// <summary>
        /// 获取ServiceProvider
        /// </summary>
        /// <returns>服务提供者实例</returns>
        /// <exception cref="InvalidOperationException">如果ServiceProvider未注册</exception>
        public static IServiceProvider GetServiceProvider()
        {
            lock (_lock)
            {
                if (_serviceProvider == null)
                {
                    throw new InvalidOperationException("ServiceProvider未注册。请在应用启动时调用 ShellScope.Register(serviceProvider)");
                }
                return _serviceProvider;
            }
        }

        /// <summary>
        /// 创建服务作用域
        /// </summary>
        /// <returns>服务作用域</returns>
        public static IServiceScope CreateScope()
        {
            return GetServiceProvider().CreateScope();
        }

        /// <summary>
        /// 获取服务实例
        /// </summary>
        /// <typeparam name="T">服务类型</typeparam>
        /// <returns>服务实例</returns>
        public static T GetService<T>() where T : class
        {
            return GetServiceProvider().GetService<T>();
        }

        /// <summary>
        /// 获取必需的服务实例
        /// </summary>
        /// <typeparam name="T">服务类型</typeparam>
        /// <returns>服务实例</returns>
        /// <exception cref="InvalidOperationException">如果服务未注册</exception>
        public static T GetRequiredService<T>() where T : notnull
        {
            return GetServiceProvider().GetRequiredService<T>();
        }

        /// <summary>
        /// 在新的作用域中执行操作
        /// </summary>
        /// <param name="action">要执行的操作</param>
        public static void ExecuteScoped(Action<IServiceProvider> action)
        {
            using var scope = CreateScope();
            action(scope.ServiceProvider);
        }

        /// <summary>
        /// 在新的作用域中执行异步操作
        /// </summary>
        /// <param name="func">要执行的异步操作</param>
        public static async Task ExecuteScopedAsync(Func<IServiceProvider, Task> func)
        {
            using var scope = CreateScope();
            await func(scope.ServiceProvider);
        }

        /// <summary>
        /// 在新的作用域中执行操作并返回结果
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="func">要执行的操作</param>
        /// <returns>操作结果</returns>
        public static T ExecuteScoped<T>(Func<IServiceProvider, T> func)
        {
            using var scope = CreateScope();
            return func(scope.ServiceProvider);
        }

        /// <summary>
        /// 在新的作用域中执行异步操作并返回结果
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="func">要执行的异步操作</param>
        /// <returns>操作结果</returns>
        public static async Task<T> ExecuteScopedAsync<T>(Func<IServiceProvider, Task<T>> func)
        {
            using var scope = CreateScope();
            return await func(scope.ServiceProvider);
        }

        /// <summary>
        /// 检查ServiceProvider是否已注册
        /// </summary>
        public static bool IsRegistered
        {
            get
            {
                lock (_lock)
                {
                    return _serviceProvider != null;
                }
            }
        }
    }
}
