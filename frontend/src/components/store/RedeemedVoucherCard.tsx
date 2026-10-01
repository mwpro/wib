import { CheckCircle2, Coins } from 'lucide-react'
import type { VoucherResponse } from '../../types/voucher'
import { formatRelativeTimePl } from '../../utils/relativeTime'

interface RedeemedVoucherCardProps {
  voucher: VoucherResponse
}

export function RedeemedVoucherCard({ voucher }: RedeemedVoucherCardProps) {
  const redeemedTimeText = voucher.redeemedAt
    ? `Zrealizowano ${formatRelativeTimePl(voucher.redeemedAt)}`
    : 'Zrealizowano'

  return (
    <div
      data-testid="redeemed-voucher-card"
      className="bg-stone-50/80 dark:bg-[#111319] border border-stone-200/70 dark:border-stone-800/70 rounded-xl p-3 flex items-center justify-between gap-3 text-stone-500 dark:text-stone-400"
    >
      <div className="flex items-center gap-2.5 min-w-0">
        <div className="w-8 h-8 rounded-lg bg-stone-100 dark:bg-stone-800 text-stone-400 dark:text-stone-500 flex items-center justify-center shrink-0">
          <CheckCircle2 className="w-4 h-4 text-emerald-600 dark:text-emerald-400" />
        </div>

        <div className="flex flex-col min-w-0">
          <span className="text-xs font-semibold text-stone-700 dark:text-stone-300 truncate">
            {voucher.titleSnapshot}
          </span>
          <span className="text-[11px] text-stone-400 dark:text-stone-500">
            {redeemedTimeText}
          </span>
        </div>
      </div>

      <div className="shrink-0 flex items-center gap-1 text-[11px] font-semibold text-stone-400 dark:text-stone-500 bg-stone-100 dark:bg-stone-800 px-2 py-0.5 rounded-md">
        <Coins className="h-3 w-3" />
        <span>{voucher.pointCostSnapshot} pkt</span>
      </div>
    </div>
  )
}
