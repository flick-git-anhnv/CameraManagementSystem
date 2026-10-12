// G4.3: Copy nguyên xi từ Players/FFMPEG/UserControls/PlateDuplicateFilter.cs (WinForms).
// Pure dedup logic, Dictionary<string, DateTime> — không có System.Drawing reference, không cần thay đổi gì.
using System;
using System.Collections.Generic;
using System.Linq;

namespace Kztek.Cameras
{
    /// <summary>
    /// Lưu biển số đã nhận dạng gần đây để lọc trùng lặp, tách riêng khỏi <see cref="AnvPlayerService"/>
    /// để có thể unit test độc lập mà không cần khởi tạo engine LPR (NhanDangBienSo) tốn tài nguyên
    /// GPU/model native.
    /// </summary>
    internal sealed class PlateDuplicateFilter
    {
        // Giới hạn cứng số lượng biển số lưu lại, phòng trường hợp cấu hình suppress-seconds lớn
        // + lưu lượng xe cao bất thường khiến Dictionary phình to khi camera chạy liên tục thời gian dài
        private const int MaxRecentPlateEntries = 500;

        private readonly Dictionary<string, DateTime> _recentDetectedPlates = new();
        private readonly object _lock = new();

        /// <summary>
        /// Thời gian (giây) lưu lại biển số đã nhận dạng để bỏ qua các lần nhận dạng trùng lặp gần đây.
        /// </summary>
        internal int SuppressSeconds { get; set; } = 10;

        /// <summary>
        /// Kiểm tra biển số vừa nhận dạng có trùng với biển số đã nhận dạng trong khoảng
        /// SuppressSeconds gần nhất hay không. Nếu chưa trùng thì ghi nhận lại thời điểm
        /// nhận dạng hiện tại cho biển số đó.
        /// </summary>
        internal bool IsRecentDuplicate(string plateText)
        {
            if (string.IsNullOrEmpty(plateText))
            {
                return false;
            }

            var now = DateTime.Now;
            var window = TimeSpan.FromSeconds(Math.Max(0, SuppressSeconds));

            lock (_lock)
            {
                // Dọn các biển số đã hết hạn (quá SuppressSeconds) để tránh tồn dữ liệu cũ
                foreach (var key in _recentDetectedPlates.Where(kv => now - kv.Value > window).Select(kv => kv.Key).ToList())
                {
                    _recentDetectedPlates.Remove(key);
                }

                // Vẫn còn quá nhiều entry sau khi dọn hết hạn -> loại bớt các entry cũ nhất
                while (_recentDetectedPlates.Count >= MaxRecentPlateEntries)
                {
                    var oldestKey = _recentDetectedPlates.OrderBy(kv => kv.Value).First().Key;
                    _recentDetectedPlates.Remove(oldestKey);
                }

                if (_recentDetectedPlates.TryGetValue(plateText, out var lastSeen) && now - lastSeen <= window)
                {
                    return true;
                }

                _recentDetectedPlates[plateText] = now;
                return false;
            }
        }

        internal void Clear()
        {
            lock (_lock)
            {
                _recentDetectedPlates.Clear();
            }
        }
    }
}
