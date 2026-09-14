using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Volo.Abp.DependencyInjection;

namespace TuTa.Wms.AgvTasks
{
    /// <summary>
    /// 机台监控线程管理器（单例）。
    /// AgvTaskService 为瞬时对象，跨请求无法通过实例字段取消监控线程，
    /// 因此将每个任务对应的取消令牌集中保存在该单例中，
    /// 供 taskCancel / taskFinish 回调时结束对应的监控线程。
    /// </summary>
    public class AgvTaskMonitorManager : ISingletonDependency
    {
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _monitoringTasks =
            new ConcurrentDictionary<string, CancellationTokenSource>();

        /// <summary>
        /// 为任务注册监控取消令牌（带超时，超时后自动取消，避免机台无响应时线程常驻）。
        /// 同一任务重复注册时，旧的监控线程会被取消，避免线程叠加。
        /// </summary>
        /// <param name="taskCode">任务编号（AgvTask.ReqCode）</param>
        /// <param name="timeout">最长监控时长</param>
        /// <returns>本次注册的取消令牌源</returns>
        public CancellationTokenSource Register(string taskCode, TimeSpan timeout)
        {
            var cts = new CancellationTokenSource(timeout);
            _monitoringTasks.AddOrUpdate(taskCode, cts, (key, existing) =>
            {
                // 取消并释放旧的令牌，结束旧监控线程
                existing.Cancel();
                existing.Dispose();
                return cts;
            });
            return cts;
        }

        /// <summary>
        /// 取消并结束指定任务的监控线程
        /// </summary>
        /// <param name="taskCode">任务编号</param>
        /// <returns>true：找到并已取消；false：该任务没有正在进行的监控线程</returns>
        public bool TryCancel(string taskCode)
        {
            if (_monitoringTasks.TryRemove(taskCode, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 监控结束后移除令牌（仅当字典中保存的仍是本次注册的令牌时才移除并释放，
        /// 避免误删重复注册的新令牌）
        /// </summary>
        public void Unregister(string taskCode, CancellationTokenSource cts)
        {
            var kvp = new KeyValuePair<string, CancellationTokenSource>(taskCode, cts);
            if (((ICollection<KeyValuePair<string, CancellationTokenSource>>)_monitoringTasks).Remove(kvp))
            {
                cts.Dispose();
            }
        }
    }
}
