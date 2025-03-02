using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.PaymentProviders.VnPay
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(decimal amount, string orderDesc, string orderId);
        bool VerifySignature(IDictionary<string, string> queryParams);
    }
}
