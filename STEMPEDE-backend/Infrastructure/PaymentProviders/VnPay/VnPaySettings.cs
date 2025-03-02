using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.PaymentProviders.VnPay
{
    public class VnPaySettings
    {
        public string vnp_TmnCode { get; set; } = null!;
        public string vnp_HashSecret { get; set; } = null!;
        public string vnp_BaseUrl { get; set; } = null!;
        public string vnp_Command { get; set; } = null!;
        public string vnp_CurrCode { get; set; } = null!;
        public string vnp_Locale { get; set; } = null!;
        public string vnp_ReturnUrl { get; set; } = null!;
    }
}
