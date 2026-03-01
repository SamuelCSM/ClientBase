using System;
using System.Collections.Generic;
using System.Linq;
using SQLite;
using UnityEngine;

namespace Framework.Data
{
    /// <summary>
    /// SQLite数据库助手类
    /// 基于SQLite-net封装，提供简化的数据库操作接口
    /// </summary>
    public class SQLiteHelper : IDisposable
    {
        private SQLiteConnection _connection;
        private readonly string _dbPath;
        private bool _disposed;
        private bool _inTransaction;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dbPath">数据库文件路径</param>
        public SQLiteHelper(string dbPath)
        {
            if (string.IsNullOrEmpty(dbPath))
            {
                throw new ArgumentNullException(nameof(dbPath));
            }

            _dbPath = dbPath;

            try
            {
                _connection = new SQLiteConnection(dbPath);
                Debug.Log($"[SQLiteHelper] 数据库已打开: {dbPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SQLiteHelper] 无法打开数据库: {dbPath}, 错误: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 获取数据库连接是否有效
        /// </summary>
        public bool IsConnected => _connection != null;

        /// <summary>
        /// 获取当前是否在事务中
        /// </summary>
        public bool IsInTransaction => _inTransaction;

        /// <summary>
        /// 获取底层SQLiteConnection（用于高级操作）
        /// </summary>
        public SQLiteConnection Connection => _connection;

        #region 查询方法

        /// <summary>
        /// 查询多条记录
        /// </summary>
        /// <typeparam name="T">结果类型</typeparam>
        /// <param name="sql">SQL查询语句</param>
        /// <param name="args">查询参数</param>
        /// <returns>查询结果列表</returns>
        public List<T> Query<T>(string sql, params object[] args) where T : new()
        {
            if (string.IsNullOrEmpty(sql))
            {
                throw new ArgumentNullException(nameof(sql));
            }

            EnsureConnected();

            try
            {
                var results = _connection.Query<T>(sql, args);
                Debug.Log($"[SQLiteHelper] 查询返回 {results.Count} 行");
                return results;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SQLiteHelper] 查询失败: {ex.Message}\nSQL: {sql}");
                throw;
            }
        }

        /// <summary>
        /// 查询单条记录
        /// </summary>
        /// <typeparam name="T">结果类型</typeparam>
        /// <param name="sql">SQL查询语句</param>
        /// <param name="args">查询参数</param>
        /// <returns>查询结果，如果没有结果返回default(T)</returns>
        public T QueryFirst<T>(string sql, params object[] args) where T : new()
        {
            var results = Query<T>(sql, args);
            return results.Count > 0 ? results[0] : default(T);
        }

        /// <summary>
        /// 使用LINQ查询（返回Table对象用于链式查询）
        /// </summary>
        /// <typeparam name="T">表类型</typeparam>
        /// <returns>Table查询对象</returns>
        public TableQuery<T> Table<T>() where T : new()
        {
            EnsureConnected();
            return _connection.Table<T>();
        }

        /// <summary>
        /// 根据主键获取记录
        /// </summary>
        /// <typeparam name="T">表类型</typeparam>
        /// <param name="pk">主键值</param>
        /// <returns>查询结果</returns>
        public T Get<T>(object pk) where T : new()
        {
            EnsureConnected();

            try
            {
                return _connection.Get<T>(pk);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SQLiteHelper] 根据主键获取记录失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 尝试根据主键获取记录
        /// </summary>
        /// <typeparam name="T">表类型</typeparam>
        /// <param name="pk">主键值</param>
        /// <returns>查询结果，如果不存在返回default(T)</returns>
        public T Find<T>(object pk) where T : new()
        {
            EnsureConnected();

            try
            {
                return _connection.Find<T>(pk);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SQLiteHelper] 查找记录失败: {ex.Message}");
                return default(T);
            }
        }

        #endregion

        #region 执行方法

        /// <summary>
        /// 执行SQL命令（INSERT、UPDATE、DELETE等）
        /// </summary>
        /// <param name="sql">SQL命令</param>
        /// <param name="args">参数</param>
        /// <returns>受影响的行数</returns>
        public int Execute(string sql, params object[] args)
        {
            if (string.IsNullOrEmpty(sql))
            {
                throw new ArgumentNullException(nameof(sql));
            }

            EnsureConnected();

            try
            {
                int affected = _connection.Execute(sql, args);
                Debug.Log($"[SQLiteHelper] 执行SQL影响了 {affected} 行");
                return affected;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SQLiteHelper] 执行SQL失败: {ex.Message}\nSQL: {sql}");
                throw;
            }
        }

        /// <summary>
        /// 执行标量查询（返回单个值）
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="sql">SQL查询</param>
        /// <param name="args">参数</param>
        /// <returns>查询结果</returns>
        public T ExecuteScalar<T>(string sql, params object[] args)
        {
            if (string.IsNullOrEmpty(sql))
            {
                throw new ArgumentNullException(nameof(sql));
            }

            EnsureConnected();

            try
            {
                return _connection.ExecuteScalar<T>(sql, args);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SQLiteHelper] 执行标量查询失败: {ex.Message}\nSQL: {sql}");
                throw;
            }
        }

        #endregion

        #region 插入方法

        /// <summary>
        /// 插入单条记录
        /// </summary>
        /// <param name="obj">要插入的对象</param>
        /// <returns>受影响的行数</returns>
        public int Insert(object obj)
        {
            EnsureConnected();

            try
            {
                int affected = _connection.Insert(obj);
                Debug.Log($"[SQLiteHelper] 插入记录成功");
                return affected;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SQLiteHelper] 插入记录失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 插入或替换记录
        /// </summary>
        /// <param name="obj">要插入的对象</param>
        /// <returns>受影响的行数</returns>
        public int InsertOrReplace(object obj)
        {
            EnsureConnected();

            try
            {
                int affected = _connection.InsertOrReplace(obj);
                Debug.Log($"[SQLiteHelper] 插入或替换记录成功");
                return affected;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SQLiteHelper] 插入或替换记录失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 批量插入记录
        /// </summary>
        /// <param name="objects">要插入的对象集合</param>
        /// <returns>插入的记录数</returns>
        public int InsertAll(System.Collections.IEnumerable objects)
        {
            EnsureConnected();

            try
            {
                int count = _connection.InsertAll(objects);
                Debug.Log($"[SQLiteHelper] 批量插入 {count} 条记录成功");
                return count;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SQLiteHelper] 批量插入失败: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region 更新方法

        /// <summary>
        /// 更新记录
        /// </summary>
        /// <param name="obj">要更新的对象</param>
        /// <returns>受影响的行数</returns>
        public int Update(object obj)
        {
            EnsureConnected();

            try
            {
                int affected = _connection.Update(obj);
                Debug.Log($"[SQLiteHelper] 更新记录成功");
                return affected;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SQLiteHelper] 更新记录失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 批量更新记录
        /// </summary>
        /// <param name="objects">要更新的对象集合</param>
        /// <returns>更新的记录数</returns>
        public int UpdateAll(System.Collections.IEnumerable objects)
        {
            EnsureConnected();

            try
            {
                int count = _connection.UpdateAll(objects);
                Debug.Log($"[SQLiteHelper] 批量更新 {count} 条记录成功");
                return count;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SQLiteHelper] 批量更新失败: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region 删除方法

        /// <summary>
        /// 删除记录
        /// </summary>
        /// <param name="obj">要删除的对象</param>
        /// <returns>受影响的行数</returns>
        public int Delete(object obj)
        {
            EnsureConnected();

            try
            {
                int affected = _connection.Delete(obj);
                Debug.Log($"[SQLiteHelper] 删除记录成功");
                return affected;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SQLiteHelper] 删除记录失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 根据主键删除记录
        /// </summary>
        /// <typeparam name="T">表类型</typeparam>
        /// <param name="pk">主键值</param>
        /// <returns>受影响的行数</returns>
        public int Delete<T>(object pk)
        {
            EnsureConnected();

            try
            {
                int affected = _connection.Delete<T>(pk);
                Debug.Log($"[SQLiteHelper] 根据主键删除记录成功");
                return affected;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SQLiteHelper] 根据主键删除记录失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 删除所有记录
        /// </summary>
        /// <typeparam name="T">表类型</typeparam>
        /// <returns>删除的记录数</returns>
        public int DeleteAll<T>()
        {
            EnsureConnected();

            try
            {
                int count = _connection.DeleteAll<T>();
                Debug.Log($"[SQLiteHelper] 删除所有记录成功，共 {count} 条");
                return count;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SQLiteHelper] 删除所有记录失败: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region 表操作

        /// <summary>
        /// 创建表
        /// </summary>
        /// <typeparam name="T">表类型</typeparam>
        public void CreateTable<T>() where T : new()
        {
            EnsureConnected();

            try
            {
                _connection.CreateTable<T>();
                Debug.Log($"[SQLiteHelper] 创建表 {typeof(T).Name} 成功");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SQLiteHelper] 创建表失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 删除表
        /// </summary>
        /// <typeparam name="T">表类型</typeparam>
        public void DropTable<T>() where T : new()
        {
            EnsureConnected();

            try
            {
                _connection.DropTable<T>();
                Debug.Log($"[SQLiteHelper] 删除表 {typeof(T).Name} 成功");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SQLiteHelper] 删除表失败: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region 事务支持

        /// <summary>
        /// 开始事务
        /// </summary>
        public void BeginTransaction()
        {
            if (_inTransaction)
            {
                throw new InvalidOperationException("事务已经开始");
            }

            EnsureConnected();

            try
            {
                _connection.BeginTransaction();
                _inTransaction = true;
                Debug.Log("[SQLiteHelper] 事务已开始");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SQLiteHelper] 开始事务失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 提交事务
        /// </summary>
        public void Commit()
        {
            if (!_inTransaction)
            {
                throw new InvalidOperationException("没有正在进行的事务");
            }

            try
            {
                _connection.Commit();
                _inTransaction = false;
                Debug.Log("[SQLiteHelper] 事务已提交");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SQLiteHelper] 提交事务失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 回滚事务
        /// </summary>
        public void Rollback()
        {
            if (!_inTransaction)
            {
                throw new InvalidOperationException("没有正在进行的事务");
            }

            try
            {
                _connection.Rollback();
                _inTransaction = false;
                Debug.Log("[SQLiteHelper] 事务已回滚");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SQLiteHelper] 回滚事务失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 在事务中执行操作
        /// </summary>
        /// <param name="action">要执行的操作</param>
        public void RunInTransaction(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            EnsureConnected();

            try
            {
                _connection.RunInTransaction(action);
                Debug.Log("[SQLiteHelper] 事务执行成功");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SQLiteHelper] 事务执行失败: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region 连接管理

        /// <summary>
        /// 关闭数据库连接
        /// </summary>
        public void Close()
        {
            if (_connection != null)
            {
                // 如果有未完成的事务，自动回滚
                if (_inTransaction)
                {
                    try
                    {
                        Rollback();
                        Debug.LogWarning("[SQLiteHelper] 关闭连接时自动回滚未完成的事务");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[SQLiteHelper] 回滚事务失败: {ex.Message}");
                    }
                }

                _connection.Close();
                _connection = null;
                Debug.Log("[SQLiteHelper] 数据库连接已关闭");
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                Close();
            }

            _disposed = true;
        }

        ~SQLiteHelper()
        {
            Dispose(false);
        }

        #endregion

        #region 私有方法

        private void EnsureConnected()
        {
            if (_connection == null)
            {
                throw new InvalidOperationException("数据库连接未打开");
            }
        }

        #endregion
    }
}
