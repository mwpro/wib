using Wib.Domain.Common;

namespace Wib.Domain.Vouchers;

public static class VoucherErrors
{
    public static readonly DomainError NotFound = DomainError.NotFound("Voucher.NotFound");
    public static readonly DomainError AlreadyRedeemed = DomainError.Invalid("Voucher.AlreadyRedeemed");
}
