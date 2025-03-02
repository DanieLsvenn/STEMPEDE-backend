using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;


namespace Infrastructure.PaymentProviders.VnPay
{
    public class VnPayService : IVnPayService
    {
        private readonly VnPaySettings _settings;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public VnPayService(IOptions<VnPaySettings> options, IHttpContextAccessor httpContextAccessor)
        {
            _settings = options.Value;
            _httpContextAccessor = httpContextAccessor;
        }

        public string CreatePaymentUrl(decimal amount, string orderDesc, string orderId)
        {
            // VNPAY quy ước: amount phải *100
            var vnp_Amount = ((long)(amount * 100)).ToString();
            var clientIp = GetClientIpAddress();

            var paramList = new SortedList<string, string>
            {
                { "vnp_Version", "2.1.0" },
                { "vnp_Command", _settings.vnp_Command },
                { "vnp_TmnCode", _settings.vnp_TmnCode },
                { "vnp_Amount", vnp_Amount },
                { "vnp_CurrCode", _settings.vnp_CurrCode },
                { "vnp_TxnRef", orderId }, // Id của AccountMembership
                { "vnp_OrderInfo", orderDesc },
                { "vnp_OrderType", "other" },
                { "vnp_Locale", _settings.vnp_Locale },
                { "vnp_ReturnUrl", _settings.vnp_ReturnUrl },
                { "vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss") },
                { "vnp_IpAddr", clientIp }
            };

            // Tạo chuỗi query
            var queryBuilder = new StringBuilder();
            foreach (var kv in paramList)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    queryBuilder.Append($"{kv.Key}={Uri.EscapeDataString(kv.Value)}&");
                }
            }
            queryBuilder.Length -= 1; // Xóa dấu & cuối

            // Ký
            var signData = queryBuilder.ToString();
            var vnp_SecureHash = HmacSha512(_settings.vnp_HashSecret, signData);

            // Tạo url cuối
            var paymentUrl = $"{_settings.vnp_BaseUrl}?{signData}&vnp_SecureHash={vnp_SecureHash}";
            return paymentUrl;
        }

        public bool VerifySignature(IDictionary<string, string> queryParams)
        {
            // Lấy vnp_SecureHash
            if (!queryParams.TryGetValue("vnp_SecureHash", out var vnpSecureHash))
                return false;

            // Sort & tạo chuỗi bỏ qua vnp_SecureHash
            var sortedList = new SortedList<string, string>();
            foreach (var kv in queryParams)
            {
                if (!kv.Key.Equals("vnp_SecureHash", StringComparison.OrdinalIgnoreCase))
                {
                    sortedList.Add(kv.Key, kv.Value);
                }
            }

            var rawData = new StringBuilder();
            foreach (var kv in sortedList)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    rawData.Append($"{kv.Key}={kv.Value}&");
                }
            }
            if (rawData.Length > 0)
                rawData.Length -= 1; // xóa & cuối

            var computedHash = HmacSha512(_settings.vnp_HashSecret, rawData.ToString());

            // So sánh
            return computedHash.Equals(vnpSecureHash, StringComparison.OrdinalIgnoreCase);
        }

        private string GetClientIpAddress()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return "0.0.0.0";

            string ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";

            if (httpContext.Request.Headers.ContainsKey("X-Forwarded-For"))
            {
                var forwardedIp = httpContext.Request.Headers["X-Forwarded-For"].ToString();
                if (!string.IsNullOrEmpty(forwardedIp))
                {
                    ip = forwardedIp.Split(',')[0].Trim();
                }
            }

            return ip;
        }

        private string HmacSha512(string key, string inputData)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            using (var hmac = new HMACSHA512(keyBytes))
            {
                var hashValue = hmac.ComputeHash(Encoding.UTF8.GetBytes(inputData));
                return BitConverter.ToString(hashValue).Replace("-", "").ToLower();
            }
        }
    }
}
