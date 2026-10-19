using Wib.Domain.Common;

namespace Wib.Domain.Members;

public static class WalletErrors
{
    public static readonly DomainError NotEnoughPoints = DomainError.Invalid("Wallet.NotEnoughPoints");
}
